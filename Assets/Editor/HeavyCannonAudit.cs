using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object = UnityEngine.Object;

[InitializeOnLoad]
public static class HeavyCannonAudit
{
    const string Pending = "HeavyCannonAudit.Pending";
    const string Folder = "art_prototypes/ZZ_HyperCannon_20260908/unity_evidence";
    static int step;
    static double next;
    static readonly List<string> checks = new List<string>();
    static readonly List<string> errors = new List<string>();
    static Damageable target;
    [Serializable] class Report { public bool passed; public string[] checks, errors; public string editor; }
    static HeavyCannonAudit()
    {
        if (!SessionState.GetBool(Pending, false)) return;
        EditorApplication.update += Tick;
        Application.logMessageReceived += Log;
        next = EditorApplication.timeSinceStartup + 3;
    }
    public static void Run()
    {
        Directory.CreateDirectory(Folder);
        EditorSceneManager.OpenScene("Assets/Scenes/Demo_Main.unity");
        SessionState.SetBool(Pending, true);
        EditorApplication.isPlaying = true;
    }
    static void Log(string text, string trace, LogType type)
    {
        if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) errors.Add(text + "\n" + trace);
    }
    static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
        checks.Add(message);
    }
    static void Click(string name)
    {
        var button = GameObject.Find(name)?.GetComponent<Button>();
        Check(button != null && button.interactable, "Available UI: " + name);
        ExecuteEvents.Execute(button.gameObject, new PointerEventData(EventSystem.current), ExecuteEvents.pointerClickHandler);
    }
    static void Capture(string name)
    {
        var camera = Camera.main;
        var canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).Where(c => c.isRootCanvas && c.renderMode == RenderMode.ScreenSpaceOverlay).ToArray();
        foreach (var c in canvases) { c.renderMode = RenderMode.ScreenSpaceCamera; c.worldCamera = camera; c.planeDistance = 1; }
        Canvas.ForceUpdateCanvases();
        JishenHeroIntegration.Capture(camera, Folder + "/" + name + ".png", 1600, 900);
        foreach (var c in canvases) { c.renderMode = RenderMode.ScreenSpaceOverlay; c.worldCamera = null; }
    }
    static void Tick()
    {
        if (!EditorApplication.isPlaying || EditorApplication.timeSinceStartup < next) return;
        var gm = GameManager.Instance;
        if (gm == null || gm.playerController == null) return;
        var p = gm.playerController;
        var l = p.Loadout;
        var v = p.GetComponentInChildren<LoadoutVisual>();
        try
        {
            switch (step)
            {
                case 0:
                    Check(l.Selected == PrimaryWeapon.Type08 && l.CanDeploy && v.WeaponObject != null, "Default hangar equips the ready-to-deploy TYPE08 cannon");
                    Check(l.Armory.weapons.Length == 4, "All four weapons retained in armory");
                    Click("EquipM7"); break;
                case 1:
                    Check(l.Selected == PrimaryWeapon.M7 && v.RightGripError < .06f && v.LeftGripError < .06f, "M7 still equips and both grips reach");
                    Click("EquipGreatsword"); break;
                case 2:
                    Check(l.Selected == PrimaryWeapon.Greatsword && v.RightGripError < .06f && v.LeftGripError < .06f, "Greatsword still equips and both grips reach");
                    Click("EquipM14"); break;
                case 3:
                    Check(l.Selected == PrimaryWeapon.M14 && v.RightGripError < .06f && v.LeftGripError < .06f, "M14 still equips and both grips reach");
                    Click("EquipType08"); break;
                case 4:
                    Check(l.Selected == PrimaryWeapon.Type08 && v.WeaponObject != null, "Cannon UI equips the actual model");
                    Check(l.Equipped.rightHandOnly && v.RightGripError < .015f, "Cannon right grip reaches: " + v.RightGripError);
                    Check(v.assembly.GetComponentsInChildren<Transform>(true).Any(t => t.name == "VALKYR_Model_Revision_V3_c7297dd9"), "Mounted on the correct VALKYR V3 body");
                    Check(v.WeaponObject.GetComponent<LODGroup>().GetLODs().Length == 3, "All three cannon LODs present");
                    Check(p.weaponController.muzzle == v.WeaponMuzzle, "Live firing origin follows cannon muzzle");
                    Capture("01_HANGAR_TYPE08");
                    Click("UnequipButton"); break;
                case 5:
                    Check(v.WeaponObject == null && !l.CanDeploy, "Cannon unequips cleanly and unarmed deploy is disabled");
                    Click("EquipType08"); break;
                case 6: Click("DeployButton"); break;
                case 7:
                    Check(gm.IsCombatActive && l.Selected == PrimaryWeapon.Type08, "Cannon selection survives deployment");
                    Check(v.RightGripError < .06f, "Right grip reaches in combat: " + v.RightGripError);
                    Check(!l.Select(PrimaryWeapon.M7), "Combat cannot change the hangar loadout");
                    Check(!p.Melee.TryAttack(), "Cannon does not trigger a hidden sword");
                    Capture("02_COMBAT_TYPE08");
                    Vector3 origin = v.WeaponMuzzle.position;
                    var dummy = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    dummy.name = "TYPE08_AuditTarget";
                    dummy.transform.position = origin + p.transform.forward * 5;
                    dummy.transform.localScale = new Vector3(1.2f, 3, 1.2f);
                    target = dummy.AddComponent<Damageable>(); target.team = 1; target.RestoreLife(1000, 1000);
                    Physics.SyncTransforms();
                    p.AimAt(target.AimCenter);
                    origin = v.WeaponMuzzle.position;
                    p.weaponController.ResetCooldowns(); p.weaponController.TryFireBeam();
                    var shots = Object.FindObjectsByType<MinovskyBeam>(FindObjectsSortMode.None);
                    Check(shots.Length == 1, "One pink particle beam charges at the cannon");
                    Check(Mathf.Abs(shots[0].Damage - 120) < .01f && shots[0].Penetration == 2 && Mathf.Abs(shots[0].BlastRadius - 1.8f) < .01f, "Cannon beam damage, penetration and blast match its profile");
                    Check(Vector3.Distance(shots[0].Origin, origin) < .01f, "Beam begins at the visible muzzle");
                    Check(!Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None).Any(s => s.team == 0), "Cannon no longer emits the orange projectile");
                    p.weaponController.TryFireBeam();
                    Check(Object.FindObjectsByType<MinovskyBeam>(FindObjectsSortMode.None).Length == 1, "Cannon cooldown prevents an immediate second beam");
                    break;
                case 8:
                    Check(target != null && target.CurrentHealth < 1000, "Cannon beam hits and damages the target");
                    Check(Object.FindObjectsByType<MinovskyBeam>(FindObjectsSortMode.None).Length == 0, "Beam glow and particle effect finish and clean up");
                    gm.RestartRun(); break;
                case 9:
                    Check(l.Selected == PrimaryWeapon.Type08 && l.CanDeploy && v.WeaponObject != null, "Returning to hangar restores the starter cannon");
                    Finish(); return;
            }
            step++;
            next = EditorApplication.timeSinceStartup + 1.2;
        }
        catch (Exception e) { errors.Add(e.ToString()); Finish(); }
    }
    static void Finish()
    {
        bool passed = errors.Count == 0;
        File.WriteAllText(Folder + "/runtime-report.json", JsonUtility.ToJson(new Report { passed = passed, checks = checks.ToArray(), errors = errors.ToArray(), editor = Application.unityVersion }, true));
        SessionState.SetBool(Pending, false);
        EditorApplication.update -= Tick;
        Application.logMessageReceived -= Log;
        Debug.Log(passed ? "TYPE08_RUNTIME_PASS" : "TYPE08_RUNTIME_FAIL");
        EditorApplication.Exit(passed ? 0 : 1);
    }
}
