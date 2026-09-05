using System.Collections;
using UnityEngine;

public class BossController : MonoBehaviour
{
    public Transform target;
    public StageManager stageManager;
    public EnemySpawner spawner;
    public float moveSpeed = 2.2f;
    public int killReward = 60;

    private Damageable damageable;
    private float nextActionTime;
    private int actionIndex;
    private bool phaseTwoTriggered;
    private bool charging;

    public float DifficultyHealthMultiplier { get; private set; } = 1f;

    public bool IsPhaseTwo
    {
        get { return phaseTwoTriggered; }
    }

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

    public void Init(Transform targetTransform, StageManager ownerStage, EnemySpawner ownerSpawner)
    {
        target = targetTransform;
        stageManager = ownerStage;
        spawner = ownerSpawner;
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

        FaceTarget();
        if (!charging)
        {
            MoveTowardPreferredRange();
        }

        if (!phaseTwoTriggered && damageable.CurrentHealth <= damageable.maxHealth * 0.5f)
        {
            phaseTwoTriggered = true;
            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetProgress("Boss - Phase 2");
            }

            CombatFeedback.SpawnWarningDisc(transform.position, 4.2f, 0.75f, new Color(1f, 0.02f, 0.3f, 1f));
            GameAudio.Play(GameAudioCue.Warning, 0.58f, 0.72f);
            SummonMinions(4);
        }

