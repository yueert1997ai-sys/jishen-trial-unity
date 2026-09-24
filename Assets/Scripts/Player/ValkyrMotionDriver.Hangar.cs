using UnityEngine;

public sealed partial class ValkyrMotionDriver
{
    // Katoki-style model display: grounded outward toes, long legs, level shoulders,
    // a lightly tucked chin and relaxed arms. Weapon grips are resolved by LoadoutVisual.
    void ApplyKatokiPose()
    {
        RunBlend=FlightBlend=0;
        pelvis.position+=transform.TransformDirection(new Vector3(0,-.055f,-.045f));
        waist.rotation=transform.rotation*Quaternion.Euler(-2,0,0)*rest[waist].world;
        chest.rotation=transform.rotation*Quaternion.Euler(-3,0,0)*rest[chest].world;
        head.rotation=transform.rotation*Quaternion.Euler(5,0,0)*rest[head].world;
        KatokiLeg(legL);KatokiLeg(legR);
        KatokiArm(armL,foreL,handL,-1);KatokiArm(armR,foreR,handR,1);
        foreach(var f in pose.followers)f.target.rotation=Quaternion.Slerp(f.from.rotation,f.to.rotation,.22f)*f.rest;
        if(nemesis!=null)nemesis.ParkBlade();
        else
        {
            Quaternion torso=chest.rotation*Quaternion.Inverse(rest[chest].world);
            Quaternion frame=torso*ValkyrMotionProfile.Frame(new Vector3(-.20f,-.97f,-.10f),Vector3.right);
            blade.bladeRoot.rotation=frame*rootInFrame;
            Vector3 grip=chest.position+torso*new Vector3(.34f,.65f,-.98f);
            grip.y+=Mathf.Max(0,player.transform.position.y+.16f-(grip+frame*Vector3.forward*blade.Reach).y);
            blade.bladeRoot.position+=grip-blade.grip.position;
        }
        FeetAboveGround=float.PositiveInfinity;
        foreach(var c in pose.soleContacts)FeetAboveGround=Mathf.Min(FeetAboveGround,c.part.TransformPoint(c.localPoint).y-player.transform.position.y);
        PlantSlip=0;MotionState="Hangar/Ka";rig.RefreshSockets();
    }
    void KatokiLeg(Leg leg)
    {
        Vector3 target=leg.foot.position+transform.right*(leg.side*.16f);
        Quaternion neutral=transform.rotation*rest[leg.foot].world;
        float existingYaw=Vector3.SignedAngle(transform.forward,neutral*Vector3.forward,transform.up);
        Quaternion angle=Quaternion.AngleAxis(leg.side*15-existingYaw,transform.up)*neutral;
        float bottom=float.PositiveInfinity;
        foreach(var point in leg.sole)bottom=Mathf.Min(bottom,(angle*point).y);
        if(float.IsInfinity(bottom))bottom=0;
        target.y=player.transform.position.y+.025f-bottom;
        Solve(leg.thigh,leg.knee,leg.foot,target,leg.knee.position+transform.forward*.8f,angle);
        leg.planted=false;
    }
    void KatokiArm(Transform upper,Transform lower,Transform hand,int side)
    {
        float reach=Vector3.Distance(upper.position,lower.position)+Vector3.Distance(lower.position,hand.position);
        Vector3 target=upper.position+transform.TransformDirection(new Vector3(side*.12f,-reach*.988f,.035f));
        Quaternion angle=transform.rotation*Quaternion.Euler(0,0,-side*4)*rest[hand].world;
        Solve(upper,lower,hand,target,upper.position+transform.TransformDirection(new Vector3(side*.4f,-.8f,-.12f)),angle);
        hand.GetComponentInChildren<ValkyrHandGrip>()?.Pose(0);
    }
}
