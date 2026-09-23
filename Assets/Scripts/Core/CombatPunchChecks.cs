using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEngine;
using Object=UnityEngine.Object;

public static class CombatPunchChecks
{
    static void Lethal(EnemyBase e,PlayerController p)
    {
        var hp=e.GetComponent<Damageable>();
        hp.ApplyDamage(10000,new DamageInfo(p.gameObject,p.transform.position,p.GetComponent<Damageable>(),10000)
            {Kind=CombatHitKind.Rifle,HasContact=true,ContactPoint=hp.AimCenter,ContactNormal=Vector3.back});
    }
    static EnemyBase Spawn(GameManager gm,Vector3 position)
    {var e=gm.stageManager.enemySpawner.SpawnEnemy(EnemyKind.Ranged,position);e.target=null;return e;}
    public static IEnumerator Run(GameManager gm,PlayerController p,Action<bool,string> check,Action<string> capture)
    {
        var rules=CombatRules.Current;
        check(rules.Encounters.Sum(b=>b.Roles.Count)==34&&rules.MaxHostiles==5,"default trial reduced from 69 to 34 enemies, occupancy cap reduced from seven to five");
        check(EncounterCatalog.Rooms.Select(r=>r.Beats.Sum(b=>b.Roles.Count)).SequenceEqual(new[]{27,28,30,30,32,32}),"six mission rooms reduced to 27/28/30/30/32/32 enemies");
        foreach(int fps in new[]{30,60,120})
        {
            float dt=1f/fps;var flow=new EncounterFlow(rules,false);
            while(flow.Groups==0)flow.Tick(dt,0,false);
            for(int i=0;i<fps/4;i++)flow.Tick(dt,4,false);
            float start=flow.Elapsed;
            // Three defeated units leave one survivor; the next group now fits.
            while(flow.Groups<2&&flow.Elapsed-start<1)flow.Tick(dt,1,false);
            check(flow.Groups==2&&flow.Elapsed-start<=.18f+dt*2,$"{fps} FPS slot release queues reinforcement in about 0.18 seconds");
            check(rules.SpawnWarning>=.30f&&flow.Elapsed-start+rules.SpawnWarning<.60f,"reinforcement arrives within 0.6 seconds while retaining readable entry warning");
            int groups=flow.Groups;for(int i=0;i<fps*4;i++)flow.Tick(dt,5,false);
            check(flow.Groups==groups,"kill refill never bypasses the live plus reserved enemy cap");
        }
        gm.ExitPractice();yield return null;gm.BeginWeaponTrial();yield return null;
        var trial=Object.FindFirstObjectByType<WeaponTrial>();trial.ClearTargets();yield return null;
        p.enabled=false;p.InputRouter.readKeyboard=false;p.RestoreAt(new Vector3(0,.1f,-2));
        var camera=Camera.main;var follow=camera.GetComponent<CameraFollow>();follow.enabled=false;
        camera.orthographic=true;camera.orthographicSize=4.4f;camera.transform.position=new Vector3(7,6,-6);camera.transform.LookAt(new Vector3(0,1.3f,4));
        var e=Spawn(gm,new Vector3(0,.1f,4));yield return null;
        var hp=e.GetComponent<Damageable>();int kills=gm.Kills,occupied=gm.stageManager.EnemiesAlive;
        capture("death-before.png");Lethal(e,p);var fx=MechDeathVfx.Instance;
        int deaths=fx.Deaths,bursts=fx.MainBursts,secondary=fx.SecondaryBursts;
        check(hp.IsDead&&gm.Kills==kills+1&&gm.stageManager.EnemiesAlive==occupied-1,"death awards exactly one kill and frees its spawn slot immediately");
        check(e.GetComponents<Collider>().All(c=>!c.enabled),"dying mech cannot block player motion or later shots");
        hp.ApplyDamage(10000,null);check(gm.Kills==kills+1&&fx.Deaths==deaths,"corpse cannot replay death or award another kill");
        for(int frame=0;frame<78;frame++)
        {
            yield return null;
            if(frame%2==0)capture("death-"+(frame/2).ToString("D3")+".png");
            if(frame==7)
            {
                gm.SetPaused(true);int a=fx.MainBursts,b=fx.SecondaryBursts,particles=fx.ActiveParticles;
                var pose=e.GetComponent<E01SoldierMotion>().DeathProgress;
                for(int n=0;n<12;n++)yield return null;
                check(a==fx.MainBursts&&b==fx.SecondaryBursts&&particles==fx.ActiveParticles&&pose==e.GetComponent<E01SoldierMotion>().DeathProgress,"pause freezes both the dying body and delayed explosions");gm.SetPaused(false);
            }
        }
        check(e==null&&fx.MainBursts==bursts+1&&fx.SecondaryBursts==secondary+1,"one main and one secondary blast; source body is cleaned up");
        check(ArmorContactVfx.Instance.DetachedPieces>=4,"death throws recognizable head, shoulder and backpack mesh parts");
        for(int i=0;i<70;i++)yield return null;
        check(fx.ActiveParticles==0&&fx.ActiveSequences==0,"smoke and sparks finish without lingering effects");
        int objectCount=fx.GetComponentsInChildren<ParticleSystem>().Length;
        var crowd=new EnemyBase[5];for(int i=0;i<5;i++)crowd[i]=Spawn(gm,new Vector3((i-2)*1.6f,.1f,4));
        yield return null;foreach(var actor in crowd)Lethal(actor,p);
        camera.orthographicSize=8;camera.transform.position=new Vector3(9,13,-11);camera.transform.LookAt(new Vector3(0,1,3));
        for(int i=0;i<13;i++)yield return null;capture("five-mech-explosions.png");
        check(fx.ActiveSequences<=MechDeathVfx.Capacity&&fx.ActiveParticles<=MechDeathVfx.ParticleLimit&&fx.GetComponentsInChildren<ParticleSystem>().Length==objectCount,"five concurrent deaths reuse bounded systems without growing the pool");
        gm.ExitPractice();yield return null;yield return null;
        check(fx.ActiveSequences==0&&fx.ActiveParticles==0&&ArmorContactVfx.Instance.ActivePieces==0,"returning to hangar clears pending blasts, smoke and flying armor");
        follow.enabled=true;
    }
    [Serializable] sealed class Timing {public string phase;public float meanMs,p95Ms,maxMs;public int frames,peakParticles;}
    [Serializable] sealed class Report {public string method="1600x900 4x MSAA, fixed 60-Hz simulation plus GPU-synchronized offscreen render. One live five-enemy scene versus five simultaneous deaths; not displayed FPS.";public Timing[] phases;}
    public static IEnumerator Performance(GameManager gm,PlayerController p,string output,Action<bool,string> check)
    {
        gm.ExitPractice();yield return null;gm.BeginWeaponTrial();yield return null;
        Object.FindFirstObjectByType<WeaponTrial>().ClearTargets();yield return null;p.enabled=false;p.InputRouter.readKeyboard=false;
        var cam=Camera.main;var follow=cam.GetComponent<CameraFollow>();follow.enabled=false;
        cam.orthographic=true;cam.orthographicSize=9;cam.transform.position=new Vector3(10,16,-14);cam.transform.LookAt(new Vector3(0,1,4));
        var old=cam.targetTexture;var active=RenderTexture.active;var rt=new RenderTexture(1600,900,24){antiAliasing=4};rt.Create();
        var pixel=new Texture2D(1,1,TextureFormat.RGB24,false);var results=new List<Timing>();
        QualitySettings.vSyncCount=0;Application.targetFrameRate=-1;
        try
        {
            foreach(bool explode in new[]{false,true})
            {
                var crowd=new EnemyBase[5];for(int i=0;i<5;i++)crowd[i]=Spawn(gm,new Vector3((i-2)*1.6f,.1f,4));
                for(int i=0;i<45;i++)yield return null;
                var samples=new List<float>();long stamp=0;int peak=0;
                for(int frame=0;frame<121;frame++)
                {
                    long now=Stopwatch.GetTimestamp();if(frame>0)samples.Add((float)((now-stamp)*1000.0/Stopwatch.Frequency));stamp=now;
                    if(frame==1&&explode)foreach(var e in crowd)Lethal(e,p);
                    yield return null;cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;pixel.ReadPixels(new Rect(800,450,1,1),0,0);RenderTexture.active=active;
                    if(MechDeathVfx.Instance!=null)peak=Mathf.Max(peak,MechDeathVfx.Instance.ActiveParticles);
                }
                samples.Sort();results.Add(new Timing{phase=explode?"five simultaneous mech deaths":"five live mechs",meanMs=samples.Average(),p95Ms=samples[(int)(samples.Count*.95f)],maxMs=samples.Last(),frames=samples.Count,peakParticles=peak});
                foreach(var e in crowd)if(e!=null)Object.Destroy(e.gameObject);
                if(MechDeathVfx.Instance!=null)MechDeathVfx.Instance.Clear();ArmorContactVfx.Get().Clear();yield return null;
            }
            File.WriteAllText(Path.Combine(output,"death-performance.json"),JsonUtility.ToJson(new Report{phases=results.ToArray()},true));
            check(results.All(r=>r.frames==120&&r.meanMs>0)&&results[1].peakParticles>100,"measured real five-mech explosion rendering with GPU synchronization");
        }
        finally {cam.targetTexture=old;RenderTexture.active=active;rt.Release();Object.Destroy(rt);Object.Destroy(pixel);follow.enabled=true;gm.ExitPractice();}
    }
}