        if (Time.time >= nextActionTime)
        {
            nextActionTime = Time.time + (phaseTwoTriggered ? 2.1f : 2.7f);
            RunNextAction();
        }
    }

    private void FaceTarget()
    {
        Vector3 direction = target.position - transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude > 0.01f)
        {
            transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        }
    }

    private void MoveTowardPreferredRange()
    {
        Vector3 direction = target.position - transform.position;
        direction.y = 0f;
        float distance = direction.magnitude;
        if (distance > 10f)
        {
            transform.position += direction.normalized * moveSpeed * Time.deltaTime;
        }
    }

    private void RunNextAction()
    {
        actionIndex++;
        int pattern = actionIndex % 4;
        if (pattern == 1)
        {
            StartCoroutine(TelegraphScatterBeams());
        }
        else if (pattern == 2)
        {
            StartCoroutine(TelegraphMissileVolley());
        }
        else if (pattern == 3)
        {
            StartCoroutine(Charge());
        }
        else
        {
            SummonMinions(phaseTwoTriggered ? 3 : 2);
        }
    }

    private IEnumerator TelegraphScatterBeams()
    {
        GameAudio.Play(GameAudioCue.Warning, 0.32f, 1.05f);
        Vector3 forward = transform.forward;
        int count = phaseTwoTriggered ? 9 : 7;
        for (int i = 0; i < count; i++)
        {
            float spread = Mathf.Lerp(-38f, 38f, i / (float)(count - 1));
            Vector3 direction = Quaternion.AngleAxis(spread, Vector3.up) * forward;
            CombatFeedback.SpawnGroundLine(transform.position, direction, 16f, 0.12f, 0.5f, new Color(1f, 0.16f, 0.03f));
        }

        yield return new WaitForSeconds(0.5f);
        FireScatterBeams();
    }

    private IEnumerator TelegraphMissileVolley()
    {
        GameAudio.Play(GameAudioCue.Warning, 0.35f, 0.9f);
        Vector3 targetPosition = target != null ? target.position : transform.position + transform.forward * 6f;
        CombatFeedback.SpawnWarningDisc(targetPosition, phaseTwoTriggered ? 2.4f : 1.9f, 0.65f, new Color(1f, 0.28f, 0.02f));
        yield return new WaitForSeconds(0.65f);
        FireMissileVolley();
    }

    private void FireScatterBeams()
    {
        Vector3 forward = transform.forward;
        int count = phaseTwoTriggered ? 9 : 7;
        for (int i = 0; i < count; i++)
        {
            float spread = Mathf.Lerp(-38f, 38f, i / (float)(count - 1));
            Vector3 direction = Quaternion.AngleAxis(spread, Vector3.up) * forward;
            CreateProjectile("BossScatterBeam", direction, 9f, 15f, 0f);
        }
    }

    private void FireMissileVolley()
    {
        Damageable playerDamageable = target.GetComponent<Damageable>();
        int count = phaseTwoTriggered ? 8 : 5;
        for (int i = 0; i < count; i++)
        {
            float spread = Mathf.Lerp(-55f, 55f, i / (float)(count - 1));
            Vector3 direction = Quaternion.AngleAxis(spread, Vector3.up) * transform.forward;
            GameObject missileObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            missileObject.name = "BossMissile";
            missileObject.transform.position = transform.position + Vector3.up * 1.3f + direction * 1.1f;
            missileObject.transform.localScale = new Vector3(0.28f, 0.28f, 0.65f);
            Renderer renderer = missileObject.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = new Color(1f, 0.18f, 0.05f);
            }

            ProjectileVisuals.AddTrail(missileObject, new Color(1f, 0.7f, 0.1f, 1f), new Color(0.9f, 0.02f, 0.01f, 1f), 0.23f, 0.32f);

            MissileProjectile missile = missileObject.AddComponent<MissileProjectile>();
            missile.target = playerDamageable;
            missile.Init(1, damageable, direction, 12f, 12f, 5f, phaseTwoTriggered ? 2.1f : 1.6f, 0);
        }
    }

    private IEnumerator Charge()
    {
        charging = true;
        Vector3 direction = target != null ? target.position - transform.position : transform.forward;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.01f)
        {
            direction = transform.forward;
        }

        direction.Normalize();
        GameAudio.Play(GameAudioCue.Warning, 0.48f, 0.76f);
        CombatFeedback.SpawnGroundLine(transform.position, direction, 18f, 1.35f, 0.6f, new Color(1f, 0.08f, 0.025f));
        yield return new WaitForSeconds(0.6f);

        Damageable playerDamageable = target != null ? target.GetComponent<Damageable>() : null;
        bool playerHit = false;
        float timer = 0.65f;
        while (timer > 0f)
        {
            transform.position += direction * 11f * Time.deltaTime;
            if (!playerHit && playerDamageable != null && !playerDamageable.IsDead && Vector3.Distance(transform.position, playerDamageable.transform.position) <= 2.8f)
            {
                playerDamageable.TakeDamage(18f, new DamageInfo(gameObject, transform.position, damageable, 18f));
                playerHit = true;
            }

            timer -= Time.deltaTime;
            yield return null;
        }

        if (!playerHit && playerDamageable != null && !playerDamageable.IsDead && Vector3.Distance(transform.position, playerDamageable.transform.position) <= 2.8f)
        {
            playerDamageable.TakeDamage(18f, new DamageInfo(gameObject, transform.position, damageable, 18f));
        }

        charging = false;
    }

    private void SummonMinions(int count)
    {
        if (spawner == null)
        {
            return;
        }

        for (int i = 0; i < count; i++)
        {
            Vector2 offset = Random.insideUnitCircle.normalized * Random.Range(4f, 8f);
            spawner.SpawnEnemy(i % 2 == 0 ? EnemyKind.Melee : EnemyKind.Ranged, transform.position + new Vector3(offset.x, 0f, offset.y));
        }
    }

    private void CreateProjectile(string projectileName, Vector3 direction, float damage, float speed, float explosionRadius)
    {
        GameObject projectileObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        projectileObject.name = projectileName;
        projectileObject.transform.position = transform.position + Vector3.up * 1.2f + direction.normalized * 1.2f;
        projectileObject.transform.localScale = Vector3.one * 0.3f;
        Renderer renderer = projectileObject.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = new Color(1f, 0.1f, 0.18f);
        }

        ProjectileVisuals.AddTrail(projectileObject, new Color(1f, 0.42f, 0.18f, 1f), new Color(0.75f, 0.01f, 0.08f, 1f), 0.2f, 0.22f);
        ProjectileVisuals.SpawnMuzzleFlash(projectileObject.transform.position, new Color(1f, 0.2f, 0.08f, 1f), 0.32f);

        Projectile projectile = projectileObject.AddComponent<Projectile>();
        projectile.Init(1, damageable, direction.normalized, damage, speed, 3.4f, explosionRadius, 0);
    }

    private void OnDied(Damageable dead)
    {
        if (stageManager != null)
        {
            stageManager.NotifyBossKilled();
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.RegisterKill(killReward);
        }
    }
}
