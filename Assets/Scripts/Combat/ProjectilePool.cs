using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.Rendering;

public class ProjectilePool : MonoBehaviour
{
    private static ProjectilePool instance;
    private ObjectPool<Projectile> beams;
    private ObjectPool<Projectile> missiles;
    private Material material;
    public int CreatedCount { get; private set; }
    public static ProjectilePool Instance => instance;

    private void Awake()
    {
        instance = this;
        material = new Material(Shader.Find("Sprites/Default"));
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
        var go = projectile.gameObject;
        go.name = name;
        go.transform.SetPositionAndRotation(origin, Quaternion.identity);
        go.transform.localScale = new Vector3(width, width, homing ? width * 2f : width * 2.8f);
        var block = projectile.VisualBlock;
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

    public void Release(Projectile projectile)
    {
        if (projectile is MissileProjectile) missiles.Release(projectile);
        else beams.Release(projectile);
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
        if (material != null) Destroy(material);
    }
}
