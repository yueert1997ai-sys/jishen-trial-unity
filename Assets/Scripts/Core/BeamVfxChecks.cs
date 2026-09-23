using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

// Gundam beam VFX round: opt-in standalone replay that fires every beam weapon
// in the weapon trial, captures frame-by-frame evidence of the new layered
// presentation and asserts the structural contracts (shader, layer counts,
// shared particle kit, afterglow cleanup). It writes only to its isolated
// test profile directory.
[DefaultExecutionOrder(1500)]
public sealed class BeamVfxChecks : MonoBehaviour
{
    IEnumerator scenario; string output; float deadline; int errors; bool done;
    readonly List<string> report=new List<string>();
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Bootstrap()
    {
        if(Array.IndexOf(Environment.GetCommandLineArgs(),"-beamVfxCheck")>=0)
            DontDestroyOnLoad(new GameObject("BeamVfxChecks").AddComponent<BeamVfxChecks>());
    }
    void Awake(){Application.logMessageReceived+=Log;}
    void Start()
    {
        var profile=Environment.GetEnvironmentVariable("MECH_EQUIPMENT_PROFILE");
        if(string.IsNullOrEmpty(profile)){Application.Quit(2);return;}
        output=Path.GetDirectoryName(profile);Directory.CreateDirectory(output);
        Application.runInBackground=true;
        AudioListener.volume=0;PlayerInputRouter.AllowUnfocusedReplay=true;Time.captureDeltaTime=1f/60;Application.targetFrameRate=240;
        deadline=Time.realtimeSinceStartup+240;scenario=Scenario();
    }
    void LateUpdate()
    {
        if(done||scenario==null)return;
        try{if(Time.realtimeSinceStartup>deadline)throw new Exception("Beam VFX replay timeout");if(!scenario.MoveNext())Finish(null);}
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
        Check(gm.Phase==GamePhase.Hangar,"fresh launch reaches the hangar");

        foreach(var weapon in new[]{PrimaryWeapon.Type08,PrimaryWeapon.Halbreaker,PrimaryWeapon.M14,PrimaryWeapon.M7})
        {
            if(FindFirstObjectByType<HangarDeploymentUI>()==null || !Button("ToggleEquipment").gameObject.activeInHierarchy)
            { gm.RestartRun();for(int i=0;i<50;i++)yield return null; }
            // Legacy weapons remain regression fixtures; the J01 drawer lists its own equipment.
            if(p.GetComponentInChildren<NemesisMotionRig>()!=null)p.Loadout.Select(weapon);
            else {Button("ToggleEquipment").onClick.Invoke();yield return null;Button("Equip"+weapon).onClick.Invoke();}
            for(int i=0;i<10;i++)yield return null;
            Check(p.Loadout.Selected==weapon && p.Loadout.CanDeploy,"equip "+weapon+" from the hangar drawer");
            // The hangar's practice button now runs the short combat; the weapon
            // trial itself is entered through the manager API.
            gm.BeginWeaponTrial();for(int i=0;i<25;i++)yield return null;
            var trial=FindFirstObjectByType<WeaponTrial>();trial.ClearTargets();
            for(int i=0;i<2;i++)yield return null;
            p.enabled=false;p.InputRouter.readKeyboard=false;p.RestoreAt(new Vector3(0,.2f,0));

            if(weapon==PrimaryWeapon.Type08 || weapon==PrimaryWeapon.Halbreaker)
            {
                // TYPE-08 stops on its first target; the AX-01 pierces a column.
                var targets=new List<Damageable>();
                var slots=weapon==PrimaryWeapon.Halbreaker?new[]{12f,15f,18f,21f}:new[]{14f};
                foreach(float z in slots)targets.Add(trial.SpawnTarget(new Vector3(0,.1f,z),"",1000).GetComponent<Damageable>());
                // Both weapons must hit ordinary floor-level targets with the current body.
                Vector3 beamAim;
                if (weapon==PrimaryWeapon.Halbreaker)
                {
                    beamAim=targets[targets.Count-1].AimCenter;
                }
                else beamAim=targets[0].AimCenter;
                for(int i=0;i<12;i++){p.Simulate(new PlayerCommand{HasAim=true,AimPoint=beamAim},Time.deltaTime);yield return null;}
                p.weaponController.ResetCooldowns();p.weaponController.TryFireBeam();
                MonoBehaviour beam=weapon==PrimaryWeapon.Halbreaker?(MonoBehaviour)FindFirstObjectByType<HalbreakerBeam>():FindFirstObjectByType<MinovskyBeam>();
                Check(beam!=null,"beam spawns on command for "+weapon);
                // Side-on orthographic view of the whole beam line.
                var camera=Camera.main;var follow=camera.GetComponent<CameraFollow>();follow.enabled=false;
                camera.orthographic=true;camera.orthographicSize=13;
                camera.transform.position=new Vector3(22,10,8);camera.transform.LookAt(new Vector3(0,1.4f,12));
                string tag=weapon==PrimaryWeapon.Halbreaker?"hal":"type08";
                for(int frame=1;frame<=26;frame++)
                {
                    yield return null;
                    if(frame==4||frame==7||frame==9||frame==12||frame==15||frame==19||frame==24)
                        Capture(tag+"_f"+frame.ToString("00")+".png",camera);
                }
                // The kit is created lazily by the beam's first effect call, which
                // runs after this check in execution order, so assert it post-loop.
                Check(FindFirstObjectByType<BeamFxKit>()!=null,"textured beam kit is alive during fire");
                // Geometry diagnostics for the fired shot vs the target column.
                Vector3 firedOrigin=weapon==PrimaryWeapon.Halbreaker?((HalbreakerBeam)beam).Origin:((MinovskyBeam)beam).Origin;
                Vector3 firedEnd=weapon==PrimaryWeapon.Halbreaker?((HalbreakerBeam)beam).End:((MinovskyBeam)beam).End;
                report.Add(weapon+" aim="+beamAim.ToString("F2")+" origin="+firedOrigin.ToString("F2")
                    +" end="+firedEnd.ToString("F2")+" muzzle="+p.GetComponentInChildren<LoadoutVisual>().WeaponMuzzle.position.ToString("F2")
                    +" targets="+string.Join("|",targets.Select(t=>t.AimCenter.ToString("F2"))));
                bool fired=weapon==PrimaryWeapon.Halbreaker
                    ?((HalbreakerBeam)beam).HasFired:((MinovskyBeam)beam).HasFired;
                Check(fired,"beam discharges after the charge window for "+weapon);
                int directHits=weapon==PrimaryWeapon.Halbreaker
                    ?((HalbreakerBeam)beam).DirectHitCount:((MinovskyBeam)beam).DirectHitCount;
                var lines=((Component)beam).GetComponentsInChildren<LineRenderer>();
                int expected=weapon==PrimaryWeapon.Halbreaker?13:11;
                Check(lines.Length==expected,weapon+" renders "+expected+" line layers, got "+lines.Length);
                Check(lines.All(l=>l.sharedMaterial.shader.name=="MECH ROUGE/Minovsky Glow"&&l.sharedMaterial.shader.isSupported),
                    weapon+" standalone build keeps the supported Minovsky Glow shader on every line");
                Check(targets.All(t=>t.CurrentHealth<1000),weapon+" beam damage lands on the trial targets: direct="+directHits
                    +" hp="+string.Join(",",targets.Select(t=>Mathf.RoundToInt(t.CurrentHealth))));
                float[] hp=targets.Select(t=>t.CurrentHealth).ToArray();
                float maxAxisError=0,maxSocketGap=0;
                for(int i=0;i<8;i++)
                {
                    p.Simulate(new PlayerCommand{HasAim=true,Move=Vector2.right,AimPoint=new Vector3(12+i,.2f,19-i)},Time.deltaTime);
                    yield return null;
                    var socket=p.weaponController.muzzle;
                    Vector3 o=weapon==PrimaryWeapon.Halbreaker?((HalbreakerBeam)beam).Origin:((MinovskyBeam)beam).Origin;
                    Vector3 e=weapon==PrimaryWeapon.Halbreaker?((HalbreakerBeam)beam).End:((MinovskyBeam)beam).End;
                    maxAxisError=Mathf.Max(maxAxisError,Vector3.Angle(e-o,socket.forward));
                    maxSocketGap=Mathf.Max(maxSocketGap,Vector3.Distance(o,socket.position));
                }
                Check(maxAxisError<.1f && maxSocketGap<.001f,weapon+" stays on moving barrel axis: degrees="+maxAxisError+" gap="+maxSocketGap);
                for(int i=0;i<50;i++)yield return null;
                Check(FindFirstObjectByType<HalbreakerBeam>()==null&&FindFirstObjectByType<MinovskyBeam>()==null
                    &&targets.Select((t,i)=>Mathf.Abs(t.CurrentHealth-hp[i])<.01f).All(x=>x),
                    weapon+" afterglow cleans up without repeated damage");
                // Low cover must block a high mounted cannon on the same gameplay plane.
                var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);
                wall.name="Beam regression low cover";
                wall.transform.position=new Vector3(0,1.1f,7);
                wall.transform.localScale=new Vector3(5,2.2f,1);
                p.RestoreAt(new Vector3(0,.2f,0));
                Physics.SyncTransforms();
                for(int i=0;i<12;i++){p.Simulate(new PlayerCommand{HasAim=true,AimPoint=beamAim},Time.deltaTime);yield return null;}
                p.weaponController.ResetCooldowns();p.weaponController.TryFireBeam();
                for(int i=0;i<26;i++)yield return null;
                Check(targets.Select((t,i)=>Mathf.Abs(t.CurrentHealth-hp[i])<.01f).All(x=>x),
                    weapon+" low cover blocks direct and splash damage at high muzzle height");
                var blockedHal=FindFirstObjectByType<HalbreakerBeam>();
                var blockedType=FindFirstObjectByType<MinovskyBeam>();
                Check(weapon==PrimaryWeapon.Halbreaker
                    ?blockedHal!=null&&blockedHal.HasFired&&blockedHal.DirectHitCount==0&&blockedHal.End.z<7
                    :blockedType!=null&&blockedType.HasFired&&blockedType.DirectHitCount==0&&blockedType.End.z<7,
                    weapon+" beam terminates at the cover face");
                Destroy(wall);
                follow.enabled=true;
            }
            else
            {
                var target=trial.SpawnTarget(new Vector3(0,.1f,10),"",1000).GetComponent<Damageable>();
                for(int i=0;i<8;i++){p.Simulate(new PlayerCommand{HasAim=true,AimPoint=target.AimCenter},Time.deltaTime);yield return null;}
                var camera=Camera.main;var follow=camera.GetComponent<CameraFollow>();follow.enabled=false;
                // Side view of the whole flight line: muzzle glare and impact share the frame.
                camera.transform.position=new Vector3(7f,2.8f,5.5f);camera.transform.LookAt(new Vector3(-.5f,1.4f,5.5f));
                bool sawEnergy=false;bool fired=false;Action onFired=()=>fired=true;p.weaponController.BeamFired+=onFired;
                for(int i=0;i<40;i++)
                {
                    p.Simulate(new PlayerCommand{Fire=true,HasAim=true,AimPoint=target.AimCenter},Time.deltaTime);yield return null;
                    var packet=FindObjectsByType<EnergyBoltVisual>(FindObjectsSortMode.None).FirstOrDefault(v=>v.Active);
                    if(packet!=null)
                    {
                        sawEnergy=true;
                        Check(packet.LayerCount==4 && packet.GetComponentsInChildren<LineRenderer>().All(l=>l.sharedMaterial.shader.isSupported),weapon+" live energy packet has four supported layers");
                        if(i%3==0)Capture(weapon+"_packet_f"+i.ToString("00")+".png",camera);
                    }
                    if(fired){Capture(weapon+"_muzzle_f"+i.ToString("00")+".png",camera);fired=false;}
                }
                p.weaponController.BeamFired-=onFired;
                Check(sawEnergy,weapon+" renders live beam packets");
                Check(target.CurrentHealth<1000,weapon+" live rounds hit the trial target");
                follow.enabled=true;
            }
            gm.EndWeaponTrial();p.enabled=true;for(int i=0;i<20;i++)yield return null;
            Check(gm.Phase==GamePhase.Hangar,"trial returns to the hangar for the next weapon");
        }
        // Shared pool must recolor packets when ownership changes.
        var pooled=ProjectilePool.Spawn(false,"Collection beam packet",Vector3.zero,Color.cyan);
        pooled.Init(0,p.GetComponent<Damageable>(),Vector3.forward,10,30,2,0,0);
        Check(pooled.GetComponent<EnergyBoltVisual>().Active,"collection projectiles share the energy packet renderer");
        pooled.Despawn();
        var enemy=ProjectilePool.Spawn(false,"Enemy reuse",Vector3.zero,Color.red);
        enemy.Init(1,null,Vector3.forward,10,20,2,0,0);
        Check(enemy==pooled&&enemy.GetComponent<EnergyBoltVisual>().Active&&!enemy.GetComponent<MeshRenderer>().enabled
            &&enemy.GetComponent<EnergyBoltVisual>().Tint==EnergyBoltVisual.EnemyRed
            &&enemy.GetComponentsInChildren<LineRenderer>().All(l=>l.enabled),"pool reuse changes player packet to red enemy beam layers");
        enemy.Despawn();
        var reused=ProjectilePool.Spawn(false,"Player reuse",Vector3.zero,Color.cyan);
        reused.Init(0,p.GetComponent<Damageable>(),Vector3.forward,10,30,2,0,0);
        Check(reused==enemy&&reused.GetComponent<EnergyBoltVisual>().Tint==Color.cyan,"enemy red does not leak into reused player shots");
        reused.Despawn();
        var missile=ProjectilePool.Spawn(true,"Support energy packet",Vector3.zero,new Color(1,.6f,.12f));
        missile.Init(0,p.GetComponent<Damageable>(),Vector3.forward,18,18,4,1.6f,0);
        Check(missile is MissileProjectile&&!missile.Planar&&missile.GetComponent<EnergyBoltVisual>().Active,"support missiles keep homing simulation with energy presentation");
        missile.Despawn();
        report.Add("Version="+Application.version+" GPU="+SystemInfo.graphicsDeviceName);
    }

    // Combat captures skip the UI overlays: this round judges the particle
    // presentation itself, not the HUD.
    void Capture(string name, Camera camera)
    {
        report.Add("CAPTURE "+name+" camera="+camera.transform.position+" size="+camera.orthographicSize);
        var rt=new RenderTexture(1600,900,24){antiAliasing=4};rt.Create();var active=RenderTexture.active;var target=camera.targetTexture;
        camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;
        var image=new Texture2D(1600,900,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1600,900),0,0);image.Apply();
        File.WriteAllBytes(Path.Combine(output,name),image.EncodeToPNG());
        camera.targetTexture=target;RenderTexture.active=active;rt.Release();Destroy(rt);Destroy(image);
    }
    void Finish(string failure)
    {
        done=true;if(failure!=null)report.Add("FAIL "+failure);int code=failure==null&&errors==0?0:1;
        report.Add("P0_CHECK code="+code+" errors="+errors);File.WriteAllLines(Path.Combine(output,"quick-check.txt"),report);
        Application.logMessageReceived-=Log;Time.captureDeltaTime=0;Application.Quit(code);
    }
}
