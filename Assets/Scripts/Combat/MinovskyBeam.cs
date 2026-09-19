using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// TYPE-08: a short charge, one continuous particle beam and a fading ion trail.
// Damage resolves once at discharge; lingering light cannot deal damage every frame.
[DefaultExecutionOrder(1100)]
public sealed class MinovskyBeam : MonoBehaviour
{
    public const float ChargeDuration = .12f;
    public const float BeamDuration = .25f;
    public const float ResidualDuration = .22f;
    public const float MaximumRange = 42f;
    public static readonly Color Pink = new Color(1f, .055f, .40f);
    public static readonly Color Core = new Color(1f, .83f, .96f);
    public float Damage { get; private set; }
    public int Penetration { get; private set; }
    public float BlastRadius { get; private set; }
    public bool HasFired { get; private set; }
    public float Age { get; private set; }
    public Vector3 Origin { get; private set; }
    public Vector3 End { get; private set; }
    public int DirectHitCount { get; private set; }
    static Material lineMaterial, radialMaterial;
    Transform muzzle;
    Damageable owner;
    int team;
    Vector3 aimPoint, fallbackDirection;
    LineRenderer core, sheath, glow, muzzleRing, impactRing;
    Transform muzzleCorona, impactCorona;
    Light muzzleLight, impactLight;
    ParticleSystem motes;
    readonly HashSet<Damageable> struck = new HashSet<Damageable>();
    readonly HashSet<Damageable> directTargets = new HashSet<Damageable>();
    readonly RaycastHit[] hits = new RaycastHit[128];
    readonly List<Vector3> impactPoints = new List<Vector3>();
    MaterialPropertyBlock tint;
    static readonly IComparer<RaycastHit> NearFirst = Comparer<RaycastHit>.Create((a,b)=>a.distance.CompareTo(b.distance));

