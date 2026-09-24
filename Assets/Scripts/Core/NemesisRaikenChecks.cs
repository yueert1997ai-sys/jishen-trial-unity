using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public static class NemesisRaikenChecks
{
    public static IEnumerator Run(GameManager gm, PlayerController p, string output, Action<bool,string> check, Action<string> capture)
    {
        var blade = p.GetComponentInChildren<RaikenBladePresentation>();
        var fit = blade.bladeRoot.GetComponent<NemesisRaikenBlade>();
        var motion = p.GetComponentInChildren<ValkyrMotionDriver>();
        check(fit != null && fit.sourceBlade.Contains("VALKYR_V9"), "NEMESIS equips the original VALKYR anti-ship blade variant");
        check(Mathf.Abs(blade.Reach-fit.fittedReach)<.005f && Mathf.Abs(fit.fullLength/fit.bodyHeight-1)<.001f, "full blade length equals the actual NEMESIS body height");
        var meshFilters = blade.bladeRoot.GetComponentsInChildren<MeshFilter>(true);
        var meshes = meshFilters.Select(m => m.sharedMesh.vertices).ToArray();
        var axis=(blade.tip.position-blade.grip.position).normalized;
        var lengths=meshFilters.SelectMany((m,k)=>meshes[k].Select(v=>Vector3.Dot(m.transform.TransformPoint(v)-blade.grip.position,axis))).ToArray();
        float actualLength=lengths.Max()-lengths.Min();
        check(Mathf.Abs(actualLength-fit.bodyHeight)<.01f,"mounted geometry matches mech height: "+actualLength.ToString("F3")+" / "+fit.bodyHeight.ToString("F3"));
        var colors = blade.bladeRoot.GetComponentsInChildren<MeshRenderer>(true).SelectMany(r => r.sharedMaterials).Distinct().ToArray();
        check(colors.All(m => m.name.StartsWith("NEMESIS graphite") && m.color.maxColorComponent - Mathf.Min(m.color.r,m.color.g,m.color.b) < .12f), "all blade surfaces use independent neutral gray materials");
        check(colors.All(m => m.GetColor("_EmissionColor").maxColorComponent < .001f), "steel edge is gray rather than the original cyan glow");
        var cam = Camera.main; var follow = cam.GetComponent<CameraFollow>();
        void View() { follow.enabled=false; cam.orthographic=true; cam.orthographicSize=4.1f; cam.transform.position=p.transform.TransformPoint(new Vector3(7,4,10)); cam.transform.LookAt(p.transform.TransformPoint(new Vector3(0,2.6f,.3f))); }
        void Command(bool slash=false, Vector2 move=default, bool boost=false) { p.Simulate(new PlayerCommand { Melee=slash, Move=move, BoostHeld=boost, HasAim=true, AimPoint=p.transform.position+Vector3.forward*16 }, Time.deltaTime); }
        float Bottom() { float low=float.PositiveInfinity; for(int k=0;k<meshes.Length;k++) foreach(var v in meshes[k]) low=Mathf.Min(low,meshFilters[k].transform.TransformPoint(v).y); return low; }
        var rows = new List<string> { "fps,stage,elapsed,gripError,lowestVertex,tipY,gripY" };
        gm.EnterResult(false); gm.EnterHangar(); for(int i=0;i<20;i++) yield return null;
        View(); capture("01-hangar-raiken.png");
        float hangarBottom=Bottom();
        gm.BeginWeaponTrial(); for(int i=0;i<15;i++) yield return null;
        UnityEngine.Object.FindFirstObjectByType<WeaponTrial>().ClearTargets(); yield return null;
        p.enabled=false; p.InputRouter.readKeyboard=false;
        float minimum=float.PositiveInfinity, maxGrip=0;
        foreach(int fps in new[]{30,60,120})
        {
            Time.captureDeltaTime=1f/fps; p.RestoreAt(new Vector3(0,.1f,0));
            for(int i=0;i<fps/2;i++) { Command(); yield return null; }
            var seen=new bool[3]; Command(true); yield return null;
            for(int i=0;i<fps*3;i++)
            {
                Command(p.Melee.IsAttacking && p.Melee.ComboStage<2 && p.Melee.AttackElapsed>.08f); yield return null;
                float bottom=Bottom(); minimum=Mathf.Min(minimum,bottom); maxGrip=Mathf.Max(maxGrip,motion.RightGripError);
                rows.Add(FormattableString.Invariant($"{fps},{p.Melee.ComboStage},{p.Melee.AttackElapsed:F5},{motion.RightGripError:F6},{bottom:F5},{blade.tip.position.y:F5},{blade.grip.position.y:F5}"));
                if(p.Melee.IsAttacking) seen[p.Melee.ComboStage]=true;
                if(fps==60 && i%3==0) { View(); capture("combo-"+i.ToString("000")+".png"); }
                if(!p.Melee.IsAttacking && seen[2]) break;
            }
            check(seen.All(x=>x) && !p.Melee.IsAttacking, "complete three cuts and recovery at "+fps+" FPS");
        }
        Time.captureDeltaTime=1f/60;
        for(int i=0;i<35;i++) { Command(); yield return null; }
        View(); capture("02-ready-raiken.png");
        gm.SetPaused(true); var paused=blade.tip.position;
        for(int i=0;i<8;i++) yield return null;
        check(Vector3.Distance(paused,blade.tip.position)<.00001f, "pause freezes the mounted long blade"); gm.SetPaused(false);
        follow.enabled=true; for(int i=0;i<20;i++) { Command(); yield return null; } capture("03-production-raiken.png");
        File.WriteAllLines(Path.Combine(output,"raiken-pose.csv"),rows);
        File.WriteAllText(Path.Combine(output,"raiken-metrics.txt"),FormattableString.Invariant($"fullLength={actualLength:F5}\nbodyHeight={fit.bodyHeight:F5}\nhangarBottom={hangarBottom:F5}\ncomboBottom={minimum:F5}\nmaxGripError={maxGrip:F6}\nreach={blade.Reach:F5}\n"));
        check(maxGrip<.015f,"real hilt stays seated throughout the long-blade combo");
        check(hangarBottom>=-.02f,"stowed anti-ship blade clears the hangar floor: "+hangarBottom);
        check(minimum>=-.04f,"anti-ship blade geometry clears the floor through swings and recovery: "+minimum);
    }
}
