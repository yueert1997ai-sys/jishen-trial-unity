using UnityEngine;
using UnityEngine.SceneManagement;

public enum GamePhase
{
    Hangar,
    Combat,
    Reward,
    Shop,
    Result,
    Loadout
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
    public EquipmentLoop equipmentLoop;
    public HangarPresentation Hangar { get; private set; }
    public string LastRunUpgradeSummary { get; private set; }
    public bool WeaponTrialActive => weaponTrial != null;
    private WeaponTrial weaponTrial;

    public int Coins { get; private set; }
    public int Kills { get; private set; }
    public Vector3 LastKillPosition {get;private set;}
    public GamePhase Phase { get; private set; }
    public string ProgressText { get; private set; }
    public bool IsPaused { get; private set; }
    public RunDifficulty Difficulty { get; private set; } = RunDifficulty.Cadet;
    public int CompletedEncounters { get; private set; }
    public bool ContinueUsed { get; private set; }
    public bool LastResultVictory { get; private set; }
    public bool AwaitingContinue=>Phase==GamePhase.Combat && stageManager.Flow?.Phase==EncounterPhase.RewardHold;
    public bool CanContinue => equipmentLoop == null && Phase == GamePhase.Result && !LastResultVictory && !ContinueUsed
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
        get { return !IsPaused && (equipmentLoop == null || !equipmentLoop.UI.IsVisible) && Phase == GamePhase.Combat; }
    }

    private void Awake()
    {
        Instance = this;
        Time.timeScale = 1f;
        AudioListener.pause = false;
        FindMissingReferences();
        equipmentLoop = GetComponent<EquipmentLoop>() ?? gameObject.AddComponent<EquipmentLoop>();
        equipmentLoop.Initialize(this);
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
        if (equipmentLoop != null && equipmentLoop.UI.IsVisible)
        {
            if (Input.GetKeyDown(KeyCode.Escape) && Phase == GamePhase.Hangar) equipmentLoop.CloseWarehouse();
            return;
        }
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
        if(!IsPaused && AwaitingContinue && Input.GetKeyDown(KeyCode.Return))ContinueAfterSalvage();
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
        GameAudio.ResetCombatSound();
        Phase = GamePhase.Hangar;
        var playerDamageable=playerController.GetComponent<Damageable>();
        if(playerDamageable!=null&&playerDamageable.IsDead)
            playerDamageable.RestoreLife(playerStats.MaxHp,playerStats.MaxHp);
        playerController.Loadout.EnterHangar();
        Hangar.Show(this);
        ProgressText = "Hangar ready - press E to start";
        if (equipmentLoop != null) equipmentLoop.ApplyLoadout();
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

    public void BeginP0Combat()
    {
        if(!PrepareRun(CombatMode.ShortCombat))return;
        stageManager.StopStage();Phase=GamePhase.Combat;arenaSector.ShowSector(1);
        GameAudio.ResetCombatSound();
        ArmorContactVfx.Get().Clear();
        playerStats.baseDashCooldown=.48f;playerStats.ResetStats();
        playerController.RestoreAt(arenaSector.PlayerEntry);
        combatHUD.SetVisible(false);
    }
    public void BeginRun(){BeginFullDemo();}
    public void BeginFullDemo()
    {
        if(!PrepareRun(CombatMode.FullDemo))return;
        ClosePractice();StartEncounter(0,true);
    }
    void ClosePractice()
    {
        foreach(var range in FindObjectsByType<WeaponTrial>(FindObjectsSortMode.None))
        {range.enabled=false;Destroy(range);}
        weaponTrial=null;
        foreach(var demo in FindObjectsByType<P0CombatDemo>(FindObjectsSortMode.None))
        {demo.enabled=false;Destroy(demo.gameObject);}
        CombatLabSettings.Exit();
    }
    public void ExitPractice(){ClosePractice();CombatRuntime.EndRun();StopCombat();EnterHangar();}
    public void BeginShortCombat()
    {
        if(Phase!=GamePhase.Hangar||IsPaused)return;
        var demo=FindFirstObjectByType<P0CombatDemo>();
        if(demo==null)new GameObject("Combat practice").AddComponent<P0CombatDemo>();else demo.Restart();
    }
    public void ReplayCurrentRun()
    {
        if(Phase!=GamePhase.Result)return;
        EnterHangar();playerController.Loadout.Select(PrimaryWeapon.M7);BeginFullDemo();
    }

    private bool PrepareRun(CombatMode mode=CombatMode.FullDemo)
    {
        if (Phase != GamePhase.Hangar || IsPaused || !playerController.Loadout.CanDeploy || (equipmentLoop != null && equipmentLoop.UI.IsVisible))
        {
            return false;
        }

        if (hangarUI != null)
        {
            hangarUI.Hide();
        }

        if (combatHUD != null)
        {
            combatHUD.SetVisible(true);
        }

        CombatRuntime.BeginRun(mode:mode);UnityEngine.Random.InitState(CombatRuntime.Run.Seed);
        GameAudio.ResetCombatSound();OverdriveVfx.Clear();ArmorContactVfx.Get().Clear();
        Coins = 0;
        Hangar.Hide();
        Kills = 0;LastKillPosition=Vector3.zero;
        CompletedEncounters = 0;
        ContinueUsed = false;
        checkpoint = null;
        if (runManager != null)
        {
            runManager.BeginRun();
        }

        if (playerStats != null)
        {
            playerStats.baseDashCooldown=.48f;
            playerStats.ResetStats();
            playerStats.Heal(9999f);
        }

        if (upgradeSystem != null)
        {
            upgradeSystem.ResetUpgrades(CombatRuntime.Run!=null?CombatRuntime.Run.Seed:0);
        }

        if (equipmentLoop != null) equipmentLoop.BeginRun();

        playerController.GetComponent<Damageable>().RestoreLife(playerStats.MaxHp, playerStats.MaxHp);
        return true;
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

    private void StartEncounter(int index, bool saveCheckpoint, GameObject bossOverride = null)
    {
        SetPaused(false);
        Phase = GamePhase.Combat;
        if (combatHUD != null) combatHUD.SetVisible(true);
        if (arenaSector != null) arenaSector.ShowSector(index < 3 ? 1 : 2);
        if (index == 0 || index == 3 || index == 6)
        {
            playerController.RestoreAt(index==6?new Vector3(0,.1f,-6):arenaSector.PlayerEntry);
            if (saveCheckpoint)
            {
                playerStats.Heal(playerStats.MaxHp);
                checkpoint = new RunCheckpoint { encounter = index, kills = Kills, coins = Coins,
                    seed = upgradeSystem.Seed, upgrades = new System.Collections.Generic.List<RunUpgradeKind>(upgradeSystem.Acquired).ToArray(),
                    player = playerStats.Capture() };
            }
        }
        stageManager.StartEncounter(index, bossOverride);
        if(CombatRuntime.Run!=null)CombatRuntime.Run.Encounter=index;
    }

    public void OnEncounterCleared(int index)
    {
        if (Phase != GamePhase.Combat || stageManager.CurrentEncounter != index || CompletedEncounters != index) return;
        CompletedEncounters++;
        CombatFeedback.EncounterCleared(LastKillPosition);
        AddCoins(30 + index * 10);
        SetProgress("清场完成 · F 试装残骸武器 · Enter 继续并选择强化");
        foreach(var shot in FindObjectsByType<Projectile>(FindObjectsSortMode.None))shot.Despawn();
    }
    public bool ContinueAfterSalvage()
    {
        if(IsPaused||!AwaitingContinue||equipmentLoop.Absorption.Busy)return false;
        if(!equipmentLoop.Absorption.CollectWithoutInstalling())return false;
        if(!stageManager.Flow.ContinueRoom())return false;
        EnterReward();return true;
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
        CombatRuntime.InvalidateActions();ResetPlayerInput();GameAudio.ResetCombatSound();
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

        if(stageManager.Flow!=null&&!stageManager.Flow.CompleteReward())return;
        StartEncounter(CompletedEncounters, true);
    }

    public void BeginBossPreview()
    {
        if (!PrepareRun()) return;
        CompletedEncounters = 6;
        StartEncounter(6, false, stageManager.enemySpawner.bossPrefab);
    }

    public void BeginLiquidBossChallenge()
    {
        var bossPrefab = stageManager.enemySpawner.liquidBossPrefab;
        if (bossPrefab == null)
        {
            Debug.LogError("Liquid E-01 boss is not assigned to this scene.");
            return;
        }
        if (!PrepareRun()) return;
        // Match the six random upgrades normally earned before the final encounter.
        // These use the run-only buff system and never enter the permanent warehouse.
        for (int i = 0; i < 6; i++)
        {
            var options = upgradeSystem.GenerateOptions();
            upgradeSystem.ApplyOption(options[Random.Range(0, options.Count)]);
        }
        CompletedEncounters = 6;
        playerController.GetComponent<Damageable>().RestoreLife(playerStats.MaxHp, playerStats.MaxHp);
        StartEncounter(6, false, bossPrefab);
    }
    public void BeginWeaponTrial()
    {
        if(Phase!=GamePhase.Hangar || IsPaused || !playerController.Loadout.CanDeploy)return;
        Hangar.Hide();
        if (arenaSector != null) arenaSector.ShowSector(1);
        StopCombat();equipmentLoop.ClearPickups();
        Phase=GamePhase.Combat;hangarUI.Hide();combatHUD.SetVisible(false);
        playerController.RestoreAt(new Vector3(0,.1f,-12));
        playerController.GetComponent<Damageable>().RestoreLife(playerStats.MaxHp,playerStats.MaxHp);
        weaponTrial=gameObject.AddComponent<WeaponTrial>();weaponTrial.Open(this);
    }
    public void EndWeaponTrial()
    {
        if(weaponTrial!=null)Destroy(weaponTrial);weaponTrial=null;
        StopCombat();equipmentLoop.ClearPickups();
        EnterHangar();
    }

    public void ContinueFromLoadout()
    {
        if (Phase != GamePhase.Loadout) return;
        equipmentLoop.UI.Hide();
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
        if(!victory && stageManager.Flow?.Phase==EncounterPhase.BossDefeat)return;
        SetPaused(false);
        Phase = GamePhase.Result;
        LastResultVictory = victory;
        CombatRuntime.EndRun();
        LastRunUpgradeSummary = upgradeSystem != null ? upgradeSystem.GetSummary() : "";
        StopCombat();
        if (equipmentLoop != null)
        {
            equipmentLoop.UI.Hide();
            equipmentLoop.ClearPickups();
            checkpoint = null;
            if (upgradeSystem != null) upgradeSystem.ResetUpgrades(CombatRuntime.Run!=null?CombatRuntime.Run.Seed:0);
        }
        if (combatHUD != null) combatHUD.SetVisible(false);
        if (rewardUI != null) rewardUI.Hide();
        if (shopUI != null) shopUI.Hide();
        if(!CombatLabSettings.Active)GameAudio.Play(victory ? GameAudioCue.Victory : GameAudioCue.Defeat, 0.55f, 1f);
        if (runManager != null)
        {
            runManager.EndRun();
        }

        if (resultUI != null)
        {
            resultUI.Show(this, victory);
        }
    }

    public void RegisterKill(int rewardCoins,Vector3? position=null)
    {
        if(position.HasValue)LastKillPosition=position.Value;
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
        CombatRuntime.InvalidateActions();
        GameAudio.ResetCombatSound();
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
            CombatRuntime.InvalidateActions();
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
        CombatRuntime.EndRun();
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
