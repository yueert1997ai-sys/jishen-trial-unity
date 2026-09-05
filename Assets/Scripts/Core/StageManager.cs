using System.Collections;
using UnityEngine;

public class StageManager : MonoBehaviour
{
    public EnemySpawner enemySpawner;
    public Transform player;
    public float waveRepairFraction = 0.12f;

    private int enemiesAlive;
    private bool bossAlive;
    private Coroutine stageRoutine;

    public int EnemiesAlive
    {
        get { return enemiesAlive; }
    }

    public int WaveRepairsGranted { get; private set; }

    public void StartStage(int stageIndex)
    {
        if (stageRoutine != null)
        {
            StopCoroutine(stageRoutine);
        }

        if (enemySpawner != null)
        {
            enemySpawner.CancelPendingSpawns();
        }

        ClearEnemies();
        stageRoutine = StartCoroutine(RunStage(stageIndex));
    }

    public void NotifyEnemySpawned()
    {
        enemiesAlive++;
    }

    public void NotifyEnemyKilled()
    {
        enemiesAlive = Mathf.Max(0, enemiesAlive - 1);
    }

    public void NotifyBossSpawned()
    {
        bossAlive = true;
    }

    public void NotifyBossKilled()
    {
        bossAlive = false;
    }

    private IEnumerator RunStage(int stageIndex)
    {
        enemiesAlive = 0;
        bossAlive = false;
        WaveRepairsGranted = 0;
        int totalWaves = 3;

        for (int wave = 1; wave <= totalWaves; wave++)
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetProgress("Stage " + stageIndex + " - Wave " + wave + "/" + totalWaves);
            }

            GameAudio.Play(GameAudioCue.Wave, 0.34f, 1f + wave * 0.035f);

            SpawnWave(stageIndex, wave);
            while (enemiesAlive > 0)
            {
                yield return null;
            }

            ApplyWaveRepair();
            yield return new WaitForSeconds(1.1f);
        }

        if (stageIndex == 2)
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetProgress("Boss incoming");
            }

            GameAudio.Play(GameAudioCue.Warning, 0.5f, 0.82f);

            yield return new WaitForSeconds(1.2f);
            if (enemySpawner != null)
            {
                enemySpawner.SpawnBoss();
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetProgress("Boss - Phase 1");
            }

            while (bossAlive)
            {
                yield return null;
            }
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnStageCleared(stageIndex);
        }
    }

    private void ApplyWaveRepair()
    {
        PlayerStats stats = player != null ? player.GetComponent<PlayerStats>() : null;
        if (stats == null || stats.CurrentHp <= 0f || stats.CurrentHp >= stats.MaxHp)
        {
            return;
        }

        float before = stats.CurrentHp;
        float difficultyRepair = GameManager.Instance != null ? GameManager.Instance.WaveRepairMultiplier : 1f;
        stats.Heal(stats.MaxHp * Mathf.Clamp01(waveRepairFraction * difficultyRepair));
        int restored = Mathf.CeilToInt(stats.CurrentHp - before);
        if (restored <= 0)
        {
            return;
        }

        WaveRepairsGranted++;
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetProgress("Wave clear // Field repair +" + restored + " HP");
        }

        GameAudio.Play(GameAudioCue.Reward, 0.2f, 1.25f);
    }

    private void SpawnWave(int stageIndex, int wave)
    {
        if (enemySpawner == null)
        {
            return;
        }

        if (stageIndex == 1)
        {
            if (wave == 1)
            {
                enemySpawner.SpawnGroup(EnemyKind.Melee, 3);
            }
            else if (wave == 2)
            {
                enemySpawner.SpawnGroup(EnemyKind.Melee, 2);
                enemySpawner.SpawnGroup(EnemyKind.Ranged, 1);
            }
            else
            {
                enemySpawner.SpawnGroup(EnemyKind.Melee, 2);
                enemySpawner.SpawnGroup(EnemyKind.Ranged, 1);
                enemySpawner.SpawnGroup(EnemyKind.Drone, 1);
            }

            return;
        }

        if (wave == 1)
        {
            enemySpawner.SpawnGroup(EnemyKind.Melee, 3);
            enemySpawner.SpawnGroup(EnemyKind.Ranged, 1);
        }
        else if (wave == 2)
        {
            enemySpawner.SpawnGroup(EnemyKind.Ranged, 2);
            enemySpawner.SpawnGroup(EnemyKind.Drone, 2);
        }
        else
        {
            enemySpawner.SpawnGroup(EnemyKind.Elite, 1);
            enemySpawner.SpawnGroup(EnemyKind.Melee, 2);
        }
    }

    private void ClearEnemies()
    {
        Damageable[] damageables = FindObjectsByType<Damageable>(FindObjectsSortMode.None);
        for (int i = 0; i < damageables.Length; i++)
        {
            if (damageables[i] != null && damageables[i].team == 1)
            {
                Destroy(damageables[i].gameObject);
            }
        }

        enemiesAlive = 0;
        bossAlive = false;
    }
}
