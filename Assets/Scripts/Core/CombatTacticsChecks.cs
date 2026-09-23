using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.AI;

// Actual production actors/projectiles. Protected player fixtures are separate from the full-input playtest.
public static class CombatTacticsChecks
{
    static void ClearShots(){foreach(var p in UnityEngine.Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None))p.Despawn();}
    static DamageInfo Hit(PlayerController p,Vector3 from,float impact=0)
    {return new DamageInfo(p.gameObject,from,p.GetComponent<Damageable>(),20){Impact=impact};}
    public static IEnumerator Run(GameManager gm,PlayerController p,string output,Action<bool,string> check,Action<string> capture)
    {
        // This contract specifically checks missile evasion. Exercise the restored
        // VALKYR's real SALVO; NEMESIS now has a beam-drone support contract of its own.
        if(p.Loadout.IsNemesis)
        {
            gm.ExitPractice();yield return null;
            check(p.GetComponent<PlayerMechLoader>().SelectHero(HeroMech.Valkyr),"select retained VALKYR for legacy SALVO evasion checks");
            for(int frame=0;frame<12;frame++)yield return null;
            gm.BeginP0Combat();for(int frame=0;frame<15;frame++)yield return null;p.enabled=false;
        }
        EnemyBase enemy=null;GameObject cover=null;
        var metrics=new List<string>();
        try
        {
            foreach(int fps in new[]{30,60,120})
            {
                Time.captureDeltaTime=1f/fps;yield return null;
                p.RestoreAt(new Vector3(0,.1f,0));p.GetComponent<Damageable>().SetInvulnerable(60);
                enemy=gm.stageManager.enemySpawner.SpawnEnemy(EnemyKind.Elite,new Vector3(0,0,6));
                var hp=enemy.GetComponent<Damageable>();hp.destroyOnDeath=false;hp.RestoreLife(5000,5000);
                enemy.enabled=false;enemy.GetComponent<NavMeshAgent>().enabled=false;
                enemy.transform.rotation=Quaternion.Euler(0,180,0);yield return null;
                float capacity=enemy.Armor.Maximum;
                var front=hp.ApplyDamage(20,Hit(p,enemy.transform.position+enemy.transform.forward*5));
                check(front.ArmorDamage==20&&front.HealthDamage==0&&hp.CurrentHealth==5000,"front hit damages finite armor at "+fps);
                hp.RestoreLife(5000,5000);
                var flank=hp.ApplyDamage(20,Hit(p,enemy.transform.position+enemy.transform.right*5));
                check(flank.ArmorDamage==front.ArmorDamage&&flank.HealthDamage==0,"all approach angles share finite armor at "+fps);
                enemy.ReceiveMeleeImpact(Vector3.forward,false,true);
                check(enemy.HitStaggerRemaining==0,"intact armor resists blade stagger at "+fps);
                var broken=hp.ApplyDamage(capacity,Hit(p,p.transform.position));
                check(broken.BrokeArmor&&!enemy.GuardActive&&hp.CurrentHealth==5000,"damage exhausts armor with no health overflow at "+fps);
                float before=hp.CurrentHealth;
                hp.ApplyDamage(20,new DamageInfo(p.gameObject,p.transform.position,p.GetComponent<Damageable>(),20){MeleeStrike=true});
                check(Mathf.Abs(before-hp.CurrentHealth-20)<.01f,"unarmored Elite takes base blade damage at "+fps);
                if(fps==60)capture("elite-armor-depleted.png");
                hp.RestoreLife(5000,5000);
                check(enemy.GuardActive&&enemy.Armor.Current==capacity,"pooled restore rearms finite armor at "+fps);
                UnityEngine.Object.Destroy(enemy.gameObject);enemy=null;yield return null;

                // Real telegraph must match the shot even when the player crosses its line during windup.
                enemy=gm.stageManager.enemySpawner.SpawnEnemy(EnemyKind.Elite,new Vector3(0,0,6));
                hp=enemy.GetComponent<Damageable>();hp.destroyOnDeath=false;hp.RestoreLife(5000,5000);
                enemy.transform.rotation=Quaternion.Euler(0,180,0);enemy.fireInterval=100;
                for(int frame=0;frame<fps*2&&!enemy.HasAttackWarning;frame++)yield return null;
                check(enemy.HasAttackWarning,"Elite begins visible committed shot at "+fps);
                Vector3 committed=enemy.CommittedShotDirection;Quaternion facing=enemy.transform.rotation;
                p.RestoreAt(new Vector3(5,.1f,0));p.GetComponent<Damageable>().SetInvulnerable(60);
                for(int frame=0;frame<fps*2&&enemy.ShotsCommitted==0;frame++)yield return null;
                Projectile fired=null;
                foreach(var shot in UnityEngine.Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None))if(shot.team==1){fired=shot;break;}
                check(fired!=null && Vector3.Angle(Vector3.ProjectOnPlane(fired.direction,Vector3.up),committed)<1,"actual shot follows announced line at "+fps);
                check(Quaternion.Angle(facing,enemy.transform.rotation)<1 && enemy.IsInRecovery && enemy.GuardActive,"shot recovery preserves finite armor without snapping toward player at "+fps);
                ClearShots();
                for(int frame=0;frame<fps*2&&enemy.IsInRecovery;frame++)yield return null;
                facing=enemy.transform.rotation;yield return null;
                check(Quaternion.Angle(facing,enemy.transform.rotation)<=CombatLoopV2.EliteTurnSpeed/fps+.3f,"Elite turn speed remains flankable at "+fps);
                UnityEngine.Object.Destroy(enemy.gameObject);enemy=null;yield return null;

                p.RestoreAt(new Vector3(0,.1f,0));p.GetComponent<Damageable>().SetInvulnerable(60);
                enemy=gm.stageManager.enemySpawner.SpawnEnemy(EnemyKind.Elite,new Vector3(0,0,7));
                hp=enemy.GetComponent<Damageable>();hp.destroyOnDeath=false;hp.RestoreLife(5000,5000);
                enemy.transform.rotation=Quaternion.Euler(0,180,0);enemy.fireInterval=100;
                for(int frame=0;frame<fps*3&&(enemy.ShotsCommitted==0||enemy.IsInRecovery);frame++)yield return null;
                ClearShots();
                // A wall blocks perception as well as the missile; threat alone cannot dodge through cover.
                cover=GameObject.CreatePrimitive(PrimitiveType.Cube);cover.name="TacticsCover";
                cover.transform.position=(p.transform.position+enemy.transform.position)*.5f+Vector3.up;
                cover.transform.localScale=new Vector3(8,4,.25f);Physics.SyncTransforms();
                check(!enemy.TryEvadeMissile(hp.AimCenter+enemy.transform.forward*4,-enemy.transform.forward),"missile behind solid cover cannot trigger evasion at "+fps);
                UnityEngine.Object.Destroy(cover);cover=null;yield return null;
                p.weaponController.ResetCooldowns();
                check(p.weaponController.TryFireSkill(hp),"actual SALVO launches at "+fps);
                for(int frame=0;frame<fps&&!enemy.IsEvading;frame++)yield return null;
                check(enemy.IsEvading && enemy.EvasionsStarted==1 && enemy.GuardActive,"incoming SALVO forces one bounded sidestep at "+fps);
                Vector3 evadeStart=enemy.transform.position;
                check(!hp.IsInvulnerable,"sidestep grants no invulnerability at "+fps);
                gm.SetPaused(true);Vector3 pausedAt=enemy.transform.position;
                for(int frame=0;frame<10;frame++)yield return null;
                check(Vector3.Distance(pausedAt,enemy.transform.position)<.02f && enemy.IsEvading,"pause freezes evasion movement and timer at "+fps);
                gm.SetPaused(false);
                for(int frame=0;frame<fps*2;frame++)yield return null;
                float displacement=Vector3.Distance(evadeStart,enemy.transform.position);
                check(displacement>.35f && displacement<3.5f && enemy.EvasionsStarted==1,"four missiles create one bounded reposition at "+fps+": "+displacement.ToString("F2"));
                check(!enemy.IsEvading && !enemy.IsInRecovery && enemy.GuardActive,"sidestep and recovery finish at "+fps);
                check(enemy.GetComponent<NavMeshAgent>().isOnNavMesh,"missile evasion stays on navigation mesh at "+fps);
                metrics.Add("FPS="+fps+" evade displacement="+displacement.ToString("F3")+" count="+enemy.EvasionsStarted);
                ClearShots();
                for(int frame=0;frame<fps*2;frame++)yield return null;
                Vector3 threat=hp.AimCenter-enemy.transform.forward*4;
                // A second real incoming missile allows interrupt/cleanup to be checked during movement.
                var missile=(MissileProjectile)ProjectilePool.Spawn(true,"TacticsMissile",threat,Color.yellow);
                missile.target=hp;missile.Init(0,p.GetComponent<Damageable>(),enemy.transform.forward,1,8,3,0,0);
                missile.SetImpact(20,CombatHitKind.Missile);
                for(int frame=0;frame<fps&&!enemy.IsEvading;frame++)yield return null;
                check(enemy.IsEvading,"evasion becomes available after cooldown at "+fps);
                hp.ApplyDamage(enemy.Armor.Current,Hit(p,p.transform.position));yield return null;
                check(!enemy.Armor.Intact && !enemy.IsEvading && !enemy.HasAttackWarning,"Armor depletion interrupts sidestep and clears warnings at "+fps);
                enemy.TrainingTarget=true;
                hp.TakeDamage(99999,Hit(p,p.transform.position));yield return null;
                check(hp.IsDead && !enemy.GuardActive && !enemy.IsEvading,"death clears armor and movement state at "+fps);
                ClearShots();UnityEngine.Object.Destroy(enemy.gameObject);enemy=null;yield return null;
            }
            File.WriteAllLines(Path.Combine(output,"tactics-metrics.txt"),metrics);
        }
        finally
        {
            gm.SetPaused(false);Time.captureDeltaTime=1f/60;
            if(enemy!=null)UnityEngine.Object.Destroy(enemy.gameObject);
            if(cover!=null)UnityEngine.Object.Destroy(cover);ClearShots();
        }
    }
}
