using UnityEngine;
using UnityEngine.Rendering;

// Textured additive VFX layer for enemy combat feedback. Sprites are CC0 from
// the Kenney Particle Pack (see docs/ASSET_PROVENANCE.md); systems, materials
// and meshes are runtime-authored like the rest of the presentation stack.
public class EnemyVfx : MonoBehaviour
{
    private static EnemyVfx instance;
    private ParticleSystem glow, fire, fireAlt, ring, muzzle, crackle, smokeTex, hotSparks, shrapnel;
    private Material additive;
    private Material smokeMaterial;

    private static EnemyVfx Get()
    {
        if (instance == null) new GameObject("EnemyVfx").AddComponent<EnemyVfx>();
        return instance;
    }

    private void Awake()
    {
        instance = this;
        additive = new Material(Shader.Find("MECH ROUGE/Particle Additive"));
        smokeMaterial = new Material(Shader.Find("Sprites/Default"));
        smokeMaterial.mainTexture = Load("VFX/Sprites/smoke_05");
        glow = Textured("EnemyVfxGlow", "VFX/Sprites/light_01", 128, additive);
        fire = Textured("EnemyVfxFire", "VFX/Sprites/fire_01", 128, additive);
        fireAlt = Textured("EnemyVfxFireAlt", "VFX/Sprites/fire_02", 128, additive);
        muzzle = Textured("EnemyVfxMuzzle", "VFX/Sprites/muzzle_02", 96, additive);
        crackle = Textured("EnemyVfxCrackle", "VFX/Sprites/spark_01", 96, additive);
        ring = Textured("EnemyVfxRing", "VFX/Sprites/circle_03", 64, additive);
        // The ring system only ever emits shockwaves: size grows over its lifetime.
        var size = ring.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1, AnimationCurve.EaseInOut(0, 0.15f, 1, 1f));
        smokeTex = Textured("EnemyVfxSmoke", "VFX/Sprites/smoke_05", 96, smokeMaterial);
        hotSparks = Textured("EnemyVfxHotSparks", "VFX/Sprites/light_01", 256, additive);
        ConfigureHotSparks(hotSparks);
        shrapnel = Textured("EnemyVfxShrapnel", "VFX/Sprites/dirt_01", 128, smokeMaterial);
        ConfigureShrapnel(shrapnel);
    }

    // White-hot streaks along velocity with gravity: the readable "steel on steel" spray.
    private static void ConfigureHotSparks(ParticleSystem ps)
    {
        var main = ps.main;
        main.gravityModifier = 0.95f;
        var color = ps.colorOverLifetime;
        color.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(new Color(1f, .62f, .2f), .55f), new GradientColorKey(new Color(.9f, .3f, .06f), 1) },
            new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(.85f, .5f), new GradientAlphaKey(0, 1) });
        color.color = gradient;
        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Stretch;
        renderer.lengthScale = 1.5f;
        renderer.velocityScale = 0.12f;
    }

    // Dark tumbling fragments: mass reads through weight and spin, not brightness.
    private static void ConfigureShrapnel(ParticleSystem ps)
    {
        var main = ps.main;
        main.gravityModifier = 1.25f;
        var spin = ps.rotationOverLifetime;
        spin.enabled = true;
        spin.z = Mathf.PI * 0.9f;
        var size = ps.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1, AnimationCurve.Linear(0, 1, 1, 0.55f));
    }

    private static Texture2D Load(string path)
    {
        var texture = Resources.Load<Texture2D>(path);
        if (texture == null) Debug.LogError("EnemyVfx missing sprite: " + path);
        return texture;
    }

    private ParticleSystem Textured(string name, string texturePath, int limit, Material material)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform, false);
        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main;
        main.loop = true;
        main.playOnAwake = false;
        main.maxParticles = limit;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        var emission = ps.emission;
        emission.enabled = false;
        var shape = ps.shape;
        shape.enabled = false;
        var color = ps.colorOverLifetime;
        color.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) },
            new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(0.6f, 0.5f), new GradientAlphaKey(0, 1) });
        color.color = gradient;
        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = material;
        // Each system owns one texture: swap on the renderer's material instance.
        var textured = new Material(material) { mainTexture = Resources.Load<Texture2D>(texturePath) };
        renderer.sharedMaterial = textured;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        ps.Play();
        return ps;
    }

    private void Emit(ParticleSystem system, Vector3 position, Color color, float size,
        float lifetime, Vector3 velocity, float rotation = 0f)
    {
        system.Emit(new ParticleSystem.EmitParams
        {
            position = position,
            velocity = velocity,
            startLifetime = lifetime,
            startSize = size,
            startColor = color,
            rotation = rotation,
        }, 1);
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
        if (additive != null) Destroy(additive);
        if (smokeMaterial != null) Destroy(smokeMaterial);
        foreach (var ps in GetComponentsInChildren<ParticleSystem>(true))
        {
            if (ps.TryGetComponent<ParticleSystemRenderer>(out var renderer) && renderer.sharedMaterial != null)
                Destroy(renderer.sharedMaterial);
        }
    }

    public static void DeathBurst(Vector3 center, Color color, float scale)
    {
        if (CombatLabSettings.MinimalFeedback) return;
        var host = Get();
        Color hot = Color.Lerp(color, Color.white, 0.55f);
        host.Emit(host.glow, center, hot, 2.8f * scale, 0.14f, Vector3.zero);
        for (int i = 0; i < 5; i++)
        {
            Vector3 direction = Random.onUnitSphere;
            direction.y = Mathf.Abs(direction.y) * 0.6f;
            host.Emit(i % 2 == 0 ? host.fire : host.fireAlt, center + Random.insideUnitSphere * 0.35f * scale,
                Color.Lerp(color, hot, Random.value * 0.5f), Random.Range(0.9f, 1.9f) * scale,
                Random.Range(0.28f, 0.5f), direction * Random.Range(1.2f, 3.2f) * scale,
                Random.Range(0f, Mathf.PI * 2f));
        }
        host.Emit(host.ring, center, hot, 3.6f * scale, 0.3f, Vector3.zero);
        for (int i = 0; i < 2; i++)
            host.Emit(host.crackle, center + Random.insideUnitSphere * 0.4f, Color.Lerp(color, Color.white, 0.3f),
                Random.Range(0.7f, 1.2f) * scale, 0.22f, Vector3.zero, Random.Range(0f, Mathf.PI * 2f));
        for (int i = 0; i < 6; i++)
            host.Emit(host.shrapnel, center + Random.insideUnitSphere * 0.3f * scale, new Color(0.36f, 0.32f, 0.28f, 1f),
                Random.Range(0.12f, 0.3f) * scale, Random.Range(0.6f, 1.1f),
                Random.onUnitSphere * Random.Range(2f, 5.5f) * scale + Vector3.up * Random.Range(1.5f, 4f),
                Random.Range(0f, Mathf.PI * 2f));
        for (int i = 0; i < 4; i++)
            host.Emit(host.smokeTex, center + Random.insideUnitSphere * 0.4f * scale,
                new Color(0.16f, 0.17f, 0.18f, 0.34f), Random.Range(0.9f, 1.7f) * scale,
                Random.Range(0.7f, 1.2f), Random.insideUnitSphere * 0.8f + Vector3.up * 1.1f);
    }

    public static void MuzzleFlash(Vector3 position, Vector3 direction, Color color)
    {
        if (CombatLabSettings.MinimalFeedback) return;
        var host = Get();
        // Orient the muzzle sprite's up axis with the shot as seen on screen.
        float rotation = 0f;
        var camera = Camera.main;
        if (camera != null)
        {
            Vector2 screen = new Vector2(Vector3.Dot(direction, camera.transform.right),
                Vector3.Dot(direction, camera.transform.up));
            if (screen.sqrMagnitude > 0.0001f) rotation = -Mathf.Atan2(screen.x, screen.y);
        }
        host.Emit(host.muzzle, position, Color.Lerp(color, Color.white, 0.5f), 0.85f, 0.07f, Vector3.zero, rotation);
        host.Emit(host.glow, position, Color.Lerp(color, Color.white, 0.35f), 0.55f, 0.1f, Vector3.zero);
    }

    public static void ImpactBurst(Vector3 position, Color color)
    {
        if (CombatLabSettings.MinimalFeedback) return;
        var host = Get();
        host.Emit(host.glow, position, Color.Lerp(color, Color.white, 0.5f), 0.8f, 0.1f, Vector3.zero);
        for (int i = 0; i < 2; i++)
            host.Emit(host.fireAlt, position + Random.insideUnitSphere * 0.12f,
                Color.Lerp(color, Color.white, Random.value * 0.4f), Random.Range(0.35f, 0.6f),
                Random.Range(0.16f, 0.26f), Random.onUnitSphere * 1.6f, Random.Range(0f, Mathf.PI * 2f));
    }

    public static void BreakBurst(Vector3 center, Color color)
    {
        if (CombatLabSettings.MinimalFeedback) return;
        var host = Get();
        Color hot = Color.Lerp(color, Color.white, 0.45f);
        host.Emit(host.glow, center, hot, 2.2f, 0.16f, Vector3.zero);
        host.Emit(host.ring, center, hot, 3f, 0.26f, Vector3.zero);
        for (int i = 0; i < 3; i++)
            host.Emit(host.crackle, center + Random.insideUnitSphere * 0.5f, hot,
                Random.Range(0.8f, 1.4f), 0.24f, Vector3.zero, Random.Range(0f, Mathf.PI * 2f));
    }

    // Bullet-on-chassis contact: hot streaks, tumbling fragments and a tight flash.
    // Armored plates ring brighter and cooler; bare hulls burn warmer.
    public static void MetalHit(Vector3 point, Vector3 normal, bool heavy, bool armored)
    {
        if (CombatLabSettings.MinimalFeedback) return;
        var host = Get();
        normal = Vector3.ProjectOnPlane(normal, Vector3.up);
        if (normal.sqrMagnitude < 0.01f) normal = Vector3.up;
        Color sparkTint = armored ? new Color(0.85f, 0.92f, 1f) : new Color(1f, 0.82f, 0.45f);
        int sparks = armored ? (heavy ? 15 : 10) : (heavy ? 13 : 8);
        for (int i = 0; i < sparks; i++)
        {
            Vector3 velocity = normal * Random.Range(2.2f, heavy ? 7.5f : 5f)
                + Random.insideUnitSphere * 2.3f + Vector3.up * Random.Range(0f, 1.6f);
            host.Emit(host.hotSparks, point, Color.Lerp(sparkTint, Color.white, Random.value * 0.6f),
                Random.Range(0.05f, heavy ? 0.13f : 0.1f), Random.Range(0.2f, 0.42f), velocity);
        }
        int chunks = heavy ? 4 : 2;
        for (int i = 0; i < chunks; i++)
            host.Emit(host.shrapnel, point, new Color(0.34f, 0.31f, 0.27f, 1f),
                Random.Range(0.09f, 0.2f), Random.Range(0.45f, 0.8f),
                normal * Random.Range(1.5f, 3.4f) + Random.insideUnitSphere * 1.7f + Vector3.up * Random.Range(0.6f, 2.2f),
                Random.Range(0f, Mathf.PI * 2f));
        host.Emit(host.glow, point, Color.Lerp(sparkTint, Color.white, 0.65f), heavy ? 0.7f : 0.45f, 0.07f, Vector3.zero);
        if (heavy) host.Emit(host.ring, point, Color.Lerp(sparkTint, Color.white, 0.4f), 1.5f, 0.16f, Vector3.zero);
    }
}
