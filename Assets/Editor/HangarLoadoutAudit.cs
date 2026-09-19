using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[InitializeOnLoad]
public static class HangarLoadoutAudit
{
    const string Pending="HangarLoadoutAudit.Pending";
    const string Folder="AuditEvidence/hangar";
    static int step;
    static double next;
    static readonly List<string> checks=new List<string>();
    static readonly List<string> errors=new List<string>();
    static readonly List<string> editorErrors=new List<string>();
    static Damageable target;
    [Serializable] class Report { public bool passed; public string[] checks, errors, editorErrors; public string editor; }
    static HangarLoadoutAudit()
    {
        if (SessionState.GetBool(Pending,false))
        {
            EditorApplication.update+=Tick;
            Application.logMessageReceived+=Log;
            next=EditorApplication.timeSinceStartup+3;
        }
    }
    public static void Run()
    {
        Directory.CreateDirectory(Folder);
        EditorSceneManager.OpenScene("Assets/Scenes/Demo_Main.unity");
        SessionState.SetBool(Pending,true);
        EditorApplication.isPlaying=true;
    }
    static void Log(string condition,string trace,LogType type)
    {
        if(type!=LogType.Error && type!=LogType.Exception && type!=LogType.Assert) return;
        if(trace.Contains("UnityEditor.Search.SearchDatabase")) editorErrors.Add(condition+"\n"+trace);
        else errors.Add(condition+"\n"+trace);
    }
    static void Check(bool value,string message)
    { if (!value) throw new Exception(message); checks.Add(message); }
    static void Click(string name)
    {
        var button=GameObject.Find(name)?.GetComponent<Button>();
        Check(button!=null && button.interactable,"UI button available: "+name);
        ExecuteEvents.Execute(button.gameObject,new PointerEventData(EventSystem.current),ExecuteEvents.pointerClickHandler);
    }
    static void Shot(PlayerController player,float damage,float speed,int pierce)
    {
        player.AimAt(player.transform.position+Vector3.forward*8+Vector3.up*1.5f);
        player.weaponController.ResetCooldowns();player.weaponController.TryFireBeam();
        var shot=UnityEngine.Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None).FirstOrDefault(p=>p.team==0);
        Check(shot!=null && Mathf.Abs(shot.damage-damage)<.01f && shot.speed==speed && shot.pierceCount==pierce,"Selected rifle controls live projectile damage, speed and penetration");
        Check(!player.Melee.TryAttack(),"Rifle cannot trigger an invisible sword attack");
    }
    static void Grip(LoadoutVisual visual)
    {
        checks.Add("Grip errors: "+visual.RightGripError+", "+visual.LeftGripError);
        Check(visual.RightGripError<.06f && visual.LeftGripError<.06f,"Both hands reach the selected weapon grips");
    }
    static void Capture(string name)
    {
        var camera=Camera.main;
        var canvases=UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).Where(c=>c.isRootCanvas && c.renderMode==RenderMode.ScreenSpaceOverlay).ToArray();
        foreach(var canvas in canvases) { canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1; }
        Canvas.ForceUpdateCanvases();
        JishenHeroIntegration.Capture(camera,Folder+"/"+name+".png",1600,900);
        foreach(var canvas in canvases) { canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.worldCamera=null; }
    }
    static void Tick()
    {
        if (!EditorApplication.isPlaying || EditorApplication.timeSinceStartup<next) return;
        var gm=GameManager.Instance;
        if(gm==null || gm.playerController==null) return;
        var player=gm.playerController;var loadout=player.Loadout;
        var visual=player.GetComponentInChildren<LoadoutVisual>();
        if(visual==null) { Finish(false,"V3 loadout visual missing");return; }
        try
        {
            switch(step)
            {
                case 0:
                    Check(loadout.Selected==PrimaryWeapon.Type08 && loadout.CanDeploy && visual.WeaponObject!=null,"Initial hangar equips the starter cannon");
                    Capture("01_STARTER_TYPE08");Click("EquipM7");break;
                case 1:
                    Check(loadout.Selected==PrimaryWeapon.M7 && visual.WeaponObject!=null,"M7 selection equips real model");
                    Grip(visual);
                    Capture("02_M7");Click("EquipGreatsword");break;
                case 2:
                    Check(loadout.Selected==PrimaryWeapon.Greatsword,"Greatsword replaces rifle");
                    Grip(visual);
                    Capture("03_RAIKEN");Click("EquipM14");break;
                case 3:
                    Check(loadout.Selected==PrimaryWeapon.M14,"M14 replaces greatsword");
                    Grip(visual);
                    Capture("04_M14");Click("UnequipButton");break;
                case 4:
                    Check(loadout.Selected==PrimaryWeapon.None && visual.WeaponObject==null,"Unequip removes the weapon");
                    gm.BeginRun();Check(gm.Phase==GamePhase.Hangar,"Unarmed deploy is blocked through gameplay API");
                    Click("EquipM7");break;
                case 5: Click("DeployButton");break;
                case 6:
                    Check(gm.IsCombatActive && loadout.Selected==PrimaryWeapon.M7,"Deploy retains M7 and enters combat");
                    Check(!loadout.Select(PrimaryWeapon.M14),"Loadout cannot change during combat");
                    Shot(player,18,38,0);Capture("05_M7_COMBAT");gm.RestartRun();break;
                case 7:
                    Check(loadout.Selected==PrimaryWeapon.Type08 && loadout.CanDeploy && visual.WeaponObject!=null,"Return to hangar restores the starter cannon");
                    Click("EquipM14");break;
                case 8: Click("DeployButton");break;
                case 9: Shot(player,46,48,1);gm.RestartRun();break;
                case 10: Click("EquipGreatsword");break;
                case 11: Click("DeployButton");break;
                case 12:
                    player.AimAt(player.transform.position+Vector3.forward*6+Vector3.up);
                    var dummy=GameObject.CreatePrimitive(PrimitiveType.Cube);dummy.name="AuditMeleeTarget";
                    dummy.transform.position=player.transform.position+Vector3.forward*1.8f+Vector3.up*1.1f;
                    target=dummy.AddComponent<Damageable>();target.team=1;target.RestoreLife(100,100);Physics.SyncTransforms();
                    Check(player.Melee.TryAttack(),"Selected greatsword starts real melee attack");
                    player.weaponController.TryFireBeam();
                    Check(!UnityEngine.Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None).Any(p=>p.team==0),"Greatsword does not fire invisible rifle rounds");break;
                case 13:
                    Check(target.CurrentHealth<100,"Greatsword damages a nearby target");
                    gm.RestartRun();break;
                default: Finish(errors.Count==0,null);return;
            }
            step++;next=EditorApplication.timeSinceStartup+1.2;
        }
        catch(Exception ex) { Finish(false,ex.ToString()); }
    }
    static void Finish(bool passed,string error)
    {
        if(error!=null) errors.Add(error);
        File.WriteAllText(Folder+"/runtime-report.json",JsonUtility.ToJson(new Report{passed=passed,checks=checks.ToArray(),errors=errors.ToArray(),editorErrors=editorErrors.ToArray(),editor=Application.unityVersion},true));
        SessionState.SetBool(Pending,false);EditorApplication.update-=Tick;Application.logMessageReceived-=Log;
        Debug.Log(passed?"HANGAR_RUNTIME_PASS":"HANGAR_RUNTIME_FAIL");
        EditorApplication.Exit(passed?0:1);
    }
}
