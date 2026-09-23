using UnityEngine;
using UnityEngine.Rendering;

[DefaultExecutionOrder(200)]
public sealed class KineticRifleFeedback : MonoBehaviour
{
    ParticleSystem flash,shells,smoke;ParticleSystem[] systems;Material fire,brass,smokeMaterial;Mesh casing,flame;Texture2D puff;
    public ParticleSystem MuzzleFlash=>flash;
    readonly ParticleSystem.Particle[] floorParticles=new ParticleSystem.Particle[160];
    public int EjectedCount {get;private set;}
    public ParticleSystem Casings=>shells;
    void Awake()
    {
        fire=new Material(Shader.Find("Sprites/Default"));
        brass=new Material(Shader.Find("Standard"));brass.color=new Color(.72f,.46f,.16f);
        brass.SetFloat("_Metallic",.82f);brass.SetFloat("_Glossiness",.62f);brass.enableInstancing=true;
        puff=new Texture2D(64,64,TextureFormat.RGBA32,false);
        for(int y=0;y<64;y++)for(int x=0;x<64;x++){float r=Vector2.Distance(new Vector2(x,y),new Vector2(31.5f,31.5f))/31.5f;float a=Mathf.Pow(Mathf.Clamp01(1-r),2);puff.SetPixel(x,y,new Color(1,1,1,a));}
        puff.Apply();fire.mainTexture=puff;
        smokeMaterial=new Material(fire);
        flash=Create("M7_CombustionFlash",fire,48);shells=Create("M7_BrassCasings",brass,160);smoke=Create("M7_BarrelSmoke",smokeMaterial,72);
        {
            // Crossed tapered flame lobes aligned to the bore; the shape reads as combustion.
            var vertices=new Vector3[15];var triangles=new int[27];var uv=new Vector2[15];
            for(int plane=0;plane<3;plane++)
            {
                var q=Quaternion.Euler(0,0,plane*60);int v=plane*5,k=plane*9;
                vertices[v]=q*new Vector3(-.10f,0,0);vertices[v+1]=q*new Vector3(.10f,0,0);vertices[v+2]=q*new Vector3(.52f,0,.27f);vertices[v+3]=q*new Vector3(0,0,1);vertices[v+4]=q*new Vector3(-.44f,0,.24f);
                for(int j=0;j<5;j++)uv[v+j]=new Vector2(.5f+vertices[v+j].x*.65f,.25f+vertices[v+j].z*.45f);
                triangles[k]=v;triangles[k+1]=v+1;triangles[k+2]=v+3;triangles[k+3]=v+1;triangles[k+4]=v+2;triangles[k+5]=v+3;triangles[k+6]=v;triangles[k+7]=v+3;triangles[k+8]=v+4;
            }
            flame=new Mesh{name="M7_BoreFlame",vertices=vertices,uv=uv,triangles=triangles};flame.RecalculateNormals();flame.RecalculateBounds();
            var fr=flash.GetComponent<ParticleSystemRenderer>();fr.renderMode=ParticleSystemRenderMode.Mesh;fr.mesh=flame;
            var fm=flash.main;fm.startSize3D=true;fm.startRotation3D=true;
        }
        systems=new[]{flash,shells,smoke};
        var main=shells.main;main.gravityModifier=1;main.startSize3D=true;main.startRotation3D=true;
        var renderer=shells.GetComponent<ParticleSystemRenderer>();renderer.renderMode=ParticleSystemRenderMode.Mesh;
        casing=M7MachinedMesh.Casing();renderer.mesh=casing;renderer.enableGPUInstancing=true;renderer.shadowCastingMode=ShadowCastingMode.On;
        var rotation=shells.rotationBySpeed;rotation.enabled=true;rotation.separateAxes=true;
        rotation.range=new Vector2(0,3);
        rotation.x=new ParticleSystem.MinMaxCurve(12,AnimationCurve.Linear(0,0,1,1));rotation.y=new ParticleSystem.MinMaxCurve(-8,AnimationCurve.Linear(0,0,1,1));rotation.z=new ParticleSystem.MinMaxCurve(7,AnimationCurve.Linear(0,0,1,1));
        var collision=shells.collision;collision.enabled=true;collision.type=ParticleSystemCollisionType.World;collision.mode=ParticleSystemCollisionMode.Collision3D;
        collision.quality=ParticleSystemCollisionQuality.Medium;collision.enableDynamicColliders=false;collision.bounce=.32f;collision.dampen=.55f;collision.radiusScale=.6f;
        var size=shells.sizeOverLifetime;size.enabled=true;size.size=new ParticleSystem.MinMaxCurve(1, new AnimationCurve(new Keyframe(0,1),new Keyframe(.87f,1),new Keyframe(1,0)));
        var fade=smoke.colorOverLifetime;fade.enabled=true;var gradient=new Gradient();gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},new[]{new GradientAlphaKey(.5f,0),new GradientAlphaKey(0,1)});fade.color=gradient;
        var grow=smoke.sizeOverLifetime;grow.enabled=true;grow.size=new ParticleSystem.MinMaxCurve(1,new AnimationCurve(new Keyframe(0,.35f),new Keyframe(1,1)));
    }
    ParticleSystem Create(string name,Material material,int maximum)
    {
        var go=new GameObject(name);go.transform.SetParent(transform,false);
        var ps=go.AddComponent<ParticleSystem>();ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
        var main=ps.main;main.playOnAwake=false;main.loop=true;main.maxParticles=maximum;main.simulationSpace=ParticleSystemSimulationSpace.World;
        var emission=ps.emission;emission.enabled=false;var shape=ps.shape;shape.enabled=false;
        var render=ps.GetComponent<ParticleSystemRenderer>();render.sharedMaterial=material;render.shadowCastingMode=ShadowCastingMode.Off;render.receiveShadows=true;
        ps.Play();return ps;
    }
    public void Shot(Vector3 muzzle,Vector3 direction,Transform port=null)
    {
        direction.Normalize();Vector3 right=Vector3.Cross(Vector3.up,direction).normalized;
        {
            flash.Emit(new ParticleSystem.EmitParams{position=muzzle+direction*.025f,velocity=direction*.25f,startLifetime=.045f,startSize3D=new Vector3(.33f,.33f,Random.Range(.48f,.62f)),rotation3D=Quaternion.LookRotation(direction).eulerAngles,startColor=new Color(1,.80f,.34f,1)},1);
            smoke.Emit(new ParticleSystem.EmitParams{position=muzzle,velocity=direction*.25f+Vector3.up*.23f,startLifetime=.23f,startSize=.22f,startColor=new Color(.38f,.36f,.32f,.17f)},1);
        }
        Vector3 eject=port!=null?port.position:muzzle-direction*1.5f+right*.10f;
        if(port!=null)right=port.right;
        Vector3 inherited=GetComponent<PlayerController>().Velocity*.55f;
        shells.Emit(new ParticleSystem.EmitParams{position=eject,velocity=inherited+right*Random.Range(1.7f,2.7f)+Vector3.up*Random.Range(1.2f,2f)-direction*Random.Range(.2f,.7f),startLifetime=Random.Range(8,10f),startSize3D=new Vector3(.082f,.082f,.22f),startColor=Color.white,rotation3D=Random.insideUnitSphere*180},1);
        EjectedCount++;
    }
    void Update()
    {
        if(GameManager.Instance==null)return;
        bool pause=GameManager.Instance.IsPaused;
        foreach(var ps in systems){if(pause&&!ps.isPaused)ps.Pause();else if(!pause&&ps.isPaused)ps.Play();}
        if(!GameManager.Instance.IsCombatActive&&!pause){shells.Clear();flash.Clear();smoke.Clear();}
    }
    void LateUpdate()
    {
        // The P0 floor is visual scenery; planar gameplay does not require a floor collider.
        // Catch cosmetic brass on that same flat floor without adding gameplay obstacles.
        if(GameManager.Instance==null||!GameManager.Instance.IsCombatActive)return;
        int count=shells.GetParticles(floorParticles);bool changed=false;
        for(int i=0;i<count;i++)
        {
            var particle=floorParticles[i];if(particle.position.y>=.055f)continue;
            var position=particle.position;position.y=.055f;particle.position=position;
            var velocity=particle.velocity;velocity=new Vector3(velocity.x*.52f,Mathf.Abs(velocity.y)*.27f,velocity.z*.52f);
            if(velocity.y<.22f)velocity=Vector3.zero;
            particle.velocity=velocity;floorParticles[i]=particle;changed=true;
        }
        if(changed)shells.SetParticles(floorParticles,count);
    }
    void OnDestroy(){if(fire!=null)Destroy(fire);if(brass!=null)Destroy(brass);if(smokeMaterial!=null)Destroy(smokeMaterial);if(casing!=null)Destroy(casing);if(flame!=null)Destroy(flame);if(puff!=null)Destroy(puff);}
}
