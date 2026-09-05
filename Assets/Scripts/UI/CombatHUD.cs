using System.Text;
using UnityEngine;
using UnityEngine.UI;

public class CombatHUD : MonoBehaviour
{
    public GameManager gameManager;
    public PlayerStats playerStats;
    public EquipmentManager equipmentManager;
    public RunUpgradeSystem upgradeSystem;

    private Canvas canvas;
    private RectTransform panel;
    private Text statusText;
    private Text hpText;
    private Text energyText;
    private Text dashText;
    private Text objectiveText;
    private Text announcementText;
    private Text bossText;
    private RectTransform hpFill;
    private RectTransform energyFill;
    private RectTransform bossPanel;
    private RectTransform bossFill;
    private RectTransform crosshairRoot;
    private Image hpFillImage;
    private Image damageOverlay;
    private PlayerController playerController;
    private Damageable playerDamageable;
    private float damageFlash;
    private float announcementTimer;
    private float nextBossLookupTime;
    private string lastProgressText;
    private BossController trackedBoss;
    private Damageable bossDamageable;

    public bool IsVisible
    {
        get { return canvas != null && canvas.gameObject.activeSelf; }
    }

    private void Start()
    {
        BuildUI();
        playerController = playerStats != null ? playerStats.GetComponent<PlayerController>() : null;
        playerDamageable = playerStats != null ? playerStats.GetComponent<Damageable>() : null;
        if (playerDamageable != null)
        {
            playerDamageable.OnDamaged += HandlePlayerDamaged;
        }

        if (gameManager != null && gameManager.Phase == GamePhase.Hangar)
        {
            SetVisible(false);
        }
    }

    private void OnDestroy()
    {
        if (playerDamageable != null)
        {
            playerDamageable.OnDamaged -= HandlePlayerDamaged;
        }

        Cursor.visible = true;
    }

    private void Update()
    {
        if (statusText == null)
        {
            return;
        }

        if (gameManager == null)
        {
            gameManager = GameManager.Instance;
        }

        StringBuilder builder = new StringBuilder();
        if (playerStats != null)
        {
            int hpCurrent = Mathf.CeilToInt(playerStats.CurrentHp);
            int hpMax = Mathf.CeilToInt(playerStats.MaxHp);
            int energyCurrent = Mathf.CeilToInt(playerStats.CurrentEnergy);
            int energyMax = Mathf.CeilToInt(playerStats.MaxEnergy);
            if (hpText != null)
            {
                hpText.text = "HP " + hpCurrent + " / " + hpMax;
            }

            if (energyText != null)
            {
                energyText.text = "EN " + energyCurrent + " / " + energyMax;
            }

            SetFill(hpFill, hpMax > 0 ? playerStats.CurrentHp / playerStats.MaxHp : 0f);
            SetFill(energyFill, energyMax > 0 ? playerStats.CurrentEnergy / playerStats.MaxEnergy : 0f);
            if (hpFillImage != null)
            {
                float hpRatio = hpMax > 0 ? playerStats.CurrentHp / playerStats.MaxHp : 0f;
                hpFillImage.color = Color.Lerp(new Color(1f, 0.05f, 0.02f), new Color(0.15f, 0.9f, 0.38f), hpRatio);
            }
        }

        if (gameManager != null)
        {
            builder.AppendLine("Difficulty " + gameManager.DifficultyDisplayName);
            builder.AppendLine("Kills " + gameManager.Kills + "    Salvage " + gameManager.Coins);
            if (gameManager.Phase == GamePhase.Combat && gameManager.stageManager != null)
            {
                builder.AppendLine("Hostiles " + gameManager.stageManager.EnemiesAlive);
            }

            if (objectiveText != null)
            {
                objectiveText.text = gameManager.ProgressText;
            }

            string progress = gameManager.ProgressText ?? "";
            if (progress != lastProgressText)
            {
                lastProgressText = progress;
                if (gameManager.Phase == GamePhase.Combat && announcementText != null)
                {
                    announcementText.text = progress.ToUpperInvariant();
                    announcementText.color = Color.white;
                    announcementText.rectTransform.localScale = Vector3.one * 1.06f;
                    announcementText.gameObject.SetActive(true);
                    announcementTimer = 1.35f;
                }
            }
        }

        UpdateBossHud();
        UpdateCrosshair();

        if (dashText != null && playerController != null && playerStats != null)
        {
            if (!playerController.IsDashReady)
            {
                dashText.text = "DASH  " + playerController.DashCooldownRemaining.ToString("0.0") + "s";
                dashText.color = new Color(0.65f, 0.72f, 0.78f);
            }
            else if (playerStats.CurrentEnergy < 25f)
            {
                dashText.text = "DASH  LOW ENERGY";
                dashText.color = new Color(1f, 0.55f, 0.18f);
            }
            else
            {
                dashText.text = "DASH  READY";
                dashText.color = new Color(0.15f, 0.85f, 1f);
            }
        }

        if (upgradeSystem != null)
        {
            builder.AppendLine(upgradeSystem.GetSummary());
        }

        if (equipmentManager != null && equipmentManager.GetEquippedItems().Count > 0)
        {
            builder.AppendLine("Equipment");
            foreach (EquipmentManager.EquippedItem item in equipmentManager.GetEquippedItems())
            {
                if (item.data != null)
                {
                    builder.AppendLine("- " + item.data.displayName + " Lv" + item.level);
                }
            }
        }

        statusText.text = builder.ToString();

        if (damageOverlay != null)
        {
            damageFlash = Mathf.MoveTowards(damageFlash, 0f, Time.unscaledDeltaTime * 2.8f);
            Color overlayColor = damageOverlay.color;
            overlayColor.a = damageFlash * 0.3f;
            damageOverlay.color = overlayColor;
        }

        if (announcementText != null && announcementText.gameObject.activeSelf)
        {
            announcementTimer -= Time.unscaledDeltaTime;
            Color announcementColor = announcementText.color;
            announcementColor.a = Mathf.Clamp01(announcementTimer * 2.4f);
            announcementText.color = announcementColor;
            announcementText.rectTransform.localScale = Vector3.one * (1f + Mathf.Clamp01(announcementTimer) * 0.06f);
            if (announcementTimer <= 0f)
            {
                announcementText.gameObject.SetActive(false);
            }
        }
    }

