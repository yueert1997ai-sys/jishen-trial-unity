using UnityEngine;
using UnityEngine.Rendering;

// Presentation only. Six reusable discharge rigs and five bounded particle
// systems; the controller alone owns targeting, pulse cadence and damage.
public sealed class NemesisDroneVfx : MonoBehaviour
{
    public const int UnitCount=6, BeamLinesPerUnit=5, LinesPerUnit=7, ParticleLimit=456;
    public const float ChargeTime=.22f, PulseTime=.18f, TrailTime=.20f;
    static readonly Color Violet=new Color(.46f,.055f,1f), Hot=new Color(.86f,.64f,1f);
    sealed class Unit
    {
        public LineRenderer core,sheath,halo,helixA,helixB,compression,shock;
        public Transform corona; public MeshRenderer coronaRenderer;
        public float age=10,charge,emission;
        public Vector3 start,end;public bool contact;
    }
    Unit[] units;
    Material lineMaterial,radialMaterial,glowMaterial,starMaterial,sparkMaterial,smokeMaterial;
    Mesh quad;
    ParticleSystem motes,stars,sparks,clouds,smoke;
    ParticleSystem[] systems;
    readonly MaterialPropertyBlock tint=new MaterialPropertyBlock();
    uint random=0x6e656d31;
    bool cleared=true;
    public int Discharges {get;private set;}
    public int Impacts {get;private set;}
    public int VisibleBeams {get {int n=0;if(units!=null)foreach(var u in units)if(u.core.enabled)n++;return n;}}
    public int ChargingUnits {get {int n=0;if(units!=null)foreach(var u in units)if(u.charge>0)n++;return n;}}
    public int ActiveParticles {get {int n=0;if(systems!=null)foreach(var ps in systems)n+=ps.particleCount;return n;}}
    public int VisibleRenderers {get {int n=0;if(units!=null)foreach(var u in units)
        {if(u.core.enabled)n++;if(u.sheath.enabled)n++;if(u.halo.enabled)n++;if(u.helixA.enabled)n++;if(u.helixB.enabled)n++;if(u.compression.enabled)n++;if(u.shock.enabled)n++;if(u.coronaRenderer.enabled)n++;}return n;}}
    public float PulseAge(int i)=>units[i].age;
    public Vector3 BeamOrigin(int i)=>units[i].start;
    public Vector3 BeamEnd(int i)=>units[i].end;
    public bool IsBeamVisible(int i)=>units[i].core.enabled;

