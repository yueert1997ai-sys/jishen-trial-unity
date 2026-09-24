using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

// Identical input/fixtures before and after. Stationary 1000-HP production actors isolate motion, not difficulty.
public static class MeleeFeelChecks
{
    static void Drive(PlayerController p,bool slash=false,Vector2 move=default,bool turn=false)
    {p.Simulate(new PlayerCommand{Melee=slash,Move=move,HasAim=true,AimPoint=p.transform.position+(turn?Vector3.right:Vector3.forward)*15},Time.deltaTime);}
    public static IEnumerator Run(GameManager gm,PlayerController p,string output,Action<bool,string> check,Action<string> capture)
    {
        bool baseline=CombatRuntime.HasArgument("-feelBaseline");
        var rows=new List<string>{"fps,nextCutHeading,heldTravel,contactPoseDrift,contactAudioDelayMs,rifleThenMeleeStagger,rifleGripRecoil"};
        var events=new List<string>{"fps,frame,time,event,stage,elapsed,x,z"};
        var motion=p.GetComponentInChildren<ValkyrMotionDriver>();var blade=p.GetComponentInChildren<RaikenBladePresentation>();
        GameObject actor=null;PlayerAudioCapture mix=null;
        try
        {
            foreach(int fps in new[]{30,60,120})
            {
                Time.captureDeltaTime=1f/fps;gm.stageManager.StopStage();p.RestoreAt(new Vector3(0,.1f,0));yield return null;
                float heading=-999;
                for(int frame=0;frame<fps*2;frame++)
                {
                    bool turn=p.Melee.IsAttacking&&(p.Melee.ComboStage>0||p.Melee.AttackElapsed>=.16f);
                    Drive(p,frame==0||p.Melee.IsAttacking&&p.Melee.ComboStage==0&&p.Melee.AttackElapsed>.10f,turn:turn);
                    yield return null;
                    events.Add($"{fps},{frame},{Time.time:F4},aim-{p.Stance.State}-{p.Melee.IsAttacking},{p.Melee.ComboStage},{p.Melee.AttackElapsed:F4},0,0");
                    if(p.Melee.IsAttacking&&p.Melee.ComboStage==1&&p.Melee.AttackElapsed>=p.Melee.CurrentStroke.contactStart)
                    {heading=Vector3.SignedAngle(Vector3.forward,p.Melee.AttackForward,Vector3.up);capture("feel-turn-"+fps+".png");break;}
                }
                check(heading!=-999,"next cut reached for retarget comparison at "+fps+" heading="+heading);
                if(!baseline)check(heading>20&&heading<65,"next cut retargets within the 360 recovery / 540 preparation rate limits at "+fps);
                p.RestoreAt(new Vector3(0,.1f,0));yield return null;
                actor=UnityEngine.Object.Instantiate(gm.stageManager.enemySpawner.meleePrefab,new Vector3(0,.1f,3.2f),Quaternion.identity);
                var enemy=actor.GetComponent<EnemyBase>();enemy.ConfigureP0Role(EnemyKind.Melee);enemy.TrainingTarget=true;enemy.Init(p.transform,null);enemy.enabled=false;
                var hp=actor.GetComponent<Damageable>();hp.destroyOnDeath=false;hp.RestoreLife(1000,1000);
                var targetMotion=actor.GetComponent<E01SoldierMotion>();targetMotion.TrainingTarget=true;
                yield return null;
                int tick=0;float contactAt=-1,audioAt=-1,travel=0,drift=-1;Vector3 hitTip=Vector3.zero;
                Action<Damageable,float> hit=(target,amount)=>{if(target!=hp||contactAt>=0)return;contactAt=Time.time;hitTip=blade.tip.position;};
                Action<GameAudioCue> cue=c=>{if(c==GameAudioCue.SwordHit||c==GameAudioCue.SwordHitHeavy)audioAt=Time.time;
                    events.Add(FormattableString.Invariant($"{fps},{tick},{Time.time:F4},{c},{p.Melee.ComboStage},{p.Melee.AttackElapsed:F4},{p.transform.position.x:F4},{p.transform.position.z:F4}"));};
                p.Melee.StrikeHit+=hit;GameAudio.CuePlayed+=cue;
                if(fps==60){mix=new PlayerAudioCapture();AudioListener.volume=1;}
                try
                {
                    for(tick=0;tick<fps;tick++)
                    {
                        bool held=p.Melee.ImpactHeld;Vector3 before=p.transform.position;
                        Drive(p,tick==0,Vector2.up);
                        if(held)travel+=Vector3.Distance(before,p.transform.position);
                        yield return null;
                        if(mix!=null)mix.Advance(tick%2==1);
                        if(contactAt>=0&&drift<0){drift=Vector3.Distance(blade.tip.position,hitTip);capture("feel-contact-"+fps+".png");}
                    }
                }
                finally{p.Melee.StrikeHit-=hit;GameAudio.CuePlayed-=cue;if(mix!=null){mix.Save(Path.Combine(output,"contact-native.wav"));mix=null;}}
                check(contactAt>=0&&audioAt>=0,"actual swept blade produces a real contact and sound at "+fps);
                if(!baseline){check(travel<.005f,"contact hold stops player skating at "+fps);check(drift<.08f,"presented blade holds its actual contact sample at "+fps);}
                check(Mathf.Abs(audioAt-contactAt)<.001f,"contact sound and committed hit share one Player frame at "+fps);
                p.CancelMovement();hp.RestoreLife(1000,1000);enemy.ConfigureP0Role(EnemyKind.Melee);
                for(int i=0;i<fps/2;i++)yield return null;
                var source=p.GetComponent<Damageable>();var position=actor.transform.position;
                hp.ApplyDamage(1,new DamageInfo(null,position-Vector3.right*5,source,1){Kind=CombatHitKind.Rifle});
                for(int i=0;i<Mathf.CeilToInt(.065f*fps);i++)yield return null;
                var gripBefore=targetMotion.rifle.position;
                var result=hp.ApplyDamage(1,new DamageInfo(p.gameObject,position-Vector3.right*5,source,1){Kind=CombatHitKind.Melee,MeleeStrike=true,HasContact=true,ContactPoint=hp.AimCenter,ContactTangent=Vector3.right,ContactNormal=Vector3.left});
                yield return null;
                float gripMotion=Vector3.Distance(gripBefore,targetMotion.rifle.position);
                if(!baseline){check(result.Staggered,"rifle immunity cannot suppress a subsequent blade reaction at "+fps);check(gripMotion>.025f,"carried rifle and arms follow the struck torso at "+fps);}
                rows.Add(FormattableString.Invariant($"{fps},{heading:F3},{travel:F5},{drift:F5},{(audioAt-contactAt)*1000:F3},{result.Staggered},{gripMotion:F5}"));
                capture("feel-reaction-"+fps+".png");UnityEngine.Object.Destroy(actor);actor=null;yield return null;
            }
        }
        finally
        {
            if(mix!=null)mix.Save(Path.Combine(output,"contact-incomplete.wav"));if(actor!=null)UnityEngine.Object.Destroy(actor);
            Time.captureDeltaTime=1f/60;p.RestoreAt(new Vector3(0,.1f,0));
            File.WriteAllLines(Path.Combine(output,"feel-metrics.csv"),rows);File.WriteAllLines(Path.Combine(output,"contact-events.csv"),events);
        }
    }
}
