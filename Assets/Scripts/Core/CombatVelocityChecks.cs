using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.AI;

public static class CombatVelocityChecks
{
    public static IEnumerator Run(GameManager gm,PlayerController p,string output,Action<bool,string> check,Action<string> capture)
    {
        var cam=Camera.main;var follow=cam.GetComponent<CameraFollow>();
        var rows=new List<string>{"fps,speed,startupSeconds,oneSecondDistance,wing,hover"};
        foreach(var hero in new[]{HeroMech.Nemesis,HeroMech.Valkyr})
        {
            gm.EnterResult(false);gm.EnterHangar();for(int i=0;i<12;i++)yield return null;
            p.GetComponent<PlayerMechLoader>().SelectHero(hero);for(int i=0;i<25;i++)yield return null;
            var motion=p.GetComponentInChildren<ValkyrMotionDriver>();
            check(motion.MotionState=="Hangar/Ka","both hangar models use the dedicated grounded Ka pose: "+hero);
            check(motion.FeetAboveGround>-.06f&&motion.FeetAboveGround<.10f,"Ka pose soles meet the floor: "+hero+" "+motion.FeetAboveGround);
            check(Vector3.Distance(motion.LeftFoot.position,motion.RightFoot.position)>.85f,"Ka stance has an open stable silhouette: "+hero);
            var top=cam.WorldToViewportPoint(p.transform.position+Vector3.up*4.85f);
            var left=cam.WorldToViewportPoint(motion.LeftFoot.position);var right=cam.WorldToViewportPoint(motion.RightFoot.position);
            check(top.y<.97f&&left.y>.18f&&right.y>.18f,"hangar framing exposes the complete Ka silhouette above the bottom controls: "+hero);
            capture("velocity-hangar-"+hero+"-production.png");
        }
        gm.EnterResult(false);gm.EnterHangar();for(int i=0;i<10;i++)yield return null;
        p.GetComponent<PlayerMechLoader>().SelectHero(HeroMech.Nemesis);for(int i=0;i<20;i++)yield return null;
        gm.BeginWeaponTrial();for(int i=0;i<20;i++)yield return null;
        UnityEngine.Object.FindFirstObjectByType<WeaponTrial>().ClearTargets();yield return null;
        p.enabled=false;p.InputRouter.readKeyboard=false;
        var rig=p.GetComponentInChildren<NemesisMotionRig>();var driver=p.GetComponentInChildren<ValkyrMotionDriver>();
        void Command(Vector2 move=default,bool dash=false){p.Simulate(new PlayerCommand{Move=move,Dash=dash,HasAim=true,AimPoint=p.transform.position+Vector3.forward*15},Time.deltaTime);}
        foreach(int fps in new[]{30,60,120})
        {
            Time.captureDeltaTime=1f/fps;p.RestoreAt(new Vector3(0,.1f,-8));p.stats.ResetStats();
            for(int i=0;i<fps;i++){Command();yield return null;}
            float start=p.transform.position.z,reach=0;
            for(int i=0;i<fps;i++)
            {Command(Vector2.up);yield return null;if(reach==0&&p.Velocity.magnitude>=10)reach=(i+1f)/fps;}
            float distance=p.transform.position.z-start;
            check(reach>0&&reach<=.16f,"accelerates to 10 m/s within 160 ms at "+fps+" Hz: "+reach);
            check(distance>12.5f&&distance<13.6f,"actual one-second traversal matches fast motor at "+fps+" Hz: "+distance);
            check(rig.WingOpen>.75f&&driver.FlightBlend>.7f,"ordinary movement unfolds wings and uses propulsion posture at "+fps+" Hz");
            check(driver.FeetAboveGround>.15f,"hover pose has real floor clearance at "+fps+" Hz");
            rows.Add(FormattableString.Invariant($"{fps},{p.Velocity.magnitude:F3},{reach:F4},{distance:F3},{rig.WingOpen:F3},{driver.FlightBlend:F3}"));
            if(fps==60)
            {
                follow.enabled=false;cam.orthographicSize=4.2f;cam.transform.position=p.transform.TransformPoint(new Vector3(6,4,-9));cam.transform.LookAt(p.transform.position+Vector3.up*2.5f);
                capture("velocity-movement-wings.png");follow.enabled=true;
            }
            for(int i=0;i<fps/5;i++){Command(Vector2.down);yield return null;}
            check(p.Velocity.z< -9f,"rapid reversal changes actual travel direction at "+fps+" Hz");
            for(int i=0;i<fps;i++){Command();yield return null;}
            check(p.Velocity.sqrMagnitude<.001f&&rig.WingOpen==0,"braking then delayed wing closure settle fully at "+fps+" Hz");
        }
        Time.captureDeltaTime=1f/60;p.RestoreAt(new Vector3(-3,.1f,0));follow.enabled=true;
        for(int i=0;i<60;i++){Command();yield return null;}
        check(Mathf.Abs(cam.orthographicSize-17)<.01f,"production camera converges to requested size 17");
        var go=UnityEngine.Object.Instantiate(gm.stageManager.enemySpawner.meleePrefab,new Vector3(3,.1f,2),Quaternion.identity);
        var enemy=go.GetComponent<EnemyBase>();enemy.Init(p.transform,null);enemy.ConfigureP0Role(EnemyKind.Ranged);enemy.TrainingTarget=true;
        for(int i=0;i<5;i++)yield return null;enemy.enabled=false;
        var soldier=go.GetComponent<E01SoldierMotion>();var capsule=go.GetComponent<CapsuleCollider>();var nav=go.GetComponent<NavMeshAgent>();
        check(Mathf.Abs(capsule.height-4.5f)<.01f&&Mathf.Abs(nav.height-4.5f)<.01f,"white chassis collider and navigation match full-size body");
        check(Mathf.Abs(soldier.visual.localScale.x-4.5f/2.8f)<.001f,"white chassis complete visual hierarchy is enlarged coherently");
        check(capsule.radius>.8f&&nav.radius>=.8f,"scaled bodies have matching collision and navigation clearance");
        check(go.GetComponent<WorldHealthBar>().height>4.5f,"health bar is above the scaled helmet");
        check(soldier.GripError<.025f,"scaled hands retain contact with actual weapon grip: "+soldier.GripError);
        Bounds body=default;bool hasBody=false;
        foreach(var renderer in soldier.ArmorRenderers)
        {if(!hasBody){body=renderer.bounds;hasBody=true;}else body.Encapsulate(renderer.bounds);}
        float heroHeight=p.GetComponentInChildren<NemesisRaikenBlade>().bodyHeight;
        check(hasBody&&body.size.y/heroHeight>.9f&&body.size.y/heroHeight<1.08f,"actual white armor height matches NEMESIS body: "+body.size.y+" / "+heroHeight);
        capture("velocity-scale-camera-production.png");
        foreach(var cue in new[]{GameAudioCue.RifleShot,GameAudioCue.EnemyShot,GameAudioCue.SwordCut1,GameAudioCue.SwordHitHeavy,GameAudioCue.Dash})
        {
            var clips=GameAudio.Instance.SliceClips(cue);
            check(clips.Length==3&&Array.TrueForAll(clips,c=>c.frequency==48000&&c.length<.5f),"processed short 48 kHz variants loaded for "+cue);
        }
        check(GameAudio.Instance.MusicLoaded,"new original battle loop loaded");
        UnityEngine.Object.Destroy(go);File.WriteAllLines(Path.Combine(output,"velocity-metrics.csv"),rows);
    }
}
