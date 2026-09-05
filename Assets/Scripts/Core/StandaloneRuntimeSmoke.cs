using System;
using System.Collections;
using UnityEngine;

public class StandaloneRuntimeSmoke : MonoBehaviour
{
    private static bool waitingForRestart;
    private static string runtimeError;

    private bool smokeActive;
    private bool pauseVerified;
    private bool lodVerified;
    private int rewardsApplied;
    private bool eliteSeen;
    private bool bossSeen;
    private bool difficultyActorVerified;

    private void Awake()
    {
        string[] arguments = Environment.GetCommandLineArgs();
        for (int i = 0; i < arguments.Length; i++)
        {
            if (string.Equals(arguments[i], "-mechSmoke", StringComparison.OrdinalIgnoreCase))
            {
                smokeActive = true;
                break;
            }
        }

        enabled = smokeActive;
        if (smokeActive)
        {
            Application.logMessageReceived += CaptureRuntimeError;
        }
    }

    private void OnDestroy()
    {
        if (smokeActive)
        {
            Application.logMessageReceived -= CaptureRuntimeError;
        }
    }

    private IEnumerator Start()
    {
        if (!smokeActive)
        {
            yield break;
        }

        yield return null;
        if (waitingForRestart)
        {
            GameManager restartedManager = GameManager.Instance;
            if (restartedManager == null || restartedManager.Phase != GamePhase.Hangar)
            {
                Fail("Restarted scene did not return to Hangar.");
                yield break;
            }

            if (restartedManager.hangarUI == null || !restartedManager.hangarUI.IsVisible)
            {
                Fail("Restarted scene did not restore the hangar deployment UI.");
                yield break;
            }

            if (!string.IsNullOrEmpty(runtimeError))
            {
                Fail("Runtime error was logged: " + runtimeError);
                yield break;
            }

            Debug.Log("MECH_ROUGE_STANDALONE_PASS: Full built-player flow reached victory and restart returned to Hangar.");
            Application.Quit(0);
            yield break;
        }

        float deadline = Time.realtimeSinceStartup + 45f;
        GameManager gameManager = GameManager.Instance;
        if (gameManager == null)
        {
            Fail("GameManager was not initialized.");
            yield break;
        }

        if (gameManager.hangarUI == null || !gameManager.hangarUI.IsVisible)
        {
            Fail("Built player did not show the hangar deployment UI.");
            yield break;
        }

        gameManager.SetDifficulty(RunDifficulty.Veteran);
        if (gameManager.Difficulty != RunDifficulty.Veteran || Mathf.Abs(gameManager.EnemyHealthMultiplier - 1.25f) > 0.001f)
        {
            Fail("Built player did not apply the Veteran difficulty profile.");
            yield break;
        }

        gameManager.SetDifficulty(RunDifficulty.Cadet);
        if (gameManager.Difficulty != RunDifficulty.Cadet || Mathf.Abs(gameManager.EnemyDamageMultiplier - 0.65f) > 0.001f)
        {
            Fail("Built player did not apply the Cadet difficulty profile.");
            yield break;
        }

        Time.timeScale = 20f;
        gameManager.BeginRun();
        if (gameManager.hangarUI.IsVisible || gameManager.combatHUD == null || !gameManager.combatHUD.IsVisible)
        {
            Fail("Built player did not transition from deployment UI to combat HUD.");
            yield break;
        }
        while (Time.realtimeSinceStartup < deadline)
        {
            gameManager = GameManager.Instance;
            if (gameManager == null)
            {
                yield return null;
                continue;
            }

            if (!string.IsNullOrEmpty(runtimeError))
            {
                Fail("Runtime error was logged: " + runtimeError);
                yield break;
            }

            if (gameManager.Phase == GamePhase.Combat)
            {
                if (!pauseVerified && !VerifyPause(gameManager))
                {
                    yield break;
                }

                if (!lodVerified && !VerifyLod())
                {
                    yield break;
                }

                if (!ObserveActors())
                {
                    yield break;
                }

                KillEnemyTeam();
            }
            else if (gameManager.Phase == GamePhase.Reward)
            {
                if (gameManager.upgradeSystem == null)
                {
                    Fail("Reward phase has no RunUpgradeSystem.");
                    yield break;
                }

                RunUpgradeOption option = gameManager.upgradeSystem.GenerateOptions()[0];
                gameManager.upgradeSystem.ApplyOption(option);
                rewardsApplied++;
                gameManager.FinishReward();
            }
            else if (gameManager.Phase == GamePhase.Result)
            {
                if (rewardsApplied != 6 || !eliteSeen || !bossSeen || !difficultyActorVerified || gameManager.Kills <= 0 || gameManager.playerStats == null || gameManager.playerStats.CurrentHp <= 0f)
                {
                    Fail("Built-player result did not satisfy the victory flow contract.");
                    yield break;
                }

                waitingForRestart = true;
                Time.timeScale = 1f;
                gameManager.RestartRun();
                yield break;
            }

            yield return null;
        }

        Fail("Timed out before completing the built-player flow.");
    }

