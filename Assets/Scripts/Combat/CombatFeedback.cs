using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Damageable))]
public class CombatFeedback : MonoBehaviour
{
    public Color hitColor = Color.white;
    public float flashDuration = 0.08f;

    private Damageable damageable;
    private Renderer[] renderers;
    private Coroutine flashRoutine;
    private MaterialPropertyBlock flashBlock;

    private void Awake()
    {
        damageable = GetComponent<Damageable>();
        flashBlock = new MaterialPropertyBlock();
    }

    private void Start()
    {
        CacheRenderers();
    }

    private void OnEnable()
    {
        if (damageable == null)
        {
            damageable = GetComponent<Damageable>();
        }

        if (damageable != null)
        {
            damageable.OnDamaged += HandleDamaged;
            damageable.OnDied += HandleDied;
        }
    }

    private void OnDisable()
    {
        if (damageable != null)
        {
            damageable.OnDamaged -= HandleDamaged;
            damageable.OnDied -= HandleDied;
        }
    }

    private void HandleDamaged(Damageable target, DamageInfo info)
    {
        if (flashRoutine != null)
        {
            StopCoroutine(flashRoutine);
        }

        flashRoutine = StartCoroutine(Flash());
        Color pulseColor = target.team == 0 ? new Color(1f, 0.16f, 0.08f) : new Color(0.25f, 0.9f, 1f);
        SpawnImpactPulse(transform.position + Vector3.up * 0.8f, pulseColor, target.team == 0 ? 0.5f : 0.3f);
        SpawnDamageNumber(transform.position + Vector3.up * (target.team == 0 ? 2.2f : 1.75f), info.Amount, pulseColor);
        GameAudio.Play(GameAudioCue.Hit, target.team == 0 ? 0.42f : 0.2f, Random.Range(0.92f, 1.08f));

        if (target.team == 0)
        {
            CameraFollow cameraFollow = Camera.main != null ? Camera.main.GetComponent<CameraFollow>() : null;
            if (cameraFollow != null)
            {
                cameraFollow.AddShake(0.22f, 0.2f);
            }
        }
    }

    private void HandleDied(Damageable target)
    {
        Color burstColor = target.team == 0 ? new Color(0.1f, 0.75f, 1f) : new Color(1f, 0.18f, 0.06f);
        SpawnDeathBurst(transform.position + Vector3.up * 0.75f, burstColor, target.team == 0 ? 1.4f : 0.75f);
        GameAudio.Play(GameAudioCue.Death, target.team == 0 ? 0.65f : 0.28f, Random.Range(0.88f, 1.04f));

        CameraFollow cameraFollow = Camera.main != null ? Camera.main.GetComponent<CameraFollow>() : null;
        if (cameraFollow != null)
        {
            cameraFollow.AddShake(target.team == 0 ? 0.45f : 0.12f, target.team == 0 ? 0.45f : 0.12f);
        }
    }

    private IEnumerator Flash()
    {
        CacheRenderers();
        flashBlock.Clear();
        flashBlock.SetColor("_Color", hitColor);
        flashBlock.SetColor("_BaseColor", hitColor);
        flashBlock.SetColor("_EmissionColor", hitColor * 1.8f);

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
            {
                renderers[i].SetPropertyBlock(flashBlock);
            }
        }

        yield return new WaitForSeconds(flashDuration);

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
            {
                renderers[i].SetPropertyBlock(null);
            }
        }

        flashRoutine = null;
    }

    private void CacheRenderers()
    {
        if (renderers == null || renderers.Length == 0)
        {
            renderers = GetComponentsInChildren<Renderer>(true);
        }
    }

    public static void SpawnGroundLine(Vector3 origin, Vector3 direction, float length, float width, float duration, Color color)
    {
        Vector3 flatDirection = direction;
        flatDirection.y = 0f;
        if (flatDirection.sqrMagnitude < 0.01f)
        {
            flatDirection = Vector3.forward;
        }

        flatDirection.Normalize();
        GameObject line = GameObject.CreatePrimitive(PrimitiveType.Cube);
        line.name = "CombatGroundLine";
        line.transform.position = origin + flatDirection * (length * 0.5f) + Vector3.up * 0.035f;
        line.transform.rotation = Quaternion.LookRotation(flatDirection, Vector3.up);
        line.transform.localScale = new Vector3(width, 0.025f, length);
        DisableCollider(line);
        SetEffectMaterial(line, color);
        Object.Destroy(line, Mathf.Max(0.05f, duration));
    }

    public static void SpawnWarningDisc(Vector3 position, float radius, float duration, Color color)
    {
        GameObject disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        disc.name = "CombatWarningDisc";
        disc.transform.position = new Vector3(position.x, 0.045f, position.z);
        disc.transform.localScale = new Vector3(radius * 2f, 0.018f, radius * 2f);
        DisableCollider(disc);
        SetEffectMaterial(disc, color);
        Object.Destroy(disc, Mathf.Max(0.05f, duration));
    }

    public static void SpawnImpactPulse(Vector3 position, Color color, float scale)
    {
        GameObject pulse = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        pulse.name = "HitPulse";
        pulse.transform.position = position;
        pulse.transform.localScale = Vector3.one * scale;
        DisableCollider(pulse);
        SetEffectMaterial(pulse, color);
        Object.Destroy(pulse, 0.1f);
    }

    private static void SpawnDamageNumber(Vector3 position, float amount, Color color)
    {
        GameObject numberObject = new GameObject("FloatingDamageText");
        numberObject.transform.position = position + Random.insideUnitSphere * 0.12f;
        FloatingCombatText floatingText = numberObject.AddComponent<FloatingCombatText>();
        floatingText.Init(amount, color);
    }

    private static void SpawnDeathBurst(Vector3 position, Color color, float scale)
    {
        for (int i = 0; i < 6; i++)
        {
            GameObject shard = GameObject.CreatePrimitive(PrimitiveType.Cube);
            shard.name = "DeathBurstShard";
            Vector3 direction = Random.onUnitSphere;
            direction.y = Mathf.Abs(direction.y) + 0.2f;
            shard.transform.position = position + direction * scale * 0.55f;
            shard.transform.rotation = Random.rotation;
            shard.transform.localScale = Vector3.one * scale * Random.Range(0.12f, 0.24f);
            DisableCollider(shard);
            SetEffectMaterial(shard, color);
            Object.Destroy(shard, 0.22f);
        }
    }

    private static void DisableCollider(GameObject target)
    {
        Collider collider = target.GetComponent<Collider>();
        if (collider != null)
        {
            collider.enabled = false;
            Object.Destroy(collider);
        }
    }

    private static void SetEffectMaterial(GameObject target, Color color)
    {
        Renderer renderer = target.GetComponent<Renderer>();
        if (renderer == null)
        {
            return;
        }

        renderer.material.color = color;
        if (renderer.material.HasProperty("_EmissionColor"))
        {
            renderer.material.EnableKeyword("_EMISSION");
            renderer.material.SetColor("_EmissionColor", color * 1.6f);
        }
    }
}
