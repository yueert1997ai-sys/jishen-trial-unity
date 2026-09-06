using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// Reusable world-space particles follow the real blade, floor contacts and thruster sockets.
[DefaultExecutionOrder(170)]
[DisallowMultipleComponent]
public sealed class RaikenCombatVfx : MonoBehaviour
{
    public int SlashParticles { get; private set; }
    public int ScrapeParticles { get; private set; }
    public int ThrustParticles { get; private set; }
    public int FootfallBursts { get; private set; }
    public int ImpactBursts { get; private set; }
    public bool Scraping { get; private set; }
    public float ScrapeGap { get; private set; }
    public int LiveParticles => energy.particleCount+metal.particleCount+wake.particleCount+dust.particleCount;
    private RaikenBladePresentation blade;
    private ValkyrMotionDriver motion;
    private PlayerController player;
    private Damageable health;
    private ParticleSystem energy,metal,wake,dust,flash;
    private LineRenderer edgeHalo,edgeCore;
    private Material glow,strip,smoke;
    private Texture2D dotTexture,stripTexture;
    private readonly List<Vector3> edgePoints=new List<Vector3>();
    private readonly List<Vector3> beamVertices=new List<Vector3>();
    private Vector3 previousTip,previousGrip;
    private float slashBudget,scrapeBudget,thrustBudget;
    private bool wasLeft,wasRight;
    private readonly RaycastHit[] floorHits=new RaycastHit[20];
    private Transform[] thrusters;

