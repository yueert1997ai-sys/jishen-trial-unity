using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

public static class CombatSliceChecks
{
    public static IEnumerator Run(GameManager gm, PlayerController p, string output, Action<bool,string> check, Action<string> capture)
    {
        check(CombatSliceSettings.Enabled,"slice explicitly selected");
        Time.captureDeltaTime=1f/60;
        var cam=Camera.main;var follow=cam.GetComponent<CameraFollow>();
        if(CombatSliceSettings.Enabled)
        {
            p.enabled=false;
            p.RestoreAt(new Vector3(0,.1f,-4));
            var health=p.GetComponent<Damageable>();var motion=p.GetComponentInChildren<ValkyrMotionDriver>();
            health.RestoreLife(p.stats.MaxHp,p.stats.MaxHp);
            health.TakeDamage(10,new DamageInfo(null,p.transform.position+Vector3.right*5,null,10));
            yield return null;yield return null;
            check(motion!=null&&motion.HitReactionWeight>.1f,"actual player damage drives authored armor recoil");
            capture("r2_player_hit.png");
            for(int i=0;i<24;i++)yield return null;
            check(motion!=null&&motion.HitReactionWeight<.001f,"armor recoil settles without drift");
            p.RestoreAt(new Vector3(0,.1f,-4));health.RestoreLife(p.stats.MaxHp,p.stats.MaxHp);
            check(p.TryDash(Vector2.right),"R2 dash starts");
            float hp=health.CurrentHealth;
            health.TakeDamage(10,new DamageInfo(null,p.transform.position+Vector3.right*5,null,10));
            check(health.CurrentHealth==hp&&motion.HitReactionWeight<.001f,"dash iframe rejects damage and phantom recoil");
            p.RestoreAt(new Vector3(0,.1f,-4));
            for(int i=0;i<35;i++){p.Simulate(new PlayerCommand{Move=Vector2.up,BoostHeld=true},1f/60);yield return null;}
            check(p.IsBoosting&&p.Velocity.magnitude>11f,"held boost reaches 1.6x travel speed in actual arena");
            for(int i=0;i<10;i++){p.Simulate(new PlayerCommand{Move=Vector2.down,BoostHeld=true},1f/60);yield return null;}
            check(p.Velocity.z<0,"reverse boost changes travel direction within 167ms");
            OverdriveVfx.Clear();OverdriveVfx.Dash(p.transform.position,Vector3.forward);OverdriveVfx.Hit(p.transform.position,Vector3.up,true,true);OverdriveVfx.Break(p.transform.position);OverdriveVfx.Death(p.transform.position);
            yield return null;
            var rings=GameObject.Find("Pressure rings")?.GetComponent<ParticleSystem>();
            check(rings!=null&&rings.particleCount==0,"dash heavy hit break death emit no decorative rings");
            OverdriveVfx.Clear();p.RestoreAt(new Vector3(0,.1f,-4));health.RestoreLife(p.stats.MaxHp,p.stats.MaxHp);
        }

        var rows=new List<string>{"view,size,pitch,lookAhead,moveSpeed,boostMultiplier,dashDistance,dashDuration,dashCooldown,invulnerability,energyCost,zeroBuff"};
        var positions=new[]{new Vector3(0,.1f,-4),new Vector3(0,.1f,0),new Vector3(22,.1f,0),new Vector3(0,.1f,22),new Vector3(-22,.1f,0),new Vector3(0,.1f,-22)};
        for(int view=0;view<3;view++)
        {
            CombatSliceSettings.SelectView(view);
            foreach(var position in positions)
            {
                p.RestoreAt(position);
                for(int i=0;i<65;i++)yield return null;
                var point=cam.WorldToViewportPoint(p.transform.position+Vector3.up);
                check(point.z>0&&point.x>.05f&&point.x<.95f&&point.y>.05f&&point.y<.95f,$"view {view} player visible at {position}");
                check(Mathf.Abs(cam.orthographicSize-CameraFollow.StandardCombatSize)<.01f,"ordinary combat keeps selected C size despite legacy comparison selection");
                check(Mathf.Abs(cam.transform.eulerAngles.x-68)<.05f,"pitch unchanged between variants");
                if(position.x==0&&position.z==0)
                {
                    foreach(var direction in new[]{Vector3.forward,Vector3.back,Vector3.right,Vector3.left})
                    {
                        var landing=cam.WorldToViewportPoint(p.transform.position+direction*5);
                        check(landing.z>0&&landing.x>0&&landing.x<1&&landing.y>0&&landing.y<1,"5m cardinal dash destination visible at arena center");
                    }
                    capture("view_"+view+"_center.png");
                }
            }
            rows.Add(string.Format(CultureInfo.InvariantCulture,"{0},{1},{2},{3},{4},1.6,{5},{6},{7},0.16,25,{8}",view,cam.orthographicSize,cam.transform.eulerAngles.x,follow.movementLookAhead,p.stats.MoveSpeed,p.stats.DashDistance,p.dashDuration,p.stats.DashCooldown,gm.upgradeSystem.Count==0));
        }
        File.WriteAllLines(Path.Combine(output,"runtime-camera.csv"),rows);
        gm.SetPaused(true);Vector3 cameraBefore=cam.transform.position;float sizeBefore=cam.orthographicSize;
        for(int i=0;i<12;i++)yield return null;
        check(Vector3.Distance(cameraBefore,cam.transform.position)<.001f&&Mathf.Abs(sizeBefore-cam.orthographicSize)<.001f,"pause keeps camera stable");
        gm.SetPaused(false);
        gm.EnterHangar();for(int i=0;i<65;i++)yield return null;
        gm.BeginLiquidBossChallenge();
        for(int i=0;i<180;i++)yield return null;
        var boss=UnityEngine.Object.FindFirstObjectByType<BossController>();
        check(boss!=null,"existing liquid boss challenge remains available");
        check(Mathf.Abs(cam.orthographicSize-20)<.01f,"boss framing keeps the same requested camera 20");
        capture("boss_camera.png");
        gm.EnterHangar();for(int i=0;i<5;i++)yield return null;
        var demo=new GameObject("SliceDirectorCheck").AddComponent<P0CombatDemo>();
        for(int i=0;i<4;i++)yield return null;
        p.enabled=false;
        check(gm.upgradeSystem.Count==0&&p.Loadout.CanUseSword&&p.Loadout.CanUseRifle,"slice resets boss buffs and carries M7 plus sword");
        int peak=0;int spawned=0;
        // Structural regression uses a protected player and explicit kills. It is not a difficulty replay.
        for(int frame=0;frame<8000&&!demo.Finished;frame++)
        {
            p.GetComponent<Damageable>().SetInvulnerable(1);
            peak=Mathf.Max(peak,gm.stageManager.EnemiesAlive);
            if(frame%60==0)
            {
                foreach(var e in UnityEngine.Object.FindObjectsByType<EnemyBase>(FindObjectsSortMode.None))
                    e.GetComponent<Damageable>().TakeDamage(10000,new DamageInfo(p.gameObject,p.transform.position,p.GetComponent<Damageable>(),10000));
                if(demo.SliceBoss!=null&&!demo.SliceBoss.GetComponent<Damageable>().IsDead)
                {check(demo.SliceBoss.GetComponent<FazzBossController>()!=null,"short battle ends with first-tier FAZZ");demo.SliceBoss.GetComponent<Damageable>().Kill(new DamageInfo(p.gameObject,p.transform.position,p.GetComponent<Damageable>(),99999));}
            }
            if(gm.stageManager.EnemiesAlive==0&&gm.equipmentLoop.Absorption.Offering)
                check(gm.equipmentLoop.Absorption.CollectWithoutInstalling(),"structural replay explicitly continues after salvaging the offered weapon");
            spawned=Mathf.Max(spawned,demo.SliceSpawned);
            yield return null;
        }
        check(demo.Finished&&demo.SliceWon&&gm.LastResultVictory,"all fixed groups cleared reaches victory");
        check(spawned==34&&peak<=CombatRules.Current.MaxHostiles,"fixed encounter groups and bounded live plus reserved enemies");
        demo.Restart();yield return null;
        check(!demo.Finished&&demo.SliceGroup==0&&gm.upgradeSystem.Count==0,"restart resets director, zero buffs and victory flag");
        gm.SetPaused(true);float elapsed=demo.Elapsed;for(int i=0;i<30;i++)yield return null;
        check(demo.Elapsed==elapsed,"pause stops slice timer");gm.SetPaused(false);
        // The ordinary short fight has no failure deadline. Only the explicit 30-second lab is timed.
        for(int frame=0;frame<8000&&!demo.Finished;frame++)
        {p.GetComponent<Damageable>().SetInvulnerable(1);yield return null;}
        check(!demo.Finished&&!demo.SliceWon&&gm.IsCombatActive,"ordinary fight neither wins nor fails merely because the legacy time cap elapsed");
        demo.Restart();yield return null;
        p.GetComponent<Damageable>().RestoreLife(p.stats.MaxHp,p.stats.MaxHp);
        p.GetComponent<Damageable>().Kill(new DamageInfo(null,p.transform.position,null,999));yield return null;
        check(gm.Phase==GamePhase.Result&&!demo.SliceWon,"death reaches defeat");
        demo.Restart();yield return null;check(gm.IsCombatActive&&!p.GetComponent<Damageable>().IsDead,"restart after death works");
        File.WriteAllText(Path.Combine(output,"test-boundaries.txt"),"Structural test only: fixed 60Hz simulation; protected player, forced enemy kills for completion; isolated save. Boss entry grants six buffs by existing design, then verifies slice clears them. No human feel or audio approval.\n");
    }
}
