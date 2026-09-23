using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using UnityEngine;

public static class CombatAudioChecks
{
    public static IEnumerator Run(GameManager gm,PlayerController p,Action<bool,string> check)
    {
        var audio=GameAudio.Instance;
        var voices=(AudioSource[])typeof(GameAudio).GetField("voices",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(audio);
        var cues=(GameAudioCue[])typeof(GameAudio).GetField("voiceCues",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(audio);
        float effects=GamePreferences.Effects;
        try
        {
            GamePreferences.SetEffects(1);Time.captureDeltaTime=1f/60;
            var cadence=P0AudioCadenceChecks.Run(p,check);while(cadence.MoveNext())yield return cadence.Current;
            Time.captureDeltaTime=1f/60;yield return null;p.RestoreAt(new Vector3(0,.1f,0));p.stats.ResetStats();
            var hp=p.GetComponent<Damageable>();hp.RestoreLife(p.stats.MaxHp,p.stats.MaxHp);
            hp.TakeDamage(4,new DamageInfo(null,p.transform.position+Vector3.right*5,null,4));
            check(cues[9]==GameAudioCue.PlayerHit&&voices[9].clip!=null&&voices[9].panStereo==0,"actual player damage owns centered reserved impact voice");
            var playerClip=voices[9].clip;
            for(int n=0;n<6;n++)
            {
                GameAudio.PlayAt(GameAudioCue.Hit,p.transform.position+Vector3.right*8,.3f);
                GameAudio.Play(GameAudioCue.Dash,.3f);
                for(int frame=0;frame<5;frame++)yield return null;
            }
            check(cues[9]==GameAudioCue.PlayerHit&&voices[9].clip==playerClip,"enemy impacts and dash bursts cannot steal player-hit voice");
            check(cues[13]==GameAudioCue.Dash,"dash launch owns its reserved voice");
            float near=0,far=0;
            for(int pass=0;pass<2;pass++)
            {
                GameAudio.ResetCombatSound();
                float ready=Time.unscaledTime+.075f;
                while(Time.unscaledTime<ready)yield return null;
                for(int i=6;i<9;i++)voices[i].clip=null;
                GameAudio.PlayAt(GameAudioCue.Hit,p.transform.position+(pass==0?Vector3.right*2:Vector3.left*18),.5f);
                var impact=voices.Skip(6).Take(3).Single(v=>v.clip!=null);
                check(pass==0?impact.panStereo>0:impact.panStereo<0,"impact stereo side follows world source "+pass);
                if(pass==0)near=impact.volume;else far=impact.volume;
            }
            check(far<near*.5f,"distant impacts are quieter without muting nearby confirmation");
            p.RestoreAt(new Vector3(0,.1f,0));p.stats.ResetStats();
            for(int i=0;i<25;i++){p.Simulate(new PlayerCommand{Move=Vector2.up,BoostHeld=true},1f/60);yield return null;}
            check(p.IsBoosting&&audio.BoostLevel>.075f,"real held boost opens engine envelope");
            var engine=audio.GetComponentsInChildren<AudioSource>().Single(v=>v.gameObject.name=="BoostMotor");
            check(engine.clip!=null&&engine.loop&&engine.volume>0,"boost motor has existing engine asset and bounded gain");
            GamePreferences.SetEffects(0);check(engine.volume==0&&voices.All(v=>v.volume==0),"effects mute covers engine and all impact channels immediately");
            GamePreferences.SetEffects(1);
            gm.SetPaused(true);yield return null;check(audio.BoostLevel==0&&engine.volume==0,"pause clears boost sound envelope");gm.SetPaused(false);
            p.CancelMovement();for(int i=0;i<18;i++)yield return null;
            check(audio.BoostLevel==0&&engine.volume==0,"release leaves no continuing engine sound");
            var windup=audio.SliceClips(GameAudioCue.SwordWindup)[0];
            var light=audio.SliceClips(GameAudioCue.SwordHit)[0];
            var heavy=audio.SliceClips(GameAudioCue.SwordHitHeavy)[0];
            check(windup!=null&&light!=null&&heavy!=null&&light!=heavy,"authored preparation light contact and heavy contact clips remain distinct");
            GameAudio.Play(GameAudioCue.SwordCut3,.5f);yield return null;GameAudio.StopSwordSwings();
            float fadeEnd=Time.unscaledTime+.06f;while(Time.unscaledTime<fadeEnd)yield return null;
            check(voices.Skip(14).Where((v,i)=>cues[i+14]>=GameAudioCue.SwordCut1&&cues[i+14]<=GameAudioCue.SwordCut3).All(v=>v.volume==0),"canceled sword swing voice fades to zero");
        }
        finally{GamePreferences.SetEffects(effects);gm.SetPaused(false);p.CancelMovement();Time.captureDeltaTime=1f/60;}
    }
}
