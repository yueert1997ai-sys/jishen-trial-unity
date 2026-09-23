using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// AX-01: a short charge, one continuous particle beam and a fading ion trail.
// Damage resolves once at discharge; lingering light cannot deal damage every frame.
[DefaultExecutionOrder(1100)]
public sealed class HalbreakerBeam : MonoBehaviour
{
    public const float ChargeDuration = .18f;
    public const float BeamDuration = .34f;
    public const float ResidualDuration = .30f;
    public const float MaximumRange = 55f;
    public static readonly Color Blue = new Color(.025f, .32f, 1f);
    public static readonly Color Core = new Color(.70f, .94f, 1f);
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
    float visibleLength;
    Damageable owner;
    int team;
    Vector3 aimPoint, fallbackDirection;
    LineRenderer core, sheath, glow, hotLine, muzzleRing, impactRing;
    LineRenderer[] filaments = new LineRenderer[3], shockRings = new LineRenderer[4];
    readonly List<Transform> hitCoronas = new List<Transform>();
    Transform muzzleCorona, impactCorona;
    Light muzzleLight, impactLight;
    ParticleSystem motes;
    readonly HashSet<Damageable> struck = new HashSet<Damageable>();
    readonly HashSet<Damageable> directTargets = new HashSet<Damageable>();
    
    readonly List<Vector3> impactPoints = new List<Vector3>();
    MaterialPropertyBlock tint;
    static readonly IComparer<RaycastHit> NearFirst = Comparer<RaycastHit>.Create((a,b)=>a.distance.CompareTo(b.distance));

