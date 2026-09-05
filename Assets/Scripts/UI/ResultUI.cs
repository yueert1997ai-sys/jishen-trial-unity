using UnityEngine;
using UnityEngine.UI;

public class ResultUI : MonoBehaviour
{
    private Canvas canvas;
    private Text title;
    private Text resultText;
    private GameManager gameManager;
    private Button continueButton;
    private Button returnButton;

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
        continueButton.gameObject.SetActive(owner.CanContinue);
        RuntimeUIFactory.Place(returnButton.GetComponent<RectTransform>(), new Vector2(0, 1),
            new Vector2(owner.CanContinue ? 462 : 320, -370), new Vector2(owner.CanContinue ? 276 : 560, 56));
        if (owner.CanContinue)
            continueButton.GetComponentInChildren<Text>().text = GameText.T(owner.CheckpointEncounter == 6 ? "Retry Boss (1)" : "Retry sector (1)");
        title.text = GameText.T(victory ? "MISSION COMPLETE" : "MISSION FAILED");
        title.color = victory ? new Color(0.3f, 0.95f, 0.73f) : new Color(1f, 0.48f, 0.36f);
        int seconds = Mathf.CeilToInt(gameManager.GetRunTime());
        resultText.text = GameText.T("Kills") + "  " + gameManager.Kills + "     " + GameText.T("Salvage") + "  " + gameManager.Coins
            + "\n" + gameManager.DifficultyDisplayName + "     " + GameText.T("Time") + "  " + (seconds / 60).ToString("00") + ":" + (seconds % 60).ToString("00")
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
        continueButton = RuntimeUIFactory.MenuButton(panel, "ContinueRunButton", "Retry sector (1)", new Vector2(178, -370), new Vector2(276, 56));
        continueButton.onClick.AddListener(() => gameManager.ContinueRun());
        returnButton = RuntimeUIFactory.MenuButton(panel, "ReturnHangarButton", "Return to hangar", new Vector2(320, -370), new Vector2(560, 56));
        returnButton.onClick.AddListener(() => gameManager.RestartRun());
    }
}
