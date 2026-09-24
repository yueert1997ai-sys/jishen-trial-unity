using UnityEngine;
using UnityEngine.Rendering;

// Delayed presentation follows a committed death, never holds an enemy slot,
// awards another kill, applies blast damage, or owns a weapon/AI component.
public sealed class MechDeathVfx : MonoBehaviour
{
    public const int Capacity=8,ParticleLimit=672;
    public static MechDeathVfx Instance {get;private set;}
    public int Deaths {get;private set;}
    public int MainBursts {get;private set;}
    public int SecondaryBursts {get;private set;}
    public int ActiveSequences {get {int n=0;foreach(var e in events)if(e.active)n++;return n;}}
    public int ActiveParticles {get {int n=0;if(systems!=null)foreach(var ps in systems)n+=ps.particleCount;return n;}}
    sealed class Entry {public Damageable target;public Vector3 center,direction;public float age,scale;public bool active,main,secondary;public int generation;}
    readonly Entry[] events=new Entry[Capacity];int next;
    ParticleSystem fire,fireAlt,glow,ring,sparks,smoke;ParticleSystem[] systems;
    Material[] materials;uint random=0xdecaf123;int generation;
    public static void Play(Damageable target)
    {
        if(CombatLabSettings.MinimalFeedback)return;
        if(Instance==null)new GameObject("Mech reactor death effects").AddComponent<MechDeathVfx>();
        Instance.Begin(target);
    }
    void Awake()
    {
        Instance=this;generation=CombatRuntime.Generation;
        for(int i=0;i<Capacity;i++)events[i]=new Entry();
        materials=new Material[6];
        fire=Make(0,"Reactor fire","fire_01",96);fireAlt=Make(1,"Reactor secondary fire","fire_02",96);
        glow=Make(2,"Reactor ignition","light_01",48);ring=Make(3,"Reactor pressure ring","circle_03",32);
        sparks=Make(4,"Reactor hot fragments","light_01",320);smoke=Make(5,"Reactor cooling smoke","smoke_05",80,true);
        var sr=sparks.GetComponent<ParticleSystemRenderer>();sr.renderMode=ParticleSystemRenderMode.Stretch;sr.lengthScale=1.6f;sr.velocityScale=.11f;
        var sm=sparks.main;sm.gravityModifier=.85f;
        var size=ring.sizeOverLifetime;size.enabled=true;size.size=new ParticleSystem.MinMaxCurve(1,AnimationCurve.EaseInOut(0,.12f,1,1));
        size=smoke.sizeOverLifetime;size.enabled=true;size.size=new ParticleSystem.MinMaxCurve(1,AnimationCurve.Linear(0,.4f,1,1));
        systems=new[]{fire,fireAlt,glow,ring,sparks,smoke};
    }
    ParticleSystem Make(int index,string label,string texture,int limit,bool alpha=false)
    {
        var go=new GameObject(label);go.transform.SetParent(transform,false);var ps=go.AddComponent<ParticleSystem>();
        ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
        materials[index]=new Material(Shader.Find(alpha?"Sprites/Default":"MECH ROUGE/Particle Additive")){mainTexture=Resources.Load<Texture2D>("VFX/Sprites/"+texture)};
        var m=ps.main;m.loop=true;m.playOnAwake=false;m.maxParticles=limit;m.simulationSpace=ParticleSystemSimulationSpace.World;m.useUnscaledTime=false;
        ps.useAutoRandomSeed=false;ps.randomSeed=(uint)(index+813);
        var emission=ps.emission;emission.enabled=false;var shape=ps.shape;shape.enabled=false;
        var c=ps.colorOverLifetime;c.enabled=true;var g=new Gradient();
        g.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},new[]{new GradientAlphaKey(1,0),new GradientAlphaKey(.75f,.25f),new GradientAlphaKey(0,1)});c.color=g;
        var r=ps.GetComponent<ParticleSystemRenderer>();r.sharedMaterial=materials[index];r.shadowCastingMode=ShadowCastingMode.Off;r.receiveShadows=false;ps.Play();return ps;
    }
    float R(float a,float b){random^=random<<13;random^=random>>17;random^=random<<5;return Mathf.Lerp(a,b,(random&0xffffff)/16777216f);}
    Vector3 Scatter()=>new Vector3(R(-1,1),R(-.3f,1),R(-1,1)).normalized;
    void Emit(ParticleSystem ps,Vector3 p,Vector3 v,Color c,float size,float life)
    {ps.Emit(new ParticleSystem.EmitParams{position=p,velocity=v,startColor=c,startSize=size,startLifetime=life,rotation=R(0,360)},1);}
    static Vector3 Reactor(Damageable target)=>target.GetComponentInChildren<ImportedEnemyModel>()?.Chest.position??target.GetComponent<E01SoldierMotion>()?.ReactorCenter??target.AimCenter;
    void Begin(Damageable target)
    {
        if(generation!=CombatRuntime.Generation){Clear();generation=CombatRuntime.Generation;}
        var e=events[next++%Capacity];e.target=target;e.center=Reactor(target);e.age=0;e.active=true;e.main=e.secondary=false;e.generation=generation;
        e.scale=target.GetComponent<EnemyBase>()?.kind==EnemyKind.Elite?1.2f:1;
        var info=target.LastHit;e.direction=info!=null?Vector3.ProjectOnPlane(target.transform.position-info.SourcePosition,Vector3.up).normalized:target.transform.forward;
        if(info!=null&&info.MeleeStrike&&info.ContactTangent.sqrMagnitude>.01f)e.direction=info.ContactTangent.normalized;
        if(e.direction.sqrMagnitude<.1f)e.direction=Vector3.back;
        Deaths++;
        ArmorContactVfx.Get().Death(target,target.LastHit);
        Emit(glow,e.center,Vector3.zero,new Color(1,.52f,.16f),.9f,.09f);
        for(int i=0;i<10;i++)Emit(sparks,e.center,e.direction*R(1,3)+Scatter()*R(2,5),new Color(1,.65f,.23f),R(.04f,.08f),R(.15f,.3f));
    }
    void Detonate(Entry e,bool secondary)
    {
        if(secondary)SecondaryBursts++;else MainBursts++;
        Vector3 center=e.center+(secondary?new Vector3(.28f,-.24f,.1f):Vector3.zero);
        float scale=e.scale*(secondary?.65f:1);
        Emit(glow,center,Vector3.zero,new Color(1,.68f,.29f,.85f),3.0f*scale,.10f);
        Emit(ring,center,Vector3.zero,new Color(1,.43f,.08f,.6f),4.3f*scale,.32f);
        for(int i=0;i<(secondary?4:8);i++)
        {
            var dir=Scatter();
            Emit(i%2==0?fire:fireAlt,center+dir*.22f,dir*R(.6f,2.4f),new Color(1,R(.23f,.46f),.025f,.9f),R(1.3f,2.1f)*scale,R(.25f,.46f));
        }
        for(int i=0;i<(secondary?10:26);i++)Emit(sparks,center,Scatter()*R(3.5f,7)+e.direction*1.2f,new Color(1,R(.46f,.8f),.18f),R(.045f,.1f),R(.28f,.65f));
        for(int i=0;i<(secondary?2:5);i++)Emit(smoke,center+Scatter()*.3f,Scatter()*.4f+Vector3.up*R(.65f,1.1f),new Color(.15f,.14f,.13f,.3f),R(1.1f,1.8f)*scale,R(.85f,1.35f));
        if(!secondary)
        {
            GameAudio.PlayAt(GameAudioCue.Death,center,.5f,.83f);
            Camera.main?.GetComponent<CameraFollow>()?.AddShake(.11f,.13f);
        }
    }
    void Update()
    {
        var gm=GameManager.Instance;
        if(gm==null||gm.Phase==GamePhase.Hangar||generation!=CombatRuntime.Generation){Clear();generation=CombatRuntime.Generation;return;}
        if(gm.IsPaused)return;
        foreach(var e in events)
        {
            if(!e.active)continue;
            if(e.target!=null&&!e.target.IsDead){e.active=false;continue;}
            e.age+=Time.deltaTime;
            if(e.target!=null)e.center=Reactor(e.target);
            if(!e.main&&e.age>=.14f){e.main=true;Detonate(e,false);}
            if(!e.secondary&&e.age>=.34f){e.secondary=true;Detonate(e,true);}
            if(e.age>=.55f){e.active=false;e.target=null;}
        }
    }
    public void Clear(){foreach(var e in events)if(e!=null){e.active=false;e.target=null;}if(systems!=null)foreach(var ps in systems)ps.Clear();}
    void OnDestroy(){if(Instance==this)Instance=null;if(materials!=null)foreach(var m in materials)Destroy(m);}
}
