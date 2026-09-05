using System.Text;
using UnityEngine;
using UnityEngine.UI;

public class ResultUI : MonoBehaviour
{
    private Canvas canvas;
    private RectTransform panel;
    private Text resultText;
    private GameManager gameManager;

    private void Update()
    {
        if (panel != null && panel.gameObject.activeSelf && Input.GetKeyDown(KeyCode.R))
        {
            if (gameManager != null)
            {
                gameManager.RestartRun();
            }
        }
    }

    public void Show(GameManager owner, bool victory)
    {
        gameManager = owner;
        BuildUI();
        panel.gameObject.SetActive(true);

        StringBuilder builder = new StringBuilder();
        builder.AppendLine(victory ? "Victory" : "Mission failed");
        builder.AppendLine("");
        builder.AppendLine("Kills: " + gameManager.Kills);
        builder.AppendLine("Coins: " + gameManager.Coins);
        builder.AppendLine("Difficulty: " + gameManager.DifficultyDisplayName);
        builder.AppendLine("Clear time: " + Mathf.CeilToInt(gameManager.GetRunTime()) + "s");
        builder.AppendLine("");
        builder.AppendLine("Upgrades:");
        if (gameManager.upgradeSystem != null)
        {
            builder.AppendLine(gameManager.upgradeSystem.GetSummary());
        }
        else if (gameManager.equipmentManager != null)
        {
            foreach (EquipmentManager.EquippedItem item in gameManager.equipmentManager.GetEquippedItems())
            {
                if (item.data != null)
                {
                    builder.AppendLine("- " + item.data.displayName + " Lv" + item.level);
                }
            }
        }

        builder.AppendLine("");
        builder.AppendLine("Press R to restart");
        resultText.text = builder.ToString();
    }

    public void Hide()
    {
        if (panel != null)
        {
            panel.gameObject.SetActive(false);
        }
    }

    private void BuildUI()
    {
        if (canvas != null)
        {
            return;
        }

        canvas = RuntimeUIFactory.CreateCanvas("ResultCanvas");
        panel = RuntimeUIFactory.CreatePanel(canvas.transform, "ResultPanel", new Vector2(0.28f, 0.18f), new Vector2(0.72f, 0.84f), Vector2.zero, Vector2.zero, new Color(0.02f, 0.04f, 0.07f, 0.96f));
        resultText = RuntimeUIFactory.CreateText(panel, "ResultText", "", 30, TextAnchor.UpperLeft, Color.white);
        RectTransform textRect = resultText.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.08f, 0.08f);
        textRect.anchorMax = new Vector2(0.92f, 0.92f);
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
    }
}
