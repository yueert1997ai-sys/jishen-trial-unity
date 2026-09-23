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
public sealed class HalbreakerQuickCheck : MonoBehaviour
{
    IEnumerator scenario; string output; float deadline; int errors; bool done;
    readonly List<string> report=new List<string>();
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Bootstrap()
    {
        if(Array.IndexOf(Environment.GetCommandLineArgs(),"-halbreakerCheck")>=0)
            DontDestroyOnLoad(new GameObject("HalbreakerQuickCheck").AddComponent<HalbreakerQuickCheck>());
    }
    void Start()
    {
        var profile=Environment.GetEnvironmentVariable("MECH_EQUIPMENT_PROFILE");
        if(string.IsNullOrEmpty(profile)){Application.Quit(2);return;}
        output=Path.GetDirectoryName(profile);Directory.CreateDirectory(output);
        Application.logMessageReceived+=Log;Application.runInBackground=true;
        AudioListener.volume=0; PlayerInputRouter.AllowUnfocusedReplay=true;Time.captureDeltaTime=1f/60;Application.targetFrameRate=240;
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
    Button Button(string name)=>FindObjectsByType<Button>(FindObjectsInactive.Include,FindObjectsSortMode.None).Single(b=>b.name==name);
    IEnumerator Scenario()
    {
        for(int i=0;i<50;i++)yield return null;
        var gm=GameManager.Instance;var p=gm.playerController;
        var ui=gm.GetComponent<HangarDeploymentUI>();
        if(ui==null)ui=FindFirstObjectByType<HangarDeploymentUI>();
        Check(gm.Phase==GamePhase.Hangar && p.Loadout.Selected==PrimaryWeapon.Halbreaker && p.Loadout.CanDeploy,"fresh launch equips reviewed AX01 and allows immediate sortie");
        var visual=p.GetComponentInChildren<LoadoutVisual>();
        var mount=visual.WeaponObject.GetComponent<HalbreakerMount>();
        Check(mount!=null && mount.exposedBarrelLength==2.2f && visual.WeaponObject.transform.parent.name=="Thorax","approved cannon is mounted to chest, not hand");
        Check(visual.RightGripError<.025f && visual.ShoulderContactError<.001f,"hangar right U grip and shoulder contacts: "+visual.RightGripError+" / "+visual.ShoulderContactError);
        Check(!ui.EquipmentExpanded && !ui.MoreExpanded,"default hangar hides equipment lists and secondary controls");
        Check(FindObjectsByType<Button>(FindObjectsSortMode.None).Count(b=>b.GetComponentInParent<Canvas>().name=="HangarDeploymentCanvas")==5,"default hangar shows only five buttons");
        Capture("01_clean_hangar.png");
        Button("ToggleEquipment").onClick.Invoke();yield return null;
        Check(ui.EquipmentExpanded && Button("EquipHalbreaker").gameObject.activeInHierarchy,"equipment drawer opens");Capture("02_equipment_drawer.png");
        foreach(var choice in new[]{PrimaryWeapon.M7,PrimaryWeapon.M14,PrimaryWeapon.Type08,PrimaryWeapon.Greatsword,PrimaryWeapon.Halbreaker})
        {
            if(!ui.EquipmentExpanded)Button("ToggleEquipment").onClick.Invoke();
            Button("Equip"+choice).onClick.Invoke();for(int i=0;i<8;i++)yield return null;
            Check(p.Loadout.Selected==choice && !ui.EquipmentExpanded && p.Loadout.CanDeploy,"select "+choice+" equips and closes drawer");
        }
        Button("ToggleMore").onClick.Invoke();yield return null;
        Check(ui.MoreExpanded&&Button("LiquidBossChallengeButton").interactable,"secondary boss and difficulty entries remain available");Capture("03_more_drawer.png");
        Button("CloseMore").onClick.Invoke();yield return null;
        Check(!ui.MoreExpanded,"more drawer closes");
        Screen.SetResolution(1280,800,false);for(int i=0;i<35;i++)yield return null;
        CheckUiBounds();Capture("04_hangar_16x10.png");
        Screen.SetResolution(1600,900,false);for(int i=0;i<35;i++)yield return null;
        Button("WeaponTrialButton").onClick.Invoke();for(int i=0;i<25;i++)yield return null;
        var trial=FindFirstObjectByType<WeaponTrial>();trial.ClearTargets();
        Check(!FindObjectsByType<Canvas>(FindObjectsSortMode.None).Any(c=>c.name=="MobileControlsCanvas"),"desktop combat hides touch joystick clutter");
        // Let Destroy remove the default range targets before placing the player at the audit origin.
        for(int i=0;i<2;i++)yield return null;
        p.enabled=false;p.InputRouter.readKeyboard=false;p.RestoreAt(new Vector3(0,.2f,0));
        var targets=new List<Damageable>();
        foreach(float z in new[]{12f,15f,18f,21f})targets.Add(trial.SpawnTarget(new Vector3(0,.1f,z),"",1000).GetComponent<Damageable>());
        for(int i=0;i<12;i++){p.Simulate(new PlayerCommand{HasAim=true,AimPoint=new Vector3(0,1.7f,24)},Time.deltaTime);yield return null;}
        p.weaponController.ResetCooldowns();p.weaponController.TryFireBeam();
        var beam=FindFirstObjectByType<HalbreakerBeam>();
        Check(beam!=null&&!beam.HasFired&&targets.All(t=>t.CurrentHealth==1000),"shot charges before damage");
        Check(beam.Damage==160 && beam.Penetration==99 && HalbreakerBeam.Blue.b>HalbreakerBeam.Blue.r*10,"blue heavy beam damage profile");
        p.weaponController.TryFireBeam();Check(FindObjectsByType<HalbreakerBeam>(FindObjectsSortMode.None).Length==1,"cooldown prevents duplicate shots");
        Capture("05_charge.png");
        for(int i=0;i<16;i++)yield return null;
        report.Add("Target HP="+string.Join(",",targets.Select(t=>t.CurrentHealth))+" beam "+beam.Origin+" to "+beam.End+" direct="+beam.DirectHitCount);
        Check(beam!=null&&beam.HasFired&&beam.DirectHitCount==4&&targets.All(t=>t.CurrentHealth<=840),"one discharge directly pierces all four aligned enemies");
        Check(Vector3.Distance(beam.Origin,visual.WeaponMuzzle.position)<.015f,"beam originates at the actual 2.2 m barrel muzzle");
        Check(Vector3.Dot(visual.WeaponObject.transform.forward,(beam.End-beam.Origin).normalized)>.985f,"barrel and beam remain aligned");
        Check(beam.GetComponentsInChildren<LineRenderer>().All(l=>l.sharedMaterial.shader.isSupported),"blue glow shader is present in standalone");
        Check(beam.GetComponentsInChildren<LineRenderer>().Length>=11,"layered beam, ion spirals and compression rings render");
        Capture("06_blue_piercing_beam.png");
        // A second camera shows the whole real ray and the impacted lineup for inspection.
        var cam=Camera.main;var follow=cam.GetComponent<CameraFollow>();follow.enabled=false;
        cam.orthographic=true;cam.orthographicSize=12;cam.transform.position=new Vector3(24,19,-9);cam.transform.LookAt(new Vector3(0,1.5f,13));
        Capture("07_four_target_penetration.png");follow.enabled=true;
        float[] hp=targets.Select(t=>t.CurrentHealth).ToArray();
        for(int i=0;i<52;i++)yield return null;
        Check(FindFirstObjectByType<HalbreakerBeam>()==null && targets.Select((t,i)=>Mathf.Abs(t.CurrentHealth-hp[i])<.01f).All(x=>x),"afterglow cleans up without repeated damage");
        // Solid scene cover stops the ray even though enemies do not.
        var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.name="AuditSolidCover";wall.transform.position=new Vector3(0,2.5f,10);wall.transform.localScale=new Vector3(8,7,.6f);
        p.weaponController.ResetCooldowns();p.weaponController.TryFireBeam();for(int i=0;i<16;i++)yield return null;
        var blocked=FindFirstObjectByType<HalbreakerBeam>();Check(blocked!=null&&blocked.HasFired&&blocked.DirectHitCount==0,"solid cover stops the penetrating laser");
        Destroy(wall);for(int i=0;i<55;i++)yield return null;
        float leftTravel=0,maxGrip=0,maxShoulder=0;Vector3 last=visual.upperL.InverseTransformPoint(visual.handL.position);
        for(int i=0;i<90;i++)
        {
            p.Simulate(new PlayerCommand{Move=new Vector2(.45f,.8f),BoostHeld=i>40,HasAim=true,AimPoint=p.transform.position+new Vector3(2,1.6f,18)},Time.deltaTime);yield return null;
            Vector3 now=visual.upperL.InverseTransformPoint(visual.handL.position);leftTravel+=Vector3.Distance(now,last);last=now;
            maxGrip=Mathf.Max(maxGrip,visual.RightGripError);maxShoulder=Mathf.Max(maxShoulder,visual.ShoulderContactError);
            if(i==30)Capture("08_run.png");if(i==70)Capture("09_boost.png");
        }
        Check(maxGrip<.06f&&maxShoulder<.001f,"run/boost retain right grip and shoulder contact: "+maxGrip+" / "+maxShoulder);
        Check(leftTravel>.15f,"left arm moves freely during locomotion: "+leftTravel);
        Check(p.GetComponentInChildren<ValkyrMotionDriver>().FlightBlend>.5f,"boost flight retained");
        gm.EndWeaponTrial();p.enabled=true;for(int i=0;i<20;i++)yield return null;
        Button("ToggleEquipment").onClick.Invoke();Button("UnequipButton").onClick.Invoke();yield return null;
        Check(!Button("DeployButton").interactable&&!p.Loadout.CanDeploy,"unarmed deployment remains blocked");
        Button("EquipHalbreaker").onClick.Invoke();for(int i=0;i<10;i++)yield return null;
        Button("DeployButton").onClick.Invoke();for(int i=0;i<170;i++)yield return null;
        Check(gm.IsCombatActive&&FindObjectsByType<EnemyBase>(FindObjectsSortMode.None).Length>0,"normal sortie still spawns enemies");
        Capture("10_sortie.png");
        gm.RestartRun();for(int i=0;i<55;i++)yield return null;
        Check(GameManager.Instance.playerController.Loadout.Selected==PrimaryWeapon.Halbreaker,"restart restores new starter cannon");
        report.Add("Version="+Application.version+" GPU="+SystemInfo.graphicsDeviceName);
    }
    void CheckUiBounds()
    {
        foreach(var b in FindObjectsByType<Button>(FindObjectsSortMode.None))
        {
            var corners=new Vector3[4];b.GetComponent<RectTransform>().GetWorldCorners(corners);
            Check(corners.All(v=>v.x>=-1&&v.y>=-1&&v.x<=Screen.width+1&&v.y<=Screen.height+1),"visible button fits viewport: "+b.name);
        }
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
        report.Add("HALBREAKER_V10_CHECK code="+code+" errors="+errors);File.WriteAllLines(Path.Combine(output,"quick-check.txt"),report);
        Application.logMessageReceived-=Log;Time.captureDeltaTime=0;Application.Quit(code);
    }
}
