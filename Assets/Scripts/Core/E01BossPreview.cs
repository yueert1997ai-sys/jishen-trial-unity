using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

// Explicit PC launch switches only. The normal six-encounter route remains the default.
public class E01BossPreview : MonoBehaviour
{
    bool smoke;
    string output;
    string runtimeError;
    [Serializable] class Report
    {
        public bool passed, phaseTwo, coreOpened, pauseFreezesPose, hitFeedback, victory, restart, lodSwitch;
        public int patternsSeen, actionsCompleted, skeletonTransforms;
        public float travelledMeters, tendrilRotationDegrees, coreTravelMeters, averageFrameMs;
        public string version, graphicsDevice, error;
    }
    Report report = new Report();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Launch()
    {
        var args = Environment.GetCommandLineArgs();
        if (!args.Any(a => a == "-e01BossPreview" || a == "-e01BossSmoke")) return;
        var runner = new GameObject("E01 Boss Review Launcher").AddComponent<E01BossPreview>();
        runner.smoke = args.Contains("-e01BossSmoke");
    }
    void OnEnable() { Application.logMessageReceived += Log; }
    void OnDisable() { Application.logMessageReceived -= Log; }
    void Log(string message,string trace,LogType type)
    {
        if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) runtimeError = message + "\n" + trace;
    }
    IEnumerator Start()
    {
        output = Path.GetFullPath(Path.Combine(Application.dataPath,"..","E01_Boss_Evidence"));
        var args = Environment.GetCommandLineArgs();
        int arg = Array.IndexOf(args,"-e01Evidence");
        if (arg >= 0 && arg + 1 < args.Length) output = Path.GetFullPath(args[arg+1]);
        if (smoke) Directory.CreateDirectory(output);
        yield return null;
        var gm = GameManager.Instance;
        if (gm == null) { Fail("GameManager not loaded."); yield break; }
        if (gm.Phase != GamePhase.Hangar) { Fail("Expected the normal hangar startup."); yield break; }
        gm.BeginRun();
        // Use the game's own reward and encounter APIs, so the Boss checkpoint and victory gate are valid.
        for (int i = 0; i < 6; i++)
        {
            gm.stageManager.StopStage();
            gm.OnEncounterCleared(i);
            if (gm.Phase != GamePhase.Reward) { Fail("Could not reach reward " + i); yield break; }
            gm.upgradeSystem.ApplyOption(gm.upgradeSystem.GenerateOptions()[0]);
            gm.FinishReward();
            yield return null;
        }
        Debug.Log("E01_BOSS_PREVIEW_STARTED: standard Boss encounter, six upgrades, player controls enabled.");
        if (!smoke) { Destroy(gameObject); yield break; }
        var player = gm.playerController.GetComponent<Damageable>();
        player.SetInvulnerable(180);
        BossController boss = null;
        float deadline = Time.realtimeSinceStartup + 20;
        while (boss == null && Time.realtimeSinceStartup < deadline) { boss = FindFirstObjectByType<BossController>(); yield return null; }
        if (boss == null) { Fail("Boss did not spawn."); yield break; }
        var pose = boss.GetComponent<E01ElitePoseDriver>();
        var hp = boss.GetComponent<Damageable>();
        if (pose == null || pose.BoundBones < 86 || pose.surfaces.Length != 2) { Fail("Frozen Boss rig is missing."); yield break; }
        var all = pose.rigRoot.GetComponentsInChildren<Transform>();
        var tendril = all.Single(t=>t.name == "Tendril.02.03");
        var iris = all.Single(t=>t.name == "Iris.01");
        Quaternion tendrilStart=tendril.localRotation;
        Vector3 irisStart=iris.localPosition;
        Vector3 position=boss.transform.position;
        report.version=Application.version; report.graphicsDevice=SystemInfo.graphicsDeviceName;
        report.skeletonTransforms=pose.BoundBones;
        var lod=pose.rigRoot.GetComponent<LODGroup>();
        yield return new WaitForSeconds(.65f);
        var nearProbe=pose.surfaces.Single(s=>s.name=="E01Elite_LOD0").gameObject.AddComponent<E01LodRenderProbe>();
        var distantProbe=pose.surfaces.Single(s=>s.name=="E01Elite_LOD1").gameObject.AddComponent<E01LodRenderProbe>();
        lod.ForceLOD(1); CaptureClose(boss,"lod1_actual.png");
        bool distantVisible=distantProbe.draws>0 && nearProbe.draws==0;
        nearProbe.draws=distantProbe.draws=0;
        lod.ForceLOD(0); CaptureClose(boss,"lod0_actual.png");
        report.lodSwitch=distantVisible && nearProbe.draws>0 && distantProbe.draws==0;
        Debug.Log("E01_LOD_CHECK distant="+distantVisible+" nearDraws="+nearProbe.draws+" distantDraws="+distantProbe.draws);
        Destroy(nearProbe);Destroy(distantProbe);
        lod.ForceLOD(-1);
        bool hero=false, core=false, phaseShot=false, pause=false, phaseTriggered=false;
        float started=Time.realtimeSinceStartup, milliseconds=0; int frames=0;
        while (Time.realtimeSinceStartup-started<80)
        {
            if (runtimeError != null) { Fail(runtimeError); yield break; }
            if (boss == null || hp.IsDead || gm.Phase != GamePhase.Combat) { Fail("Boss encounter ended before audit completed."); yield break; }
            float dt=Time.unscaledDeltaTime;
            if (dt<.2f) { milliseconds+=dt*1000;frames++; }
            report.travelledMeters+=Vector3.Distance(position,boss.transform.position); position=boss.transform.position;
            report.tendrilRotationDegrees=Mathf.Max(report.tendrilRotationDegrees,Quaternion.Angle(tendrilStart,tendril.localRotation));
            if (boss.ActionRunning) report.patternsSeen |= 1 << (int)boss.CurrentPattern;
            report.coreOpened |= boss.CoreExposed && pose.CoreOpening>.85f && Mathf.Approximately(hp.IncomingDamageScale,1);
            report.phaseTwo |= boss.IsPhaseTwo && pose.PhaseBlend>.9f;
            report.actionsCompleted=boss.ActionsCompleted;
            if (!hero && Time.realtimeSinceStartup-started>1.2f)
            {
                CaptureScreen("game_phase1.png"); CaptureClose(boss,"boss_phase1_close.png"); hero=true;
                float before=hp.CurrentHealth;
                hp.TakeDamage(50,new DamageInfo(player.gameObject,player.transform.position,player,50));
                report.hitFeedback=hp.CurrentHealth<before && !hp.IsDead;
            }
            if (!pause && boss.AttackWindup>.3f)
            {
                float wind=boss.AttackWindup; Quaternion rotation=tendril.localRotation;
                gm.SetPaused(true); yield return new WaitForSecondsRealtime(.25f);
                var after=tendril.localRotation;
                float difference=new Vector4(rotation.x-after.x,rotation.y-after.y,rotation.z-after.z,rotation.w-after.w).sqrMagnitude;
                report.pauseFreezesPose=Mathf.Approximately(wind,boss.AttackWindup) && difference<1e-10f;
                gm.SetPaused(false); pause=true;
            }
            if (!core && report.coreOpened)
            {
                report.coreTravelMeters=iris.parent.TransformVector(iris.localPosition-irisStart).magnitude;
                CaptureScreen("game_core_open.png"); core=true;
            }
            if (!phaseTriggered && boss.ActionsCompleted>=4)
            { hp.SetCurrentHealth(hp.maxHealth*.40f); phaseTriggered=true; }
            if (report.phaseTwo && !phaseShot)
            { CaptureScreen("game_phase2.png");CaptureClose(boss,"boss_phase2_close.png");phaseShot=true; }
            if (phaseShot && report.patternsSeen==15 && report.coreOpened && boss.ActionsCompleted>=6) break;
            yield return null;
        }
        report.averageFrameMs=frames>0 ? milliseconds/frames : 0;
        if (!report.phaseTwo || !report.coreOpened || report.patternsSeen!=15 || !report.pauseFreezesPose || !report.lodSwitch || report.travelledMeters<.2f || report.tendrilRotationDegrees<2)
        { Fail("Boss state or deformation audit incomplete."); yield break; }
        hp.Kill(new DamageInfo(player.gameObject,player.transform.position,player,99999));
        yield return new WaitForSeconds(.5f);
        CaptureScreen("game_defeat.png");
        yield return new WaitForSeconds(1.4f);
        report.victory=gm.Phase==GamePhase.Result && gm.LastResultVictory;
        CaptureScreen("game_victory.png");
        yield return new WaitForSecondsRealtime(.3f);
        if (!report.victory || runtimeError!=null) { Fail(runtimeError ?? "Boss death did not complete the normal mission."); yield break; }
        Save();
        // RuntimeInitializeOnLoadMethod runs once per process, not on every scene restart.
        DontDestroyOnLoad(gameObject);
        gm.RestartRun();
        yield return null; yield return null;
        gm=GameManager.Instance;
        report.restart=gm!=null && gm.Phase==GamePhase.Hangar && gm.CompletedEncounters==0 && FindFirstObjectByType<BossController>()==null;
        report.passed=report.victory && report.restart && runtimeError==null;
        report.error=runtimeError;
        Save();
        Debug.Log(report.passed ? "E01_BOSS_STANDALONE_PASS" : "E01_BOSS_RESTART_FAILED");
        Application.Quit(report.passed ? 0 : 1);
    }
    void Save() { File.WriteAllText(Path.Combine(output,"runtime-check.json"),JsonUtility.ToJson(report,true)); }
    void Fail(string error)
    {
        if (!smoke) { Debug.LogError(error); return; }
        report.error=error; report.passed=false; Save(); Debug.LogError("E01_BOSS_AUDIT_FAILED: "+error); Application.Quit(1);
    }
    void CaptureScreen(string name)
    {
        var camera=Camera.main;
        int width=Mathf.Max(1,Screen.width),height=Mathf.Max(1,Screen.height);
        var rt=new RenderTexture(width,height,24){antiAliasing=4};rt.Create();
        var previous=RenderTexture.active;var oldTarget=camera.targetTexture;
        var overlays=FindObjectsByType<Canvas>(FindObjectsSortMode.None).Where(c=>c.isRootCanvas && c.renderMode==RenderMode.ScreenSpaceOverlay).ToArray();
        var oldCameras=overlays.Select(c=>c.worldCamera).ToArray();var oldPlanes=overlays.Select(c=>c.planeDistance).ToArray();
        camera.targetTexture=rt;
        foreach (var canvas in overlays) { canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1; }
        Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=rt;
        var texture=new Texture2D(width,height,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,width,height),0,0);texture.Apply();
        File.WriteAllBytes(Path.Combine(output,name),texture.EncodeToPNG());
        for(int i=0;i<overlays.Length;i++) { overlays[i].renderMode=RenderMode.ScreenSpaceOverlay;overlays[i].worldCamera=oldCameras[i];overlays[i].planeDistance=oldPlanes[i]; }
        camera.targetTexture=oldTarget;RenderTexture.active=previous;Canvas.ForceUpdateCanvases();
        rt.Release();Destroy(rt);Destroy(texture);
    }
    void CaptureClose(BossController boss,string name)
    {
        var camera=new GameObject("Audit Close Camera").AddComponent<Camera>();
        camera.CopyFrom(Camera.main); camera.orthographic=false;camera.fieldOfView=33;
        camera.transform.position=boss.transform.position+boss.transform.forward*10+boss.transform.right*6+Vector3.up*6;
        camera.transform.LookAt(boss.transform.position+Vector3.up*2.9f);
        var rt=new RenderTexture(1400,1200,24){antiAliasing=4};rt.Create();var previous=RenderTexture.active;
        camera.targetTexture=rt; camera.Render();RenderTexture.active=rt;
        var texture=new Texture2D(1400,1200,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,1400,1200),0,0);texture.Apply();
        File.WriteAllBytes(Path.Combine(output,name),texture.EncodeToPNG());
        camera.targetTexture=null;RenderTexture.active=previous;rt.Release();Destroy(rt);Destroy(texture);Destroy(camera.gameObject);
    }
}