    private void Start()
    {
        blade=GetComponent<RaikenBladePresentation>();motion=GetComponent<ValkyrMotionDriver>();
        player=GetComponentInParent<PlayerController>();health=player.GetComponent<Damageable>();
        dotTexture=Falloff(false);stripTexture=Falloff(true);
        glow=new Material(Resources.Load<Shader>("MechActionGlow")){mainTexture=dotTexture};
        strip=new Material(glow){mainTexture=stripTexture};
        smoke=new Material(Shader.Find("Sprites/Default")){mainTexture=dotTexture};
        energy=Particles("Raiken_BlueCutParticles",900,glow,true,.1f);
        metal=Particles("Raiken_MetalContactSparks",900,glow,true,.9f);
        wake=Particles("Valkyr_ThrusterMotes",600,glow,true,0);
        dust=Particles("Valkyr_FootfallDust",180,smoke,false,-.025f);
        flash=Particles("Raiken_ContactFlash",24,glow,false,0);
        CacheEdge();
        edgeHalo=EdgeLine("Raiken_BlueEdgeHalo",.19f,new Color(.025f,.19f,1,.52f));
        edgeCore=EdgeLine("Raiken_BlueEdgeCore",.055f,new Color(.16f,.7f,1,.92f));
        thrusters=blade.rig.rigidPose.thrusters;
        previousTip=blade.tip.position;previousGrip=blade.grip.position;
        player.Melee.StrikeHit+=Impact;
    }
    private Texture2D Falloff(bool line)
    {
        var tex=new Texture2D(32,32,TextureFormat.RGBA32,false){wrapMode=TextureWrapMode.Clamp};
        var colors=new Color[1024];
        for(int y=0;y<32;y++)for(int x=0;x<32;x++)
        {
            float dx=(x-15.5f)/15.5f,dy=(y-15.5f)/15.5f;
            float r=line?Mathf.Abs(dy):Mathf.Sqrt(dx*dx+dy*dy);
            colors[y*32+x]=new Color(1,1,1,Mathf.Pow(Mathf.Clamp01(1-r),1.65f));
        }
        tex.SetPixels(colors);tex.Apply(false,true);return tex;
    }
    private ParticleSystem Particles(string name,int capacity,Material material,bool stretch,float gravity)
    {
        var obj=new GameObject(name);obj.transform.SetParent(transform,false);
        var ps=obj.AddComponent<ParticleSystem>();ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
        var main=ps.main;main.loop=true;main.playOnAwake=false;main.maxParticles=capacity;
        main.simulationSpace=ParticleSystemSimulationSpace.World;main.gravityModifier=gravity;
        var emission=ps.emission;emission.enabled=false;var shape=ps.shape;shape.enabled=false;
        var color=ps.colorOverLifetime;color.enabled=true;
        var gradient=new Gradient();gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},
            new[]{new GradientAlphaKey(1,0),new GradientAlphaKey(.9f,.25f),new GradientAlphaKey(0,1)});color.color=gradient;
        var size=ps.sizeOverLifetime;size.enabled=true;
        size.size=new ParticleSystem.MinMaxCurve(1,AnimationCurve.Linear(0,stretch?1:.65f,1,stretch?.12f:1.5f));
        var renderer=ps.GetComponent<ParticleSystemRenderer>();renderer.sharedMaterial=material;
        renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
        renderer.renderMode=stretch?ParticleSystemRenderMode.Stretch:ParticleSystemRenderMode.Billboard;
        renderer.lengthScale=2;renderer.velocityScale=.065f;
        if(gravity>.5f)
        {
            var collision=ps.collision;collision.enabled=true;collision.type=ParticleSystemCollisionType.World;
            collision.quality=ParticleSystemCollisionQuality.Low;collision.bounce=.28f;collision.dampen=.45f;
            collision.lifetimeLoss=.35f;collision.enableDynamicColliders=false;
        }
        ps.Play();return ps;
    }
    private void CacheEdge()
    {
        Vector3 origin=blade.grip.position,axis=(blade.tip.position-origin).normalized,edge=motion.BladeEdge;
        float length=Vector3.Distance(origin,blade.tip.position);
        foreach(var filter in blade.beam.GetComponentsInChildren<MeshFilter>(true))
            foreach(var vertex in filter.sharedMesh.vertices)beamVertices.Add(filter.transform.TransformPoint(vertex));
        for(int i=0;i<18;i++)
        {
            float along=Mathf.Lerp(length*.10f,length,i/17f),best=float.NegativeInfinity,offset=0;
            foreach(var vertex in beamVertices)
            {
                Vector3 d=vertex-origin;float e=Vector3.Dot(d,edge);
                float score=e-Mathf.Abs(Vector3.Dot(d,axis)-along)*5;
                if(score>best){best=score;offset=e;}
            }
            edgePoints.Add(blade.bladeRoot.InverseTransformPoint(origin+axis*along+edge*offset));
        }
        beamVertices.Clear();
    }
    private LineRenderer EdgeLine(string name,float width,Color color)
    {
        var line=new GameObject(name).AddComponent<LineRenderer>();line.transform.SetParent(blade.bladeRoot,false);
        line.sharedMaterial=strip;line.useWorldSpace=false;line.positionCount=edgePoints.Count;line.SetPositions(edgePoints.ToArray());
        // LineRenderer width is a world-space measurement even under the scaled weapon root.
        line.startWidth=line.endWidth=width;line.startColor=line.endColor=color;line.numCapVertices=4;
        line.shadowCastingMode=ShadowCastingMode.Off;line.receiveShadows=false;return line;
    }
    private void Emit(ParticleSystem ps,Vector3 position,Vector3 velocity,Color color,float size,float life)
    {
        ps.Emit(new ParticleSystem.EmitParams{position=position,velocity=velocity,startColor=color,startSize=size,startLifetime=life,rotation=Random.Range(0,360)},1);
    }
    private void LateUpdate()
    {
        if(blade==null||GameManager.Instance==null||GameManager.Instance.IsPaused)return;
        float dt=Time.deltaTime;if(dt<=0)return;
        bool active=GameManager.Instance.IsCombatActive&&!health.IsDead;
        bool cut=active&&blade.BeamEnabled&&player.Stance.CanMelee&&player.Melee.IsAttacking;
        float elapsed=player.Melee.AttackElapsed;
        edgeHalo.enabled=edgeCore.enabled=blade.BeamEnabled&&!health.IsDead;
        float strength=cut?1.35f:1;
        edgeHalo.startWidth=edgeHalo.endWidth=.19f*strength;
        edgeCore.startWidth=edgeCore.endWidth=.055f*strength;
        Vector3 tip=blade.tip.position,grip=blade.grip.position;
        Vector3 tipVelocity=(tip-previousTip)/dt;
        if(tipVelocity.magnitude>160)tipVelocity=tipVelocity.normalized*160;
        if(cut&&player.Melee.IsCutting&&!player.Melee.ImpactHeld)
        {
            slashBudget+=dt*320;
            while(slashBudget>=1)
            {
                slashBudget--;
                float frame=Random.value,t=Random.Range(.34f,.98f);
                Vector3 now=Vector3.Lerp(grip,tip,t),old=Vector3.Lerp(previousGrip,previousTip,t);
                Vector3 point=Vector3.Lerp(old,now,frame)+motion.BladeEdge*.06f;
                Emit(energy,point,-tipVelocity*.025f+motion.BladeEdge*Random.Range(1f,4f)+Random.insideUnitSphere,
                    new Color(.025f,Random.Range(.32f,.72f),1,.95f),Random.Range(.055f,.12f),Random.Range(.13f,.29f));
                SlashParticles++;
            }
        }
        else slashBudget=0;
        Scraping=false;
        if(active&&player.Stance.CanMelee&&!player.Melee.IsAttacking&&!player.IsDashing&&!player.IsBoosting&&motion.FlightBlend<.08f&&player.Velocity.sqrMagnitude>1)
        {
            // Only emit where the current moving tip is actually within contact tolerance of a floor.
            if(Floor(tip,out Vector3 point,out Vector3 normal))
            {
                ScrapeGap=Vector3.Dot(tip-point,normal);
                if(ScrapeGap>-.035f&&ScrapeGap<.16f)
                {
                    Scraping=true;scrapeBudget+=dt*Mathf.Lerp(85,160,Mathf.Clamp01(player.Velocity.magnitude/8));
                    Vector3 backwards=-Vector3.ProjectOnPlane(player.Velocity,normal).normalized;
                    while(scrapeBudget>=1)
                    {
                        scrapeBudget--;
                        Vector3 velocity=backwards*Random.Range(2,6)+normal*Random.Range(.6f,2.9f)+Vector3.Cross(backwards,normal)*Random.Range(-1.6f,1.6f);
                        Emit(metal,point+normal*.025f,velocity,new Color(1,Random.Range(.35f,.78f),.06f,1),Random.Range(.045f,.095f),Random.Range(.18f,.42f));
                        ScrapeParticles++;
                    }
                    if(Random.value<dt*25)Emit(flash,point+normal*.04f,Vector3.zero,new Color(1,.48f,.07f,.85f),.26f,.06f);
                }
            }
        }
        if(!Scraping)scrapeBudget=0;
        float speed=player.Velocity.magnitude;
        if(active&&(speed>.7f||player.IsBoosting))
        {
            thrustBudget+=dt*(player.IsBoosting||player.IsDashing?100:54);
            Vector3 exhaust=-(speed>.1f?player.Velocity.normalized:player.transform.forward)+Vector3.down*.18f;
            while(thrustBudget>=1)
            {
                thrustBudget--;
                foreach(var jet in thrusters)
                {
                    Emit(wake,jet.position+Random.insideUnitSphere*.035f,exhaust*Random.Range(1.7f,4.5f)+Random.insideUnitSphere*.45f,
                        new Color(.04f,.5f,1,.7f),Random.Range(.055f,.12f),Random.Range(.16f,.34f));ThrustParticles++;
                }
            }
            if(motion.FlightBlend<.08f&&!player.Melee.IsAttacking)
            {
                if(motion.LeftPlanted&&!wasLeft)Footfall(motion.LeftFoot);
                if(motion.RightPlanted&&!wasRight)Footfall(motion.RightFoot);
            }
        }
        else thrustBudget=0;
        wasLeft=motion.LeftPlanted;wasRight=motion.RightPlanted;
        previousTip=tip;previousGrip=grip;
    }
    private bool Floor(Vector3 near,out Vector3 point,out Vector3 normal)
    {
        point=normal=Vector3.zero;float closest=float.PositiveInfinity;
        int n=Physics.RaycastNonAlloc(near+Vector3.up*.3f,Vector3.down,floorHits,.8f,~0,QueryTriggerInteraction.Ignore);
        for(int i=0;i<n;i++)
        {
            var h=floorHits[i];if(h.normal.y<.65f||h.collider.GetComponentInParent<Damageable>()!=null||h.distance>=closest)continue;
            closest=h.distance;point=h.point;normal=h.normal;
        }
        return closest<float.PositiveInfinity;
    }
    private void Footfall(Transform foot)
    {
        Vector3 near=foot.position;near.y=player.transform.position.y+.12f;
        if(!Floor(near,out Vector3 point,out Vector3 normal))return;
        FootfallBursts++;
        for(int i=0;i<7;i++)Emit(dust,point+normal*.045f,Random.insideUnitSphere*.6f-player.Velocity*.08f+normal*.25f,
            new Color(.45f,.53f,.58f,.22f),Random.Range(.15f,.30f),Random.Range(.2f,.4f));
    }
    private void Impact(Damageable target,float damage)
    {
        if(target==null)return;
        ImpactBursts++;
        Vector3 axis=blade.tip.position-blade.grip.position;
        Vector3 onBlade=blade.grip.position+axis*Mathf.Clamp01(Vector3.Dot(target.AimCenter-blade.grip.position,axis)/axis.sqrMagnitude);
        Vector3 outward=(onBlade-target.AimCenter).normalized;if(outward.sqrMagnitude<.1f)outward=-player.Melee.AttackForward;
        var collider=target.GetComponent<Collider>();
        // ClosestPoint returns the query itself when it is inside a collider. Project from
        // outside so particles leave the contacted armor surface instead of dying inside it.
        Vector3 outside=target.AimCenter+outward*(collider!=null?collider.bounds.extents.magnitude+1:1);
        Vector3 point=(collider!=null?collider.ClosestPoint(outside):target.AimCenter)+outward*.12f;
        for(int i=0;i<42;i++)
            Emit(metal,point+Random.insideUnitSphere*.07f,outward*Random.Range(2,5)+motion.BladeEdge*Random.Range(2,7)+Random.onUnitSphere*3.5f+Vector3.up,
                new Color(1,Random.Range(.45f,.9f),.18f,1),Random.Range(.065f,.14f),Random.Range(.18f,.48f));
        for(int i=0;i<30;i++)
            Emit(energy,point+Random.insideUnitSphere*.10f,motion.BladeEdge*Random.Range(2,7)+Random.onUnitSphere*4,
                new Color(.025f,.5f,1,1),Random.Range(.085f,.18f),Random.Range(.15f,.34f));
        Emit(flash,point,Vector3.zero,new Color(.12f,.58f,1,.9f),1.45f,.10f);
        Emit(flash,point,Vector3.zero,new Color(.65f,.9f,1,1),.58f,.065f);
        for(int i=0;i<8;i++)Emit(dust,point,Random.insideUnitSphere+Vector3.up*.5f,new Color(.25f,.31f,.36f,.32f),Random.Range(.25f,.5f),.45f);
        if(!target.IsDead)(target.GetComponent<MechBladeHitReaction>()??target.gameObject.AddComponent<MechBladeHitReaction>()).Trigger(player.Melee.AttackForward);
    }
    private void OnDestroy()
    {
        if(player!=null&&player.Melee!=null)player.Melee.StrikeHit-=Impact;
        if(edgeHalo!=null)Destroy(edgeHalo.gameObject);if(edgeCore!=null)Destroy(edgeCore.gameObject);
        if(glow!=null)Destroy(glow);if(strip!=null)Destroy(strip);if(smoke!=null)Destroy(smoke);
        if(dotTexture!=null)Destroy(dotTexture);if(stripTexture!=null)Destroy(stripTexture);
    }
}
