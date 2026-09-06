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
    public Transform rifleMuzzle;
    public bool TrainingTarget { get; set; }

    private Damageable damageable;
    private float nextAttackTime;
    private bool detonating;
    private NavMeshAgent navigation;
    private float nextPathTime;
    private bool attacking;
    private float attackStarted, attackDuration;
    public float AttackWindup => attacking ? Mathf.Clamp01((Time.time - attackStarted) / attackDuration) : 0f;

    public float DifficultyHealthMultiplier { get; private set; } = 1f;

    private void Awake()
    {
        damageable = GetComponent<Damageable>();
        navigation = gameObject.AddComponent<NavMeshAgent>();
        navigation.radius = kind == EnemyKind.Elite ? 0.8f : 0.6f;
        navigation.height = rifleMuzzle != null ? 2.8f : 2.5f;
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
        StopAllCoroutines();
        attacking = false;
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
        if (GameManager.Instance != null && GameManager.Instance.equipmentLoop != null) GameManager.Instance.equipmentLoop.AttachCarrier(this);
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

        if (attacking) return;

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
            nextAttackTime = Time.time + 1.2f;
            StartCoroutine(MeleeStrike());
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

        if (distance <= 15f && Time.time >= nextAttackTime)
        {
            nextAttackTime = Time.time + Mathf.Max(1.6f, fireInterval);
            StopMoving();
            StartCoroutine(RangedStrike());
        }
    }

    private IEnumerator MeleeStrike()
    {
        BeginWindup(0.38f);
        Vector3 center = transform.position;
        float radius = Mathf.Max(1.7f, attackRange + 0.2f);
        CombatEffects.Disc(center, radius, attackDuration, new Color(1f, 0.32f, 0.1f));
        yield return new WaitForSeconds(attackDuration);
        Vector3 delta = target.position - center;
        delta.y = 0;
        if (!damageable.IsDead && delta.sqrMagnitude <= radius * radius)
            target.GetComponent<Damageable>().TakeDamage(contactDamage, new DamageInfo(gameObject, center, damageable, contactDamage));
        CombatEffects.Impact(center + transform.forward, new Color(1f, 0.6f, 0.2f), 0.45f);
        attacking = false;
    }

    private IEnumerator RangedStrike()
    {
        Vector3 direction = (target.position - transform.position);
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.01f)
        {
            direction = transform.forward;
        }

        Vector3 origin = transform.position + Vector3.up * 0.9f + direction.normalized * 0.75f;
        Color color = kind == EnemyKind.Elite ? new Color(1f, 0.25f, 0.42f) : new Color(1f, 0.35f, 0.08f);
        BeginWindup(0.55f);
        CombatEffects.Line(origin, direction, 18f, 0.24f, attackDuration, color);
        yield return new WaitForSeconds(attackDuration);
        if (damageable.IsDead) yield break;
        if(rifleMuzzle!=null)
        {
            origin=rifleMuzzle.position;
            direction=target.GetComponent<Damageable>().AimCenter-origin;
            GetComponent<E01SoldierMotion>()?.Recoil();
        }
        float damage = kind == EnemyKind.Elite ? 9f : 6f;
        ProjectileVisuals.SpawnMuzzleFlash(origin, color, 0.24f);
        var carrier = GetComponent<SalvageCarrier>();
        bool scatter = carrier != null && carrier.gear.id == "scatter";
        bool swarm = carrier != null && carrier.gear.id == "salvo";
        int shots = scatter ? 5 : swarm ? 4 : 1;
        for (int i = 0; i < shots; i++)
        {
            float angle = shots == 1 ? 0 : Mathf.Lerp(-18, 18, i / (float)(shots - 1));
            var projectile = ProjectilePool.Spawn(false, "EnemyProjectile", origin, color, .24f);
            projectile.Init(1, damageable, Quaternion.AngleAxis(angle, Vector3.up) * direction.normalized,
                damage * (shots > 1 ? .55f : 1f), kind == EnemyKind.Elite ? 12f : 10f, 3f, 0f, 0);
        }
        attacking = false;
    }

    private void BeginWindup(float duration)
    {
        attacking = true;
        attackStarted = Time.time;
        attackDuration = duration;
    }

    private void Explode()
    {
        var actors = Damageable.Active;
        for (int i = actors.Count - 1; i >= 0; i--)
        {
            Damageable targetDamageable = actors[i];
            if (targetDamageable == null || targetDamageable.team == 1 || targetDamageable.IsDead)
            {
                continue;
            }

            Vector3 delta = targetDamageable.transform.position - transform.position;
            delta.y = 0;
            if (delta.sqrMagnitude <= 2.2f * 2.2f)
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
        if(TrainingTarget)return;
        foreach(var collider in GetComponents<Collider>())collider.enabled=false;
        if (GameManager.Instance != null && GameManager.Instance.equipmentLoop != null) GameManager.Instance.equipmentLoop.DropFrom(this);
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
