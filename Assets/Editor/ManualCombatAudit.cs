using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using Object = UnityEngine.Object;

[InitializeOnLoad]
public static class ManualCombatAudit
{
    private const string Active = "MechTrial.ManualCombatAudit";
    private static IEnumerator routine;
    private static int lastFrame, errors, warnings, editorErrors;
    private static double deadline;
    private static readonly List<string> report = new List<string>();
    static ManualCombatAudit()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(Active, false)) return;
            lastFrame = -1; errors = warnings = editorErrors = 0;
            report.Clear(); deadline = EditorApplication.timeSinceStartup + 180;
            Application.logMessageReceived += OnLog;
            routine = Scenarios(); EditorApplication.update += Tick;
        };
    }
    public static void Run()
    {
        Directory.CreateDirectory("AuditEvidence/manual");
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
            if (EditorApplication.timeSinceStartup > deadline) throw new Exception("Manual audit timeout");
            if (!routine.MoveNext()) Finish(null);
        }
        catch (Exception ex) { Finish(ex.ToString()); }
    }
    private static IEnumerator Scenarios()
    {
        yield return null; yield return null;
        GamePreferences.SetMaster(0);
        Time.captureDeltaTime = 1f / 60;
        typeof(PlayerInputRouter).GetProperty("AllowUnfocusedReplay").SetValue(null, true);
        var gm = GameManager.Instance;
        var p = gm.playerController;
        p.InputRouter.readKeyboard = false;
        ProjectAudit.Click("DeployButton");
        yield return null;
        gm.stageManager.StopStage();
        foreach (var enemy in Object.FindObjectsByType<EnemyBase>(FindObjectsSortMode.None)) Object.Destroy(enemy.gameObject);
        p.RestoreAt(new Vector3(0, 0, -4));
        yield return null;
        var front = Target(new Vector3(0, 0, -1.4f));
        var back = Target(new Vector3(0, 0, -6.6f));
        var far = Target(new Vector3(0, 0, 3));
        var ally = Target(new Vector3(2, 0, -2));
        ally.team = p.GetComponent<Damageable>().team;
        int beams = 0, frontHits = 0, backHits = 0;
        p.weaponController.BeamFired += () => beams++;
        front.OnDamaged += (d, hit) => frontHits++;
        back.OnDamaged += (d, hit) => backHits++;
        for (int i = 0; i < 60; i++) yield return null;
        Check(beams == 0 && frontHits == 0, "no_automatic_fire");
        p.InputRouter.SetTouchAim(Vector2.up, true);
        for (int i = 0; i < 60; i++) yield return null;
        report.Add("FIRE beams=" + beams + " front=" + frontHits + " back=" + backHits + " phase=" + gm.Phase + " aim=" + p.AimPoint);
        ProjectAudit.Capture("manual/fire_debug", 1920, 1080);
        Check(beams >= 4 && frontHits > 0 && backHits == 0, "held_manual_direction_hits_only_front");
        p.InputRouter.SetTouchAim(Vector2.zero, false);
        int stopped = beams;
        for (int i = 0; i < 35; i++) yield return null;
        Check(beams == stopped, "release_stops_fire");
        var controls = p.GetComponent<MobileControls>();
        var stick = controls.AimJoystick;
        Canvas.ForceUpdateCanvases();
        var point = RectTransformUtility.WorldToScreenPoint(null, stick.transform.position);
        var pointer = new PointerEventData(EventSystem.current) { pointerId = 7, position = point + Vector2.right * 80 };
        stick.OnPointerDown(pointer);
        controls.Joystick.input.SetTouchMove(Vector2.up);
        var command = p.InputRouter.ReadCommand();
        Check(command.Fire && command.Move.y > .9f && command.AimPoint.x > p.transform.position.x + 10, "independent_move_and_aim");
        stick.OnPointerUp(new PointerEventData(EventSystem.current) { pointerId = 8 });
        Check(stick.PointerId == 7, "second_pointer_cannot_release_aim");
        stick.OnPointerUp(pointer);
        p.InputRouter.Clear();
        Check(!p.InputRouter.ReadCommand().Fire, "pointer_release_clears_fire");
        var camera = Camera.main;
        for (int i = 0; i < 12; i++) yield return null;
        ProjectAudit.Capture("manual/01_gameplay", 1920, 1080);
        ProjectAudit.Capture("manual/02_wide_mobile", 2400, 1080);
        camera.GetComponent<CameraFollow>().enabled = false;
        camera.transform.position = new Vector3(5, 4, -10);
        camera.transform.LookAt(p.transform.position + Vector3.up * 1.3f);
        camera.orthographicSize = 4.7f;
        p.AimAt(front.AimCenter);
        int beforeHits = frontHits;
        float beforeHealth = front.CurrentHealth;
        float farHealth = far.CurrentHealth, allyHealth = ally.CurrentHealth;
        var extraCollider = new GameObject("ExtraCollider", typeof(BoxCollider));
        extraCollider.transform.SetParent(front.transform, false);
        extraCollider.transform.localPosition = Vector3.up;
        var rig = p.GetComponentInChildren<RiggedMechAnimator>();
        Quaternion startHand = rig.rightArm.localRotation;
        float maxAngle = 0;
        GameObject.Find("MeleeButton").GetComponent<MobileActionButton>().OnPointerDown(new PointerEventData(EventSystem.current));
        for (int i = 0; i < 38; i++)
        {
            yield return null;
            maxAngle = Mathf.Max(maxAngle, Quaternion.Angle(startHand, rig.rightArm.localRotation));
            if (i == 3) Check(frontHits == beforeHits, "slash_has_windup");
            if (i % 5 == 0) ProjectAudit.Capture("manual/slash_" + i.ToString("D2"), 1280, 720);
        }
        Check(frontHits == beforeHits + 1 && front.CurrentHealth < beforeHealth && backHits == 0, "real_slash_front_damage_once_no_rear_hit");
        Check(far.CurrentHealth == farHealth && ally.CurrentHealth == allyHealth, "slash_excludes_distant_and_friendly_actors");
        Check(maxAngle > 15, "actual_arm_animation_deforms_" + maxAngle);
        Check(beams == stopped, "slash_not_disguised_shooting");
        for (int i = 0; i < 10; i++) yield return null;
        p.Melee.TryAttack();
        Check(p.TryDash(Vector2.right), "dash_cancels_slash");
        Check(!p.Melee.IsAttacking, "no_pending_slash_after_dash");
        for (int i = 0; i < 45; i++) yield return null;
        p.RestoreAt(new Vector3(0, 0, -4));
        p.AimAt(front.AimCenter);
        var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.transform.position = new Vector3(0, 1.1f, -2.8f);
        wall.transform.localScale = new Vector3(2, 2.2f, .2f);
        yield return null;
        beforeHits = frontHits;
        p.Melee.TryAttack();
        for (int i = 0; i < 45; i++) yield return null;
        Check(frontHits == beforeHits, "solid_wall_blocks_slash");
        Object.Destroy(wall);
        yield return null;
        p.Melee.TryAttack();
        gm.SetPaused(true);
        for (int i = 0; i < 6; i++) yield return null;
        Check(!p.Melee.IsAttacking && !p.InputRouter.ReadCommand().Fire, "pause_clears_attack_and_fire");
        gm.SetPaused(false);
        for (int i = 0; i < 40; i++) yield return null;
        Check(frontHits == beforeHits, "resume_has_no_delayed_slash");
        int skills = 0;
        p.weaponController.SkillFired += () => skills++;
        p.InputRouter.QueueSkill();
        for (int i = 0; i < 15; i++) yield return null;
        Check(skills == 1, "salvo_still_connected");
        Check(Application.targetFrameRate == -1 && QualitySettings.vSyncCount == 0, "desktop_still_uncapped");
        Check(GameAudio.Instance.LoadedCueCount == Enum.GetValues(typeof(GameAudioCue)).Length, "all_audio_cues_including_slash_loaded");
    }
    private static Damageable Target(Vector3 position)
    {
        var go = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemies/Enemy_Melee.prefab"), position, Quaternion.identity);
        go.GetComponent<EnemyBase>().enabled = false;
        var health = go.GetComponent<Damageable>(); health.SetMaxHealth(10000, true); return health;
    }
    private static void Check(bool ok, string name) { report.Add((ok ? "PASS " : "FAIL ") + name); if (!ok) throw new Exception(name); }
    private static void OnLog(string message, string stack, LogType type)
    {
        if (type == LogType.Warning) warnings++;
        if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert) return;
        if (stack.Contains("UnityEditor.Search.SearchDatabase")) { editorErrors++; return; }
        errors++; report.Add(message + "\n" + stack);
    }
    private static void Finish(string failure)
    {
        Time.captureDeltaTime = 0;
        if (failure != null) report.Add(failure);
        int code = failure == null && errors == 0 ? 0 : 1;
        report.Add("MANUAL_AUDIT code=" + code + " errors=" + errors + " warnings=" + warnings + " knownEditorErrors=" + editorErrors);
        File.WriteAllLines("AuditEvidence/manual/report.txt", report);
        SessionState.SetBool(Active, false);
        EditorApplication.update -= Tick;
        Application.logMessageReceived -= OnLog;
        Debug.Log(report.Last()); EditorApplication.Exit(code);
    }
}
