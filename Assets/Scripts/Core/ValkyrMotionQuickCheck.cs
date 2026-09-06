using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;

[DefaultExecutionOrder(250)]
public sealed class ValkyrMotionQuickCheck : MonoBehaviour
{
    private IEnumerator scenario;
    private string output;
    private int errors, frame;
    private float deadline;
    private bool finished;
    private bool effectsPreview;
    private readonly List<string> report=new List<string>();
    private readonly List<string> trace=new List<string>{"clip,frame,time,state,left_grip_error,right_grip_error,feet_clearance,chest_x,chest_y,chest_z,grip_x,grip_y,grip_z,tip_x,tip_y,tip_z,right_wrist,left_wrist,pelvis_x,pelvis_y,pelvis_z,edge_dot_velocity,plant_slip"};
    private Vector3 previousBladeTip;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if(Array.IndexOf(Environment.GetCommandLineArgs(),"-motionQuickCheck")>=0)
            DontDestroyOnLoad(new GameObject("ValkyrMotionQuickCheck").AddComponent<ValkyrMotionQuickCheck>());
    }
    private void Start()
    {
        string profile=Environment.GetEnvironmentVariable("MECH_EQUIPMENT_PROFILE");
        if(string.IsNullOrEmpty(profile)) { Finish("Separate test profile is required"); return; }
        output=Path.GetDirectoryName(profile);
        effectsPreview=Array.IndexOf(Environment.GetCommandLineArgs(),"-actionVfxPreview")>=0;
        Directory.CreateDirectory(output); Directory.CreateDirectory(Path.Combine(output,"frames"));
        Directory.CreateDirectory(Path.Combine(output,"game-frames"));
        Application.logMessageReceived+=OnLog;
        PlayerInputRouter.AllowUnfocusedReplay=true; Application.runInBackground=true;
        Time.captureDeltaTime=1f/60; Application.targetFrameRate=60;
        deadline=Time.realtimeSinceStartup+180; scenario=Scenario();
    }
    private void LateUpdate()
    {
        if(finished || scenario==null) return;
        try { if(Time.realtimeSinceStartup>deadline) throw new Exception("Motion preview timeout"); if(!scenario.MoveNext()) Finish(null); }
        catch(Exception ex) { Finish(ex.ToString()); }
    }
    private IEnumerator Scenario()
    {
        for(int i=0;i<35;i++) yield return null;
        GamePreferences.SetMaster(0); GamePreferences.SetLanguage(true);
        var gm=GameManager.Instance; var player=gm.playerController;
        player.InputRouter.readKeyboard=false;
        var motion=player.GetComponentInChildren<ValkyrMotionDriver>();
        var blade=player.GetComponentInChildren<RaikenBladePresentation>();
        var fx=player.GetComponentInChildren<RaikenCombatVfx>();
        Check(motion!=null && !blade.rig.animator.enabled,"new rigid-joint motion owns the hero; generic walk/slash clips are disabled");
        Capture("01_ready");
        gm.BeginBossPreview();
        BossController boss=null;
        for(int i=0;i<120 && boss==null;i++) { yield return null; boss=FindFirstObjectByType<BossController>(); }
        Check(boss!=null,"existing elite encounter remains playable");
        boss.target=null;
        player.RestoreAt(new Vector3(0,.1f,-12));
        boss.GetComponent<NavMeshAgent>().Warp(new Vector3(0,0,-8.1f));
        boss.transform.rotation=Quaternion.Euler(0,180,0);
        player.AimAt(boss.GetComponent<Damageable>().AimCenter);
        for(int i=0;i<20;i++) yield return null;
        float healthBefore=boss.GetComponent<Damageable>().CurrentHealth;
        Quaternion chestBefore=motion.Chest.rotation;
        float torsoTravel=0, leftError=0,rightError=0,maxGripHeight=0,maxWrist=0,minHip=10,maxHip=-10;
        float strikeStartZ=player.transform.position.z;
        for(int i=0;i<85;i++)
        {
            if(i==12) player.InputRouter.QueueMelee();
            yield return null;
            if(i%2==0) Film("slash",i,player,motion,blade,145);
            torsoTravel=Mathf.Max(torsoTravel,Quaternion.Angle(chestBefore,motion.Chest.rotation));
            maxGripHeight=Mathf.Max(maxGripHeight,blade.grip.position.y-player.transform.position.y);
            if(player.Melee.IsAttacking)
            {
                maxWrist=Mathf.Max(maxWrist,motion.RightWristAngle,motion.LeftWristAngle);
                float hipX=player.transform.InverseTransformPoint(motion.Pelvis.position).x;
                minHip=Mathf.Min(minHip,hipX);maxHip=Mathf.Max(maxHip,hipX);
            }
            if(player.Melee.IsAttacking && player.Melee.AttackElapsed>.12f && player.Melee.AttackElapsed<.58f)
            { leftError=Mathf.Max(leftError,motion.LeftGripError);rightError=Mathf.Max(rightError,motion.RightGripError); }
            if(i==24) Capture("02_raised_guard");
            if(i==34) Capture("03_whole_body_cut");
            if(i==43) Capture("04_follow_through");
        }
        report.Add("MEASURE torso rotation="+torsoTravel+" max hilt="+maxGripHeight+" left grip="+leftError+" right grip="+rightError);
        Check(torsoTravel>40 && maxGripHeight>2.75f,"slash raises both arms above the chest and rotates the torso");
        Check(leftError<.23f && rightError<.16f,"both arm chains reach the authored handle through the strike");
        Check(blade.ImpactCount==1 && boss.GetComponent<Damageable>().CurrentHealth<healthBefore,"physical moving blade hits the elite once during the actual cut");
        if(effectsPreview)Check(fx!=null&&fx.SlashParticles>20&&fx.ImpactBursts==1&&boss.GetComponent<MechBladeHitReaction>()!=null,
            "real blade cut emits blue particles and the damaged elite receives armor feedback");
        Check(maxWrist<60,"both wrists stay below 60 degrees of flex through the whole grounded stroke");
        Check(maxHip-minHip>.4f && player.transform.position.z-strikeStartZ>.4f,"the pelvis crosses between support legs and the strike advances the real player");
        report.Add("MEASURE wrist flex max="+maxWrist+" lateral pelvis travel="+(maxHip-minHip));
        gm.stageManager.StopStage();
        player.RestoreAt(new Vector3(0,.1f,-14));player.AimAt(new Vector3(0,1.6f,20));
        blade.SetBeamEnabled(false);
        for(int i=0;i<(effectsPreview?0:80);i++)
        {
            if(i==12)player.InputRouter.QueueMelee();
            yield return null;
            if(i%2==0)Film("clean_slash",i,player,motion,blade,145);
        }
        blade.SetBeamEnabled(true);
        player.RestoreAt(new Vector3(0,.1f,-14)); player.AimAt(new Vector3(0,1.6f,20));
        player.InputRouter.SetTouchMove(Vector2.up);
        float startZ=player.transform.position.z, maxRunClearance=0,maxSlip=0;
        for(int i=0;i<110;i++)
        {
            yield return null;
            if(i>20) maxRunClearance=Mathf.Max(maxRunClearance,motion.FeetAboveGround);
            if(i>20)maxSlip=Mathf.Max(maxSlip,motion.PlantSlip);
            if(i%2==0) Film("run",i,player,motion,blade,90);
            if(i==40) Capture("05_combat_run");
        }
        Check(player.transform.position.z-startZ>8 && motion.RunBlend>.9f && motion.FlightBlend<.01f,"grounded input uses a moving combat run");
        Check(maxRunClearance>.05f,"running includes brief airborne phases rather than continuous walking support");
        Check(maxSlip<.015f,"a planted running ankle moves less than 1.5 cm per frame");
        if(effectsPreview)
        {
            Check(fx.ScrapeParticles>30&&fx.FootfallBursts>1&&fx.ThrustParticles>30,"running emits real tip-to-floor sparks, footfall dust and nozzle particles");
            report.Add("VFX slash="+fx.SlashParticles+" scrape="+fx.ScrapeParticles+" footfalls="+fx.FootfallBursts+" thrust="+fx.ThrustParticles+" final tip floor gap="+fx.ScrapeGap);
        }
        report.Add("MEASURE planted-foot maximum per-frame drift="+maxSlip);
        player.RestoreAt(new Vector3(0,.1f,-14)); player.AimAt(new Vector3(0,1.6f,20));
        player.InputRouter.SetTouchMove(Vector2.up);
        MobileActionButton dashButton=null;
        foreach(var button in FindObjectsByType<MobileActionButton>(FindObjectsSortMode.None)) if(button.isDash) dashButton=button;
        var pointer=new PointerEventData(EventSystem.current) {pointerId=42,button=PointerEventData.InputButton.Left};
        dashButton.OnPointerDown(pointer);
        float minClearance=10, energyBefore=player.stats.CurrentEnergy;
        for(int i=0;i<110;i++)
        {
            yield return null;
            if(i>30) minClearance=Mathf.Min(minClearance,motion.FeetAboveGround);
            if(i%2==0) Film("flight",i,player,motion,blade,90);
            if(i==50) Capture("06_boost_flight");
        }
        Check(player.IsBoosting && motion.FlightBlend>.9f && minClearance>.35f,"held boost has sustained flight with both feet visibly clear of the floor");
        Check(player.stats.CurrentEnergy<energyBefore-20,"sustained boost spends energy and does not regain it simultaneously");
        player.InputRouter.SetTouchMove(Vector2.zero);
        player.Melee.ResetCooldown(); player.InputRouter.QueueMelee();
        bool sawAirSlash=false;
        for(int i=0;i<60;i++)
        {
            yield return null;
            if(i%2==0) Film("air_slash",i,player,motion,blade,145);
            if(player.Melee.IsAttacking && player.Melee.AttackElapsed>.3f)
                sawAirSlash|=player.IsBoosting && motion.FlightBlend>.9f && motion.FeetAboveGround>.35f && motion.LeftGripError<.23f;
        }
        Check(sawAirSlash,"a held-boost slash keeps the mech airborne with both hands on the sword");
        dashButton.OnPointerUp(pointer);
        Check(!dashButton.IsHoldingBoost,"the on-screen hold button releases its boost input");
        player.InputRouter.Clear();
        for(int i=0;i<42;i++) { yield return null; if(i%2==0) Film("land",i,player,motion,blade,90); }
        Check(!player.IsBoosting && motion.FlightBlend<.01f && Mathf.Abs(motion.FeetAboveGround)<.12f,"releasing boost settles back to a grounded ready stance");
        player.stats.TrySpendEnergy(Mathf.Max(0,player.stats.CurrentEnergy-.1f));
        player.InputRouter.SetBoostHeld(true);
        bool exhausted=false,wasBoosting=false,restarted=false;
        for(int i=0;i<95;i++)
        {
            yield return null;
            if(exhausted && player.IsBoosting) restarted=true;
            if(wasBoosting && !player.IsBoosting) exhausted=true;
            wasBoosting|=player.IsBoosting;
        }
        Check(exhausted && !restarted,"empty energy ends boost without repeatedly taking off as energy regenerates");
        player.InputRouter.SetBoostHeld(false); yield return null;
        player.InputRouter.SetBoostHeld(true);
        for(int i=0;i<22;i++) yield return null;
        Check(player.IsBoosting,"boost can restart after releasing and pressing the button again");
        player.InputRouter.Clear();
        for(int i=0;i<60;i++) yield return null;
        player.Melee.ResetCooldown(); player.InputRouter.QueueMelee();
        for(int i=0;i<5;i++) yield return null;
        player.InputRouter.QueueDash();
        for(int i=0;i<15;i++) yield return null;
        Check(!player.Melee.IsAttacking && blade.TrailSamples==0,"dash still cancels the full-body slash and its blade trail");
        report.Add("Rendered frames="+frame+" GPU="+SystemInfo.graphicsDeviceName);
    }
    private void Film(string clip,int index,PlayerController player,ValkyrMotionDriver motion,RaikenBladePresentation blade,float yaw)
    {
        var camera=Camera.main; var position=camera.transform.position; var rotation=camera.transform.rotation;float size=camera.orthographicSize;
        EquipmentLoopQuickCheck.CaptureTo(Path.Combine(output,"game-frames"),"frame_"+frame.ToString("D4"),960,540,true);
        camera.orthographicSize=clip=="air_slash"?3.95f:3.45f;
        camera.transform.rotation=Quaternion.Euler(18,yaw,0);
        camera.transform.position=player.transform.position+Vector3.up*(clip=="air_slash"?2.65f:2.2f)-camera.transform.forward*22;
        EquipmentLoopQuickCheck.CaptureTo(Path.Combine(output,"frames"),"frame_"+(frame++).ToString("D4"),960,540,false);
        if(index%10==0) EquipmentLoopQuickCheck.CaptureTo(output,clip+"_pose_"+index,1280,720,false);
        if(effectsPreview&&clip=="run"&&(index==30||index==60))
        {
            camera.orthographicSize=1.55f;camera.transform.rotation=Quaternion.Euler(20,90,0);
            camera.transform.position=blade.tip.position+Vector3.up*.55f-camera.transform.forward*12;
            EquipmentLoopQuickCheck.CaptureTo(output,"drag_contact_"+index,1280,720,false);
        }
        if(clip=="slash" && (index==8||index==24||index==34||index==42))
        {
            var enemies=new List<Renderer>();
            foreach(var actor in FindObjectsByType<BossController>(FindObjectsSortMode.None))
                foreach(var renderer in actor.GetComponentsInChildren<Renderer>())if(renderer.enabled){enemies.Add(renderer);renderer.enabled=false;}
            camera.orthographicSize=.84f;
            camera.transform.rotation=Quaternion.Euler(8,150,0);
            camera.transform.position=blade.grip.position-camera.transform.forward*8;
            EquipmentLoopQuickCheck.CaptureTo(output,"grip_"+index,1280,720,false);
            foreach(var renderer in enemies)renderer.enabled=true;
        }
        camera.transform.SetPositionAndRotation(position,rotation);camera.orthographicSize=size;
        Vector3 c=motion.Chest.eulerAngles,g=blade.grip.position,t=blade.tip.position;
        Vector3 pelvis=motion.Pelvis.position-player.transform.position;
        Vector3 velocity=Vector3.ProjectOnPlane(t-previousBladeTip,t-g);
        trace.Add(string.Join(",",clip,index,player.Melee.AttackElapsed,motion.MotionState,motion.LeftGripError,motion.RightGripError,motion.FeetAboveGround,c.x,c.y,c.z,g.x,g.y,g.z,t.x,t.y,t.z,motion.RightWristAngle,motion.LeftWristAngle,pelvis.x,pelvis.y,pelvis.z,Vector3.Dot(motion.BladeEdge,velocity.normalized),motion.PlantSlip));
        previousBladeTip=t;
    }
    private void Capture(string name) => EquipmentLoopQuickCheck.CaptureTo(output,name);
    private void Check(bool okay,string name) { report.Add((okay?"PASS ":"FAIL ")+name);if(!okay) throw new Exception(name); }
    private void OnLog(string message,string stack,LogType type) { if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert) {errors++;report.Add("ERROR "+message+"\n"+stack);} }
    private void Finish(string reason)
    {
        if(finished)return;finished=true;if(reason!=null)report.Add(reason);
        int code=errors==0&&reason==null?0:1;
        report.Add("MOTION_QUICK_CHECK code="+code+" errors="+errors);
        if(output!=null) {File.WriteAllLines(Path.Combine(output,"quick-check.txt"),report);File.WriteAllLines(Path.Combine(output,"motion.csv"),trace);}
        Application.logMessageReceived-=OnLog;Application.Quit(code);
    }
}
