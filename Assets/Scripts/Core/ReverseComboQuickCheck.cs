using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

[DefaultExecutionOrder(250)]
public sealed class ReverseComboQuickCheck : MonoBehaviour
{
    IEnumerator scenario;string output;bool done,film,power,captureSound;int errors,frame,shots,invalidShots;float deadline;
    PlayerAudioCapture audioCapture;
    readonly List<GameAudioCue> audioCues=new List<GameAudioCue>();
    GameManager gm;PlayerController player;ValkyrMotionDriver motion;RaikenBladePresentation blade;WeaponTrial trial;
    readonly List<string> report=new List<string>();readonly List<string> trace=new List<string>{"frame,clip,stage,time,forward_grip,wrist_angle,grip_x,grip_y,grip_z,tip_x,tip_y,tip_z,hand_x,hand_y,hand_z,elbow_x,elbow_y,elbow_z"};
    readonly List<int> stages=new List<int>();readonly List<string> hits=new List<string>();
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Bootstrap(){if(Array.IndexOf(Environment.GetCommandLineArgs(),"-reverseComboCheck")>=0)DontDestroyOnLoad(new GameObject("ReverseComboQuickCheck").AddComponent<ReverseComboQuickCheck>());}
    void Start()
    {
        var path=Environment.GetEnvironmentVariable("MECH_EQUIPMENT_PROFILE");if(string.IsNullOrEmpty(path)){Application.Quit(2);return;}
        output=Path.GetDirectoryName(path);film=Array.IndexOf(Environment.GetCommandLineArgs(),"-comboFilm")>=0;
        power=Array.IndexOf(Environment.GetCommandLineArgs(),"-powerComboCheck")>=0;
        Directory.CreateDirectory(output+"/frames");Directory.CreateDirectory(output+"/game-frames");
        Application.logMessageReceived+=OnLog;PlayerInputRouter.AllowUnfocusedReplay=true;Application.runInBackground=true;
        Time.captureDeltaTime=1f/60;Application.targetFrameRate=120;deadline=Time.realtimeSinceStartup+240;scenario=Scenario();
    }
    void LateUpdate()
    {
        if(done)return;
        try
        {
            if(Time.realtimeSinceStartup>deadline)throw new Exception("Combo preview timeout");
            bool more=scenario.MoveNext();
            if(audioCapture!=null)audioCapture.Advance(captureSound);captureSound=false;
            if(!more)Finish(null);
        }
        catch(Exception e){Finish(e.ToString());}
    }
    PlayerCommand Command(bool melee=false,bool fire=false,Vector2 move=default,bool boost=false,bool dash=false)=>new PlayerCommand{Melee=melee,Fire=fire,Move=move,BoostHeld=boost,Dash=dash,HasAim=true,AimPoint=player.transform.position+Vector3.forward*18+Vector3.up*1.4f};
    void Step(PlayerCommand command)=>player.Simulate(command,Time.deltaTime);
    void Begin(){stages.Add(player.Melee.ComboStage);}
    void Hit(Damageable actor,float amount){hits.Add(player.Melee.ComboStage+":"+actor.name+":"+amount);}
    void Shot(){shots++;if(player.Stance.State!=WeaponStance.Ranged||player.Melee.IsAttacking)invalidShots++;}
    IEnumerator Scenario()
    {
        for(int i=0;i<35;i++)yield return null;
        gm=GameManager.Instance;player=gm.playerController;player.enabled=false;player.InputRouter.readKeyboard=false;
        GamePreferences.SetLanguage(true);GamePreferences.SetMaster(power?1:0);
        if(power){audioCapture=new PlayerAudioCapture();GameAudio.CuePlayed+=AudioCue;}
        motion=player.GetComponentInChildren<ValkyrMotionDriver>();blade=player.GetComponentInChildren<RaikenBladePresentation>();
        player.Melee.AttackStarted+=Begin;player.Melee.StrikeHit+=Hit;player.weaponController.BeamFired+=Shot;
        gm.BeginWeaponTrial();trial=gm.GetComponent<WeaponTrial>();trial.ClearTargets();
        for(int i=0;i<10;i++){Step(Command());yield return null;}
        Capture("ready_front",180);Capture("ready_right",270);Capture("ready_threequarter",205);Capture("hand_ready",225,true);
        Check(motion.GetComponentInChildren<ValkyrHandGrip>()!=null,"articulated palm and 15 finger segments are active");
        Check(motion.ForwardGrip<.01f,"initial carry uses reverse grip");
        Check(motion.BladeEdge.y<-.5f,"the actual blue blade edge faces down during reverse carry");
        // Actual three input pulses. The observer never writes the animation clock or transforms.
        var target=trial.SpawnTarget(player.transform.position+Vector3.forward*(power?3.5f:2.5f),"",1000).GetComponent<Damageable>();
        var captured=new HashSet<string>();bool second=false,third=false;
        for(int i=0;i<190;i++)
        {
            bool press=i==8;
            if(player.Melee.IsAttacking&&player.Melee.ComboStage==0&&player.Melee.AttackElapsed>=.21f&&!second){press=true;second=true;}
            if(player.Melee.IsAttacking&&player.Melee.ComboStage==1&&player.Melee.AttackElapsed>=.40f&&!third){press=true;third=true;}
            Step(Command(melee:press));yield return null;
            int s=player.Melee.ComboStage;float t=player.Melee.AttackElapsed;
            if(player.Melee.IsAttacking)
            {
                float[] moments=s==0?new[]{.12f,.245f,.33f}:s==1?new[]{.11f,.20f,.28f,.40f}:new[]{.24f,.40f,.50f};
                foreach(float at in moments){string key="s"+(s+1)+"_"+at.ToString("F2").Replace('.','_');if(t>=at&&captured.Add(key)){Capture(key,205);if(s==1&&at==.20f)Capture("hand_turn",215,true);}}
            }
            if(film&&i%2==0)Film("combo");
        }
        Capture("after_combo",205);
        Check(stages.SequenceEqual(new[]{0,1,2}),"three presses produce reverse cut, regrip return cut and frontal chop in order");
        Check(!player.Melee.IsAttacking&&motion.ForwardGrip<.01f,"combo settles back to low reverse grip");
        report.Add("OBSERVED hits="+string.Join(",",hits));
        Check(target.CurrentHealth==766,"all three actual blade sweeps hit once each for 78 damage");
        if(power)Check(audioCues.Count(c=>c==GameAudioCue.SwordCut1)==1&&audioCues.Count(c=>c==GameAudioCue.SwordCut2)==1&&audioCues.Count(c=>c==GameAudioCue.SwordCut3)==1&&audioCues.Contains(GameAudioCue.SwordHitHeavy)&&!audioCues.Contains(GameAudioCue.Slash),"three distinct cuts and a heavy armor impact replace the repeated force-field sound");
        // Stop after one and two inputs; there is no automatic third strike.
        for(int count=1;count<=2;count++)
        {
            stages.Clear();bool queued=false;
            for(int i=0;i<150;i++)
            {
                bool press=i==0;if(count==2&&player.Melee.IsAttacking&&player.Melee.ComboStage==0&&player.Melee.AttackElapsed>=.23f&&!queued){press=true;queued=true;}
                Step(Command(melee:press));yield return null;
                if(film&&i%2==0&&count==2)Film("stop_after_two");
            }
            Check(stages.Count==count&&!player.Melee.IsAttacking&&motion.ForwardGrip<.01f,"stop after "+count+" input(s) finishes the recovery and restores reverse grip");
        }
        trial.ClearTargets();yield return null;Vector3 runStart=player.transform.position;
        for(int i=0;i<75;i++){Step(Command(move:Vector2.up));yield return null;if(film&&i%2==0)Film("reverse_drag_run");if(i==45)Capture("run_reverse",205);}
        Check(Vector3.Distance(runStart,player.transform.position)>2,"the running preview travels through the range without a target blocking it");
        Check(motion.BladeEdge.y<-.35f,"reverse carry keeps the blue edge down while running");
        for(int i=0;i<65;i++){Step(Command(fire:i<25,move:Vector2.up,boost:i>30));yield return null;if(film&&i%2==0)Film("moving_stow");}
        Check(shots>0&&invalidShots==0&&!blade.BeamEnabled,"moving or boosting cannot fire while holding a live sword");
        stages.Clear();int before=shots;
        for(int i=0;i<115;i++){Step(Command(melee:i==0,fire:true,move:new Vector2(.65f,-.7f),boost:true));yield return null;if(film&&i%2==0)Film("boost_reverse_cut");}
        Check(shots==before&&stages.Count==1&&player.Stance.State==WeaponStance.Sword,"new melee wins over held fire and draws directly into reverse grip");
        Step(Command(melee:true));yield return null;for(int i=0;i<12;i++){Step(Command());yield return null;}
        Step(Command(dash:true));yield return null;
        Check(!player.Melee.IsAttacking&&!player.Melee.NextQueued,"dash cancels the cut and its queued continuation");
        for(int i=0;i<50;i++){Step(Command());yield return null;}
        Check(invalidShots==0,"all observed ranged shots occurred after completed stow");
        // Only the affected hit windows are compared at multiple frame rates.
        for(int f=0;f<3;f++)
        {
            int fps=new[]{30,60,120}[f];Time.captureDeltaTime=1f/fps;trial.ClearTargets();player.RestoreAt(new Vector3(0,.1f,-12));player.AimAt(new Vector3(0,1.4f,0));
            for(int i=0;i<8;i++){Step(Command());yield return null;}
            var probe=trial.SpawnTarget(new Vector3(0,.1f,-9.5f),"",1000).GetComponent<Damageable>();
            var outside=trial.SpawnTarget(new Vector3(0,.1f,-5.0f),"",1000).GetComponent<Damageable>();
            bool q2=false,q3=false;
            for(int i=0;i<fps*4;i++)
            {
                bool press=i==0;
                if(player.Melee.IsAttacking&&player.Melee.ComboStage==0&&player.Melee.AttackElapsed>=.21f&&!q2){press=true;q2=true;}
                if(player.Melee.IsAttacking&&player.Melee.ComboStage==1&&player.Melee.AttackElapsed>=.40f&&!q3){press=true;q3=true;}
                Step(Command(melee:press));yield return null;
            }
            Check(probe.CurrentHealth==766&&outside.CurrentHealth==1000,fps+" fps: each cut hits once; the out-of-range target is untouched");
        }
        Time.captureDeltaTime=1f/60;
        gm.EndWeaponTrial();report.Add("Frames="+frame+" GPU="+SystemInfo.graphicsDeviceName);
    }
    void Capture(string name,float yaw,bool hand=false)
    {
        var cam=Camera.main;var p=cam.transform.position;var r=cam.transform.rotation;float size=cam.orthographicSize;
        cam.orthographicSize=hand?.62f:power?4.50f:4.15f;cam.transform.rotation=Quaternion.Euler(hand?10:14,yaw,0);
        Vector3 aim=hand?motion.RightWrist.position:player.transform.position+Vector3.up*2.45f;
        cam.transform.position=aim-cam.transform.forward*20;
        EquipmentLoopQuickCheck.CaptureTo(output,name,1280,900,false);cam.transform.SetPositionAndRotation(p,r);cam.orthographicSize=size;
    }
    void Film(string clip)
    {
        string file="frame_"+frame.ToString("D4");var cam=Camera.main;var p=cam.transform.position;var r=cam.transform.rotation;float size=cam.orthographicSize;
        EquipmentLoopQuickCheck.CaptureTo(output+"/game-frames",file,960,540,true);
        cam.orthographicSize=power?4.5f:4.15f;cam.transform.rotation=Quaternion.Euler(14,235,0);cam.transform.position=player.transform.position+Vector3.up*2.45f-cam.transform.forward*20;
        EquipmentLoopQuickCheck.CaptureTo(output+"/frames",file,960,540,false);cam.transform.SetPositionAndRotation(p,r);cam.orthographicSize=size;
        var g=blade.grip.position;var t=blade.tip.position;var w=motion.RightWrist.position;var e=motion.RightElbow.position;
        trace.Add(string.Join(",",frame,clip,player.Melee.ComboStage,player.Melee.AttackElapsed,motion.ForwardGrip,motion.RightWristAngle,g.x,g.y,g.z,t.x,t.y,t.z,w.x,w.y,w.z,e.x,e.y,e.z));frame++;
        captureSound=power;
    }
    void Check(bool ok,string message){report.Add((ok?"PASS ":"FAIL ")+message);if(!ok)throw new Exception(message);}
    void OnLog(string m,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert){errors++;report.Add("ERROR "+m+"\n"+stack);}}
    void AudioCue(GameAudioCue cue){audioCues.Add(cue);}
    void Finish(string failure)
    {
        if(done)return;done=true;
        if(audioCapture!=null)
        {
            audioCapture.Save(output+"/game-mix.wav");
            report.Add("ACTUAL_AUDIO duration="+audioCapture.Duration+" peak="+audioCapture.Peak);
            if(failure==null&&(audioCapture.Peak<.005f||audioCapture.Peak>=1))failure="Actual mixer capture is silent or clipped";
        }
        GameAudio.CuePlayed-=AudioCue;
        if(failure!=null)report.Add(failure);int code=failure==null&&errors==0?0:1;
        report.Add((power?"POWER_COMBO_CHECK":"REVERSE_COMBO_CHECK")+" code="+code+" errors="+errors);
        File.WriteAllLines(output+"/quick-check.txt",report);File.WriteAllLines(output+"/motion.csv",trace);
        File.WriteAllLines(output+"/audio-cues.txt",audioCues.Select(c=>c.ToString()));
        Application.logMessageReceived-=OnLog;Application.Quit(code);
    }
}
