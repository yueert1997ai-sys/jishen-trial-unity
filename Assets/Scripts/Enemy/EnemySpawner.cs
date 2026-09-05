using System.Collections;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    public StageManager stageManager;
    public Transform player;
    public GameObject meleePrefab;
    public GameObject rangedPrefab;
    public GameObject dronePrefab;
    public GameObject elitePrefab;
    public GameObject bossPrefab;
    public float spawnRadius = 18f;

    public void SpawnGroup(EnemyKind kind, int count)
    {
        for (int i = 0; i < count; i++)
        {
            Vector2 ring = Random.insideUnitCircle.normalized * Random.Range(spawnRadius * 0.65f, spawnRadius);
            QueueEnemy(kind, new Vector3(ring.x, 0f, ring.y), i * 0.08f);
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
        return InstantiateEnemy(prefab, position);
    }

    public void CancelPendingSpawns()
    {
        StopAllCoroutines();
    }

    public BossController SpawnBoss()
    {
        if (bossPrefab == null)
        {
            return null;
        }

        GameObject bossObject = Instantiate(bossPrefab, new Vector3(0f, 0f, 18f), Quaternion.identity);
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

        StartCoroutine(SpawnAfterTelegraph(prefab, kind, position, stagger));
    }

    private IEnumerator SpawnAfterTelegraph(GameObject prefab, EnemyKind kind, Vector3 position, float stagger)
    {
        if (stagger > 0f)
        {
            yield return new WaitForSeconds(stagger);
        }

        float radius = kind == EnemyKind.Elite ? 1.35f : kind == EnemyKind.Drone ? 0.65f : 0.85f;
        CombatFeedback.SpawnWarningDisc(position, radius, 0.48f, GetSpawnColor(kind));
        yield return new WaitForSeconds(0.48f);
        InstantiateEnemy(prefab, position);
    }

    private EnemyBase InstantiateEnemy(GameObject prefab, Vector3 position)
    {
        GameObject enemyObject = Instantiate(prefab, position, Quaternion.identity);
        EnemyBase enemy = enemyObject.GetComponent<EnemyBase>();
        if (enemy != null)
        {
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
