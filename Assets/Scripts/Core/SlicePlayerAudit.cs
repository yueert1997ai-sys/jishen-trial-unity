using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Profiling;
using UnityEngine.UI;

// Dormant in normal play. Release-Player verification: -sliceAudit <output directory> [-sliceAuditSmoke].
[DefaultExecutionOrder(1000)]
public sealed class SlicePlayerAudit : MonoBehaviour
{
    [Serializable]
    private sealed class Report
    {
        public string utc, unity, gpu, renderer, mode, build, failure, upgrades, renderMethod, gcSource;
        public int width, height, choices, kills, restarts, errors, warnings, samples, gcCollections;
        public bool success, victory, phaseTwo, gcRecorderAvailable;
        public float runSeconds, bossSeconds, hp, meanFrameMs, p95FrameMs, maxFrameMs;
        public float[] encounters;
        public long gcBytesTotal, gcBytesMax;
        public int gcPositiveFrames;
        public long[] restartAllocatedBytes, restartManagedBytes;
        public int renderedFrames, captures;
        public int targetFrameRate, vSyncCount, dashes;
    }

    private static string output;
    private static bool smoke;
    private static bool dashReplay;
    private readonly Report report = new Report();
    private readonly List<float> frames = new List<float>(60000);
    private readonly Vector3[] route = { new Vector3(-4, 0, -5), new Vector3(4, 0, -5), new Vector3(4, 0, 5), new Vector3(-4, 0, 5) };
    private ProfilerRecorder allocations;
    private IEnumerator scenario;
    private bool finished, sampling;
    private int skipSamples, waypoint, gcStart;
    private float deadline;
    private RenderTexture target;
    private Camera uiCamera, sceneCamera;
    private GameManager configuredManager;
    private GamePhase configuredPhase;
    private string pendingCapture;
    private Func<long> allocatedOnThread;
    private long lastThreadBytes;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        string[] args = Environment.GetCommandLineArgs();
        int index = Array.IndexOf(args, "-sliceAudit");
        if (index < 0 || Application.isEditor) return;
        if (index + 1 >= args.Length) { Debug.LogError("Missing -sliceAudit output directory"); Application.Quit(1); return; }
        output = Path.GetFullPath(args[index + 1]);
        Directory.CreateDirectory(output);
        smoke = Array.IndexOf(args, "-sliceAuditSmoke") >= 0;
        dashReplay = Array.IndexOf(args, "-sliceAuditDash") >= 0;
        PlayerInputRouter.AllowUnfocusedReplay = true;
        Application.runInBackground = true;
        DontDestroyOnLoad(new GameObject("OptInPlayerAudit").AddComponent<SlicePlayerAudit>());
    }

    private void Start()
    {
        Application.logMessageReceived += OnLog;
        report.utc = DateTime.UtcNow.ToString("O");
        report.unity = Application.unityVersion;
        report.gpu = SystemInfo.graphicsDeviceName;
        report.renderer = SystemInfo.graphicsDeviceType.ToString();
        report.build = Application.version;
        report.targetFrameRate = Application.targetFrameRate;
        report.vSyncCount = QualitySettings.vSyncCount;
        report.mode = smoke ? "95-second smoke" : "complete live-fire replay";
        if (dashReplay) report.mode += " / repeated dash";
        report.renderMethod = "Complete Player simulation + scene/UI Camera.Render every LateUpdate to 1920x1080 RT; hidden window, no OS presentation cost";
        target = new RenderTexture(1920, 1080, 24) { antiAliasing = 4, name = "PlayerAudit1080p" };
        target.Create();
        uiCamera = new GameObject("PlayerAuditUICamera").AddComponent<Camera>();
        uiCamera.transform.SetParent(transform, false);
        uiCamera.transform.position = new Vector3(0, 10000, 0);
        uiCamera.orthographic = true;
        uiCamera.clearFlags = CameraClearFlags.Depth;
        uiCamera.cullingMask = 1 << 5;
        uiCamera.nearClipPlane = 0.01f;
        uiCamera.farClipPlane = 10;
        uiCamera.depth = 100;
        uiCamera.enabled = false;
        uiCamera.targetTexture = target;
        deadline = Time.realtimeSinceStartup + 1100;
        allocations = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame", 1);
        report.gcRecorderAvailable = allocations.Valid;
        report.gcSource = allocations.Valid ? "Unity GC Allocated In Frame" : "unavailable";
        if (!allocations.Valid)
        {
            var method = typeof(GC).GetMethod("GetAllocatedBytesForCurrentThread", Type.EmptyTypes);
            if (method != null)
            {
                allocatedOnThread = (Func<long>)Delegate.CreateDelegate(typeof(Func<long>), method);
                long before = allocatedOnThread();
                var probe = new byte[4096];
                lastThreadBytes = allocatedOnThread();
                GC.KeepAlive(probe);
                if (lastThreadBytes - before >= 4096) report.gcSource = "Mono main-thread allocated-byte deltas (includes replay/render harness; excludes worker threads)";
                else allocatedOnThread = null;
            }
        }
        scenario = Scenarios();
    }

    private void LateUpdate()
    {
        try { RenderFrame(); }
        catch (Exception e) { Finish(e.ToString()); }
    }

    private void RenderFrame()
    {
        var gm = GameManager.Instance;
        if (finished || gm == null) return;
        if (configuredManager != gm || configuredPhase != gm.Phase)
        {
            configuredManager = gm;
            configuredPhase = gm.Phase;
            sceneCamera = Camera.main;
            sceneCamera.targetTexture = target;
            sceneCamera.cullingMask &= ~(1 << 5);
            foreach (var canvas in FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (canvas.renderMode == RenderMode.WorldSpace) continue;
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = uiCamera;
                canvas.planeDistance = 1;
                foreach (var child in canvas.GetComponentsInChildren<Transform>(true)) child.gameObject.layer = 5;
            }
            skipSamples = 8;
        }
        Canvas.ForceUpdateCanvases();
        sceneCamera.Render();
        uiCamera.Render();
        report.renderedFrames++;
        AfterRender();
    }

    private void AfterRender()
    {
        if (finished) return;
        if (pendingCapture == null) return;
        var previous = RenderTexture.active;
        var texture = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
        try
        {
            RenderTexture.active = target;
            texture.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
            texture.Apply();
            var colors = new HashSet<Color32>();
            var pixels = texture.GetPixels32();
            for (int i = 0; i < pixels.Length; i += 97) colors.Add(pixels[i]);
            Require(colors.Count > 100, "Blank/flat Player render: " + pendingCapture);
            File.WriteAllBytes(Path.Combine(output, pendingCapture + ".png"), texture.EncodeToPNG());
            report.captures++;
            pendingCapture = null;
        }
        catch (Exception e) { Finish(e.ToString()); }
        finally { RenderTexture.active = previous; Destroy(texture); }
    }

    private void OnAuditedDash(Vector3 direction) { report.dashes++; }

    private void Update()
    {
        if (finished || scenario == null) return;
        try
        {
            long threadBytes = allocatedOnThread != null ? allocatedOnThread() : 0;
            long allocatedDelta = threadBytes - lastThreadBytes;
            lastThreadBytes = threadBytes;
            if (Time.realtimeSinceStartup > deadline) throw new Exception("Player audit timed out");
            if (!scenario.MoveNext()) { Finish(null); return; }
            if (sampling && GameManager.Instance != null && GameManager.Instance.IsCombatActive)
            {
                if (skipSamples > 0) { skipSamples--; return; }
                frames.Add(Time.unscaledDeltaTime * 1000);
                if (allocations.Valid || allocatedOnThread != null)
                {
                    long value = allocations.Valid ? allocations.LastValue : Math.Max(0, allocatedDelta);
                    report.gcBytesTotal += value;
                    report.gcBytesMax = Math.Max(report.gcBytesMax, value);
                    if (value > 0) report.gcPositiveFrames++;
                }
            }
        }
        catch (Exception e) { Finish(e.ToString()); }
    }

    private IEnumerator Scenarios()
    {
        yield return null;
        yield return null;
        GamePreferences.SetLanguage(false);
        GamePreferences.SetQuality(1);
        GamePreferences.SetMaster(0); // Silent background audit only; never save diagnostic preferences.
        float warm = Time.realtimeSinceStartup + 4;
        while (Time.realtimeSinceStartup < warm) yield return null;
        report.width = Screen.width;
        report.height = Screen.height;
        Require(SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null, "No Player renderer");
        Require(Screen.width >= 1280 && Screen.height >= 720, "Player render resolution too small");
        Capture("01_hangar");
        yield return null;
        yield return null;
        var gm = GameManager.Instance;
        gm.playerController.InputRouter.readKeyboard = false;
        if (dashReplay) gm.playerController.Dashed += OnAuditedDash;
        Click("DeployButton");
        gm.upgradeSystem.ResetUpgrades(5092026);
        float started = Time.time, bossStarted = -1;
        bool[] captured = new bool[6];
        gcStart = GC.CollectionCount(0);
        while (gm.Phase != GamePhase.Result && Time.time - started < (smoke ? 95 : 1000))
        {
            sampling = Time.time - started > 5;
            if (gm.Phase == GamePhase.Reward)
            {
                sampling = false;
                Capture("reward_" + report.choices);
                yield return null;
                yield return null;
                var options = gm.upgradeSystem.GenerateOptions();
                RunUpgradeKind[] desired = { RunUpgradeKind.BeamDamage, RunUpgradeKind.FireRate, RunUpgradeKind.PiercingRounds,
                    RunUpgradeKind.BurstCore, RunUpgradeKind.DashCapacitor, RunUpgradeKind.Nanorepair };
                int choice = options.FindIndex(o => o.kind == desired[report.choices]);
                if (choice < 0) choice = 0;
                Click("ChooseButton", "RewardCard" + choice);
                report.choices++;
                skipSamples = 8;
                Debug.Log("PLAYER_CHOICE " + report.choices + " " + gm.upgradeSystem.GetSummary());
            }
            if (gm.IsCombatActive)
            {
                Vector3 delta = route[waypoint] - gm.playerController.transform.position;
                delta.y = 0;
                if (delta.magnitude < 0.8f) waypoint = (waypoint + 1) % route.Length;
                gm.playerController.InputRouter.SetTouchMove(new Vector2(delta.x, delta.z).normalized);
                gm.playerController.InputRouter.QueueSkill();
                if (dashReplay && delta.magnitude > 5.7f && gm.playerController.IsDashReady)
                    gm.playerController.InputRouter.QueueDash();
                int encounter = gm.stageManager.CurrentEncounter;
                if (encounter < 6 && !captured[encounter] && gm.stageManager.EnemiesAlive >= 5)
                {
                    captured[encounter] = true;
                    Capture("encounter_" + encounter);
                }
                // The Boss is unique and only looked up during its encounter, not normal gameplay code.
                if (encounter == 6)
                {
                    var boss = FindFirstObjectByType<BossController>();
                    if (boss != null)
                    {
                        if (bossStarted < 0) { bossStarted = Time.time; Capture("boss_1"); }
                        if (!report.phaseTwo && boss.IsPhaseTwo) { report.phaseTwo = true; Capture("boss_2"); }
                    }
                }
            }
            yield return null;
        }
        sampling = false;
        report.gcCollections = GC.CollectionCount(0) - gcStart;
        report.victory = gm.LastResultVictory;
        report.runSeconds = Time.time - started;
        report.bossSeconds = bossStarted < 0 ? -1 : Time.time - bossStarted;
        report.hp = gm.playerStats.CurrentHp;
        report.kills = gm.Kills;
        report.upgrades = gm.upgradeSystem.GetSummary();
        report.encounters = gm.stageManager.EncounterSeconds.ToArray();
        Capture(smoke ? "smoke_end" : "result");
        yield return null;
        yield return null;
        Require(smoke ? gm.IsCombatActive : gm.Phase == GamePhase.Result && report.victory && report.choices == 6 && report.phaseTwo, "Live combat did not complete expected flow");
        if (!smoke) Require(report.runSeconds >= 600 && report.runSeconds <= 900, "Run outside 10-15 minutes");
        Require(frames.Count > 1000, "Insufficient rendered Player samples");
        Require(report.renderedFrames >= frames.Count && report.captures >= 3, "Player did not continuously render/capture");
        if (smoke) { Click("PauseButton"); yield return null; Click("RestartButton"); }
        else Click("ReturnHangarButton");
        var native = new List<long>();
        var managed = new List<long>();
        for (int i = 0; i < 4; i++)
        {
            yield return null;
            yield return null;
            float settle = Time.realtimeSinceStartup + 1;
            while (Time.realtimeSinceStartup < settle) yield return null;
            var unload = Resources.UnloadUnusedAssets();
            while (!unload.isDone) yield return null;
            GC.Collect();
            yield return null;
            Require(GameManager.Instance.Phase == GamePhase.Hangar, "Restart did not reach hangar");
            Require(FindObjectsByType<GameManager>(FindObjectsSortMode.None).Length == 1, "Duplicate game manager after restart");
            Require(FindObjectsByType<EnemyBase>(FindObjectsSortMode.None).Length == 0, "Live enemy leaked into hangar");
            native.Add(Profiler.GetTotalAllocatedMemoryLong());
            managed.Add(GC.GetTotalMemory(false));
            if (i < 3) { GameManager.Instance.RestartRun(); report.restarts++; }
        }
        report.restartAllocatedBytes = native.ToArray();
        report.restartManagedBytes = managed.ToArray();
        Require(native[3] - native[0] < 16 * 1024 * 1024, "Native allocation grows >16MiB over three restarts after explicit cleanup");
        Require(managed[3] - managed[0] < 4 * 1024 * 1024, "Managed allocation grows >4MiB over three restarts after explicit cleanup");
        Capture("restart_hangar");
        yield return null;
        yield return null;
        Require(report.errors == 0, "Runtime errors in Player log");
    }

    private void Capture(string name)
    {
        skipSamples = 8;
        Require(pendingCapture == null, "Previous Player frame was never rendered");
        pendingCapture = name;
    }

    private static void Click(string name, string parent = null)
    {
        var button = FindObjectsByType<Button>(FindObjectsSortMode.None).FirstOrDefault(b => b.name == name && (parent == null || b.transform.parent.name == parent));
        Require(button != null && button.interactable, "Missing usable button " + name);
        Canvas.ForceUpdateCanvases();
        var rect = button.GetComponent<RectTransform>();
        var canvas = button.GetComponentInParent<Canvas>();
        var pointer = new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left,
            position = RectTransformUtility.WorldToScreenPoint(canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera, rect.TransformPoint(rect.rect.center)) };
        var hits = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointer, hits);
        Require(hits.Count > 0 && hits[0].gameObject.GetComponentInParent<Button>() == button, "Blocked UI button " + name);
        ExecuteEvents.ExecuteHierarchy(hits[0].gameObject, pointer, ExecuteEvents.pointerClickHandler);
    }

    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
    private void OnLog(string message, string stack, LogType type)
    {
        if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) report.errors++;
        else if (type == LogType.Warning) report.warnings++;
    }

    private void Finish(string failure)
    {
        if (finished) return;
        finished = true;
        allocations.Dispose();
        report.failure = failure;
        report.success = failure == null && report.errors == 0;
        frames.Sort();
        report.samples = frames.Count;
        if (frames.Count > 0)
        {
            report.meanFrameMs = frames.Average();
            report.p95FrameMs = frames[Mathf.Min(frames.Count - 1, Mathf.FloorToInt(frames.Count * 0.95f))];
            report.maxFrameMs = frames[frames.Count - 1];
        }
        File.WriteAllText(Path.Combine(output, "player-report.json"), JsonUtility.ToJson(report, true));
        Debug.Log("SLICE_PLAYER_AUDIT " + (report.success ? "PASS" : "FAIL") + " " + failure);
        Application.logMessageReceived -= OnLog;
        Application.Quit(report.success ? 0 : 1);
    }

    private void OnDestroy()
    {
        Application.logMessageReceived -= OnLog;
        if (!finished) allocations.Dispose();
        if (target != null) { target.Release(); Destroy(target); }
    }
}
