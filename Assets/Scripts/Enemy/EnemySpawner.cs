using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemySpawner : MonoBehaviour
{
    public StageManager stageManager;
    public Transform player;
    public GameObject meleePrefab;
    public GameObject rangedPrefab;
    public GameObject dronePrefab;
    public GameObject elitePrefab;
    public GameObject bossPrefab;
    public GameObject liquidBossPrefab;
    public GameObject DefaultBossPrefab => liquidBossPrefab != null ? liquidBossPrefab : bossPrefab;
    public float spawnRadius = 18f;
    sealed class SpawnReservation
    {
        public int Generation;
        public TelegraphVisual Warning;
        public int WarningGeneration;
    }
    readonly List<SpawnReservation> pending = new List<SpawnReservation>();
    public int PendingSpawns => pending.Count;
    public int SpawnGeneration { get; private set; }

    public void SpawnGroup(EnemyKind kind, int count)
    {
        for (int i = 0; i < count; i++)
        {
            Vector2 ring = Random.insideUnitCircle.normalized * Random.Range(spawnRadius * 0.65f, spawnRadius);
            QueueEnemy(kind, new Vector3(ring.x, 0f, ring.y), i * 0.08f);
        }
    }

    public void SpawnEntry(EnemyKind kind, int count, int entry)
    {
        Vector3[] entries = { new Vector3(-21, 0, 0), new Vector3(0, 0, 21), new Vector3(21, 0, 0), new Vector3(0, 0, -21) };
        Vector3 origin = entries[Mathf.Abs(entry) % entries.Length];
        Vector3 tangent = Vector3.Cross(origin.normalized, Vector3.up);
        for (int i = 0; i < count; i++)
        {
            Vector3 position = origin + tangent * (i - (count - 1) * 0.5f) * 1.8f;
            if (player != null && Vector3.Distance(position, player.position) < 7f)
                position = -origin + tangent * i;
            if (NavMesh.SamplePosition(position, out var point, 4, NavMesh.AllAreas)) position = point.position;
            QueueEnemy(kind, position, i * 0.12f);
        }
    }

    public EnemyBase SpawnEnemy(EnemyKind kind, Vector3 position)
    {
        GameObject prefab = GetPrefab(kind);
        if (prefab == null)
        {
            return null;
        }

        if (stageManager != null)
        {
            stageManager.NotifyEnemySpawned();
        }

        CombatFeedback.SpawnWarningDisc(position, kind == EnemyKind.Elite ? 1.25f : 0.75f, 0.18f, GetSpawnColor(kind));
        return InstantiateEnemy(prefab, position, kind);
    }

    public void SpawnSliceEntry(EnemyKind kind, Vector3 position, float delay)
    {
        if (NavMesh.SamplePosition(position, out var point, 3f, NavMesh.AllAreas)) position = point.position;
        QueueEnemy(kind, position, delay);
    }

    public void CancelPendingSpawns()
    {
        SpawnGeneration++;
        StopAllCoroutines();
        foreach(var reservation in pending)
        {
            if(reservation.Warning!=null)reservation.Warning.Cancel(reservation.WarningGeneration);
            stageManager?.NotifyEnemyKilled(); // release only the reserved slot; this is not a gameplay kill
        }
        pending.Clear();
    }
    private void OnDisable() { CancelPendingSpawns(); }

    public BossController SpawnBoss(GameObject prefabOverride = null)
    {
        var prefab = prefabOverride != null ? prefabOverride : DefaultBossPrefab;
        if (prefab == null)
        {
            return null;
        }

        GameObject bossObject = Instantiate(prefab, new Vector3(0f, 0f, 18f), Quaternion.identity);
        BossController boss = bossObject.GetComponent<BossController>();
        if (boss != null)
        {
            boss.Init(player, stageManager, this);
        }

        if (stageManager != null)
        {
            stageManager.NotifyBossSpawned();
        }

        return boss;
    }

    private GameObject GetPrefab(EnemyKind kind)
    {
        switch (kind)
        {
            case EnemyKind.Ranged:
                return rangedPrefab;
            case EnemyKind.Drone:
                return dronePrefab;
            case EnemyKind.Elite:
                return elitePrefab;
            default:
                return meleePrefab;
        }
    }

    private void QueueEnemy(EnemyKind kind, Vector3 position, float stagger)
    {
        GameObject prefab = GetPrefab(kind);
        if (prefab == null)
        {
            return;
        }

        if (stageManager != null)
        {
            stageManager.NotifyEnemySpawned();
        }

        var reservation=new SpawnReservation { Generation=SpawnGeneration };
        pending.Add(reservation);
        StartCoroutine(SpawnAfterTelegraph(prefab, kind, position, stagger, reservation));
    }

    private IEnumerator SpawnAfterTelegraph(GameObject prefab, EnemyKind kind, Vector3 position, float stagger, SpawnReservation reservation)
    {
        if (stagger > 0f)
        {
            yield return new WaitForSeconds(stagger);
        }

        if(reservation.Generation!=SpawnGeneration)yield break;
        float radius = kind == EnemyKind.Elite ? 1.35f : kind == EnemyKind.Drone ? 0.65f : 0.85f;
        float warning=CombatRules.Current.SpawnWarning;
        reservation.Warning=CombatEffects.Disc(position, radius, warning, GetSpawnColor(kind));
        reservation.WarningGeneration=reservation.Warning.Generation;
        yield return new WaitForSeconds(warning);
        if(reservation.Generation!=SpawnGeneration)yield break;
        pending.Remove(reservation);
        if(GameManager.Instance!=null && !GameManager.Instance.IsCombatActive)
        {stageManager?.NotifyEnemyKilled();yield break;}
        InstantiateEnemy(prefab, position, kind);
    }

    private EnemyBase InstantiateEnemy(GameObject prefab, Vector3 position, EnemyKind kind)
    {
        if (NavMesh.SamplePosition(position, out var point, 5f, NavMesh.AllAreas)) position = point.position;
        GameObject enemyObject = Instantiate(prefab, position, Quaternion.identity);
        EnemyBase enemy = enemyObject.GetComponent<EnemyBase>();
        if (enemy != null)
        {
            enemy.ConfigureP0Role(kind);
            enemy.Init(player, stageManager);
        }

        return enemy;
    }

    private static Color GetSpawnColor(EnemyKind kind)
    {
        if (kind == EnemyKind.Elite)
        {
            return new Color(1f, 0.08f, 0.75f, 1f);
        }

        if (kind == EnemyKind.Drone)
        {
            return new Color(1f, 0.55f, 0.02f, 1f);
        }

        return new Color(1f, 0.14f, 0.04f, 1f);
    }
}
