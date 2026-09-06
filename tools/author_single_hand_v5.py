"""Apply the V5 single-hand pose revision to the prior authored driver."""
from pathlib import Path
root=Path(__file__).resolve().parents[1]
if 'private void ApplyPose(float dt,float poseTime)' in (root/'Assets/Scripts/Player/ValkyrMotionDriver.cs').read_text(encoding='utf-8'):
    raise SystemExit('This one-time V4-to-V5 authoring pass is already applied; edit the current C# profile directly.')
p=root/'Assets/Scripts/Player/ValkyrMotionProfile.cs'
s=p.read_text(encoding='utf-8')
a=s.index('        P(0,');b=s.index('\n    };',a)
s=s[:a]+'''        P(0,    V(0,-.10f,0),       V(0,-8,0), V(3,-5,0), V(5,-8,-2), V(.87f,2.07f,.26f),-28,0,V(-.48f,0,.26f),V(.48f,0,-.25f)),
        P(.13f, V(.18f,-.20f,-.10f),V(-3,23,-6),V(-5,35,-7),V(-6,48,-9),V(1.06f,2.56f,-.19f),-42,1,V(-.48f,.09f,.36f),V(.48f,0,-.25f)),
        P(.20f, V(.21f,-.24f,-.10f),V(4,12,-3),V(-2,32,-6),V(-4,52,-9),V(1.12f,2.48f,-.12f),-32,1,V(-.52f,.08f,.65f),V(.48f,0,-.25f)),
        P(.275f,V(-.12f,-.28f,.12f),V(10,-25,5),V(12,-12,3),V(15,2,2),V(.71f,2.28f,.68f),68,1,V(-.55f,0,.91f),V(.48f,0,-.25f)),
        P(.34f, V(-.29f,-.33f,.14f),V(12,-39,8),V(15,-47,9),V(18,-61,11),V(-.02f,2.05f,.87f),132,1,V(-.55f,0,.91f),V(.48f,.035f,-.25f)),
        P(.40f, V(-.28f,-.31f,.12f),V(9,-37,7),V(14,-51,8),V(19,-78,11),V(-.43f,1.98f,.53f),168,1,V(-.55f,0,.91f),V(.48f,.07f,-.20f)),
        P(.49f, V(-.23f,-.26f,.08f),V(6,-29,4),V(10,-43,6),V(14,-68,8),V(-.40f,2.02f,.48f),177,1,V(-.55f,0,.89f),V(.48f,.13f,-.10f)),
        P(.61f, V(-.08f,-.15f,.02f),V(1,-16,1),V(6,-25,2),V(9,-39,1),V(.22f,2.17f,.53f),205,.35f,V(-.50f,.03f,.76f),V(.48f,.06f,.12f)),
        P(.72f, V(0,-.10f,0),       V(0,-8,0),V(3,-5,0),V(5,-8,-2),V(.87f,2.07f,.26f),220,0,V(-.48f,0,.71f),V(.48f,0,.20f))'''+s[b:]
