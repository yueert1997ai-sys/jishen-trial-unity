using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

// A short opt-in replay of the actual Player. Uses a separate equipment save.
public sealed class ValkyrCombatQuickCheck : MonoBehaviour
{
    private IEnumerator scenario;
    private readonly List<string> report = new List<string>();
    private string output;
    private float deadline;
    private int errors;
    private bool finished;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (Array.IndexOf(Environment.GetCommandLineArgs(), "-valkyrQuickCheck") >= 0)
            DontDestroyOnLoad(new GameObject("ValkyrCombatQuickCheck").AddComponent<ValkyrCombatQuickCheck>());
    }
    private void Start()
    {
        string profile = Environment.GetEnvironmentVariable("MECH_EQUIPMENT_PROFILE");
        if (string.IsNullOrEmpty(profile)) { Finish("A separate test profile is required"); return; }
        output = Path.GetDirectoryName(profile);
        Directory.CreateDirectory(output);
        PlayerInputRouter.AllowUnfocusedReplay = true;
        Application.runInBackground = true;
        Time.captureDeltaTime = 1f / 60;
        Application.targetFrameRate = 60;
        Application.logMessageReceived += OnLog;
        deadline = Time.realtimeSinceStartup + 90;
        scenario = Scenario();
    }
    private void Update()
    {
        if (finished || scenario == null) return;
        try
        {
            if (Time.realtimeSinceStartup > deadline) throw new Exception("Preview exceeded 90 seconds");
            if (!scenario.MoveNext()) Finish(null);
        }
        catch (Exception ex) { Finish(ex.ToString()); }
    }
    private IEnumerator Scenario()
    {
        for (int i = 0; i < 35; i++) yield return null;
        GamePreferences.SetLanguage(true); GamePreferences.SetMaster(0);
        var gm = GameManager.Instance;
        var player = gm.playerController;
        player.InputRouter.readKeyboard = false;
        var blade = player.GetComponentInChildren<RaikenBladePresentation>();
        Check(blade != null && blade.enabled && blade.rig.enabled, "latest Valkyr and Raiken are the live playable hero");
        var beamRenderers = blade.beam.GetComponentsInChildren<MeshRenderer>(true);
        var physicalRenderers = blade.bladeRoot.GetComponentsInChildren<MeshRenderer>(true)
            .Where(r => !r.transform.IsChildOf(blade.beam.transform)).ToArray();
        Check(beamRenderers.Length > 0 && physicalRenderers.Length > 0,
            "the imported energy edge and physical blade have independent renderers");
        Check(Vector3.Distance(blade.grip.position, blade.tip.position) > 3.2f, "authored long blade scale retained");
        Capture("01_valkyr_hangar");
        blade.SetBeamEnabled(false);
        yield return null;
        Check(!blade.BeamEnabled && beamRenderers.All(r => !r.gameObject.activeInHierarchy)
            && physicalRenderers.All(r => r.enabled && r.gameObject.activeInHierarchy),
            "beam can switch off while every physical blade renderer stays visible");
        Capture("02_physical_blade_beam_off");
        blade.SetBeamEnabled(true);
        Click("BossTrialButton");
        BossController boss = null;
        for (int i = 0; i < 120 && boss == null; i++) { yield return null; boss = FindFirstObjectByType<BossController>(); }
        Check(gm.IsCombatActive && boss != null && boss.mechDuel, "hangar trial button starts the actual elite boss encounter");
        var rival = boss.GetComponent<MechRivalPresentation>();
        Check(rival != null && rival.visual != null && !rival.visual.enabled && boss.GetComponentInChildren<RaikenBladePresentation>() == null,
            "yesterday's mech has its own enemy animation adapter and remains distinct from the hero");
        boss.target = null;
        player.RestoreAt(new Vector3(0, .1f, -12));
        Check(boss.GetComponent<NavMeshAgent>().Warp(new Vector3(0, 0, -8.1f)), "elite can stand and navigate on the arena navmesh");
        boss.transform.rotation = Quaternion.Euler(0, 180, 0);
        var hp = boss.GetComponent<Damageable>();
        player.AimAt(hp.AimCenter);
        for (int i = 0; i < 25; i++) yield return null;
        Capture("03_elite_duel");
        float before = hp.CurrentHealth;
        int hits = 0;
        hp.OnDamaged += (victim, info) => hits++;
        Vector3 startTip = blade.tip.position;
        float travel = 0;
        int samples = 0;
        player.InputRouter.QueueMelee();
        for (int i = 0; i < 45; i++)
        {
            yield return null;
            samples = Mathf.Max(samples, blade.TrailSamples);
            travel = Mathf.Max(travel, Vector3.Distance(startTip, blade.tip.position));
            if (i == 6) Check(hits == 0, "windup cannot damage a target before the blade sweep reaches it");
            if (i == 14) Capture("04_raiken_sweep_early");
            if (i == 20) { Capture("05_raiken_sweep"); CaptureClose(player.transform.position, "06_raiken_sweep_close"); }
        }
        Check(hits == 1 && Mathf.Abs(before - hp.CurrentHealth - 78 * hp.IncomingDamageScale) < .1f,
            "one Q strike damages the elite once at 3.9 m using the real armor multiplier");
        Check(samples >= 3 && travel > 2 && blade.ImpactCount == 1, "actual moving blade generates its swept ribbon and impact feedback");
        report.Add("Measured maximum ribbon samples=" + samples + " tip displacement=" + travel.ToString("F2") + " damage=" + (before - hp.CurrentHealth).ToString("F2"));
        player.Melee.ResetCooldown();
        player.InputRouter.QueueMelee();
        for (int i = 0; i < 3; i++) yield return null;
        player.InputRouter.SetTouchMove(Vector2.left);
        player.InputRouter.QueueDash();
        for (int i = 0; i < 16; i++) yield return null;
        player.InputRouter.Clear();
        Check(hits == 1 && !player.Melee.IsAttacking && blade.TrailSamples == 0, "dash cancels windup without a phantom hit or stuck ribbon");
        player.RestoreAt(new Vector3(0, .1f, -12));
        boss.GetComponent<NavMeshAgent>().Warp(new Vector3(0, 0, -2));
        boss.target = player.transform;
        player.AimAt(hp.AimCenter);
        for (int i = 0; i < 20; i++) yield return null;
        Check(boss.ActionRunning, "elite autonomously acquires player and begins its attack pattern");
        Capture("07_elite_volley_warning");
        bool fired = false;
        for (int i = 0; i < 55; i++)
        {
            yield return null;
            if (FindObjectsByType<Projectile>(FindObjectsSortMode.None).Any(p => p.team == 1))
            { fired = true; break; }
        }
        Check(fired, "old mech fires real enemy projectiles from its shoulder cannon");
        for (int i = 0; i < 12; i++) yield return null;
        Capture("08_elite_live_volley");
        for (int i = 0; i < 220 && boss.ActionRunning; i++) yield return null;
        Check(boss.StartPattern(BossPattern.Charge), "elite starts its telegraphed charge after recovering from fire");
        Vector3 chargeStart = boss.transform.position;
        float chargeDistance = 0;
        for (int i = 0; i < 135; i++)
        {
            yield return null;
            chargeDistance = Mathf.Max(chargeDistance, Vector3.Distance(chargeStart, boss.transform.position));
            if (i == 76) Capture("08b_elite_charge");
        }
        Check(chargeDistance > 3, "elite charge moves its actual animated body across the arena");
        hp.SetCurrentHealth(hp.maxHealth * .49f);
        yield return null; yield return null;
        Check(boss.IsPhaseTwo, "elite enters its second phase below half health");
        hp.TakeDamage(99999, new DamageInfo(player.gameObject, player.transform.position, player.GetComponent<Damageable>(), 99999));
        for (int i = 0; i < 8; i++) yield return null;
        Check(gm.Phase == GamePhase.Result && gm.LastResultVictory, "defeating the elite completes the encounter and shows victory");
        Capture("09_victory");
        gm.RestartRun();
        for (int i = 0; i < 30; i++) yield return null;
        gm = GameManager.Instance;
        Check(gm.Phase == GamePhase.Hangar && gm.playerController.GetComponentInChildren<RaikenBladePresentation>() != null
            && gm.equipmentLoop.Warehouse.Profile.owned.Count == 2 && gm.upgradeSystem.Count == 0, "restart retains new hero and permanent gear without carrying run buffs");
        gm.playerController.InputRouter.readKeyboard = false;
        gm.BeginRun();
        for (int i = 0; i < 80; i++) yield return null;
        Check(gm.IsCombatActive && gm.stageManager.CurrentEncounter == 0 && FindFirstObjectByType<EnemyBase>() != null,
            "normal deployment still starts the equipment run with the new hero");
        Capture("10_normal_deployment");
        report.Add("GPU " + SystemInfo.graphicsDeviceName + " / " + SystemInfo.graphicsDeviceType);
    }
    private void Capture(string name) => EquipmentLoopQuickCheck.CaptureTo(output, name);
    private void CaptureClose(Vector3 playerPosition, string name)
    {
        var camera = Camera.main;
        Vector3 before = camera.transform.position;
        Quaternion rotation = camera.transform.rotation;
        float size = camera.orthographicSize;
        camera.orthographicSize = 4.8f;
        camera.transform.rotation = Quaternion.Euler(46, -24, 0);
        camera.transform.position = playerPosition + new Vector3(0, .9f, 1.4f) - camera.transform.forward * 20;
        Capture(name);
        camera.transform.SetPositionAndRotation(before, rotation); camera.orthographicSize = size;
    }
    private void Check(bool okay, string name)
    {
        report.Add((okay ? "PASS " : "FAIL ") + name);
        if (!okay) throw new Exception(name);
    }
    private static void Click(string name)
    {
        var button = FindObjectsByType<Button>(FindObjectsSortMode.None).FirstOrDefault(b => b.name == name && b.isActiveAndEnabled && b.interactable);
        if (button == null) throw new Exception("Missing button " + name);
        button.onClick.Invoke();
    }
    private void OnLog(string message, string stack, LogType type)
    {
        if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert) return;
        errors++; report.Add("RUNTIME ERROR " + message + "\n" + stack);
    }
    private void Finish(string failure)
    {
        if (finished) return;
        finished = true;
        if (failure != null) report.Add(failure);
        int code = failure == null && errors == 0 ? 0 : 1;
        report.Add("VALKYR_COMBAT_CHECK code=" + code + " errors=" + errors);
        if (!string.IsNullOrEmpty(output)) File.WriteAllLines(Path.Combine(output, "quick-check.txt"), report);
        Debug.Log(report.Last()); Application.logMessageReceived -= OnLog;
        Application.Quit(code);
    }
}
