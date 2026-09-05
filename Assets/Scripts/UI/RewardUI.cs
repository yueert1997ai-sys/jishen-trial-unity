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
    private Text title;

    private void Update()
    {
        if (canvas == null || !canvas.gameObject.activeSelf)
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
        canvas.gameObject.SetActive(true);
        title.text = GameText.T("SELECT UPGRADE") + "   " + owner.CompletedEncounters + " / 6";
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
            canvas.gameObject.SetActive(false);
        }
    }

    private void Choose(int index)
    {
        if (gameManager == null || gameManager.Phase != GamePhase.Reward || canvas == null || !canvas.gameObject.activeSelf) return;
        if (index < 0 || index >= options.Count)
        {
            return;
        }

        if (upgradeSystem != null)
        {
            if (!upgradeSystem.ApplyOption(options[index])) return;
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

        canvas = RuntimeUIFactory.CreateCanvas("RewardCanvas", 960);
        canvas.sortingOrder = 30;
        panel = RuntimeUIFactory.CreateMenuSurface(canvas, "RewardPanel", new Vector2(820, 420));
        title = RuntimeUIFactory.MenuText(panel, "Title", "SELECT UPGRADE", 26, new Vector2(410, -42), new Vector2(760, 44));
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
            RectTransform card = RuntimeUIFactory.CreatePanel(panel, "RewardCard" + i, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(0.10f, 0.16f, 0.18f));
            RuntimeUIFactory.Place(card, new Vector2(0, 1), new Vector2(150 + i * 260, -242), new Vector2(244, 300));
            RuntimeUIFactory.MenuText(card, "UpgradeTitle", option.title, 21, new Vector2(122, -49), new Vector2(208, 68));
            RuntimeUIFactory.MenuText(card, "Text", option.description, 17, new Vector2(122, -162), new Vector2(208, 132), TextAnchor.UpperLeft);
            Button button = RuntimeUIFactory.MenuButton(card, "ChooseButton", "Install", new Vector2(122, -258), new Vector2(208, 52));
            int capturedIndex = i;
            button.onClick.AddListener(delegate { Choose(capturedIndex); });
        }
    }
}