    private bool VerifyPause(GameManager gameManager)
    {
        float previousScale = Time.timeScale;
        gameManager.SetPaused(true);
        if (!gameManager.IsPaused || Time.timeScale != 0f || gameManager.IsCombatActive || gameManager.CanPlayerControl)
        {
            Fail("Pause contract failed in the built player.");
            return false;
        }

        gameManager.SetPaused(false);
        if (gameManager.IsPaused || Mathf.Abs(Time.timeScale - previousScale) > 0.01f)
        {
            Fail("Resume contract failed in the built player.");
            return false;
        }

        pauseVerified = true;
        return true;
    }

    private bool VerifyLod()
    {
        LODGroup group = FindFirstObjectByType<LODGroup>();
        if (group == null || group.GetLODs().Length != 3)
        {
            Fail("Three-level player LODGroup was not present in the built player.");
            return false;
        }

        lodVerified = true;
        return true;
    }

    private bool ObserveActors()
    {
        EnemyBase[] enemies = FindObjectsByType<EnemyBase>(FindObjectsSortMode.None);
        for (int i = 0; i < enemies.Length; i++)
        {
            if (enemies[i] == null)
            {
                continue;
            }

            if (Mathf.Abs(enemies[i].DifficultyHealthMultiplier - 0.78f) > 0.001f)
            {
                Fail("Built enemy did not receive the selected difficulty health multiplier.");
                return false;
            }

            difficultyActorVerified = true;
            if (enemies[i].kind == EnemyKind.Elite)
            {
                eliteSeen = true;
            }
        }

        BossController boss = FindFirstObjectByType<BossController>();
        if (boss != null)
        {
            if (Mathf.Abs(boss.DifficultyHealthMultiplier - 0.78f) > 0.001f)
            {
                Fail("Built Boss did not receive the selected difficulty health multiplier.");
                return false;
            }

            bossSeen = true;
        }

        return true;
    }

    private static void KillEnemyTeam()
    {
        Damageable[] damageables = FindObjectsByType<Damageable>(FindObjectsSortMode.None);
        for (int i = 0; i < damageables.Length; i++)
        {
            Damageable target = damageables[i];
            if (target != null && target.team == 1 && !target.IsDead)
            {
                target.Kill(new DamageInfo(null, target.transform.position, null, target.CurrentHealth));
            }
        }
    }

    private static void CaptureRuntimeError(string condition, string stackTrace, LogType type)
    {
        if ((type == LogType.Error || type == LogType.Exception || type == LogType.Assert) && string.IsNullOrEmpty(runtimeError))
        {
            runtimeError = condition + "\n" + stackTrace;
        }
    }

    private static void Fail(string message)
    {
        Debug.LogError("MECH_ROUGE_STANDALONE_FAIL: " + message);
        Time.timeScale = 1f;
        Application.Quit(1);
    }
}
