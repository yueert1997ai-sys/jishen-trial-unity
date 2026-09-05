using UnityEngine;
using UnityEngine.UI;

public class HangarDeploymentUI : MonoBehaviour
{
    private readonly Button[] difficultyButtons = new Button[3];
    private Canvas canvas;
    private Text difficultyDetails;
    private GameManager gameManager;
    public bool IsVisible => canvas != null && canvas.gameObject.activeSelf;

    private void Awake() { BuildUI(); Hide(); }
    public void Show(GameManager owner) { gameManager = owner; BuildUI(); canvas.gameObject.SetActive(true); Refresh(); }
    public void Hide() { if (canvas != null) canvas.gameObject.SetActive(false); }

    public void Refresh()
    {
        if (gameManager == null || difficultyDetails == null) return;
        var difficulty = gameManager.Difficulty;
        difficultyDetails.text = GameText.T(difficulty == RunDifficulty.Cadet ? "DEMO\nEnemy armor 78%  /  Damage 65%\nField repair 20%  /  One continue"
            : difficulty == RunDifficulty.Standard ? "STANDARD\nEnemy armor 100%  /  Damage 100%\nField repair 12%  /  No continue"
            : "VETERAN\nEnemy armor 125%  /  Damage 120%\nField repair 8%  /  No continue");
        for (int i = 0; i < difficultyButtons.Length; i++)
            difficultyButtons[i].GetComponent<Image>().color = i == (int)difficulty
                ? new Color(0.04f, 0.5f, 0.58f) : new Color(0.13f, 0.19f, 0.21f);
    }

    private void BuildUI()
    {
        if (canvas != null) return;
        canvas = RuntimeUIFactory.CreateCanvas("HangarDeploymentCanvas", 960);
        canvas.sortingOrder = 70;
        var safe = SafeAreaLayout.Create(canvas);
        var title = RuntimeUIFactory.MenuText(safe, "GameTitle", "MECH TRIAL", 34, new Vector2(234, -50), new Vector2(420, 56));
        title.color = new Color(0.025f, 0.07f, 0.09f);
        title.fontStyle = FontStyle.Bold;
        var settings = RuntimeUIFactory.MenuButton(safe, "HangarSettingsButton", "\u2699", new Vector2(54, -118), new Vector2(56, 52));
        settings.GetComponentInChildren<Text>().fontSize = 28;
        settings.onClick.AddListener(() => gameManager.settingsUI.Show(gameManager));
        var band = RuntimeUIFactory.CreatePanel(safe, "DeploymentBand", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(0.045f, 0.075f, 0.09f, 1f));
        RuntimeUIFactory.Place(band, new Vector2(1, 0.5f), new Vector2(-210, 0), new Vector2(380, 440));
        RuntimeUIFactory.MenuText(band, "Eyebrow", "SORTIE 01", 14, new Vector2(190, -32), new Vector2(324, 28));
        RuntimeUIFactory.MenuText(band, "MechTitle", "ROSE GOLD\nSENTINEL", 28, new Vector2(190, -92), new Vector2(324, 90));
        RuntimeUIFactory.MenuText(band, "MissionRoute", "OUTER DECK  /  REACTOR  /  BOSS", 14, new Vector2(190, -154), new Vector2(324, 28));
        string[] labels = { "CADET", "STANDARD", "VETERAN" };
        for (int i = 0; i < 3; i++)
        {
            var button = RuntimeUIFactory.MenuButton(band, "Difficulty" + labels[i], i == 0 ? "DEMO" : labels[i], new Vector2(81 + i * 109, -210), new Vector2(103, 48));
            button.GetComponentInChildren<Text>().fontSize = 14;
            var difficulty = (RunDifficulty)i;
            button.onClick.AddListener(() => gameManager.SetDifficulty(difficulty));
            difficultyButtons[i] = button;
        }
        difficultyDetails = RuntimeUIFactory.MenuText(band, "DifficultyDetails", "", 16, new Vector2(190, -294), new Vector2(324, 96));
        RuntimeUIFactory.MenuButton(band, "DeployButton", "DEPLOY", new Vector2(190, -390), new Vector2(324, 56))
            .onClick.AddListener(() => gameManager.BeginRun());
    }
}
