using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

[InitializeOnLoad]
public static class HeroVisualAudit
{
    private const string Active = "MechTrial.HeroAudit";
    private static IEnumerator routine;
    private static int lastFrame, errors, editorSearchErrors;
    private static double deadline;
    private static readonly List<string> report = new List<string>();
    private static RiggedMechAnimator hero;
    private static PlayerController player;
    private static Camera camera;
    private static bool closeup;
    private static Vector3 cameraOffset = new Vector3(4.5f, 3.8f, -7);
    private static Transform shin, thigh, foot;
    private static float idleKneeBend;
    private static string Output => Path.GetFullPath("AuditEvidence/hero");

    static HeroVisualAudit()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(Active, false)) return;
            report.Clear();
            errors = 0;
            editorSearchErrors = 0;
            lastFrame = -1;
            deadline = EditorApplication.timeSinceStartup + 240;
            Application.logMessageReceived += OnLog;
            routine = Scenarios();
            EditorApplication.update += Tick;
        };
    }

    public static void Run()
    {
        Directory.CreateDirectory(Output);
        SessionState.SetBool(Active, true);
        EditorSceneManager.OpenScene("Assets/Scenes/Demo_Main.unity");
        EditorApplication.EnterPlaymode();
    }

    private static void Tick()
    {
        if (!EditorApplication.isPlaying || Time.frameCount == lastFrame) return;
        lastFrame = Time.frameCount;
        try
        {
            if (EditorApplication.timeSinceStartup > deadline) throw new Exception("Hero audit timeout");
            if (closeup && player != null)
            {
                camera.transform.position = player.transform.position + cameraOffset;
                camera.transform.LookAt(player.transform.position + Vector3.up * 1.65f);
            }
            if (!routine.MoveNext()) Finish(null);
        }
        catch (Exception error) { Finish(error.ToString()); }
    }

    private static IEnumerator Scenarios()
    {
        yield return null;
        yield return null;
        Time.captureDeltaTime = 1f / 60;
        var gm = GameManager.Instance;
        player = gm.playerController;
        player.InputRouter.readKeyboard = false;
        typeof(PlayerInputRouter).GetProperty("AllowUnfocusedReplay").SetValue(null, true);
        GamePreferences.SetMaster(0);
        hero = player.GetComponentInChildren<RiggedMechAnimator>();
        Check(hero != null && hero.enabled && hero.animator.isInitialized, "rigged_hero_loaded");
        Check(!player.GetComponent<MechMotionAnimator>().enabled, "legacy_whole_mesh_bob_disabled");
        Check(player.weaponController.muzzle == hero.muzzle && hero.muzzle.IsChildOf(hero.rigidPose != null ? hero.rigidPose.cannon : hero.leftForearm), "cannon_uses_real_bone_socket");
        Check(hero.bladeTip.IsChildOf(hero.rigidPose != null ? hero.rigidPose.Resolve(hero.rightHand) : hero.rightHand), "blade_attached_to_hand");
        var skin = hero.GetComponentInChildren<SkinnedMeshRenderer>();
        if (hero.rigidPose != null)
        {
            var meshes = hero.rigidPose.assemblyRoot.GetComponentsInChildren<MeshFilter>();
            Check(skin == null && meshes.Length == 110 && hero.rigidPose.segments.Length >= 15, "frozen_rigid_armor_and_animation_bindings_present");
            Check(meshes.Sum(m => m.sharedMesh.triangles.Length / 3) == 129824, "frozen_geometry_triangles_preserved");
            Check(hero.rigidPose.thrusters.Length == 2 && !hero.GetComponentsInChildren<Transform>().Any(t => t.name == "ThrusterNozzle"), "vfx_uses_existing_backpack_nozzles");
            hero.rigidPose.SetBeamActive(false);
            Check(!hero.rigidPose.beam.activeInHierarchy && meshes.Where(m => m.name == "LOD0_AntiShipBlade_Weapon").All(m => m.gameObject.activeInHierarchy), "beam_switch_preserves_physical_blade");
            hero.rigidPose.SetBeamActive(true);
            report.Add("RIGID meshes=" + meshes.Length + " materials=" + meshes.SelectMany(m => m.GetComponent<Renderer>().sharedMaterials).Distinct().Count());
        }
        else
        {
            Check(skin.bones.Length >= 40 && skin.sharedMesh.vertexCount > 1000, "skinned_mesh_and_bones_present");
            report.Add("SKIN vertices=" + skin.sharedMesh.vertexCount + " bones=" + skin.bones.Length + " submeshes=" + skin.sharedMesh.subMeshCount);
        }
        camera = Camera.main;
        Capture("01_hangar_game_camera");
        ProjectAudit.Click("DeployButton");
        yield return null;
        gm.stageManager.StopStage();
        foreach (var enemy in Object.FindObjectsByType<EnemyBase>(FindObjectsSortMode.None)) Object.Destroy(enemy.gameObject);
        player.RestoreAt(new Vector3(0, 0, -8));
        player.transform.rotation = Quaternion.identity;
        gm.SetPaused(false);
        player.automaticFire = false;
        yield return null;
        Capture("02_game_camera");
        foreach (var behaviour in camera.GetComponents<MonoBehaviour>()) behaviour.enabled = false;
        camera.fieldOfView = 30;
        closeup = true;
        var idle = Sequence("idle", 4, .23f, .0005f);
        while (idle.MoveNext()) yield return null;
        shin = hero.GetComponentsInChildren<Transform>().Single(t => t.name == "shin.L");
        thigh = hero.GetComponentsInChildren<Transform>().Single(t => t.name == "thigh.L");
        foot = hero.GetComponentsInChildren<Transform>().Single(t => t.name == "foot.L");
        if (hero.rigidPose != null) { shin = hero.rigidPose.Resolve(shin); thigh = hero.rigidPose.Resolve(thigh); foot = hero.rigidPose.Resolve(foot); }
        idleKneeBend = Vector3.Angle(shin.position - thigh.position, foot.position - shin.position);
        cameraOffset.z = 7;
        Capture("idle_front");
        cameraOffset.z = -7;
        player.InputRouter.SetTouchMove(Vector2.up);
        var run = Sequence("run", 6, .11f, .04f);
        while (run.MoveNext()) yield return null;
        Check(player.Velocity.magnitude > 3, "actual_controller_movement");
        player.InputRouter.QueueDash();
        var dash = Sequence("dash", 4, .05f, .04f);
        while (dash.MoveNext()) yield return null;
        player.InputRouter.Clear();
        player.RestoreAt(new Vector3(0, 0, -8));
        player.transform.rotation = Quaternion.identity;
        float settle = Time.time + .3f;
        while (Time.time < settle) yield return null;
        hero.PreviewSlash();
        var slash = Sequence("slash_PREVIEW_NOT_DAMAGE", 7, .07f, .1f);
        while (slash.MoveNext()) yield return null;
        report.Add("LIMITATION: This sequence is visual-only. Playable slash damage is verified separately by ManualCombatAudit.");
        settle = Time.time + .5f;
        while (Time.time < settle) yield return null;
        cameraOffset.x = -4.5f;
        var enemyObject = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemies/Enemy_Melee.prefab"), new Vector3(0, 0, -4.5f), Quaternion.identity);
        enemyObject.GetComponent<EnemyBase>().enabled = false;
        var target = enemyObject.GetComponent<Damageable>();
        target.SetMaxHealth(10000, true);
        int beams = 0, skills = 0, hits = 0;
        player.weaponController.BeamFired += () => beams++;
        player.weaponController.SkillFired += () => skills++;
        target.OnDamaged += (d, info) => hits++;
        player.InputRouter.SetTouchAim(Vector2.up, true);
        var fire = Sequence("cannon", 5, .16f, .02f);
        while (fire.MoveNext()) yield return null;
        player.InputRouter.QueueSkill();
        var skill = Sequence("salvo", 4, .1f, .002f);
        while (skill.MoveNext()) yield return null;
        report.Add("COMBAT beams=" + beams + " hits=" + hits + " skills=" + skills + " targetHP=" + target.CurrentHealth);
        Check(beams >= 3 && hits >= 1 && skills == 1, "real_beam_hits_and_missile_skill");
        player.InputRouter.SetTouchAim(Vector2.zero, false);
        Object.Destroy(enemyObject);
        settle = Time.time + .4f;
        while (Time.time < settle) yield return null;
        player.GetComponent<Damageable>().TakeDamage(2, new DamageInfo(null, player.transform.position + Vector3.forward, null, 2));
        var hit = Sequence("hit", 3, .08f, .001f);
        while (hit.MoveNext()) yield return null;
        Check(hero.muzzle.position.y > .2f && hero.muzzle.position.y < 3.5f, "muzzle_above_ground");
        float feet = SkinPoints().Min(v => v.y) + player.transform.position.y;
        report.Add("GROUND sampledSkinMinY=" + feet + " actorY=" + player.transform.position.y);
        Check(feet > -.06f && feet < .15f, "idle_feet_grounded");
        player.InputRouter.SetTouchMove(Vector2.right);
        settle = Time.time + .3f;
        while (Time.time < settle) yield return null;
        gm.SetPaused(true);
        yield return null;
        Vector3[] paused = SkinPoints();
        double until = EditorApplication.timeSinceStartup + .3;
        while (EditorApplication.timeSinceStartup < until) yield return null;
        Check(paused.Zip(SkinPoints(), Vector3.Distance).Max() < .002f, "pause_does_not_accumulate_bone_offsets");
        gm.SetPaused(false);
        player.InputRouter.Clear();
        player.RestoreAt(new Vector3(0, 0, -8));
        hero.enabled = false;
        Check(player.GetComponent<MechMotionAnimator>().enabled && player.weaponController.muzzle != hero.muzzle, "adapter_disable_restores_fallback_bindings");
        hero.enabled = true;
        yield return null;
        Check(player.weaponController.muzzle == hero.muzzle && !player.GetComponent<MechMotionAnimator>().enabled, "adapter_reenable_rebinds");
        player.GetComponent<Damageable>().TakeDamage(99999, new DamageInfo(null, player.transform.position + Vector3.forward, null, 99999));
        yield return null;
        Check(player.GetComponent<Damageable>().IsDead, "actual_player_death");
        var death = Sequence("death", 4, .18f, .04f, true);
        while (death.MoveNext()) yield return null;
        report.Add("RENDER: Unity Play Mode, actual scene and visible mesh vertices (skinned or rigid armor). Closeups move only the diagnostic camera. Time.captureDeltaTime=1/60; not a performance measurement.");
    }

    private static IEnumerator Sequence(string name, int count, float interval, float minimumMotion, bool unscaled = false)
    {
        Vector3[] initial = null;
        float maximum = 0;
        for (int i = 0; i < count; i++)
        {
            float end = (unscaled ? Time.unscaledTime : Time.time) + interval;
            while ((unscaled ? Time.unscaledTime : Time.time) < end) yield return null;
            Vector3[] pose = SkinPoints();
            if (name == "dash" && i == 1)
            {
                float kneeBend = Vector3.Angle(shin.position - thigh.position, foot.position - shin.position);
                report.Add("KNEE idle=" + idleKneeBend + " dash=" + kneeBend);
                Check(player.IsDashing && kneeBend > idleKneeBend + 12, "dash_has_real_crouched_knee_pose");
            }
            if (initial == null) initial = pose;
            for (int j = 0; j < pose.Length; j++) maximum = Mathf.Max(maximum, Vector3.Distance(initial[j], pose[j]));
            Capture(name + "_" + i.ToString("D2"));
            var state = hero.animator.GetCurrentAnimatorStateInfo(0);
            report.Add(name + " frame=" + Time.frameCount + " time=" + Time.time + " state=" + state.shortNameHash + " normalized=" + state.normalizedTime
                + " hand=" + player.transform.InverseTransformPoint(hero.rightHand.position) + " muzzle=" + player.transform.InverseTransformPoint(hero.muzzle.position));
        }
        report.Add("SKIN_LOCAL_MOTION " + name + " maximum=" + maximum);
        Check(maximum < 4f, "body_deformation_within_scale_" + name);
        Check(maximum >= minimumMotion, "deformed_body_moves_" + name);
    }

    private static Vector3[] SkinPoints()
    {
        if (hero.rigidPose != null)
        {
            var samples = new List<Vector3>();
            foreach (var filter in hero.rigidPose.assemblyRoot.GetComponentsInChildren<MeshFilter>().Where(f => !f.name.Contains("AntiShipBlade")))
            {
                var rigidVertices = filter.sharedMesh.vertices;
                Matrix4x4 transform = hero.transform.worldToLocalMatrix * filter.transform.localToWorldMatrix;
                for (int i = 0; i < rigidVertices.Length; i += 37) samples.Add(transform.MultiplyPoint3x4(rigidVertices[i]));
            }
            return samples.ToArray();
        }
        var skin = hero.GetComponentInChildren<SkinnedMeshRenderer>();
        var mesh = new Mesh();
        skin.BakeMesh(mesh);
        var vertices = mesh.vertices;
        var points = new List<Vector3>();
        Matrix4x4 local = hero.transform.worldToLocalMatrix * skin.transform.localToWorldMatrix;
        for (int i = 0; i < vertices.Length; i += 37) points.Add(local.MultiplyPoint3x4(vertices[i]));
        Object.DestroyImmediate(mesh);
        return points.ToArray();
    }

    private static void Capture(string name)
    {
        if (closeup)
        {
            camera.transform.position = player.transform.position + cameraOffset;
            camera.transform.LookAt(player.transform.position + Vector3.up * 1.65f);
            camera.fieldOfView = 30;
            camera.orthographicSize = 2.6f;
        }
        var canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).Where(c => c.enabled && c.renderMode != RenderMode.WorldSpace).ToArray();
        bool showBody = name.StartsWith("death", StringComparison.Ordinal);
        try
        {
            if (showBody) foreach (var canvas in canvases) canvas.enabled = false;
            ProjectAudit.Capture("hero/" + name, 1280, 720);
        }
        finally { if (showBody) foreach (var canvas in canvases) canvas.enabled = true; }
    }
    private static void Check(bool value, string name)
    {
        if (!value) throw new Exception("HERO_FAIL " + name);
        report.Add("PASS " + name);
    }

    private static void OnLog(string message, string stack, LogType type)
    {
        if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
        {
            if (stack.Contains("UnityEditor.Search.SearchDatabase"))
            {
                editorSearchErrors++;
                report.Add("EXISTING_EDITOR_SEARCH_ERROR " + message);
                return;
            }
            errors++;
            report.Add("ERROR " + message + "\n" + stack);
        }
    }

    private static void Finish(string failure)
    {
        Time.captureDeltaTime = 0;
        if (failure != null) report.Add(failure);
        int code = failure == null && errors == 0 ? 0 : 1;
        report.Add("HERO_AUDIT_FINISHED code=" + code + " runtimeErrors=" + errors + " existingEditorSearchErrors=" + editorSearchErrors);
        File.WriteAllLines(Path.Combine(Output, "report.txt"), report);
        SessionState.SetBool(Active, false);
        EditorApplication.update -= Tick;
        Application.logMessageReceived -= OnLog;
        Debug.Log(report.Last());
        EditorApplication.Exit(code);
    }
}
