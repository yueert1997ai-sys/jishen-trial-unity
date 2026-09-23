using UnityEngine;
using UnityEngine.Rendering;

// Pooled, world-space beam packets. Flight, collision and weapon cadence stay in Projectile.
[DefaultExecutionOrder(1150)]
public sealed class EnergyBoltVisual : MonoBehaviour
{
    static Material material;
    LineRenderer halo, sheath, core, spiral;
    Projectile shot;
    Color tint;
    float age, length, width, particleClock;
    public bool Active { get; private set; }
    public int LayerCount => 4;
    public static readonly Color EnemyRed=new Color(1f,.045f,.08f);
    public Color Tint => tint;
    public void Configure(Projectile projectile, bool active)
    {
        shot=projectile;Active=active;age=particleClock=0;
        if(active && core==null) Build();
        if(halo!=null)foreach(var line in new[]{halo,sheath,core,spiral})line.enabled=active;
        GetComponent<MeshRenderer>().enabled=!active;
        var trail=GetComponent<TrailRenderer>();if(trail!=null){trail.Clear();trail.enabled=!active;}
        if(!active)return;
        tint=shot.team==1?EnemyRed:shot.Kinetic?new Color(.10f,1f,.48f):shot.VisualBlock.GetColor("_Color");
        bool missile=shot is MissileProjectile;
        length=missile?2.6f:shot.Kinetic?1.5f:2.2f;
        width=missile?.20f:shot.Kinetic?.095f:.15f;
        if(shot.team==1){length=1.7f;width=.12f;}
        RenderPacket();
    }
    void Build()
    {
        if(material==null)
        {
            material=new Material(Shader.Find("MECH ROUGE/Minovsky Glow")){name="Energy packet shared additive"};
            material.SetFloat("_Pulses",3);material.SetFloat("_Flow",4);material.SetFloat("_PulseAmp",.45f);
        }
        halo=Line("Energy hue-shift halo",2);sheath=Line("Energy color sheath",2);
        core=Line("Energy white core",2);spiral=Line("Energy spiral filament",17);
    }
    LineRenderer Line(string label,int count)
    {
        var go=new GameObject(label);go.transform.SetParent(transform,false);
        var line=go.AddComponent<LineRenderer>();line.sharedMaterial=material;line.useWorldSpace=true;
        line.positionCount=count;line.alignment=LineAlignment.View;line.textureMode=LineTextureMode.Stretch;
        line.numCapVertices=3;line.shadowCastingMode=ShadowCastingMode.Off;line.receiveShadows=false;
        return line;
    }
    void LateUpdate()
    {
        if(!Active || shot==null || Time.deltaTime<=0)return;
        age+=Time.deltaTime;RenderPacket();
        particleClock+=Time.deltaTime;
        if(particleClock>=.035f)
        {
            particleClock%=.035f;
            BeamFxKit.BeamStream(transform.position-shot.direction*Mathf.Min(length,shot.speed*age),
                transform.position,Color.white,tint,Color.white,width*1.1f,1,.3f);
        }
    }
    void RenderPacket()
    {
        Vector3 heading=shot.direction.normalized;
        Vector3 tip=transform.position,tail=tip-heading*Mathf.Min(length,.06f+shot.speed*age);
        Color outer=shot.team==1?new Color(1f,.015f,.09f):Color.Lerp(tint,new Color(.36f,.12f,1f),.28f);outer.a=.28f;
        Segment(halo,tail,tip,width*4,outer);
        Segment(sheath,tail,tip,width*1.8f,new Color(tint.r,tint.g,tint.b,.88f));
        Segment(core,tail,tip,width*.55f,new Color(1,1,1,.98f));
        var side=Vector3.Cross(heading,Vector3.up).normalized;
        if(side.sqrMagnitude<.01f)side=Vector3.right;
        var up=Vector3.Cross(side,heading);
        spiral.startWidth=spiral.endWidth=width*.20f;
        spiral.startColor=spiral.endColor=Color.Lerp(tint,Color.white,.65f);
        for(int i=0;i<17;i++)
        {
            float t=i/16f,phase=t*Mathf.PI*4-age*32;
            spiral.SetPosition(i,Vector3.Lerp(tail,tip,t)+(side*Mathf.Cos(phase)+up*Mathf.Sin(phase))*width*Mathf.Sin(t*Mathf.PI));
        }
    }
    static void Segment(LineRenderer line,Vector3 tail,Vector3 tip,float width,Color color)
    {
        line.startWidth=width*.45f;line.endWidth=width;
        line.startColor=line.endColor=color;line.SetPosition(0,tail);line.SetPosition(1,tip);
    }
    void OnDisable(){Active=false;}
}
