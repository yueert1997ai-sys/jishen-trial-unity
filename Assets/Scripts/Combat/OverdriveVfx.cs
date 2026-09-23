using UnityEngine;
using UnityEngine.Rendering;

// Shared, bounded emitters. No objects/materials are allocated by individual combat hits.
public sealed class OverdriveVfx : MonoBehaviour
{
    static OverdriveVfx instance;
    ParticleSystem streaks, embers, halos, shockwaves;
    Material glow, ringMaterial;
    Texture2D softTexture, ringTexture;
    readonly Light[] lights = new Light[3];
    readonly float[] lightLife = new float[3];
    readonly float[] lightStrength = new float[3];
    readonly LineRenderer[] arcs = new LineRenderer[24];
    readonly float[] arcLife = new float[24];
    int nextArc;
    int nextLight, budgetFrame = -1, frameBudget;
    public static int LiveParticles => instance == null ? 0 : instance.streaks.particleCount + instance.embers.particleCount + instance.halos.particleCount + instance.shockwaves.particleCount;
    public const int ParticleCapacity = 3904;
    public static int BreakBursts { get; private set; }
    public static int DeathBursts { get; private set; }
    public static int HitBursts { get; private set; }
    public static int DashBursts { get; private set; }
    public static bool Intense { get; private set; } = true;
    public static void ToggleIntensity() { Intense = !Intense; }
    public static Material CreateJetMaterial() => new Material(Get().glow);
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { instance = null; BreakBursts = DeathBursts = HitBursts = DashBursts = 0; Intense = true; }
    static OverdriveVfx Get()
    {
        if (instance == null) new GameObject("Overdrive Combat Particles").AddComponent<OverdriveVfx>();
        return instance;
    }
    void Awake()
    {
        instance = this;
        softTexture = Texture(false); ringTexture = Texture(true);
        glow = new Material(Resources.Load<Shader>("MechActionGlow")) { mainTexture = softTexture };
        ringMaterial = new Material(glow) { mainTexture = ringTexture };
        streaks = Create("Hot armor streaks", 2400, glow, true, .55f);
        embers = Create("Ion embers", 1000, glow, false, .06f);
        halos = Create("Impact glow", 440, glow, false, 0);
        shockwaves = Create("Pressure rings", 64, ringMaterial, false, 0);
        for (int i = 0; i < lights.Length; i++)
        {
            lights[i] = new GameObject("Combat light " + i).AddComponent<Light>();
            lights[i].transform.SetParent(transform, false);
            lights[i].type = LightType.Point; lights[i].shadows = LightShadows.None; lights[i].enabled = false;
        }
        for(int i=0;i<arcs.Length;i++)
        {
            var arc=new GameObject("Armor discharge "+i).AddComponent<LineRenderer>();
            arc.transform.SetParent(transform,false);arc.sharedMaterial=glow;arc.useWorldSpace=true;
            arc.positionCount=7;arc.startWidth=arc.endWidth=.055f;
            arc.shadowCastingMode=ShadowCastingMode.Off;arc.receiveShadows=false;arc.enabled=false;arcs[i]=arc;
        }
    }
    Texture2D Texture(bool ring)
    {
        int side=ring?256:64;float center=(side-1)*.5f;
        var t = new Texture2D(side,side,TextureFormat.RGBA32,false) { wrapMode = TextureWrapMode.Clamp };
        var data = new Color[side*side];
        for (int y=0;y<side;y++) for (int x=0;x<side;x++)
        {
            float r = new Vector2((x-center)/center,(y-center)/center).magnitude;
            float a = ring ? Mathf.Pow(Mathf.Clamp01(1-Mathf.Abs(r-.73f)/.045f),2) : Mathf.Pow(Mathf.Clamp01(1-r),3.4f);
            data[y*side+x] = new Color(1,1,1,a);
        }
        t.SetPixels(data);t.Apply(false,true); return t;
    }
    ParticleSystem Create(string label,int capacity,Material mat,bool stretch,float gravity)
    {
        var go = new GameObject(label);go.transform.SetParent(transform,false);
        var ps = go.AddComponent<ParticleSystem>();ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main;main.loop=true;main.playOnAwake=false;main.maxParticles=capacity;
        main.simulationSpace=ParticleSystemSimulationSpace.World;main.gravityModifier=gravity;
        var emission=ps.emission;emission.enabled=false;var shape=ps.shape;shape.enabled=false;
        var colors=ps.colorOverLifetime;colors.enabled=true;
        var gradient=new Gradient();gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},
            new[]{new GradientAlphaKey(1,0),new GradientAlphaKey(.85f,.15f),new GradientAlphaKey(0,1)});colors.color=gradient;
        var size=ps.sizeOverLifetime;size.enabled=true;
        size.size=new ParticleSystem.MinMaxCurve(1,AnimationCurve.Linear(0,stretch?1:.45f,1,stretch?.05f:1.65f));
        var renderer=ps.GetComponent<ParticleSystemRenderer>();renderer.sharedMaterial=mat;
        renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
        renderer.renderMode=stretch?ParticleSystemRenderMode.Stretch:ParticleSystemRenderMode.Billboard;
        renderer.velocityScale=.08f;renderer.lengthScale=2.3f;
        ps.Play();return ps;
    }
    bool Spend()
    {
        if (budgetFrame != Time.frameCount) { budgetFrame = Time.frameCount; frameBudget = Intense && GamePreferences.Quality > 0 ? 650 : 300; }
        return frameBudget-- > 0;
    }
    void Emit(ParticleSystem ps,Vector3 p,Vector3 velocity,Color color,float size,float life)
    {
        if(!Spend())return;
        ps.Emit(new ParticleSystem.EmitParams { position=p,velocity=velocity,startColor=color,startSize=size,startLifetime=life,rotation=Random.Range(0,360) },1);
    }
    int Count(int n) => Intense && GamePreferences.Quality > 0 ? n : Mathf.Max(1,n/2);
    void LightPulse(Vector3 p,Color color,float strength,float range)
    {
        int i=nextLight++%lights.Length;lights[i].transform.position=p;
        lights[i].color=color;lights[i].range=range*.65f;lightStrength[i]=strength*.45f;lightLife[i]=.12f;
        lights[i].intensity=lightStrength[i];lights[i].enabled=true;
    }
    void Burst(Vector3 p,Vector3 axis,Color color,int count,float speed,float scale)
    {
        for(int i=0;i<Count(count);i++)
        {
            var v=Random.onUnitSphere;v.y=Mathf.Abs(v.y)*.65f;
            v=(v+axis*.65f)*Random.Range(speed*.35f,speed);
            Emit(streaks,p+Random.insideUnitSphere*.09f,v,Color.Lerp(color,Color.white,Random.value*.45f),Random.Range(.07f,.17f)*scale,Random.Range(.20f,.65f));
        }
        var accent=color;accent.a=.38f;
        Emit(halos,p,Vector3.zero,accent,.85f*scale,.075f);
        Emit(halos,p,Vector3.zero,new Color(1,1,1,.6f),.30f*scale,.045f);
    }
    public static void Hit(Vector3 p,Vector3 direction,bool heavy,bool direct)
    {
        if(CombatLabSettings.MinimalFeedback)return;
        var fx=Get();HitBursts++;
        var color=direct?new Color(.5f,.95f,1):heavy?new Color(.08f,.55f,1):new Color(1,.58f,.12f);
        fx.Burst(p,direction,color,heavy?100:direct?64:28,heavy?14:8,heavy?1.25f:.65f);
        fx.LightPulse(p,color,heavy?3:1.2f,heavy?6:3);
    }
    public static void Break(Vector3 p)
    {
        if(CombatLabSettings.MinimalFeedback)return;
        var fx=Get();BreakBursts++;
        ArmorContactVfx.Get().Fracture(p);
        for(int i=0;i<3;i++)Discharge(p);
        fx.LightPulse(p,new Color(.3f,.85f,1),3,4);
        Camera.main?.GetComponent<CameraFollow>()?.AddShake(.12f,.10f);
    }
    public static void Execute(Vector3 p,Vector3 direction)
    {
        if(CombatLabSettings.MinimalFeedback)return;
        var fx=Get();direction=Vector3.ProjectOnPlane(direction,Vector3.up).normalized;
        Vector3 across=Vector3.Cross(direction,Vector3.up);
        fx.Burst(p,across*2,new Color(1,.72f,.22f),22,9,1.1f);
        fx.Burst(p,-across*2,new Color(.45f,.9f,1),14,7,.8f);
        fx.LightPulse(p,new Color(1,.7f,.3f),3,4);
    }
    public static void Death(Vector3 p)
    {
        if(CombatLabSettings.MinimalFeedback)return;
        DeathBursts++;
        // The actor's real armor pieces, dust and fall are emitted by ArmorContactVfx.Death.
    }
    public static void Dash(Vector3 p,Vector3 direction,bool amethyst=false)
    {
        var fx=Get();DashBursts++;
        fx.Burst(p+Vector3.up*.6f,-direction,amethyst?NemesisMotionRig.Amethyst:new Color(.04f,.6f,1),56,9,.8f);
        fx.LightPulse(p+Vector3.up,amethyst?NemesisMotionRig.Amethyst:new Color(.03f,.5f,1),2.5f,5);
    }
    public static void Exhaust(Vector3 p,Vector3 direction,float strength,bool amethyst=false)
    {
        var fx=Get();
        fx.Emit(fx.halos,p,direction*Random.Range(2,5),amethyst?new Color(.30f,.035f,.63f,.26f):new Color(.025f,.38f,1,.26f),strength*.42f,.08f);
        fx.Emit(fx.streaks,p,direction*Random.Range(5,10)+Random.insideUnitSphere*.5f,amethyst?NemesisMotionRig.Amethyst:new Color(.12f,.8f,1),strength*.12f,.32f);
    }
    public static void Discharge(Vector3 p)
    {
        if(CombatLabSettings.MinimalFeedback)return;
        var fx=Get();int slot=fx.nextArc++%fx.arcs.Length;var arc=fx.arcs[slot];
        Vector3 start=p+Random.onUnitSphere*.6f, end=p+Random.onUnitSphere*1.35f;
        for(int i=0;i<7;i++)arc.SetPosition(i,Vector3.Lerp(start,end,i/6f)+(i==0||i==6?Vector3.zero:Random.insideUnitSphere*.2f));
        arc.startColor=arc.endColor=new Color(.3f,.85f,1);arc.enabled=true;fx.arcLife[slot]=.12f;
    }
    public static void Clear()
    {
        if(instance==null)return;
        instance.streaks.Clear();instance.embers.Clear();instance.halos.Clear();instance.shockwaves.Clear();
        for(int i=0;i<instance.lights.Length;i++){instance.lights[i].enabled=false;instance.lightLife[i]=0;}
        for(int i=0;i<instance.arcs.Length;i++){instance.arcs[i].enabled=false;instance.arcLife[i]=0;}
    }
    void Update()
    {
        if(GameManager.Instance!=null && !GameManager.Instance.IsCombatActive)
        {if(!GameManager.Instance.IsPaused)Clear();return;}
        for(int i=0;i<lights.Length;i++)
        {
            lightLife[i]=Mathf.Max(0,lightLife[i]-Time.deltaTime);
            lights[i].intensity=lightStrength[i]*lightLife[i]/.12f;
            lights[i].enabled=lightLife[i]>0;
        }
        for(int i=0;i<arcs.Length;i++)
        {
            arcLife[i]=Mathf.Max(0,arcLife[i]-Time.deltaTime);arcs[i].enabled=arcLife[i]>0;
            arcs[i].startColor=arcs[i].endColor=new Color(.3f,.85f,1,arcLife[i]/.12f);
        }
    }
    void OnDestroy()
    {
        if(instance==this)instance=null;
        Destroy(glow);Destroy(ringMaterial);Destroy(softTexture);Destroy(ringTexture);
    }
}
