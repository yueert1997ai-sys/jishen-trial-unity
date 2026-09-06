using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

// Opt-in short integration check, never active in a normal player launch.
public sealed class EquipmentLoopQuickCheck : MonoBehaviour
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
        if (Array.IndexOf(Environment.GetCommandLineArgs(), "-equipmentQuickCheck") >= 0) Create();
    }
    public static void Create()
    {
        DontDestroyOnLoad(new GameObject("EquipmentLoopQuickCheck").AddComponent<EquipmentLoopQuickCheck>());
    }
    private void Start()
    {
        string profile = Environment.GetEnvironmentVariable("MECH_EQUIPMENT_PROFILE");
        if (string.IsNullOrEmpty(profile)) { Debug.LogError("Equipment check requires its own test profile."); Finish("No test profile"); return; }
        output = Path.GetDirectoryName(profile);
        Directory.CreateDirectory(output);
        PlayerInputRouter.AllowUnfocusedReplay = true;
        Application.runInBackground = true;
        Application.logMessageReceived += OnLog;
        deadline = Time.realtimeSinceStartup + 90;
        scenario = Scenarios();
    }
    private void Update()
    {
        if (finished || scenario == null) return;
        try
        {
            if (Time.realtimeSinceStartup > deadline) throw new Exception("Quick check exceeded 90 seconds");
            if (!scenario.MoveNext()) Finish(null);
        }
        catch (Exception ex) { Finish(ex.ToString()); }
    }
    private IEnumerator Scenarios()
    {
        for (int i = 0; i < 35; i++) yield return null;
        GamePreferences.SetLanguage(true);
        GamePreferences.SetMaster(0);
        var gm = GameManager.Instance;
        var loop = gm.equipmentLoop;
        var player = gm.playerController;
        player.InputRouter.readKeyboard = false;
        Check(loop.Warehouse.Profile.owned.Count == 2, "fresh collection has only starter cannon and pack");
        Check(!loop.TryEquip("lance"), "locked equipment cannot be equipped");
        Click("OpenWarehouseButton");
        for (int i = 0; i < 4; i++) yield return null;
        Check(loop.UI.IsVisible && !gm.CanPlayerControl, "warehouse blocks movement and deployment input");
        Capture("01_collection_locked");
        Click("WarehouseContinueButton");
        Click("DeployButton");
        yield return null;
        gm.stageManager.StopStage();
        player.RestoreAt(new Vector3(0, .1f, -4));
        Check(!loop.TryEquip("standard"), "equipment cannot change during combat");
        var spawned = new List<EnemyBase>();
        foreach (var kind in new[] { EnemyKind.Ranged, EnemyKind.Drone, EnemyKind.Elite, EnemyKind.Elite })
        {
            var enemy = gm.stageManager.enemySpawner.SpawnEnemy(kind, player.transform.position + new Vector3(-3 + spawned.Count * 2, 0, 4));
            enemy.target = null;
            spawned.Add(enemy);
        }
        yield return null;
        foreach (var enemy in spawned) enemy.GetComponent<Damageable>().TakeDamage(99999, new DamageInfo(player.gameObject, player.transform.position, player.GetComponent<Damageable>(), 99999));
        yield return null;
        Check(loop.Pickups.Count == 4, "defeated equipped enemies release four distinct physical modules");
        Capture("02_recoverable_parts");
        Click("AbsorbButton");
        Check(loop.Warehouse.Profile.owned.Count == 6 && loop.Pickups.Count == 0, "absorb button permanently collects nearby modules");
        Check(loop.Weapon.id == "pulse" && loop.Backpack.id == "standard", "acquiring equipment never replaces the current loadout");
        for (int i = 0; i < 8; i++) yield return null;
        Capture("03_absorption");
        var reloaded = new SalvageWarehouse(loop.Warehouse.Path);
        Check(reloaded.Profile.owned.Count == 6, "collection survives loading a new warehouse instance from disk");
        Check(reloaded.Acquire("scatter") && reloaded.Profile.owned.Count == 6 && !reloaded.Acquire("unknown"), "duplicates do not upgrade or duplicate slots; unknown IDs rejected");
        gm.OnEncounterCleared(0);
        for (int i = 0; i < 3; i++) yield return null;
        Check(gm.Phase == GamePhase.Reward, "room clear reaches random buff choices");
        Capture("04_random_buff");
        Click("ChooseButton");
        yield return null;
        Check(gm.Phase == GamePhase.Loadout && gm.upgradeSystem.Count == 1 && loop.UI.IsVisible, "choosing a buff opens between-room equipment selection");
        gm.upgradeSystem.ApplyOption(new RunUpgradeOption { kind = RunUpgradeKind.ArmorPlating });
        gm.upgradeSystem.ApplyOption(new RunUpgradeOption { kind = RunUpgradeKind.DashCapacitor });
        float hp = player.stats.MaxHp;
        float dash = player.stats.DashDistance;
        Click("Equip_scatter");
        Click("Equip_vector");
        yield return null;
        Check(Mathf.Approximately(hp, player.stats.MaxHp) && Mathf.Approximately(dash + 1.6f, player.stats.DashDistance), "gear swap preserves armor and dash buffs");
        loop.TryEquip("standard");
        loop.TryEquip("vector");
        loop.ApplyLoadout();
        Check(Mathf.Approximately(dash + 1.6f, player.stats.DashDistance), "repeated loadout application does not stack backpack bonuses");
        Capture("05_collection_equipped");
        Click("WarehouseContinueButton");
        yield return null;
        gm.stageManager.StopStage();
        player.AimAt(player.transform.position + Vector3.forward * 20);
        player.weaponController.ResetCooldowns();
        player.weaponController.TryFireBeam();
        var beams = FindObjectsByType<Projectile>(FindObjectsSortMode.None).Where(p => p.team == 0 && !(p is MissileProjectile)).ToArray();
        Check(beams.Length == 5 * (1 + gm.upgradeSystem.BonusBeamProjectiles), "equipped scatter cannon fires its real multi-projectile fan");
        for (int i = 0; i < 7; i++) yield return null;
        Capture("06_scatter_in_combat");
        var dummy = new GameObject("SkillTarget", typeof(BoxCollider), typeof(Damageable));
        dummy.transform.position = player.transform.position + Vector3.forward * 8;
        dummy.GetComponent<Damageable>().SetMaxHealth(9999, true);
        Check(player.weaponController.TryFireSkill(dummy.GetComponent<Damageable>()), "backpack skill remains connected to combat");
        Check(FindObjectsByType<MissileProjectile>(FindObjectsSortMode.None).Count(p => p.team == 0) == 4, "vector pack keeps standard four-missile skill");
        gm.EnterResult(false);
        yield return null;
        Check(gm.upgradeSystem.Count == 0 && Mathf.Approximately(player.stats.MaxHp, player.stats.baseMaxHp), "result clears both buff ranks and armor effects");
        Check(Mathf.Approximately(player.stats.DashDistance, player.stats.baseDashDistance + 1.6f) && !gm.CanContinue, "result removes dash buffs and exposes no old buff checkpoint retry");
        Check(loop.Warehouse.Profile.owned.Count == 6, "defeat retains acquired equipment");
        string json = File.ReadAllText(loop.Warehouse.Path);
        Check(!json.Contains("buff") && !json.Contains("upgrades") && !json.Contains("seed"), "permanent save has no run buff or random seed fields");
        Capture("07_result_retains_equipment");
        gm.RestartRun();
        for (int i = 0; i < 25; i++) yield return null;
        gm = GameManager.Instance; loop = gm.equipmentLoop; player = gm.playerController;
        player.InputRouter.readKeyboard = false;
        Check(gm.Phase == GamePhase.Hangar && loop.Warehouse.Profile.owned.Count == 6 && loop.Weapon.id == "scatter" && loop.Backpack.id == "vector", "scene reload restores permanent collection and chosen loadout");
        var choices = new HashSet<string>();
        for (int i = 0; i < 8; i++)
        {
            gm.upgradeSystem.ResetUpgrades();
            choices.Add(string.Join(",", gm.upgradeSystem.GenerateOptions().Select(o => o.kind.ToString())));
        }
        Check(choices.Count > 1, "fresh run seeds produce varied buff choices");
        loop.OpenWarehouse();
        loop.TryEquip("lance"); loop.TryEquip("salvo");
        loop.CloseWarehouse(); gm.BeginRun();
        yield return null;
        gm.stageManager.StopStage();
        Check(gm.upgradeSystem.Count == 0 && Mathf.Approximately(player.stats.DashDistance, player.stats.baseDashDistance), "next sortie starts with no carried buffs or old pack bonuses");
        player.AimAt(player.transform.position + Vector3.forward * 20);
        player.weaponController.ResetCooldowns(); player.weaponController.TryFireBeam();
        beams = FindObjectsByType<Projectile>(FindObjectsSortMode.None).Where(p => p.team == 0 && !(p is MissileProjectile)).ToArray();
        Check(beams.Length == 1 && beams[0].pierceCount == 3 && beams[0].damage == 42, "lance cannon uses heavy piercing shot");
        dummy = new GameObject("SwarmTarget", typeof(BoxCollider), typeof(Damageable));
        dummy.transform.position = player.transform.position + Vector3.forward * 8;
        dummy.GetComponent<Damageable>().SetMaxHealth(9999, true);
        player.weaponController.TryFireSkill(dummy.GetComponent<Damageable>());
        Check(FindObjectsByType<MissileProjectile>(FindObjectsSortMode.None).Count(p => p.team == 0) == 8, "swarm pack launches eight missiles");
        for (int i = 0; i < 7; i++) yield return null;
        Capture("08_swarm_in_combat");
        report.Add("GPU " + SystemInfo.graphicsDeviceName + " / " + SystemInfo.graphicsDeviceType);
    }
    private void Check(bool passed, string name)
    {
        report.Add((passed ? "PASS " : "FAIL ") + name);
        if (!passed) throw new Exception(name);
    }
    private static void Click(string name)
    {
        var button = FindObjectsByType<Button>(FindObjectsSortMode.None).FirstOrDefault(b => b.name == name && b.isActiveAndEnabled && b.interactable);
        if (button == null) throw new Exception("Missing active button " + name);
        button.onClick.Invoke();
    }
    private void Capture(string name)
    {
        CaptureTo(output, name);
    }
    public static void CaptureTo(string output, string name, int width = 1600, int height = 900, bool includeUI = true)
    {
        var main = Camera.main;
        var target = new RenderTexture(width, height, 24);
        var previousTarget = main.targetTexture;
        int previousMask = main.cullingMask;
        var previousActive = RenderTexture.active;
        var uiObject = new GameObject("QuickCheckUICamera");
        var uiCamera = uiObject.AddComponent<Camera>();
        uiCamera.enabled = false;
        uiCamera.clearFlags = CameraClearFlags.Depth;
        uiCamera.cullingMask = 1 << 5;
        uiCamera.nearClipPlane = .01f; uiCamera.farClipPlane = 10;
        uiCamera.targetTexture = target;
        var canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None).Where(c => c.renderMode != RenderMode.WorldSpace).ToArray();
        var modes = canvases.Select(c => c.renderMode).ToArray();
        var cameras = canvases.Select(c => c.worldCamera).ToArray();
        var transforms = canvases.SelectMany(c => c.GetComponentsInChildren<Transform>(true)).Distinct().ToArray();
        var layers = transforms.Select(t => t.gameObject.layer).ToArray();
        var texture = new Texture2D(width, height, TextureFormat.RGB24, false);
        try
        {
            foreach (var child in transforms) child.gameObject.layer = 5;
            foreach (var canvas in canvases) { canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = uiCamera; canvas.planeDistance = 1; }
            main.targetTexture = target; main.cullingMask &= ~(1 << 5);
            Canvas.ForceUpdateCanvases(); main.Render(); if (includeUI) uiCamera.Render();
            RenderTexture.active = target;
            texture.ReadPixels(new Rect(0, 0, width, height), 0, 0); texture.Apply();
            File.WriteAllBytes(Path.Combine(output, name + ".png"), texture.EncodeToPNG());
        }
        finally
        {
            for (int i = 0; i < canvases.Length; i++) { canvases[i].renderMode = modes[i]; canvases[i].worldCamera = cameras[i]; }
            for (int i = 0; i < transforms.Length; i++) transforms[i].gameObject.layer = layers[i];
            main.targetTexture = previousTarget; main.cullingMask = previousMask;
            RenderTexture.active = previousActive;
            Destroy(texture); Destroy(target); Destroy(uiObject);
            Canvas.ForceUpdateCanvases();
        }
    }
    private void OnLog(string message, string stack, LogType type)
    {
        if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert) return;
        if (stack.Contains("UnityEditor.Search.SearchDatabase")) return;
        errors++; report.Add("RUNTIME ERROR " + message + "\n" + stack);
    }
    private void Finish(string failure)
    {
        if (finished) return;
        finished = true;
        if (failure != null) report.Add(failure);
        int code = failure == null && errors == 0 ? 0 : 1;
        report.Add("EQUIPMENT_QUICK_CHECK code=" + code + " errors=" + errors);
        if (!string.IsNullOrEmpty(output)) File.WriteAllLines(Path.Combine(output, "quick-check.txt"), report);
        Debug.Log(report.Last());
        Application.logMessageReceived -= OnLog;
#if UNITY_EDITOR
        if (Application.isEditor) { UnityEditor.EditorApplication.Exit(code); return; }
#endif
        Application.Quit(code);
    }
}
