using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class ImportedMotionChecks
{
    public static IEnumerator Run(GameManager gm,PlayerController p,string output,Action<bool,string> check,Action<string> capture)
    {
        var rows=new List<string>{"hero,fps,stage,elapsed,clip,sample,grip,feet"};
        var cam=Camera.main;var follow=cam.GetComponent<CameraFollow>();
        foreach(var hero in new[]{HeroMech.Nemesis,HeroMech.Valkyr})
        {
            gm.EnterResult(false);gm.EnterHangar();for(int i=0;i<15;i++)yield return null;
            check(p.GetComponent<PlayerMechLoader>().SelectHero(hero)||p.GetComponent<PlayerMechLoader>().SelectedHero==hero,"select imported-motion hero "+hero);
            for(int i=0;i<20;i++)yield return null;
            gm.BeginWeaponTrial();for(int i=0;i<15;i++)yield return null;
            UnityEngine.Object.FindFirstObjectByType<WeaponTrial>().ClearTargets();yield return null;
            p.enabled=false;p.InputRouter.readKeyboard=false;p.RestoreAt(new Vector3(0,.1f,0));
            var motion=p.GetComponentInChildren<ValkyrMotionDriver>();var blade=p.GetComponentInChildren<RaikenBladePresentation>();
            check(motion.ImportedMotionEnabled,"actual Player loaded 27-joint source-motion bank for "+hero);
            void Command(Vector2 move=default,bool slash=false,bool dash=false,bool boost=false)
            {p.Simulate(new PlayerCommand{Move=move,Melee=slash,Dash=dash,BoostHeld=boost,HasAim=true,AimPoint=p.transform.position+Vector3.forward*15},Time.deltaTime);}
            void View(){follow.enabled=false;cam.orthographic=true;cam.orthographicSize=2.7f;cam.transform.position=p.transform.TransformPoint(new Vector3(6,3.4f,9));cam.transform.LookAt(p.transform.TransformPoint(new Vector3(0,1.9f,.1f)));}
            for(int i=0;i<30;i++){Command();yield return null;}
            View();capture(hero+"-idle.png");
            float movement=0;var previous=motion.RightElbow.position;
            int footsteps=0;Action<GameAudioCue> stepCue=cue=>{if(cue==GameAudioCue.Footstep)footsteps++;};
            GameAudio.CuePlayed+=stepCue;
            try{
            for(int i=0;i<60;i++)
            {Command(Vector2.up);yield return null;movement+=Vector3.Distance(previous,motion.RightElbow.position-p.Velocity*Time.deltaTime);previous=motion.RightElbow.position;
             check(motion.FeetAboveGround>-.065f,"source running feet above ground "+hero+" frame="+i);
             if(i%12==0){View();capture(hero+"-run-"+i+".png");}}
            }finally{GameAudio.CuePlayed-=stepCue;}
            check(footsteps<=1,"locomotion audio matches actual hover / walking contacts "+hero+" count="+footsteps);
            check(motion.ImportedClip.EndsWith("boost")&&movement>.1f,"source locomotion actually drives the mounted rig "+hero);
            Command(Vector2.up,dash:true);yield return null;for(int i=0;i<10;i++){Command(Vector2.up,boost:true);yield return null;}
            check(motion.ImportedClip.EndsWith("boost"),"boost selects the imported flight clip "+hero);View();capture(hero+"-boost.png");
            p.RestoreAt(new Vector3(0,.1f,0));for(int i=0;i<40;i++){Command();yield return null;}
            foreach(int fps in new[]{30,60,120})
            {
                Time.captureDeltaTime=1f/fps;p.RestoreAt(new Vector3(0,.1f,0));for(int i=0;i<fps/3;i++){Command();yield return null;}
                var seen=new bool[3];float grip=0;Command(slash:true);yield return null;
                for(int i=0;i<fps*3;i++)
                {
                    Command(slash:p.Melee.IsAttacking&&p.Melee.ComboStage<2&&p.Melee.AttackElapsed>.08f);yield return null;
                    // A buffered slash first draws the sword; wait for that transition.
                    if(!p.Melee.IsAttacking){if(seen[0])break;continue;}
                    seen[p.Melee.ComboStage]=true;grip=Mathf.Max(grip,motion.RightGripError);
                    // CompletePoseFrame links the next stage after the last pose
                    // was presented. At elapsed=0 that last pose still belongs
                    // to the preceding stage; the next tick samples the new one.
                    if(p.Melee.AttackElapsed>0)
                        check(motion.ImportedClip.StartsWith("cut"+(p.Melee.ComboStage+1)),"live strike samples matching source stage "+hero+" "+fps);
                    rows.Add(FormattableString.Invariant($"{hero},{fps},{p.Melee.ComboStage},{p.Melee.AttackElapsed:F4},{motion.ImportedClip},{motion.ImportedSampleTime:F4},{motion.RightGripError:F5},{motion.FeetAboveGround:F4}"));
                    if(fps==60&&i%3==0){View();capture(hero+"-cut-"+p.Melee.ComboStage+"-"+i+".png");}
                }
                check(seen[0]&&seen[1]&&seen[2]&&!p.Melee.IsAttacking,"all three imported cuts complete at "+fps+" for "+hero);
                check(grip<.015f,"source animation keeps the real sword hilt seated "+hero+" "+fps);
            }
            Time.captureDeltaTime=1f/60;Command(slash:true);yield return null;for(int i=0;i<4;i++){Command();yield return null;}
            gm.SetPaused(true);var frozen=blade.tip.position;var time=motion.ImportedSampleTime;
            for(int i=0;i<8;i++)yield return null;
            check(Vector3.Distance(blade.tip.position,frozen)<.00001f&&motion.ImportedSampleTime==time,"pause freezes imported body and sword "+hero);
            gm.SetPaused(false);p.RestoreAt(new Vector3(0,.1f,0));for(int i=0;i<20;i++){Command();yield return null;}
            check(!p.Melee.IsAttacking&&!motion.ImportedClip.StartsWith("cut"),"restart clears source attack state "+hero);
        }
        follow.enabled=true;File.WriteAllLines(Path.Combine(output,"imported-motion.csv"),rows);
    }
}

