using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Replaces the retired pressure/execution expectation with finite armor and free gun/blade damage.
public static class ArmorBreakChecks
{
    public static IEnumerator Run(GameManager gm,PlayerController p,Action<bool,string> check,Action<string> capture)
    {
        var cues=new List<GameAudioCue>();Action<GameAudioCue> hear=c=>cues.Add(c);GameAudio.CuePlayed+=hear;
        GameObject go=null;
        try
        {
            foreach(int fps in new[]{30,60,120})
            {
                Time.captureDeltaTime=1f/fps;p.RestoreAt(new Vector3(0,.1f,0));p.stats.ResetStats();yield return null;
                go=UnityEngine.Object.Instantiate(gm.stageManager.enemySpawner.meleePrefab,new Vector3(0,.1f,2.8f),Quaternion.identity);
                var enemy=go.GetComponent<EnemyBase>();enemy.ConfigureP0Role(EnemyKind.Melee);enemy.TrainingTarget=true;enemy.Init(p.transform,null);enemy.enabled=false;
                var motion=go.GetComponent<E01SoldierMotion>();motion.TrainingTarget=true;
                var hp=go.GetComponent<Damageable>();hp.destroyOnDeath=false;hp.RestoreLife(110,110);yield return null;
                for(int strike=0;strike<2;strike++)
                {
                    p.RestoreAt(new Vector3(0,.1f,0));
                    p.Simulate(new PlayerCommand{Melee=true,HasAim=true,AimPoint=go.transform.position},Time.deltaTime);
                    for(int i=0;i<fps/2;i++){p.Simulate(default,Time.deltaTime);yield return null;}
                    check(strike==0?Mathf.Abs(hp.CurrentHealth-32)<.01f:hp.IsDead,"ordinary soldier takes direct health damage and dies within two real cuts at "+fps+", strike="+strike+", hp="+hp.CurrentHealth);
                }
                UnityEngine.Object.Destroy(go);yield return null;
                go=UnityEngine.Object.Instantiate(gm.stageManager.enemySpawner.meleePrefab,new Vector3(0,.1f,3.4f),Quaternion.identity);
                enemy=go.GetComponent<EnemyBase>();enemy.ConfigureP0Role(EnemyKind.Elite);enemy.TrainingTarget=true;enemy.Init(p.transform,null);enemy.enabled=false;
                motion=go.GetComponent<E01SoldierMotion>();motion.TrainingTarget=true;hp=go.GetComponent<Damageable>();hp.destroyOnDeath=false;hp.RestoreLife(156,156);
                var armor=go.GetComponent<ArmorHealth>();armor.Configure(20);yield return null;
                int cracks=cues.FindAll(c=>c==GameAudioCue.ArmorBreak).Count;
                for(int i=0;i<fps*3&&armor.Intact;i++)
                {p.Simulate(new PlayerCommand{Fire=true,HasAim=true,AimPoint=go.transform.position},Time.deltaTime);yield return null;}
                check(!armor.Intact&&Mathf.Abs(hp.CurrentHealth-156)<.01f,"real rifle breaks armor with no breaking-hit overflow at "+fps);
                check(cues.FindAll(c=>c==GameAudioCue.ArmorBreak).Count==cracks+1,"one depleted armor layer emits one crack");
                foreach(var bullet in UnityEngine.Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None))bullet.Despawn();p.CancelMovement();
                for(int i=0;i<Mathf.CeilToInt(fps*.10f);i++)yield return null;
                check(motion.BreakPoseWeight>.1f,"finite armor break has a local recoil pose");
                gm.SetPaused(true);float pose=motion.BreakPoseWeight;
                for(int i=0;i<4;i++)yield return null;
                check(pose==motion.BreakPoseWeight&&!armor.Intact,"pause freezes break presentation without restoring armor");gm.SetPaused(false);
                if(fps==60)capture("v4-armor-break.png");
                var result=hp.ApplyDamage(20,new DamageInfo(p.gameObject,p.transform.position,p.GetComponent<Damageable>(),20){MeleeStrike=true});
                check(result.HealthDamage==20&&!result.Lethal&&!result.BrokeArmor,"post-armor blade deals base damage with no execution multiplier");
                float health=hp.CurrentHealth;
                for(int i=0;i<fps*2;i++)yield return null;
                check(!armor.Intact&&hp.CurrentHealth==health,"depleted armor never automatically regenerates");
                for(int i=0;i<fps*4&&!hp.IsDead;i++)
                {p.Simulate(new PlayerCommand{Fire=true,HasAim=true,AimPoint=go.transform.position},Time.deltaTime);yield return null;}
                check(hp.IsDead,"rifle independently finishes an unarmored heavy at "+fps);
                check(cues.FindAll(c=>c==GameAudioCue.ArmorFinish).Count==0,"no obsolete forced-execution sound is emitted");
                UnityEngine.Object.Destroy(go);go=null;yield return null;
            }
        }
        finally{gm.SetPaused(false);GameAudio.CuePlayed-=hear;if(go!=null)UnityEngine.Object.Destroy(go);Time.captureDeltaTime=1f/60;}
    }
}