    public static MinovskyBeam Fire(Transform socket, Vector3 origin, Vector3 direction, Vector3 aim,
        int sourceTeam, Damageable source, float damage, int pierce, float blast)
    {
        var beam = new GameObject("TYPE08_Minovsky_PinkBeam").AddComponent<MinovskyBeam>();
        beam.muzzle=socket; beam.Origin=origin; beam.aimPoint=aim;
        beam.fallbackDirection=direction.normalized; beam.team=sourceTeam; beam.owner=source;
        beam.Damage=damage; beam.Penetration=pierce; beam.BlastRadius=blast;
        beam.BuildVisuals(); beam.Display();
        return beam;
    }
    void BuildVisuals()
    {
        tint=new MaterialPropertyBlock();
        if (lineMaterial==null)
        {
            var shader=Shader.Find("MECH ROUGE/Minovsky Glow");
            if (shader==null) throw new InvalidOperationException("Missing Minovsky Glow shader");
            lineMaterial=new Material(shader) { name="Minovsky beam additive glow" };
            radialMaterial=new Material(shader) { name="Minovsky radial glow" };
            radialMaterial.SetFloat("_Radial",1);
        }
        glow=Line("Pink atmospheric halo",2);
        sheath=Line("Saturated pink particle sheath",2);
        core=Line("White-pink beam core",2);
        muzzleRing=Line("Muzzle particle compression ring",49);
        impactRing=Line("Impact ion ring",49);
        muzzleCorona=Corona("Muzzle pink corona");
        impactCorona=Corona("Impact pink corona");
        muzzleLight=Light("Muzzle armor illumination",4.5f);
        impactLight=Light("Impact illumination",4f);
        var particleObject=new GameObject("Minovsky particle motes");particleObject.transform.SetParent(transform,false);
        motes=particleObject.AddComponent<ParticleSystem>();
        motes.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
        var main=motes.main;main.loop=false;main.playOnAwake=false;main.maxParticles=96;
        main.simulationSpace=ParticleSystemSimulationSpace.World;main.gravityModifier=0;
        var emission=motes.emission;emission.enabled=false;
        var shape=motes.shape;shape.enabled=false;
        var color=motes.colorOverLifetime;color.enabled=true;
        var gradient=new Gradient();gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Pink,1)},
            new[]{new GradientAlphaKey(.9f,0),new GradientAlphaKey(0,1)});color.color=gradient;
        var size=motes.sizeOverLifetime;size.enabled=true;size.size=new ParticleSystem.MinMaxCurve(1,AnimationCurve.Linear(0,1,1,0));
        var renderer=motes.GetComponent<ParticleSystemRenderer>();renderer.sharedMaterial=radialMaterial;
        renderer.renderMode=ParticleSystemRenderMode.Billboard;renderer.shadowCastingMode=ShadowCastingMode.Off;
        motes.Play();
    }
    LineRenderer Line(string name,int count)
    {
        var go=new GameObject(name);go.transform.SetParent(transform,false);
        var line=go.AddComponent<LineRenderer>();line.sharedMaterial=lineMaterial;line.useWorldSpace=true;
        line.positionCount=count;line.alignment=LineAlignment.View;line.textureMode=LineTextureMode.Stretch;
        line.numCapVertices=4;line.numCornerVertices=2;line.shadowCastingMode=ShadowCastingMode.Off;line.receiveShadows=false;
        return line;
    }
    Transform Corona(string name)
    {
        var go=GameObject.CreatePrimitive(PrimitiveType.Quad);go.name=name;go.transform.SetParent(transform,false);
        var collider=go.GetComponent<Collider>();collider.enabled=false;Destroy(collider);
        var renderer=go.GetComponent<MeshRenderer>();renderer.sharedMaterial=radialMaterial;
        renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
        return go.transform;
    }
    Light Light(string name,float range)
    {
        var go=new GameObject(name);go.transform.SetParent(transform,false);
        var light=go.AddComponent<Light>();light.type=LightType.Point;light.color=Pink;light.range=range;
        light.shadows=LightShadows.None;return light;
    }
    void LateUpdate()
    {
        var gm=GameManager.Instance;
        if (muzzle==null || (owner!=null && owner.IsDead) || (gm!=null && gm.Phase!=GamePhase.Combat))
        { Destroy(gameObject);return; }
        if (gm!=null && gm.IsPaused) return;
        Age+=Time.deltaTime;
        Origin=muzzle.position;
        if (!HasFired)
        {
            Vector3 offset=UnityEngine.Random.onUnitSphere*UnityEngine.Random.Range(.22f,.46f);
            Emit(Origin+offset,-offset*9,.12f,.055f,Core);
            if (Age>=ChargeDuration) Discharge();
        }
        Display();
        if (Age>=ChargeDuration+BeamDuration+ResidualDuration) Destroy(gameObject);
    }
    void Discharge()
    {
        HasFired=true;
        Vector3 delta=aimPoint-Origin;
        Vector3 direction=delta.sqrMagnitude>.01f?delta.normalized:fallbackDirection;
        End=Origin+direction*MaximumRange;
        int count=Physics.SphereCastNonAlloc(Origin,.13f,direction,hits,MaximumRange,~0,QueryTriggerInteraction.Ignore);
        Array.Sort(hits,0,count,NearFirst);
        int remaining=Penetration+1;
        // Collect the direct path first, so splash never consumes penetration or reduces a later direct hit.
        for (int i=0;i<count;i++)
        {
            var target=hits[i].collider.GetComponentInParent<Damageable>();
            if (target!=null && (target.team==team || target.IsDead || target==owner || directTargets.Contains(target))) continue;
            Vector3 point=Origin+direction*hits[i].distance;
            if (target==null) { End=point;impactPoints.Add(point);break; }
            directTargets.Add(target);impactPoints.Add(point);
            if (--remaining==0) { End=point;break; }
        }
        foreach (var target in directTargets)
        {
            struck.Add(target);
            target.TakeDamage(Damage,new DamageInfo(gameObject,target.AimCenter,owner,Damage));
        }
        DirectHitCount=directTargets.Count;
        foreach (var point in impactPoints)
        {
            if (BlastRadius>.05f)
                for (int i=Damageable.Active.Count-1;i>=0;i--)
                {
                    var target=Damageable.Active[i];
                    if (target==null || target.team==team || target.IsDead || struck.Contains(target)) continue;
                    float distance=Vector3.Distance(point,target.AimCenter);
                    if (distance>BlastRadius) continue;
                    // Solid cover also shields targets from particle splash.
                    Vector3 ray=target.AimCenter-point;
                    if (ray.sqrMagnitude>.01f && Physics.Raycast(point+ray.normalized*.035f,ray.normalized,out var cover,ray.magnitude-.035f,~0,QueryTriggerInteraction.Ignore)
                        && cover.collider.GetComponentInParent<Damageable>()==null) continue;
                    struck.Add(target);
                    float amount=Damage*(.45f+Mathf.Clamp01(1-distance/BlastRadius)*.55f);
                    target.TakeDamage(amount,new DamageInfo(gameObject,point,owner,amount));
                }
            for (int i=0;i<14;i++) Emit(point,UnityEngine.Random.onUnitSphere*UnityEngine.Random.Range(1.8f,5),.30f,.07f,Core);
        }
        for (int i=0;i<28;i++)
            Emit(Vector3.Lerp(Origin,End,UnityEngine.Random.value)+UnityEngine.Random.insideUnitSphere*.14f,
                direction*UnityEngine.Random.Range(1,3)+UnityEngine.Random.onUnitSphere*.35f,.35f,UnityEngine.Random.Range(.025f,.07f),Pink);
        GameAudio.Play(GameAudioCue.Beam,.58f,.80f);
        if (Camera.main!=null) Camera.main.GetComponent<CameraFollow>()?.AddShake(.13f,.14f);
    }
    void Emit(Vector3 position,Vector3 velocity,float life,float size,Color color)
    {
        motes.Emit(new ParticleSystem.EmitParams { position=position,velocity=velocity,startLifetime=life,startSize=size,startColor=color },1);
    }
    void Display()
    {
        float firedAge=Age-ChargeDuration;
        float charge=Mathf.Clamp01(Age/ChargeDuration);
        float fade=HasFired?Mathf.Clamp01(1-firedAge/BeamDuration):0;
        float envelope=fade>0?Mathf.Min(1,fade*4):0;
        float flicker=.96f+.04f*Mathf.Sin(Age*130);
        float glowFade=HasFired?Mathf.Clamp01(1-firedAge/(BeamDuration+ResidualDuration)):0;
        Beam(glow,1.15f, new Color(.82f,.014f,.21f,.33f*glowFade));
        Beam(sheath,.48f*flicker, new Color(1,.055f,.4f,.95f*envelope));
        Beam(core,.16f*flicker, new Color(1,.83f,.96f,envelope));
        float corona=HasFired?glowFade:charge;
        Billboard(muzzleCorona,Origin,(HasFired?1.04f:.56f)*(.55f+.45f*corona),new Color(1,.08f,.45f,corona*.9f));
        Billboard(impactCorona,End,1.8f,new Color(1,.06f,.36f,impactPoints.Count>0?glowFade*.8f:0));
        Ring(muzzleRing,Origin,HasFired?(End-Origin).normalized:fallbackDirection,
            HasFired?.22f+Mathf.Max(0,firedAge)*2.1f:.37f*(1-charge)+.10f,.025f,new Color(1,.16f,.58f,corona*.8f));
        Ring(impactRing,End,HasFired?(End-Origin).normalized:fallbackDirection,
            .18f+Mathf.Max(0,firedAge)*4.5f,.035f,new Color(1,.08f,.40f,impactPoints.Count>0?glowFade*.65f:0));
        muzzleLight.transform.position=Origin;muzzleLight.intensity=corona*(HasFired?4.5f:1.8f);
        impactLight.transform.position=End;impactLight.intensity=impactPoints.Count>0?glowFade*3:0;
    }
    void Beam(LineRenderer line,float width,Color color)
    {
        line.enabled=HasFired && color.a>.001f;
        line.startWidth=line.endWidth=width;line.startColor=line.endColor=color;
        line.SetPosition(0,Origin);line.SetPosition(1,End);
    }
    void Billboard(Transform quad,Vector3 position,float size,Color color)
    {
        quad.position=position;quad.localScale=Vector3.one*size;
        if (Camera.main!=null) quad.rotation=Camera.main.transform.rotation;
        tint.SetColor("_Color",color);quad.GetComponent<Renderer>().SetPropertyBlock(tint);
    }
    void Ring(LineRenderer line,Vector3 center,Vector3 axis,float radius,float width,Color color)
    {
        if (axis.sqrMagnitude<.01f) axis=Vector3.forward;
        var rotation=Quaternion.LookRotation(axis,Vector3.up);
        line.startWidth=line.endWidth=width;line.startColor=line.endColor=color;
        for (int i=0;i<49;i++)
        {
            float angle=i*Mathf.PI*2/48;
            line.SetPosition(i,center+rotation*new Vector3(Mathf.Cos(angle)*radius,Mathf.Sin(angle)*radius,0));
        }
    }
}
