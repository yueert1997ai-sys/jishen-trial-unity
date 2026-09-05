using UnityEngine;
using UnityEngine.UI;

public class CombatHUD : MonoBehaviour
{
    public GameManager gameManager;
    public PlayerStats playerStats;
    public EquipmentManager equipmentManager;
    public RunUpgradeSystem upgradeSystem;

    public bool IsVisible => canvas != null && canvas.gameObject.activeSelf;
    private Canvas canvas;
    private RectTransform safeRoot;
    private Text hpText, energyText, statusText, objectiveText, buildText, bossText;
    private RectTransform hpFill, energyFill, bossFill, bossPanel, lockRing;
    private Damageable playerDamageable, bossDamageable;
    private BossController boss;
    private PlayerController player;
    private Image damageOverlay;
    private float flash;
    private float nextRefresh;
    private float nextBossLookup;

    private void Start()
    {
        BuildUI();
        player = playerStats != null ? playerStats.GetComponent<PlayerController>() : null;
        playerDamageable = playerStats != null ? playerStats.GetComponent<Damageable>() : null;
        if (playerDamageable != null) playerDamageable.OnDamaged += HandleDamage;
        SetVisible(gameManager != null && gameManager.Phase == GamePhase.Combat);
    }

    private void OnDestroy()
    {
        if (playerDamageable != null) playerDamageable.OnDamaged -= HandleDamage;
        Cursor.visible = true;
    }

    public void SetVisible(bool value)
    {
        BuildUI();
        canvas.gameObject.SetActive(value);
        if (!value) flash = 0f;
    }

    private void Update()
    {
        if (canvas == null || !IsVisible) return;
        if (gameManager == null) gameManager = GameManager.Instance;
        flash = Mathf.MoveTowards(flash, 0f, Time.unscaledDeltaTime * 4f);
        damageOverlay.color = new Color(0.95f, 0.08f, 0.04f, flash * 0.1f);
        Cursor.visible = true;
        UpdateLock();
        if (Time.unscaledTime < nextRefresh) return;
        nextRefresh = Time.unscaledTime + 0.1f;
        if (playerStats != null)
        {
            hpText.text = "HP  " + Mathf.CeilToInt(playerStats.CurrentHp) + " / " + Mathf.CeilToInt(playerStats.MaxHp);
            energyText.text = "EN  " + Mathf.CeilToInt(playerStats.CurrentEnergy);
            SetFill(hpFill, playerStats.CurrentHp / Mathf.Max(1f, playerStats.MaxHp));
            SetFill(energyFill, playerStats.CurrentEnergy / Mathf.Max(1f, playerStats.MaxEnergy));
        }
        if (gameManager != null)
        {
            objectiveText.text = gameManager.ProgressText;
            statusText.text = "Kills " + gameManager.Kills + "    Hostiles " + (gameManager.stageManager != null ? gameManager.stageManager.EnemiesAlive : 0);
        }
        buildText.text = upgradeSystem != null ? upgradeSystem.GetSummary() : "";
        if (Time.unscaledTime >= nextBossLookup)
        {
            nextBossLookup = Time.unscaledTime + 0.25f;
            boss = FindFirstObjectByType<BossController>();
            bossDamageable = boss != null ? boss.GetComponent<Damageable>() : null;
        }
        bool showBoss = bossDamageable != null && !bossDamageable.IsDead;
        bossPanel.gameObject.SetActive(showBoss);
        if (showBoss)
        {
            bossText.text = "REACTOR WARDEN   /   " + (boss.CoreExposed ? "CORE EXPOSED" : boss.IsPhaseTwo ? "ARMORED II" : "ARMORED I");
            bossFill.GetComponent<Image>().color = boss.CoreExposed ? new Color(0.2f, 0.95f, 0.8f) : new Color(1f, 0.26f, 0.17f);
            SetFill(bossFill, bossDamageable.CurrentHealth / bossDamageable.maxHealth);
        }
    }

