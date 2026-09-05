using UnityEngine;
using UnityEngine.UI;

public class ResultUI : MonoBehaviour
{
    private Canvas canvas;
    private Text title;
    private Text resultText;
    private GameManager gameManager;

    private void Update()
    {
        if (canvas != null && canvas.gameObject.activeSelf && Input.GetKeyDown(KeyCode.R))
            gameManager.RestartRun();
    }

    public void Show(GameManager owner, bool victory)
    {
        gameManager = owner;
        BuildUI();
        canvas.gameObject.SetActive(true);
        title.text = victory ? "MISSION COMPLETE" : "MISSION FAILED";
        title.color = victory ? new Color(0.3f, 0.95f, 0.73f) : new Color(1f, 0.48f, 0.36f);
        int seconds = Mathf.CeilToInt(gameManager.GetRunTime());
        resultText.text = "Kills  " + gameManager.Kills + "     Salvage  " + gameManager.Coins
            + "\n" + gameManager.DifficultyDisplayName + "     Time  " + (seconds / 60).ToString("00") + ":" + (seconds % 60).ToString("00")
            + "\n\n" + (gameManager.upgradeSystem != null ? gameManager.upgradeSystem.GetSummary() : "");
    }

    public void Hide() { if (canvas != null) canvas.gameObject.SetActive(false); }

    private void BuildUI()
    {
        if (canvas != null) return;
        canvas = RuntimeUIFactory.CreateCanvas("ResultCanvas", 960);
        canvas.sortingOrder = 30;
        var panel = RuntimeUIFactory.CreateMenuSurface(canvas, "ResultPanel", new Vector2(640, 420));
        title = RuntimeUIFactory.MenuText(panel, "ResultTitle", "", 28, new Vector2(320, -54), new Vector2(560, 48));
        resultText = RuntimeUIFactory.MenuText(panel, "ResultText", "", 18, new Vector2(320, -220), new Vector2(560, 246), TextAnchor.UpperLeft);
        RuntimeUIFactory.MenuButton(panel, "ReturnHangarButton", "Return to hangar", new Vector2(320, -370), new Vector2(560, 56))
            .onClick.AddListener(() => gameManager.RestartRun());
    }
}
