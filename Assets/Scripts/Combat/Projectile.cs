using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    public int team;
    public float damage = 10f, speed = 24f, lifetime = 2f, explosionRadius;
    public int pierceCount;
    public Vector3 direction = Vector3.forward;
    protected Damageable source;
    [System.NonSerialized] public ProjectilePool pool;
    private MaterialPropertyBlock visualBlock;
    public MaterialPropertyBlock VisualBlock => visualBlock ?? (visualBlock = new MaterialPropertyBlock());
    private readonly RaycastHit[] sweepHits = new RaycastHit[24];
    private readonly HashSet<Damageable> struck = new HashSet<Damageable>();
    private static readonly IComparer<RaycastHit> HitOrder = Comparer<RaycastHit>.Create((a, b) => a.distance.CompareTo(b.distance));
    private bool spent;

    private void Awake()
    {
        var collider = GetComponent<Collider>();
        if (collider == null) collider = gameObject.AddComponent<SphereCollider>();
        collider.isTrigger = true;
        var body = GetComponent<Rigidbody>();
        if (body == null) body = gameObject.AddComponent<Rigidbody>();
        body.isKinematic = true;
        body.useGravity = false;
    }

    public void Init(int sourceTeam, Damageable sourceDamageable, Vector3 shotDirection, float shotDamage, float shotSpeed, float shotLifetime, float shotExplosionRadius, int shotPierceCount)
    {
        team = sourceTeam;
        source = sourceDamageable;
        direction = shotDirection.sqrMagnitude > 0.01f ? shotDirection.normalized : Vector3.forward;
        damage = shotDamage;
        speed = shotSpeed;
        lifetime = shotLifetime;
        explosionRadius = shotExplosionRadius;
        pierceCount = shotPierceCount;
        spent = false;
        struck.Clear();
    }

    protected virtual void Update()
    {
        if (spent || (GameManager.Instance != null && !GameManager.Instance.IsCombatActive)) return;
        float distance = speed * Time.deltaTime;
        // Sweep the entire travelled segment: fast shots must not tunnel through cover or targets.
        int count = Physics.SphereCastNonAlloc(transform.position, 0.09f, direction, sweepHits, distance, ~0, QueryTriggerInteraction.Ignore);
        System.Array.Sort(sweepHits, 0, count, HitOrder);
        for (int i = 0; i < count && !spent; i++) Hit(sweepHits[i].collider, sweepHits[i].point);
        if (spent) return;
        transform.position += direction * distance;
        transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
        lifetime -= Time.deltaTime;
        if (lifetime <= 0f) Despawn();
    }

    private void OnTriggerEnter(Collider other) { if (!spent) Hit(other, transform.position); }

    private void Hit(Collider other, Vector3 point)
    {
        if (other == null || other.isTrigger || (GameManager.Instance != null && !GameManager.Instance.IsCombatActive)) return;
        var target = other.GetComponentInParent<Damageable>();
        if (target != null && (target.team == team || target.IsDead || struck.Contains(target))) return;
        if (target == null)
        {
            CombatFeedback.SpawnImpactPulse(point, new Color(1f, 0.72f, 0.22f), 0.2f);
            Despawn();
            return;
        }
        if (explosionRadius > 0.05f)
        {
            struck.Add(target);
            target.TakeDamage(damage, new DamageInfo(gameObject, point, source, damage));
            ApplyExplosionDamage(point);
            CombatFeedback.SpawnImpactPulse(point, new Color(1f, 0.65f, 0.15f), explosionRadius);
            Despawn();
            return;
        }
        struck.Add(target);
        target.TakeDamage(damage, new DamageInfo(gameObject, point, source, damage));
        if (spent) return;
        if (pierceCount-- > 0) return;
        Despawn();
    }

    protected void ApplyExplosionDamage(Vector3 center)
    {
        // Damageable registry avoids duplicate damage from multi-collider actors.
        for (int i = Damageable.Active.Count - 1; i >= 0; i--)
        {
            var target = Damageable.Active[i];
            if (target == null || target.team == team || target.IsDead || struck.Contains(target)) continue;
            float distance = Vector3.Distance(center, target.AimCenter);
            if (distance > explosionRadius) continue;
            struck.Add(target);
            float amount = damage * (0.45f + Mathf.Clamp01(1f - distance / explosionRadius) * 0.55f);
            target.TakeDamage(amount, new DamageInfo(gameObject, center, source, amount));
        }
    }

    public void Despawn()
    {
        if (spent) return;
        spent = true;
        if (pool != null) pool.Release(this);
        else { gameObject.SetActive(false); Destroy(gameObject); }
    }
}
