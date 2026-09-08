using UnityEngine;
using UnityEngine.SceneManagement;

public enum GamePhase
{
    Hangar,
    Combat,
    Reward,
    Shop,
    Result
}

public enum RunDifficulty
{
    Cadet,
    Standard,
    Veteran
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public RunManager runManager;
    public StageManager stageManager;
    public PlayerController playerController;
    public PlayerStats playerStats;
    public EquipmentManager equipmentManager;
    public EquipmentRewardSystem rewardSystem;
    public RunUpgradeSystem upgradeSystem;
    public EquipmentShop equipmentShop;
    public CombatHUD combatHUD;
    public HangarDeploymentUI hangarUI;
    public PauseUI pauseUI;
    public RewardUI rewardUI;
    public ShopUI shopUI;
    public ResultUI resultUI;
    public ArenaSector arenaSector;
    public SettingsUI settingsUI;
    public HangarPresentation Hangar { get; private set; }

    public int Coins { get; private set; }
    public int Kills { get; private set; }
    public GamePhase Phase { get; private set; }
    public string ProgressText { get; private set; }
    public bool IsPaused { get; private set; }
    public RunDifficulty Difficulty { get; private set; } = RunDifficulty.Cadet;
    public int CompletedEncounters { get; private set; }
    public bool ContinueUsed { get; private set; }
    public bool LastResultVictory { get; private set; }
    public bool CanContinue => Phase == GamePhase.Result && !LastResultVictory && !ContinueUsed
        && Difficulty == RunDifficulty.Cadet && checkpoint != null;
    public int CheckpointEncounter => checkpoint != null ? checkpoint.encounter : -1;
    private RunCheckpoint checkpoint;

    public string DifficultyDisplayName
    {
        get
        {
            if (Difficulty == RunDifficulty.Veteran)
            {
                return GameText.T("Veteran");
            }

            return GameText.T(Difficulty == RunDifficulty.Standard ? "Standard" : "Demo");
        }
    }

    public float EnemyHealthMultiplier
    {
        get { return Difficulty == RunDifficulty.Cadet ? 0.78f : Difficulty == RunDifficulty.Veteran ? 1.25f : 1f; }
    }

    public float EnemyDamageMultiplier
    {
        get { return Difficulty == RunDifficulty.Cadet ? 0.65f : Difficulty == RunDifficulty.Veteran ? 1.2f : 1f; }
    }

    public float WaveRepairMultiplier
    {
        get { return Difficulty == RunDifficulty.Cadet ? 1.67f : Difficulty == RunDifficulty.Veteran ? 0.67f : 1f; }
    }

    private float timeScaleBeforePause = 1f;

    public bool IsCombatActive
    {
        get { return Phase == GamePhase.Combat && !IsPaused; }
    }

    public bool CanPlayerControl
    {
        get { return !IsPaused && Phase == GamePhase.Combat; }
    }

    private void Awake()
    {
        Instance = this;
        Time.timeScale = 1f;
        AudioListener.pause = false;
        FindMissingReferences();
        Hangar = GetComponent<HangarPresentation>() ?? gameObject.AddComponent<HangarPresentation>();
    }

    private void Start()
    {
        Damageable playerDamageable = playerController != null ? playerController.GetComponent<Damageable>() : null;
        if (playerDamageable != null)
        {
            playerDamageable.OnDied += OnPlayerDied;
        }

        EnterHangar();
    }

    private void Update()
    {
        if (settingsUI != null && settingsUI.IsVisible && Input.GetKeyDown(KeyCode.Escape))
        {
            settingsUI.Hide();
            return;
        }
        if ((Phase == GamePhase.Hangar || Phase == GamePhase.Combat) && Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePause();
        }

        if (!IsPaused && Phase == GamePhase.Hangar && Input.GetKeyDown(KeyCode.E))
        {
            BeginRun();
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
            Time.timeScale = 1f;
            AudioListener.pause = false;
            Cursor.visible = true;
        }
    }

    public void EnterHangar()
    {
        SetPaused(false);
        Phase = GamePhase.Hangar;
        playerController.GetComponent<PlayerLoadout>().EnterHangar();
        Hangar.Show(this);
        ProgressText = "Hangar ready - press E to start";
        if (combatHUD != null)
        {
            combatHUD.SetVisible(false);
        }

        if (hangarUI != null)
        {
            hangarUI.Show(this);
        }

        if (rewardUI != null)
        {
            rewardUI.Hide();
        }

        if (shopUI != null)
        {
            shopUI.Hide();
        }

        if (resultUI != null)
        {
            resultUI.Hide();
        }
    }

