using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using Object = UnityEngine.Object;

[InitializeOnLoad]
public static class IndustrialPolishAudit
{
    private const string Active = "MechTrial.IndustrialAudit";
    private static IEnumerator routine;
    private static int lastFrame, errors, editorErrors;
    private static double deadline;
    private static readonly List<string> report = new List<string>();
    static IndustrialPolishAudit()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(Active, false)) return;
            lastFrame = -1; errors = editorErrors = 0;
            report.Clear();
            deadline = EditorApplication.timeSinceStartup + 180;
            Application.logMessageReceived += OnLog;
            routine = Scenarios();
            EditorApplication.update += Tick;
        };
    }
    public static void Run()
    {
        Directory.CreateDirectory("AuditEvidence/industrial");
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
            if (EditorApplication.timeSinceStartup > deadline) throw new Exception("Polish audit timed out");
            if (!routine.MoveNext()) Finish(null);
        }
        catch (Exception ex) { Finish(ex.ToString()); }
    }
    private static IEnumerator Scenarios()
    {
        yield return null; yield return null;
        GamePreferences.SetMaster(0);
        for (int quality = 0; quality <= 1; quality++)
        {
            GamePreferences.SetQuality(quality);
            Check(Application.targetFrameRate == -1 && QualitySettings.vSyncCount == 0, "desktop_uncapped_quality_" + quality);
        }
        Time.captureDeltaTime = 1f / 120;
        var gm = GameManager.Instance;
        var p = gm.playerController;
        p.InputRouter.readKeyboard = false;
        typeof(PlayerInputRouter).GetProperty("AllowUnfocusedReplay").SetValue(null, true);
        ProjectAudit.Click("DeployButton");
        yield return null;
        gm.stageManager.StopStage();
        foreach (var enemy in Object.FindObjectsByType<EnemyBase>(FindObjectsSortMode.None)) Object.Destroy(enemy.gameObject);
        p.automaticFire = false;
        var camera = Camera.main;
        for (int i = 0; i < 45; i++) yield return null;
        ProjectAudit.Capture("industrial/01_gameplay", 1920, 1080);
        var follow = camera.GetComponent<CameraFollow>();
        follow.enabled = false;
        camera.transform.SetPositionAndRotation(new Vector3(0, 38, -22), Quaternion.Euler(60, 0, 0));
        camera.orthographicSize = 28;
        ProjectAudit.Capture("industrial/02_layout", 1920, 1080);
        camera.transform.position = new Vector3(-17, 12, -22);
        camera.transform.LookAt(new Vector3(-18, 1, -12));
        camera.orthographicSize = 7;
        ProjectAudit.Capture("industrial/03_building", 1920, 1080);
        var sector = Object.FindFirstObjectByType<ArenaSector>();
        Check(sector.GetComponentsInChildren<MeshRenderer>().Count(r => r.name == "ServiceBuilding") == 4, "four_active_buildings");
        foreach (var renderer in sector.GetComponentsInChildren<MeshRenderer>().Where(r => r.name == "ServiceBuilding"))
            Check(renderer.bounds.size.y > 4.5f && renderer.bounds.min.y > -.1f, "building_geometry_upright_and_grounded_" + renderer.transform.position);
        foreach (var renderer in sector.GetComponentsInChildren<MeshRenderer>().Where(r => r.name == "DrainGrate"))
            Check(renderer.bounds.size.y < .15f, "drain_geometry_flat_" + renderer.transform.position);
        for (int stage = 1; stage <= 2; stage++)
        {
            sector.ShowSector(stage);
            yield return null;
            foreach (var entry in new[] { new Vector3(-21,0,0), new Vector3(21,0,0), new Vector3(0,0,-21), new Vector3(0,0,21) })
            {
                Check(NavMesh.SamplePosition(entry, out var point, 2, NavMesh.AllAreas), "entry_on_nav_" + stage + "_" + entry);
                var path = new NavMeshPath();
                Check(NavMesh.CalculatePath(point.position, Vector3.zero, NavMesh.AllAreas, path) && path.status == NavMeshPathStatus.PathComplete,
                    "entry_reaches_center_" + stage + "_" + entry);
            }
        }
        camera.transform.SetPositionAndRotation(new Vector3(0, 38, -22), Quaternion.Euler(60, 0, 0));
        camera.orthographicSize = 28;
        ProjectAudit.Capture("industrial/04_reactor_layout", 1920, 1080);
        sector.ShowSector(1);
        p.RestoreAt(new Vector3(0, 0, -4));
        yield return null;
        Check(Physics.Linecast(new Vector3(6.8f, 1, -8.2f), new Vector3(12, 1, -8.2f), out var hit) && hit.collider.name == "CoolantPump", "pump_blocks_fire");
        var around = new NavMeshPath();
        Check(NavMesh.CalculatePath(new Vector3(6.8f, 0, -8.2f), new Vector3(12, 0, -8.2f), NavMesh.AllAreas, around)
            && around.status == NavMeshPathStatus.PathComplete && around.corners.Length >= 3, "pump_has_route_around");
        p.RestoreAt(new Vector3(6.8f, 0, -8.2f));
        Check(p.TryDash(Vector2.right), "dash_toward_cover_accepted");
        for (int i = 0; i < 40; i++) yield return null;
        Check(p.transform.position.x < 7.4f && p.transform.position.x > 6.8f, "dash_stops_before_solid_pump");
        p.RestoreAt(new Vector3(0, 0, -4));
        for (int i = 0; i < 50; i++) yield return null;
        camera.transform.position = new Vector3(5, 4, -9);
        camera.transform.LookAt(p.transform.position + Vector3.up * 1.5f);
        camera.orthographicSize = 4.3f;
        ProjectAudit.Capture("industrial/dash_00_ready", 1280, 720);
        var rig = p.GetComponentInChildren<RiggedMechAnimator>();
        var feedback = p.GetComponent<MechDashPresentation>();
        Check(feedback != null, "dash_presentation_attached");
        Vector3 chestBefore = p.transform.InverseTransformPoint(rig.chest.position);
        Quaternion hipsBefore = rig.hips.rotation;
        Check(p.TryDash(Vector2.up), "open_dash_accepted");
        float maximumPulse = 0, maximumAngle = 0;
        int trails = 0;
        for (int i = 0; i < 48; i++)
        {
            yield return null;
            maximumPulse = Mathf.Max(maximumPulse, feedback.Pulse);
            maximumAngle = Mathf.Max(maximumAngle, Quaternion.Angle(hipsBefore, rig.hips.rotation));
            trails = Mathf.Max(trails, feedback.ActiveTrailCount);
            if (i == 3 || i == 9 || i == 18 || i == 32 || i == 47)
            {
                camera.transform.position = p.transform.position + new Vector3(5, 4, -5);
                camera.transform.LookAt(p.transform.position + Vector3.up * 1.5f);
                ProjectAudit.Capture("industrial/dash_" + i.ToString("00"), 1280, 720);
                report.Add("DASH_SAMPLE " + i + " pulse=" + feedback.Pulse + " chestLocal=" + p.transform.InverseTransformPoint(rig.chest.position));
            }
        }
        Check(maximumPulse > .95f && maximumAngle > 20 && trails == 2, "body_action_and_twin_boost_wakes");
        Check(Mathf.Abs(p.transform.position.z - 1) < .15f, "dash_distance_unchanged_5m");
        Check(feedback.Pulse < .01f, "dash_recovers_without_stuck_pose");
        Check(p.TryDash(Vector2.up) == false, "cooldown_preserved");
        gm.SetPaused(true);
        Quaternion pausedHips = rig.hips.rotation;
        for (int i = 0; i < 12; i++) yield return null;
        Check(Quaternion.Angle(pausedHips, rig.hips.rotation) < .01f, "paused_bones_do_not_accumulate_offsets");
        gm.SetPaused(false);
        foreach (int rate in new[] { 30, 60, 240 })
        {
            Time.captureDeltaTime = 1f / rate;
            p.stats.ResetStats();
            p.RestoreAt(new Vector3(0, 0, -4));
            yield return null;
            Check(p.TryDash(Vector2.up), "dash_at_rate_" + rate);
            for (int i = 0; i < Mathf.CeilToInt(rate * .3f); i++) yield return null;
            Check(Mathf.Abs(p.transform.position.z - 1) < .15f, "same_5m_at_rate_" + rate);
        }
        report.Add("METHOD: actual Unity Play Mode; 1/120 fixed pose capture, not FPS evidence. Full uncapped Player replay is separate.");
    }
    private static void Check(bool ok, string name) { if (!ok) throw new Exception("POLISH_FAIL " + name); report.Add("PASS " + name); }
    private static void OnLog(string message, string stack, LogType type)
    {
        if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert) return;
        if (stack.Contains("UnityEditor.Search.SearchDatabase")) { editorErrors++; return; }
        errors++; report.Add(message + "\n" + stack);
    }
    private static void Finish(string failure)
    {
        Time.captureDeltaTime = 0;
        if (failure != null) report.Add(failure);
        int code = failure == null && errors == 0 ? 0 : 1;
        report.Add("INDUSTRIAL_AUDIT code=" + code + " runtimeErrors=" + errors + " knownEditorErrors=" + editorErrors);
        File.WriteAllLines("AuditEvidence/industrial/report.txt", report);
        SessionState.SetBool(Active, false);
        EditorApplication.update -= Tick;
        Application.logMessageReceived -= OnLog;
        Debug.Log(report.Last());
        EditorApplication.Exit(code);
    }
}
