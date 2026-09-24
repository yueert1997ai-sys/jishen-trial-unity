using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using Object=UnityEngine.Object;

public static class NemesisDroneVfxChecks
{
    public static IEnumerator Run(GameManager gm,PlayerController p,Action<bool,string> check,Action<string> capture)
    {
        gm.ExitPractice();yield return null;gm.BeginWeaponTrial();yield return null;
        var trial=Object.FindFirstObjectByType<WeaponTrial>();trial.ClearTargets();yield return null;
        p.enabled=false;p.InputRouter.readKeyboard=false;p.RestoreAt(new Vector3(0,.1f,0));p.stats.ResetStats();
        var drones=p.GetComponentInChildren<NemesisDroneController>();var vfx=drones.Visuals;
        var camera=Camera.main;var follow=camera.GetComponent<CameraFollow>();follow.enabled=false;
        void View(bool production=false)
        {
            camera.orthographic=true;camera.orthographicSize=production?CameraFollow.StandardCombatSize:9;
            if(production){camera.transform.rotation=Quaternion.Euler(68,0,0);camera.transform.position=p.transform.position-camera.transform.forward*24;}
            else {camera.transform.position=new Vector3(11,17,-15);camera.transform.LookAt(new Vector3(0,1.6f,4));}
        }
        void Command(Vector2 move=default,bool skill=false,bool boost=false)
        {p.Simulate(new PlayerCommand{Move=move,Skill=skill,BoostHeld=boost,HasAim=true,AimPoint=p.transform.position+Vector3.forward*12},Time.deltaTime);}
        View();
        var renderers=vfx.GetComponentsInChildren<Renderer>(true).Where(r=>r.name.StartsWith("Drone ")).ToArray();
        var rendererIds=renderers.Select(r=>r.GetInstanceID()).OrderBy(id=>id).ToArray();
        var particles=vfx.GetComponentsInChildren<ParticleSystem>().Where(ps=>ps.name.StartsWith("Drone ")).ToArray();
        check(renderers.All(r=>r.sharedMaterial!=null&&r.sharedMaterial.shader.isSupported),"all drone VFX shaders are supported in the actual Windows Player");
        check(particles.Length==5&&particles.Sum(ps=>ps.main.maxParticles)==NemesisDroneVfx.ParticleLimit,"all six drones share five particle systems capped at 456 live particles");
        check(renderers.OfType<LineRenderer>().Count()==6*NemesisDroneVfx.LinesPerUnit,"beam, filament and compression/contact geometry has a fixed 42-line pool");

        // Pure visual emission must not consume the global combat random sequence.
        var state=UnityEngine.Random.state;float expected=UnityEngine.Random.value;UnityEngine.Random.state=state;
        vfx.Fire(0,drones.Muzzle(0),new Vector3(0,1,10),true);vfx.Tick(0,drones.Muzzle(0),Vector3.forward,.8f,1f/60);
        check(UnityEngine.Random.value==expected,"particle variation does not perturb combat's random stream");UnityEngine.Random.state=state;
        vfx.Clear();check(vfx.ActiveParticles==0&&vfx.VisibleRenderers==0,"clearing a discharge removes beam geometry and every particle layer");

        var a=trial.SpawnTarget(new Vector3(-2,.1f,12),"",1000).GetComponent<Damageable>();
        var b=trial.SpawnTarget(new Vector3(5,.1f,10),"",1000).GetComponent<Damageable>();
        p.weaponController.ResetCooldowns();int shots=drones.ShotsFired,discharges=vfx.Discharges;
        Command(skill:true);yield return null;
        bool charged=false,aligned=true;int peakParticles=0,peakBeams=0;float chargeAge=0;
        for(int frame=0;frame<126;frame++)
        {
            Command();yield return null;
            if(vfx.ChargingUnits>0&&drones.ShotsFired==shots&&!charged&&drones.Age>=.40f){charged=true;chargeAge=drones.Age;capture("drone-charge.png");}
            for(int i=0;i<6;i++)if(vfx.IsBeamVisible(i))aligned&=Vector3.Distance(vfx.BeamOrigin(i),drones.Muzzle(i))<.002f;
            peakParticles=Mathf.Max(peakParticles,vfx.ActiveParticles);peakBeams=Mathf.Max(peakBeams,vfx.VisibleBeams);
            if(frame>=12&&frame%3==0)capture("sequence-"+((frame-12)/3).ToString("D3")+".png");
            if(frame==43)capture("drone-volley.png");
            if(frame==59)capture("drone-afterglow.png");
        }
        check(charged&&chargeAge<NemesisDroneVfx.ChargeTime+.48f,"muzzle charge is visible before the existing first damage pulse");
        check(aligned,"every active beam starts at its real moving muzzle with less than 2 mm separation");
        check(vfx.Discharges-discharges==drones.ShotsFired-shots&&vfx.Impacts>0,"one visual discharge per gameplay pulse; contact effects come from confirmed impacts");
        check(peakBeams>=2&&peakParticles>30&&peakParticles<=NemesisDroneVfx.ParticleLimit,
            $"staggered overlapping volley is visible within the particle budget: beams={peakBeams}, particles={peakParticles}");

        while(vfx.VisibleBeams==0){Command();yield return null;}
        gm.SetPaused(true);var ages=Enumerable.Range(0,6).Select(vfx.PulseAge).ToArray();
        var psTimes=particles.Select(ps=>ps.time).ToArray();int pausedParticles=vfx.ActiveParticles;
        for(int f=0;f<12;f++)yield return null;
        check(Enumerable.Range(0,6).All(i=>ages[i]==vfx.PulseAge(i))&&particles.Select((ps,i)=>Mathf.Abs(ps.time-psTimes[i])).All(d=>d<.0001f)
            &&pausedParticles==vfx.ActiveParticles,"pause freezes beam envelopes, charge and particle lifetimes");gm.SetPaused(false);
        // Keep floor-level targets on screen at the actual battle camera scale.
        a.transform.position=new Vector3(-2,.1f,7);b.transform.position=new Vector3(5,.1f,8);Physics.SyncTransforms();
        View(true);
        bool movingAligned=true,boostCaptured=false;
        for(int f=0;f<110;f++)
        {
            Command(new Vector2(f%60<30?1:-1,0),boost:true);yield return null;View(true);
            for(int i=0;i<6;i++)if(vfx.IsBeamVisible(i))movingAligned&=Vector3.Distance(vfx.BeamOrigin(i),drones.Muzzle(i))<.002f;
            if(f>=25&&vfx.VisibleBeams>=2&&!boostCaptured){capture("drone-boost-production.png");boostCaptured=true;}
            if(f>=42&&f<=87&&f%3==0)capture("boost-"+((f-42)/3).ToString("D3")+".png");
        }
        check(movingAligned,"boosting player and flying drones keep live beam starts on their muzzles");
        check(boostCaptured,"actual battle camera captures overlapping fire while Space boost is active");
        while(drones.Active){Command();yield return null;}
        check(vfx.VisibleRenderers==0&&vfx.ActiveParticles==0,"timed recall clears all beams, coronae, rings and vapor");
        float health=a.CurrentHealth+b.CurrentHealth;for(int f=0;f<30;f++)yield return null;
        check(health==a.CurrentHealth+b.CurrentHealth,"lingering ion trails never apply additional damage");

        for(int run=0;run<3;run++)
        {
            p.RestoreAt(new Vector3(0,.1f,0));p.weaponController.ResetCooldowns();Command(skill:true);yield return null;
            for(int f=0;f<55;f++){Command();yield return null;}
            check(drones.Active&&vfx.ActiveParticles>0,"repeat deployment produces real contact effects "+run);
            if(run==0)drones.Cancel();
            else if(run==1)p.RestoreAt(new Vector3(0,.1f,0));
            else {gm.ExitPractice();yield return null;}
            check(!drones.Active&&vfx.ActiveParticles==0&&vfx.VisibleRenderers==0,"cancel/reset/return clears every VFX layer "+run);
        }
        check(vfx.GetComponentsInChildren<Renderer>(true).Where(r=>r.name.StartsWith("Drone ")).Select(r=>r.GetInstanceID()).OrderBy(id=>id).SequenceEqual(rendererIds),
            "repeated discharges reuse the original renderer pool without adding objects");
        follow.enabled=true;
    }
}
