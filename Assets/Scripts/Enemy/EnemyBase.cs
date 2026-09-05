using System.Collections;
using UnityEngine;
using UnityEngine.AI;

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
    private NavMeshAgent navigation;
    private float nextPathTime;

    public float DifficultyHealthMultiplier { get; private set; } = 1f;

    private void Awake()
    {
        damageable = GetComponent<Damageable>();
        navigation = gameObject.AddComponent<NavMeshAgent>();
        navigation.radius = kind == EnemyKind.Elite ? 0.8f : 0.6f;
        navigation.height = 2.5f;
        navigation.updateRotation = false;
        navigation.acceleration = 20f;
        navigation.angularSpeed = 540f;
        navigation.stoppingDistance = 0.15f;
        navigation.obstacleAvoidanceType = ObstacleAvoidanceType.MedQualityObstacleAvoidance;
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
        if (navigation != null && navigation.isOnNavMesh) navigation.ResetPath();
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
        if (navigation != null && navigation.isOnNavMesh)
            navigation.isStopped = target == null || damageable.IsDead || (GameManager.Instance != null && !GameManager.Instance.IsCombatActive);
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
                StopMoving();
                StartCoroutine(DroneDetonation());
            }

            return;
        }

        if (distance > attackRange)
        {
            MoveWithSeparation(toTarget.normalized, 1f);
            return;
        }

        StopMoving();
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
        else StopMoving();

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

        Vector3 origin = transform.position + Vector3.up * 0.9f + direction.normalized * 0.75f;
        Color color = kind == EnemyKind.Elite ? new Color(1f, 0.25f, 0.42f) : new Color(1f, 0.35f, 0.08f);
        var projectile = ProjectilePool.Spawn(false, "EnemyProjectile", origin, color, 0.24f);
        ProjectileVisuals.SpawnMuzzleFlash(origin, color, 0.24f);
        projectile.Init(1, damageable, direction.normalized, damage, kind == EnemyKind.Elite ? 12f : 10f, 3f, 0f, 0);
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
        if (navigation == null || !navigation.isOnNavMesh) return;
        navigation.speed = moveSpeed * speedScale;
        if (Time.time < nextPathTime) return;
        nextPathTime = Time.time + 0.2f;
        bool approaching = Vector3.Dot(desiredDirection, target.position - transform.position) > 0f;
        Vector3 destination = approaching ? target.position : transform.position + desiredDirection * 3f;
        if (NavMesh.SamplePosition(destination, out var point, 3f, NavMesh.AllAreas)) navigation.SetDestination(point.position);
    }

    private void StopMoving() { if (navigation != null && navigation.isOnNavMesh) navigation.ResetPath(); }

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