s=s.replace('Frame(new Vector3(.38f,.43f,.82f),new Vector3(.30f,-.86f,.38f))','Frame(new Vector3(.30f,-.50f,.81f),Vector3.down)').replace('new Vector3(.8f,.6f,0)','new Vector3(.985f,.17f,0)')
p.write_text(s,encoding='utf-8')
p=root/'Assets/Scripts/Player/ValkyrMotionDriver.cs';s=p.read_text(encoding='utf-8')
s=s.replace('SlashDuration=.84f, ContactStart=.285f, ContactEnd=.48f','SlashDuration=.72f, ContactStart=.20f, ContactEnd=.40f')
s=s.replace('weaponInHand,leftHandInWeapon,attackHeading','weaponInHand,attackHeading,heldRotation,rootInFrame').replace('cycle,death,recoil,supportBlend=1,supportRoll','cycle,death,recoil,previousPoseTime')
s=s.replace('    private Leg legL,legR;','    private Vector3 heldPosition;\n    private Leg legL,legR;')
a=s.index('        Vector3 knucklesL=');b=s.index('        legL=MakeLeg',a)
s=s[:a]+'''        heldPosition=blade.bladeRoot.localPosition;heldRotation=blade.bladeRoot.localRotation;
        rootInFrame=Quaternion.Inverse(neutral)*blade.bladeRoot.rotation;
'''+s[b:]
s=s.replace('        attackOrigin=player.transform.position;', '        previousPoseTime=0;\n        attackOrigin=player.transform.position;')
# Keep gait integration once per rendered frame, evaluate cut poses at a fixed sub-frame resolution.
a=s.index('        foreach(var item in rest)')
s=s[:a]+'''        if(attacking)
        {
            float end=Mathf.Min(ContactEnd,player.Melee.AttackElapsed);
            float start=Mathf.Max(ContactStart,previousPoseTime);
            if(end>=start)
            {
                int count=Mathf.Max(1,Mathf.CeilToInt((end-start)*240));
                for(int i=0;i<=count;i++)
                {
                    float time=Mathf.Lerp(start,end,i/(float)count);
                    ApplyPose(0,time);
                    blade.RecordCutSample(blade.grip.position,blade.tip.position);
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
'''+s[a:]
s=s.replace('        pose.assemblyRoot.localPosition=pose.assemblyRestPosition;', '        pose.assemblyRoot.localPosition=pose.assemblyRestPosition;\n        blade.bladeRoot.SetLocalPositionAndRotation(heldPosition,heldRotation);')
s=s.replace('profile.Evaluate(player.Melee.AttackElapsed)','profile.Evaluate(poseTime)')
s=s.replace('new Vector3(.12f,2.28f,.69f)','new Vector3(.91f,2.19f,.36f)').replace('new Vector3(.27f,.12f,.96f),new Vector3(.3f,-.93f,.02f)','new Vector3(.55f,-.25f,-.80f),Vector3.down')
a=s.index('        Quaternion rotR=');b=s.index('        if(!attacking&&run>.65f)',a)
s=s[:a]+'''        Quaternion rotR=worldWeapon*Quaternion.Inverse(weaponInHand);
        Vector3 desired=ReachableGrip(World(grip),rotR);
'''+s[b:]
s=s.replace('ReachableGrip(World(grip),rotR,rotL,worldWeapon*Vector3.forward,supportBlend)','ReachableGrip(World(grip),rotR)')
a=s.index('        Vector3 wristR=');b=s.index('        RightWristAngle=',a)
s=s[:a]+'''        float stow=Mathf.SmoothStep(0,1,player.Stance.StowBlend);
        Vector3 freeRight=World(new Vector3(.73f,1.91f+lift-swing*.14f,.15f-swing*.33f));
        Quaternion freeRightRotation=transform.rotation*Quaternion.Euler(-18+swing*18,0,12)*rest[handR].world;
        Vector3 wristR=Vector3.Lerp(desired-rotR*palmR,freeRight,stow);
        rotR=Quaternion.Slerp(rotR,freeRightRotation,stow);
        PoseArm(armR,foreR,handR,wristR,rotR,palmR,1);
        RightGripError=Vector3.Distance(desired,blade.grip.position);
        // The left arm counterbalances the chest; it never seeks the sword or its hilt.
        float cut=attacking?Mathf.Sin(Mathf.InverseLerp(.13f,.49f,poseTime)*Mathf.PI):0;
        Vector3 freeHand=World(new Vector3(-.81f-cut*.32f,2.02f+lift+swing*.17f+cut*.28f,.14f+swing*.50f-cut*.65f));
        Quaternion freeRotation=transform.rotation*Quaternion.Euler(-25-swing*22-cut*25,cut*-30,-12)*rest[handL].world;
        PoseArm(armL,foreL,handL,freeHand,freeRotation,palmL,-1);
        handL.rotation=foreL.rotation*Quaternion.Inverse(rest[foreL].world)*rest[handL].world;
        LeftGripError=0;
        if(stow>0)
        {
            Quaternion torso=chest.rotation*Quaternion.Inverse(rest[chest].world);
            Quaternion back=torso*ValkyrMotionProfile.Frame(new Vector3(-.20f,-.97f,-.10f),Vector3.right);
            Vector3 backGrip=chest.position+torso*new Vector3(.34f,.65f,-.98f);
            Vector3 backTip=backGrip+back*Vector3.forward*blade.Reach;
            backGrip.y+=Mathf.Max(0,player.transform.position.y+.16f-backTip.y);
            Vector3 contact=Vector3.Lerp(blade.grip.position,backGrip,stow);
            blade.bladeRoot.rotation=Quaternion.Slerp(blade.bladeRoot.rotation,back*rootInFrame,stow);
            blade.bladeRoot.position+=contact-blade.grip.position;
        }
'''+s[b:]
a=s.index('    private Quaternion SupportRotation(');b=s.index('    private void PoseLeg(',a)
s=s[:a]+'''    private Vector3 ReachableGrip(Vector3 grip,Quaternion handRotation)
    {
        float reach=Vector3.Distance(armR.position,foreR.position)+Vector3.Distance(foreR.position,handR.position)-.045f;
        Vector3 center=armR.position+handRotation*palmR,offset=grip-center;
        return center+Vector3.ClampMagnitude(offset,reach);
    }
'''+s[b:]
s=s.replace('rig.RefreshSockets();','rig.RefreshSockets();\n        if(player.Stance.State!=WeaponStance.Sword)MotionState=player.Stance.State.ToString();')
p.write_text(s,encoding='utf-8')
print('Single-hand profile and pose evaluator authored.')
