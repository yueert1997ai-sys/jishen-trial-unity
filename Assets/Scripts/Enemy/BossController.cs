using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public enum BossPattern { Scatter, Mortar, Charge, Reinforcements }

public class BossController : MonoBehaviour
{
    public Transform target;
    public StageManager stageManager;
    public EnemySpawner spawner;
    public float moveSpeed = 2.2f;
    public int killReward = 60;
    public float encounterHealth = 6200f;
    public bool mechDuel;
    public float armoredDamageScale = .12f;
    public string displayName = "REACTOR WARDEN";
    public Transform coreSocket;
    public float defeatDelay;
    public string DisplayName => mechDuel ? EquipmentWarehouseUI.T("苍钢卫士 · 精英首领", "STEEL VETERAN · ELITE") : GameText.T(displayName);
    public float DifficultyHealthMultiplier { get; private set; } = 1f;
    public bool IsPhaseTwo { get; private set; }
    public bool CoreExposed { get; private set; }
    public bool ActionRunning { get; private set; }
    public BossPattern CurrentPattern { get; private set; }
    public int ActionsCompleted { get; private set; }
    public Vector3 LockedOrigin { get; private set; }
    public Vector3 LockedDirection { get; private set; }
    public Vector3 LockedImpact { get; private set; }
    public float LockedLength { get; private set; }
    public float AttackWindup => winding ? Mathf.Clamp01((Time.time - windupStart) / windupDuration) : 0f;

    private Damageable damageable;
    private NavMeshAgent navigation;
    private readonly RaycastHit[] coverHits = new RaycastHit[32];
    private float nextActionTime, nextPathTime, windupStart, windupDuration;
    private bool winding;
    private int actionIndex;
    private static readonly Color warning = new Color(1f, 0.27f, 0.08f);

    private void Awake()
    {
        damageable = GetComponent<Damageable>();
        navigation = gameObject.AddComponent<NavMeshAgent>();
        navigation.radius = mechDuel ? .85f : 1.35f;
        navigation.height = GetComponent<E01ElitePoseDriver>() != null ? 4.8f : 3.5f;
        navigation.speed = moveSpeed;
        navigation.acceleration = 10;
        navigation.stoppingDistance = 9;
        navigation.updateRotation = false;
    }

    private void OnEnable() { damageable.OnDied += OnDied; }
    private void OnDisable()
    {
        StopAllCoroutines();
        winding = ActionRunning = false;
        damageable.OnDied -= OnDied;
        if (navigation != null && navigation.isOnNavMesh) navigation.ResetPath();
    }

    public void Init(Transform targetTransform, StageManager ownerStage, EnemySpawner ownerSpawner)
    {
        target = targetTransform;
        stageManager = ownerStage;
        spawner = ownerSpawner;
        DifficultyHealthMultiplier = GameManager.Instance != null ? GameManager.Instance.EnemyHealthMultiplier : 1f;
        damageable.SetMaxHealth(encounterHealth * DifficultyHealthMultiplier, true);
        damageable.IncomingDamageScale = armoredDamageScale;
        nextActionTime = Time.time + 1.8f;
    }