    public void Initialize()
    {
        if(units!=null)return;
        lineMaterial=new Material(Shader.Find("MECH ROUGE/Minovsky Glow")){name="NEMESIS violet pulse"};
        lineMaterial.SetFloat("_Pulses",7);lineMaterial.SetFloat("_Flow",4);lineMaterial.SetFloat("_PulseAmp",.38f);
        radialMaterial=new Material(lineMaterial){name="NEMESIS muzzle corona"};
        radialMaterial.SetFloat("_Radial",1);radialMaterial.SetFloat("_Pulses",0);
        glowMaterial=SpriteMaterial("light_01");starMaterial=SpriteMaterial("star_01");sparkMaterial=SpriteMaterial("spark_01");
        smokeMaterial=new Material(Shader.Find("Sprites/Default")){name="NEMESIS ion smoke",mainTexture=Resources.Load<Texture2D>("VFX/Sprites/smoke_05")};
        // One shared billboard mesh, no collider or duplicate primitive meshes.
        quad=new Mesh{name="NEMESIS corona quad"};
        quad.vertices=new[]{new Vector3(-.5f,-.5f,0),new Vector3(.5f,-.5f,0),new Vector3(.5f,.5f,0),new Vector3(-.5f,.5f,0)};
        quad.uv=new[]{Vector2.zero,Vector2.right,Vector2.one,Vector2.up};quad.triangles=new[]{0,1,2,0,2,3};quad.RecalculateBounds();
        units=new Unit[UnitCount];
        for(int i=0;i<UnitCount;i++)
        {
            var u=units[i]=new Unit();
            u.halo=Line("Drone beam halo "+i,2);u.sheath=Line("Drone beam sheath "+i,2);u.core=Line("Drone beam core "+i,2);
            u.helixA=Line("Drone beam filament A "+i,33);u.helixB=Line("Drone beam filament B "+i,33);
            u.compression=Line("Drone charge ring "+i,33);u.shock=Line("Drone impact ring "+i,33);
            var go=new GameObject("Drone muzzle corona "+i);go.transform.SetParent(transform,false);
            go.AddComponent<MeshFilter>().sharedMesh=quad;u.corona=go.transform;u.coronaRenderer=go.AddComponent<MeshRenderer>();
            u.coronaRenderer.sharedMaterial=radialMaterial;u.coronaRenderer.shadowCastingMode=ShadowCastingMode.Off;u.coronaRenderer.receiveShadows=false;u.coronaRenderer.enabled=false;
        }
        motes=Particles("Drone charge and ion motes",192,sparkMaterial);
        stars=Particles("Drone discharge stars",48,starMaterial);
        sparks=Particles("Drone contact streaks",144,glowMaterial);
        var sr=sparks.GetComponent<ParticleSystemRenderer>();sr.renderMode=ParticleSystemRenderMode.Stretch;sr.lengthScale=1.25f;sr.velocityScale=.09f;
        var sm=sparks.main;sm.gravityModifier=.45f;
        clouds=Particles("Drone violet impact corona",48,glowMaterial);
        smoke=Particles("Drone cooling vapor",24,smokeMaterial);
        var size=smoke.sizeOverLifetime;size.enabled=true;size.size=new ParticleSystem.MinMaxCurve(1,AnimationCurve.Linear(0,.35f,1,1));
        systems=new[]{motes,stars,sparks,clouds,smoke};
    }
    Material SpriteMaterial(string sprite)=>new Material(Shader.Find("MECH ROUGE/Particle Additive"))
        {name="NEMESIS "+sprite,mainTexture=Resources.Load<Texture2D>("VFX/Sprites/"+sprite)};
    LineRenderer Line(string label,int count)
    {
        var go=new GameObject(label);go.transform.SetParent(transform,false);var l=go.AddComponent<LineRenderer>();
        l.sharedMaterial=lineMaterial;l.useWorldSpace=true;l.positionCount=count;l.textureMode=LineTextureMode.Stretch;
        l.numCapVertices=2;l.shadowCastingMode=ShadowCastingMode.Off;l.receiveShadows=false;l.enabled=false;
        return l;
    }
    ParticleSystem Particles(string label,int limit,Material material)
    {
        var go=new GameObject(label);go.transform.SetParent(transform,false);var ps=go.AddComponent<ParticleSystem>();
        ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
        var main=ps.main;main.loop=true;main.playOnAwake=false;main.simulationSpace=ParticleSystemSimulationSpace.World;
        main.maxParticles=limit;main.useUnscaledTime=false;main.cullingMode=ParticleSystemCullingMode.AlwaysSimulate;
        main.scalingMode=ParticleSystemScalingMode.Shape;
        ps.useAutoRandomSeed=false;ps.randomSeed=(uint)(limit+173);
        var emission=ps.emission;emission.enabled=false;var shape=ps.shape;shape.enabled=false;
        var fade=ps.colorOverLifetime;fade.enabled=true;var gradient=new Gradient();
        gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},
            new[]{new GradientAlphaKey(1,0),new GradientAlphaKey(.75f,.35f),new GradientAlphaKey(0,1)});fade.color=gradient;
        var renderer=ps.GetComponent<ParticleSystemRenderer>();renderer.sharedMaterial=material;renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
        ps.Play();return ps;
    }
    // Local PRNG keeps presentation density from perturbing combat's random stream.
    float Random01(){random^=random<<13;random^=random>>17;random^=random<<5;return (random&0x00ffffff)/16777216f;}
    float Range(float a,float b)=>Mathf.Lerp(a,b,Random01());
    static void Basis(Vector3 axis,out Vector3 side,out Vector3 up)
    {side=Vector3.Cross(axis,Mathf.Abs(axis.y)>.95f?Vector3.forward:Vector3.up).normalized;up=Vector3.Cross(side,axis).normalized;}
    void Emit(ParticleSystem ps,Vector3 position,Vector3 velocity,Color color,float size,float life)
    {ps.Emit(new ParticleSystem.EmitParams{position=position,velocity=velocity,startColor=color,startSize=size,startLifetime=life,rotation=Range(0,360)},1);}

    public void Fire(int index,Vector3 origin,Vector3 end,bool contact)
    {
        if(CombatLabSettings.MinimalFeedback)return;
        cleared=false;Discharges++;if(contact)Impacts++;
        var u=units[index];u.age=0;u.start=origin;u.end=end;u.contact=contact;
        var direction=(end-origin).normalized;Basis(direction,out var side,out var up);
        Emit(stars,origin,Vector3.zero,Hot,1.12f,.16f);
        Emit(clouds,origin,Vector3.zero,Alpha(Violet,.7f),1.05f,.22f);
        // The pulse leaves drifting, sparse ion filaments in its wake.
        for(int n=0;n<12;n++)
        {
            float t=(n+Random01())/12,phase=t*24+index;
            var radial=(side*Mathf.Cos(phase)+up*Mathf.Sin(phase))*.15f;
            Emit(motes,Vector3.Lerp(origin,end,t)+radial,direction*Range(.7f,2)+radial*2,
                n%3==0?Hot:Violet,Range(.04f,.075f),Range(.22f,.42f));
        }
        if(!contact)return;
        Emit(stars,end,Vector3.zero,Hot,1.65f,.13f);
        Emit(stars,end,Vector3.zero,Alpha(Violet,.75f),1.9f,.23f);
        Emit(clouds,end,Vector3.zero,Alpha(Violet,.7f),1.8f,.28f);
        for(int n=0;n<10;n++)
        {
            float phase=Range(0,Mathf.PI*2);
            var radial=side*Mathf.Cos(phase)+up*Mathf.Sin(phase);
            var velocity=radial*Range(1.5f,4.5f)-direction*Range(.5f,2.8f);
            Emit(sparks,end,velocity,n%3==0?Hot:Violet,Range(.035f,.085f),Range(.22f,.4f));
        }
        Emit(smoke,end,-direction*.25f+Vector3.up*.4f,new Color(.19f,.13f,.25f,.24f),1.05f,.48f);
    }
    public void Tick(int index,Vector3 muzzle,Vector3 direction,float charge,float dt)
    {
        if(CombatLabSettings.MinimalFeedback){Clear();return;}
        var u=units[index];u.age+=dt;u.charge=charge;
        if(charge>0)cleared=false;
        float pulse=Mathf.Clamp01(1-u.age/PulseTime),trail=Mathf.Clamp01(1-u.age/(PulseTime+TrailTime));
        float energy=Mathf.Min(1,pulse*4);
        if(u.age<PulseTime)u.start=muzzle; // Detached afterglow stays in world space.
        float width=.75f+.25f*energy;
        Beam(u.halo,u.start,u.end,.92f*width,Alpha(Violet,.32f*trail));
        Beam(u.sheath,u.start,u.end,.29f*width,Alpha(Violet,.85f*energy));
        Beam(u.core,u.start,u.end,.09f*width,Alpha(Hot,energy));
        Filament(u.helixA,u,0,trail,Alpha(Violet,.70f*trail));
        Filament(u.helixB,u,Mathf.PI,trail,Alpha(Hot,.33f*trail));
        float muzzlePower=Mathf.Max(charge*.65f,energy);
        u.coronaRenderer.enabled=muzzlePower>.001f;
        if(u.coronaRenderer.enabled)
        {
            u.corona.position=muzzle;var cam=Camera.main;if(cam!=null)u.corona.rotation=cam.transform.rotation;
            float size=(energy>0?.9f:.48f)*(.55f+.45f*muzzlePower);var scale=transform.lossyScale;
            u.corona.localScale=new Vector3(size/scale.x,size/scale.y,size/scale.z);
            tint.SetColor("_Color",Alpha(Violet,muzzlePower*.85f));u.coronaRenderer.SetPropertyBlock(tint);
        }
        Ring(u.compression,muzzle+direction*.025f,direction,energy>0?.17f+u.age*1.2f:.38f-.24f*charge,
            .045f,Alpha(Violet,muzzlePower*.9f));
        Ring(u.shock,u.end,(u.end-u.start).normalized,.08f+u.age*2.6f,.035f,
            Alpha(Violet,u.contact?Mathf.Clamp01(1-u.age/.29f)*.6f:0));
        if(charge>0)
        {
            u.emission+=dt*36;
            Basis(direction,out var side,out var up);
            while(u.emission>=1)
            {
                u.emission--;float angle=Range(0,Mathf.PI*2);
                var offset=(side*Mathf.Cos(angle)+up*Mathf.Sin(angle))*Range(.18f,.4f);
                Emit(motes,muzzle+offset,-offset*6+direction*.08f,Random01()>.75f?Hot:Violet,.045f,.16f);
            }
        }
        else u.emission=0;
    }
    static Color Alpha(Color color,float alpha){color.a=alpha;return color;}
    static void Beam(LineRenderer line,Vector3 from,Vector3 end,float width,Color color)
    {
        line.enabled=color.a>.002f;if(!line.enabled)return;
        line.SetPosition(0,from);line.SetPosition(1,end);line.startWidth=width;line.endWidth=width*.78f;
        line.startColor=line.endColor=color;
    }
    static void Ring(LineRenderer line,Vector3 center,Vector3 axis,float radius,float width,Color color)
    {
        line.enabled=color.a>.002f;if(!line.enabled)return;
        Basis(axis,out var side,out var up);line.startWidth=line.endWidth=width;line.startColor=line.endColor=color;
        for(int j=0;j<33;j++){float a=j*(Mathf.PI*2/32);line.SetPosition(j,center+(side*Mathf.Cos(a)+up*Mathf.Sin(a))*radius);}
    }
    static void Filament(LineRenderer line,Unit u,float offset,float trail,Color color)
    {
        line.enabled=trail>.01f;if(!line.enabled)return;
        var delta=u.end-u.start;Basis(delta.normalized,out var side,out var up);
        line.startWidth=line.endWidth=.032f;line.startColor=line.endColor=color;
        float turns=Mathf.Min(4,delta.magnitude*.36f);
        for(int j=0;j<33;j++)
        {
            float t=j/32f,phase=t*turns*Mathf.PI*2-u.age*32+offset;
            float radius=(.12f+(1-trail)*.08f)*Mathf.Sin(t*Mathf.PI);
            line.SetPosition(j,u.start+delta*t+(side*Mathf.Cos(phase)+up*Mathf.Sin(phase))*radius);
        }
    }
    public void Clear()
    {
        if(units==null||cleared)return;cleared=true;
        foreach(var u in units)
        {
            u.age=10;u.charge=u.emission=0;u.contact=false;
            u.core.enabled=u.sheath.enabled=u.halo.enabled=u.helixA.enabled=u.helixB.enabled=u.compression.enabled=u.shock.enabled=u.coronaRenderer.enabled=false;
        }
        foreach(var ps in systems)ps.Clear(true);
    }
    void OnDisable(){Clear();}
    void OnDestroy()
    {
        if(quad!=null)Destroy(quad);
        foreach(var material in new[]{lineMaterial,radialMaterial,glowMaterial,starMaterial,sparkMaterial,smokeMaterial})if(material!=null)Destroy(material);
    }
}
