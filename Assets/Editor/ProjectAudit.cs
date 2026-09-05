using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

[InitializeOnLoad]
public static class ProjectAudit
{
    private const string Active = "MechTrialAudit.Active";
    private static IEnumerator routine;
    private static int lastFrame;
    private static double deadline;
    private static readonly List<string> evidence = new List<string>();
    private static string Output => Path.GetFullPath("AuditEvidence");

    static ProjectAudit()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Active, false))
            {
                evidence.Clear();
                deadline = EditorApplication.timeSinceStartup + 220;
                lastFrame = -1;
                routine = SessionState.GetBool(Active + ".Shots", false) ? ShotDiagnostics() : RunScenarios();
                Application.logMessageReceived += CaptureLog;
                EditorApplication.update += Tick;
            }
        };
    }

    public static void Run()
    {
        SessionState.SetBool(Active + ".Shots", false);
        Directory.CreateDirectory(Output);
        Inventory();
        SessionState.SetBool(Active, true);
        EditorSceneManager.OpenScene("Assets/Scenes/Demo_Main.unity");
        EditorApplication.EnterPlaymode();
    }

    public static void RunShotDiagnostics()
    {
        Directory.CreateDirectory(Output);
        SessionState.SetBool(Active + ".Shots", true);
        SessionState.SetBool(Active, true);
        EditorSceneManager.OpenScene("Assets/Scenes/Demo_Main.unity");
        EditorApplication.EnterPlaymode();
    }

    private static IEnumerator ShotDiagnostics()
    {
        yield return null;
        var gm = GameManager.Instance;
        gm.BeginRun();
        gm.stageManager.StopAllCoroutines();
        gm.stageManager.enemySpawner.CancelPendingSpawns();
        gm.playerController.enabled = false;
        var weapon = gm.playerController.weaponController;
        weapon.enabled = false;
        gm.playerController.transform.rotation = Quaternion.identity;
        gm.playerController.transform.position = Vector3.zero;
        foreach (var enemy in Object.FindObjectsByType<EnemyBase>(FindObjectsSortMode.None)) Object.Destroy(enemy.gameObject);
        yield return null;
        foreach (string mode in new[] { "weapon", "centered_x", "lowered_y", "converged" })
        {
            var enemy = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemies/Enemy_Melee.prefab"), Vector3.forward * 8, Quaternion.identity);
            enemy.GetComponent<EnemyBase>().enabled = false;
            var target = enemy.GetComponent<Damageable>();
            Physics.SyncTransforms();
            var bounds = enemy.GetComponent<Collider>().bounds;
            float hp = target.CurrentHealth;
            Vector3 origin = weapon.muzzle.position;
            Record("SHOT_GEOMETRY mode=" + mode + " muzzle=" + origin + " enemy_bounds=" + bounds + " hp=" + hp);
            if (mode == "weapon")
            {
                gm.playerController.AimAt(bounds.center);
                weapon.TryFireBeam();
            }
            else
            {
                if (mode == "centered_x") origin.x = 0;
                if (mode == "lowered_y") origin.y = bounds.center.y;
                var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                go.transform.position = origin;
                go.transform.localScale = new Vector3(0.16f, 0.16f, 0.45f);
                go.AddComponent<Projectile>().Init(0, null, mode == "converged" ? bounds.center - origin : Vector3.forward, 18, 30, 2.1f, 0, 0);
            }
            float until = Time.time + 0.7f;
            while (Time.time < until) yield return null;
            Record("SHOT_RESULT mode=" + mode + " before=" + hp + " after=" + target.CurrentHealth);
            if (mode == "weapon" && Mathf.Abs(hp - target.CurrentHealth - 18f) > 0.01f)
                throw new Exception("Weapon did not deal one 18 HP hit.");
            Object.Destroy(enemy);
            foreach (var p in Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None)) Object.Destroy(p.gameObject);
            yield return null;
        }

        foreach (float distance in new[] { 2f, 8f, 18f })
        foreach (float angle in new[] { 0f, 90f, 180f, 270f })
        {
            Vector3 point = Quaternion.Euler(0, angle, 0) * Vector3.forward * distance;
            var enemy = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemies/Enemy_Melee.prefab"), point, Quaternion.identity);
            enemy.GetComponent<EnemyBase>().enabled = false;
            var target = enemy.GetComponent<Damageable>();
            Physics.SyncTransforms();
            gm.playerController.AimAt(enemy.GetComponent<Collider>().bounds.center);
            float before = target.CurrentHealth;
            weapon.TryFireBeam();
            float until = Time.time + 0.9f;
            while (Time.time < until) yield return null;
            if (Mathf.Abs(before - target.CurrentHealth - 18f) > 0.01f)
                throw new Exception("Aim regression distance=" + distance + " angle=" + angle);
            Record("AIM_PASS distance=" + distance + " angle=" + angle + " damage=" + (before - target.CurrentHealth));
            Object.Destroy(enemy);
            yield return null;
        }
    }

    private static void Inventory()
    {
        var lines = new List<string>
        {
            "Unity=" + Application.unityVersion,
            "Target=" + EditorUserBuildSettings.activeBuildTarget,
            "Pipeline=" + (UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline == null ? "Built-in" : "SRP"),
            "Backend=" + PlayerSettings.GetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Standalone),
            "Graphics=" + string.Join(",", PlayerSettings.GetGraphicsAPIs(BuildTarget.StandaloneWindows64)),
            "BuildScenes=" + string.Join(",", EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path))
        };
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/Demo_Main.unity");
        foreach (var go in scene.GetRootGameObjects()) Inspect(go, "Scene", lines);
        foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Inspect(AssetDatabase.LoadAssetAtPath<GameObject>(path), path, lines);
        }
        var dependencies = new HashSet<string>(AssetDatabase.GetDependencies(scene.path, true));
        foreach (string path in AssetDatabase.GetAllAssetPaths().Where(p => p.StartsWith("Assets/") && !AssetDatabase.IsValidFolder(p)))
        {
            if (!dependencies.Contains(path)) lines.Add("NOT_SCENE_DEPENDENCY " + path);
        }
        foreach (var group in Directory.GetFiles("Assets", "*.meta", SearchOption.AllDirectories)
            .Select(p => new { Path = p, Guid = File.ReadLines(p).FirstOrDefault(l => l.StartsWith("guid: ")) })
            .Where(x => x.Guid != null).GroupBy(x => x.Guid).Where(g => g.Count() > 1))
            lines.Add("DUPLICATE_GUID " + string.Join(",", group.Select(x => x.Path)));
        File.WriteAllLines(Path.Combine(Output, "inventory.txt"), lines);
        Debug.Log("AUDIT_INVENTORY: " + lines.Count + " entries; broken=" + lines.Count(l => l.StartsWith("MISSING")));
    }

    private static void Inspect(GameObject root, string owner, List<string> lines)
    {
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
        {
            int missing = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject);
            if (missing > 0) lines.Add("MISSING_SCRIPT " + owner + "/" + t.name + " count=" + missing);
            foreach (var component in t.GetComponents<Component>())
            {
                if (component == null) continue;
                if (owner == "Scene" && component is MonoBehaviour) lines.Add("SCENE_COMPONENT " + t.name + " " + component.GetType().Name);
                var serialized = new SerializedObject(component);
                var property = serialized.GetIterator();
                while (property.NextVisible(true))
                {
                    if (property.propertyType == SerializedPropertyType.ObjectReference && property.objectReferenceValue == null && property.objectReferenceInstanceIDValue != 0)
                        lines.Add("MISSING_REFERENCE " + owner + "/" + t.name + "/" + property.propertyPath);
                }
            }
        }
    }

    private static void Tick()
    {
        if (!EditorApplication.isPlaying || Time.frameCount == lastFrame) return;
        lastFrame = Time.frameCount;
        try
        {
            if (EditorApplication.timeSinceStartup > deadline) throw new Exception("Audit runtime deadline exceeded.");
            if (!routine.MoveNext()) Finish(0);
        }
        catch (Exception error)
        {
            Record("AUDIT_ABORT " + error);
            Finish(1);
        }
    }

    private static IEnumerator RunScenarios()
    {
        yield return null;
        var gm = GameManager.Instance;
        Record("START hangar=" + gm.hangarUI.IsVisible + " hud=" + gm.combatHUD.IsVisible + " equipment=" + gm.equipmentManager.GetEquippedItems().Count);
        Capture("01_hangar");
        Click("DeployButton");
        gm.playerController.enabled = false;
        gm.playerStats.GetComponent<Damageable>().OnDamaged += (d, info) => Record("PLAYER_HIT hp=" + d.CurrentHealth + " phase=" + gm.Phase + " damage=" + info.Amount);
        int startFrame = Time.frameCount;
        float started = Time.time;
        int rewardChoices = 0;
        bool bossSeen = false;
        bool phaseTwoSeen = false;
        var kinds = new HashSet<EnemyKind>();
        var progress = new HashSet<string>();
        var frameTimes = new List<float>();
        int shots = 0;
        gm.playerController.weaponController.BeamFired += () => shots++;
        while (Time.time - started < 150f && gm.Phase != GamePhase.Result)
        {
            if (progress.Add(gm.ProgressText)) Record("FLOW " + gm.ProgressText + " hp=" + gm.playerStats.CurrentHp + " kills=" + gm.Kills);
            foreach (var e in Object.FindObjectsByType<EnemyBase>(FindObjectsSortMode.None)) kinds.Add(e.kind);
            var boss = Object.FindFirstObjectByType<BossController>();
            if (boss != null)
            {
                if (!bossSeen) { Capture("04_boss"); bossSeen = true; }
                phaseTwoSeen |= boss.IsPhaseTwo;
            }
            if (gm.Phase == GamePhase.Reward)
            {
                yield return null;
                Capture("03_reward");
                Record("REWARD cards=" + Object.FindObjectsByType<Button>(FindObjectsSortMode.None).Count(b => b.name == "ChooseButton"));
                Click("ChooseButton");
                rewardChoices++;
                Record("UPGRADE " + gm.upgradeSystem.GetSummary());
            }
            if (gm.IsCombatActive)
            {
                var target = Object.FindObjectsByType<Damageable>(FindObjectsSortMode.None)
                    .Where(d => d.team == 1 && !d.IsDead)
                    .OrderBy(d => (d.transform.position - gm.playerController.transform.position).sqrMagnitude).FirstOrDefault();
                if (target != null)
                {
                    var collider = target.GetComponent<Collider>();
                    gm.playerController.AimAt(collider != null ? collider.bounds.center : target.transform.position + Vector3.up * 0.75f);
                    gm.playerController.weaponController.TryFireBeam();
                }
            }
            if (Time.frameCount == startFrame + 180) Capture("02_combat");
            if (Time.time - started > 2) frameTimes.Add(Time.unscaledDeltaTime);
            yield return null;
        }
        yield return null;
        Capture("05_result");
        Record("LIVE_FIRE_RESULT phase=" + gm.Phase + " hp=" + gm.playerStats.CurrentHp + " kills=" + gm.Kills + " shots=" + shots + " choices=" + rewardChoices + " kinds=" + string.Join(",", kinds) + " boss=" + bossSeen + " phase2=" + phaseTwoSeen + " seconds=" + (Time.time - started));
        frameTimes.Sort();
        if (frameTimes.Count > 0) Record("FRAME_OBSERVATION samples=" + frameTimes.Count + " avg_ms=" + frameTimes.Average() * 1000 + " p95_ms=" + frameTimes[(int)(frameTimes.Count * 0.95f)] * 1000 + " note=editor+audit_logic+uncapped_not_benchmark");
        gm.RestartRun();
        yield return null;
        yield return null;
        gm = GameManager.Instance;
        Record("RESTART hangar=" + gm.hangarUI.IsVisible + " hp=" + gm.playerStats.CurrentHp + " upgrades=" + gm.upgradeSystem.GetSummary());

        Click("DeployButton");
        gm.playerController.enabled = false;
        var player = gm.playerStats.GetComponent<Damageable>();
        // An in-flight enemy shot crossing the player after the reward opens.
        var shot = new GameObject("AuditEnemyShot").AddComponent<Projectile>();
        shot.transform.position = player.transform.position + Vector3.up + Vector3.forward * 5;
        shot.Init(1, null, Vector3.back, 30, 12, 2, 0, 0);
        gm.EnterReward();
        float hpBefore = player.CurrentHealth;
        float until = Time.time + 1;
        while (Time.time < until) yield return null;
        Record("REWARD_PROTECTION hp_before=" + hpBefore + " hp_after=" + player.CurrentHealth + " phase=" + gm.Phase);
        Capture("06_reward_damage");
        gm.RestartRun();
        yield return null;
        yield return null;
        gm = GameManager.Instance;
        Click("DeployButton");
        player = gm.playerStats.GetComponent<Damageable>();
        player.TakeDamage(9999, new DamageInfo(null, player.transform.position, null, 9999));
        int before = Object.FindObjectsByType<EnemyBase>(FindObjectsSortMode.None).Length;
        until = Time.time + 1;
        while (Time.time < until) yield return null;
        Record("DEFEAT phase=" + gm.Phase + " hp=" + gm.playerStats.CurrentHp + " enemies_before=" + before + " enemies_after=" + Object.FindObjectsByType<EnemyBase>(FindObjectsSortMode.None).Length + " hud=" + gm.combatHUD.IsVisible + " result_buttons=" + Object.FindObjectsByType<Button>(FindObjectsSortMode.None).Length);
        Capture("07_defeat");
        gm.RestartRun();
        yield return null;
        yield return null;
        Record("DEFEAT_RESTART hangar=" + GameManager.Instance.hangarUI.IsVisible);
    }

    private static void Click(string name)
    {
        var button = Object.FindObjectsByType<Button>(FindObjectsSortMode.None).FirstOrDefault(b => b.name == name);
        if (button == null) throw new Exception("Missing visible button " + name);
        var pointer = new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left };
        ExecuteEvents.Execute(button.gameObject, pointer, ExecuteEvents.pointerClickHandler);
        Record("UI_CLICK " + name);
    }

    private static void Capture(string name)
    {
        if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null) return;
        var camera = Camera.main;
        var canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).Where(c => c.renderMode != RenderMode.WorldSpace).ToArray();
        var modes = canvases.Select(c => c.renderMode).ToArray();
        var texture = new RenderTexture(1920, 1080, 24);
        var previousTarget = camera.targetTexture;
        var previousActive = RenderTexture.active;
        camera.targetTexture = texture;
        for (int i = 0; i < canvases.Length; i++)
        {
            canvases[i].renderMode = RenderMode.ScreenSpaceCamera;
            canvases[i].worldCamera = camera;
            canvases[i].planeDistance = 1;
        }
        Canvas.ForceUpdateCanvases();
        camera.Render();
        RenderTexture.active = texture;
        var frame = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
        frame.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0);
        frame.Apply();
        File.WriteAllBytes(Path.Combine(Output, name + ".png"), frame.EncodeToPNG());
        Record("CAPTURE " + name + " colors=" + frame.GetPixels32().Where((p, i) => i % 997 == 0).Distinct().Count());
        for (int i = 0; i < canvases.Length; i++) canvases[i].renderMode = modes[i];
        camera.targetTexture = previousTarget;
        RenderTexture.active = previousActive;
        texture.Release();
        Object.DestroyImmediate(texture);
        Object.DestroyImmediate(frame);
    }

    private static void CaptureLog(string condition, string stack, LogType type)
    {
        if (type == LogType.Warning || type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
            evidence.Add("CONSOLE " + type + " " + condition + " " + stack);
    }

    private static void Record(string value)
    {
        evidence.Add(value);
        Debug.Log("AUDIT " + value);
    }

    private static void Finish(int code)
    {
        File.WriteAllLines(Path.Combine(Output, "runtime.txt"), evidence);
        SessionState.SetBool(Active, false);
        EditorApplication.update -= Tick;
        Application.logMessageReceived -= CaptureLog;
        Debug.Log("AUDIT_FINISHED code=" + code);
        EditorApplication.Exit(code);
    }
}
