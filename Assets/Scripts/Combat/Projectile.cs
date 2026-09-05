using UnityEngine;

public class Projectile : MonoBehaviour
{
    public int team;
    public float damage = 10f;
    public float speed = 24f;
    public float lifetime = 2f;
    public float explosionRadius;
    public int pierceCount;
    public Vector3 direction = Vector3.forward;

    protected Damageable source;

    private void Awake()
    {
        Collider[] colliders = GetComponents<Collider>();
        if (colliders.Length == 0)
        {
            gameObject.AddComponent<SphereCollider>();
            colliders = GetComponents<Collider>();
        }

        for (int i = 0; i < colliders.Length; i++)
        {
            colliders[i].isTrigger = true;
        }

        Rigidbody body = GetComponent<Rigidbody>();
        if (body == null)
        {
            body = gameObject.AddComponent<Rigidbody>();
        }

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
    }

    protected virtual void Update()
    {
        transform.position += direction * speed * Time.deltaTime;
        transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
        lifetime -= Time.deltaTime;
        if (lifetime <= 0f)
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        Damageable target = other.GetComponentInParent<Damageable>();
        if (target == null || target.team == team || target.IsDead)
        {
            return;
        }

        if (explosionRadius > 0.05f)
        {
            ApplyExplosionDamage(transform.position);
            Destroy(gameObject);
            return;
        }

        target.TakeDamage(damage, new DamageInfo(gameObject, transform.position, source, damage));
        if (pierceCount > 0)
        {
            pierceCount--;
            return;
        }

        Destroy(gameObject);
    }

    protected void ApplyExplosionDamage(Vector3 center)
    {
        Collider[] colliders = Physics.OverlapSphere(center, explosionRadius);
        for (int i = 0; i < colliders.Length; i++)
        {
            Damageable target = colliders[i].GetComponentInParent<Damageable>();
            if (target == null || target.team == team || target.IsDead)
            {
                continue;
            }

            float distance = Vector3.Distance(center, target.transform.position);
            float falloff = Mathf.Clamp01(1f - distance / Mathf.Max(0.1f, explosionRadius));
            target.TakeDamage(damage * (0.45f + falloff * 0.55f), new DamageInfo(gameObject, center, source, damage));
        }
    }
}
