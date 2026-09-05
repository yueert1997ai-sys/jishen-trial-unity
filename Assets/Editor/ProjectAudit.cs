using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
    private static bool projectError;
    private static readonly List<string> evidence = new List<string>();
    private static string Output => Path.GetFullPath("AuditEvidence");

    static ProjectAudit()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Active, false))
            {
                evidence.Clear();
                projectError = false;
                UnityEngine.Random.InitState(5092026);
                deadline = EditorApplication.timeSinceStartup + 1200;
                lastFrame = -1;
                routine = SessionState.GetBool(Active + ".Progression", false) ? ProgressionScenarios() : SessionState.GetBool(Active + ".Feedback", false) ? FeedbackScenarios() : SessionState.GetBool(Active + ".Mobile", false) ? MobileScenarios() : SessionState.GetBool(Active + ".Shots", false) ? ShotDiagnostics() : RunScenarios();
                string suite = SessionState.GetString(Active + ".Suite", "");
                if (suite == "Boss") routine = BossScenarios();
                if (suite == "Slice") routine = SliceScenarios();
                if (suite == "Builds") routine = BuildScenarios();
                if (suite == "BossLive") routine = BossLiveScenarios();
                if (suite == "Presentation") routine = PresentationAudit.Scenarios();
                Application.logMessageReceived += CaptureLog;
                EditorApplication.update += Tick;
            }
        };
    }

    public static void Run()
    {
        SessionState.SetString(Active + ".Suite", "");
        SessionState.SetBool(Active + ".Progression", false);
        SessionState.SetBool(Active + ".Feedback", false);
        SessionState.SetBool(Active + ".Mobile", false);
        SessionState.SetBool(Active + ".Shots", false);
        Directory.CreateDirectory(Output);
        Inventory();
        SessionState.SetBool(Active, true);
        EditorSceneManager.OpenScene("Assets/Scenes/Demo_Main.unity");
        EditorApplication.EnterPlaymode();
    }

    public static void RunShotDiagnostics()
    {
        SessionState.SetString(Active + ".Suite", "");
        SessionState.SetBool(Active + ".Progression", false);
        SessionState.SetBool(Active + ".Feedback", false);
        SessionState.SetBool(Active + ".Mobile", false);
        Directory.CreateDirectory(Output);
        SessionState.SetBool(Active + ".Shots", true);
        SessionState.SetBool(Active, true);
        EditorSceneManager.OpenScene("Assets/Scenes/Demo_Main.unity");
        EditorApplication.EnterPlaymode();
    }

    public static void RunMobileTests()
    {
        SessionState.SetString(Active + ".Suite", "");
        SessionState.SetBool(Active + ".Progression", false);
        SessionState.SetBool(Active + ".Feedback", false);
        Directory.CreateDirectory(Output);
        SessionState.SetBool(Active + ".Mobile", true);
        SessionState.SetBool(Active, true);
        EditorSceneManager.OpenScene("Assets/Scenes/Demo_Main.unity");
        EditorApplication.EnterPlaymode();
    }

    public static void RunFeedbackTests()
    {
        SessionState.SetString(Active + ".Suite", "");
        SessionState.SetBool(Active + ".Progression", false);
        Directory.CreateDirectory(Output);
        SessionState.SetBool(Active + ".Feedback", true);
        SessionState.SetBool(Active, true);
        EditorSceneManager.OpenScene("Assets/Scenes/Demo_Main.unity");
        EditorApplication.EnterPlaymode();
    }

    public static void RunProgressionTests()
    {
        SessionState.SetString(Active + ".Suite", "");
        Directory.CreateDirectory(Output);
        SessionState.SetBool(Active + ".Progression", true);
        SessionState.SetBool(Active, true);
        EditorSceneManager.OpenScene("Assets/Scenes/Demo_Main.unity");
        EditorApplication.EnterPlaymode();
    }

    public static void RunBossTests() { StartSuite("Boss"); }
    public static void RunSliceTests() { StartSuite("Slice"); }
    public static void RunBuildTests() { StartSuite("Builds"); }
    public static void RunBossLiveTests() { StartSuite("BossLive"); }
    public static void RunPresentationTests() { StartSuite("Presentation"); }

    private static void StartSuite(string suite)
    {
        Directory.CreateDirectory(Output);
        SessionState.SetString(Active + ".Suite", suite);
        SessionState.SetBool(Active, true);
        EditorSceneManager.OpenScene("Assets/Scenes/Demo_Main.unity");
        EditorApplication.EnterPlaymode();
    }

    private static IEnumerator BossLiveScenarios()
    {
        yield return null;
        yield return null;
        var gm = GameManager.Instance;
        gm.playerController.InputRouter.readKeyboard = false;
        Click("DeployButton");
        gm.stageManager.StopStage();
        gm.arenaSector.ShowSector(2);
        gm.upgradeSystem.ResetUpgrades(5092026);
        foreach (var kind in new[] { RunUpgradeKind.FireRate, RunUpgradeKind.FireRate, RunUpgradeKind.FireRate,
            RunUpgradeKind.PiercingRounds, RunUpgradeKind.ArmorPlating, RunUpgradeKind.DashCapacitor })
            gm.upgradeSystem.ApplyOption(new RunUpgradeOption { kind = kind });
        var boss = gm.stageManager.enemySpawner.SpawnBoss();
        float started = Time.time;
        bool phaseTwo = false;
        int waypoint = 0, actions = 0;
        Vector3[] route = { new Vector3(-4, 0, -5), new Vector3(4, 0, -5), new Vector3(4, 0, 5), new Vector3(-4, 0, 5) };
        while (boss != null && !boss.GetComponent<Damageable>().IsDead && gm.IsCombatActive && Time.time - started < 180)
        {
            var player = gm.playerController;
            Vector3 delta = route[waypoint] - player.transform.position;
            delta.y = 0;
            if (delta.magnitude < 0.8f) waypoint = (waypoint + 1) % route.Length;
            player.InputRouter.SetTouchMove(new Vector2(delta.x, delta.z).normalized);
            // Save the active salvo for a genuine core opening.
            if (boss.CoreExposed) player.InputRouter.QueueSkill();
            phaseTwo |= boss.IsPhaseTwo;
            actions = boss.ActionsCompleted;
            yield return null;
        }
        float seconds = Time.time - started;
        Record("BOSS_LIVE seconds=" + seconds + " hp=" + gm.playerStats.CurrentHp + " phaseTwo=" + phaseTwo + " actions=" + actions + " dead=" + (boss == null || boss.GetComponent<Damageable>().IsDead));
        Check((boss == null || boss.GetComponent<Damageable>().IsDead) && gm.playerStats.CurrentHp > 0 && phaseTwo, "boss_live_fire_win");
        Check(seconds >= 45 && seconds <= 100, "boss_live_pacing");
        gm.EnterResult(true);
        Capture("boss_live_result", 1280, 720);
    }

    private static IEnumerator BuildScenarios()
    {
        yield return null;
        yield return null;
        var gm = GameManager.Instance;
        Click("DeployButton");
        gm.stageManager.StopStage();
        gm.playerController.enabled = false;
        Warp(gm.playerController, Vector3.zero);
        var first = SpawnStationaryEnemy(new Vector3(0, 0, 4));
        var second = SpawnStationaryEnemy(new Vector3(0, 0, 8));
        var splash = SpawnStationaryEnemy(new Vector3(1, 0, 4));
        foreach (var actor in new[] { first, second, splash }) actor.SetMaxHealth(1000, true);
        int firstHits = 0, secondHits = 0, splashHits = 0;
        first.OnDamaged += (d, info) => firstHits++;
        second.OnDamaged += (d, info) => secondHits++;
        splash.OnDamaged += (d, info) => splashHits++;
        Physics.SyncTransforms();
        var shot = ProjectilePool.Spawn(false, "AuditPiercingBlast", Vector3.up * 0.9f, Color.cyan);
        shot.Init(0, null, Vector3.forward, 25, 80, 1, 1.45f, 1);
        float until = Time.time + 0.3f;
        while (Time.time < until) yield return null;
        Check(firstHits == 1 && secondHits == 1 && splashHits == 1, "piercing_blast_hits_both_groups_once");
        Check(Mathf.Approximately(first.CurrentHealth, 975) && Mathf.Approximately(second.CurrentHealth, 975) && splash.CurrentHealth < 1000, "pierce_keeps_direct_and_splash_damage");
        foreach (var actor in new[] { first, second, splash }) Object.Destroy(actor.gameObject);
        yield return null;
        var upgrades = gm.upgradeSystem;
        var weapon = gm.playerController.weaponController;
        var target = SpawnStationaryEnemy(new Vector3(0, 0, 8));
        target.SetMaxHealth(10000, true);
        gm.playerController.AimAt(target.AimCenter);
        RunUpgradeKind[][] builds = {
            new[] { RunUpgradeKind.FireRate, RunUpgradeKind.FireRate, RunUpgradeKind.PiercingRounds, RunUpgradeKind.PiercingRounds, RunUpgradeKind.BeamDamage, RunUpgradeKind.BeamDamage },
            new[] { RunUpgradeKind.SplitterBeam, RunUpgradeKind.SplitterBeam, RunUpgradeKind.BurstCore, RunUpgradeKind.BurstCore, RunUpgradeKind.BeamDamage, RunUpgradeKind.BeamDamage }
        };
        for (int build = 0; build < builds.Length; build++)
        {
            upgrades.ResetUpgrades(100 + build);
            foreach (var kind in builds[build]) upgrades.ApplyOption(new RunUpgradeOption { kind = kind });
            weapon.ResetCooldowns();
            weapon.TryFireBeam();
            var shots = Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None).Where(p => p.name == "BeamProjectile").ToArray();
            Check(shots.Length == (build == 0 ? 1 : 5), "build_projectile_count_" + build);
            Check(build == 0 ? shots[0].pierceCount == 4 && Mathf.Approximately(upgrades.FireRateMultiplier, 1.5f)
                : shots.All(p => p.explosionRadius > 2 && p.damage < 12f), "build_distinct_combat_payload_" + build);
            float hp = target.CurrentHealth;
            until = Time.time + 0.5f;
            while (Time.time < until) yield return null;
            Check(target.CurrentHealth < hp, "build_real_target_damage_" + build);
            Record("BUILD_DAMAGE build=" + build + " loss=" + (hp - target.CurrentHealth) + " config=" + upgrades.GetSummary());
            foreach (var projectile in Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None)) projectile.Despawn();
        }
        gm.EnterResult(true);
        Record("BUILD_CONTRACT_PASS real projectile payloads/hits, not a human build preference test");
    }

    private static IEnumerator BossScenarios()
    {
        yield return null;
        yield return null;
        var gm = GameManager.Instance;
        Click("DeployButton");
        gm.stageManager.StopStage();
        var player = gm.playerController;
        player.enabled = false;
        Warp(player, new Vector3(0, 0, -4));
        gm.arenaSector.ShowSector(2);
        var boss = gm.stageManager.enemySpawner.SpawnBoss();
        boss.enabled = false;
        boss.GetComponent<UnityEngine.AI.NavMeshAgent>().Warp(new Vector3(0, 0, 6));
        yield return null;
        Check(boss.StartPattern(BossPattern.Scatter), "boss_accepts_single_action");
        Check(!boss.StartPattern(BossPattern.Charge), "boss_actions_cannot_overlap");
        Vector3 direction = boss.LockedDirection;
        Warp(player, new Vector3(6, 0, -4));
        float until = Time.time + 0.95f;
        while (Time.time < until) yield return null;
        var beams = Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None).Where(p => p.name == "BossScatterBeam").ToArray();
        Check(beams.Length == 7 && beams.Any(p => Vector3.Angle(p.direction, direction) < 0.01f), "scatter_uses_locked_warning_direction");
        Check(beams.All(p => p.pool != null), "boss_beams_are_pooled");
        Capture("boss_01_locked_scatter");
        while (boss.ActionRunning) yield return null;
        foreach (var shot in Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None)) shot.Despawn();
        Warp(player, new Vector3(0, 0, -3));
        float hp = gm.playerStats.CurrentHp;
        boss.StartPattern(BossPattern.Mortar);
        Vector3 marked = boss.LockedImpact;
        Warp(player, marked + Vector3.back * 4f);
        until = Time.time + 1.8f;
        while (Time.time < until) yield return null;
        Check(Mathf.Approximately(gm.playerStats.CurrentHp, hp), "mortar_dodge_outside_mark_is_safe");
        Check(boss.LockedImpact == marked, "mortar_never_tracks_after_warning");
        while (boss.ActionRunning) yield return null;
        boss.StartPattern(BossPattern.Mortar);
        hp = gm.playerStats.CurrentHp;
        until = Time.time + 1.2f;
        while (Time.time < until) yield return null;
        Check(gm.playerStats.CurrentHp < hp, "mortar_damages_inside_mark");
        until = Time.time + 2f;
        while (Time.time < until && !boss.CoreExposed) yield return null;
        Check(boss.CoreExposed && Mathf.Approximately(boss.GetComponent<Damageable>().IncomingDamageScale, 1f), "boss_recovery_exposes_core");
        while (boss.ActionRunning) yield return null;
        Warp(player, new Vector3(0, 0, -7));
        boss.GetComponent<UnityEngine.AI.NavMeshAgent>().Warp(new Vector3(0, 0, 6));
        var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = "AuditChargeCover";
        wall.transform.position = new Vector3(0, 1.5f, 0);
        wall.transform.localScale = new Vector3(7, 3, 0.4f);
        Physics.SyncTransforms();
        boss.StartPattern(BossPattern.Charge);
        Check(boss.LockedLength < 5f && boss.LockedLength > 3f, "charge_telegraph_stops_at_cover");
        Vector3 end = boss.LockedOrigin + boss.LockedDirection * boss.LockedLength;
        hp = gm.playerStats.CurrentHp;
        while (boss.ActionRunning) yield return null;
        Check(Vector3.Distance(boss.transform.position, end) < 0.1f && Mathf.Approximately(hp, gm.playerStats.CurrentHp), "charge_motion_matches_warning_and_cover");
        Object.Destroy(wall);
        Check(BossController.InsideSweptDisc(new Vector3(1.9f, 3, 3), Vector3.zero, Vector3.forward * 6, 2), "charge_swept_hit_inside");
        Check(!BossController.InsideSweptDisc(new Vector3(2.1f, 0, 3), Vector3.zero, Vector3.forward * 6, 2), "charge_swept_hit_outside");
        boss.enabled = true;
        boss.GetComponent<Damageable>().SetCurrentHealth(boss.GetComponent<Damageable>().maxHealth * 0.49f);
        yield return null;
        Check(boss.IsPhaseTwo, "boss_second_phase_at_half_health");
        gm.EnterResult(false);
        yield return null;
        Check(Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None).Length == 0 && Object.FindObjectsByType<TelegraphVisual>(FindObjectsSortMode.None).Length == 0, "boss_defeat_cancels_attacks");
        Record("BOSS_CONTRACT_PASS real timed coroutines / synthetic positions, no pacing claim");
    }

    private static IEnumerator SliceScenarios()
    {
        yield return null;
        yield return null;
        var gm = GameManager.Instance;
        gm.playerController.InputRouter.readKeyboard = false;
        Click("DeployButton");
        gm.upgradeSystem.ResetUpgrades(5092026);
        float started = Time.time;
        float bossStarted = -1;
        int choices = 0, waypoint = 0;
        bool phaseTwo = false;
        bool[] captured = new bool[6];
        Vector3[] route = { new Vector3(-4, 0, -5), new Vector3(4, 0, -5), new Vector3(4, 0, 5), new Vector3(-4, 0, 5) };
        while (gm.Phase != GamePhase.Result && Time.time - started < 1000f)
        {
            if (gm.Phase == GamePhase.Reward)
            {
                yield return null;
                Capture("slice_reward_" + choices, 1280, 720);
                Click("ChooseButton");
                choices++;
                Record("SLICE_CHOICE count=" + choices + " seconds=" + (Time.time - started) + " build=" + gm.upgradeSystem.GetSummary());
            }
            if (gm.IsCombatActive)
            {
                var player = gm.playerController;
                Vector3 delta = route[waypoint] - player.transform.position;
                delta.y = 0;
                if (delta.magnitude < 0.8f) waypoint = (waypoint + 1) % route.Length;
                player.InputRouter.SetTouchMove(new Vector2(delta.x, delta.z).normalized);
                player.InputRouter.QueueSkill();
                int index = gm.stageManager.CurrentEncounter;
                if (index < 6 && !captured[index] && gm.stageManager.EnemiesAlive >= 5)
                {
                    captured[index] = true;
                    Capture("slice_encounter_" + index);
                }
                var boss = Object.FindFirstObjectByType<BossController>();
                if (boss != null)
                {
                    if (bossStarted < 0) { bossStarted = Time.time; Capture("slice_boss_1"); }
                    if (!phaseTwo && boss.IsPhaseTwo) { phaseTwo = true; Capture("slice_boss_2"); }
                }
            }
            yield return null;
        }
        Record("SLICE_LIVE_FLOW victory=" + gm.LastResultVictory + " seconds=" + (Time.time - started) + " choices=" + choices + " hp=" + gm.playerStats.CurrentHp + " kills=" + gm.Kills + " bossSeconds=" + (bossStarted < 0 ? -1 : Time.time - bossStarted) + " phaseTwo=" + phaseTwo);
        Record("SLICE_ENCOUNTERS " + string.Join(",", gm.stageManager.EncounterSeconds.Select(x => x.ToString("0.00"))));
        Capture("slice_result", 1280, 720);
        Check(gm.Phase == GamePhase.Result && gm.LastResultVictory && choices == 6 && phaseTwo, "slice_real_fire_complete_loop");
        Check(Time.time - started >= 600f && Time.time - started <= 900f, "slice_ten_to_fifteen_minutes");
        Check(gm.stageManager.EncounterSeconds.All(x => x >= 68 && x <= 130), "encounter_pacing_no_runaway_backlog");
        Click("ReturnHangarButton");
        yield return null;
        yield return null;
        Check(GameManager.Instance.Phase == GamePhase.Hangar, "slice_restart_to_hangar");
    }

    private static IEnumerator ProgressionScenarios()
    {
        yield return null;
        yield return null;
        var gm = GameManager.Instance;
        Click("DeployButton");
        gm.stageManager.StopStage();
        gm.playerController.enabled = false;
        var upgrades = gm.upgradeSystem;
        upgrades.ResetUpgrades(5092026);
        var options = upgrades.GenerateOptions();
        for (int i = 0; i < 100; i++) UnityEngine.Random.Range(0f, 1f);
        Check(options.Select(x => x.kind).SequenceEqual(upgrades.GenerateOptions().Select(x => x.kind)), "upgrade_rng_independent_of_effects");
        for (int i = 0; i < 2; i++) Check(upgrades.ApplyOption(new RunUpgradeOption { kind = RunUpgradeKind.SplitterBeam }), "split_rank_" + (i + 1));
        Check(!upgrades.ApplyOption(new RunUpgradeOption { kind = RunUpgradeKind.SplitterBeam }), "rank_cap_rejects_overflow");
        Check(upgrades.BonusBeamProjectiles == 4 && Mathf.Approximately(upgrades.SplitShotMultiplier, 0.4f), "split_build_damage_budget");
        Check(upgrades.GenerateOptions().All(x => x.kind != RunUpgradeKind.SplitterBeam), "capped_upgrade_not_offered");
        upgrades.ResetUpgrades(5092026);
        for (int i = 0; i < 3; i++)
        {
            gm.stageManager.StopStage();
            gm.OnEncounterCleared(i);
            yield return null;
            Check(gm.Phase == GamePhase.Reward && gm.CompletedEncounters == i + 1, "reward_transition_" + i);
            gm.FinishReward();
            Check(gm.Phase == GamePhase.Reward, "cannot_skip_upgrade_" + i);
            Click("ChooseButton");
            Check(upgrades.Count == i + 1 && gm.stageManager.CurrentEncounter == i + 1, "installed_before_next_encounter_" + i);
            yield return null;
        }
        Check(gm.CheckpointEncounter == 3 && upgrades.Count == 3, "sector_two_checkpoint_after_third_choice");
        gm.stageManager.StopStage();
        int savedKills = gm.Kills;
        int savedCoins = gm.Coins;
        float savedHp = gm.playerStats.CurrentHp;
        float savedMax = gm.playerStats.MaxHp;
        var savedBuild = upgrades.Acquired.ToArray();
        var savedOptions = upgrades.GenerateOptions().Select(x => x.kind).ToArray();
        gm.OnEncounterCleared(3);
        yield return null;
        Click("ChooseButton");
        gm.stageManager.StopStage();
        gm.RegisterKill(500);
        gm.playerStats.AddMaxHealth(100, true);
        gm.playerController.GetComponent<Damageable>().Kill(null);
        yield return null;
        Check(gm.CanContinue, "demo_defeat_offers_continue");
        Capture("progression_01_continue", 1280, 720);
        float frozenTime = gm.GetRunTime();
        float wait = Time.time + 0.4f;
        while (Time.time < wait) yield return null;
        Click("ContinueRunButton");
        gm.stageManager.StopStage();
        Check(gm.Phase == GamePhase.Combat && gm.ContinueUsed && gm.CompletedEncounters == 3, "continue_restores_sector_entry");
        Check(gm.Kills == savedKills && gm.Coins == savedCoins, "continue_discards_failed_segment_rewards");
        Check(upgrades.Acquired.SequenceEqual(savedBuild) && upgrades.GenerateOptions().Select(x => x.kind).SequenceEqual(savedOptions), "continue_restores_build_and_choice_rng");
        Check(Mathf.Approximately(gm.playerStats.MaxHp, savedMax) && Mathf.Abs(gm.playerStats.CurrentHp - savedHp) < 0.01f, "continue_restores_health_and_stats");
        Check(!gm.playerController.GetComponent<Damageable>().IsDead && gm.GetRunTime() - frozenTime < 0.1f, "continue_revives_and_excludes_result_wait");
        for (int i = 3; i < 6; i++)
        {
            gm.stageManager.StopStage();
            gm.OnEncounterCleared(i);
            yield return null;
            if (i == 5) Capture("progression_02_sixth_choice", 1280, 720);
            Click("ChooseButton");
            Check(gm.Phase == GamePhase.Combat && upgrades.Count == i + 1, "six_choices_contract_" + i);
            yield return null;
        }
        Check(gm.CheckpointEncounter == 6 && gm.CompletedEncounters == 6, "boss_checkpoint_after_sixth_choice");
        gm.stageManager.StopStage();
        gm.EnterResult(false);
        yield return null;
        Check(!gm.CanContinue && !gm.ContinueRun(), "continue_only_once");
        Click("ReturnHangarButton");
        yield return null;
        yield return null;
        gm = GameManager.Instance;
        gm.SetDifficulty(RunDifficulty.Standard);
        Click("DeployButton");
        gm.stageManager.StopStage();
        gm.playerController.GetComponent<Damageable>().Kill(null);
        yield return null;
        Check(!gm.CanContinue, "standard_has_no_continue");
        Click("ReturnHangarButton");
        yield return null;
        yield return null;
        gm = GameManager.Instance;
        Click("DeployButton");
        gm.stageManager.StopStage();
        gm.OnStageCleared(2);
        Check(gm.Phase == GamePhase.Combat, "victory_cannot_skip_six_encounters");
        Record("PROGRESSION_CONTRACT_PASS synthetic phase completions; not pacing/combat evidence");
    }

    private static IEnumerator FeedbackScenarios()
    {
        var mobile = MobileScenarios();
        while (mobile.MoveNext()) yield return null;
        var gm = GameManager.Instance;
        Click("DeployButton");
        gm.stageManager.StopStage();
        var player = gm.playerController;
        player.enabled = false;
        player.automaticFire = false;
        Check(GameAudio.Instance.LoadedCueCount == System.Enum.GetValues(typeof(GameAudioCue)).Length, "audio_all_cues_loaded");
        Check(GameAudio.Instance.MusicLoaded, "music_loaded");
        Warp(player, new Vector3(9.4f, 0, 2.5f));
        var moving = SpawnStationaryEnemy(new Vector3(9.4f, 0, 14));
        var ai = moving.GetComponent<EnemyBase>();
        ai.enabled = true;
        ai.Init(player.transform, gm.stageManager);
        float until = Time.time + 7;
        while (Time.time < until)
        {
            Vector3 pos = moving.transform.position;
            if (pos.x > 8 && pos.x < 10.8f && pos.z > 5.8f && pos.z < 10.6f) throw new Exception("Enemy entered repair bank.");
            yield return null;
        }
        Check(Vector3.Distance(moving.transform.position, player.transform.position) < 2.2f, "navmesh_routes_around_bank");
        Object.Destroy(moving.gameObject);
        Warp(player, Vector3.zero);
        var target = SpawnStationaryEnemy(new Vector3(0, 0, 6));
        target.SetMaxHealth(1000, true);
        var extra = new GameObject("AuditCompoundCollider");
        extra.transform.SetParent(target.transform, false);
        var capsule = extra.AddComponent<CapsuleCollider>();
        capsule.center = Vector3.up * 0.9f;
        capsule.height = 1.8f;
        capsule.radius = 0.5f;
        Physics.SyncTransforms();
        int hits = 0;
        target.OnDamaged += (d, info) => hits++;
        var shot = ProjectilePool.Spawn(false, "AuditBlast", Vector3.up * 0.9f, Color.cyan);
        shot.Init(0, null, Vector3.forward, 25, 80, 1, 1.6f, 0);
        until = Time.time + 0.3f;
        while (Time.time < until) yield return null;
        Check(hits == 1 && Mathf.Abs(target.CurrentHealth - 975) < 0.01f, "blast_compound_collider_once_full_direct_damage");
        Object.Destroy(target.gameObject);
        yield return null;
        var bossObject = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemies/Boss_HeavyMech.prefab"), Vector3.forward * 6, Quaternion.identity);
        bossObject.GetComponent<BossController>().enabled = false;
        target = bossObject.GetComponent<Damageable>();
        target.SetMaxHealth(1000, true);
        hits = 0;
        target.OnDamaged += (d, info) => hits++;
        Physics.SyncTransforms();
        shot = ProjectilePool.Spawn(false, "AuditLargeBossBlast", Vector3.up * 1.4f, Color.cyan);
        shot.Init(0, null, Vector3.forward, 25, 80, 1, 0.5f, 0);
        until = Time.time + 0.3f;
        while (Time.time < until) yield return null;
        Check(hits == 1 && Mathf.Abs(target.CurrentHealth - 975) < 0.01f, "blast_direct_hit_larger_than_radius");
        var reuse = ProjectilePool.Spawn(false, "AuditReuse", Vector3.up * 4, Color.cyan);
        reuse.Init(0, null, Vector3.right, 1, 1, 1, 0, 3);
        reuse.Despawn();
        int created = ProjectilePool.Instance.CreatedCount;
        var again = ProjectilePool.Spawn(false, "AuditReuseAgain", Vector3.up * 4, Color.cyan);
        again.Init(0, null, Vector3.right, 2, 1, 1, 0, 0);
        Check(again == reuse && again.pierceCount == 0 && ProjectilePool.Instance.CreatedCount == created, "projectile_pool_reuses_and_resets");
        again.Despawn();
        again.Despawn();
        var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.transform.position = new Vector3(0, 1, 3);
        wall.transform.localScale = new Vector3(3, 2, 0.1f);
        Physics.SyncTransforms();
        hits = 0;
        shot = ProjectilePool.Spawn(false, "AuditWallShot", Vector3.up * 0.9f, Color.cyan);
        shot.Init(0, null, Vector3.forward, 25, 200, 1, 0, 2);
        until = Time.time + 0.2f;
        while (Time.time < until) yield return null;
        Check(hits == 0 && !shot.gameObject.activeSelf, "swept_shot_stops_at_thin_wall");
        Object.Destroy(wall);
        CombatEffects.Impact(Vector3.up, Color.yellow, 1, true);
        yield return null;
        var ps = GameObject.Find("ArmorSparks").GetComponent<ParticleSystem>();
        Check(ps.particleCount > 0 && ps.isPlaying, "particles_live_simulation");
        Capture("feedback_07_particles");
        gm.EnterResult(false);
        yield return null;
        Check(Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None).Length == 0 && Object.FindObjectsByType<TelegraphVisual>(FindObjectsSortMode.None).Length == 0, "pool_phase_cleanup");
        Record("FEEDBACK_SUITE_PASS");
    }

    private static IEnumerator MobileScenarios()
    {
        yield return null;
        yield return null;
        var gm = GameManager.Instance;
        float opening = Time.time + 0.8f;
        while (Time.time < opening) yield return null;
        Capture("mobile_00_hangar");
        Capture("mobile_00_hangar_wide", 2400, 1080, new Rect(90, 35, 2220, 1045));
        Click("DeployButton");
        gm.stageManager.StopStage();
        var player = gm.playerController;
        player.enabled = false;
        player.automaticFire = false;
        player.InputRouter.readKeyboard = false;
        yield return null;
        var controls = player.GetComponent<MobileControls>();
        var joystick = controls.Joystick;
        Canvas.ForceUpdateCanvases();
        var pointer = new PointerEventData(EventSystem.current) { pointerId = 31, position = RectTransformUtility.WorldToScreenPoint(null, joystick.transform.TransformPoint(new Vector3(44, 0, 0))) };
        ExecuteEvents.Execute(joystick.gameObject, pointer, ExecuteEvents.pointerDownHandler);
        Check(joystick.Value.x > 0.99f, "stick_drag");
        var second = new PointerEventData(EventSystem.current) { pointerId = 32, position = pointer.position - Vector2.right * 40 };
        ExecuteEvents.Execute(joystick.gameObject, second, ExecuteEvents.pointerDownHandler);
        Check(joystick.PointerId == 31 && joystick.Value.x > 0.99f, "stick_pointer_ownership");
        var dash = GameObject.Find("DashButton");
        ExecuteEvents.Execute(dash, second, ExecuteEvents.pointerDownHandler);
        var command = player.InputRouter.ReadCommand();
        Check(command.Move.x > 0.99f && command.Dash, "two_finger_move_dash");
        ExecuteEvents.Execute(joystick.gameObject, second, ExecuteEvents.pointerUpHandler);
        Check(joystick.Value.x > 0.99f, "other_finger_release");
        ExecuteEvents.Execute(joystick.gameObject, pointer, ExecuteEvents.pointerUpHandler);
        Check(player.InputRouter.ReadCommand().Move == Vector2.zero, "release_clears_move");
        ExecuteEvents.Execute(joystick.gameObject, pointer, ExecuteEvents.pointerDownHandler);
        Click("PauseButton");
        yield return null;
        Check(joystick.Value == Vector2.zero && player.InputRouter.ReadCommand().Move == Vector2.zero, "pause_clears_input");
        Capture("mobile_00_pause", 1280, 720);
        Click("ResumeButton");
        yield return null;
        Check(gm.IsCombatActive, "resume_combat");

        float cardinal = 0f;
        foreach (Vector2 movement in new[] { Vector2.right, Vector2.one })
        {
            Warp(player, Vector3.zero);
            for (int i = 0; i < 60; i++)
            {
                player.Simulate(new PlayerCommand { Move = movement }, 1f / 60f);
                yield return null;
            }
            float distance = new Vector2(player.transform.position.x, player.transform.position.z).magnitude;
            if (movement == Vector2.right) cardinal = distance;
            else Check(Mathf.Abs(distance - cardinal) < 0.12f, "diagonal_speed_normalized");
            Check(distance > 6f && distance < 7.6f, "motor_distance_" + movement);
            for (int i = 0; i < 20; i++) { player.Simulate(default, 1f / 60f); yield return null; }
            Check(player.Velocity.magnitude < 0.02f, "motor_brakes");
        }
        Warp(player, Vector3.zero);
        var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = "AuditMovementWall";
        wall.transform.position = new Vector3(2, 1.5f, 0);
        wall.transform.localScale = new Vector3(0.4f, 3, 8);
        Physics.SyncTransforms();
        Vector3 beforeDash = player.transform.position;
        Check(player.TryDash(Vector2.right), "dash_accepted");
        Check(player.transform.position == beforeDash, "dash_not_teleport");
        for (int i = 0; i < 20; i++) { player.Simulate(default, 1f / 60f); yield return null; }
        Check(player.transform.position.x > 0.4f && player.transform.position.x < 1.15f, "dash_stops_at_wall");
        Check(!player.TryDash(Vector2.right), "dash_cooldown");
        Object.Destroy(wall);
        yield return null;
        Warp(player, Vector3.zero);
        float settle = Time.time + 0.5f;
        while (Time.time < settle) yield return null;
        var enemy = SpawnStationaryEnemy(new Vector3(0, 0, 6));
        Physics.SyncTransforms();
        player.AutoAim.Clear();
        player.AutoAim.Tick();
        Check(player.AutoAim.CurrentTarget == enemy, "auto_acquires_visible_target");
        var farther = SpawnStationaryEnemy(new Vector3(4.5f, 0, 0));
        Physics.SyncTransforms();
        settle = Time.time + 0.12f;
        while (Time.time < settle) yield return null;
        player.AutoAim.Tick();
        Check(player.AutoAim.CurrentTarget == enemy, "auto_target_hysteresis");
        farther.transform.position = new Vector3(3, 0, 0);
        Physics.SyncTransforms();
        settle = Time.time + 0.12f;
        while (Time.time < settle) yield return null;
        player.AutoAim.Tick();
        Check(player.AutoAim.CurrentTarget == farther, "auto_switches_closer_target");
        Object.Destroy(farther.gameObject);
        var blocker = GameObject.CreatePrimitive(PrimitiveType.Cube);
        blocker.transform.position = new Vector3(0, 1.5f, 3);
        blocker.transform.localScale = new Vector3(3, 3, 0.4f);
        Physics.SyncTransforms();
        Check(!player.AutoAim.IsValidTarget(enemy), "auto_rejects_occlusion");
        Object.Destroy(blocker);
        enemy.transform.position = Vector3.forward * 30;
        Physics.SyncTransforms();
        Check(!player.AutoAim.IsValidTarget(enemy), "auto_rejects_far_target");
        enemy.transform.position = new Vector3(0, 0, 6);
        enemy.SetMaxHealth(500, true);
        yield return null;
        Physics.SyncTransforms();
        player.AutoAim.Clear();
        player.AutoAim.Tick();
        player.AimAt(enemy.AimCenter);
        float hp = enemy.CurrentHealth;
        Check(player.weaponController.TryFireSkill(enemy), "skill_without_equipment");
        Check(!player.weaponController.TryFireSkill(enemy), "skill_cooldown");
        settle = Time.time + 1.5f;
        while (Time.time < settle) yield return null;
        Check(enemy.CurrentHealth < hp, "homing_skill_real_hit");
        Record("SKILL_DAMAGE " + (hp - enemy.CurrentHealth));
        var safe = SafeAreaLayout.Calculate(new Vector2(2400, 1080), new Rect(90, 35, 2220, 1045));
        Check(safe.xMin >= 0.0374f && safe.xMax <= 0.9626f && safe.yMin > 0f, "notch_safe_area");
        var narrow = SafeAreaLayout.Calculate(new Vector2(1024, 768), new Rect(0, 0, 1024, 768));
        Check(Mathf.Abs(narrow.yMin - 0.125f) < 0.001f && Mathf.Abs(narrow.height - 0.75f) < 0.001f, "narrow_letterbox");
        Capture("mobile_01_controls");
        Capture("mobile_02_wide", 2400, 1080);
        Record("MOBILE_FOCUSED_PASS");

        gm.RestartRun();
        yield return null;
        yield return null;
        var live = SliceScenarios();
        while (live.MoveNext()) yield return null;
    }

    private static void Warp(PlayerController player, Vector3 position)
    {
        player.CancelMovement();
        player.Motor.enabled = false;
        player.transform.position = position;
        player.Motor.enabled = true;
        Physics.SyncTransforms();
    }

    private static Damageable SpawnStationaryEnemy(Vector3 position)
    {
        var enemy = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemies/Enemy_Melee.prefab"), position, Quaternion.identity);
        enemy.GetComponent<EnemyBase>().enabled = false;
        return enemy.GetComponent<Damageable>();
    }

    internal static void Check(bool valid, string name)
    {
        if (!valid) throw new Exception("MOBILE_FAIL " + name);
        Record("MOBILE_PASS " + name);
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
        var live = SliceScenarios();
        while (live.MoveNext()) yield return null;
        var progression = ProgressionScenarios();
        while (progression.MoveNext()) yield return null;
        var gm = GameManager.Instance;
        gm.SetPaused(true);
        VerifyBlockedCombat(gm);
        gm.SetPaused(false);
        var player = gm.playerStats.GetComponent<Damageable>();
        var shot = ProjectilePool.Spawn(false, "AuditEnemyShot", player.transform.position + Vector3.up + Vector3.forward * 5, Color.red);
        shot.Init(1, null, Vector3.back, 30, 12, 2, 0, 0);
        gm.EnterReward();
        VerifyBlockedCombat(gm);
        float before = player.CurrentHealth;
        float until = Time.time + 1;
        while (Time.time < until) yield return null;
        Check(Mathf.Approximately(before, player.CurrentHealth), "reward_blocks_in_flight_damage");
        gm.RestartRun();
        yield return null;
        yield return null;
        gm = GameManager.Instance;
        Click("DeployButton");
        gm.playerStats.GetComponent<Damageable>().TakeDamage(99999, null);
        int kills = gm.Kills, coins = gm.Coins;
        gm.RegisterKill(100);
        gm.EnterResult(true);
        VerifyBlockedCombat(gm);
        until = Time.time + 3;
        while (Time.time < until) yield return null;
        Check(!gm.LastResultVictory && gm.Kills == kills && gm.Coins == coins && !gm.combatHUD.IsVisible, "result_freezes_score_and_outcome");
        Check(Object.FindObjectsByType<EnemyBase>(FindObjectsSortMode.None).Length == 0, "result_stops_delayed_spawns");
        Click("ReturnHangarButton");
        yield return null;
        yield return null;
        Check(GameManager.Instance.Phase == GamePhase.Hangar, "p0_defeat_restart");
    }

    private static void VerifyBlockedCombat(GameManager gm)
    {
        float hp = gm.playerStats.CurrentHp;
        int shots = Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None).Length;
        gm.playerController.weaponController.TryFireBeam();
        gm.playerController.weaponController.TryFireMissiles();
        gm.playerStats.GetComponent<Damageable>().TakeDamage(30, null);
        if (gm.playerStats.CurrentHp != hp || Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None).Length != shots)
            throw new Exception("Combat not blocked in " + gm.Phase + " paused=" + gm.IsPaused);
        Record("COMBAT_BLOCK_PASS phase=" + gm.Phase + " paused=" + gm.IsPaused);
    }

    internal static void Click(string name)
    {
        var button = Object.FindObjectsByType<Button>(FindObjectsSortMode.None).FirstOrDefault(b => b.name == name);
        if (button == null) throw new Exception("Missing visible button " + name);
        Canvas.ForceUpdateCanvases();
        var rect = button.GetComponent<RectTransform>();
        var canvas = button.GetComponentInParent<Canvas>();
        var position = RectTransformUtility.WorldToScreenPoint(canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera, rect.TransformPoint(rect.rect.center));
        var pointer = new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left, position = position };
        var hits = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointer, hits);
        if (hits.Count == 0 || hits[0].gameObject.GetComponentInParent<Button>() != button)
            throw new Exception("UI raycast blocked for " + name + " by " + (hits.Count == 0 ? "nothing" : hits[0].gameObject.name));
        ExecuteEvents.ExecuteHierarchy(hits[0].gameObject, pointer, ExecuteEvents.pointerClickHandler);
        Record("UI_CLICK " + name);
    }

    internal static void Capture(string name, int width = 1920, int height = 1080, Rect? safeArea = null, bool validateUI = false)
    {
        if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null) return;
        var camera = Camera.main;
        var canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).Where(c => c.renderMode != RenderMode.WorldSpace).ToArray();
        var modes = canvases.Select(c => c.renderMode).ToArray();
        var canvasCameras = canvases.Select(c => c.worldCamera).ToArray();
        var canvasScales = canvases.Select(c => c.scaleFactor).ToArray();
        var nodes = canvases.SelectMany(c => c.GetComponentsInChildren<Transform>(true)).Distinct().ToArray();
        var layers = nodes.Select(t => t.gameObject.layer).ToArray();
        var safeLayouts = canvases.SelectMany(c => c.GetComponentsInChildren<SafeAreaLayout>(true)).ToArray();
        var texture = new RenderTexture(width, height, 24);
        var previousTarget = camera.targetTexture;
        var previousActive = RenderTexture.active;
        var previousRect = camera.rect;
        int previousMask = camera.cullingMask;
        var uiCamera = new GameObject("AuditUICamera").AddComponent<Camera>();
        uiCamera.enabled = false;
        uiCamera.orthographic = true;
        uiCamera.transform.position = new Vector3(0, 10000, 0);
        uiCamera.clearFlags = CameraClearFlags.Depth;
        uiCamera.cullingMask = 1 << 5;
        uiCamera.nearClipPlane = 0.01f;
        uiCamera.farClipPlane = 10;
        uiCamera.targetTexture = texture;
        float viewportHeight = Mathf.Min(1f, width / (float)height * 9f / 16f);
        camera.rect = new Rect(0, (1f - viewportHeight) * 0.5f, 1, viewportHeight);
        camera.cullingMask &= ~(1 << 5);
        camera.targetTexture = texture;
        foreach (var node in nodes) node.gameObject.layer = 5;
        for (int i = 0; i < canvases.Length; i++)
        {
            canvases[i].renderMode = RenderMode.ScreenSpaceCamera;
            canvases[i].worldCamera = uiCamera;
            canvases[i].planeDistance = 1;
            var scaler = canvases[i].GetComponent<CanvasScaler>();
            if (scaler != null) canvases[i].scaleFactor = Mathf.Min(height, width * 9f / 16f) / scaler.referenceResolution.y;
        }
        foreach (var layout in safeLayouts) layout.Apply(new Vector2(width, height), safeArea ?? new Rect(0, 0, width, height));
        Canvas.ForceUpdateCanvases();
        if (validateUI) PresentationAudit.CheckTextFits();
        camera.Render();
        uiCamera.Render();
        RenderTexture.active = texture;
        var frame = new Texture2D(width, height, TextureFormat.RGB24, false);
        frame.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        frame.Apply();
        File.WriteAllBytes(Path.Combine(Output, name + ".png"), frame.EncodeToPNG());
        Record("CAPTURE " + name + " colors=" + frame.GetPixels32().Where((p, i) => i % 997 == 0).Distinct().Count());
        for (int i = 0; i < canvases.Length; i++)
        {
            canvases[i].renderMode = modes[i];
            canvases[i].worldCamera = canvasCameras[i];
            canvases[i].scaleFactor = canvasScales[i];
        }
        for (int i = 0; i < nodes.Length; i++) nodes[i].gameObject.layer = layers[i];
        foreach (var layout in safeLayouts) layout.Apply(new Vector2(Screen.width, Screen.height), Screen.safeArea);
        camera.rect = previousRect;
        camera.cullingMask = previousMask;
        camera.targetTexture = previousTarget;
        RenderTexture.active = previousActive;
        texture.Release();
        Object.DestroyImmediate(texture);
        Object.DestroyImmediate(frame);
        Object.DestroyImmediate(uiCamera.gameObject);
    }

    private static void CaptureLog(string condition, string stack, LogType type)
    {
        if (type == LogType.Warning || type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
            evidence.Add("CONSOLE " + type + " " + condition + " " + stack);
        if ((type == LogType.Error || type == LogType.Exception || type == LogType.Assert) && (stack.Contains("Assets/") || stack.Contains("Assembly-CSharp")))
            projectError = true;
    }

    internal static void Record(string value)
    {
        evidence.Add(value);
        Debug.Log("AUDIT " + value);
    }

    private static void Finish(int code)
    {
        if (projectError) code = 1;
        File.WriteAllLines(Path.Combine(Output, "runtime.txt"), evidence);
        SessionState.SetBool(Active, false);
        EditorApplication.update -= Tick;
        Application.logMessageReceived -= CaptureLog;
        Debug.Log("AUDIT_FINISHED code=" + code);
        EditorApplication.Exit(code);
    }
}