    public void BeginRun()
    {
        if (Phase != GamePhase.Hangar || IsPaused || !playerController.GetComponent<PlayerLoadout>().CanDeploy)
        {
            return;
        }

        if (hangarUI != null)
        {
            hangarUI.Hide();
        }
        Hangar.Hide();

        if (combatHUD != null)
        {
            combatHUD.SetVisible(true);
        }

        Coins = 0;
        Kills = 0;
        CompletedEncounters = 0;
        ContinueUsed = false;
        checkpoint = null;
        if (runManager != null)
        {
            runManager.BeginRun();
        }

        if (playerStats != null)
        {
            playerStats.ResetStats();
            playerStats.Heal(9999f);
        }

        if (upgradeSystem != null)
        {
            upgradeSystem.ResetUpgrades();
        }

        playerController.GetComponent<Damageable>().RestoreLife(playerStats.MaxHp, playerStats.MaxHp);
        StartEncounter(0, true);
    }

    public void SetDifficulty(RunDifficulty difficulty)
    {
        if (Phase != GamePhase.Hangar)
        {
            return;
        }

        Difficulty = difficulty;
        if (hangarUI != null)
        {
            hangarUI.Refresh();
        }
    }

    public void StartStage(int stageIndex)
    {
        StartEncounter((stageIndex - 1) * 3, true);
    }

    private void StartEncounter(int index, bool saveCheckpoint)
    {
        SetPaused(false);
        Phase = GamePhase.Combat;
        if (combatHUD != null) combatHUD.SetVisible(true);
        if (arenaSector != null) arenaSector.ShowSector(index < 3 ? 1 : 2);
        if (index == 0 || index == 3 || index == 6)
        {
            playerController.RestoreAt(new Vector3(0f, 0.1f, index == 6 ? -6f : -4f));
            if (saveCheckpoint)
            {
                playerStats.Heal(playerStats.MaxHp);
                checkpoint = new RunCheckpoint { encounter = index, kills = Kills, coins = Coins,
                    seed = upgradeSystem.Seed, upgrades = new System.Collections.Generic.List<RunUpgradeKind>(upgradeSystem.Acquired).ToArray(),
                    player = playerStats.Capture() };
            }
        }
        stageManager.StartEncounter(index);
    }

    public void OnEncounterCleared(int index)
    {
        if (Phase != GamePhase.Combat || stageManager.CurrentEncounter != index || CompletedEncounters != index) return;
        CompletedEncounters++;
        AddCoins(30 + index * 10);
        EnterReward();
    }

    public void OnStageCleared(int stageIndex)
    {
        if (Phase != GamePhase.Combat || stageIndex != 2 || CompletedEncounters != 6) return;
        AddCoins(150);
        EnterResult(true);
    }

    public void EnterReward()
    {
        if (Phase != GamePhase.Combat) return;
        SetPaused(false);
        Phase = GamePhase.Reward;
        StopCombat();
        if (combatHUD != null) combatHUD.SetVisible(false);
        ProgressText = "ENCOUNTER " + CompletedEncounters + " / 6 CLEARED";
        GameAudio.Play(GameAudioCue.Reward, 0.45f, 1f);
        if (rewardUI != null)
        {
            rewardUI.Show(this, upgradeSystem);
        }
    }

    public void FinishReward()
    {
        if (Phase != GamePhase.Reward || upgradeSystem.Count != CompletedEncounters) return;
        if (rewardUI != null)
        {
            rewardUI.Hide();
        }

        StartEncounter(CompletedEncounters, true);
    }

    public void EnterShop()
    {
        SetPaused(false);
        Phase = GamePhase.Shop;
        if (shopUI != null)
        {
            shopUI.Show(this, equipmentShop);
        }
    }

    public void ContinueFromShop()
    {
        if (shopUI != null)
        {
            shopUI.Hide();
        }

        StartStage(2);
    }

    public void EnterResult(bool victory)
    {
        if (Phase == GamePhase.Result) return;
        SetPaused(false);
        Phase = GamePhase.Result;
        LastResultVictory = victory;
        StopCombat();
        if (combatHUD != null) combatHUD.SetVisible(false);
        if (rewardUI != null) rewardUI.Hide();
        if (shopUI != null) shopUI.Hide();
        GameAudio.Play(victory ? GameAudioCue.Victory : GameAudioCue.Defeat, 0.55f, 1f);
        if (runManager != null)
        {
            runManager.EndRun();
        }

        if (resultUI != null)
        {
            resultUI.Show(this, victory);
        }
    }

    public void RegisterKill(int rewardCoins)
    {
        if (Phase != GamePhase.Combat) return;
        Kills++;
        if (rewardCoins > 0)
        {
            AddCoins(rewardCoins);
        }

        if (upgradeSystem != null && upgradeSystem.KillHeal > 0f && playerStats != null)
        {
            playerStats.Heal(upgradeSystem.KillHeal);
        }
    }

