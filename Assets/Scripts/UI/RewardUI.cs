using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RewardUI : MonoBehaviour
{
    private Canvas canvas;
    private RectTransform panel;
    private readonly List<RunUpgradeOption> options = new List<RunUpgradeOption>();
    private GameManager gameManager;
    private RunUpgradeSystem upgradeSystem;

    private void Update()
    {
        if (panel == null || !panel.gameObject.activeSelf)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            Choose(0);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            Choose(1);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            Choose(2);
        }
    }

    public void Show(GameManager owner, RunUpgradeSystem system)
    {
        gameManager = owner;
        upgradeSystem = system;
        BuildUI();
        panel.gameObject.SetActive(true);
        options.Clear();
        if (upgradeSystem != null)
        {
            options.AddRange(upgradeSystem.GenerateOptions());
        }

        RefreshCards();
    }

    public void Hide()
    {
        if (panel != null)
        {
            panel.gameObject.SetActive(false);
        }
    }

    private void Choose(int index)
    {
        if (gameManager == null || gameManager.Phase != GamePhase.Reward || panel == null || !panel.gameObject.activeSelf) return;
        if (index < 0 || index >= options.Count)
        {
            return;
        }

        if (upgradeSystem != null)
        {
            upgradeSystem.ApplyOption(options[index]);
        }

        Hide();
        if (gameManager != null)
        {
            gameManager.FinishReward();
        }
    }

    private void BuildUI()
    {
        if (canvas != null)
        {
            return;
        }

        canvas = RuntimeUIFactory.CreateCanvas("RewardCanvas");
        panel = RuntimeUIFactory.CreatePanel(canvas.transform, "RewardPanel", new Vector2(0.12f, 0.16f), new Vector2(0.88f, 0.86f), Vector2.zero, Vector2.zero, new Color(0.02f, 0.04f, 0.07f, 0.94f));
        Text title = RuntimeUIFactory.CreateText(panel, "Title", "Choose an upgrade", 38, TextAnchor.UpperCenter, Color.white);
        RectTransform titleRect = title.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.anchoredPosition = new Vector2(0f, -28f);
        titleRect.sizeDelta = new Vector2(0f, 60f);
    }

    private void RefreshCards()
    {
        for (int i = panel.childCount - 1; i >= 0; i--)
        {
            Transform child = panel.GetChild(i);
            if (child.name.StartsWith("RewardCard"))
            {
                Destroy(child.gameObject);
            }
        }

        for (int i = 0; i < options.Count; i++)
        {
            RunUpgradeOption option = options[i];
            RectTransform card = RuntimeUIFactory.CreatePanel(panel, "RewardCard" + i, new Vector2(0.06f + i * 0.31f, 0.15f), new Vector2(0.29f + i * 0.31f, 0.78f), Vector2.zero, Vector2.zero, new Color(0.08f, 0.12f, 0.17f, 0.98f));
            Text text = RuntimeUIFactory.CreateText(card, "Text", (i + 1) + ". " + option.title + "\n\n" + option.description + "\n\nApplies immediately", 24, TextAnchor.UpperLeft, Color.white);
            RectTransform textRect = text.GetComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0.08f, 0.22f);
            textRect.anchorMax = new Vector2(0.92f, 0.92f);
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            Button button = RuntimeUIFactory.CreateButton(card, "ChooseButton", "Install");
            RectTransform buttonRect = button.GetComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(0.12f, 0.06f);
            buttonRect.anchorMax = new Vector2(0.88f, 0.18f);
            buttonRect.offsetMin = Vector2.zero;
            buttonRect.offsetMax = Vector2.zero;
            int capturedIndex = i;
            button.onClick.AddListener(delegate { Choose(capturedIndex); });
        }
    }
}