    public static HalbreakerBeam Fire(Transform socket, Vector3 origin, Vector3 direction, Vector3 aim,
        int sourceTeam, Damageable source, float damage, int pierce, float blast)
    {
        var beam = new GameObject("HALBREAKER_Blue_PiercingBeam").AddComponent<HalbreakerBeam>();
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
            // Dense fast lobes: the AX-01 reads as a compressed high-density
            // particle stream, not a loose glow.
            lineMaterial.SetFloat("_Pulses",8);lineMaterial.SetFloat("_Flow",3.4f);lineMaterial.SetFloat("_PulseAmp",.7f);
            radialMaterial=new Material(shader) { name="Minovsky radial glow" };
            radialMaterial.SetFloat("_Radial",1);
        }
        glow=Line("Indigo atmospheric halo",2);
        sheath=Line("Cyan particle sheath",2);
        core=Line("Ice-white beam core",2);
        hotLine=Line("Overexposed white filament",2);
        muzzleRing=Line("Muzzle particle compression ring",49);
        impactRing=Line("Impact ion ring",49);
        muzzleCorona=Corona("Muzzle blue corona");
        impactCorona=Corona("Impact blue corona");
        muzzleLight=Light("Muzzle armor illumination",4.5f);
        impactLight=Light("Impact illumination",4f);
        for(int i=0;i<filaments.Length;i++) filaments[i]=Line("Blue ion spiral "+i,49);
        for(int i=0;i<shockRings.Length;i++) shockRings[i]=Line("Traveling compression wave "+i,49);
        var particleObject=new GameObject("Minovsky particle motes");particleObject.transform.SetParent(transform,false);
        motes=particleObject.AddComponent<ParticleSystem>();
        motes.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
        var main=motes.main;main.loop=false;main.playOnAwake=false;main.maxParticles=640;
        main.simulationSpace=ParticleSystemSimulationSpace.World;main.gravityModifier=0;
        var emission=motes.emission;emission.enabled=false;
        var shape=motes.shape;shape.enabled=false;
        var color=motes.colorOverLifetime;color.enabled=true;
        var gradient=new Gradient();gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Blue,1)},
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
        var light=go.AddComponent<Light>();light.type=LightType.Point;light.color=Blue;light.range=range;
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
            BeamFxKit.ChargeConverge(Origin,fallbackDirection,Core,Blue,3,.55f);
            if (Age>=ChargeDuration) Discharge();
        }
        // A connected beam follows the bore through turn, recoil and translation.
        // Damage remains a single discharge, never a sweeping damage tick.
        if(HasFired) End=Origin+muzzle.forward*visibleLength;
        Display();
        if (Age>=ChargeDuration+BeamDuration+ResidualDuration) Destroy(gameObject);
    }
    void Discharge()
    {
        HasFired=true;
        Vector3 delta=aimPoint-Origin;
        Vector3 direction=muzzle!=null?muzzle.forward:PlanarCombat.Direction(delta,fallbackDirection);
        // Match projectile collision height; the chest/hand socket is presentation only.
        Vector3 collisionOrigin=PlanarCombat.Point(Origin);
        End=Origin+direction*MaximumRange;
        var hits=Physics.SphereCastAll(collisionOrigin,.20f,direction,MaximumRange,~0,QueryTriggerInteraction.Ignore);
        int count=hits.Length;
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
        visibleLength=Vector3.Distance(Origin,End);
        foreach (var point in impactPoints)
        {
            hitCoronas.Add(Corona("Penetration flare "+hitCoronas.Count));
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
            for (int i=0;i<36;i++) Emit(point,UnityEngine.Random.onUnitSphere*UnityEngine.Random.Range(1.8f,5),.30f,.07f,Core);
            BeamFxKit.ImpactBurst(point,direction,Blue,.85f+BlastRadius*.15f);
        }
        for (int i=0;i<125;i++)
            Emit(Vector3.Lerp(Origin,End,UnityEngine.Random.value)+UnityEngine.Random.insideUnitSphere*.14f,
                direction*UnityEngine.Random.Range(1,3)+UnityEngine.Random.onUnitSphere*.35f,.35f,UnityEngine.Random.Range(.025f,.07f),Blue);
        BeamFxKit.MuzzleBlast(Origin,direction,Blue,1.45f);
        BeamFxKit.StarGlare(Origin,Core,3.2f,.2f);
        BeamFxKit.Twinkles(Origin,End,Blue,30);
        GameAudio.Play(GameAudioCue.Beam,.58f,.80f);
        if (Camera.main!=null) Camera.main.GetComponent<CameraFollow>()?.AddShake(.20f,.22f);
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
        // Razor-edged density: a pure-white filament inside an ice core, tight
        // cyan sheath and a restrained indigo halo. Crisp edges read as
        // compression; only the pink beam gets the loose anime bloom.
        Beam(glow,2.6f, new Color(.28f,.10f,.95f,.38f*glowFade));
        Beam(sheath,1.05f*flicker, new Color(.08f,.52f,1f,.95f*envelope));
        Beam(core,.30f*flicker, new Color(.80f,.97f,1f,envelope));
        Beam(hotLine,.10f, new Color(1f,1f,1f,.95f*envelope));
        float corona=HasFired?glowFade:charge;
        Billboard(muzzleCorona,Origin,(HasFired?1.85f:.80f)*(.55f+.45f*corona),new Color(.03f,.4f,1f,corona*.9f));
        Billboard(impactCorona,End,2.8f,new Color(.03f,.35f,1f,impactPoints.Count>0?glowFade*.8f:0));
        Ring(muzzleRing,Origin,HasFired?(End-Origin).normalized:fallbackDirection,
            HasFired?.22f+Mathf.Max(0,firedAge)*2.1f:.37f*(1-charge)+.10f,.025f,new Color(.12f,.65f,1f,corona*.8f));
        Ring(impactRing,End,HasFired?(End-Origin).normalized:fallbackDirection,
            .18f+Mathf.Max(0,firedAge)*4.5f,.035f,new Color(.04f,.45f,1f,impactPoints.Count>0?glowFade*.65f:0));
        muzzleLight.transform.position=Origin;muzzleLight.intensity=corona*(HasFired?6.5f:1.8f);
        impactLight.transform.position=End;impactLight.intensity=impactPoints.Count>0?glowFade*4.5f:0;
        Vector3 beamAxis=HasFired?(End-Origin).normalized:fallbackDirection;
        Quaternion beamRotation=Quaternion.LookRotation(beamAxis,Vector3.up);
        for(int strand=0;strand<filaments.Length;strand++)
        {
            var line=filaments[strand];line.enabled=HasFired&&glowFade>0;
            line.startWidth=line.endWidth=.045f*glowFade;
            line.startColor=line.endColor=new Color(.15f,.75f,1f,.85f*glowFade);
            float dir=strand==1?-1:1;
            for(int i=0;i<49;i++)
            {
                float t=i/48f,phase=t*38f*dir-Age*32f+strand*Mathf.PI*2/3;
                float radius=.22f*Mathf.Sin(t*Mathf.PI)*glowFade;
                line.SetPosition(i,Vector3.Lerp(Origin,End,t)+beamRotation*new Vector3(Mathf.Cos(phase)*radius,Mathf.Sin(phase)*radius,0));
            }
        }
        if (HasFired&&envelope>.15f)
            BeamFxKit.BeamStream(Origin,End,Core,Blue,new Color(.85f,.95f,1f),.28f,3,3.2f);
        for(int i=0;i<shockRings.Length;i++)
        {
            float t=Mathf.Repeat(Mathf.Max(0,firedAge)*2.6f+i*.24f,1);
            Vector3 center=HasFired?Vector3.Lerp(Origin,End,t):Origin+fallbackDirection*(i*.12f);
            float radius=HasFired?.22f+t*.70f:(.55f-i*.09f)*(1-charge)+.12f;
            Ring(shockRings[i],center,beamAxis,radius,.045f,new Color(.035f,.52f,1f,(HasFired?glowFade*(1-t):charge)*.7f));
        }
        for(int i=0;i<hitCoronas.Count;i++)
            Billboard(hitCoronas[i],impactPoints[i],2.0f+Mathf.Max(0,firedAge)*3,new Color(.04f,.5f,1f,.65f*glowFade));
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
