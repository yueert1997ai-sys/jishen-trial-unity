using System.Collections;
using UnityEngine;

public enum EnemyKind
{
    Melee,
    Ranged,
    Drone,
    Elite
}

public class EnemyBase : MonoBehaviour
{
    public EnemyKind kind = EnemyKind.Melee;
    public Transform target;
    public StageManager stageManager;
    public float moveSpeed = 3.3f;
    public float attackRange = 1.35f;
    public float contactDamage = 12f;
    public float fireInterval = 1.25f;
    public int killReward = 5;

    private Damageable damageable;
    private float nextAttackTime;
    private bool detonating;

    public float DifficultyHealthMultiplier { get; private set; } = 1f;

    private void Awake()
    {
        damageable = GetComponent<Damageable>();
    }

    private void OnEnable()
    {
        if (damageable != null)
        {
            damageable.OnDied += OnDied;
        }
    }

    private void OnDisable()
    {
        if (damageable != null)
        {
            damageable.OnDied -= OnDied;
        }
    }

    public void Init(Transform targetTransform, StageManager ownerStage)
    {
        target = targetTransform;
        stageManager = ownerStage;
        if (damageable != null && GameManager.Instance != null)
        {
            DifficultyHealthMultiplier = GameManager.Instance.EnemyHealthMultiplier;
            damageable.SetMaxHealth(damageable.maxHealth * DifficultyHealthMultiplier, true);
        }
    }

    private void Update()
    {
        if (target == null || damageable == null || damageable.IsDead)
        {
            return;
        }

        if (GameManager.Instance != null && !GameManager.Instance.IsCombatActive)
        {
            return;
        }

        Vector3 toTarget = target.position - transform.position;
        toTarget.y = 0f;
        float distance = toTarget.magnitude;
        if (distance > 0.05f)
        {
            transform.rotation = Quaternion.LookRotation(toTarget.normalized, Vector3.up);
        }

        if (kind == EnemyKind.Ranged || kind == EnemyKind.Elite)
        {
            UpdateRanged(toTarget, distance);
        }
        else
        {
            UpdateMelee(toTarget, distance);
        }
    }

    private void UpdateMelee(Vector3 toTarget, float distance)
    {
        if (kind == EnemyKind.Drone)
        {
            if (detonating)
            {
                return;
            }

            if (distance > attackRange)
            {
                MoveWithSeparation(toTarget.normalized, 1f);
            }
            else
            {
                StartCoroutine(DroneDetonation());
            }

            return;
        }

        if (distance > attackRange)
        {
            MoveWithSeparation(toTarget.normalized, 1f);
            return;
        }

        if (Time.time >= nextAttackTime)
        {
            nextAttackTime = Time.time + 0.85f;
            Damageable targetDamageable = target.GetComponent<Damageable>();
            if (targetDamageable != null)
            {
                targetDamageable.TakeDamage(contactDamage, new DamageInfo(gameObject, transform.position, damageable, contactDamage));
            }

        }
    }

    private void UpdateRanged(Vector3 toTarget, float distance)
    {
        float desiredDistance = kind == EnemyKind.Elite ? 6.5f : 8.5f;
        if (distance > desiredDistance + 1f)
        {
            MoveWithSeparation(toTarget.normalized, 1f);
        }
        else if (distance < desiredDistance - 1f)
        {
            MoveWithSeparation(-toTarget.normalized, 0.7f);
        }

        if (Time.time >= nextAttackTime)
        {
            nextAttackTime = Time.time + fireInterval;
            FireAtPlayer(kind == EnemyKind.Elite ? 9f : 6f);
        }
    }

    private void FireAtPlayer(float damage)
    {
        Vector3 direction = (target.position - transform.position);
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.01f)
        {
            direction = transform.forward;
        }

        GameObject projectileObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        projectileObject.name = "EnemyProjectile";
        projectileObject.transform.position = transform.position + Vector3.up * 0.9f + direction.normalized * 0.75f;
        projectileObject.transform.localScale = Vector3.one * 0.24f;
        Renderer renderer = projectileObject.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = kind == EnemyKind.Elite ? Color.magenta : Color.red;
        }

        Color trailStart = kind == EnemyKind.Elite ? new Color(1f, 0.25f, 1f, 1f) : new Color(1f, 0.38f, 0.12f, 1f);
        ProjectileVisuals.AddTrail(projectileObject, trailStart, new Color(0.65f, 0.01f, 0.01f, 1f), 0.14f, 0.18f);
        ProjectileVisuals.SpawnMuzzleFlash(projectileObject.transform.position, trailStart, 0.24f);

        Projectile projectile = projectileObject.AddComponent<Projectile>();
        projectile.Init(1, damageable, direction.normalized, damage, kind == EnemyKind.Elite ? 17f : 14f, 3f, 0f, 0);
    }

    private void Explode()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, 2.2f);
        for (int i = 0; i < colliders.Length; i++)
        {
            Damageable targetDamageable = colliders[i].GetComponentInParent<Damageable>();
            if (targetDamageable == null || targetDamageable.team == 1)
            {
                continue;
            }

            targetDamageable.TakeDamage(16f, new DamageInfo(gameObject, transform.position, damageable, 16f));
        }

        damageable.Kill(new DamageInfo(gameObject, transform.position, damageable, 999f));
    }

    private IEnumerator DroneDetonation()
    {
        detonating = true;
        CombatFeedback.SpawnWarningDisc(transform.position, 2.2f, 0.55f, new Color(1f, 0.2f, 0.015f, 1f));
        GameAudio.Play(GameAudioCue.Warning, 0.34f, 1.2f);
        yield return new WaitForSeconds(0.55f);
        if (damageable != null && !damageable.IsDead)
        {
            Explode();
        }
    }

    private void MoveWithSeparation(Vector3 desiredDirection, float speedScale)
    {
        Vector3 separation = Vector3.zero;
        Collider[] nearby = Physics.OverlapSphere(transform.position, 1.3f);
        for (int i = 0; i < nearby.Length; i++)
        {
            Damageable other = nearby[i].GetComponentInParent<Damageable>();
            if (other == null || other == damageable || other.team != damageable.team)
            {
                continue;
            }

            Vector3 away = transform.position - other.transform.position;
            away.y = 0f;
            float distance = away.magnitude;
            if (distance > 0.01f)
            {
                separation += away.normalized * Mathf.Clamp01((1.3f - distance) / 1.3f);
            }
        }

        Vector3 steering = desiredDirection.normalized + separation * 0.9f;
        if (steering.sqrMagnitude > 0.01f)
        {
            transform.position += steering.normalized * moveSpeed * speedScale * Time.deltaTime;
        }
    }

    private void OnDied(Damageable dead)
    {
        if (stageManager != null)
        {
            stageManager.NotifyEnemyKilled();
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.RegisterKill(killReward);
        }
    }
}
