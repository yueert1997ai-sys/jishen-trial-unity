using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.Rendering;

public class CombatEffects : MonoBehaviour
{
    private static CombatEffects instance;
    private ParticleSystem sparks, smoke;
    private Material material;
    private Material smokeMaterial;
    private Material fillMaterial;
    private Texture2D smokeTexture;
    private ObjectPool<TelegraphVisual> warnings;
    private ObjectPool<FloatingCombatText> numbers;
    private static CombatEffects Get()
    {
        if (instance == null) new GameObject("CombatEffects").AddComponent<CombatEffects>();
        return instance;
    }

    private void Awake()
    {
        instance = this;
        material = new Material(Shader.Find("Sprites/Default"));
        smokeTexture = new Texture2D(32, 32, TextureFormat.RGBA32, false);
        var pixels = new Color32[1024];
        for (int y = 0; y < 32; y++)
        for (int x = 0; x < 32; x++)
        {
            float radius = new Vector2((x - 15.5f) / 15.5f, (y - 15.5f) / 15.5f).magnitude;
            pixels[y * 32 + x] = new Color32(255, 255, 255, (byte)(Mathf.Pow(Mathf.Clamp01(1 - radius), 1.5f) * 255));
        }
        smokeTexture.SetPixels32(pixels);
        smokeTexture.Apply(false, true);
        smokeMaterial = new Material(material);
        smokeMaterial.mainTexture = smokeTexture;
        fillMaterial = new Material(Shader.Find("MECH ROUGE/Particle Additive"));
        fillMaterial.mainTexture = Resources.Load<Texture2D>("VFX/Sprites/circle_05");
        sparks = Particles("ArmorSparks", 2048, true);
        smoke = Particles("DebrisSmoke", 512, false);
        warnings = new ObjectPool<TelegraphVisual>(() =>
        {
            var go = new GameObject("CombatTelegraph");
            go.transform.SetParent(transform, false);
            var effect = go.AddComponent<TelegraphVisual>();
            effect.line = go.AddComponent<LineRenderer>();
            effect.line.sharedMaterial = material;
            effect.line.shadowCastingMode = ShadowCastingMode.Off;
            effect.line.receiveShadows = false;
            effect.line.numCapVertices = 4;
            effect.CreateFill(fillMaterial);
            effect.release = warnings.Release;
            return effect;
        }, v => v.gameObject.SetActive(true), v => v.gameObject.SetActive(false), v => Destroy(v.gameObject), true, 32, 128);
        numbers = new ObjectPool<FloatingCombatText>(() =>
        {
            var go = new GameObject("FloatingDamageText");
            go.transform.SetParent(transform, false);
            var number = go.AddComponent<FloatingCombatText>();
            number.release = numbers.Release;
            return number;
        }, n => n.gameObject.SetActive(true), n => n.gameObject.SetActive(false), n => Destroy(n.gameObject), true, 24, 100);
    }