    private void Update()
    {
        bool active = target != null && !damageable.IsDead && (GameManager.Instance == null || GameManager.Instance.IsCombatActive);
        if (navigation.isOnNavMesh) navigation.isStopped = !active || ActionRunning;
        if (!active) return;
        if (!IsPhaseTwo && damageable.CurrentHealth <= damageable.maxHealth * 0.5f)
        {
            IsPhaseTwo = true;
            GameAudio.Play(GameAudioCue.Warning, 0.55f, 0.72f);
            SetStatus("PHASE 2");
        }
        if (ActionRunning) return;
        Vector3 direction = target.position - transform.position;
        direction.y = 0;
        if (direction.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(direction);
        if (navigation.isOnNavMesh && Time.time >= nextPathTime)
        {
            nextPathTime = Time.time + 0.25f;
            navigation.SetDestination(target.position);
        }
        if (Time.time >= nextActionTime) StartPattern((BossPattern)(actionIndex++ % 4));
    }

    public bool StartPattern(BossPattern pattern)
    {
        if (ActionRunning || target == null || damageable.IsDead || (GameManager.Instance != null && !GameManager.Instance.IsCombatActive)) return false;
        ActionRunning = true;
        CurrentPattern = pattern;
        if (navigation.isOnNavMesh) navigation.ResetPath();
        StartCoroutine(ActionCycle(pattern));
        return true;
    }

    private IEnumerator ActionCycle(BossPattern pattern)
    {
        CoreExposed = false;
        damageable.IncomingDamageScale = armoredDamageScale;
        switch (pattern)
        {
            case BossPattern.Scatter: yield return Scatter(); break;
            case BossPattern.Mortar: yield return Mortar(); break;
            case BossPattern.Charge: yield return Charge(); break;
            default:
                SetStatus("REINFORCEMENTS");
                if (spawner != null && (stageManager == null || stageManager.EnemiesAlive < 5))
                {
                    spawner.SpawnEntry(EnemyKind.Melee, IsPhaseTwo ? 3 : 2, actionIndex % 4);
                    spawner.SpawnEntry(EnemyKind.Ranged, 1, (actionIndex + 2) % 4);
                }
                yield return new WaitForSeconds(1.2f);
                break;
        }
        winding = false;
        CoreExposed = true;
        damageable.IncomingDamageScale = 1f;
        SetStatus("CORE EXPOSED");
        CombatEffects.Impact(coreSocket != null ? coreSocket.position : transform.position + Vector3.up * 2,
            coreSocket != null ? new Color(1f, .035f, .015f) : Color.cyan, 0.6f);
        yield return new WaitForSeconds(IsPhaseTwo ? 2f : 2.5f);
        CoreExposed = false;
        damageable.IncomingDamageScale = armoredDamageScale;
        ActionsCompleted++;
        ActionRunning = false;
        nextActionTime = Time.time + (IsPhaseTwo ? 0.5f : 0.8f);
        SetStatus(IsPhaseTwo ? "PHASE 2" : "PHASE 1");
    }

    private IEnumerator Scatter()
    {
        LockDirection();
        LockedOrigin = transform.position + Vector3.up * 1.2f + LockedDirection * 1.6f;
        if (mechDuel) LockedOrigin = GetComponent<MechRivalPresentation>().Muzzle.position;
        Vector3 volleyDirection = mechDuel ? (target.GetComponent<Damageable>().AimCenter - LockedOrigin).normalized : LockedDirection;
        LockedLength = 18f;
        int count = IsPhaseTwo ? 9 : 7;
        SetStatus("FAN VOLLEY");
        BeginWindup(0.85f);
        for (int i = 0; i < count; i++)
        {
            Vector3 direction = Quaternion.AngleAxis(Mathf.Lerp(-48, 48, i / (float)(count - 1)), Vector3.up) * volleyDirection;
            CombatEffects.Line(LockedOrigin, direction, LockedLength, 0.25f, windupDuration, warning);
        }
        yield return new WaitForSeconds(windupDuration);
        winding = false;
        int bursts = mechDuel ? (IsPhaseTwo ? 2 : 1) : IsPhaseTwo ? 4 : 3;
        for (int burst = 0; burst < bursts; burst++)
        {
            for (int i = 0; i < count; i++)
            {
                Vector3 direction = Quaternion.AngleAxis(Mathf.Lerp(-48, 48, i / (float)(count - 1)), Vector3.up) * volleyDirection;
                var shot = ProjectilePool.Spawn(false, "BossScatterBeam", LockedOrigin, warning, 0.25f);
                shot.Init(1, damageable, direction, 11f, 12f, LockedLength / 12f, 0, 0);
            }
            GameAudio.Play(GameAudioCue.Beam, 0.35f, 0.7f);
            yield return new WaitForSeconds(0.52f);
        }
    }

    private IEnumerator Mortar()
    {
        LockedImpact = target.position;
        LockedImpact = new Vector3(LockedImpact.x, 0, LockedImpact.z);
        int count = IsPhaseTwo ? 3 : 2;
        Vector3[] impacts = new Vector3[count];
        impacts[0] = LockedImpact;
        for (int i = 1; i < count; i++) impacts[i] = LockedImpact + Vector3.right * (i == 1 ? -4.5f : 4.5f);
        SetStatus("ARTILLERY STRIKE");
        BeginWindup(1.05f);
        for (int i = 0; i < impacts.Length; i++) CombatEffects.Disc(impacts[i], 2.4f, windupDuration + i * 0.6f, warning);
        yield return new WaitForSeconds(windupDuration);
        winding = false;
        foreach (var center in impacts)
        {
            DamageDisc(center, 2.4f, 22f);
            CombatEffects.Impact(center + Vector3.up * 0.5f, warning, 2.4f, true);
            GameAudio.Play(GameAudioCue.Death, 0.45f, 0.85f);
            yield return new WaitForSeconds(0.6f);
        }
    }

    private IEnumerator Charge()
    {
        LockDirection();
        LockedOrigin = transform.position;
        LockedLength = 13f;
        if (NavMesh.Raycast(LockedOrigin, LockedOrigin + LockedDirection * LockedLength, out var edge, NavMesh.AllAreas))
            LockedLength = Mathf.Max(0, edge.distance - 0.8f);
        int count = Physics.SphereCastNonAlloc(LockedOrigin + Vector3.up * 1.4f, 1.25f, LockedDirection, coverHits, LockedLength, ~0, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < count; i++)
            if (coverHits[i].collider.GetComponentInParent<Damageable>() == null)
                LockedLength = Mathf.Min(LockedLength, Mathf.Max(0, coverHits[i].distance - 0.15f));
        SetStatus("RAM CHARGE");
        BeginWindup(0.95f);
        CombatEffects.Line(LockedOrigin, LockedDirection, LockedLength, 4f, windupDuration, warning);
        CombatEffects.Disc(LockedOrigin, 2, windupDuration, warning);
        CombatEffects.Disc(LockedOrigin + LockedDirection * LockedLength, 2, windupDuration, warning);
        yield return new WaitForSeconds(windupDuration);
        winding = false;
        bool hit = false;
        float traveled = 0;
        while (traveled < LockedLength)
        {
            Vector3 before = transform.position;
            traveled = Mathf.Min(LockedLength, traveled + 17f * Time.deltaTime);
            Vector3 next = LockedOrigin + LockedDirection * traveled;
            if (navigation.isOnNavMesh) navigation.Warp(next);
            else transform.position = next;
            if (!hit && InsideSweptDisc(target.position, before, next, 2f))
            {
                target.GetComponent<Damageable>().TakeDamage(24f, new DamageInfo(gameObject, before, damageable, 24f));
                hit = true;
            }
            CombatEffects.Thrust(transform.position + Vector3.up, -LockedDirection, true);
            yield return null;
        }
        Vector3 landing = transform.position;
        CombatEffects.Disc(landing, 2.8f, 0.7f, warning);
        if (mechDuel)
        {
            yield return new WaitForSeconds(.45f);
            GetComponent<MechRivalPresentation>().PlaySlash();
            yield return new WaitForSeconds(.25f);
        }
        else yield return new WaitForSeconds(0.7f);
        DamageDisc(landing, 2.8f, 18f);
        CombatEffects.Impact(landing + Vector3.up, warning, 2.8f, true);
    }

    public static bool InsideSweptDisc(Vector3 point, Vector3 start, Vector3 end, float radius)
    {
        point.y = start.y = end.y = 0;
        Vector3 segment = end - start;
        float t = segment.sqrMagnitude > 0.001f ? Mathf.Clamp01(Vector3.Dot(point - start, segment) / segment.sqrMagnitude) : 0;
        return (point - start - segment * t).sqrMagnitude <= radius * radius;
    }

    private void DamageDisc(Vector3 center, float radius, float amount)
    {
        var actors = Damageable.Active;
        for (int i = actors.Count - 1; i >= 0; i--)
        {
            var actor = actors[i];
            if (actor.team != 0 || actor.IsDead || !InsideSweptDisc(actor.transform.position, center, center, radius)) continue;
            actor.TakeDamage(amount, new DamageInfo(gameObject, transform.position, damageable, amount));
        }
    }

    private void LockDirection()
    {
        Vector3 direction = target.position - transform.position;
        direction.y = 0;
        LockedDirection = direction.sqrMagnitude > 0.01f ? direction.normalized : transform.forward;
        transform.rotation = Quaternion.LookRotation(LockedDirection);
    }

    private void BeginWindup(float duration)
    {
        winding = true;
        windupStart = Time.time;
        windupDuration = duration;
        GameAudio.Play(GameAudioCue.Warning, 0.4f, IsPhaseTwo ? 0.85f : 1f);
    }

    private void SetStatus(string action)
    {
        if (GameManager.Instance != null) GameManager.Instance.SetProgress(DisplayName + "  /  " + GameText.T(action));
    }

    private void OnDied(Damageable dead)
    {
        StopAllCoroutines();
        winding = ActionRunning = false;
        if (defeatDelay > 0 && !dead.destroyOnDeath) StartCoroutine(FinishDefeat());
        else CompleteDefeat();
    }

    private IEnumerator FinishDefeat()
    {
        float elapsed = 0;
        var pose = GetComponent<E01ElitePoseDriver>();
        if (pose != null) pose.enabled = false;
        var root = pose != null ? pose.rigRoot : null;
        var rotation = root != null ? root.localRotation : Quaternion.identity;
        while (elapsed < defeatDelay)
        {
            elapsed += Time.deltaTime;
            if (root != null) root.localRotation = rotation * Quaternion.Euler(Mathf.SmoothStep(0, 28, elapsed / defeatDelay), 0, 9 * elapsed / defeatDelay);
            yield return null;
        }
        CompleteDefeat();
        Destroy(gameObject);
    }

    private void CompleteDefeat()
    {
        if (stageManager != null) stageManager.NotifyBossKilled();
        if (GameManager.Instance != null) GameManager.Instance.RegisterKill(killReward);
    }
}