    public void SetVisible(bool value)
    {
        if (!value)
        {
            damageFlash = 0f;
            announcementTimer = 0f;
            if (announcementText != null) announcementText.gameObject.SetActive(false);
        }
        if (canvas != null)
        {
            canvas.gameObject.SetActive(value);
        }
    }

    private void BuildUI()
    {
        if (canvas != null)
        {
            return;
        }

        canvas = RuntimeUIFactory.CreateCanvas("CombatHUDCanvas");

        panel = RuntimeUIFactory.CreatePanel(canvas.transform, "StatusPanel", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24f, -354f), new Vector2(500f, -24f), new Color(0.02f, 0.035f, 0.05f, 0.82f));

        RectTransform objectivePanel = RuntimeUIFactory.CreatePanel(canvas.transform, "ObjectivePanel", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-310f, -82f), new Vector2(310f, -24f), new Color(0.02f, 0.035f, 0.05f, 0.78f));
        objectiveText = RuntimeUIFactory.CreateText(objectivePanel, "ObjectiveText", "Hangar ready", 26, TextAnchor.MiddleCenter, Color.white);
        RectTransform objectiveRect = objectiveText.GetComponent<RectTransform>();
        objectiveRect.anchorMin = Vector2.zero;
        objectiveRect.anchorMax = Vector2.one;
        objectiveRect.offsetMin = new Vector2(12f, 4f);
        objectiveRect.offsetMax = new Vector2(-12f, -4f);

        bossPanel = RuntimeUIFactory.CreatePanel(canvas.transform, "BossPanel", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-360f, -146f), new Vector2(360f, -92f), new Color(0.055f, 0.018f, 0.025f, 0.92f));
        bossText = RuntimeUIFactory.CreateText(bossPanel, "BossText", "BOSS", 21, TextAnchor.MiddleCenter, Color.white);
        RectTransform bossTextRect = bossText.rectTransform;
        bossTextRect.anchorMin = new Vector2(0f, 0.38f);
        bossTextRect.anchorMax = Vector2.one;
        bossTextRect.offsetMin = new Vector2(12f, 0f);
        bossTextRect.offsetMax = new Vector2(-12f, -2f);
        RectTransform bossBarBack = RuntimeUIFactory.CreatePanel(bossPanel, "BossBarBack", new Vector2(0.025f, 0.12f), new Vector2(0.975f, 0.36f), Vector2.zero, Vector2.zero, new Color(0.18f, 0.02f, 0.035f, 1f));
        bossFill = RuntimeUIFactory.CreatePanel(bossBarBack, "BossBarFill", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(1f, 0.08f, 0.16f, 1f));
        bossPanel.gameObject.SetActive(false);

        announcementText = RuntimeUIFactory.CreateText(canvas.transform, "WaveAnnouncement", "", 42, TextAnchor.MiddleCenter, Color.white);
        RectTransform announcementRect = announcementText.GetComponent<RectTransform>();
        announcementRect.anchorMin = new Vector2(0.18f, 0.72f);
        announcementRect.anchorMax = new Vector2(0.82f, 0.84f);
        announcementRect.offsetMin = Vector2.zero;
        announcementRect.offsetMax = Vector2.zero;
        Outline announcementOutline = announcementText.gameObject.AddComponent<Outline>();
        announcementOutline.effectColor = new Color(0f, 0.05f, 0.08f, 0.9f);
        announcementOutline.effectDistance = new Vector2(2f, -2f);
        announcementText.gameObject.SetActive(false);

        hpText = RuntimeUIFactory.CreateText(panel, "HpText", "HP", 26, TextAnchor.MiddleLeft, Color.white);
        RectTransform hpTextRect = hpText.GetComponent<RectTransform>();
        hpTextRect.anchorMin = new Vector2(0f, 1f);
        hpTextRect.anchorMax = new Vector2(1f, 1f);
        hpTextRect.anchoredPosition = new Vector2(18f, -28f);
        hpTextRect.sizeDelta = new Vector2(-36f, 34f);

        RectTransform hpBarBack = RuntimeUIFactory.CreatePanel(panel, "HpBarBack", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(18f, -72f), new Vector2(-18f, -48f), new Color(0.16f, 0.03f, 0.035f, 0.95f));
        hpFill = RuntimeUIFactory.CreatePanel(hpBarBack, "HpBarFill", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(0.95f, 0.12f, 0.1f, 1f));
        hpFillImage = hpFill.GetComponent<Image>();

        energyText = RuntimeUIFactory.CreateText(panel, "EnergyText", "EN", 22, TextAnchor.MiddleLeft, Color.white);
        RectTransform energyTextRect = energyText.GetComponent<RectTransform>();
        energyTextRect.anchorMin = new Vector2(0f, 1f);
        energyTextRect.anchorMax = new Vector2(1f, 1f);
        energyTextRect.anchoredPosition = new Vector2(18f, -100f);
        energyTextRect.sizeDelta = new Vector2(-36f, 30f);

        RectTransform energyBarBack = RuntimeUIFactory.CreatePanel(panel, "EnergyBarBack", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(18f, -138f), new Vector2(-18f, -116f), new Color(0.02f, 0.1f, 0.14f, 0.95f));
        energyFill = RuntimeUIFactory.CreatePanel(energyBarBack, "EnergyBarFill", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(0.1f, 0.75f, 1f, 1f));

        dashText = RuntimeUIFactory.CreateText(panel, "DashText", "DASH  READY", 20, TextAnchor.MiddleLeft, new Color(0.15f, 0.85f, 1f));
        RectTransform dashRect = dashText.GetComponent<RectTransform>();
        dashRect.anchorMin = new Vector2(0f, 1f);
        dashRect.anchorMax = new Vector2(1f, 1f);
        dashRect.anchoredPosition = new Vector2(18f, -164f);
        dashRect.sizeDelta = new Vector2(-36f, 28f);

        statusText = RuntimeUIFactory.CreateText(panel, "StatusText", "", 21, TextAnchor.UpperLeft, Color.white);
        RectTransform rect = statusText.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.offsetMin = new Vector2(18f, 12f);
        rect.offsetMax = new Vector2(-18f, -190f);

        RectTransform overlayRect = RuntimeUIFactory.CreatePanel(canvas.transform, "DamageOverlay", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(0.9f, 0.02f, 0.01f, 0f));
        damageOverlay = overlayRect.GetComponent<Image>();
        damageOverlay.raycastTarget = false;

        GameObject crosshairObject = new GameObject("CombatCrosshair", typeof(RectTransform));
        crosshairObject.transform.SetParent(canvas.transform, false);
        crosshairRoot = crosshairObject.GetComponent<RectTransform>();
        crosshairRoot.anchorMin = new Vector2(0.5f, 0.5f);
        crosshairRoot.anchorMax = new Vector2(0.5f, 0.5f);
        crosshairRoot.sizeDelta = new Vector2(42f, 42f);
        crosshairRoot.anchoredPosition = Vector2.zero;
        Color crosshairColor = new Color(0.12f, 0.86f, 1f, 0.94f);
        CreateCrosshairPart(crosshairRoot, "CrosshairTop", new Vector2(-1.5f, 9f), new Vector2(1.5f, 18f), crosshairColor);
        CreateCrosshairPart(crosshairRoot, "CrosshairBottom", new Vector2(-1.5f, -18f), new Vector2(1.5f, -9f), crosshairColor);
        CreateCrosshairPart(crosshairRoot, "CrosshairLeft", new Vector2(-18f, -1.5f), new Vector2(-9f, 1.5f), crosshairColor);
        CreateCrosshairPart(crosshairRoot, "CrosshairRight", new Vector2(9f, -1.5f), new Vector2(18f, 1.5f), crosshairColor);
        CreateCrosshairPart(crosshairRoot, "CrosshairDot", new Vector2(-2f, -2f), new Vector2(2f, 2f), Color.white);
        crosshairRoot.gameObject.SetActive(false);
    }

    private void UpdateBossHud()
    {
        if (bossPanel == null)
        {
            return;
        }

        if (trackedBoss == null && Time.unscaledTime >= nextBossLookupTime)
        {
            nextBossLookupTime = Time.unscaledTime + 0.25f;
            trackedBoss = FindFirstObjectByType<BossController>();
            bossDamageable = trackedBoss != null ? trackedBoss.GetComponent<Damageable>() : null;
        }

        bool showBoss = gameManager != null && gameManager.Phase == GamePhase.Combat && trackedBoss != null && bossDamageable != null && !bossDamageable.IsDead;
        bossPanel.gameObject.SetActive(showBoss);
        if (!showBoss)
        {
            if (trackedBoss == null)
            {
                bossDamageable = null;
            }

            return;
        }

        int current = Mathf.CeilToInt(bossDamageable.CurrentHealth);
        int maximum = Mathf.CeilToInt(bossDamageable.maxHealth);
        bossText.text = "BOSS // " + current + " / " + maximum + " // PHASE " + (trackedBoss.IsPhaseTwo ? "2" : "1");
        SetFill(bossFill, maximum > 0 ? bossDamageable.CurrentHealth / bossDamageable.maxHealth : 0f);
    }

    private void UpdateCrosshair()
    {
        if (crosshairRoot == null || canvas == null)
        {
            return;
        }

        bool showCrosshair = gameManager != null && gameManager.Phase == GamePhase.Combat && !gameManager.IsPaused;
        crosshairRoot.gameObject.SetActive(showCrosshair);
        Cursor.visible = !showCrosshair;
        if (!showCrosshair)
        {
            return;
        }

        RectTransform canvasRect = canvas.transform as RectTransform;
        Camera uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        Vector2 localPoint;
        if (canvasRect != null && RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, Input.mousePosition, uiCamera, out localPoint))
        {
            crosshairRoot.anchoredPosition = localPoint;
        }
    }

    private static void CreateCrosshairPart(Transform parent, string name, Vector2 offsetMin, Vector2 offsetMax, Color color)
    {
        RectTransform part = RuntimeUIFactory.CreatePanel(parent, name, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), offsetMin, offsetMax, color);
        part.GetComponent<Image>().raycastTarget = false;
    }

    private void HandlePlayerDamaged(Damageable target, DamageInfo info)
    {
        damageFlash = 1f;
    }

    private static void SetFill(RectTransform fill, float value)
    {
        if (fill == null)
        {
            return;
        }

        Vector2 anchorMax = fill.anchorMax;
        anchorMax.x = Mathf.Clamp01(value);
        fill.anchorMax = anchorMax;
        fill.offsetMin = Vector2.zero;
        fill.offsetMax = Vector2.zero;
    }
}
