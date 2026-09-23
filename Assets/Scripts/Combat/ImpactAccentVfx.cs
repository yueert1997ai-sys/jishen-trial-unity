using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// Authored silhouettes over the physical sparks: short strike, broad break, directional kill.
// Driven exclusively by committed DamageResult; bounded, local and independent of gameplay RNG.
[DefaultExecutionOrder(210)]
public sealed class ImpactAccentVfx : MonoBehaviour
{
    public const int Capacity=48;
    public static ImpactAccentVfx Instance {get;private set;}
    public int ActiveCount {get;private set;}
    public int Presented {get;private set;}
    public HitPresentation LastTier {get;private set;}
    public Vector3 LastPoint {get;private set;}
    struct Burst {public Vector3 point,tangent;public Color color;public float age,life,size;public bool cut,heavy,kill,broken;}
    readonly Burst[] bursts=new Burst[Capacity];int next,generation;
    readonly List<Vector3> vertices=new List<Vector3>(4096);
    readonly List<Color> colors=new List<Color>(4096);
    readonly List<int> triangles=new List<int>(6144);
    Mesh lightMesh,darkMesh;Material lightMaterial,darkMaterial;
    public static ImpactAccentVfx Get()
    {if(Instance==null)new GameObject("Layered combat impacts").AddComponent<ImpactAccentVfx>();return Instance;}
    void Awake()
    {
        Instance=this;generation=CombatRuntime.Generation;
        lightMaterial=new Material(Resources.Load<Shader>("RaikenEnergy"));
        darkMaterial=new Material(Resources.Load<Shader>("CombatContrast"));
        lightMesh=Make("Contact light",lightMaterial);darkMesh=Make("Contact silhouette",darkMaterial);
    }
    Mesh Make(string name,Material material)
    {
        var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(transform,false);
        var mesh=new Mesh{name=name};mesh.MarkDynamic();go.GetComponent<MeshFilter>().sharedMesh=mesh;
        var renderer=go.GetComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
        return mesh;
    }
    public void Present(Damageable target,DamageResult result)
    {
        if(!result.Applied||target.team==0||CombatLabSettings.MinimalFeedback)return;
        if(generation!=CombatRuntime.Generation){Clear();generation=CombatRuntime.Generation;}
        var info=result.ToDamageInfo();bool cut=info.MeleeStrike,armor=result.ArmorDamage>0;
        Vector3 point=info.HasContact?info.ContactPoint:target.AimCenter;
        Vector3 tangent=cut?info.ContactTangent:Vector3.Cross(Vector3.up,info.ContactNormal);
        if(tangent.sqrMagnitude<.01f)tangent=Vector3.right;
        LastPoint=point;LastTier=result.Presentation;Presented++;
        // A mech's lethal silhouette is its reactor blast and flying armor.
        // Keep the resolved event accounting without covering it with a flat star.
        if(result.Lethal&&target.GetComponent<E01SoldierMotion>()!=null)return;
        bursts[next++%Capacity]=new Burst{point=point,tangent=tangent.normalized,cut=cut,heavy=info.HeavyImpact,kill=result.Lethal,broken=result.BrokeArmor,
            life=result.Lethal?.34f:result.BrokeArmor?.32f:cut?.20f:.14f,
            size=result.Lethal?1.9f:result.BrokeArmor?2.1f:cut?(info.HeavyImpact?1.65f:1.25f):.65f,
            color=result.BrokeArmor?new Color(.30f,.92f,1):armor?new Color(.46f,.68f,1):cut?new Color(.14f,.78f,1):new Color(1,.65f,.23f)};
    }
    void LateUpdate()
    {
        var gm=GameManager.Instance;
        if(gm==null||generation!=CombatRuntime.Generation||!gm.IsCombatActive&&!gm.IsPaused){Clear();generation=CombatRuntime.Generation;return;}
        if(gm.IsPaused)return;
        ActiveCount=0;
        for(int i=0;i<Capacity;i++){if(bursts[i].life<=0)continue;bursts[i].age+=Time.deltaTime;if(bursts[i].age>=bursts[i].life)bursts[i].life=0;else ActiveCount++;}
        Draw(darkMesh,true);Draw(lightMesh,false);
    }
    void Triangle(Vector3 a,Vector3 b,Vector3 c,Color tint)
    {
        int n=vertices.Count;vertices.Add(a);vertices.Add(b);vertices.Add(c);
        colors.Add(tint);colors.Add(tint);colors.Add(new Color(tint.r,tint.g,tint.b,0));
        triangles.Add(n);triangles.Add(n+1);triangles.Add(n+2);
    }
    void Spike(Vector3 p,Vector3 direction,Vector3 across,float length,float width,Color color)
    {Triangle(p-across*width,p+across*width,p+direction*length,color);}
    void Draw(Mesh mesh,bool dark)
    {
        vertices.Clear();colors.Clear();triangles.Clear();var camera=Camera.main;if(camera==null)return;
        Vector3 right=camera.transform.right,up=camera.transform.up,normal=-camera.transform.forward;
        foreach(var b in bursts)
        {
            if(b.life<=0)continue;
            float t=b.age/b.life,fade=Mathf.Pow(1-t,1.35f),size=b.size*Mathf.Lerp(.72f,1.20f,Mathf.Sqrt(t));
            if(dark)size*=1.13f;
            Vector3 p=b.point+normal*(dark?.18f:.20f);
            Vector3 along=Vector3.ProjectOnPlane(b.tangent,normal).normalized;
            if(along.sqrMagnitude<.1f)along=right;
            Vector3 across=Vector3.Cross(normal,along).normalized;
            var color=b.color;color.a=fade*(dark?.70f:.90f);
            int count=b.kill||b.broken?10:b.cut?6:4;
            for(int k=0;k<count;k++)
            {
                float angle=k*Mathf.PI*2/count;
                Vector3 direction=along*Mathf.Cos(angle)+across*Mathf.Sin(angle);
                float length=size*(k%2==0?1:.5f);
                if(b.cut&&k%3==0)length*=1.4f;
                Spike(p,direction,Vector3.Cross(normal,direction),length,size*(b.cut?.095f:.13f),color);
            }
            if(!dark)
            {
                var core=Color.Lerp(b.color,Color.white,.88f);core.a=Mathf.Clamp01((.11f-b.age)/.06f)*.9f;
                Spike(p+normal*.01f,along,across,size*.54f,size*.18f,core);
                Spike(p+normal*.01f,-along,across,size*.54f,size*.18f,core);
            }
            if((b.kill||b.broken)&&!dark)
            {
                float radius=b.size*(.35f+t*1.1f),width=(1-t)*.11f;
                var ring=b.color;ring.a=fade*.55f;
                for(int k=0;k<24;k++)
                {
                    if(b.broken&&k%4==3)continue;
                    float a=k*Mathf.PI/12,z=(k+1)*Mathf.PI/12;
                    Vector3 d=right*Mathf.Cos(a)+up*Mathf.Sin(a),e=right*Mathf.Cos(z)+up*Mathf.Sin(z);
                    int n=vertices.Count;vertices.Add(p+d*radius);vertices.Add(p+d*(radius+width));vertices.Add(p+e*radius);vertices.Add(p+e*(radius+width));
                    for(int j=0;j<4;j++)colors.Add(ring);
                    triangles.Add(n);triangles.Add(n+1);triangles.Add(n+2);triangles.Add(n+1);triangles.Add(n+3);triangles.Add(n+2);
                }
            }
        }
        mesh.Clear();mesh.SetVertices(vertices);mesh.SetColors(colors);mesh.SetTriangles(triangles,0);mesh.RecalculateBounds();
    }
    public void Clear(){for(int i=0;i<Capacity;i++)bursts[i].life=0;ActiveCount=0;if(lightMesh!=null)lightMesh.Clear();if(darkMesh!=null)darkMesh.Clear();}
    void OnDestroy(){if(Instance==this)Instance=null;Destroy(lightMesh);Destroy(darkMesh);Destroy(lightMaterial);Destroy(darkMaterial);}
}
