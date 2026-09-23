using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using Object=UnityEngine.Object;

public static class NemesisDronesChecks
{
    public static IEnumerator Run(GameManager gm,PlayerController p,Action<bool,string> check,Action<string> capture)
    {
        gm.ExitPractice();yield return null;gm.BeginWeaponTrial();yield return null;
        var trial=Object.FindFirstObjectByType<WeaponTrial>();trial.ClearTargets();yield return null;
        p.enabled=false;p.InputRouter.readKeyboard=false;p.RestoreAt(new Vector3(0,.1f,0));p.stats.ResetStats();
        var drones=p.GetComponentInChildren<NemesisDroneController>();var rig=p.GetComponentInChildren<NemesisMotionRig>();
        var docks=rig.Drones.Select(t=>t.localPosition).ToArray();
        void Command(Vector2 move=default,bool skill=false,bool fire=false,bool slash=false)
        {p.Simulate(new PlayerCommand{Move=move,Skill=skill,Fire=fire,Melee=slash,HasAim=true,AimPoint=p.transform.position+Vector3.forward*12},Time.deltaTime);}
        Command(skill:true);yield return null;
        check(drones.Active&&p.weaponController.SkillCooldownRemaining>0,"actual E input deploys support without a preselected enemy");
        for(int i=0;i<45;i++){Command();yield return null;}
        check(rig.Deployment>.95f&&rig.Drones.Select((t,i)=>Vector3.Distance(t.localPosition,docks[i])).All(d=>d>1),"all six original wing assemblies leave their docks");
        check(drones.ShotsFired==0&&Object.FindObjectsByType<MissileProjectile>(FindObjectsSortMode.None).Length==0,"no target means no blind shots or legacy homing missiles");
        p.weaponController.ResetCooldowns();
        check(p.weaponController.SkillCooldownRemaining>0&&!p.weaponController.TryFireSkill(null),"an occupied six-unit deployment cannot display ready or be duplicated by an early cooldown reset");
        var a=trial.SpawnTarget(new Vector3(-2,.1f,12),"",1000).GetComponent<Damageable>();
        var b=trial.SpawnTarget(new Vector3(5,.1f,10),"",1000).GetComponent<Damageable>();
        for(int i=0;i<90;i++){Command();yield return null;}
        check(a.CurrentHealth<1000&&b.CurrentHealth<1000&&drones.Hits>=6,"autonomous beams acquire and damage ordinary floor-level targets on both sides");
        check(Enumerable.Range(0,6).All(i=>drones.Target(i)==a||drones.Target(i)==b),"all six units track live enemies");
        var cam=Camera.main;var follow=cam.GetComponent<CameraFollow>();follow.enabled=false;
        cam.orthographic=true;cam.orthographicSize=9;cam.transform.position=p.transform.position+new Vector3(11,17,-15);cam.transform.LookAt(p.transform.position+new Vector3(0,1.6f,4));
        for(int i=0;i<70&&drones.VisibleBeams==0;i++){Command();yield return null;}
        check(drones.VisibleBeams>0,"real autonomous discharge has visible pooled beam layers");capture("drones-autofire-production.png");
        gm.SetPaused(true);float age=drones.Age;int shots=drones.ShotsFired;
        var positions=rig.Drones.Select(t=>t.position).ToArray();
        for(int i=0;i<10;i++)yield return null;
        check(age==drones.Age&&shots==drones.ShotsFired&&rig.Drones.Select((t,i)=>Vector3.Distance(t.position,positions[i])).All(d=>d<.0001f),"pause freezes flight, fire and support lifetime");gm.SetPaused(false);
        var previous=rig.Drones[0].position;var start=p.transform.position;
        for(int i=0;i<35;i++){Command(Vector2.right);yield return null;}
        check(Vector3.Distance(p.transform.position,start)>1&&Vector3.Distance(rig.Drones[0].position,previous)>1
            &&rig.Drones.All(t=>Vector3.Distance(t.position,p.transform.position)<7),"six support units follow a moving player and remain in formation");
        a.Kill(new DamageInfo(p.gameObject,p.transform.position,p.GetComponent<Damageable>(),9999));
        for(int i=0;i<25;i++){Command();yield return null;}
        check(Enumerable.Range(0,6).All(i=>drones.Target(i)==b),"destroyed target is abandoned and every unit reacquires the surviving enemy");
        int beams=rig.GetComponentsInChildren<LineRenderer>(true).Count(l=>l.name.StartsWith("Drone beam"));
        check(beams==6*NemesisDroneVfx.BeamLinesPerUnit,"beam geometry is a fixed pool of three beam layers and two ion filaments for each of six units");
        while(drones.Active){Command();yield return null;}
        yield return null;
        check(rig.Drones.Select((t,i)=>Vector3.Distance(t.localPosition,docks[i])).All(d=>d<.001f)&&drones.VisibleBeams==0,"timed support ends with exact docking and no lingering beams");

        // A real solid wall spans every formation position; beams must never hit through it.
        trial.ClearTargets();yield return null;p.RestoreAt(new Vector3(0,.1f,0));
        var blocked=trial.SpawnTarget(new Vector3(0,.1f,12),"",1000).GetComponent<Damageable>();
        var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.name="Drone check cover";
        wall.transform.position=new Vector3(0,2,6);wall.transform.localScale=new Vector3(20,6,.5f);Physics.SyncTransforms();
        p.weaponController.ResetCooldowns();Command(skill:true);yield return null;
        for(int i=0;i<120;i++){Command();yield return null;}
        check(blocked.CurrentHealth==1000,"solid cover blocks drone target acquisition and damage");
        Object.Destroy(wall);yield return null;
        for(int i=0;i<90;i++){Command();yield return null;}
        check(blocked.CurrentHealth<1000,"removing cover restores autonomous firing without another E press");
        blocked.transform.position=new Vector3(0,.1f,55);Physics.SyncTransforms();
        for(int i=0;i<30;i++){Command();yield return null;}
        check(Enumerable.Range(0,6).All(i=>drones.Target(i)==null),"targets outside support range are released");
        int events=0;Action shot=()=>events++;p.weaponController.BeamFired+=shot;
        Command(fire:true);yield return null;
        for(int i=0;i<18;i++){Command();yield return null;}
        Command(slash:true);yield return null;
        for(int i=0;i<12;i++){Command();yield return null;}
        check(events==1&&p.Melee.IsAttacking&&drones.Active,"independent support allows the player's rifle and sword actions");p.weaponController.BeamFired-=shot;
        p.GetComponent<Damageable>().Kill(new DamageInfo(blocked.gameObject,blocked.transform.position,blocked,9999));
        yield return null;yield return null;
        check(!drones.Active&&drones.VisibleBeams==0,"player death cancels the deployment and all beam flashes");
        gm.ExitPractice();for(int i=0;i<12;i++)yield return null;
        check(!drones.Active&&rig.Deployment==0&&!p.GetComponent<Damageable>().IsDead,"return to hangar restores live player and docked drones");
        gm.BeginWeaponTrial();yield return null;trial=Object.FindFirstObjectByType<WeaponTrial>();trial.ClearTargets();
        p.enabled=false;p.weaponController.ResetCooldowns();Command(skill:true);yield return null;
        check(drones.Active&&rig.GetComponentsInChildren<LineRenderer>(true).Count(l=>l.name.StartsWith("Drone beam"))==beams,"next run reuses the same bounded beam pool");
        gm.ExitPractice();for(int i=0;i<3;i++)yield return null;
        check(!drones.Active&&drones.VisibleBeams==0,"leaving combat cancels active support");follow.enabled=true;
        // Fast and slow update rates must not duplicate pulses or lose the final volley.
        foreach(int fps in new[]{30,120})
        {
            Time.captureDeltaTime=1f/fps;gm.BeginWeaponTrial();yield return null;
            trial=Object.FindFirstObjectByType<WeaponTrial>();trial.ClearTargets();yield return null;
            p.enabled=false;p.RestoreAt(new Vector3(0,.1f,0));p.stats.ResetStats();
            var target=trial.SpawnTarget(new Vector3(0,.1f,fps==30?4:12),"",1000).GetComponent<Damageable>();
            int before=drones.ShotsFired;p.weaponController.ResetCooldowns();Command(skill:true);yield return null;
            for(int i=0;i<Mathf.CeilToInt(NemesisDroneController.Duration*fps)+3;i++){Command();yield return null;}
            float budget=18*Mathf.Max(1,gm.equipmentLoop.Backpack.missiles)*p.stats.DamageMultiplier*gm.upgradeSystem.DamageMultiplier;
            check(drones.ShotsFired-before==36&&!drones.Active,$"{fps} FPS emits six pulses from each of six drones and completes recall");
            check(Mathf.Abs(1000-target.CurrentHealth-budget)<.1f,$"{fps} FPS autonomous beams preserve the support direct-damage budget: {1000-target.CurrentHealth:F2}");
            gm.ExitPractice();yield return null;
        }
        Time.captureDeltaTime=1f/60;
    }
}
