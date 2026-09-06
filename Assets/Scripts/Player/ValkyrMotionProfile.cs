using System;
using UnityEngine;

[CreateAssetMenu(menuName="MECH TRIAL/Valkyr full body motion")]
public sealed class ValkyrMotionProfile : ScriptableObject
{
    [Serializable]
    public struct Pose
    {
        public float time, cutAngle, cutWeight;
        public Vector3 pelvis, hips, waist, chest, grip, leftFoot, rightFoot;
        public Vector3 leftFootAngles, rightFootAngles;
    }
    public Pose[] slash;
    // Separate poses for loading, hip initiation, contact, overshoot and recovery.
    public static Pose[] DefaultSlash() => new [] {
        P(0,    V(0,-.10f,0),       V(0,-8,0), V(3,-5,0), V(5,-8,-2), V(.87f,2.07f,.26f),-28,0,V(-.48f,0,.26f),V(.48f,0,-.25f)),
        P(.13f, V(.18f,-.20f,-.10f),V(-3,23,-6),V(-5,35,-7),V(-6,48,-9),V(1.06f,2.56f,-.19f),-42,1,V(-.48f,.09f,.36f),V(.48f,0,-.25f)),
        P(.20f, V(.21f,-.24f,-.10f),V(4,12,-3),V(-2,32,-6),V(-4,52,-9),V(1.12f,2.48f,-.12f),-32,1,V(-.52f,.08f,.65f),V(.48f,0,-.25f)),
        P(.275f,V(-.12f,-.28f,.12f),V(10,-25,5),V(12,-12,3),V(15,2,2),V(.71f,2.28f,.68f),68,1,V(-.55f,0,.91f),V(.48f,0,-.25f)),
        P(.34f, V(-.29f,-.33f,.14f),V(12,-39,8),V(15,-47,9),V(18,-61,11),V(-.02f,2.05f,.87f),132,1,V(-.55f,0,.91f),V(.48f,.035f,-.25f)),
        P(.40f, V(-.28f,-.31f,.12f),V(9,-37,7),V(14,-51,8),V(19,-78,11),V(-.43f,1.98f,.53f),168,1,V(-.55f,0,.91f),V(.48f,.07f,-.20f)),
        P(.49f, V(-.23f,-.26f,.08f),V(6,-29,4),V(10,-43,6),V(14,-68,8),V(-.40f,2.02f,.48f),177,1,V(-.55f,0,.89f),V(.48f,.13f,-.10f)),
        P(.61f, V(-.08f,-.15f,.02f),V(1,-16,1),V(6,-25,2),V(9,-39,1),V(.22f,2.17f,.53f),205,.35f,V(-.50f,.03f,.76f),V(.48f,.06f,.12f)),
        P(.72f, V(0,-.10f,0),       V(0,-8,0),V(3,-5,0),V(5,-8,-2),V(.87f,2.07f,.26f),220,0,V(-.48f,0,.71f),V(.48f,0,.20f))
    };
    private static Vector3 V(float x,float y,float z)=>new Vector3(x,y,z);
    private static Pose P(float t,Vector3 pelvis,Vector3 hips,Vector3 waist,Vector3 chest,Vector3 grip,float cut,float weight,Vector3 left,Vector3 right)
        =>new Pose{time=t,pelvis=pelvis,hips=hips,waist=waist,chest=chest,grip=grip,cutAngle=cut,cutWeight=weight,leftFoot=left,rightFoot=right,
            leftFootAngles=V(0,-10,0),rightFootAngles=V(t>.3f&&t<.65f?22:0,18,0)};
    public static Quaternion ReadyFrame => Frame(new Vector3(.30f,-.50f,.81f),Vector3.down);
    public static Quaternion Frame(Vector3 blade,Vector3 edge)
    { blade.Normalize();return Quaternion.LookRotation(blade,Vector3.Cross(blade,edge).normalized); }
    public static Quaternion CutFrame(float degrees)
    {
        float a=degrees*Mathf.Deg2Rad;
        Vector3 high=new Vector3(.985f,.17f,0),front=Vector3.forward;
        return Frame(high*Mathf.Cos(a)+front*Mathf.Sin(a),-high*Mathf.Sin(a)+front*Mathf.Cos(a));
    }
    public Pose Evaluate(float time)
    {
        int i=0;while(i<slash.Length-2&&time>slash[i+1].time)i++;
        Pose a=slash[i],b=slash[i+1],p=slash[Mathf.Max(0,i-1)],n=slash[Mathf.Min(slash.Length-1,i+2)];
        float t=Mathf.Clamp01((time-a.time)/(b.time-a.time));
        return new Pose{time=time,pelvis=Mix(p.pelvis,a.pelvis,b.pelvis,n.pelvis,p.time,a.time,b.time,n.time,t),
            hips=Mix(p.hips,a.hips,b.hips,n.hips,p.time,a.time,b.time,n.time,t),waist=Mix(p.waist,a.waist,b.waist,n.waist,p.time,a.time,b.time,n.time,t),
            chest=Mix(p.chest,a.chest,b.chest,n.chest,p.time,a.time,b.time,n.time,t),grip=Mix(p.grip,a.grip,b.grip,n.grip,p.time,a.time,b.time,n.time,t),
            leftFoot=Mix(p.leftFoot,a.leftFoot,b.leftFoot,n.leftFoot,p.time,a.time,b.time,n.time,t),rightFoot=Mix(p.rightFoot,a.rightFoot,b.rightFoot,n.rightFoot,p.time,a.time,b.time,n.time,t),
            leftFootAngles=Vector3.Lerp(a.leftFootAngles,b.leftFootAngles,t),rightFootAngles=Vector3.Lerp(a.rightFootAngles,b.rightFootAngles,t),
            cutAngle=Mix(p.cutAngle,a.cutAngle,b.cutAngle,n.cutAngle,p.time,a.time,b.time,n.time,t),cutWeight=Mathf.Lerp(a.cutWeight,b.cutWeight,t)};
    }
    private static Vector3 Mix(Vector3 p,Vector3 a,Vector3 b,Vector3 n,float tp,float ta,float tb,float tn,float t)=>new Vector3(
        Mix(p.x,a.x,b.x,n.x,tp,ta,tb,tn,t),Mix(p.y,a.y,b.y,n.y,tp,ta,tb,tn,t),Mix(p.z,a.z,b.z,n.z,tp,ta,tb,tn,t));
    private static float Mix(float p,float a,float b,float n,float tp,float ta,float tb,float tn,float t)
    {
        float h=tb-ta,s=(b-a)/h,before=(a-p)/Mathf.Max(.001f,ta-tp),after=(n-b)/Mathf.Max(.001f,tn-tb);
        // Monotone cubic: preserve velocity through a cut; stop only at authored extrema.
        float m0=before*s>0?2*before*s/(before+s):0,m1=after*s>0?2*after*s/(after+s):0;
        return (2*t*t*t-3*t*t+1)*a+(t*t*t-2*t*t+t)*h*m0+(-2*t*t*t+3*t*t)*b+(t*t*t-t*t)*h*m1;
    }
}
