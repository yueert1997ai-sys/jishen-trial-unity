using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;

[InitializeOnLoad]
public static class MinovskyBeamAudit
{
    const string Pending="MinovskyBeamAudit.Pending";
    const string Folder="art_prototypes/ZZ_HyperCannon_20260908/unity_evidence/pink_beam";
    static readonly List<string> checks=new List<string>(),errors=new List<string>();
    static readonly List<Damageable> targets=new List<Damageable>();
    static MinovskyBeam beam;
    static int step;
    static double next;
    static float pauseAge;
    static Vector3 shotOrigin;
    [Serializable] class Report { public bool passed; public string[] checks,errors; public string editor; }
    static MinovskyBeamAudit()
    {
        if (!SessionState.GetBool(Pending,false)) return;
        EditorApplication.update+=Tick;Application.logMessageReceived+=Log;
        next=EditorApplication.timeSinceStartup+3;
    }
    public static void Run()
    {
        Directory.CreateDirectory(Folder);
        EditorSceneManager.OpenScene("Assets/Scenes/Demo_Main.unity");
        SessionState.SetBool(Pending,true);EditorApplication.isPlaying=true;
    }
    static void Log(string text,string trace,LogType type)
    {
        if (type==LogType.Error || type==LogType.Exception || type==LogType.Assert) errors.Add(text+"\n"+trace);
    }
    static void Check(bool condition,string message)
    { if (!condition) throw new Exception(message);checks.Add(message); }
    static void Capture(string file)
    {
        var canvases=Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).Where(c=>c.enabled).ToArray();
        foreach(var c in canvases)c.enabled=false;
        JishenHeroIntegration.Capture(Camera.main,Folder+"/"+file+".png",1800,1000);
        foreach(var c in canvases)c.enabled=true;
    }
    static void Tick()
    {
        if (!EditorApplication.isPlaying || EditorApplication.timeSinceStartup<next) return;
        var gm=GameManager.Instance;
        if(gm==null || gm.playerController==null) return;
        var player=gm.playerController;var visual=player.GetComponentInChildren<LoadoutVisual>();
        try
        {
            switch(step)
            {
                case 0:
                    Time.captureDeltaTime=1f/60;
                    Check(player.Loadout.Select(PrimaryWeapon.Type08),"Particle cannon is selectable");
                    step++;next=EditorApplication.timeSinceStartup+.3;break;
                case 1: gm.BeginRun();step++;next=EditorApplication.timeSinceStartup+.5;break;
                case 2:
                    gm.stageManager.enabled=false;
                    foreach(var d in Damageable.Active.ToArray()) if(d.team!=0)d.gameObject.SetActive(false);
                    player.RestoreAt(new Vector3(0,.38f,0));
                    player.AimAt(new Vector3(0,2.4f,20));
                    player.enabled=false;
                    var camera=Camera.main;camera.GetComponent<CameraFollow>().enabled=false;
                    camera.orthographic=true;camera.orthographicSize=3.8f;camera.rect=new Rect(0,0,1,1);
                    camera.transform.position=new Vector3(12,7,8);camera.transform.LookAt(new Vector3(.4f,1.9f,4.5f));
                    camera.allowHDR=true;
                    var key=new GameObject("Beam audit armor key").AddComponent<Light>();key.type=LightType.Directional;
                    key.transform.rotation=Quaternion.Euler(35,-45,0);key.intensity=.9f;key.color=new Color(.88f,.91f,1);
                    step++;next=EditorApplication.timeSinceStartup+.25;break;
                case 3:
                    shotOrigin=visual.WeaponMuzzle.position;
                    Vector3 axis=(player.AimPoint-shotOrigin).normalized;
                    for(int i=0;i<4;i++)
                    {
                        var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name="Beam verification target "+i;
                        go.transform.position=shotOrigin+axis*(3+2.2f*i);go.transform.localScale=new Vector3(.7f,1.5f,.7f);
                        var material=new Material(Shader.Find("Standard"));material.color=new Color(.19f,.23f,.28f);
                        material.SetFloat("_Metallic",.7f);go.GetComponent<Renderer>().material=material;
                        var target=go.AddComponent<Damageable>();target.team=1;target.destroyOnDeath=false;target.RestoreLife(1000,1000);targets.Add(target);
                    }
                    Physics.SyncTransforms();
                    player.weaponController.ResetCooldowns();player.weaponController.TryFireBeam();
                    beam=Object.FindFirstObjectByType<MinovskyBeam>();Check(beam!=null,"Runtime firing creates the Minovsky beam effect");
                    Check(Shader.Find("MECH ROUGE/Minovsky Glow").isSupported,"Additive particle glow shader compiles on the active graphics device");
                    step++;break;
                case 4:
                    if(beam.Age<.045f)return;
                    Check(!beam.HasFired && targets.All(t=>t.CurrentHealth==1000),"Charge glows before the discharge and deals no early damage");
                    Capture("01_CHARGE");step++;break;
                case 5:
                    if(!beam.HasFired || beam.Age<.17f)return;
                    Check(beam.DirectHitCount==3,"One beam penetrates exactly three targets with penetration 2");
                    Check(targets.Take(3).All(t=>Mathf.Abs(t.CurrentHealth-880)<.01f) && targets[3].CurrentHealth==1000,"Each direct target receives one 120-damage hit and the fourth remains intact");
                    Check(Vector3.Distance(beam.Origin,visual.WeaponMuzzle.position)<.001f,"Continuous beam remains attached to the visible muzzle");
                    Check(Vector3.Dot(visual.WeaponMuzzle.forward,(beam.End-beam.Origin).normalized)>.995f,"Cannon barrel and particle beam point in the same direction");
                    var core=beam.GetComponentsInChildren<LineRenderer>().Single(l=>l.name=="White-pink beam core");
                    var sheath=beam.GetComponentsInChildren<LineRenderer>().Single(l=>l.name=="Saturated pink particle sheath");
                    Check(core.enabled && sheath.enabled && core.startColor.g>.7f && sheath.startColor.r>.9f && sheath.startColor.b>.3f && sheath.startColor.g<.2f,"Beam has a bright white-pink core and saturated pink sheath");
                    Capture("02_PINK_BEAM");
                    gm.SetPaused(true);pauseAge=beam.Age;step++;next=EditorApplication.timeSinceStartup+.18;break;
                case 6:
                    Check(beam!=null && Mathf.Abs(beam.Age-pauseAge)<.0001f,"Pausing freezes charge, beam and fade timing");
                    gm.SetPaused(false);step++;break;
                case 7:
                    if(beam!=null && beam.Age<.42f)return;
                    Check(beam!=null,"Particle halo lingers briefly after the bright beam");
                    Capture("03_AFTERGLOW");step++;break;
                case 8:
                    if(beam!=null)return;
                    Check(targets.Take(3).All(t=>Mathf.Abs(t.CurrentHealth-880)<.01f),"The lingering beam never repeats damage each frame");
                    foreach(var target in targets)target.RestoreLife(1000,1000);
                    Vector3 direction=(player.AimPoint-visual.WeaponMuzzle.position).normalized;
                    var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.name="Beam cover verification";
                    wall.transform.position=visual.WeaponMuzzle.position+direction*1.5f;wall.transform.localScale=new Vector3(3,4,.5f);
                    Physics.SyncTransforms();
                    player.weaponController.ResetCooldowns();player.weaponController.TryFireBeam();
                    beam=Object.FindFirstObjectByType<MinovskyBeam>();step++;break;
                case 9:
                    if(!beam.HasFired)return;
                    Check(beam.DirectHitCount==0 && Vector3.Distance(beam.Origin,beam.End)<2,"Solid cover stops the beam at its surface");
                    Check(targets.All(t=>t.CurrentHealth==1000),"Targets behind cover receive neither direct nor splash damage");
                    gm.RestartRun();step++;next=EditorApplication.timeSinceStartup+.8;break;
                case 10:
                    Check(Object.FindObjectsByType<MinovskyBeam>(FindObjectsSortMode.None).Length==0 && gm.Phase==GamePhase.Hangar,"Returning to hangar clears active beam effects");
                    Finish();break;
            }
        }
        catch(Exception e){errors.Add(e.ToString());Finish();}
    }
    static void Finish()
    {
        Time.captureDeltaTime=0;Time.timeScale=1;
        bool passed=errors.Count==0;
        File.WriteAllText(Folder+"/runtime-report.json",JsonUtility.ToJson(new Report{passed=passed,checks=checks.ToArray(),errors=errors.ToArray(),editor=Application.unityVersion},true));
        SessionState.SetBool(Pending,false);EditorApplication.update-=Tick;Application.logMessageReceived-=Log;
        Debug.Log(passed?"MINOVSKY_BEAM_PASS":"MINOVSKY_BEAM_FAIL");EditorApplication.Exit(passed?0:1);
    }
}
