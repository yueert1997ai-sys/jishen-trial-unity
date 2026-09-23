using UnityEngine;
using UnityEngine.Rendering;

// One bounded presentation owner for slice contacts. All input comes from resolved physical hits.
[DefaultExecutionOrder(205)]
public sealed class ArmorContactVfx : MonoBehaviour
{
    public const int MarkCapacity=48,PieceCapacity=24;
    public static ArmorContactVfx Instance { get; private set; }
    public int BulletContacts {get;private set;}
    public int BladeContacts {get;private set;}
    public int WallContacts {get;private set;}
    public int Fractures {get;private set;}
    public int DetachedPieces {get;private set;}
    public Vector3 LastPoint {get;private set;}
    public Vector3 LastTangent {get;private set;}
    public int ActiveMarks {get;private set;}
    public int ActivePieces {get;private set;}
    ParticleSystem sparks,chips,dust;ParticleSystem[] systems;
    Material glow,steel,haze;Texture2D dot;Mesh shard;
    readonly ParticleSystem.Particle[] ground=new ParticleSystem.Particle[256];
    readonly Mark[] marks=new Mark[MarkCapacity];readonly Piece[] pieces=new Piece[PieceCapacity];
    int nextMark,nextPiece;
    class Mark {public LineRenderer line;public Transform armor;public Vector3 a,b;public float age,life,width;public bool cut;}
    class Piece {public Transform root;public MeshRenderer renderer;public MeshFilter filter;public Vector3 velocity,spin,scale;public float age;public bool active;}
    public static ArmorContactVfx Get()
    {if(Instance==null)new GameObject("Armor contact presentation").AddComponent<ArmorContactVfx>();return Instance;}
    void Awake()
    {
        Instance=this;dot=new Texture2D(32,32,TextureFormat.RGBA32,false);
        for(int y=0;y<32;y++)for(int x=0;x<32;x++)
        {float r=new Vector2((x-15.5f)/15.5f,(y-15.5f)/15.5f).magnitude;dot.SetPixel(x,y,new Color(1,1,1,Mathf.Pow(Mathf.Clamp01(1-r),3)));}
        dot.Apply(false,true);glow=new Material(Resources.Load<Shader>("MechActionGlow")){mainTexture=dot};
        steel=new Material(Shader.Find("Standard")){color=new Color(.27f,.30f,.32f)};steel.SetFloat("_Metallic",.75f);steel.SetFloat("_Glossiness",.6f);
        haze=new Material(Shader.Find("Sprites/Default")){mainTexture=dot};
        shard=new Mesh{name="AngularArmorChip",vertices=new[]{new Vector3(-.5f,-.3f,0),new Vector3(.5f,-.18f,0),new Vector3(.22f,.48f,.12f),new Vector3(.05f,.03f,-.20f)},triangles=new[]{0,1,2,0,3,1,1,3,2,2,3,0}};shard.RecalculateNormals();shard.RecalculateBounds();
        sparks=Make("Contact hot metal",768,glow,ParticleSystemRenderMode.Stretch,.45f);
        chips=Make("Contact armor chips",256,steel,ParticleSystemRenderMode.Mesh,1);
        var cr=chips.GetComponent<ParticleSystemRenderer>();cr.mesh=shard;cr.shadowCastingMode=ShadowCastingMode.On;
        var rotation=chips.rotationOverLifetime;rotation.enabled=true;rotation.separateAxes=true;rotation.x=3.5f;rotation.y=5.2f;rotation.z=2.8f;
        dust=Make("Contact dust",128,haze,ParticleSystemRenderMode.Billboard,-.08f);systems=new[]{sparks,chips,dust};
        for(int i=0;i<marks.Length;i++)
        {
            var line=new GameObject("Armor scar "+i).AddComponent<LineRenderer>();line.transform.SetParent(transform,false);line.sharedMaterial=glow;
            line.positionCount=2;line.numCapVertices=0;line.shadowCastingMode=ShadowCastingMode.Off;line.receiveShadows=false;line.enabled=false;
            marks[i]=new Mark{line=line};
        }
        for(int i=0;i<pieces.Length;i++)
        {
            var obj=new GameObject("Released armor "+i,typeof(MeshFilter),typeof(MeshRenderer));obj.transform.SetParent(transform,false);obj.SetActive(false);
            pieces[i]=new Piece{root=obj.transform,filter=obj.GetComponent<MeshFilter>(),renderer=obj.GetComponent<MeshRenderer>()};
        }
    }
    ParticleSystem Make(string name,int limit,Material material,ParticleSystemRenderMode mode,float gravity)
    {
        var obj=new GameObject(name);obj.transform.SetParent(transform,false);var ps=obj.AddComponent<ParticleSystem>();ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
        var main=ps.main;main.loop=true;main.playOnAwake=false;main.maxParticles=limit;main.simulationSpace=ParticleSystemSimulationSpace.World;main.gravityModifier=gravity;
        var e=ps.emission;e.enabled=false;var shape=ps.shape;shape.enabled=false;
        var color=ps.colorOverLifetime;color.enabled=true;var g=new Gradient();g.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(new Color(.65f,.45f,.26f),1)},new[]{new GradientAlphaKey(1,0),new GradientAlphaKey(1,.15f),new GradientAlphaKey(0,1)});color.color=g;
        var r=ps.GetComponent<ParticleSystemRenderer>();r.sharedMaterial=material;r.renderMode=mode;r.lengthScale=1.25f;r.velocityScale=.025f;r.shadowCastingMode=ShadowCastingMode.Off;
        ps.Play();return ps;
    }
    void Emit(ParticleSystem ps,Vector3 p,Vector3 v,Color c,float size,float life)
    {ps.Emit(new ParticleSystem.EmitParams{position=p,velocity=v,startColor=c,startSize=size,startLifetime=life,rotation=Random.Range(0,360)},1);}
    public void Contact(Damageable target,DamageInfo info)
    {
        if(CombatLabSettings.MinimalFeedback)return;
        if(!info.HasContact)return;
        LastPoint=info.ContactPoint;LastTangent=info.ContactTangent;
        bool cut=info.MeleeStrike;if(cut)BladeContacts++;else BulletContacts++;
        Vector3 n=info.ContactNormal.normalized,t=info.ContactTangent.normalized;
        if(t.sqrMagnitude<.1f)t=Vector3.Cross(Vector3.up,n);
        int count=info.BrokeArmor?14:cut?(info.HeavyImpact?44:30):13;
        for(int i=0;i<count;i++)
        {
            Vector3 velocity=cut?t*Random.Range(4,10)+n*Random.Range(1,3)+Random.insideUnitSphere*1.3f:n*Random.Range(2,6)+Random.insideUnitSphere*1.8f;
            Emit(sparks,LastPoint,velocity,new Color(1,Random.Range(.54f,.9f),.26f),Random.Range(.055f,cut?.13f:.085f),Random.Range(.10f,cut?.32f:.20f));
        }
        for(int i=0;i<(cut?7:3);i++)Emit(chips,LastPoint,n*Random.Range(1,3)+t*Random.Range(-1,cut?5:2)+Vector3.up*Random.Range(1,3),Color.white,Random.Range(.04f,cut?.15f:.085f),Random.Range(.45f,1));
        Emit(dust,LastPoint,n*.45f+Vector3.up*.25f,new Color(.29f,.27f,.24f,.16f),cut?.30f:.16f,cut?.25f:.16f);
        Scar(target,info,t,n);
    }
    void Scar(Damageable target,DamageInfo info,Vector3 tangent,Vector3 normal)
    {
        Renderer nearest=null;float distance=float.MaxValue;
        var actor=target.GetComponent<E01SoldierMotion>();if(actor==null)return;
        foreach(var r in actor.ArmorRenderers)
        {
            float d=(r.bounds.ClosestPoint(info.ContactPoint)-info.ContactPoint).sqrMagnitude;
            if(r.enabled&&d<distance){nearest=r;distance=d;}
        }
        if(nearest==null)return;
        var m=marks[nextMark++%marks.Length];m.armor=nearest.transform;m.age=0;m.life=info.MeleeStrike?.55f:.22f;m.cut=info.MeleeStrike;m.width=m.cut?.09f:.07f;
        Vector3 point=nearest.bounds.ClosestPoint(info.ContactPoint)+normal*.025f;
        float half=m.cut?(info.HeavyImpact?.44f:.28f):.027f;
        m.a=m.armor.InverseTransformPoint(point-tangent*half);m.b=m.armor.InverseTransformPoint(point+tangent*half);m.line.enabled=true;
    }
    public void Wall(Vector3 point,Vector3 normal)
    {
        if(CombatLabSettings.MinimalFeedback)return;
        WallContacts++;LastPoint=point;
        for(int i=0;i<6;i++)Emit(sparks,point,normal*Random.Range(1,4)+Random.insideUnitSphere*1.4f,new Color(1,.63f,.25f),.027f,Random.Range(.05f,.12f));
        Emit(dust,point,normal*.4f,new Color(.38f,.36f,.32f,.18f),.2f,.25f);
    }
    public void Fracture(Vector3 p)
    {
        if(CombatLabSettings.MinimalFeedback)return;
        Fractures++;
        for(int i=0;i<22;i++)
        {Vector3 direction=Random.onUnitSphere;direction.y=Mathf.Abs(direction.y)*.5f;Emit(chips,p+direction*.30f,direction*Random.Range(3,6),new Color(.8f,.9f,1),Random.Range(.08f,.20f),Random.Range(.35f,.70f));Emit(sparks,p,direction*Random.Range(3,7),new Color(.35f,.85f,1),.065f,.18f);}
    }
    public void Death(Damageable target,DamageInfo info)
    {
        if(CombatLabSettings.MinimalFeedback)return;
        var actor=target.GetComponent<E01SoldierMotion>();if(actor==null)return;
        Vector3 force=info!=null&&info.MeleeStrike?info.ContactTangent:target.transform.forward;
        if(force.sqrMagnitude<.01f)force=target.transform.right;force.Normalize();
        string side=Vector3.Dot(force,target.transform.right)>0?"R":"L";
        foreach(var filter in actor.visual.GetComponentsInChildren<MeshFilter>())
        {
            if(filter.name!="E01_L_SHOULDER"&&filter.name!="E01_R_SHOULDER"&&filter.name!="E01_BACKPACK"
                &&filter.name!="E01_HEAD"&&filter.name!="E01_"+side+"_FOREARM")continue;
            var renderer=filter.GetComponent<MeshRenderer>();if(renderer==null||!renderer.enabled)continue;
            var piece=pieces[nextPiece++%pieces.Length];piece.filter.sharedMesh=filter.sharedMesh;piece.renderer.sharedMaterials=renderer.sharedMaterials;
            piece.root.SetPositionAndRotation(filter.transform.position,filter.transform.rotation);piece.scale=filter.transform.lossyScale;piece.root.localScale=piece.scale;
            var outward=(filter.transform.position-target.AimCenter).normalized;
            piece.velocity=force*Random.Range(1.8f,3.5f)+outward*Random.Range(1.5f,3)+Vector3.up*Random.Range(2.5f,4.5f);piece.spin=Random.onUnitSphere*Random.Range(180,340);piece.age=0;piece.active=true;piece.root.gameObject.SetActive(true);
            renderer.enabled=false;DetachedPieces++;
        }
        for(int i=0;i<10;i++)Emit(chips,target.AimCenter,force*Random.Range(2,5)+Random.onUnitSphere*2,Color.white,Random.Range(.08f,.20f),.9f);
        Emit(dust,target.AimCenter,Vector3.up*.5f,new Color(.23f,.25f,.27f,.20f),.65f,.42f);
    }
    void Update()
    {
        var gm=GameManager.Instance;if(gm==null)return;
        foreach(var ps in systems){if(gm.IsPaused&&!ps.isPaused)ps.Pause();else if(!gm.IsPaused&&ps.isPaused)ps.Play();}
        if(gm.IsPaused)return;
        // The last kill's detached armor can finish its bounded fall behind the
        // result/reward UI. Hangar and explicit run reset still clear immediately.
        if(gm.Phase==GamePhase.Hangar||gm.Phase==GamePhase.Loadout){Clear();return;}
        float dt=Time.deltaTime;ActiveMarks=ActivePieces=0;
        foreach(var m in marks)
        {
            if(!m.line.enabled)continue;m.age+=dt;
            if(m.armor==null||m.age>m.life){m.line.enabled=false;continue;}
            float f=1-m.age/m.life;m.line.SetPosition(0,m.armor.TransformPoint(m.a));m.line.SetPosition(1,m.armor.TransformPoint(m.b));
            m.line.startWidth=m.line.endWidth=m.width;
            m.line.startColor=m.line.endColor=Color.Lerp(new Color(.8f,.28f,.06f,0),new Color(1,.83f,.44f,.9f),f);ActiveMarks++;
        }
        foreach(var p in pieces)
        {
            if(!p.active)continue;p.age+=dt;
            if(p.age>1.8f){p.active=false;p.root.gameObject.SetActive(false);continue;}
            p.velocity+=Vector3.down*9.81f*dt;p.root.position+=p.velocity*dt;
            if(p.root.position.y<.12f){var position=p.root.position;position.y=.12f;p.root.position=position;p.velocity=new Vector3(p.velocity.x*.65f,Mathf.Abs(p.velocity.y)*.20f,p.velocity.z*.65f);p.spin*=.6f;if(p.velocity.y<.4f){p.velocity=Vector3.zero;p.spin=Vector3.zero;}}
            p.root.Rotate(p.spin*dt,Space.World);p.root.localScale=p.scale*Mathf.Clamp01((1.8f-p.age)/.2f);ActivePieces++;
        }
        int count=chips.GetParticles(ground);bool changed=false;
        for(int i=0;i<count;i++)if(ground[i].position.y<.04f){var p=ground[i];var pos=p.position;pos.y=.04f;p.position=pos;p.velocity=Vector3.zero;ground[i]=p;changed=true;}
        if(changed)chips.SetParticles(ground,count);
    }
    public void Clear(){sparks.Clear();chips.Clear();dust.Clear();foreach(var m in marks)m.line.enabled=false;foreach(var p in pieces){p.active=false;p.root.gameObject.SetActive(false);}ActiveMarks=ActivePieces=0;}
    void OnDestroy(){if(Instance==this)Instance=null;Destroy(glow);Destroy(steel);Destroy(haze);Destroy(dot);Destroy(shard);}
}
