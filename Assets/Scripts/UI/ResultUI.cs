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
            gameManager.ReplayCurrentRun();
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
            + "\n\n" + gameManager.LastRunUpgradeSummary;
        if (owner.equipmentLoop != null)
            resultText.text += "\n\n" + EquipmentWarehouseUI.T("本局新装备已永久入库：", "New equipment kept: ") + owner.equipmentLoop.NewThisRun
                + "\n" + EquipmentWarehouseUI.T("本局 buff 已清空。下次出击重新随机。", "Run buffs cleared. Next sortie starts fresh.");
    }

    public void Hide() { if (canvas != null) canvas.gameObject.SetActive(false); }

    private void BuildUI()
    {
        if (canvas != null) return;
        canvas = RuntimeUIFactory.CreateCanvas("ResultCanvas", 960);
        canvas.sortingOrder = 30;
        var panel = RuntimeUIFactory.CreateMenuSurface(canvas, "ResultPanel", new Vector2(640, 420));
        title = RuntimeUIFactory.MenuText(panel, "ResultTitle", "", 28, new Vector2(320, -54), new Vector2(560, 48));
        var hangar = RuntimeUIFactory.MenuButton(panel,"ResultHangarButton","出击准备",new Vector2(544,-22),new Vector2(124,32));
        hangar.GetComponentInChildren<Text>().fontSize=14;
        hangar.onClick.AddListener(()=>gameManager.EnterHangar());
        resultText = RuntimeUIFactory.MenuText(panel, "ResultText", "", 18, new Vector2(320, -220), new Vector2(560, 246), TextAnchor.UpperLeft);
        continueButton = RuntimeUIFactory.MenuButton(panel, "ContinueRunButton", "Retry sector (1)", new Vector2(178, -370), new Vector2(276, 56));
        continueButton.onClick.AddListener(() => gameManager.ContinueRun());
        returnButton = RuntimeUIFactory.MenuButton(panel, "ReturnHangarButton", "重新出击 [R]", new Vector2(320, -370), new Vector2(560, 56));
        returnButton.onClick.AddListener(() => gameManager.ReplayCurrentRun());
    }
}
