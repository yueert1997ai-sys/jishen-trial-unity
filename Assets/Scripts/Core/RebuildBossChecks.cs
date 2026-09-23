using System;
using System.Collections;
using UnityEngine;

public static class RebuildBossChecks
{
    public static IEnumerator Run(GameManager gm,PlayerController p,Action<bool,string> check,Action<string> capture)
    {
        gm.stageManager.StopStage();p.GetComponent<Damageable>().SetInvulnerable(1000);
        foreach(int fps in new[]{30,60,120})
        {
            Time.captureDeltaTime=1f/fps;p.RestoreAt(new Vector3(0,.1f,-4));yield return null;
            var boss=gm.stageManager.enemySpawner.SpawnBoss();boss.enabled=false;
            var hp=boss.GetComponent<Damageable>();hp.destroyOnDeath=false;
            check(boss.Stability==null&&!boss.UsesPlayerBreak,"Boss uses attack recovery instead of shared posture at "+fps+" FPS");
            foreach(var pattern in new[]{BossPattern.Scatter,BossPattern.Mortar,BossPattern.Charge})
            {
                check(boss.StartPattern(pattern),"Boss starts authored "+pattern+" pattern at "+fps);
                yield return null;var direction=boss.LockedDirection;var impact=boss.LockedImpact;
                float armored=hp.ApplyDamage(18,new DamageInfo(p.gameObject,p.transform.position,p.GetComponent<Damageable>(),18){Kind=CombatHitKind.Rifle}).HealthDamage;
                for(int i=0;i<fps/2;i++)yield return null;
                p.RestoreAt(new Vector3(8,.1f,-4));
                for(int i=0;i<fps*7&&!boss.CoreExposed;i++)yield return null;
                check(boss.CoreExposed&&boss.LockedDirection==direction&&boss.LockedImpact==impact,"advertised attack stays locked and opens a recovery window: "+pattern+" / "+fps);
                var gun=hp.ApplyDamage(18,new DamageInfo(p.gameObject,p.transform.position,p.GetComponent<Damageable>(),18){Kind=CombatHitKind.Rifle});
                var sword=hp.ApplyDamage(78,new DamageInfo(p.gameObject,p.transform.position,p.GetComponent<Damageable>(),78){Kind=CombatHitKind.Melee,MeleeStrike=true});
                check(Mathf.Abs(gun.HealthDamage-18)<.01f&&Mathf.Abs(sword.HealthDamage-78)<.01f&&armored>0&&armored<18,"both weapons independently benefit from exposed core: "+pattern+" / "+fps);
                if(fps==60&&pattern==BossPattern.Charge)capture("v4-boss-recovery.png");
                for(int i=0;i<fps*4&&boss.ActionRunning;i++)yield return null;
                check(!boss.ActionRunning&&!boss.CoreExposed,"Boss closes recovery once before selecting another move");
            }
            hp.SetCurrentHealth(hp.maxHealth*.45f);boss.enabled=true;yield return null;boss.enabled=false;
            check(boss.IsPhaseTwo,"health threshold activates phase two at an action boundary");
            hp.Kill(new DamageInfo(p.gameObject,p.transform.position,p.GetComponent<Damageable>(),99999));
            for(int i=0;i<fps;i++)yield return null;
            int hostileShots=0;foreach(var shot in UnityEngine.Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None))if(shot.team==1)hostileShots++;
            check(hostileShots==0&&gm.stageManager.enemySpawner.PendingSpawns==0,"Boss death cancels remaining hostile attacks and spawn reservations");
            if(boss!=null)UnityEngine.Object.Destroy(boss.gameObject);gm.stageManager.StopStage();yield return null;
        }
        Time.captureDeltaTime=1f/60;
    }
}