    private void StopCombat()
    {
        ResetPlayerInput();
        CombatEffects.ClearTelegraphs();
        if (stageManager != null) stageManager.StopStage();
        foreach (var projectile in FindObjectsByType<Projectile>(FindObjectsSortMode.None))
        {
            projectile.Despawn();
        }
    }

    private void ResetPlayerInput()
    {
        if (playerController == null) return;
        playerController.CancelMovement();
        if (playerController.InputRouter != null) playerController.InputRouter.Clear();
        var controls = playerController.GetComponent<MobileControls>();
        if (controls != null) controls.SetVisible(false);
    }

    public void AddCoins(int amount)
    {
        Coins = Mathf.Max(0, Coins + amount);
    }

    public bool SpendCoins(int amount)
    {
        if (Coins < amount)
        {
            return false;
        }

        Coins -= amount;
        return true;
    }

    public void SetProgress(string value)
    {
        ProgressText = value;
    }

    public float GetRunTime()
    {
        return runManager != null ? runManager.ElapsedTime : 0f;
    }

    public void TogglePause()
    {
        SetPaused(!IsPaused);
    }

    public void SetPaused(bool paused)
    {
        if (paused && Phase != GamePhase.Hangar && Phase != GamePhase.Combat)
        {
            return;
        }

        if (paused == IsPaused)
        {
            return;
        }

        if (paused)
        {
            timeScaleBeforePause = Mathf.Max(0.0001f, Time.timeScale);
            IsPaused = true;
            ResetPlayerInput();
            Time.timeScale = 0f;
            AudioListener.pause = true;
            if (pauseUI != null)
            {
                pauseUI.Show(this);
            }

            return;
        }

        IsPaused = false;
        Time.timeScale = timeScaleBeforePause;
        AudioListener.pause = false;
        if (pauseUI != null)
        {
            pauseUI.Hide();
        }
    }

    public void RestartRun()
    {
        IsPaused = false;
        Time.timeScale = 1f;
        AudioListener.pause = false;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public bool ContinueRun()
    {
        if (!CanContinue) return false;
        ContinueUsed = true;
        resultUI.Hide();
        Kills = checkpoint.kills;
        Coins = checkpoint.coins;
        CompletedEncounters = checkpoint.encounter;
        playerStats.ResetStats();
        upgradeSystem.Restore(checkpoint.upgrades, checkpoint.seed);
        playerStats.Restore(checkpoint.player);
        runManager.ResumeRun();
        StartEncounter(checkpoint.encounter, false);
        return true;
    }

    private void OnPlayerDied(Damageable playerDamageable)
    {
        if (Phase == GamePhase.Result)
        {
            return;
        }

        EnterResult(false);
    }

    private void FindMissingReferences()
    {
        if (settingsUI == null) settingsUI = GetComponent<SettingsUI>() ?? gameObject.AddComponent<SettingsUI>();
        if (arenaSector == null) arenaSector = FindFirstObjectByType<ArenaSector>();
        if (runManager == null)
        {
            runManager = GetComponent<RunManager>();
        }

        if (stageManager == null)
        {
            stageManager = GetComponent<StageManager>();
        }

        if (playerController == null)
        {
            playerController = FindFirstObjectByType<PlayerController>();
        }

        if (playerStats == null && playerController != null)
        {
            playerStats = playerController.GetComponent<PlayerStats>();
        }

        if (equipmentManager == null && playerController != null)
        {
            equipmentManager = playerController.GetComponent<EquipmentManager>();
        }

        if (rewardSystem == null)
        {
            rewardSystem = GetComponent<EquipmentRewardSystem>();
        }

        if (upgradeSystem == null)
        {
            upgradeSystem = GetComponent<RunUpgradeSystem>();
        }

        if (equipmentShop == null)
        {
            equipmentShop = GetComponent<EquipmentShop>();
        }

        if (combatHUD == null)
        {
            combatHUD = FindFirstObjectByType<CombatHUD>();
        }

        if (hangarUI == null)
        {
            hangarUI = FindFirstObjectByType<HangarDeploymentUI>();
        }

        if (pauseUI == null)
        {
            pauseUI = FindFirstObjectByType<PauseUI>();
        }

        if (rewardUI == null)
        {
            rewardUI = FindFirstObjectByType<RewardUI>();
        }

        if (shopUI == null)
        {
            shopUI = FindFirstObjectByType<ShopUI>();
        }

        if (resultUI == null)
        {
            resultUI = FindFirstObjectByType<ResultUI>();
        }
    }
}
