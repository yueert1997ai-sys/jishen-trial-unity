using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class P0AudioCadenceChecks
{
    public static IEnumerator Run(PlayerController p,Action<bool,string> check)
    {
        var shots=new List<float>();var sounds=new List<float>();
        Action onShot=()=>shots.Add(Time.time);
        Action<GameAudioCue> onSound=c=>{if(c==GameAudioCue.RifleShot)sounds.Add(Time.time);};
        p.weaponController.BeamFired+=onShot;GameAudio.CuePlayed+=onSound;
        float previousEffects=GamePreferences.Effects;
        void Tick(bool fire)=>p.Simulate(new PlayerCommand{Fire=fire,HasAim=true,AimPoint=new Vector3(0,1.1f,18)},Time.deltaTime);
        void Reset(){p.RestoreAt(new Vector3(0,.1f,0));p.stats.ResetStats();p.Stance.ResetStance();p.weaponController.ResetCooldowns();shots.Clear();sounds.Clear();}
        try
        {
            foreach(int fps in new[]{30,60,120})
            {
                Time.captureDeltaTime=1f/fps;yield return null;Reset();
                for(int tap=0;tap<3;tap++)
                {
                    Tick(true);yield return null;
                    for(int i=0;i<Mathf.CeilToInt(.32f*fps);i++){Tick(false);yield return null;}
                    check(shots.Count==tap+1&&sounds.Count==tap+1,$"{fps} FPS tap {tap+1}: exactly one bullet and one sound, no automatic continuation");
                }
                Reset();int startCount=GameAudio.Instance.RifleShotsPlayed;
                for(int i=0;i<fps*6/5;i++){Tick(true);yield return null;}
                check(shots.Count==7&&sounds.SequenceEqual(shots),$"{fps} FPS held trigger: 7 shots / 7 simultaneous sounds");
                for(int i=1;i<shots.Count;i++)check(Mathf.Abs((shots[i]-shots[0])-i*.18f)<=1f/fps+.003f,$"{fps} FPS cadence stays on the weapon clock: shot {i+1}");
                int released=shots.Count;
                for(int i=0;i<fps/2;i++){Tick(false);yield return null;}
                check(shots.Count==released&&sounds.Count==released&&GameAudio.Instance.RifleShotsPlayed-startCount==released,$"{fps} FPS release emits no extra shot sounds");
                // A faster legal weapon rate must not be swallowed by generic SFX throttling.
                Reset();p.weaponController.SetTemporaryFireRateBonus(3f,2f);
                for(int i=0;i<fps*3/5;i++){Tick(true);yield return null;}
                check(shots.Count>=9&&sounds.SequenceEqual(shots),$"{fps} FPS faster cadence has one sound per actual shot and no throttle losses");
            }
            Reset();GamePreferences.SetEffects(0);Tick(true);yield return null;
            var rifle=GameAudio.Instance.GetComponents<AudioSource>().Where(s=>s.clip!=null&&(s.clip.name.StartsWith("m7_attack")||s.clip.name=="m7_tail")).ToArray();
            check(rifle.Length==4&&rifle.All(s=>s.volume==0&&!s.loop),"effects mute covers both rifle body and tail; no independent audio loop exists");
        }
        finally
        {
            p.weaponController.BeamFired-=onShot;GameAudio.CuePlayed-=onSound;
            GamePreferences.SetEffects(previousEffects);Time.captureDeltaTime=1f/60;p.weaponController.ResetCooldowns();
        }
    }
}
