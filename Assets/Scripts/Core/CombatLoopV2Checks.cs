using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class CombatLoopV2Checks
{
    static void ClearShots(){foreach(var shot in UnityEngine.Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None))shot.Despawn();}
    static void Drive(PlayerController p,Vector3 aim,bool fire=false,bool slash=false,bool dash=false,bool skill=false,Vector2 move=default)
    {p.Simulate(new PlayerCommand{HasAim=true,AimPoint=aim,Fire=fire,Melee=slash,Dash=dash,Skill=skill,Move=move},Time.deltaTime);}
    public static IEnumerator Run(GameManager gm,PlayerController p,string output,Action<bool,string> check,Action<string> capture)
    {
        if(p.Loadout.IsNemesis)
        {
            gm.ExitPractice();yield return null;
            check(p.GetComponent<PlayerMechLoader>().SelectHero(HeroMech.Valkyr),"legacy compatibility checks use retained VALKYR and real four-missile SALVO");
            for(int frame=0;frame<12;frame++)yield return null;
            gm.BeginP0Combat();for(int frame=0;frame<15;frame++)yield return null;p.enabled=false;
        }
        var source=p.GetComponent<Damageable>();
        var metrics=new List<string>{"V4 compatibility suite: real dash slash, two rifles, support, finite armor and pool reuse. Isolated fixtures protect the player."};
        BossController boss=null;GameObject fixture=null;
        try
        {
            foreach(int fps in new[]{30,60,120})
            {
                Time.captureDeltaTime=1f/fps;yield return null;
                p.RestoreAt(new Vector3(0,.1f,0));p.stats.ResetStats();source.RestoreLife(p.stats.MaxHp,p.stats.MaxHp);
                // Input on the first dash frame must survive the complete dash and draw transition.
                Drive(p,new Vector3(0,1,16),dash:true,slash:true,move:Vector2.up);yield return null;
                for(int i=0;i<fps/2&&!p.Melee.IsAttacking;i++){Drive(p,new Vector3(0,1,16));yield return null;}
                check(p.Melee.IsAttacking&&p.Melee.IsDashSlash,"buffered dash slash begins at "+fps+" FPS");
                float start=p.transform.position.z;
                for(int i=0;i<fps/2;i++){Drive(p,new Vector3(0,1,16));yield return null;}
                float advance=p.transform.position.z-start;
                check(advance>.85f&&advance<1.1f,"dash slash uses collision motor for 0.99m follow-through at "+fps+" FPS: "+advance);
                check(!source.IsInvulnerable,"dash slash grants no extra invulnerability at "+fps);
                p.RestoreAt(new Vector3(0,.1f,0));yield return null;
                check(!p.DashAttackReady&&!p.Melee.IsDashSlash,"restore clears dash attack opportunity");

            }
            var bossRules=RebuildBossChecks.Run(gm,p,check,capture);while(bossRules.MoveNext())yield return bossRules.Current;
            Time.captureDeltaTime=1f/60;yield return null;
            // Exercise both loadouts through their actual shot, stance and projectile paths.
            foreach(var weapon in new[]{PrimaryWeapon.M7,PrimaryWeapon.M14})
            {
                gm.EnterHangar();check(p.Loadout.Select(weapon),"select "+weapon);gm.BeginP0Combat();p.enabled=false;
                p.RestoreAt(new Vector3(0,.1f,0));p.stats.ResetStats();yield return null;
                check(p.Loadout.CanUseSword&&p.Loadout.CanUseRifle,"both gun and blade available with "+weapon);
                fixture=UnityEngine.Object.Instantiate(gm.stageManager.enemySpawner.elitePrefab,new Vector3(0,.1f,7),Quaternion.identity);
                var enemy=fixture.GetComponent<EnemyBase>();enemy.ConfigureP0Role(EnemyKind.Elite);enemy.TrainingTarget=true;enemy.Init(p.transform,null);enemy.enabled=false;
                var motion=fixture.GetComponent<E01SoldierMotion>();if(motion!=null)motion.TrainingTarget=true;
                var hp=fixture.GetComponent<Damageable>();hp.destroyOnDeath=false;hp.RestoreLife(5000,5000);
                int hitCount=0;DamageInfo last=null;hp.OnDamaged+=(target,info)=>{hitCount++;last=info;};
                yield return null;
                float started=Time.time;
                for(int i=0;i<600&&enemy.Armor.Intact;i++){Drive(p,hp.AimCenter,fire:true);yield return null;}
                check(!enemy.Armor.Intact&&hp.CurrentHealth==5000,"real "+weapon+" depletes armor without HP overflow");
                check(hitCount==Mathf.CeilToInt(CombatRules.Current.EliteCapacity/p.Loadout.Equipped.damage),"distinct shot count to break Elite with "+weapon+": "+hitCount);
                check(last.Kind==(weapon==PrimaryWeapon.M7?CombatHitKind.Rifle:CombatHitKind.HeavyRifle),"projectile retains weapon kind through pool");
                check(Mathf.Abs(last.Impact-(weapon==PrimaryWeapon.M7?CombatLoopV2.RifleImpact:CombatLoopV2.HeavyRifleImpact))<.001f,"explicit impact survives actual collision");
                metrics.Add(weapon+" Elite break hits="+hitCount+" seconds="+(Time.time-started).ToString("F3"));
                ClearShots();hp.RestoreLife(5000,5000);p.RestoreAt(new Vector3(0,.1f,0));yield return null;
                hp.ApplyDamage(18,new DamageInfo(p.gameObject,p.transform.position,source,18));
                hp.ApplyDamage(72,new DamageInfo(p.gameObject,p.transform.position,source,72));
                check(Mathf.Abs(enemy.Armor.Current-90)<.01f&&hp.CurrentHealth==5000,"gun and blade share the same finite damage layer");
                float end=Time.time+1.25f;while(Time.time<end)yield return null;
                check(Mathf.Abs(enemy.Armor.Current-90)<.01f,"disengagement does not regenerate armor");
                Drive(p,hp.AimCenter,slash:true);yield return null;
                for(int i=0;i<10&&!p.Melee.IsAttacking;i++){Drive(p,hp.AimCenter);yield return null;}
                check(p.Melee.IsAttacking,"blade is live before support command");
                p.AutoAim.Clear();Drive(p,hp.AimCenter,skill:true);yield return null;
                check(p.weaponController.SkillCooldownRemaining>9&&p.Melee.IsAttacking,"back cannons deploy from live melee without canceling blade");
                check(p.GetComponentInChildren<ValkyrBackCannon>().Active&&UnityEngine.Object.FindObjectsByType<MissileProjectile>(FindObjectsSortMode.None).Length==0,"back cannons replace the four missiles");
                int previousHits=hitCount;
                for(int i=0;i<100&&hitCount<previousHits+2;i++){Drive(p,hp.AimCenter);yield return null;}
                check(hitCount>=previousHits+2&&last.Kind==CombatHitKind.HeavyRifle,"two back cannon beams reach target through actual collision hits="+hitCount+" before="+previousHits+" cannon="+p.GetComponentInChildren<ValkyrBackCannon>().Hits+" kind="+last.Kind);
                ClearShots();hp.RestoreLife(100,100);hp.SetInvulnerable(.1f);
                hp.TakeDamage(1,new DamageInfo(p.gameObject,p.transform.position,source,1){Impact=999});
                check(hp.CurrentHealth==100&&enemy.Armor.Current==enemy.Armor.Maximum,"invulnerability rejects both HP and armor damage");
                hp.RestoreLife(100,100);
                var breakHit=hp.ApplyDamage(1000,new DamageInfo(p.gameObject,p.transform.position,source,1000));
                check(!hp.IsDead&&breakHit.BrokeArmor&&hp.CurrentHealth==100,"overpowered breaking hit cannot spill to health");
                var killHit=hp.ApplyDamage(1000,new DamageInfo(p.gameObject,p.transform.position,source,1000));
                check(hp.IsDead&&killHit.Lethal&&!killHit.BrokeArmor,"lethal follow-up emits death without a phantom armor break");
                ClearShots();UnityEngine.Object.Destroy(fixture);fixture=null;yield return null;
            }
            // A pooled heavy projectile must not pass its impact to the next enemy bullet.
            var pooled=ProjectilePool.Spawn(false,"V2PoolProbe",Vector3.zero,Color.white);
            pooled.Init(0,source,Vector3.forward,1,10,1,0,0);pooled.SetImpact(120,CombatHitKind.HeavyRifle,1.65f);pooled.Despawn();
            var reused=ProjectilePool.Spawn(false,"V2PoolProbe",Vector3.zero,Color.white);
            reused.Init(1,null,Vector3.forward,1,10,1,0,0);
            check(reused.Impact==-1&&reused.HitKind==CombatHitKind.Generic&&reused.PunishMultiplier==-1,"pool reuse clears all weapon payload fields");reused.Despawn();
            File.WriteAllLines(Path.Combine(output,"loop-metrics.txt"),metrics);
        }
        finally
        {
            gm.SetPaused(false);if(boss!=null)UnityEngine.Object.Destroy(boss.gameObject);if(fixture!=null)UnityEngine.Object.Destroy(fixture);
            ClearShots();Time.captureDeltaTime=1f/60;
        }
    }
}
