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
    float visibleLength;
    Damageable owner;
    int team;
    Vector3 aimPoint, fallbackDirection;
    LineRenderer core, sheath, glow, hotLine, muzzleRing, impactRing;
    LineRenderer[] ribbons = new LineRenderer[3], packets = new LineRenderer[2];
    Transform muzzleCorona, impactCorona;
    Light muzzleLight, impactLight;
    ParticleSystem motes;
    readonly BeamContactResolver contacts=new BeamContactResolver();
    readonly List<Vector3> impactPoints = new List<Vector3>();
    MaterialPropertyBlock tint;

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
    bool NemesisOwner=>owner!=null&&owner.GetComponentInChildren<NemesisMotionRig>()!=null;
    Color PaletteTint=>team==1?EnergyBoltVisual.EnemyRed:NemesisOwner?MechEnergyPalette.Nemesis:Pink;
    Color PaletteCore=>team==1?new Color(1,.85f,.8f):NemesisOwner?MechEnergyPalette.NemesisCore:Core;
    Color Palette(Color color)
    {
        if(team!=1&&!NemesisOwner)return color;
        var value=color.r>.7f&&color.g>.7f&&color.b>.7f?PaletteCore:PaletteTint;value.a=color.a;return value;
    }
    void BuildVisuals()
    {
        tint=new MaterialPropertyBlock();
        if (lineMaterial==null)
        {
            var shader=Shader.Find("MECH ROUGE/Minovsky Glow");
            if (shader==null) throw new InvalidOperationException("Missing Minovsky Glow shader");
            lineMaterial=new Material(shader) { name="Minovsky beam additive glow" };
            // Energy packets stream muzzle-to-impact so the beam reads as flowing
            // particles rather than a static cylinder.
            lineMaterial.SetFloat("_Pulses",5);lineMaterial.SetFloat("_Flow",2.2f);lineMaterial.SetFloat("_PulseAmp",.85f);
            radialMaterial=new Material(shader) { name="Minovsky radial glow" };
            radialMaterial.SetFloat("_Radial",1);
        }
        glow=Line("Violet atmospheric halo",2);
        sheath=Line("Saturated pink particle sheath",2);
        core=Line("White-pink beam core",2);
        hotLine=Line("Overexposed white filament",2);
        for(int i=0;i<ribbons.Length;i++) ribbons[i]=Line("PaletteTint helix ribbon "+i,49);
        for(int i=0;i<packets.Length;i++) packets[i]=Line("Traveling energy packet "+i,2);
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
        var gradient=new Gradient();gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(PaletteTint,1)},
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
        var light=go.AddComponent<Light>();light.type=LightType.Point;light.color=PaletteTint;light.range=range;
        light.shadows=LightShadows.None;return light;
    }
    void LateUpdate()
    {
        var gm=GameManager.Instance;
        if (muzzle==null || (owner!=null && owner.IsDead) || (gm!=null && gm.Phase!=GamePhase.Combat))
        { Destroy(gameObject);return; }
        if (gm!=null && gm.IsPaused) return;
        Age+=Time.deltaTime;
        if(!contacts.BarrelObstructed)Origin=muzzle.position;
        if (!HasFired)
        {
            Vector3 offset=UnityEngine.Random.onUnitSphere*UnityEngine.Random.Range(.22f,.46f);
            Emit(Origin+offset,-offset*9,.12f,.055f,PaletteCore);
            // Motes spiral inward around the bore: the charge reads as gathering
            // Minovsky particles instead of a plain twinkle.
            BeamFxKit.ChargeConverge(Origin,fallbackDirection,PaletteCore,PaletteTint,3,.55f);
            if (Age>=ChargeDuration) Discharge();
        }
        // A connected beam follows the bore through turn, recoil and translation.
        // Damage remains a single discharge, never a sweeping damage tick.
        if(HasFired && !contacts.BarrelObstructed) End=Origin+muzzle.forward*visibleLength;
        Display();
        if (Age>=ChargeDuration+BeamDuration+ResidualDuration) Destroy(gameObject);
    }
    void Discharge()
    {
        HasFired=true;
        Vector3 delta=aimPoint-Origin;
        Vector3 direction=muzzle!=null?muzzle.forward:PlanarCombat.Direction(delta,fallbackDirection);
        DirectHitCount=contacts.Resolve(gameObject,owner,team,Origin,direction,MaximumRange,.13f,
            Penetration,Damage,BlastRadius,CombatHitKind.Generic,false,impactPoints,out var resolvedEnd);
        End=resolvedEnd;
        if(contacts.BarrelObstructed)Origin=End;
        visibleLength=Vector3.Distance(Origin,End);
        foreach(var point in impactPoints)
        {
            for (int i=0;i<14;i++) Emit(point,UnityEngine.Random.onUnitSphere*UnityEngine.Random.Range(1.8f,5),.30f,.07f,PaletteCore);
            BeamFxKit.ImpactBurst(point,direction,PaletteTint,.8f+BlastRadius*.15f);
        }
        for (int i=0;i<28;i++)
            Emit(Vector3.Lerp(Origin,End,UnityEngine.Random.value)+UnityEngine.Random.insideUnitSphere*.14f,
                direction*UnityEngine.Random.Range(1,3)+UnityEngine.Random.onUnitSphere*.35f,.35f,UnityEngine.Random.Range(.025f,.07f),PaletteTint);
        BeamFxKit.MuzzleBlast(Origin,direction,PaletteTint,1.25f);
        BeamFxKit.StarGlare(Origin,PaletteCore,2.5f,.18f);
        BeamFxKit.Twinkles(Origin,End,PaletteTint,22);
        GameAudio.Play(GameAudioCue.Beam,.58f,.80f);
        if (Camera.main!=null) Camera.main.GetComponent<CameraFollow>()?.AddShake(.15f,.16f);
    }
    void Emit(Vector3 position,Vector3 velocity,float life,float size,Color color)
    {
        color=Palette(color);
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
        // Three-layer brightness: overexposed white core, saturated pink sheath,
        // diffuse violet halo. Hue separation, not just alpha, is what reads as
        // an anime particle beam.
        Beam(glow,1.35f, new Color(.58f,.10f,.92f,.30f*glowFade));
        Beam(sheath,.50f*flicker, new Color(1,.05f,.42f,.95f*envelope));
        Beam(core,.17f*flicker, new Color(1,.90f,.97f,envelope));
        Beam(hotLine,.055f, new Color(1,.98f,1,.92f*envelope));
        float corona=HasFired?glowFade:charge;
        Billboard(muzzleCorona,Origin,(HasFired?1.18f:.56f)*(.55f+.45f*corona),new Color(1,.08f,.45f,corona*.9f));
        Billboard(impactCorona,End,2.0f,new Color(1,.06f,.36f,impactPoints.Count>0?glowFade*.8f:0));
        Ring(muzzleRing,Origin,HasFired?(End-Origin).normalized:fallbackDirection,
            HasFired?.22f+Mathf.Max(0,firedAge)*2.1f:.37f*(1-charge)+.10f,.025f,new Color(1,.16f,.58f,corona*.8f));
        Ring(impactRing,End,HasFired?(End-Origin).normalized:fallbackDirection,
            .18f+Mathf.Max(0,firedAge)*4.5f,.035f,new Color(1,.08f,.40f,impactPoints.Count>0?glowFade*.65f:0));
        muzzleLight.transform.position=Origin;muzzleLight.intensity=corona*(HasFired?6.5f:1.8f);
        impactLight.transform.position=End;impactLight.intensity=impactPoints.Count>0?glowFade*4.5f:0;
        if (HasFired)
        {
            Vector3 axis=(End-Origin).sqrMagnitude>.0001f?(End-Origin).normalized:fallbackDirection;
            Quaternion rotation=Quaternion.LookRotation(axis,Vector3.up);
            float length=Vector3.Distance(Origin,End);
            // Counter-wound pink ribbons wrapping the white core: the entwined
            // red-white beam look.
            for(int strand=0;strand<ribbons.Length;strand++)
            {
                var line=ribbons[strand];line.enabled=glowFade>0;
                line.startWidth=line.endWidth=.05f*glowFade;
                Color ribbon=strand==1?new Color(.85f,.04f,.62f):strand==2?new Color(1,.75f,.92f):new Color(1,.10f,.55f);
                line.startColor=line.endColor=Palette(new Color(ribbon.r,ribbon.g,ribbon.b,.85f*glowFade));
                float dir=strand==1?-1:1;
                for(int i=0;i<49;i++)
                {
                    float t=i/48f,phase=t*26f*dir-Age*21f+strand*Mathf.PI*2/3;
                    float radius=.30f*Mathf.Sin(t*Mathf.PI)*glowFade;
                    line.SetPosition(i,Origin+axis*(t*length)+rotation*new Vector3(Mathf.Cos(phase)*radius,Mathf.Sin(phase)*radius,0));
                }
            }
            // Bright packets race from the muzzle to the impact point.
            for(int i=0;i<packets.Length;i++)
            {
                var line=packets[i];
                float t=Mathf.Repeat(firedAge*3.1f+i*.5f,1);
                line.enabled=glowFade>0&&t<.92f;
                if (!line.enabled) continue;
                line.startWidth=line.endWidth=.11f*glowFade;
                line.startColor=line.endColor=Palette(new Color(1,.95f,.99f,.95f*envelope));
                Vector3 from=Vector3.Lerp(Origin,End,t),to=Vector3.Lerp(Origin,End,Mathf.Min(t+.09f,1));
                line.SetPosition(0,from);line.SetPosition(1,to);
            }
            if (envelope>.15f)
                BeamFxKit.BeamStream(Origin,End,PaletteCore,PaletteTint,NemesisOwner?PaletteCore:new Color(1,.95f,.55f),.34f,5,2.4f);
        }
    }
    void Beam(LineRenderer line,float width,Color color)
    {
        color=Palette(color);
        line.enabled=HasFired && color.a>.001f;
        line.startWidth=line.endWidth=width;line.startColor=line.endColor=color;
        line.SetPosition(0,Origin);line.SetPosition(1,End);
    }
    void Billboard(Transform quad,Vector3 position,float size,Color color)
    {
        color=Palette(color);
        quad.position=position;quad.localScale=Vector3.one*size;
        if (Camera.main!=null) quad.rotation=Camera.main.transform.rotation;
        tint.SetColor("_Color",color);quad.GetComponent<Renderer>().SetPropertyBlock(tint);
    }
    void Ring(LineRenderer line,Vector3 center,Vector3 axis,float radius,float width,Color color)
    {
        color=Palette(color);
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
