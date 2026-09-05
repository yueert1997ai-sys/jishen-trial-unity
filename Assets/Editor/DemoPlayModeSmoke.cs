using System.Globalization;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class DemoPlayModeSmoke
{
    private const string ScenePath = "Assets/Scenes/Demo_Main.unity";
    private const string Prefix = "MECH_ROUGE_SMOKE_";
    private const string ActiveKey = Prefix + "ACTIVE";
    private const string StartTimeKey = Prefix + "START_TIME";
    private const string BeganKey = Prefix + "BEGAN";
    private const string FiredKey = Prefix + "FIRED";
    private const string RewardKey = Prefix + "REWARD";
    private const string WonKey = Prefix + "WON";
    private const string ProjectErrorKey = Prefix + "PROJECT_ERROR";
    private const string EliteSeenKey = Prefix + "ELITE_SEEN";
    private const string BossSeenKey = Prefix + "BOSS_SEEN";
    private const string PauseTestedKey = Prefix + "PAUSE_TESTED";
    private const string PlayerDamagedKey = Prefix + "PLAYER_DAMAGED";
    private const string DamagedHpKey = Prefix + "DAMAGED_HP";
    private const string RepairVerifiedKey = Prefix + "REPAIR_VERIFIED";
    private const string EnemyVisualMaskKey = Prefix + "ENEMY_VISUAL_MASK";
    private const string DifficultyActorKey = Prefix + "DIFFICULTY_ACTOR";

    static DemoPlayModeSmoke()
    {
        if (EditorPrefs.GetBool(ActiveKey, false))
        {
            AttachUpdate();
        }
    }

    [MenuItem("MECH ROUGE/Run Play Mode Smoke")]
    public static void RunFullFlow()
    {
        EditorPrefs.SetBool(ActiveKey, true);
        EditorPrefs.SetString(StartTimeKey, EditorApplication.timeSinceStartup.ToString(CultureInfo.InvariantCulture));
        EditorPrefs.SetBool(BeganKey, false);
        EditorPrefs.SetBool(FiredKey, false);
        EditorPrefs.SetBool(RewardKey, false);
        EditorPrefs.SetBool(WonKey, false);
        EditorPrefs.SetBool(EliteSeenKey, false);
        EditorPrefs.SetBool(BossSeenKey, false);
        EditorPrefs.SetBool(PauseTestedKey, false);
        EditorPrefs.SetBool(PlayerDamagedKey, false);
        EditorPrefs.SetBool(RepairVerifiedKey, false);
        EditorPrefs.SetInt(EnemyVisualMaskKey, 0);
        EditorPrefs.SetBool(DifficultyActorKey, false);
        EditorPrefs.DeleteKey(DamagedHpKey);
        EditorPrefs.DeleteKey(ProjectErrorKey);

        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        AttachUpdate();
        EditorApplication.EnterPlaymode();
    }

    private static void AttachUpdate()
    {
        EditorApplication.update -= UpdateSmoke;
        EditorApplication.update += UpdateSmoke;
        Application.logMessageReceived -= CaptureProjectError;
        Application.logMessageReceived += CaptureProjectError;
    }

    private static void CaptureProjectError(string condition, string stackTrace, LogType type)
    {
        if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert)
        {
            return;
        }

        bool belongsToProject = stackTrace.Contains("Assets/") || stackTrace.Contains("Assets\\") || stackTrace.Contains("Assembly-CSharp");
        if (belongsToProject && !EditorPrefs.HasKey(ProjectErrorKey))
        {
            EditorPrefs.SetString(ProjectErrorKey, condition + "\n" + stackTrace);
        }
    }

    private static void UpdateSmoke()
    {
        if (!EditorPrefs.GetBool(ActiveKey, false))
        {
            Cleanup();
            return;
        }

        if (GetElapsedSeconds() > 60d)
        {
            Fail("Timed out before completing the full playable loop.");
            return;
        }

        if (!EditorApplication.isPlaying)
        {
            return;
        }

        Time.timeScale = 20f;

        GameManager gameManager = Object.FindFirstObjectByType<GameManager>();
        if (gameManager == null)
        {
            return;
        }

        if (EditorPrefs.GetBool(WonKey, false))
        {
            if (gameManager.Phase == GamePhase.Hangar)
            {
                Pass("Full flow reached victory result, restart returned to hangar.");
            }

            return;
        }

        if (!EditorPrefs.GetBool(BeganKey, false))
        {
            if (gameManager.hangarUI == null || !gameManager.hangarUI.IsVisible)
            {
                Fail("Hangar deployment UI was not visible before the run.");
                return;
            }

            gameManager.SetDifficulty(RunDifficulty.Veteran);
            if (gameManager.Difficulty != RunDifficulty.Veteran || Mathf.Abs(gameManager.EnemyHealthMultiplier - 1.25f) > 0.001f || Mathf.Abs(gameManager.EnemyDamageMultiplier - 1.2f) > 0.001f)
            {
                Fail("Veteran difficulty profile did not apply its combat multipliers.");
                return;
            }

            gameManager.SetDifficulty(RunDifficulty.Cadet);
            if (gameManager.Difficulty != RunDifficulty.Cadet || Mathf.Abs(gameManager.EnemyHealthMultiplier - 0.78f) > 0.001f || Mathf.Abs(gameManager.EnemyDamageMultiplier - 0.65f) > 0.001f || Mathf.Abs(gameManager.WaveRepairMultiplier - 1.67f) > 0.001f)
            {
                Fail("Cadet difficulty profile did not apply its accessibility multipliers.");
                return;
            }

            gameManager.BeginRun();
            if (gameManager.hangarUI.IsVisible || gameManager.combatHUD == null || !gameManager.combatHUD.IsVisible)
            {
                Fail("Deploy did not transition from the hangar UI to the combat HUD.");
                return;
            }

            EditorPrefs.SetBool(BeganKey, true);
            return;
        }

        if (!EditorPrefs.GetBool(FiredKey, false))
        {
            if (GameAudio.Instance == null)
            {
                Fail("GameAudio was not initialized.");
                return;
            }

            if (Object.FindFirstObjectByType<MechMotionAnimator>() == null)
            {
                Fail("MechMotionAnimator was not found on the player.");
                return;
            }

            LODGroup playerLodGroup = Object.FindFirstObjectByType<LODGroup>();
            if (playerLodGroup == null)
            {
                Fail("Player mech LODGroup was not found.");
                return;
            }

            LOD[] playerLods = playerLodGroup.GetLODs();
            if (playerLods.Length != 3)
            {
                Fail("Player mech does not expose the expected high, medium, and low LOD levels.");
                return;
            }

            long highTriangles = CountTriangles(playerLods[0].renderers);
            long mediumTriangles = CountTriangles(playerLods[1].renderers);
            long lowTriangles = CountTriangles(playerLods[2].renderers);
            if (highTriangles < 100000 || mediumTriangles >= highTriangles || mediumTriangles < 20000 || lowTriangles >= mediumTriangles || lowTriangles > 15000)
            {
                Fail("Player LOD triangle budgets are invalid: " + highTriangles + " / " + mediumTriangles + " / " + lowTriangles + ".");
                return;
            }

            Debug.Log("MECH_ROUGE_LOD_PASS: high=" + highTriangles + " medium=" + mediumTriangles + " low=" + lowTriangles);

            if (!EditorPrefs.GetBool(PauseTestedKey, false))
            {
                float previousScale = Time.timeScale;
                gameManager.SetPaused(true);
                if (!gameManager.IsPaused || Time.timeScale != 0f || gameManager.CanPlayerControl || gameManager.IsCombatActive)
                {
                    Fail("Pause did not suspend time and combat control.");
                    return;
                }

                if (gameManager.pauseUI == null || !gameManager.pauseUI.IsVisible || !AudioListener.pause)
                {
                    Fail("Pause UI or audio pause state was not activated.");
                    return;
                }

                gameManager.SetPaused(false);
                if (gameManager.IsPaused || Mathf.Abs(Time.timeScale - previousScale) > 0.01f || AudioListener.pause || gameManager.pauseUI.IsVisible)
                {
                    Fail("Resume did not restore the previous time, audio, and UI state.");
                    return;
                }

                Time.timeScale = 20f;
                EditorPrefs.SetBool(PauseTestedKey, true);
            }

            WeaponController weapon = Object.FindFirstObjectByType<WeaponController>();
            if (weapon == null)
            {
                Fail("WeaponController was not found.");
                return;
            }

            weapon.TryFireBeam();
            Projectile[] projectiles = Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None);
            if (projectiles.Length == 0)
            {
                Fail("Player weapon did not create a projectile.");
                return;
            }

            if (projectiles[0].GetComponent<TrailRenderer>() == null)
            {
                Fail("Player projectile was created without a TrailRenderer.");
                return;
            }

            Damageable playerDamageable = gameManager.playerStats != null ? gameManager.playerStats.GetComponent<Damageable>() : null;
            if (playerDamageable == null)
            {
                Fail("Player Damageable was not found for the fairness test.");
                return;
            }

            float healthBeforeHit = playerDamageable.CurrentHealth;
            playerDamageable.TakeDamage(20f, new DamageInfo(null, playerDamageable.transform.position + Vector3.forward, null, 20f));
            float healthAfterHit = playerDamageable.CurrentHealth;
            float damageTaken = healthBeforeHit - healthAfterHit;
            float expectedDamage = 20f * gameManager.playerStats.incomingDamageMultiplier * gameManager.EnemyDamageMultiplier;
            if (Mathf.Abs(damageTaken - expectedDamage) > 0.05f)
            {
                Fail("Difficulty-adjusted incoming damage was " + damageTaken + " instead of " + expectedDamage + ".");
                return;
            }

            playerDamageable.TakeDamage(20f, new DamageInfo(null, playerDamageable.transform.position + Vector3.forward, null, 20f));
            if (Mathf.Abs(playerDamageable.CurrentHealth - healthAfterHit) > 0.01f)
            {
                Fail("Player hit invulnerability did not prevent same-frame burst stacking.");
                return;
            }

            EditorPrefs.SetFloat(DamagedHpKey, healthAfterHit);
            EditorPrefs.SetBool(PlayerDamagedKey, true);

            EditorPrefs.SetBool(FiredKey, true);
        }

        if (!ObserveCombatActors())
        {
            return;
        }

        KillEnemyTeam();

        if (EditorPrefs.GetBool(PlayerDamagedKey, false) && !EditorPrefs.GetBool(RepairVerifiedKey, false) && gameManager.stageManager != null && gameManager.stageManager.WaveRepairsGranted > 0)
        {
            float damagedHp = EditorPrefs.GetFloat(DamagedHpKey, gameManager.playerStats.CurrentHp);
            if (gameManager.playerStats == null || gameManager.playerStats.CurrentHp <= damagedHp)
            {
                Fail("Wave repair was granted without restoring player HP.");
                return;
            }

            EditorPrefs.SetBool(RepairVerifiedKey, true);
        }

        if (gameManager.Phase == GamePhase.Reward && !EditorPrefs.GetBool(RewardKey, false))
        {
            if (!EditorPrefs.GetBool(RepairVerifiedKey, false))
            {
                Fail("Stage 1 reached reward without verifying wave repair.");
                return;
            }

            if (gameManager.upgradeSystem == null)
            {
                Fail("Reward phase reached without RunUpgradeSystem.");
                return;
            }

            RunUpgradeOption option = gameManager.upgradeSystem.GenerateOptions()[0];
            gameManager.upgradeSystem.ApplyOption(option);
            if (gameManager.upgradeSystem.GetSummary() == "Upgrades: none")
            {
                Fail("Upgrade selection did not change the run upgrade summary.");
                return;
            }

            EditorPrefs.SetBool(RewardKey, true);
            gameManager.FinishReward();
            return;
        }

        if (gameManager.Phase == GamePhase.Result)
        {
            if (!EditorPrefs.GetBool(RewardKey, false))
            {
                Fail("Result reached before the upgrade choice was applied.");
                return;
            }

            if (gameManager.Kills <= 0)
            {
                Fail("Result reached with no registered kills.");
                return;
            }

            if (!EditorPrefs.GetBool(EliteSeenKey, false))
            {
                Fail("Stage 2 completed without spawning an elite enemy.");
                return;
            }

            if (!EditorPrefs.GetBool(BossSeenKey, false))
            {
                Fail("The run completed without spawning the Boss.");
                return;
            }

            if (EditorPrefs.GetInt(EnemyVisualMaskKey, 0) != 15)
            {
                Fail("The run completed before all four enemy visual archetypes were verified.");
                return;
            }

            if (!EditorPrefs.GetBool(DifficultyActorKey, false))
            {
                Fail("No spawned combat actor verified the selected difficulty health scale.");
                return;
            }

            EditorPrefs.SetBool(WonKey, true);
            Time.timeScale = 1f;
            gameManager.RestartRun();
        }
    }

    private static void KillEnemyTeam()
    {
        Damageable[] damageables = Object.FindObjectsByType<Damageable>(FindObjectsSortMode.None);
        for (int i = 0; i < damageables.Length; i++)
        {
            Damageable damageable = damageables[i];
            if (damageable != null && damageable.team == 1 && !damageable.IsDead)
            {
                damageable.Kill(new DamageInfo(null, damageable.transform.position, null, damageable.CurrentHealth));
            }
        }
    }

    private static long CountTriangles(Renderer[] renderers)
    {
        long triangles = 0;
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null)
            {
                continue;
            }

            Mesh mesh = null;
            MeshFilter filter = renderer.GetComponent<MeshFilter>();
            if (filter != null)
            {
                mesh = filter.sharedMesh;
            }
            else
            {
                SkinnedMeshRenderer skinned = renderer as SkinnedMeshRenderer;
                if (skinned != null)
                {
                    mesh = skinned.sharedMesh;
                }
            }

            if (mesh == null)
            {
                continue;
            }

            for (int subMesh = 0; subMesh < mesh.subMeshCount; subMesh++)
            {
                triangles += (long)mesh.GetIndexCount(subMesh) / 3L;
            }
        }

        return triangles;
    }

    private static bool ObserveCombatActors()
    {
        EnemyBase[] enemies = Object.FindObjectsByType<EnemyBase>(FindObjectsSortMode.None);
        for (int i = 0; i < enemies.Length; i++)
        {
            EnemyBase enemy = enemies[i];
            if (enemy == null)
            {
                continue;
            }

            EnemyMotionAnimator motion = enemy.GetComponent<EnemyMotionAnimator>();
            Transform visualRoot = enemy.transform.Find("VisualRoot");
            int rendererCount = visualRoot != null ? visualRoot.GetComponentsInChildren<Renderer>(true).Length : 0;
            int minimumRenderers = enemy.kind == EnemyKind.Drone ? 8 : enemy.kind == EnemyKind.Elite ? 16 : 14;
            if (motion == null || visualRoot == null || rendererCount < minimumRenderers)
            {
                Fail("Enemy visual archetype " + enemy.kind + " is incomplete. Renderers: " + rendererCount + ".");
                return false;
            }

            if (Mathf.Abs(enemy.DifficultyHealthMultiplier - 0.78f) > 0.001f)
            {
                Fail("Enemy did not receive the Cadet health multiplier.");
                return false;
            }

            EditorPrefs.SetBool(DifficultyActorKey, true);

            int mask = EditorPrefs.GetInt(EnemyVisualMaskKey, 0);
            EditorPrefs.SetInt(EnemyVisualMaskKey, mask | (1 << (int)enemy.kind));
            if (enemy.kind == EnemyKind.Elite)
            {
                EditorPrefs.SetBool(EliteSeenKey, true);
            }
        }

        BossController boss = Object.FindFirstObjectByType<BossController>();
        if (boss != null)
        {
            Transform bossVisual = boss.transform.Find("VisualRoot");
            int rendererCount = bossVisual != null ? bossVisual.GetComponentsInChildren<Renderer>(true).Length : 0;
            if (boss.GetComponent<EnemyMotionAnimator>() == null || rendererCount < 24)
            {
                Fail("Boss visual hierarchy is incomplete. Renderers: " + rendererCount + ".");
                return false;
            }

            if (Mathf.Abs(boss.DifficultyHealthMultiplier - 0.78f) > 0.001f)
            {
                Fail("Boss did not receive the Cadet health multiplier.");
                return false;
            }

            EditorPrefs.SetBool(BossSeenKey, true);
        }

        return true;
    }

    private static double GetElapsedSeconds()
    {
        string raw = EditorPrefs.GetString(StartTimeKey, "0");
        double start;
        if (!double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out start))
        {
            start = EditorApplication.timeSinceStartup;
        }

        return EditorApplication.timeSinceStartup - start;
    }

    private static void Pass(string message)
    {
        string projectError = EditorPrefs.GetString(ProjectErrorKey, "");
        if (!string.IsNullOrEmpty(projectError))
        {
            Fail("Project script error was logged during the smoke test: " + projectError);
            return;
        }

        Debug.Log("MECH_ROUGE_SMOKE_PASS: " + message);
        Cleanup();
        if (Application.isBatchMode)
        {
            EditorApplication.Exit(0);
        }
        else if (EditorApplication.isPlaying)
        {
            EditorApplication.ExitPlaymode();
        }
    }

    private static void Fail(string message)
    {
        Debug.LogError("MECH_ROUGE_SMOKE_FAIL: " + message);
        Cleanup();
        if (Application.isBatchMode)
        {
            EditorApplication.Exit(1);
        }
        else if (EditorApplication.isPlaying)
        {
            EditorApplication.ExitPlaymode();
        }
    }

    private static void Cleanup()
    {
        Time.timeScale = 1f;
        EditorApplication.update -= UpdateSmoke;
        Application.logMessageReceived -= CaptureProjectError;
        EditorPrefs.DeleteKey(ActiveKey);
        EditorPrefs.DeleteKey(StartTimeKey);
        EditorPrefs.DeleteKey(BeganKey);
        EditorPrefs.DeleteKey(FiredKey);
        EditorPrefs.DeleteKey(RewardKey);
        EditorPrefs.DeleteKey(WonKey);
        EditorPrefs.DeleteKey(ProjectErrorKey);
        EditorPrefs.DeleteKey(EliteSeenKey);
        EditorPrefs.DeleteKey(BossSeenKey);
        EditorPrefs.DeleteKey(PauseTestedKey);
        EditorPrefs.DeleteKey(PlayerDamagedKey);
        EditorPrefs.DeleteKey(DamagedHpKey);
        EditorPrefs.DeleteKey(RepairVerifiedKey);
        EditorPrefs.DeleteKey(EnemyVisualMaskKey);
        EditorPrefs.DeleteKey(DifficultyActorKey);
    }
}
