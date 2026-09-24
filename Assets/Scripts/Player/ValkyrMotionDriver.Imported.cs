using System;
using UnityEngine;

public sealed partial class ValkyrMotionDriver
{
    ImportedBodyMotion importedMotion;
    ImportedBodyMotion.Joint[] importedPose,importedOther,importedEntry;
    float importedScale;
    NemesisRaikenBlade fittedBlade;
    bool importedLeftGrounded=true,importedRightGrounded=true;
    public bool ImportedMotionEnabled=>importedMotion!=null;
    public string ImportedClip {get;private set;}="";
    public float ImportedSampleTime {get;private set;}
    void InitializeImportedMotion()
    {
        fittedBlade=blade.bladeRoot.GetComponent<NemesisRaikenBlade>();
        importedMotion=ImportedBodyMotion.Load();if(importedMotion==null)return;
        importedPose=new ImportedBodyMotion.Joint[importedMotion.Count];importedOther=new ImportedBodyMotion.Joint[importedMotion.Count];importedEntry=new ImportedBodyMotion.Joint[importedMotion.Count];
        importedMotion.clips["sbr_idle"].Sample(0,importedPose);CaptureImportedEntry();
        importedScale=(Vector3.Distance(legR.thigh.position,legR.knee.position)+Vector3.Distance(legR.knee.position,legR.foot.position)) /
            (Vector3.Distance(importedMotion.bind[17].p,importedMotion.bind[18].p)+Vector3.Distance(importedMotion.bind[18].p,importedMotion.bind[20].p));
    }
    void CaptureImportedEntry(){if(importedMotion!=null)Array.Copy(importedPose,importedEntry,importedPose.Length);}
    void SampleImported(string name,float seconds,bool loop=false)
    {
        var clip=importedMotion.clips[name];ImportedClip=name;ImportedSampleTime=loop?Mathf.Repeat(seconds,clip.Duration):Mathf.Clamp(seconds,0,clip.Duration);
        clip.Sample(ImportedSampleTime,importedPose);
    }
    void BlendImported(string name,float seconds,float weight)
    {
        importedMotion.clips[name].Sample(seconds,importedOther);
        for(int i=0;i<importedPose.Length;i++)importedPose[i]=ImportedBodyMotion.Joint.Blend(importedPose[i],importedOther[i],weight);
    }
    Quaternion ImportedDelta(int index)=>importedPose[index].q*Quaternion.Inverse(importedMotion.bind[index].q);
    Quaternion ImportedRotation(Transform joint,int index)=>transform.rotation*ImportedDelta(index)*rest[joint].world;
    void ImportedLimb(Transform upper,Transform lower,Transform end,int a,int b,int c)
    {
        upper.rotation=ImportedRotation(upper,a);
        Vector3 dir=transform.TransformDirection(importedPose[b].p-importedPose[a].p);
        upper.rotation=Quaternion.FromToRotation(lower.position-upper.position,dir)*upper.rotation;
        lower.rotation=ImportedRotation(lower,b);
        dir=transform.TransformDirection(importedPose[c].p-importedPose[b].p);
        lower.rotation=Quaternion.FromToRotation(end.position-lower.position,dir)*lower.rotation;
        end.rotation=ImportedRotation(end,c);
    }
    // Hades-inspired separation of preparation, fast active travel and cancellable recovery.
    // These fractions retime the imported animation only; hit and combo clocks retain ownership.
    static float CutSourcePhase(float time,ValkyrComboProfile.Stroke stroke)
    {
        if(time<stroke.contactStart)return Mathf.Lerp(0,.22f,Mathf.SmoothStep(0,1,time/Mathf.Max(.001f,stroke.contactStart)));
        float u=Mathf.InverseLerp(stroke.contactStart,stroke.contactEnd,time);
        return Mathf.Lerp(.22f,1,Mathf.SmoothStep(0,1,u));
    }
    void ApplyImportedMotion(float dt,float poseTime)
    {
        bool attacking=player.Melee.IsAttacking;
        string weapon=player.Stance.State==WeaponStance.Ranged?"rfl":"sbr";
        if(attacking)
        {
            var stroke=player.Melee.CurrentStroke;string name="cut"+(player.Melee.ComboStage+1);
            // Retain the original body trajectories and align their cut/recovery
            // phases to the existing gameplay clock (also used by contact subsamples).
            if(poseTime<=stroke.contactEnd)
                SampleImported(name+"_s",CutSourcePhase(poseTime,stroke)*importedMotion.clips[name+"_s"].Duration);
            else SampleImported(name+"_e",Mathf.SmoothStep(0,1,Mathf.InverseLerp(stroke.contactEnd,stroke.duration,poseTime))*importedMotion.clips[name+"_e"].Duration);
            float entry=Mathf.SmoothStep(0,1,Mathf.Clamp01(poseTime/Mathf.Min(.04f,stroke.contactStart)));
            for(int i=0;i<importedPose.Length;i++)importedPose[i]=ImportedBodyMotion.Joint.Blend(importedEntry[i],importedPose[i],entry);
        }
        else
        {
            SampleImported(weapon+"_idle",Time.time,true);
            string run=weapon+(Vector3.Dot(player.Velocity,player.transform.forward)<-.3f?"_backrun":"_run");
            BlendImported(run,Mathf.Repeat(cycle,1)*importedMotion.clips[run].Duration,RunBlend*(nemesis==null?.65f:1));
            BlendImported(weapon+"_boost",Mathf.Repeat(Time.time,importedMotion.clips[weapon+"_boost"].Duration),FlightBlend);
            if(FlightBlend>.5f)ImportedClip=weapon+"_boost";else if(RunBlend>.2f)ImportedClip=run;
            if(cancelBlend>0)
                for(int i=0;i<importedPose.Length;i++)importedPose[i]=ImportedBodyMotion.Joint.Blend(importedEntry[i],importedPose[i],Mathf.SmoothStep(0,1,1-cancelBlend/CancelReturnSeconds));
        }
        float lift=.28f*FlightBlend;
        Vector3 sourceHip=importedPose[2].p-importedMotion.bind[2].p;
        if(!attacking&&nemesis==null)sourceHip.y*=.5f;
        pelvis.position=World(pelvisRest+sourceHip*importedScale)+Vector3.up*lift;
        pelvis.rotation=ImportedRotation(pelvis,2);
        waist.rotation=transform.rotation*Quaternion.Slerp(ImportedDelta(2),ImportedDelta(3),.5f)*rest[waist].world;
        chest.rotation=ImportedRotation(chest,3);
        // The broad extracted chest needs a quarter stance for its two-hand
        // rifle. Keep the imported translation/lean and the head's aim frame.
        var held=GetComponent<LoadoutVisual>();
        if(nemesis==null&&held!=null&&held.authoredGripFrames&&!attacking&&weapon=="rfl"&&player.Loadout.Selected==PrimaryWeapon.M7)
            chest.rotation=Quaternion.AngleAxis(48,player.transform.up)*chest.rotation;
        head.rotation=ImportedRotation(head,4);
        // Add velocity-driven chassis articulation to the sampled body, before limbs/weapon IK.
        if(!attacking&&nemesis!=null)
        {
            Vector3 local=player.transform.InverseTransformDirection(player.Velocity)/Mathf.Max(1,player.stats.MoveSpeed);
            float pitch=Mathf.Clamp(local.z,-1,1)*(12+Mathf.Max(0,accelerationLean)*.6f);
            float bank=-Mathf.Clamp(local.x,-1,1)*8;
            Quaternion lean=Quaternion.Euler(pitch,0,bank);
            pelvis.rotation=player.transform.rotation*lean*Quaternion.Inverse(player.transform.rotation)*pelvis.rotation;
            waist.rotation=player.transform.rotation*Quaternion.Euler(pitch*.65f,0,bank*.65f)*Quaternion.Inverse(player.transform.rotation)*waist.rotation;
            chest.rotation=player.transform.rotation*Quaternion.Euler(pitch*.45f,0,bank*.5f)*Quaternion.Inverse(player.transform.rotation)*chest.rotation;
            nemesis.ExtendLumbar(pitch);
        }
        float impact=HitReactionWeight;var impactAxis=Vector3.Cross(Vector3.up,hitDirection);
        chest.rotation=Quaternion.AngleAxis(13*impact,impactAxis)*chest.rotation;
        head.rotation=Quaternion.AngleAxis(5*impact,impactAxis)*head.rotation;
        ImportedLimb(armR,foreR,handR,6,7,9);ImportedLimb(armL,foreL,handL,12,13,15);
        ImportedLimb(legR.thigh,legR.knee,legR.foot,17,18,20);ImportedLimb(legL.thigh,legL.knee,legL.foot,22,23,25);
        // Rotate locomotion legs toward travel, retaining aim authority on the torso.
        if(!attacking&&player.Velocity.sqrMagnitude>.04f)
        {
            float yaw=Vector3.SignedAngle(player.transform.forward,player.Velocity,Vector3.up);
            if(Mathf.Abs(yaw)>90)yaw=Mathf.DeltaAngle(180,yaw);
            var turn=Quaternion.AngleAxis(yaw*RunBlend*(1-FlightBlend),Vector3.up);
            legL.thigh.rotation=turn*legL.thigh.rotation;legR.thigh.rotation=turn*legR.thigh.rotation;
        }
        float low=float.PositiveInfinity;
        foreach(var c in pose.soleContacts)low=Mathf.Min(low,c.part.TransformPoint(c.localPoint).y-player.transform.position.y);
        if(!float.IsInfinity(low))pose.assemblyRoot.position+=Vector3.up*(.025f+lift-low);
        PlantSlip=0;
        ImportedPlant(legL,attacking&&FlightBlend<.15f&&player.Melee.IsCutting&&player.Melee.ComboStage!=1);
        ImportedPlant(legR,attacking&&FlightBlend<.15f&&player.Melee.IsCutting&&player.Melee.ComboStage==1);
        Quaternion weaponRotation=transform.rotation*importedPose[10].q;
        if(fittedBlade!=null)
            for(int pass=0;pass<3;pass++)
            {
                handR.rotation=weaponRotation*Quaternion.Inverse(weaponInHand);
                weaponRotation=fittedBlade.FitRotation(weaponRotation,handR.TransformPoint(palmR),player.transform.position.y,player.transform.forward);
            }
        Vector3 axis=weaponRotation*Vector3.forward,edge=weaponRotation*Vector3.up;
        handR.rotation=weaponRotation*Quaternion.Inverse(weaponInHand);
        blade.bladeRoot.rotation=ValkyrMotionProfile.Frame(axis,edge)*rootInFrame;
        blade.bladeRoot.position+=handR.TransformPoint(palmR)-blade.grip.position;
        RightGripError=Vector3.Distance(handR.TransformPoint(palmR),blade.grip.position);LeftGripError=0;ForwardGrip=1;
        if(articulatedHand!=null)articulatedHand.Pose(0);
        float stow=Mathf.SmoothStep(0,1,player.Stance.StowBlend);
        if(stow>0)
        {
            Quaternion torso=chest.rotation*Quaternion.Inverse(rest[chest].world);
            Quaternion back=torso*ValkyrMotionProfile.Frame(new Vector3(-.20f,-.97f,-.10f),Vector3.right);
            Vector3 grip=chest.position+torso*(nemesis!=null?new Vector3(-.65f,.20f,-.47f):new Vector3(.34f,.65f,-.98f));
            var tip=grip+back*Vector3.forward*blade.Reach;grip.y+=Mathf.Max(0,player.transform.position.y+.16f-tip.y);
            Vector3 contact=Vector3.Lerp(blade.grip.position,grip,stow);
            blade.bladeRoot.rotation=Quaternion.Slerp(blade.bladeRoot.rotation,back*rootInFrame,stow);blade.bladeRoot.position+=contact-blade.grip.position;
            if(fittedBlade!=null)
            {
                var frame=blade.bladeRoot.rotation*Quaternion.Inverse(rootInFrame);
                blade.bladeRoot.rotation=fittedBlade.FitRotation(frame,contact,player.transform.position.y,player.transform.forward)*rootInFrame;
                blade.bladeRoot.position+=contact-blade.grip.position;
            }
        }
        foreach(var f in pose.followers)f.target.rotation=Quaternion.Slerp(f.from.rotation,f.to.rotation,.22f)*f.rest;
        pose.cannon.localRotation=pose.cannonRest;
        if(player.HasAimPoint)pose.cannon.rotation=Quaternion.FromToRotation(rig.muzzle.forward,(player.AimPoint-rig.muzzle.position).normalized)*pose.cannon.rotation;
        low=float.PositiveInfinity;foreach(var c in pose.soleContacts)low=Mathf.Min(low,c.part.TransformPoint(c.localPoint).y-player.transform.position.y);
        FeetAboveGround=low;
        if(dt>0)
        {
            bool walking=!attacking&&RunBlend>.12f&&FlightBlend<.1f&&!player.IsDashing&&!player.IsBoosting;
            ImportedFootstep(legL,walking,ref importedLeftGrounded);
            ImportedFootstep(legR,walking,ref importedRightGrounded);
        }
        RightWristAngle=Vector3.Angle(handR.position-foreR.position,handR.TransformDirection(palmR));
        LeftWristAngle=Vector3.Angle(handL.position-foreL.position,handL.TransformDirection(palmL));
        MotionState=player.Melee.ImpactHeld?"Impact":"GB4/"+ImportedClip;
        rig.RefreshSockets();
    }
    void ImportedFootstep(Leg leg,bool walking,ref bool grounded)
    {
        float bottom=float.PositiveInfinity;
        foreach(var point in leg.sole)bottom=Mathf.Min(bottom,leg.foot.TransformPoint(point).y-player.transform.position.y);
        bool contact=bottom<(grounded?.10f:.055f);
        if(walking&&contact&&!grounded)GameAudio.PlayAt(GameAudioCue.Footstep,leg.foot.position,.17f);
        grounded=!walking||contact;
    }
    void ImportedPlant(Leg leg,bool planted)
    {
        if(planted)
        {
            if(!leg.planted){leg.plant=leg.foot.position;leg.plantRotation=leg.foot.rotation;}
            Solve(leg.thigh,leg.knee,leg.foot,leg.plant,leg.knee.position,leg.plantRotation);
            PlantSlip=Mathf.Max(PlantSlip,Vector3.Distance(leg.foot.position,leg.plant));
        }
        leg.planted=planted;
    }
}
