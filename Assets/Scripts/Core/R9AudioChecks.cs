using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

public static class R9AudioChecks
{
    public static IEnumerator Run(GameManager gm,PlayerController p,string output,Action<bool,string> check)
    {
        var a=GameAudio.Instance;var heard=new List<GameAudioCue>();
        Action<GameAudioCue> hear=c=>heard.Add(c);GameAudio.CuePlayed+=hear;
        GameObject enemyObject=null;float oldEffects=GamePreferences.Effects;
        var voices=(AudioSource[])typeof(GameAudio).GetField("voices",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(a);
        try
        {
            GamePreferences.SetEffects(1);GameAudio.ResetCombatSound();
            foreach(GameAudioCue cue in Enum.GetValues(typeof(GameAudioCue)))
            {
                var bank=a.SliceClips(cue);
                check(bank.Length>0&&bank.All(c=>c!=null&&c.loadState==AudioDataLoadState.Loaded),"R9 material loaded for "+cue);
                foreach(var clip in bank)check(clip.length<.65f,"bounded transient (no long prerecorded burst): "+clip.name);
            }
            var one=a.SliceClips(GameAudioCue.SwordCut1)[0];var two=a.SliceClips(GameAudioCue.SwordCut2)[0];var heavy=a.SliceClips(GameAudioCue.SwordCut3)[0];
            check(one!=two&&two!=heavy&&heavy.length>one.length,"three strokes use distinct fast/reverse/heavy materials");
            var previous=UnityEngine.Random.state;
            for(int i=0;i<3;i++){GameAudio.Play(GameAudioCue.Hit,.3f);for(int f=0;f<5;f++)yield return null;}
            // Variant choice has its own sequence rather than consuming gameplay randomness.
            GameAudio.ResetCombatSound();previous=UnityEngine.Random.state;
            GameAudio.Play(GameAudioCue.Hit,.3f);
            check(JsonUtility.ToJson(previous)==JsonUtility.ToJson(UnityEngine.Random.state),"sound variation does not change gameplay RNG");
            p.RestoreAt(new Vector3(0,.1f,0));p.stats.ResetStats();p.Stance.ResetStance();yield return null;
            p.weaponController.ResetCooldowns();p.AimAt(new Vector3(0,1,18));p.weaponController.TryFireBeam();
            var body=a.GetComponents<AudioSource>().First(v=>v.clip!=null&&a.SliceClips(GameAudioCue.RifleShot).Contains(v.clip)&&v.volume>0);
            float before=body.volume;
            enemyObject=UnityEngine.Object.Instantiate(gm.stageManager.enemySpawner.meleePrefab,new Vector3(0,.1f,3),Quaternion.identity);
            var enemy=enemyObject.GetComponent<EnemyBase>();enemy.ConfigureP0Role(EnemyKind.Melee);enemy.Init(p.transform,null);enemy.enabled=false;
            var hp=enemyObject.GetComponent<Damageable>();hp.destroyOnDeath=false;
            enemyObject.AddComponent<ArmorHealth>().Configure(20);
            hp.TakeDamage(20,new DamageInfo(p.gameObject,p.transform.position,p.GetComponent<Damageable>(),20){Kind=CombatHitKind.Rifle});
            check(heard.Contains(GameAudioCue.ArmorBreak)&&body.volume<before*.85f&&a.SliceBackgroundGain<.4f,"actual break immediately clears space in gun/body and background mix");
            var protectedClip=voices[22].clip;
            GameAudio.Play(GameAudioCue.Beam,.3f);
            var recoveredVoice=voices.First(v=>v.clip==a.SliceClips(GameAudioCue.Beam)[0]);
            for(int i=0;i<8;i++){GameAudio.PlayAt(GameAudioCue.EnemyShot,p.transform.position+Vector3.right*8,.5f);GameAudio.Play(GameAudioCue.Hit,.3f);yield return null;}
            check(voices[22].clip==protectedClip&&voices[22].volume>.3f,"enemy-fire saturation cannot steal or duck armor-break voice");
            check(recoveredVoice.clip==a.SliceClips(GameAudioCue.Beam)[0],"hostile gunfire cannot steal the recovered player rifle voice");
            for(int f=0;f<28;f++)yield return null;
            check(a.SliceBackgroundGain>.99f,"critical duck recovers without permanently muffling battle");
            UnityEngine.Object.Destroy(enemyObject);enemyObject=null;
            GameAudio.ResetCombatSound();heard.Clear();
            p.RestoreAt(new Vector3(0,.1f,0));yield return null;
            for(int f=0;f<50;f++){p.Simulate(new PlayerCommand{Move=Vector2.up,BoostHeld=true},Time.deltaTime);yield return null;}
            for(int f=0;f<20;f++){p.Simulate(new PlayerCommand(),Time.deltaTime);yield return null;}
            check(heard.Count(c=>c==GameAudioCue.BoostStart)==1&&heard.Count(c=>c==GameAudioCue.BoostStop)==1,"held boost has one ignition and one release, never a per-frame retrigger");
            check(a.BoostLevel==0,"released propulsion leaves no motor level");
            heard.Clear();p.RestoreAt(new Vector3(0,.1f,0));yield return null;
            for(int f=0;f<75;f++){p.Simulate(new PlayerCommand{Move=Vector2.up},Time.deltaTime);yield return null;}
            if(p.Loadout.IsNemesis)
                check(!heard.Contains(GameAudioCue.Footstep)&&a.BoostLevel>.015f&&p.GetComponentInChildren<ValkyrMotionDriver>().FlightBlend>.5f,"ordinary hover has propulsion sound and no false ground contacts");
            else check(heard.Contains(GameAudioCue.Footstep),"actual planted run feet produce mechanical ground contacts");
            p.CancelMovement();for(int f=0;f<30;f++)yield return null;heard.Clear();
            for(int f=0;f<30;f++)yield return null;
            check(!heard.Contains(GameAudioCue.Footstep),"stationary mech has no timer-driven footsteps");
            heard.Clear();p.RestoreAt(new Vector3(0,.1f,0));p.Stance.ResetStance();yield return null;
            for(int f=0;f<45;f++){p.Simulate(new PlayerCommand{Melee=f==0,HasAim=true,AimPoint=new Vector3(0,1,12)},Time.deltaTime);yield return null;}
            for(int f=0;f<30;f++){p.Simulate(new PlayerCommand{Fire=true,HasAim=true,AimPoint=new Vector3(0,1,12)},Time.deltaTime);yield return null;}
            check(heard.Count(c=>c==GameAudioCue.WeaponDraw)==1&&heard.Count(c=>c==GameAudioCue.WeaponStow)==1,"actual gun-sword-gun transfer triggers each mechanism once");
            check(!heard.Contains(GameAudioCue.SwordHit)&&!heard.Contains(GameAudioCue.SwordHitHeavy),"empty swing contains no fake contact event");
            heard.Clear();p.RestoreAt(new Vector3(0,.1f,0));yield return null;
            enemyObject=UnityEngine.Object.Instantiate(gm.stageManager.enemySpawner.meleePrefab,new Vector3(0,.1f,8),Quaternion.identity);
            enemy=enemyObject.GetComponent<EnemyBase>();enemy.ConfigureP0Role(EnemyKind.Ranged);enemy.Init(p.transform,null);
            for(int f=0;f<65;f++)yield return null;
            check(heard.Count(c=>c==GameAudioCue.EnemyShot)==1,"production ranged enemy emits one sound when its actual projectile leaves");
            heard.Clear();enemy.InterruptForStagger();
            for(int f=0;f<45;f++)yield return null;
            check(!heard.Contains(GameAudioCue.EnemyShot),"staggered enemy has no delayed ghost gunfire");
            UnityEngine.Object.Destroy(enemyObject);enemyObject=null;
            GameAudio.Play(GameAudioCue.SwordCut3,.8f);GameAudio.Play(GameAudioCue.ArmorFinish,.8f);
            GamePreferences.SetEffects(0);
            check(a.GetComponentsInChildren<AudioSource>().Where(v=>v.clip!=null&&v.clip.name!="Velocity_Overdrive").All(v=>v.volume==0),"effects slider mutes every new mechanism, threat, propulsion and tail channel");
            GamePreferences.SetEffects(1);gm.EnterHangar();yield return null;
            check(voices.All(v=>v.volume==0)&&a.BoostLevel==0,"hangar clears transient gains and engine from previous combat");
            gm.BeginP0Combat();yield return null;
        }
        finally
        {
            GameAudio.CuePlayed-=hear;GamePreferences.SetEffects(oldEffects);GameAudio.ResetCombatSound();
            if(enemyObject!=null)UnityEngine.Object.Destroy(enemyObject);
        }
    }

    // Each bank cue passes through the actual Unity Player mixer, never Python audio mixing.
    public static IEnumerator CaptureBank(string output,Action<bool,string> check)
    {
        var a=GameAudio.Instance;GameAudio.ResetCombatSound();
        var ambience=a.GetComponents<AudioSource>().First(v=>v.clip!=null&&v.clip.name=="spaceEngineLow_000");
        ambience.Stop();var native=new PlayerAudioCapture();AudioListener.pause=false;AudioListener.volume=1;
        float oldEffects=GamePreferences.Effects,oldMusic=GamePreferences.Music;
        GamePreferences.SetMusic(0);GamePreferences.SetEffects(1);
        int frame=0;var rows=new List<string>{"cue,start,end"};
        try
        {
            foreach(GameAudioCue cue in Enum.GetValues(typeof(GameAudioCue)))
            {
                GameAudio.ResetCombatSound();float start=frame/60f;
                GameAudio.Play(cue,.72f);
                for(int f=0;f<48;f++){yield return null;native.Advance(frame%2==1);frame++;}
                rows.Add(cue+","+start.ToString("F3",System.Globalization.CultureInfo.InvariantCulture)+","+(frame/60f).ToString("F3",System.Globalization.CultureInfo.InvariantCulture));
            }
            GamePreferences.SetEffects(0);GameAudio.Play(GameAudioCue.RifleShot);GameAudio.Play(GameAudioCue.ArmorBreak);
            for(int f=0;f<60;f++){yield return null;native.Advance(frame%2==1);frame++;}
            rows.Add("MUTED,"+(frame/60f-1).ToString("F3",System.Globalization.CultureInfo.InvariantCulture)+","+(frame/60f).ToString("F3",System.Globalization.CultureInfo.InvariantCulture));
        }
        finally
        {
            native.Save(Path.Combine(output,"native-cue-bank.wav"));AudioListener.volume=0;
            GamePreferences.SetMusic(oldMusic);GamePreferences.SetEffects(oldEffects);GameAudio.ResetCombatSound();
            File.WriteAllLines(Path.Combine(output,"native-cue-bank.csv"),rows);
        }
        check(native.SampleCount>0&&native.Peak>.05f&&native.Peak<.95f,"native cue bank captured with headroom; peak="+native.Peak);
    }
}
