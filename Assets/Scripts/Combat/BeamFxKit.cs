using UnityEngine;
using UnityEngine.Rendering;

// Gundam-style textured layer for beam weapons: star glares, helix particle
// streams, converging charge motes and directional impact bursts. Sprites are
// CC0 from the Kenney Particle Pack (same provenance as EnemyVfx); systems and
// materials are runtime-authored so builds stay self-contained.
public sealed class BeamFxKit : MonoBehaviour
{
    static BeamFxKit instance;
    ParticleSystem glow, flare, muzzle, spiral, twinkle, sparks, fire, fireAlt, smoke, debris, ring;
    Material additive, smokeMaterial;

    static BeamFxKit Get()
    {
        if (instance == null) new GameObject("BeamFxKit").AddComponent<BeamFxKit>();
        return instance;
    }

    void Awake()
    {
        instance = this;
        additive = new Material(Shader.Find("MECH ROUGE/Particle Additive"));
        smokeMaterial = new Material(Shader.Find("Sprites/Default")) { mainTexture = Load("VFX/Sprites/smoke_05") };
        glow = Textured("BeamFxGlow", "VFX/Sprites/light_01", 128, additive);
        flare = Textured("BeamFxFlare", "VFX/Sprites/star_01", 32, additive);
        muzzle = Textured("BeamFxMuzzle", "VFX/Sprites/muzzle_02", 24, additive);
        spiral = Textured("BeamFxSpiral", "VFX/Sprites/spark_01", 384, additive);
        twinkle = Textured("BeamFxTwinkle", "VFX/Sprites/star_01", 160, additive);
        sparks = Textured("BeamFxSparks", "VFX/Sprites/light_01", 256, additive);
        ConfigureStreaks(sparks);
        fire = Textured("BeamFxFire", "VFX/Sprites/fire_01", 96, additive);
        fireAlt = Textured("BeamFxFireAlt", "VFX/Sprites/fire_02", 96, additive);
        smoke = Textured("BeamFxSmoke", "VFX/Sprites/smoke_05", 64, smokeMaterial);
        debris = Textured("BeamFxDebris", "VFX/Sprites/dirt_01", 96, smokeMaterial);
        ConfigureDebris(debris);
        ring = Textured("BeamFxRing", "VFX/Sprites/circle_03", 32, additive);
        // The ring system only ever emits expanding shockwaves.
        var size = ring.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1, AnimationCurve.EaseInOut(0, .12f, 1, 1f));
    }

    // White-hot streaks stretched along velocity: the readable energy spray.
    static void ConfigureStreaks(ParticleSystem ps)
    {
        var main = ps.main;
        main.gravityModifier = .8f;
        var color = ps.colorOverLifetime;
        color.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(new Color(1f, .82f, .55f), .6f), new GradientColorKey(Color.white, 1) },
            new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(.8f, .55f), new GradientAlphaKey(0, 1) });
        color.color = gradient;
        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Stretch;
        renderer.lengthScale = 1.4f;
        renderer.velocityScale = .11f;
    }

    // Dark tumbling fragments: mass reads through weight and spin, not brightness.
    static void ConfigureDebris(ParticleSystem ps)
    {
        var main = ps.main;
        main.gravityModifier = 1.15f;
        var spin = ps.rotationOverLifetime;
        spin.enabled = true;
        spin.z = Mathf.PI * .9f;
    }

    static Texture2D Load(string path)
    {
        var texture = Resources.Load<Texture2D>(path);
        if (texture == null) Debug.LogError("BeamFxKit missing sprite: " + path);
        return texture;
    }

    ParticleSystem Textured(string name, string texturePath, int limit, Material material)
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
            new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(.6f, .5f), new GradientAlphaKey(0, 1) });
        color.color = gradient;
        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        // Each system owns one texture: swap on the renderer's material instance.
        renderer.sharedMaterial = new Material(material) { mainTexture = Resources.Load<Texture2D>(texturePath) };
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        ps.Play();
        return ps;
    }

    void Emit(ParticleSystem system, Vector3 position, Color color, float size, float lifetime, Vector3 velocity, float rotation = 0f)
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

    void OnDestroy()
    {
        if (instance == this) instance = null;
        if (additive != null) Destroy(additive);
        if (smokeMaterial != null) Destroy(smokeMaterial);
        foreach (var ps in GetComponentsInChildren<ParticleSystem>(true))
            if (ps.TryGetComponent<ParticleSystemRenderer>(out var renderer) && renderer.sharedMaterial != null)
                Destroy(renderer.sharedMaterial);
    }

    // The three-layer glare from the reference: overexposed white core, star
    // rays and a wide diffuse halo. One call reads as a lens flare even though
    // every layer is just an additive sprite.
    public static void StarGlare(Vector3 position, Color color, float size, float life)
    {
        if (CombatLabSettings.MinimalFeedback) return;
        var host = Get();
        host.Emit(host.flare, position, Color.Lerp(color, Color.white, .75f), size, life * .7f, Vector3.zero, Random.value * Mathf.PI * 2f);
        host.Emit(host.flare, position, Color.Lerp(color, Color.white, .45f), size * .55f, life, Vector3.zero, Random.value * Mathf.PI * 2f);
        host.Emit(host.glow, position, Color.Lerp(color, Color.white, .2f), size * 1.6f, life * 1.25f, Vector3.zero);
    }

    // Beam-aligned muzzle report: sprite flash down the bore plus a full glare.
    public static void MuzzleBlast(Vector3 position, Vector3 direction, Color color, float scale)
    {
        if (CombatLabSettings.MinimalFeedback) return;
        var host = Get();
        // Orient the muzzle sprite's up axis with the shot as seen on screen.
        float rotation = 0f;
        var camera = Camera.main;
        if (camera != null)
        {
            Vector2 screen = new Vector2(Vector3.Dot(direction, camera.transform.right), Vector3.Dot(direction, camera.transform.up));
            if (screen.sqrMagnitude > .0001f) rotation = -Mathf.Atan2(screen.x, screen.y);
        }
        host.Emit(host.muzzle, position, Color.Lerp(color, Color.white, .5f), 1.15f * scale, .07f, direction * .3f, rotation);
        StarGlare(position, color, 2.1f * scale, .13f);
    }

    // Helix stream: particles ride a spiral around the beam axis while flowing
    // muzzle-to-impact, so the beam reads as compressed Minovsky particles
    // instead of a flat cylinder. `crackle` brightens a few motes toward yellow
    // for the stray electric flicker seen along anime beams.
    public static void BeamStream(Vector3 origin, Vector3 end, Color hot, Color cool, Color crackle, float radius, int count, float flow)
    {
        if (CombatLabSettings.MinimalFeedback) return;
        var host = Get();
        Vector3 axis = end - origin;
        float length = axis.magnitude;
        if (length < .01f) return;
        axis /= length;
        Vector3 right = Vector3.Cross(Vector3.up, axis);
        if (right.sqrMagnitude < .001f) right = Vector3.right; else right.Normalize();
        Vector3 up = Vector3.Cross(axis, right);
        float spin = Time.time * 9f;
        for (int i = 0; i < count; i++)
        {
            float t = Random.value;
            float angle = t * 22f + spin + Random.value * 6.28f;
            float taper = Mathf.Sin(t * Mathf.PI);
            float r = radius * (.45f + Random.value * .55f) * (.35f + .65f * taper);
            Vector3 offset = (Mathf.Cos(angle) * right + Mathf.Sin(angle) * up) * r;
            Vector3 tangent = (-Mathf.Sin(angle) * right + Mathf.Cos(angle) * up) * radius * 5.5f;
            Color color = Color.Lerp(hot, cool, Random.value);
            if (Random.value < .12f) color = Color.Lerp(color, crackle, .7f);
            host.Emit(host.spiral, origin + axis * (t * length) + offset, color,
                Random.Range(.03f, .075f), Random.Range(.16f, .28f), axis * flow + tangent + Random.insideUnitSphere * .4f);
        }
    }

    // Charge-up: motes spiral inward toward the muzzle, telegraphing the shot.
    public static void ChargeConverge(Vector3 center, Vector3 axis, Color hot, Color cool, int count, float radius)
    {
        if (CombatLabSettings.MinimalFeedback) return;
        var host = Get();
        Vector3 right = Vector3.Cross(Vector3.up, axis);
        if (right.sqrMagnitude < .001f) right = Vector3.right; else right.Normalize();
        Vector3 up = Vector3.Cross(axis, right);
        float spin = Time.time * 14f;
        for (int i = 0; i < count; i++)
        {
            float distance = radius * (.5f + Random.value * .8f);
            float angle = spin + Random.value * 6.28f;
            Vector3 offset = (Mathf.Cos(angle) * right + Mathf.Sin(angle) * up) * distance;
            Vector3 pull = -offset.normalized * (radius * 7f);
            Vector3 tangent = (-Mathf.Sin(angle) * right + Mathf.Cos(angle) * up) * (radius * 4.5f);
            host.Emit(host.spiral, center + offset + axis * Random.Range(-.25f, .25f),
                Color.Lerp(hot, cool, Random.value), Random.Range(.035f, .07f), Random.Range(.12f, .2f), pull + tangent);
        }
    }

    // Residual ionization: small star motes lingering along the fired path.
    public static void Twinkles(Vector3 origin, Vector3 end, Color color, int count)
    {
        if (CombatLabSettings.MinimalFeedback) return;
        var host = Get();
        for (int i = 0; i < count; i++)
            host.Emit(host.twinkle, Vector3.Lerp(origin, end, Random.value) + Random.insideUnitSphere * .3f,
                Color.Lerp(Color.white, color, Random.value * .7f), Random.Range(.05f, .13f),
                Random.Range(.3f, .55f), Random.insideUnitSphere * .5f, Random.value * 6.28f);
    }

    // Directional impact: the blast tears along the beam axis instead of
    // blooming into a uniform ball, matching the asymmetric explosions in the
    // reference stills. `scale` follows the weapon's blast radius.
    public static void ImpactBurst(Vector3 point, Vector3 direction, Color color, float scale)
    {
        if (CombatLabSettings.MinimalFeedback) return;
        var host = Get();
        Color hot = Color.Lerp(color, Color.white, .65f);
        StarGlare(point, hot, 2.3f * scale, .15f);
        host.Emit(host.ring, point, Color.Lerp(color, Color.white, .4f), 3.2f * scale, .28f, Vector3.zero);
        for (int i = 0; i < 3; i++)
            host.Emit(i % 2 == 0 ? host.fire : host.fireAlt, point + Random.insideUnitSphere * .14f * scale,
                Color.Lerp(color, hot, Random.value * .5f), Random.Range(.7f, 1.5f) * scale,
                Random.Range(.22f, .4f), direction * Random.Range(1.5f, 3.4f) + Random.insideUnitSphere * 1.5f,
                Random.Range(0f, Mathf.PI * 2f));
        int streaks = Mathf.CeilToInt(13 * scale);
        for (int i = 0; i < streaks; i++)
            host.Emit(host.sparks, point, Color.Lerp(color, Color.white, Random.value * .6f),
                Random.Range(.045f, .1f), Random.Range(.2f, .42f),
                direction * Random.Range(1.5f, 5f) + Random.onUnitSphere * 2.2f + Vector3.up * Random.Range(0f, 1.4f));
        for (int i = 0; i < 4; i++)
            host.Emit(host.debris, point, new Color(.34f, .31f, .27f, 1f), Random.Range(.09f, .19f) * scale,
                Random.Range(.45f, .85f), direction * Random.Range(.8f, 2.2f) + Random.onUnitSphere * 1.8f + Vector3.up * Random.Range(.5f, 2f));
        for (int i = 0; i < 2; i++)
            host.Emit(host.smoke, point + Random.insideUnitSphere * .2f * scale,
                new Color(.42f, .4f, .43f, .3f), Random.Range(.7f, 1.3f) * scale,
                Random.Range(.6f, 1f), Vector3.up * .9f + Random.insideUnitSphere * .5f);
    }
}
