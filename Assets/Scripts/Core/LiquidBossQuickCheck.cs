using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

// Opt-in Player integration check. Normal launch never adds this component.
[DefaultExecutionOrder(250)]
public sealed class LiquidBossQuickCheck : MonoBehaviour
{
    IEnumerator scenario;
    string output;
    bool done;
    float deadline;
    int errors;
    readonly List<string> report = new List<string>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Bootstrap()
    {
        if (Array.IndexOf(Environment.GetCommandLineArgs(), "-liquidBossCheck") >= 0)
            DontDestroyOnLoad(new GameObject("LiquidBossQuickCheck").AddComponent<LiquidBossQuickCheck>());
    }
    void Start()
    {
        var path = Environment.GetEnvironmentVariable("MECH_EQUIPMENT_PROFILE");
        if (string.IsNullOrEmpty(path)) { Application.Quit(2); return; }
        output = Path.GetDirectoryName(path);
        Directory.CreateDirectory(output);
        Application.logMessageReceived += Log;
        Application.runInBackground = true;
        PlayerInputRouter.AllowUnfocusedReplay = true;
        Time.captureDeltaTime = 1f / 60;
        Application.targetFrameRate = 240;
        deadline = Time.realtimeSinceStartup + 150;
        scenario = Scenario();
    }
    void LateUpdate()
    {
        if (done || scenario == null) return;
        try
        {
            if (Time.realtimeSinceStartup > deadline) throw new Exception("Liquid boss check timed out.");
            if (!scenario.MoveNext()) Finish(null);
        }
        catch (Exception e) { Finish(e.ToString()); }
    }
    void Log(string message, string trace, LogType kind)
    {
        if (kind == LogType.Error || kind == LogType.Exception || kind == LogType.Assert)
        { errors++; report.Add(message + "\n" + trace); }
    }
    void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
        report.Add("PASS " + message);
    }
    Button Button(string name) => FindObjectsByType<Button>(FindObjectsSortMode.None).Single(b => b.name == name);
    IEnumerator Scenario()
    {
        for (int i = 0; i < 40; i++) yield return null;
        var gm = GameManager.Instance;
        Check(gm != null && gm.Phase == GamePhase.Hangar, "normal launch opens the homepage");
        var entry = Button("LiquidBossChallengeButton");
        Check(entry.isActiveAndEnabled && entry.interactable, "homepage has an enabled liquid-boss challenge button");
        Capture("01_homepage.png");
        entry.onClick.Invoke();
        Check(gm.Phase == GamePhase.Combat && gm.stageManager.CurrentEncounter == 6 && gm.CompletedEncounters == 6,
            "homepage button goes directly to the valid final encounter");
        Check(gm.upgradeSystem.Count == 6, "direct challenge receives six temporary random upgrades");
        BossController boss = null;
        for (int i = 0; i < 600 && boss == null; i++) { boss = FindFirstObjectByType<BossController>(); yield return null; }
        Check(boss != null && !boss.mechDuel, "liquid boss actually spawns, independent of the veteran");
        var pose = boss.GetComponent<E01ElitePoseDriver>();
        Check(pose != null && pose.BoundBones >= 86 && pose.surfaces.Length == 2,
            "liquid boss imported skeleton and both LOD meshes are bound in the Player");
        Check(pose.surfaces.All(s => s.sharedMaterials.All(m => m != null && m.shader.name == "MECH ROUGE/E01 Living Metal")),
            "actual Player renderers use the living-metal materials");
        var player = gm.playerController;
        var hp = boss.GetComponent<Damageable>();
        var playerHp = player.GetComponent<Damageable>();
        Check(player.GetComponentInChildren<ValkyrMotionDriver>() != null && Mathf.Approximately(player.Melee.MotionProfile.Get(0).advance, .68f),
            "V7 protagonist, large combo profile and sword remain active");
        Check(!playerHp.IsInvulnerable, "normal direct challenge does not grant invulnerability");
        // Protection and health overrides below belong only to this opt-in fixture.
        playerHp.SetInvulnerable(100);
        player.enabled = false;
        player.InputRouter.readKeyboard = false;
        for (int i = 0; i < 120; i++) yield return null;
        Capture("02_boss_arena.png");
        Capture("03_boss_close.png", boss.transform);
        Check(boss.ActionRunning && pose.TendrilMotionDegrees > 2, "boss attacks and its liquid-metal appendages animate");
        float windup = boss.AttackWindup;
        var tendril = pose.rigRoot.GetComponentsInChildren<Transform>().Single(t => t.name == "Tendril.02.03");
        var rotation = tendril.localRotation;
        gm.SetPaused(true);
        for (int i = 0; i < 12; i++) yield return null;
        Check(Mathf.Approximately(windup, boss.AttackWindup) && Quaternion.Angle(rotation, tendril.localRotation) < .01f,
            "pause freezes the boss attack and deformation");
        gm.SetPaused(false);
        for (int i = 0; i < 600 && !boss.CoreExposed; i++) yield return null;
        Check(boss.CoreExposed, "boss exposes its core after attacking");
        for (int i = 0; i < 28; i++) yield return null;
        Check(pose.CoreOpening > .9f, "core opening is driven by the actual damage window");
        player.RestoreAt(boss.transform.position + boss.transform.forward * 3f + Vector3.up * .1f);
        player.AimAt(boss.transform.position);
        float before = hp.CurrentHealth;
        bool bladeHit = false;
        player.Melee.StrikeHit += (target, amount) => { if (target == hp) bladeHit = true; };
        for (int i = 0; i < 66; i++)
        {
            player.Simulate(new PlayerCommand { Melee = i == 0, HasAim = true, AimPoint = hp.AimCenter }, Time.deltaTime);
            yield return null;
            if (i == 19) Capture("04_sword_contact.png");
        }
        Check(bladeHit && hp.CurrentHealth < before, "V7 sword sweep damages the liquid boss collider through the real attack path");
        hp.SetCurrentHealth(hp.maxHealth * .4f);
        for (int i = 0; i < 90; i++) yield return null;
        Check(boss.IsPhaseTwo && pose.PhaseBlend > .9f, "half-health state drives phase two and its visual transition");
        Capture("05_phase_two.png", boss.transform);
        hp.Kill(new DamageInfo(player.gameObject, player.transform.position, playerHp, hp.maxHealth));
        for (int i = 0; i < 120; i++) yield return null;
        Check(gm.Phase == GamePhase.Result && gm.LastResultVictory, "boss death animation leads to the normal victory result");
        Check(gm.upgradeSystem.Count == 0, "six challenge buffs are cleared after the fight");
        Capture("06_result.png");
        Button("ReturnHangarButton").onClick.Invoke();
        for (int i = 0; i < 45; i++) yield return null;
        gm = GameManager.Instance;
        Check(gm.Phase == GamePhase.Hangar && gm.CompletedEncounters == 0, "result button returns to a fresh homepage");
        Button("BossTrialButton").onClick.Invoke();
        boss = null;
        for (int i = 0; i < 300 && boss == null; i++) { boss = FindFirstObjectByType<BossController>(); yield return null; }
        Check(boss != null && boss.mechDuel && boss.GetComponent<E01ElitePoseDriver>() == null,
            "retained veteran trial still selects the old mech boss");
        gm.RestartRun();
        for (int i = 0; i < 45; i++) yield return null;
        gm = GameManager.Instance;
        gm.BeginRun();
        Check(gm.stageManager.CurrentEncounter == 0 && gm.CompletedEncounters == 0 && gm.upgradeSystem.Count == 0,
            "ordinary sortie still starts at encounter one without challenge buffs");
        Check(gm.stageManager.enemySpawner.DefaultBossPrefab.GetComponent<E01ElitePoseDriver>() != null,
            "ordinary sortie final boss resolves to the liquid boss after returning from veteran trial");
        report.Add("Test-only protection, half-health and death overrides were used to check lifecycle; this is not a natural boss clear.");
        report.Add("Version=" + Application.version + " GPU=" + SystemInfo.graphicsDeviceName);
    }
    void Capture(string name, Transform boss = null)
    {
        var camera = Camera.main;
        var position = camera.transform.position;
        var rotation = camera.transform.rotation;
        bool orthographic = camera.orthographic;
        float fov = camera.fieldOfView;
        if (boss != null)
        {
            camera.orthographic = false;
            camera.fieldOfView = 33;
            camera.transform.position = boss.position + boss.forward * 10 + boss.right * 6 + Vector3.up * 6;
            camera.transform.LookAt(boss.position + Vector3.up * 2.9f);
        }
        var overlays = FindObjectsByType<Canvas>(FindObjectsSortMode.None).Where(c => c.isRootCanvas && c.renderMode == RenderMode.ScreenSpaceOverlay).ToArray();
        var cameras = overlays.Select(c => c.worldCamera).ToArray();
        var planes = overlays.Select(c => c.planeDistance).ToArray();
        var rt = new RenderTexture(1600, 900, 24) { antiAliasing = 4 }; rt.Create();
        var active = RenderTexture.active; var target = camera.targetTexture;
        camera.targetTexture = rt;
        foreach (var canvas in overlays)
        {
            if (boss != null) canvas.enabled = false;
            else { canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 1; }
        }
        Canvas.ForceUpdateCanvases(); camera.Render(); RenderTexture.active = rt;
        var texture = new Texture2D(1600, 900, TextureFormat.RGB24, false);
        texture.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0); texture.Apply();
        File.WriteAllBytes(Path.Combine(output, name), texture.EncodeToPNG());
        for (int i = 0; i < overlays.Length; i++)
        {
            if (boss != null) overlays[i].enabled = true;
            else { overlays[i].renderMode = RenderMode.ScreenSpaceOverlay; overlays[i].worldCamera = cameras[i]; overlays[i].planeDistance = planes[i]; }
        }
        camera.targetTexture = target; RenderTexture.active = active;
        camera.transform.SetPositionAndRotation(position, rotation); camera.orthographic = orthographic; camera.fieldOfView = fov;
        rt.Release(); Destroy(rt); Destroy(texture); Canvas.ForceUpdateCanvases();
    }
    void Finish(string failure)
    {
        if (done) return;
        done = true;
        if (failure != null) report.Add("FAIL " + failure);
        int code = failure == null && errors == 0 ? 0 : 1;
        report.Add("LIQUID_BOSS_CHECK code=" + code + " errors=" + errors);
        File.WriteAllLines(Path.Combine(output, "quick-check.txt"), report);
        Application.logMessageReceived -= Log;
        Debug.Log(report[report.Count - 1]);
        Time.captureDeltaTime = 0;
        Application.Quit(code);
    }
}
