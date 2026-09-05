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
                deadline = EditorApplication.timeSinceStartup + 300;
                lastFrame = -1;
                routine = SessionState.GetBool(Active + ".Feedback", false) ? FeedbackScenarios() : SessionState.GetBool(Active + ".Mobile", false) ? MobileScenarios() : SessionState.GetBool(Active + ".Shots", false) ? ShotDiagnostics() : RunScenarios();
                Application.logMessageReceived += CaptureLog;
                EditorApplication.update += Tick;
            }
        };
    }

    public static void Run()
    {
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
        SessionState.SetBool(Active + ".Feedback", false);
        Directory.CreateDirectory(Output);
        SessionState.SetBool(Active + ".Mobile", true);
        SessionState.SetBool(Active, true);
        EditorSceneManager.OpenScene("Assets/Scenes/Demo_Main.unity");
        EditorApplication.EnterPlaymode();
    }

    public static void RunFeedbackTests()
    {
        Directory.CreateDirectory(Output);
        SessionState.SetBool(Active + ".Feedback", true);
        SessionState.SetBool(Active, true);
        EditorSceneManager.OpenScene("Assets/Scenes/Demo_Main.unity");
        EditorApplication.EnterPlaymode();
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
        Check(GameAudio.Instance.LoadedCueCount == 10, "audio_all_cues_loaded");
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
        gm = GameManager.Instance;
        gm.playerController.InputRouter.readKeyboard = false;
        Click("DeployButton");
        float started = Time.time;
        int choices = 0;
        bool bossSeen = false;
        bool captured = false;
        bool pressureCaptured = false;
        while (gm.Phase != GamePhase.Result && Time.time - started < 150f)
        {
            if (gm.Phase == GamePhase.Reward)
            {
                Capture("mobile_05_reward", 2400, 1080, new Rect(90, 35, 2220, 1045));
                Click("ChooseButton");
                choices++;
            }
            if (!captured && Time.time - started > 3f) { Capture("mobile_03_auto_combat"); captured = true; }
            if (!pressureCaptured && Time.time - started > 45f) { Capture("feedback_03_pressure"); pressureCaptured = true; }
            var bossActor = Object.FindFirstObjectByType<BossController>();
            if (bossActor != null && !bossSeen)
            {
                float cameraSettle = Time.time + 0.7f;
                while (Time.time < cameraSettle) yield return null;
                Vector3 bossPoint = Camera.main.WorldToViewportPoint(bossActor.GetComponent<Damageable>().AimCenter);
                Check(bossPoint.y > 0.12f && bossPoint.y < 0.85f && bossPoint.x > 0.1f && bossPoint.x < 0.9f, "boss_camera_framing");
                Capture("mobile_04_boss");
                bossSeen = true;
            }
            yield return null;
        }
        Record("MOBILE_LIVE_FLOW phase=" + gm.Phase + " hp=" + gm.playerStats.CurrentHp + " kills=" + gm.Kills + " choices=" + choices + " boss=" + bossSeen + " seconds=" + (Time.time - started));
        Record("FIRST_ENCOUNTER_SECONDS " + gm.stageManager.FirstEncounterSeconds);
        Check(gm.Phase == GamePhase.Result && gm.playerStats.CurrentHp > 0 && choices == 1 && bossSeen, "automatic_fire_full_loop");
        Capture("mobile_06_result", 1280, 720);
        Click("ReturnHangarButton");
        yield return null;
        yield return null;
        Check(GameManager.Instance.Phase == GamePhase.Hangar, "mobile_restart");
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

    private static void Check(bool valid, string name)
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
        yield return null;
        var gm = GameManager.Instance;
        Record("START hangar=" + gm.hangarUI.IsVisible + " hud=" + gm.combatHUD.IsVisible + " equipment=" + gm.equipmentManager.GetEquippedItems().Count);
        Capture("01_hangar");
        Click("DeployButton");
        gm.playerController.enabled = false;
        gm.SetPaused(true);
        VerifyBlockedCombat(gm);
        gm.SetPaused(false);
        Record("PAUSE_RESUME active=" + gm.IsCombatActive);
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
                if (!phaseTwoSeen && boss.IsPhaseTwo) Capture("04b_boss_phase2");
                phaseTwoSeen |= boss.IsPhaseTwo;
            }
            if (gm.Phase == GamePhase.Reward)
            {
                yield return null;
                Capture("03_reward");
                VerifyBlockedCombat(gm);
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
        bool liveVictory = gm.Phase == GamePhase.Result && gm.playerStats.CurrentHp > 0 && rewardChoices == 1 && bossSeen && phaseTwoSeen;
        VerifyBlockedCombat(gm);
        frameTimes.Sort();
        if (frameTimes.Count > 0) Record("FRAME_OBSERVATION samples=" + frameTimes.Count + " avg_ms=" + frameTimes.Average() * 1000 + " p95_ms=" + frameTimes[(int)(frameTimes.Count * 0.95f)] * 1000 + " note=editor+audit_logic+uncapped_not_benchmark");
        Click("ReturnHangarButton");
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
        VerifyBlockedCombat(gm);
        float hpBefore = player.CurrentHealth;
        float until = Time.time + 1;
        while (Time.time < until) yield return null;
        Record("REWARD_PROTECTION hp_before=" + hpBefore + " hp_after=" + player.CurrentHealth + " phase=" + gm.Phase);
        if (player.CurrentHealth != hpBefore) throw new Exception("Reward did not protect HP.");
        Capture("06_reward_damage");
        gm.RestartRun();
        yield return null;
        yield return null;
        gm = GameManager.Instance;
        Click("DeployButton");
        player = gm.playerStats.GetComponent<Damageable>();
        player.TakeDamage(9999, new DamageInfo(null, player.transform.position, null, 9999));
        int before = Object.FindObjectsByType<EnemyBase>(FindObjectsSortMode.None).Length;
        int killsAtEnd = gm.Kills;
        int coinsAtEnd = gm.Coins;
        gm.RegisterKill(100);
        gm.EnterResult(true);
        VerifyBlockedCombat(gm);
        until = Time.time + 3;
        while (Time.time < until) yield return null;
        Record("DEFEAT phase=" + gm.Phase + " hp=" + gm.playerStats.CurrentHp + " enemies_before=" + before + " enemies_after=" + Object.FindObjectsByType<EnemyBase>(FindObjectsSortMode.None).Length + " hud=" + gm.combatHUD.IsVisible + " result_buttons=" + Object.FindObjectsByType<Button>(FindObjectsSortMode.None).Length);
        if (Object.FindObjectsByType<EnemyBase>(FindObjectsSortMode.None).Length != 0 || gm.combatHUD.IsVisible || gm.Kills != killsAtEnd || gm.Coins != coinsAtEnd)
            throw new Exception("Result did not stop combat or freeze score.");
        Capture("07_defeat");
        Capture("07b_defeat_1280", 1280, 720);
        Click("ReturnHangarButton");
        yield return null;
        yield return null;
        Record("DEFEAT_RESTART hangar=" + GameManager.Instance.hangarUI.IsVisible);
        if (!liveVictory) throw new Exception("Natural live-fire run did not reach victory with both Boss phases.");
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

    private static void Click(string name)
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

    private static void Capture(string name, int width = 1920, int height = 1080, Rect? safeArea = null)
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

    private static void Record(string value)
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