    private void BuildUI()
    {
        if (canvas != null) return;
        canvas = RuntimeUIFactory.CreateCanvas("CombatHUDCanvas", 960f);
        canvas.sortingOrder = 3;
        var overlay = RuntimeUIFactory.CreatePanel(canvas.transform, "DamageOverlay", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Color.clear);
        damageOverlay = overlay.GetComponent<Image>();
        damageOverlay.raycastTarget = false;
        safeRoot = SafeAreaLayout.Create(canvas);
        var plate = Panel(safeRoot, "StatusPanel", new Vector2(0, 1), new Vector2(16, -102), new Vector2(226, -14));
        hpText = Label(plate, "HpText", 18, new Vector2(10, -25), new Vector2(196, -4));
        hpFill = Bar(plate, "HpBar", new Vector2(10, -34), new Vector2(196, -29), new Color(0.25f, 0.93f, 0.56f));
        energyText = Label(plate, "EnergyText", 13, new Vector2(10, -58), new Vector2(196, -39));
        energyFill = Bar(plate, "EnergyBar", new Vector2(10, -65), new Vector2(196, -62), new Color(0.25f, 0.8f, 1f));
        statusText = Label(plate, "StatusText", 12, new Vector2(10, -85), new Vector2(198, -67));
        var objectiveBand = Panel(safeRoot, "ObjectiveBand", new Vector2(0.5f, 1), new Vector2(-204, -40), new Vector2(204, -10));
        objectiveText = Label(objectiveBand, "ObjectiveText", 14, new Vector2(8, -28), new Vector2(400, -2), null, TextAnchor.MiddleCenter);
        bossPanel = Panel(safeRoot, "BossPanel", new Vector2(0.5f, 1), new Vector2(-180, -82), new Vector2(180, -43));
        bossText = Label(bossPanel, "BossText", 13, new Vector2(10, -24), new Vector2(350, -3), null, TextAnchor.MiddleCenter);
        bossFill = Bar(bossPanel, "BossHealth", new Vector2(10, -33), new Vector2(350, -28), new Color(1f, 0.26f, 0.17f));
        bossPanel.gameObject.SetActive(false);
        var buildBand = Panel(safeRoot, "BuildBand", new Vector2(0.5f, 0), new Vector2(-230, 12), new Vector2(230, 56));
        buildText = Label(buildBand, "BuildText", 13, new Vector2(8, -42), new Vector2(452, -2), null, TextAnchor.MiddleCenter);
        var pause = RuntimeUIFactory.CreateButton(safeRoot, "PauseButton", "II");
        var pauseRect = pause.GetComponent<RectTransform>();
        pauseRect.anchorMin = pauseRect.anchorMax = new Vector2(1, 1);
        pauseRect.offsetMin = new Vector2(-64, -62);
        pauseRect.offsetMax = new Vector2(-16, -14);
        pause.onClick.AddListener(() => GameManager.Instance.TogglePause());
        lockRing = new GameObject("CombatCrosshair", typeof(RectTransform), typeof(ControlRingGraphic)).GetComponent<RectTransform>();
        lockRing.SetParent(safeRoot, false);
        lockRing.anchorMin = lockRing.anchorMax = new Vector2(0.5f, 0.5f);
        lockRing.sizeDelta = new Vector2(28, 28);
        var graphic = lockRing.GetComponent<ControlRingGraphic>();
        graphic.color = new Color(0.2f, 0.92f, 1f, 0.9f);
        graphic.thickness = 1.5f;
        graphic.raycastTarget = false;
    }

    private RectTransform Panel(Transform parent, string name, Vector2 anchor, Vector2 min, Vector2 max)
    {
        var rect = RuntimeUIFactory.CreatePanel(parent, name, anchor, anchor, min, max, new Color(0.025f, 0.07f, 0.09f, 0.96f));
        rect.GetComponent<Image>().raycastTarget = false;
        return rect;
    }

    private Text Label(Transform parent, string name, int size, Vector2 min, Vector2 max, Vector2? anchor = null, TextAnchor alignment = TextAnchor.MiddleLeft)
    {
        var text = RuntimeUIFactory.CreateText(parent, name, "", size, alignment, new Color(0.94f, 0.98f, 1f));
        text.raycastTarget = false;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        text.rectTransform.anchorMin = text.rectTransform.anchorMax = anchor ?? new Vector2(0, 1);
        text.rectTransform.offsetMin = min;
        text.rectTransform.offsetMax = max;
        return text;
    }

    private RectTransform Bar(Transform parent, string name, Vector2 min, Vector2 max, Color color)
    {
        var background = RuntimeUIFactory.CreatePanel(parent, name, new Vector2(0, 1), new Vector2(0, 1), min, max, new Color(0.12f, 0.18f, 0.2f));
        background.GetComponent<Image>().raycastTarget = false;
        var fill = RuntimeUIFactory.CreatePanel(background, "Fill", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, color);
        fill.GetComponent<Image>().raycastTarget = false;
        return fill;
    }

    private void UpdateLock()
    {
        var target = player != null && player.AutoAim != null ? player.AutoAim.CurrentTarget : null;
        lockRing.gameObject.SetActive(target != null && Camera.main != null && !gameManager.IsPaused);
        if (!lockRing.gameObject.activeSelf) return;
        Vector3 screen = Camera.main.WorldToScreenPoint(target.AimCenter);
        Camera uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(safeRoot, screen, uiCamera, out Vector2 point))
            lockRing.anchoredPosition = point;
    }

    private void HandleDamage(Damageable target, DamageInfo info) { flash = 1f; }
    private static void SetFill(RectTransform fill, float ratio)
    {
        fill.anchorMax = new Vector2(Mathf.Clamp01(ratio), 1f);
        fill.offsetMin = fill.offsetMax = Vector2.zero;
    }
}
