using System.Collections.Generic;
using UnityEngine;

// The full pose is authored first; IK resolves contact without inventing the action.
[DefaultExecutionOrder(75)]
public sealed partial class ValkyrMotionDriver : MonoBehaviour
{
    public const float SlashDuration=.72f, ContactStart=.20f, ContactEnd=.40f;
    public float FlightBlend {get;private set;}
    public float RunBlend {get;private set;}
    public float LeftGripError {get;private set;}
    public float RightGripError {get;private set;}
    public float FeetAboveGround {get;private set;}
    public float RightWristAngle {get;private set;}
    public float LeftWristAngle {get;private set;}
    public float PlantSlip {get;private set;}
    public Vector3 BladeEdge => edgeMarker!=null?Vector3.ProjectOnPlane(edgeMarker.position-blade.grip.position,blade.tip.position-blade.grip.position).normalized:handR.rotation*weaponInHand*Vector3.right;
    public string MotionState {get;private set;}
    public Transform Pelvis=>pelvis;
    public Transform Chest=>chest;
    public Transform Waist=>waist;
    public Transform LeftFoot=>legL.foot;
    public Transform RightFoot=>legR.foot;
    public bool LeftPlanted=>legL.planted;
    public bool RightPlanted=>legR.planted;
    private PlayerController player;
    private NemesisMotionRig nemesis;
    private Damageable health;
    private RiggedMechAnimator rig;
    private RigidMechPoseDriver pose;
    private RaikenBladePresentation blade;
    private ValkyrComboProfile combo;
    private ValkyrHandGrip articulatedHand;
    private ValkyrComboProfile.Key displayedKey,entryKey,cancelKey;
    private const float CancelReturnSeconds=.07f;
    private float cancelBlend;
    private Vector3 shownEdge=Vector3.down,entryEdge=Vector3.down;
    public float ForwardGrip {get;private set;}
    public Transform RightWrist=>handR;
    public Transform RightElbow=>foreR;
    private Transform pelvis,waist,chest,head,armR,foreR,handR,armL,foreL,handL,edgeMarker;
    private Vector3 palmR,palmL,pelvisRest,lastPosition,attackOrigin,attackFootL,attackFootR;
    private Vector3 previousVelocity;
    private float accelerationLean;
    private Quaternion weaponInHand,attackHeading,heldRotation,rootInFrame;
    private float cycle,death,recoil,previousPoseTime;
    private float hitAge=1f,hitDuration=.26f,hitStrength;
    private Vector3 hitDirection;
    public float HitReactionWeight => health!=null&&!health.IsDead ? hitStrength*(1-Mathf.Exp(-hitAge*150))*Mathf.Pow(Mathf.Clamp01(1-hitAge/hitDuration),2) : 0;
    public void ResetHitReaction(){hitAge=1;hitStrength=0;death=0;}
    private void OnDamage(Damageable target,DamageInfo info)
    {
        if(info.Amount<=0)return;
        hitDirection=Vector3.ProjectOnPlane(target.transform.position-info.SourcePosition,Vector3.up).normalized;
        if(hitDirection.sqrMagnitude<.01f)hitDirection=-player.transform.forward;
        hitStrength=Mathf.Clamp(.65f+info.Amount/30f,.65f,1.4f);
        hitDuration=info.HeavyImpact?.32f:.26f;hitAge=0;
    }
    private Vector3 heldPosition;
    private E01PlayerRifle recoveredRifle;
    private Leg legL,legR;
    private readonly Dictionary<Transform,Rest> rest=new Dictionary<Transform,Rest>();
    private struct Rest {public Vector3 position;public Quaternion local,world;}
    private sealed class Leg
    {
        public Transform thigh,knee,foot;
        public int side;
        public Vector3[] sole;
        public Vector3 plant,previousAnkle;
        public Quaternion plantRotation;
        public bool planted;
    }
    private void Remember(Transform t)
    {if(!rest.ContainsKey(t))rest.Add(t,new Rest{position=t.localPosition,local=t.localRotation,world=Quaternion.Inverse(transform.rotation)*t.rotation});}
    private Transform Part(string name)
    {foreach(var t in pose.assemblyRoot.GetComponentsInChildren<Transform>(true))if(t.name==name)return t;return null;}
    private void Awake()
    {
        rig=GetComponent<RiggedMechAnimator>();pose=GetComponent<RigidMechPoseDriver>();blade=GetComponent<RaikenBladePresentation>();
        player=GetComponentInParent<PlayerController>();health=GetComponentInParent<Damageable>();
        nemesis=GetComponent<NemesisMotionRig>();
        foreach(var s in pose.segments)Remember(s.target);
        pelvis=Part("Pelvis");waist=Part("Waist");chest=Part("Thorax");head=Part("Head");Remember(waist);
        armR=Part("UpperArm.R");foreR=Part("Forearm.R");handR=Part("Hand.R");
        armL=Part("UpperArm.L");foreL=Part("Forearm.L");handL=Part("Hand.L");
        pelvisRest=transform.InverseTransformPoint(pelvis.position);
        var contactR=Part("V3B_Sword_Grip_Socket");var contactL=Part("V3B_Left_Support_Grip_Socket");
        palmR=handR.InverseTransformPoint(contactR!=null?contactR.position:blade.grip.position);
        palmL=contactL!=null?handL.InverseTransformPoint(contactL.position):new Vector3(-.0256f,-.0342f,-.1795f);
        edgeMarker=Part("V3B_Blade_Edge_Frame");
        Vector3 axis=(blade.tip.position-blade.grip.position).normalized;
        Vector3 edge=edgeMarker!=null?edgeMarker.position-blade.grip.position:transform.forward;
        // The visible blue cutting edge is the authority; the old concept marker can
        // disagree with the latest weapon export's rolled mesh orientation.
        Vector3 beamCenter=Vector3.zero;int beamVertices=0;
        foreach(var mf in blade.beam.GetComponentsInChildren<MeshFilter>(true))
            foreach(var vertex in mf.sharedMesh.vertices){beamCenter+=mf.transform.TransformPoint(vertex);beamVertices++;}
        if(beamVertices>0)
        {
            var physical=Vector3.ProjectOnPlane(beamCenter/beamVertices-blade.grip.position,axis);
            if(physical.sqrMagnitude>.000001f){edge=physical.normalized;if(edgeMarker!=null)edgeMarker.position=blade.grip.position+edge*.5f;}
        }
        Quaternion oldFrame=ValkyrMotionProfile.Frame(axis,edge);
        articulatedHand=handR.GetComponentInChildren<ValkyrHandGrip>();
        Vector3 fingerAxis=articulatedHand!=null?articulatedHand.fingerAxis:Vector3.ProjectOnPlane(Vector3.up,palmR.normalized).normalized;
        weaponInHand=ValkyrMotionProfile.Frame(fingerAxis,Vector3.Cross(palmR.normalized,fingerAxis));
        heldPosition=blade.bladeRoot.localPosition;heldRotation=blade.bladeRoot.localRotation;
        rootInFrame=Quaternion.Inverse(oldFrame)*blade.bladeRoot.rotation;
        legL=MakeLeg(-1,"L");legR=MakeLeg(1,"R");lastPosition=player.transform.position;
        combo=nemesis!=null?NemesisComboProfile.Active:ValkyrComboProfile.LoadActive();
        displayedKey=ValkyrComboProfile.Ready;
        InitializeImportedMotion();
        rig.animator.enabled=false;
    }
    private Leg MakeLeg(int side,string suffix)
    {
        var foot=Part("Foot."+suffix);var points=new List<Vector3>();
        foreach(var c in pose.soleContacts)if(c.part.IsChildOf(foot))points.Add(foot.InverseTransformPoint(c.part.TransformPoint(c.localPoint)));
        return new Leg{side=side,thigh=Part("Thigh."+suffix),knee=Part("Shin."+suffix),foot=foot,sole=points.ToArray()};
    }
    private void Start()
    {
        player.Melee.ConfigureMotionProfile(combo);
        health.OnDamaged+=OnDamage;player.weaponController.BeamFired+=OnShot;
        player.Melee.AttackStarted+=BeginStrike;player.Melee.AttackCancelled+=BeginReturn;
    }
    private void BeginReturn(){cancelKey=displayedKey;cancelBlend=CancelReturnSeconds;CaptureImportedEntry();}
    private void OnShot(){recoil=nemesis!=null?.105f:.055f;}
    private void BeginStrike()
    {
        CaptureImportedEntry();
        entryKey=displayedKey;entryEdge=shownEdge;cancelBlend=0;
        previousPoseTime=0;
        attackOrigin=player.transform.position;attackHeading=player.transform.rotation;
        attackFootL=Quaternion.Inverse(attackHeading)*(legL.foot.position-attackOrigin);
        attackFootR=Quaternion.Inverse(attackHeading)*(legR.foot.position-attackOrigin);
        attackFootL.y=attackFootR.y=0;legL.planted=legR.planted=false;
    }
    private Vector3 World(Vector3 p)=>transform.TransformPoint(p);
    private void LateUpdate()
    {
        if(player==null||(GameManager.Instance!=null&&GameManager.Instance.IsPaused))return;
        float dt=Time.deltaTime,speed=player.Velocity.magnitude;
        hitAge+=dt;
        float acceleration=Vector3.Dot(player.Velocity-previousVelocity,player.transform.forward)/Mathf.Max(dt,.001f);
        previousVelocity=player.Velocity;
        accelerationLean=Mathf.Lerp(accelerationLean,Mathf.Clamp(acceleration*.22f,-6,7),1-Mathf.Exp(-12*dt));
        Vector3 displacement=player.transform.position-lastPosition;displacement.y=0;lastPosition=player.transform.position;
        if(player.Melee.IsAttacking)attackOrigin+=displacement;
        bool attacking=player.Melee.IsAttacking;
        RunBlend=Mathf.MoveTowards(RunBlend,Mathf.Clamp01(speed/7f),dt*12);
        float flightTarget=player.IsDashing||player.IsBoosting?1:nemesis!=null&&!attacking?Mathf.SmoothStep(0,.78f,Mathf.InverseLerp(1.2f,7f,speed)):!attacking?Mathf.SmoothStep(0,.72f,Mathf.InverseLerp(4f,7f,speed)):0;
        FlightBlend=Mathf.MoveTowards(FlightBlend,flightTarget,dt*(flightTarget>FlightBlend?12:9));
        if(health.IsDead){death=Mathf.Min(1,death+dt*1.5f);FlightBlend=0;}
        if(!attacking&&FlightBlend<.1f&&displacement.magnitude<1)cycle+=displacement.magnitude/(nemesis==null?2.535f:3.9f);
        recoil=Mathf.MoveTowards(recoil,0,dt*.8f);
        cancelBlend=Mathf.Max(0,cancelBlend-dt);
        if(attacking && !player.Melee.ImpactHeld)
        {
            float end=Mathf.Min(player.Melee.CurrentStroke.contactEnd,player.Melee.AttackElapsed);
            float start=Mathf.Max(player.Melee.CurrentStroke.contactStart,previousPoseTime);
            if(end>=start)
            {
                int count=Mathf.Max(1,Mathf.CeilToInt((end-start)*240));
                for(int i=previousPoseTime>=player.Melee.CurrentStroke.contactStart?1:0;i<=count;i++)
                {
                    float time=Mathf.Lerp(start,end,i/(float)count);
                    ApplyPose(0,time);
                    blade.RecordCutSample(blade.grip.position,blade.tip.position,Time.time-(player.Melee.AttackElapsed-time)/player.Melee.AttackSpeed);
                    if(player.Melee.ImpactHeld){player.Melee.LockContactPose(time);break;}
                }
            }
            previousPoseTime=player.Melee.AttackElapsed;
        }
        ApplyPose(dt,player.Melee.AttackElapsed);
    }
    private void ApplyPose(float dt,float poseTime)
    {
        float speed=player.Velocity.magnitude;
        bool attacking=player.Melee.IsAttacking;
        foreach(var item in rest)item.Key.SetLocalPositionAndRotation(item.Value.position,item.Value.local);
        pose.assemblyRoot.localPosition=pose.assemblyRestPosition;
        blade.bladeRoot.SetLocalPositionAndRotation(heldPosition,heldRotation);
        if(GameManager.Instance!=null&&GameManager.Instance.Phase==GamePhase.Hangar)
        {ApplyKatokiPose();return;}
        if(importedMotion!=null&&!health.IsDead&&GameManager.Instance!=null&&GameManager.Instance.IsCombatActive)
        {ApplyImportedMotion(dt,poseTime);return;}
        var m=attacking?combo.Evaluate(player.Melee.ComboStage,poseTime):ValkyrComboProfile.Ready;
        if(attacking)
        {
            // Chest leads the arm by 22 ms, elbow by 10 ms. The offset vanishes at link boundaries.
            float span=player.Melee.CurrentStroke.linkTime;
            float pulse=Mathf.Sin(Mathf.Clamp01(poseTime/span)*Mathf.PI);
            var lead=combo.Evaluate(player.Melee.ComboStage,poseTime+.022f*pulse);
            m.chest=lead.chest;m.waist=lead.waist;m.hips=lead.hips;
            m.elbow=combo.Evaluate(player.Melee.ComboStage,poseTime+.010f*pulse).elbow;
        }
        float entryDuration=.04f;
        if(attacking&&poseTime<entryDuration)m=ValkyrComboProfile.Blend(entryKey,m,Mathf.SmoothStep(0,1,poseTime/entryDuration));
        if(!attacking&&cancelBlend>0)m=ValkyrComboProfile.Blend(cancelKey,m,Mathf.SmoothStep(0,1,1-cancelBlend/CancelReturnSeconds));
        var k=m.Body;
        float run=RunBlend*(1-FlightBlend)*(attacking?0:1),lift=(.28f)*FlightBlend;
        Vector3 travel=speed>.15f?player.Velocity.normalized:player.transform.forward;
        Vector3 localTravel=transform.InverseTransformDirection(travel),sideways=Vector3.Cross(Vector3.up,travel);
        float travelYaw=Vector3.SignedAngle(transform.forward,travel,Vector3.up),swing=Mathf.Sin(cycle*Mathf.PI*2);
        if(!attacking)
        {
            float step=Mathf.Repeat(cycle,.5f)*2;
            float bounce=.135f*Mathf.Sin(step*Mathf.PI)-.075f*Mathf.Exp(-Mathf.Pow((step-.12f)/.13f,2));
            k.pelvis=Vector3.Lerp(k.pelvis,new Vector3(0,-.295f+bounce,0),run);
            k.hips=Vector3.Lerp(k.hips,new Vector3(0,Mathf.Clamp(travelYaw,-65,65)+swing*10,-swing*4),run);
            k.waist=Vector3.Lerp(k.waist,new Vector3(0,Mathf.Clamp(travelYaw,-38,38)+swing*3,-swing*3),run);
            k.chest=Vector3.Lerp(k.chest,new Vector3(3,-swing*15,swing*4),run);
            if(nemesis!=null&&player.Stance.State==WeaponStance.Ranged&&!health.IsDead&&player.Loadout.Selected==PrimaryWeapon.M7)
            {k.hips.y-=12;k.waist.y-=28;k.chest.y-=54;k.chest.z+=6;k.pelvis.x-=.07f*(1-run);}
        }
        Vector3 leanAxis=Vector3.Cross(Vector3.up,travel);
        float lean=run*(23+accelerationLean)+FlightBlend*(attacking?12:34);
        pelvis.position=World(pelvisRest+k.pelvis)+Vector3.up*(lift-death*1.1f)+travel*(run*.10f)+sideways*(-Mathf.Cos(cycle*Mathf.PI*2)*.13f*run);
        pelvis.rotation=Quaternion.AngleAxis(lean,leanAxis)*transform.rotation*Quaternion.Euler(k.hips+Vector3.right*death*72)*rest[pelvis].world;
        waist.rotation=Quaternion.AngleAxis(lean,leanAxis)*transform.rotation*Quaternion.Euler(k.waist)*rest[waist].world;
        if(nemesis!=null)nemesis.ExtendLumbar(lean+k.chest.x);
        chest.rotation=Quaternion.AngleAxis(lean*1.05f,leanAxis)*transform.rotation*Quaternion.Euler(k.chest+Vector3.right*recoil*35)*rest[chest].world;
        head.rotation=transform.rotation*Quaternion.Euler(-4,k.chest.y*.20f,k.chest.z*.2f)*rest[head].world;
        // Apply armor recoil before limb solving so feet and weapon grips remain constrained.
        float impact=HitReactionWeight;
        Vector3 impactAxis=Vector3.Cross(Vector3.up,hitDirection);
        pelvis.rotation=Quaternion.AngleAxis(3f*impact,impactAxis)*pelvis.rotation;
        waist.rotation=Quaternion.AngleAxis(6f*impact,impactAxis)*waist.rotation;
        chest.rotation=Quaternion.AngleAxis(13f*impact,impactAxis)*chest.rotation;
        head.rotation=Quaternion.AngleAxis(5f*impact,impactAxis)*head.rotation;
        PlantSlip=0;
        PoseLeg(legL,k.leftFoot,k.leftFootAngles,run,lift,localTravel,attacking);
        PoseLeg(legR,k.rightFoot,k.rightFootAngles,run,lift,localTravel,attacking);
        Vector3 wrist=m.wrist,pole=m.elbow,bladeDirection=m.blade;
        if(!attacking)
        {
            wrist=Vector3.Lerp(wrist,new Vector3(.94f,1.92f,.18f)+new Vector3(.028f*swing,.025f*swing,-swing*.06f),run);
            pole=Vector3.Lerp(pole,new Vector3(1.1f,2.4f,-.48f),run);
            bladeDirection=Vector3.Slerp(bladeDirection,new Vector3(.20f,-.50f,-.84f),run);
            wrist=Vector3.Lerp(wrist,new Vector3(1.08f,2.04f,.36f),FlightBlend);
            pole=Vector3.Lerp(pole,new Vector3(1.5f,2.60f,-.38f),FlightBlend);
            bladeDirection=Vector3.Slerp(bladeDirection,new Vector3(.27f,-.28f,-.92f),FlightBlend);
        }
        wrist.y+=lift;pole.y+=lift;
        float stow=Mathf.SmoothStep(0,1,player.Stance.StowBlend);
        if(recoveredRifle==null)recoveredRifle=player.GetComponent<E01PlayerRifle>();
        Vector3 freeRight=World(new Vector3(.73f,1.91f+lift-swing*.14f,.15f-swing*.33f));
        Quaternion freeRightRotation=transform.rotation*Quaternion.Euler(-18+swing*18,0,12)*rest[handR].world;
        if(nemesis!=null&&health.IsDead)
        {
            wrist=new Vector3(.68f,1.92f-death*.98f,.24f+death*.38f);
            pole=new Vector3(.96f,2.30f-death*.92f,.08f);
            freeRight=World(wrist);
        }
        if(recoveredRifle!=null && recoveredRifle.Equipped)
        {
            freeRightRotation=transform.rotation*Quaternion.Euler(-78,0,10)*rest[handR].world;
            freeRight=World(new Vector3(.78f,2.61f+lift-run*.06f,.60f))-freeRightRotation*palmR;
        }
        var intake=GameManager.Instance?.equipmentLoop?.Absorption;
        if(intake!=null && intake.Busy)
        {
            freeRightRotation=transform.rotation*Quaternion.Euler(-78,0,10)*rest[handR].world;
            freeRight=World(new Vector3(.78f,2.61f+lift,.80f-.20f*Mathf.SmoothStep(0,1,intake.Progress)-.08f*intake.CatchPulse))-freeRightRotation*palmR;
        }
        // The wrist and elbow are authored first. The blade is mounted to the resulting
        // natural fist; an unreachable sword target never pulls the arm out of its pose.
        Vector3 desiredAxis=transform.TransformDirection(bladeDirection).normalized;
        Vector3 wristTarget=Vector3.Lerp(World(wrist),freeRight,stow);
        Vector3 elbowTarget=World(pole);
        if(attacking&&stow<.01f)elbowTarget=ElbowForBlade(armR,foreR,handR,wristTarget,desiredAxis,elbowTarget);
        Solve(armR,foreR,handR,wristTarget,elbowTarget,handR.rotation);
        Vector3 foreAxis=(handR.position-foreR.position).normalized;
        Quaternion natural=transform.rotation*rest[handR].world;
        natural=Quaternion.FromToRotation(natural*palmR.normalized,foreAxis)*natural;
        Quaternion mount=weaponInHand*Quaternion.AngleAxis(180*m.forwardGrip,Vector3.up);
        Vector3 projected=Vector3.ProjectOnPlane(desiredAxis,foreAxis).normalized;
        if(projected.sqrMagnitude<.1f)projected=natural*mount*Vector3.forward;
        natural=Quaternion.AngleAxis(Vector3.SignedAngle(natural*mount*Vector3.forward,projected,foreAxis),foreAxis)*natural;
        Quaternion flexed=Quaternion.FromToRotation(natural*mount*Vector3.forward,desiredAxis)*natural;
        natural=Quaternion.RotateTowards(natural,flexed,12);
        handR.rotation=Quaternion.Slerp(natural,freeRightRotation,stow);
        ForwardGrip=m.forwardGrip;
        if(articulatedHand!=null)articulatedHand.Pose(m.open*(1-stow));
        Vector3 cuttingEdge=Vector3.down;
        if(attacking)
        {
            var stroke=player.Melee.CurrentStroke;float sample=Mathf.Clamp(poseTime,stroke.contactStart,stroke.contactEnd);
            var tangent=combo.Evaluate(player.Melee.ComboStage,sample+.015f).blade-combo.Evaluate(player.Melee.ComboStage,sample-.015f).blade;
            float weight=Mathf.SmoothStep(0,1,Mathf.InverseLerp(stroke.contactStart-.12f,stroke.contactStart,poseTime));
            weight*=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(stroke.contactEnd+.04f,stroke.contactEnd+.22f,poseTime));
            if(tangent.sqrMagnitude>.0001f)cuttingEdge=Vector3.Slerp(Vector3.down,tangent.normalized,weight);
            if(poseTime<.08f)cuttingEdge=Vector3.Slerp(entryEdge,cuttingEdge,Mathf.SmoothStep(0,1,poseTime/.08f));
        }
        Vector3 heldAxis=handR.rotation*mount*Vector3.forward;
        Vector3 edgeWorld=Vector3.ProjectOnPlane(transform.TransformDirection(cuttingEdge),heldAxis);
        if(edgeWorld.sqrMagnitude<.001f)edgeWorld=Vector3.ProjectOnPlane(transform.forward,heldAxis);
        if(edgeWorld.sqrMagnitude<.001f)edgeWorld=Vector3.ProjectOnPlane(transform.right,heldAxis);
        blade.bladeRoot.rotation=ValkyrMotionProfile.Frame(heldAxis,edgeWorld)*rootInFrame;
        blade.bladeRoot.position+=handR.TransformPoint(palmR)-blade.grip.position;
        RightGripError=Vector3.Distance(handR.TransformPoint(palmR),blade.grip.position);
        // The left arm counterbalances the chest; it never seeks the sword or its hilt.
        float cut=attacking?Mathf.Sin(Mathf.InverseLerp(.08f,player.Melee.CurrentStroke.linkTime,poseTime)*Mathf.PI):0;
        float balance=attacking?Mathf.Sin(k.chest.y*Mathf.Deg2Rad):0;
        Vector3 freeHand=World(new Vector3(-.81f-cut*.48f,2.02f+lift+swing*.17f+cut*.42f,.14f+swing*.50f+balance*.72f));
        if(nemesis!=null&&health.IsDead)freeHand=World(new Vector3(-.68f,2.02f-death*1.00f,.22f+death*.36f));
        Quaternion freeRotation=transform.rotation*Quaternion.Euler(-25-swing*22-cut*25,cut*-30,-12)*rest[handL].world;
        PoseArm(armL,foreL,handL,freeHand,freeRotation,palmL,-1);
        handL.rotation=foreL.rotation*Quaternion.Inverse(rest[foreL].world)*rest[handL].world;
        LeftGripError=0;
        if(stow>0)
        {
            Quaternion torso=chest.rotation*Quaternion.Inverse(rest[chest].world);
            Quaternion back=torso*ValkyrMotionProfile.Frame(new Vector3(-.20f,-.97f,-.10f),Vector3.right);
            Vector3 backGrip=chest.position+torso*(nemesis!=null?new Vector3(-.65f,.20f,-.47f):new Vector3(.34f,.65f,-.98f));
            Vector3 backTip=backGrip+back*Vector3.forward*blade.Reach;
            backGrip.y+=Mathf.Max(0,player.transform.position.y+.16f-backTip.y);
            Vector3 contact=Vector3.Lerp(blade.grip.position,backGrip,stow);
            blade.bladeRoot.rotation=Quaternion.Slerp(blade.bladeRoot.rotation,back*rootInFrame,stow);
            blade.bladeRoot.position+=contact-blade.grip.position;
        }
        RightWristAngle=Vector3.Angle(handR.position-foreR.position,handR.TransformDirection(palmR));
        LeftWristAngle=Vector3.Angle(handL.position-foreL.position,handL.TransformDirection(palmL));
        foreach(var f in pose.followers)f.target.rotation=Quaternion.Slerp(f.from.rotation,f.to.rotation,.22f)*f.rest;
        pose.cannon.localRotation=pose.cannonRest;
        if(player.HasAimPoint)pose.cannon.rotation=Quaternion.FromToRotation(rig.muzzle.forward,(player.AimPoint-rig.muzzle.position).normalized)*pose.cannon.rotation;
        float low=float.PositiveInfinity;
        foreach(var c in pose.soleContacts)low=Mathf.Min(low,c.part.TransformPoint(c.localPoint).y-player.transform.position.y);
        FeetAboveGround=low;
        MotionState=health.IsDead?"Down":attacking?(player.Melee.ImpactHeld?"Impact":player.Melee.CurrentStroke.label):FlightBlend>.15f?"BoostFlight":RunBlend>.2f?"CombatRun":"反手低位拖持";
        if(dt>0){displayedKey=m;shownEdge=cuttingEdge;}
        rig.RefreshSockets();
        if(player.Stance.State!=WeaponStance.Sword)MotionState=player.Stance.State.ToString();
    }
    private void PoseArm(Transform upper,Transform lower,Transform hand,Vector3 wrist,Quaternion orientation,Vector3 palm,int side)
    {
        Vector3 finger=orientation*palm.normalized;
        Vector3 pole=wrist-finger*Vector3.Distance(lower.position,hand.position);
        // Find the elbow nearest the continuation of the wrist, with a small outward bias.
        pole+=transform.right*side*.08f;
        Solve(upper,lower,hand,wrist,pole,orientation);
        Vector3 axis=(hand.position-lower.position).normalized;
        Vector3 a=Vector3.ProjectOnPlane(lower.right,axis),b=Vector3.ProjectOnPlane(orientation*Vector3.right,axis);
        if(a.sqrMagnitude>.001f&&b.sqrMagnitude>.001f)
            lower.rotation=Quaternion.AngleAxis(Vector3.SignedAngle(a,b,axis),axis)*lower.rotation;
        hand.rotation=orientation;
    }
    private static Vector3 ElbowForBlade(Transform upper,Transform lower,Transform hand,Vector3 wrist,Vector3 bladeAxis,Vector3 authoredPole)
    {
        // Both arm lengths and the authored wrist stay fixed. Choose the elbow on
        // their intersection circle so a neutral fist can carry the intended blade
        // direction. This avoids flattening a wide cut into a wrist-limited diagonal.
        Vector3 origin=upper.position,axis=(wrist-origin).normalized;
        float a=Vector3.Distance(origin,lower.position),b=Vector3.Distance(lower.position,hand.position);
        float distance=Mathf.Clamp(Vector3.Distance(origin,wrist),Mathf.Abs(a-b)+.012f,a+b-.008f);
        Vector3 reachable=origin+axis*distance;
        float along=(a*a-b*b+distance*distance)/(2*distance);
        Vector3 center=origin+axis*along;
        float radius=Mathf.Sqrt(Mathf.Max(0,a*a-along*along));
        Vector3 u=Vector3.ProjectOnPlane(bladeAxis,axis);
        if(u.sqrMagnitude<.0001f||radius<.001f)return authoredPole;
        float offset=Mathf.Clamp(Vector3.Dot(reachable-center,bladeAxis)/u.magnitude,-radius*.999f,radius*.999f);
        u.Normalize();Vector3 v=Vector3.Cross(axis,u).normalized;
        float side=Mathf.Sqrt(Mathf.Max(0,radius*radius-offset*offset));
        Vector3 first=center+u*offset+v*side,second=center+u*offset-v*side;
        return (first-authoredPole).sqrMagnitude<(second-authoredPole).sqrMagnitude?first:second;
    }
    private void PoseLeg(Leg leg,Vector3 strikeFoot,Vector3 strikeAngles,float run,float lift,Vector3 travel,bool attacking)
    {
        float phase=Mathf.Repeat(cycle+(leg.side==1?.5f:0),1);
        Vector3 sole=new Vector3(leg.side*.48f,.025f,leg.side<0?.26f:-.25f);
        Vector3 angles=new Vector3(0,leg.side<0?-10:18,0);
        if(nemesis!=null){sole.x=leg.side*.38f;angles.y=0;}
        Quaternion orientation;
        Vector3 target;
        bool plantThisFrame=false;
        bool wasPlanted=leg.planted;
        if(attacking&&FlightBlend<.15f)
        {
            float start=Mathf.SmoothStep(0,1,Mathf.Clamp01(player.Melee.AttackElapsed/.13f));
            sole=Vector3.Lerp(leg.side<0?attackFootL:attackFootR,strikeFoot,start);sole.y+=.025f;
            orientation=player.transform.rotation*Quaternion.Euler(nemesis!=null?strikeAngles*.35f:strikeAngles)*rest[leg.foot].world;
            target=attackOrigin+player.transform.rotation*sole;
            bool support=leg.side==(player.Melee.ComboStage==1?1:-1);
            bool contact=player.Melee.AttackElapsed>=player.Melee.CurrentStroke.contactStart
                && player.Melee.AttackElapsed<=player.Melee.CurrentStroke.contactEnd;
            if(support&&contact)
            {
                if(!leg.planted){leg.plant=target;leg.plantRotation=orientation;}
                target=leg.plant;orientation=leg.plantRotation;
            }
            leg.planted=support&&contact;
        }
        else
        {
            Vector3 gait=RunPath(phase,out float pitch);
            Vector3 moving=new Vector3(leg.side*.40f,gait.y+.025f,0)+travel*gait.x;
            sole=Vector3.Lerp(sole,moving,run);
            angles=Vector3.Lerp(angles,new Vector3(pitch,Mathf.Clamp(Vector3.SignedAngle(Vector3.forward,travel,Vector3.up),-60,60)+leg.side*5,0),run);
            Vector3 air=new Vector3(leg.side*.43f,.025f+lift+(leg.side==1?.40f:.10f),leg.side==1?-.45f:-.95f);
            sole=Vector3.Lerp(sole,air,FlightBlend);
            angles=Vector3.Lerp(angles,new Vector3(38,leg.side*8,leg.side*5),FlightBlend);
            orientation=transform.rotation*Quaternion.Euler(angles)*rest[leg.foot].world;
            target=World(sole);
            plantThisFrame=run>.85f&&phase<.36f&&FlightBlend<.01f;
            if(plantThisFrame)
            {
                if(!leg.planted){leg.plant=target;leg.plantRotation=orientation;leg.previousAnkle=leg.foot.position;}
                target=leg.plant;
            }
            leg.planted=plantThisFrame;
        }
        float bottom=float.PositiveInfinity;
        foreach(var point in leg.sole)bottom=Mathf.Min(bottom,(orientation*point).y);
        target-=Vector3.up*bottom;
        Vector3 pole=World(new Vector3(leg.side*.56f,1.18f+lift,1.40f));
        if(run>.6f)pole=pelvis.position+transform.TransformDirection(travel)*1.6f+transform.right*leg.side*.2f;
        Solve(leg.thigh,leg.knee,leg.foot,target,pole,orientation);
        if(plantThisFrame&&!wasPlanted&&!player.IsBoosting&&!player.IsDashing&&GameManager.Instance!=null&&GameManager.Instance.IsCombatActive)
            GameAudio.PlayAt(GameAudioCue.Footstep,leg.foot.position,.23f);
        if(plantThisFrame&&wasPlanted)
        {
            Vector3 drift=leg.foot.position-leg.previousAnkle;drift.y=0;
            PlantSlip=Mathf.Max(PlantSlip,drift.magnitude);leg.previousAnkle=leg.foot.position;
        }
        leg.previousAnkle=leg.foot.position;
    }
    private static readonly float[] runTimes={.36f,.53f,.70f,.87f,1};
    private static readonly Vector3[] runPoints={new Vector3(-.784f,0,26),new Vector3(-.92f,.63f,62),new Vector3(-.05f,.73f,15),new Vector3(.76f,.31f,-16),new Vector3(.62f,0,-8)};
    private static Vector3 RunPath(float phase,out float pitch)
    {
        if(phase<.36f){pitch=phase<.1f?Mathf.Lerp(-8,0,phase/.1f):Mathf.Lerp(0,26,Mathf.InverseLerp(.22f,.36f,phase));return new Vector3(.62f-3.9f*phase,0,0);}
        int i=0;while(i<runTimes.Length-2&&phase>runTimes[i+1])i++;
        float h=runTimes[i+1]-runTimes[i],t=(phase-runTimes[i])/h;
        Vector3 a=runPoints[i],b=runPoints[i+1];
        Vector3 m0=i==0?new Vector3(-3.9f,2.5f,0):(b-runPoints[i-1])/(runTimes[i+1]-runTimes[i-1]);
        Vector3 m1=i==runTimes.Length-2?new Vector3(-3.9f,-2.4f,0):(runPoints[i+2]-a)/(runTimes[i+2]-runTimes[i]);
        Vector3 value=(2*t*t*t-3*t*t+1)*a+(t*t*t-2*t*t+t)*h*m0+(-2*t*t*t+3*t*t)*b+(t*t*t-t*t)*h*m1;
        pitch=value.z;return new Vector3(value.x,Mathf.Max(0,value.y),0);
    }
    private static void Solve(Transform upper,Transform lower,Transform end,Vector3 target,Vector3 pole,Quaternion rotation)
    {
        Vector3 origin=upper.position,offset=target-origin;
        float a=Vector3.Distance(origin,lower.position),b=Vector3.Distance(lower.position,end.position);
        float d=Mathf.Clamp(offset.magnitude,Mathf.Abs(a-b)+.012f,a+b-.008f);
        Vector3 axis=offset.normalized,bend=Vector3.ProjectOnPlane(pole-origin,axis).normalized;
        if(bend.sqrMagnitude<.1f)bend=Vector3.Cross(axis,Vector3.right).normalized;
        float along=(a*a-b*b+d*d)/(2*d);
        Vector3 elbow=origin+axis*along+bend*Mathf.Sqrt(Mathf.Max(0,a*a-along*along));
        upper.rotation=Quaternion.FromToRotation(lower.position-origin,elbow-origin)*upper.rotation;
        lower.rotation=Quaternion.FromToRotation(end.position-lower.position,origin+axis*d-lower.position)*lower.rotation;
        end.rotation=rotation;
    }
    private void OnDestroy()
    {
        if(health!=null)health.OnDamaged-=OnDamage;
        if(player!=null){if(player.weaponController!=null)player.weaponController.BeamFired-=OnShot;if(player.Melee!=null){player.Melee.AttackStarted-=BeginStrike;player.Melee.AttackCancelled-=BeginReturn;}}
    }
}
