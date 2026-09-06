using System;
using UnityEngine;

[CreateAssetMenu(menuName="MECH TRIAL/Reverse grip three hit combo")]
public sealed class ValkyrComboProfile : ScriptableObject
{
    [Serializable] public struct Key
    {
        public float time,forwardGrip,open;
        public Vector3 wrist,elbow,blade,pelvis,hips,waist,chest,leftFoot,rightFoot;
        public ValkyrMotionProfile.Pose Body=>new ValkyrMotionProfile.Pose{pelvis=pelvis,hips=hips,waist=waist,chest=chest,leftFoot=leftFoot,rightFoot=rightFoot,leftFootAngles=new Vector3(0,-12,0),rightFootAngles=new Vector3(0,18,0)};
    }
    [Serializable] public sealed class Stroke
    {
        public string label;
        public float contactStart,contactEnd,linkTime,duration,advance=.34f,hitHold=.065f;
        public Key[] keys;
    }
    public Stroke[] strokes;
    public Stroke Get(int stage)=>strokes[Mathf.Clamp(stage,0,2)];
    public static ValkyrComboProfile LoadActive()
    {
        var profile=Resources.Load<ValkyrComboProfile>("ValkyrMotion/PowerComboV7");
        if(profile==null)profile=Resources.Load<ValkyrComboProfile>("ValkyrMotion/ReverseCombo");
        if(profile==null){profile=CreateInstance<ValkyrComboProfile>();profile.strokes=PowerDefaults();}
        return profile;
    }
    public static Key Ready=>K(0,V(.93f,1.94f,.24f),V(1.06f,2.35f,-.46f),V(.16f,-.51f,-.85f),0,V(0,-.08f,0),-6,-8);
    static Vector3 V(float x,float y,float z)=>new Vector3(x,y,z);
    static Key K(float t,Vector3 w,Vector3 e,Vector3 blade,float forward,Vector3 pelvis,float hip,float chest,float open=0)
    {
        return new Key{time=t,wrist=w,elbow=e,blade=blade.normalized,forwardGrip=forward,open=open,pelvis=pelvis,
            hips=V(3,hip,-hip*.12f),waist=V(5,Mathf.Lerp(hip,chest,.55f),-chest*.12f),chest=V(7,chest,-chest*.15f),
            leftFoot=V(-.48f,0,.28f),rightFoot=V(.48f,0,-.22f)};
    }
    public static Stroke[] Defaults()
    {
        Key a=K(.38f,V(-.15f,1.98f,.54f),V(.18f,2.53f,.27f),V(-.98f,-.18f,-.05f),0,V(-.20f,-.25f,.09f),-29,-51);
        Key b=K(.56f,V(1.10f,2.48f,.04f),V(1.14f,2.04f,-.4f),V(.97f,-.20f,-.16f),1,V(.15f,-.19f,.07f),31,49);
        Key end=Ready;end.time=.70f;
        var one=new Stroke{label="反手起斩",contactStart=.17f,contactEnd=.33f,linkTime=.38f,duration=.70f,advance=.34f,keys=new[]{
            Ready,K(.12f,V(1.12f,2.00f,-.12f),V(1.45f,2.50f,-.38f),V(.62f,-.27f,-.73f),0,V(.13f,-.14f,-.06f),19,29),
            K(.17f,V(1.13f,2.04f,.14f),V(1.6f,2.65f,0),V(.96f,-.08f,-.27f),0,V(.10f,-.18f,-.02f),10,26),
            K(.245f,V(.78f,2.10f,.58f),V(1.08f,2.73f,.27f),V(.26f,0,.97f),0,V(-.08f,-.26f,.07f),-18,-8),
            K(.315f,V(.23f,2.02f,.66f),V(.55f,2.66f,.36f),V(-.79f,-.12f,.61f),0,V(-.21f,-.29f,.11f),-32,-44),a,
            K(.52f,V(.34f,2.04f,.30f),V(.80f,2.48f,-.16f),V(-.56f,-.32f,-.76f),0,V(-.06f,-.14f,.03f),-17,-25),end}};
        Key start2=a;start2.time=0;end.time=.94f;
        var two=new Stroke{label="甩刀转握 / 正手回斩",contactStart=.30f,contactEnd=.49f,linkTime=.56f,duration=.94f,advance=.30f,keys=new[]{
            start2,K(.10f,V(-.36f,2.13f,.93f),V(.08f,2.43f,.40f),V(-.80f,.40f,.45f),0,V(-.13f,-.20f,.05f),-26,-48,.38f),
            K(.20f,V(-.28f,2.53f,.95f),V(.05f,2.10f,.32f),V(-.55f,.74f,-.38f),.55f,V(-.06f,-.14f,.02f),-16,-32,1),
            K(.28f,V(.01f,2.66f,.80f),V(.25f,2.10f,.28f),V(-.96f,.07f,.28f),1,V(-.12f,-.19f,.02f),-9,-30),
            K(.39f,V(.43f,2.66f,.85f),V(.58f,2.18f,.35f),V(-.10f,-.10f,.99f),1,V(.03f,-.24f,.1f),19,8),
            K(.46f,V(.96f,2.53f,.42f),V(.95f,2.05f,-.05f),V(.91f,-.18f,.37f),1,V(.16f,-.24f,.10f),36,43),b,
            K(.70f,V(1.18f,2.14f,.18f),V(1.20f,2.55f,-.30f),V(.40f,-.55f,-.73f),.55f,V(.08f,-.15f,.03f),12,18,.85f),end}};
        Key start3=b;start3.time=0;end.time=1.00f;
        var heavy=K(.49f,V(.66f,1.98f,.74f),V(.05f,2.20f,.44f),V(.02f,-.55f,.83f),1,V(-.05f,-.38f,.16f),-8,-12);
        heavy.chest.x=27;heavy.waist.x=19;heavy.hips.x=12;
        var three=new Stroke{label="正面重劈",contactStart=.32f,contactEnd=.51f,linkTime=1,duration=1,advance=.48f,hitHold=.085f,keys=new[]{
            start3,K(.16f,V(.91f,3.11f,.17f),V(.30f,2.85f,-.10f),V(.02f,.96f,.29f),1,V(.09f,-.10f,-.07f),16,23),
            K(.25f,V(.80f,3.17f,.38f),V(.14f,2.99f,.13f),V(0,.92f,.40f),1,V(.04f,-.15f,-.08f),4,11),
            K(.32f,V(.74f,3.08f,.60f),V(.06f,3.12f,.21f),V(0,.67f,.74f),1,V(0,-.22f,0),-6,0),
            K(.395f,V(.70f,2.60f,.88f),V(.06f,2.77f,.32f),V(.03f,-.10f,.99f),1,V(-.04f,-.32f,.12f),-12,-11),heavy,
            K(.63f,V(.87f,2.00f,.64f),V(.82f,2.54f,.22f),V(.36f,-.49f,.79f),1,V(-.02f,-.30f,.10f),-10,-15),
            K(.80f,V(1.15f,2.08f,.28f),V(1.20f,2.55f,-.28f),V(.64f,-.32f,-.70f),.48f,V(.04f,-.16f,.02f),3,5,.85f),end}};
        // Alternating support legs; the contact foot lands before the blade crosses the target.
        for(int s=0;s<3;s++)for(int i=0;i<(s==0?one:s==1?two:three).keys.Length;i++)
        {
            var keys=(s==0?one:s==1?two:three).keys;var k=keys[i];float step=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.04f,(s==1?.36f:.25f),k.time));
            if(s==1)k.rightFoot=V(.52f,Mathf.Sin(step*Mathf.PI)*.13f,-.22f+step*.62f);
            else k.leftFoot=V(-.52f,Mathf.Sin(step*Mathf.PI)*.12f,.28f+step*.44f);
            keys[i]=k;
        }
        return new[]{one,two,three};
    }
    public static Stroke[] PowerDefaults()
    {
        var end=Ready;end.time=.70f;
        var a=K(.38f,V(-.55f,1.98f,.81f),V(-.12f,2.70f,.22f),V(-.95f,-.15f,-.26f),0,V(-.34f,-.39f,.16f),-48,-78);
        var b=K(.56f,V(1.02f,2.70f,-.12f),V(1.13f,2.09f,-.40f),V(.94f,-.25f,-.25f),1,V(.31f,-.35f,.14f),47,76);
        var one=new Stroke{label="反手大横斩",contactStart=.17f,contactEnd=.33f,linkTime=.38f,duration=.70f,advance=.68f,hitHold=.075f,keys=new[]{
            Ready,
            K(.12f,V(1.43f,2.05f,-.45f),V(1.72f,2.65f,-.62f),V(.42f,-.15f,-.90f),0,V(.24f,-.22f,-.12f),31,49),
            K(.17f,V(1.48f,2.16f,.02f),V(1.8f,2.82f,-.14f),V(.96f,-.04f,-.28f),0,V(.15f,-.29f,-.02f),21,43),
            K(.245f,V(.72f,2.30f,1.07f),V(1.13f,2.99f,.63f),V(.08f,-.06f,.99f),0,V(-.15f,-.40f,.15f),-29,-20),
            K(.315f,V(-.38f,2.10f,.96f),V(.07f,2.81f,.54f),V(-.96f,-.09f,.26f),0,V(-.35f,-.44f,.20f),-49,-72),a,
            K(.52f,V(.09f,1.99f,.35f),V(.59f,2.54f,-.08f),V(-.67f,-.30f,-.68f),0,V(-.13f,-.22f,.06f),-25,-39),end}};
        var start2=a;start2.time=0;end.time=.94f;
        var two=new Stroke{label="侧甩换握 / 大幅回斩",contactStart=.30f,contactEnd=.49f,linkTime=.56f,duration=.94f,advance=.60f,hitHold=.075f,keys=new[]{
            start2,
            K(.10f,V(-.50f,2.29f,1.07f),V(-.02f,2.62f,.38f),V(-.83f,.45f,.32f),0,V(-.29f,-.34f,.08f),-43,-72,.38f),
            K(.20f,V(-.28f,2.81f,1.12f),V(.10f,2.18f,.46f),V(-.61f,.73f,-.30f),.55f,V(-.18f,-.25f,-.03f),-32,-53,1),
            K(.28f,V(-.28f,2.75f,1.02f),V(.08f,2.11f,.35f),V(-.97f,.05f,.18f),1,V(-.24f,-.32f,.02f),-27,-61),
            K(.39f,V(.66f,2.83f,.68f),V(.59f,2.14f,.58f),V(-.05f,-.15f,.99f),1,V(.10f,-.41f,.17f),27,12),
            K(.46f,V(1.10f,2.75f,.18f),V(.99f,2.05f,-.06f),V(.95f,-.22f,.22f),1,V(.33f,-.43f,.19f),49,69),b,
            K(.72f,V(1.27f,2.13f,.02f),V(1.30f,2.68f,-.51f),V(.43f,-.53f,-.73f),.55f,V(.13f,-.23f,.05f),23,35,.85f),end}};
        var start3=b;start3.time=0;end.time=1;
        var loaded=K(.24f,V(.66f,3.58f,.01f),V(.08f,3.13f,-.42f),V(.06f,.83f,-.55f),1,V(.10f,-.13f,-.12f),17,23);
        loaded.hips.x=-7;loaded.waist.x=-14;loaded.chest.x=-18;
        var heavy=K(.49f,V(.56f,1.63f,1.04f),V(.15f,2.34f,.55f),V(.03f,-.56f,.83f),1,V(-.14f,-.55f,.25f),-17,-24);
        heavy.hips.x=18;heavy.waist.x=29;heavy.chest.x=42;
        var through=K(.62f,V(.91f,1.64f,.82f),V(.66f,2.34f,.24f),V(.38f,-.59f,.71f),1,V(-.10f,-.48f,.19f),-15,-25);
        through.hips.x=14;through.waist.x=25;through.chest.x=34;
        var three=new Stroke{label="踏进正面重劈",contactStart=.32f,contactEnd=.51f,linkTime=1,duration=1,advance=.88f,hitHold=.095f,keys=new[]{
            start3,
            K(.14f,V(1.10f,3.38f,-.10f),V(.48f,3.05f,-.44f),V(.21f,.93f,-.28f),1,V(.18f,-.14f,-.12f),29,41),loaded,
            K(.32f,V(.65f,3.37f,.65f),V(.05f,3.24f,.10f),V(.01f,.66f,.75f),1,V(.01f,-.28f,.04f),-10,-3),
            K(.395f,V(.61f,2.55f,1.21f),V(.02f,2.89f,.56f),V(.02f,-.18f,.98f),1,V(-.13f,-.49f,.21f),-21,-22),heavy,through,
            K(.80f,V(1.30f,2.04f,.20f),V(1.34f,2.63f,-.40f),V(.69f,-.32f,-.65f),.48f,V(.03f,-.25f,.08f),9,13,.85f),end}};
        var result=new[]{one,two,three};
        for(int s=0;s<result.Length;s++)for(int i=0;i<result[s].keys.Length;i++)
        {
            var k=result[s].keys[i];float land=s==1?.35f:.27f;
            float step=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.035f,land,k.time));
            float recover=Mathf.SmoothStep(0,1,Mathf.InverseLerp(s==0?.46f:s==1?.65f:.67f,result[s].duration,k.time));
            float reach=(s==2?1.04f:.83f)*step*(1-recover);
            float lift=Mathf.Sin(step*Mathf.PI)*.24f;
            k.leftFoot=V(-.60f,0,.24f);k.rightFoot=V(.60f,0,-.24f);
            if(s==1)k.rightFoot+=V(.03f,lift,reach);else k.leftFoot+=V(-.03f,lift,reach);
            if(s==2)k.rightFoot.z+=step*.20f*(1-recover);
            k.leftFoot=Vector3.Lerp(k.leftFoot,Ready.leftFoot,recover);
            k.rightFoot=Vector3.Lerp(k.rightFoot,Ready.rightFoot,recover);
            result[s].keys[i]=k;
        }
        return result;
    }
    public Key Evaluate(int stage,float time)
    {
        var keys=Get(stage).keys;int i=0;while(i<keys.Length-2&&time>keys[i+1].time)i++;
        var a=keys[i];var b=keys[i+1];var p=keys[Mathf.Max(0,i-1)];var n=keys[Mathf.Min(keys.Length-1,i+2)];
        float t=Mathf.Clamp01((time-a.time)/(b.time-a.time));
        Vector3 M(Vector3 pv,Vector3 av,Vector3 bv,Vector3 nv)=>new Vector3(C(pv.x,av.x,bv.x,nv.x,p.time,a.time,b.time,n.time,t),C(pv.y,av.y,bv.y,nv.y,p.time,a.time,b.time,n.time,t),C(pv.z,av.z,bv.z,nv.z,p.time,a.time,b.time,n.time,t));
        return new Key{time=time,wrist=M(p.wrist,a.wrist,b.wrist,n.wrist),elbow=M(p.elbow,a.elbow,b.elbow,n.elbow),blade=Vector3.Slerp(a.blade,b.blade,Mathf.SmoothStep(0,1,t)).normalized,
            forwardGrip=Mathf.Lerp(a.forwardGrip,b.forwardGrip,Mathf.SmoothStep(0,1,t)),open=Mathf.Lerp(a.open,b.open,t),
            pelvis=M(p.pelvis,a.pelvis,b.pelvis,n.pelvis),hips=M(p.hips,a.hips,b.hips,n.hips),waist=M(p.waist,a.waist,b.waist,n.waist),chest=M(p.chest,a.chest,b.chest,n.chest),
            leftFoot=M(p.leftFoot,a.leftFoot,b.leftFoot,n.leftFoot),rightFoot=M(p.rightFoot,a.rightFoot,b.rightFoot,n.rightFoot)};
    }
    public static Key Blend(Key a,Key b,float t)
    {
        var k=b;k.wrist=Vector3.Lerp(a.wrist,b.wrist,t);k.elbow=Vector3.Lerp(a.elbow,b.elbow,t);k.blade=Vector3.Slerp(a.blade,b.blade,t);
        k.forwardGrip=Mathf.Lerp(a.forwardGrip,b.forwardGrip,t);k.open=Mathf.Lerp(a.open,b.open,t);k.pelvis=Vector3.Lerp(a.pelvis,b.pelvis,t);
        k.hips=Vector3.Lerp(a.hips,b.hips,t);k.waist=Vector3.Lerp(a.waist,b.waist,t);k.chest=Vector3.Lerp(a.chest,b.chest,t);return k;
    }
    static float C(float p,float a,float b,float n,float tp,float ta,float tb,float tn,float t)
    {
        float h=tb-ta,s=(b-a)/h,before=(a-p)/Mathf.Max(.001f,ta-tp),after=(n-b)/Mathf.Max(.001f,tn-tb);
        float m0=before*s>0?2*before*s/(before+s):0,m1=after*s>0?2*after*s/(after+s):0;
        return (2*t*t*t-3*t*t+1)*a+(t*t*t-2*t*t+t)*h*m0+(-2*t*t*t+3*t*t)*b+(t*t*t-t*t)*h*m1;
    }
}
