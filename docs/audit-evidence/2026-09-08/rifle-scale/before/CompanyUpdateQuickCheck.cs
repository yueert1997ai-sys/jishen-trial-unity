using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

// Opt-in standalone replay. It writes only to its isolated test profile directory.
[DefaultExecutionOrder(250)]
public sealed class CompanyUpdateQuickCheck : MonoBehaviour
{
    IEnumerator scenario; string output; float deadline; int errors; bool done;
    readonly List<string> report=new List<string>();
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Bootstrap()
    {
        if(Array.IndexOf(Environment.GetCommandLineArgs(),"-companyCheck")>=0)
            DontDestroyOnLoad(new GameObject("CompanyUpdateQuickCheck").AddComponent<CompanyUpdateQuickCheck>());
    }
    void Start()
    {
        var profile=Environment.GetEnvironmentVariable("MECH_EQUIPMENT_PROFILE");
        if(string.IsNullOrEmpty(profile)){Application.Quit(2);return;}
        output=Path.GetDirectoryName(profile);Directory.CreateDirectory(output);
        Application.logMessageReceived+=Log;Application.runInBackground=true;
        PlayerInputRouter.AllowUnfocusedReplay=true;Time.captureDeltaTime=1f/60;Application.targetFrameRate=240;
        deadline=Time.realtimeSinceStartup+180;scenario=Scenario();
    }
    void LateUpdate()
    {
        if(done||scenario==null)return;
        try{if(Time.realtimeSinceStartup>deadline)throw new Exception("V9 replay timeout");if(!scenario.MoveNext())Finish(null);}
        catch(Exception e){Finish(e.ToString());}
    }
    void Log(string message,string stack,LogType kind)
    {if(kind==LogType.Error||kind==LogType.Exception||kind==LogType.Assert){errors++;report.Add(message+"\n"+stack);}}
    void Check(bool condition,string message){if(!condition)throw new Exception(message);report.Add("PASS "+message);}
    Button Button(string name)=>FindObjectsByType<Button>(FindObjectsSortMode.None).Single(b=>b.name==name);
    IEnumerator Scenario()
    {
        for(int i=0;i<45;i++)yield return null;
        var gm=GameManager.Instance;var p=gm.playerController;
        Check(gm.Phase==GamePhase.Hangar&&!p.Loadout.CanDeploy,"hangar starts unarmed and prevents empty sortie");
        gm.BeginRun();Check(gm.Phase==GamePhase.Hangar,"game logic rejects empty sortie");
        Check(p.GetComponentInChildren<ValkyrMotionDriver>()!=null,"V7 authored motion is attached to the company V3 body");
        Check(p.GetComponentsInChildren<Transform>(true).Any(t=>t.name=="Company_VALKYR_V3_5251340"),"company V3 geometry is instantiated");
        Capture("01_hangar_unarmed.png");
        Button("OpenWarehouseButton").onClick.Invoke();
        Check(Button("Equip_pulse").interactable,"owned collection weapon remains selectable from the new loadout");
        Button("Equip_pulse").onClick.Invoke();Check(p.Loadout.Selected==PrimaryWeapon.Collection,"warehouse selection enters the preserved collection combat mode");
        Capture("01_collection.png");gm.equipmentLoop.CloseWarehouse();
        foreach(var choice in new[]{PrimaryWeapon.M7,PrimaryWeapon.M14})
        {
            Button("Equip"+choice).onClick.Invoke();for(int i=0;i<15;i++)yield return null;
            var visual=p.GetComponentInChildren<LoadoutVisual>();
            Check(visual.WeaponObject!=null&&visual.WeaponMuzzle!=null,choice+" real weapon and muzzle are equipped");
            Capture("02_hangar_"+choice+".png");
            report.Add(choice+" hangar grip errors R="+visual.RightGripError+" L="+visual.LeftGripError);
            Button("WeaponTrialButton").onClick.Invoke();for(int i=0;i<20;i++)yield return null;
            var trial=FindFirstObjectByType<WeaponTrial>();trial.ClearTargets();
            p.enabled=false;p.InputRouter.readKeyboard=false;p.RestoreAt(new Vector3(0,.2f,0));
            var target=trial.SpawnTarget(new Vector3(0,.1f,6),"",1000).GetComponent<Damageable>();
            for(int i=0;i<4;i++)yield return null;
            float before=target.CurrentHealth;int fired=0;Action onShot=()=>fired++;p.weaponController.BeamFired+=onShot;
            for(int i=0;i<60;i++){p.Simulate(new PlayerCommand{Fire=true,HasAim=true,AimPoint=target.AimCenter},Time.deltaTime);yield return null;}
            p.weaponController.BeamFired-=onShot;
            Check(fired>0&&target.CurrentHealth<before,choice+" live rounds hit the target from the real muzzle");
            Check(!p.Melee.TryAttack(),choice+" cannot create an invisible sword hit");
            Check(!p.Loadout.Select(PrimaryWeapon.Greatsword),"combat locks hangar weapon selection");
            report.Add(choice+" combat grip errors R="+visual.RightGripError+" L="+visual.LeftGripError);
            Check(visual.RightGripError<.06f&&visual.LeftGripError<.06f,choice+" both combat grip contacts stay within 6cm");
            Capture("03_combat_"+choice+".png");
            gm.EndWeaponTrial();p.enabled=true;for(int i=0;i<10;i++)yield return null;
        }
        Button("EquipGreatsword").onClick.Invoke();for(int i=0;i<15;i++)yield return null;
        Capture("04_hangar_raiken.png");
        Button("WeaponTrialButton").onClick.Invoke();for(int i=0;i<15;i++)yield return null;
        var range=FindFirstObjectByType<WeaponTrial>();range.ClearTargets();
        p.enabled=false;p.RestoreAt(new Vector3(0,.2f,0));
        var dummy=range.SpawnTarget(new Vector3(0,.1f,2.5f),"",1000).GetComponent<Damageable>();
        for(int i=0;i<4;i++)yield return null;
        float health=dummy.CurrentHealth;int maxStage=-1,shots=0;var captured=new bool[3];Action countShot=()=>shots++;
        p.weaponController.BeamFired+=countShot;
        for(int i=0;i<155;i++)
        {
            p.Simulate(new PlayerCommand{Fire=i==0||i==22||i==57,HasAim=true,AimPoint=dummy.AimCenter},Time.deltaTime);
            yield return null;maxStage=Mathf.Max(maxStage,p.Melee.ComboStage);
            if(p.Melee.IsCutting&&!captured[p.Melee.ComboStage]){captured[p.Melee.ComboStage]=true;Capture("05_combo_"+(p.Melee.ComboStage+1)+".png");}
        }
        p.weaponController.BeamFired-=countShot;
        Check(maxStage==2&&dummy.CurrentHealth<health,"left click drives the three-stage real blade sweep and damages targets");
        Check(shots==0,"sword selection never emits invisible rifle shots");
        Check(gm.arenaSector.lunar.activeSelf&&gm.arenaSector.lunarNavigation!=null,"lunar sector and navigation are active in combat");
        Check(p.transform.position.y>-.3f,"player remains on lunar combat ground");
        gm.EndWeaponTrial();p.enabled=true;for(int i=0;i<15;i++)yield return null;
        Button("EquipGreatsword").onClick.Invoke();for(int i=0;i<4;i++)yield return null;
        Button("LiquidBossChallengeButton").onClick.Invoke();for(int i=0;i<100;i++)yield return null;
        var boss=FindFirstObjectByType<BossController>();
        Check(boss!=null&&boss.GetComponent<E01ElitePoseDriver>()!=null&&gm.upgradeSystem.Count==6,"liquid boss button spawns the real boss with six run buffs");
        float cameraWait=Time.realtimeSinceStartup+.4f;
        while(Time.realtimeSinceStartup<cameraWait)yield return null;
        Capture("06_liquid_boss.png");
        gm.RestartRun();for(int i=0;i<50;i++)yield return null;
        gm=GameManager.Instance;p=gm.playerController;Button("EquipM7").onClick.Invoke();for(int i=0;i<5;i++)yield return null;
        Button("DeployButton").onClick.Invoke();for(int i=0;i<180;i++)yield return null;
        var enemies=FindObjectsByType<EnemyBase>(FindObjectsSortMode.None);
        Check(enemies.Length>0&&enemies.All(e=>e.GetComponent<E01SoldierMotion>()!=null),"ordinary lunar sortie spawns white E01 soldiers");
        Check(enemies.Any(e=>e.GetComponent<NavMeshAgent>()!=null&&e.GetComponent<NavMeshAgent>().isOnNavMesh),"white soldiers use the lunar navigation mesh");
        Capture("07_lunar_sortie.png");
        p.GetComponent<Damageable>().SetInvulnerable(100);p.enabled=false;
        for(int i=0;i<65;i++){p.Simulate(new PlayerCommand{Move=new Vector2(.75f,.5f),BoostHeld=true,HasAim=true,AimPoint=new Vector3(3,1,6)},Time.deltaTime);yield return null;}
        Check(p.GetComponentInChildren<ValkyrMotionDriver>().FlightBlend>.5f,"equipped rifle keeps V7 boost flight motion");
        Capture("08_rifle_boost.png");
        var camera=Camera.main;var pos=camera.transform.position;var rot=camera.transform.rotation;var size=camera.orthographicSize;
        camera.orthographicSize=30;camera.transform.rotation=Quaternion.Euler(60,-20,0);camera.transform.position=Vector3.up*1-camera.transform.forward*58;
        Capture("09_lunar_overview.png");camera.transform.SetPositionAndRotation(pos,rot);camera.orthographicSize=size;
        report.Add("Version="+Application.version+" GPU="+SystemInfo.graphicsDeviceName);
        report.Add("Short standalone replay only; not a full clear or exhaustive animation review.");
    }
    void Capture(string name)
    {
        var camera=Camera.main;
        report.Add(name+" camera="+camera.transform.position+" angles="+camera.transform.eulerAngles+" size="+camera.orthographicSize+" follows="+camera.GetComponent<CameraFollow>().enabled);
        var overlays=FindObjectsByType<Canvas>(FindObjectsSortMode.None).Where(c=>c.isRootCanvas&&c.renderMode==RenderMode.ScreenSpaceOverlay).ToArray();
        var cameras=overlays.Select(c=>c.worldCamera).ToArray();var planes=overlays.Select(c=>c.planeDistance).ToArray();
        var rt=new RenderTexture(1600,900,24){antiAliasing=4};rt.Create();var active=RenderTexture.active;var target=camera.targetTexture;
        camera.targetTexture=rt;foreach(var c in overlays){c.renderMode=RenderMode.ScreenSpaceCamera;c.worldCamera=camera;c.planeDistance=1;}
        Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=rt;
        var image=new Texture2D(1600,900,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1600,900),0,0);image.Apply();
        File.WriteAllBytes(Path.Combine(output,name),image.EncodeToPNG());
        for(int i=0;i<overlays.Length;i++){overlays[i].renderMode=RenderMode.ScreenSpaceOverlay;overlays[i].worldCamera=cameras[i];overlays[i].planeDistance=planes[i];}
        camera.targetTexture=target;RenderTexture.active=active;rt.Release();Destroy(rt);Destroy(image);Canvas.ForceUpdateCanvases();
    }
    void Finish(string failure)
    {
        done=true;if(failure!=null)report.Add("FAIL "+failure);int code=failure==null&&errors==0?0:1;
        report.Add("COMPANY_V9_CHECK code="+code+" errors="+errors);File.WriteAllLines(Path.Combine(output,"quick-check.txt"),report);
        Application.logMessageReceived-=Log;Time.captureDeltaTime=0;Application.Quit(code);
    }
}
