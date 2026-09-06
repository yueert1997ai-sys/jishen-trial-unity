using System.Collections;
using UnityEngine;

public class StageManager : MonoBehaviour
{
    public EnemySpawner enemySpawner;
    public Transform player;
    public float waveRepairFraction = 0.12f;
    public EncounterDefinition maintenanceEncounter;
    public EncounterDefinition[] encounters = new EncounterDefinition[6];
    public float FirstEncounterSeconds { get; private set; }
    public int CurrentEncounter { get; private set; }
    public int CurrentStage => Mathf.Min(2, CurrentEncounter / 3 + 1);
    public int CurrentWave => CurrentEncounter % 3 + 1;
    public int EnemiesAlive => enemiesAlive;
    public int WaveRepairsGranted { get; private set; }
    public float[] EncounterSeconds { get; } = new float[6];

    private int enemiesAlive;
    private bool bossAlive;
    private Coroutine stageRoutine;

    public void StartStage(int stageIndex) { StartEncounter((stageIndex - 1) * 3); }

    public void StartEncounter(int index, GameObject bossOverride = null)
    {
        StopStage();
        CurrentEncounter = Mathf.Clamp(index, 0, 6);
        if (index == 0)
        {
            WaveRepairsGranted = 0;
            System.Array.Clear(EncounterSeconds, 0, EncounterSeconds.Length);
        }
        stageRoutine = StartCoroutine(index == 6 ? RunBoss(bossOverride) : RunEncounterStage(index));
    }

    public void NotifyEnemySpawned() { enemiesAlive++; }
    public void NotifyEnemyKilled() { enemiesAlive = Mathf.Max(0, enemiesAlive - 1); }
    public void NotifyBossSpawned() { bossAlive = true; }
    public void NotifyBossKilled() { bossAlive = false; }

    public void StopStage()
    {
        StopAllCoroutines();
        stageRoutine = null;
        if (enemySpawner != null) enemySpawner.CancelPendingSpawns();
        foreach (var enemy in FindObjectsByType<EnemyBase>(FindObjectsSortMode.None)) enemy.StopAllCoroutines();
        foreach (var boss in FindObjectsByType<BossController>(FindObjectsSortMode.None)) boss.StopAllCoroutines();
        ClearEnemies();
    }

    private IEnumerator RunEncounterStage(int index)
    {
        float started = Time.time;
        var encounter = encounters != null && index < encounters.Length ? encounters[index] : null;
        if (encounter == null && index == 0) encounter = maintenanceEncounter;
        var gm = GameManager.Instance;
        gm.SetProgress("SECTOR " + CurrentStage + "  /  ENCOUNTER " + CurrentWave + " OF 3");
        GameAudio.Play(GameAudioCue.Wave, 0.34f, 1f + CurrentWave * 0.035f);
        if (encounter != null) yield return RunEncounter(encounter);
        else
        {
            enemySpawner.SpawnGroup(EnemyKind.Melee, 3 + index);
            if (index > 0) enemySpawner.SpawnGroup(index % 2 == 0 ? EnemyKind.Ranged : EnemyKind.Drone, 2);
        }
        while (enemiesAlive > 0) yield return null;
        EncounterSeconds[index] = Time.time - started;
        if (index == 0) FirstEncounterSeconds = EncounterSeconds[index];
        ApplyWaveRepair();
        yield return new WaitForSeconds(0.8f);
        stageRoutine = null;
        gm.OnEncounterCleared(index);
    }

    private IEnumerator RunBoss(GameObject bossOverride)
    {
        var gm = GameManager.Instance;
        var prefab = bossOverride != null ? bossOverride : enemySpawner.DefaultBossPrefab;
        var bossDefinition = prefab != null ? prefab.GetComponent<BossController>() : null;
        gm.SetProgress((bossDefinition != null ? bossDefinition.DisplayName : GameText.T("REACTOR WARDEN")) + "  /  " + GameText.T("INCOMING"));
        GameAudio.Play(GameAudioCue.Warning, 0.5f, 0.82f);
        yield return new WaitForSeconds(1.4f);
        var boss = enemySpawner.SpawnBoss(prefab);
        if (boss == null)
        {
            Debug.LogError("Boss prefab is missing; cannot complete the mission.");
            yield break;
        }
        gm.SetProgress(boss.DisplayName + "  /  " + GameText.T("PHASE 1"));
        while (bossAlive) yield return null;
        stageRoutine = null;
        gm.OnStageCleared(2);
    }

    private void ApplyWaveRepair()
    {
        PlayerStats stats = player != null ? player.GetComponent<PlayerStats>() : null;
        if (stats == null || stats.CurrentHp <= 0f || stats.CurrentHp >= stats.MaxHp) return;
        float before = stats.CurrentHp;
        float repair = GameManager.Instance != null ? GameManager.Instance.WaveRepairMultiplier : 1f;
        stats.Heal(stats.MaxHp * Mathf.Clamp01(waveRepairFraction * repair));
        int restored = Mathf.CeilToInt(stats.CurrentHp - before);
        if (restored <= 0) return;
        WaveRepairsGranted++;
        GameManager.Instance.SetProgress("ENCOUNTER CLEAR  /  REPAIR +" + restored + " HP");
        GameAudio.Play(GameAudioCue.Reward, 0.2f, 1.25f);
    }

    private IEnumerator RunEncounter(EncounterDefinition encounter)
    {
        float start = Time.time;
        for (int index = 0; index < encounter.beats.Length; index++)
        {
            var beat = encounter.beats[index];
            bool shortRooms = GameManager.Instance != null && GameManager.Instance.equipmentLoop != null;
            int limit = shortRooms ? Mathf.Min(8, Mathf.Max(1, encounter.maxAlive)) : Mathf.Max(1, encounter.maxAlive);
            int count = Mathf.Clamp(beat.count, 1, shortRooms ? Mathf.Min(3, limit) : limit);
            float at = shortRooms ? beat.at * .3f : beat.at;
            while (Time.time - start < at || enemiesAlive + count > limit) yield return null;
            if (GameManager.Instance == null || !GameManager.Instance.IsCombatActive) yield break;
            enemySpawner.SpawnEntry(beat.kind, count, beat.entry);
            GameManager.Instance.SetProgress(encounter.title + "  /  " + Mathf.RoundToInt((index + 1f) / encounter.beats.Length * 100f) + "%");
        }
    }

    private void ClearEnemies()
    {
        Damageable[] damageables = FindObjectsByType<Damageable>(FindObjectsSortMode.None);
        foreach (var actor in damageables)
        {
            if (actor != null && actor.team == 1)
            {
                actor.gameObject.SetActive(false);
                Destroy(actor.gameObject);
            }
        }
        enemiesAlive = 0;
        bossAlive = false;
    }
}