    private ParticleSystem Particles(string name, int limit, bool stretch)
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
        main.gravityModifier = stretch ? 0.7f : -0.03f;
        var emission = ps.emission;
        emission.enabled = false;
        var shape = ps.shape;
        shape.enabled = false;
        var color = ps.colorOverLifetime;
        color.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) },
            new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(0.8f, 0.45f), new GradientAlphaKey(0, 1) });
        color.color = gradient;
        var size = ps.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1, stretch ? AnimationCurve.Linear(0, 1, 1, 0.15f) : AnimationCurve.Linear(0, 0.4f, 1, 1.8f));
        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = stretch ? material : smokeMaterial;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.renderMode = stretch ? ParticleSystemRenderMode.Stretch : ParticleSystemRenderMode.Billboard;
        renderer.lengthScale = 1.6f;
        renderer.velocityScale = 0.08f;
        ps.Play();
        return ps;
    }

    public static void Impact(Vector3 position, Color color, float scale, bool death = false)
    {
        if(CombatLabSettings.MinimalFeedback)return;
        var host = Get();
        int count = death ? 30 : scale > 0.8f ? 18 : 7;
        for (int i = 0; i < count; i++)
        {
            Vector3 direction = Random.onUnitSphere;
            direction.y = Mathf.Abs(direction.y) + 0.15f;
            host.sparks.Emit(new ParticleSystem.EmitParams
            {
                position = position, velocity = direction * Random.Range(2.5f, death ? 7f : 4.5f),
                startLifetime = Random.Range(0.18f, death ? 0.65f : 0.38f), startSize = Random.Range(0.035f, 0.1f),
                startColor = Color.Lerp(color, Color.white, Random.value * 0.75f)
            }, 1);
        }
        if (death || scale > 0.8f)
            for (int i = 0; i < 8; i++) host.smoke.Emit(new ParticleSystem.EmitParams
            {
                position = position + Random.insideUnitSphere * 0.4f, velocity = Random.insideUnitSphere * 1.2f + Vector3.up,
                startLifetime = Random.Range(0.6f, 1.1f), startSize = Random.Range(0.25f, 0.65f),
                startColor = new Color(0.18f, 0.22f, 0.23f, 0.25f), rotation = Random.Range(0, 360)
            }, 1);
    }

    public static void Number(Vector3 position, float amount, Color color)
    {
        var number = Get().numbers.Get();
        number.transform.position = position + Random.insideUnitSphere * 0.08f;
        number.Init(amount, color);
    }

    public static void Thrust(Vector3 position, Vector3 direction, bool dash)
    {
        Get().sparks.Emit(new ParticleSystem.EmitParams
        {
            position = position, velocity = direction.normalized * (dash ? 6f : 2f),
            startLifetime = dash ? 0.3f : 0.16f, startSize = dash ? 0.11f : 0.07f,
            startColor = new Color(0.25f, 0.92f, 1f)
        }, 2);
    }

    public static TelegraphVisual Disc(Vector3 position, float radius, float duration, Color color)
    {
        var warning = Get().warnings.Get();
        warning.Arm(duration, color);
        warning.ArmFill(position, radius);
        warning.line.loop = true;
        warning.line.positionCount = 48;
        warning.line.startWidth = warning.line.endWidth = 0.09f;
        for (int i = 0; i < 48; i++)
        {
            float angle = i * Mathf.PI * 2 / 48;
            warning.line.SetPosition(i, new Vector3(position.x + Mathf.Cos(angle) * radius, 0.07f, position.z + Mathf.Sin(angle) * radius));
        }
        return warning;
    }

    public static TelegraphVisual Line(Vector3 origin, Vector3 direction, float length, float width, float duration, Color color)
    {
        var warning = Get().warnings.Get();
        warning.Arm(duration, color);
        warning.line.loop = false;
        warning.line.positionCount = 2;
        warning.line.startWidth = warning.line.endWidth = width;
        origin.y = 0.075f;
        direction.y = 0;
        warning.line.SetPosition(0, origin);
        warning.line.SetPosition(1, origin + direction.normalized * length);
        return warning;
    }

    public static void ClearTelegraphs()
    {
        if (instance == null) return;
        foreach (var warning in instance.GetComponentsInChildren<TelegraphVisual>()) instance.warnings.Release(warning);
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
        if (material != null) Destroy(material);
        if (smokeMaterial != null) Destroy(smokeMaterial);
        if (fillMaterial != null) Destroy(fillMaterial);
        if (smokeTexture != null) Destroy(smokeTexture);
    }
}

public class TelegraphVisual : MonoBehaviour
{
    public LineRenderer line;
    public System.Action<TelegraphVisual> release;
    private Color color;
    private float lifetime, remaining;
    public int Generation {get;private set;}

    // Optional textured ground fill for disc telegraphs: the charge ramps up as
    // the windup completes so the danger area reads at a glance.
    private Transform fill;
    private MeshRenderer fillRenderer;
    private MaterialPropertyBlock fillBlock;
    private bool filled;

    public void CreateFill(Material material)
    {
        var mesh = new Mesh { name = "TelegraphFillQuad" };
        mesh.vertices = new[]
        {
            new Vector3(-.5f, 0f, -.5f), new Vector3(.5f, 0f, .5f),
            new Vector3(.5f, 0f, -.5f), new Vector3(-.5f, 0f, .5f),
        };
        mesh.uv = new[] { new Vector2(0, 0), new Vector2(1, 1), new Vector2(1, 0), new Vector2(0, 1) };
        mesh.triangles = new[] { 0, 1, 2, 0, 3, 1 };
        var go = new GameObject("Fill");
        go.transform.SetParent(transform, false);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        fillRenderer = go.AddComponent<MeshRenderer>();
        fillRenderer.sharedMaterial = material;
        fillRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        fillRenderer.receiveShadows = false;
        fillBlock = new MaterialPropertyBlock();
        go.SetActive(false);
        fill = go.transform;
    }

    public void Cancel(int generation){if(generation==Generation&&gameObject.activeSelf)release(this);}
    public void Arm(float duration, Color tint)
    {
        Generation++; lifetime = remaining = Mathf.Max(0.06f, duration); color = tint;
        filled = false;
        if (fill != null) fill.gameObject.SetActive(false);
    }
    public void ArmFill(Vector3 center, float radius)
    {
        filled = true;
        if (fill == null) return;
        fill.position = new Vector3(center.x, 0.05f, center.z);
        fill.rotation = Quaternion.identity;
        fill.localScale = new Vector3(radius * 2f, 1f, radius * 2f);
        fill.gameObject.SetActive(true);
    }
    private void Update()
    {
        remaining -= Time.deltaTime;
        Color tint = color;
        tint.a = Mathf.Lerp(1, 0.32f, remaining / lifetime);
        line.startColor = line.endColor = tint;
        if (filled && fillRenderer != null)
        {
            float progress = Mathf.Clamp01(1f - remaining / lifetime);
            Color charge = color;
            charge.a = 0.16f + 0.30f * progress + 0.06f * Mathf.Sin(progress * 26f);
            fillBlock.SetColor("_Tint", charge);
            fillRenderer.SetPropertyBlock(fillBlock);
        }
        if (remaining <= 0) release(this);
    }
}
