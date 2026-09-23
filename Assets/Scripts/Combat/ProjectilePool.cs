using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.Rendering;

public class ProjectilePool : MonoBehaviour
{
    private static ProjectilePool instance;
    private ObjectPool<Projectile> beams;
    private ObjectPool<Projectile> missiles;
    private Material material;
    private Material kineticMaterial;
    private Mesh sphereMesh, kineticMesh;
    public int CreatedCount { get; private set; }
    public static ProjectilePool Instance => instance;

    private void Awake()
    {
        instance = this;
        material = new Material(Shader.Find("Sprites/Default"));
        kineticMaterial = new Material(Shader.Find("Standard"));
        kineticMaterial.SetFloat("_Metallic",.65f);kineticMaterial.SetFloat("_Glossiness",.35f);
        kineticMesh=M7MachinedMesh.Round();
        beams = MakePool(false, 256);
        missiles = MakePool(true, 64);
    }

    private ObjectPool<Projectile> MakePool(bool homing, int capacity)
    {
        return new ObjectPool<Projectile>(() => Create(homing), p => p.gameObject.SetActive(true), p => p.gameObject.SetActive(false), p => Destroy(p.gameObject), true, 32, capacity);
    }

    private Projectile Create(bool homing)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.SetActive(false);
        if(sphereMesh==null)sphereMesh=go.GetComponent<MeshFilter>().sharedMesh;
        go.transform.SetParent(transform, false);
        var renderer = go.GetComponent<Renderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        ProjectileVisuals.AddTrail(go, Color.white, Color.clear, 0.16f, 0.12f);
        Projectile projectile = homing ? go.AddComponent<MissileProjectile>() : go.AddComponent<Projectile>();
        projectile.pool = this;
        CreatedCount++;
        return projectile;
    }

    public static Projectile Spawn(bool homing, string name, Vector3 origin, Color color, float width = 0.16f)
    {
        if (instance == null) new GameObject("ProjectilePool").AddComponent<ProjectilePool>();
        var projectile = (homing ? instance.missiles : instance.beams).Get();
        projectile.Kinetic=false;
        var go = projectile.gameObject;
        go.name = name;
        go.GetComponent<MeshFilter>().sharedMesh=instance.sphereMesh;
        go.transform.SetPositionAndRotation(origin, Quaternion.identity);
        go.transform.localScale = new Vector3(width, width, homing ? width * 2f : width * 2.8f);
        var block = projectile.VisualBlock;
        go.GetComponent<Renderer>().sharedMaterial=instance.material;
        block.SetColor("_Color", color);
        go.GetComponent<Renderer>().SetPropertyBlock(block);
        var trail = go.GetComponent<TrailRenderer>();
        trail.Clear();
        trail.startColor = color;
        trail.endColor = new Color(color.r, color.g, color.b, 0);
        trail.startWidth = width;
        trail.time = homing ? 0.23f : 0.12f;
        return projectile;
    }
    public static Projectile SpawnKinetic(Vector3 origin)
    {
        var bullet=Spawn(false,"M7_KineticRound",origin,new Color(.72f,.53f,.26f),.065f);
        bullet.Kinetic=true;
        bullet.GetComponent<Renderer>().sharedMaterial=instance.kineticMaterial;
        bullet.GetComponent<MeshFilter>().sharedMesh=instance.kineticMesh;
        bullet.transform.localScale=new Vector3(.065f,.065f,.38f);
        var trail=bullet.GetComponent<TrailRenderer>();
        trail.startColor=new Color(1f,.81f,.43f,.44f);trail.endColor=new Color(.65f,.43f,.16f,0);trail.startWidth=.030f;trail.time=.008f;
        bullet.transform.localScale=new Vector3(.052f,.052f,.22f);
        return bullet;
    }

    public void Release(Projectile projectile)
    {
        if (projectile is MissileProjectile) missiles.Release(projectile);
        else beams.Release(projectile);
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
        if (material != null) Destroy(material);
        if (kineticMaterial != null) Destroy(kineticMaterial);
        if (kineticMesh != null) Destroy(kineticMesh);
    }
}
