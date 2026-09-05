using UnityEngine;
using UnityEngine.UI;

public class HangarDeploymentUI : MonoBehaviour
{
    private readonly Button[] difficultyButtons = new Button[3];
    private Canvas canvas;
    private Text difficultyTitle;
    private Text difficultyDetails;
    private GameManager gameManager;

    public bool IsVisible
    {
        get { return canvas != null && canvas.gameObject.activeSelf; }
    }

    private void Awake()
    {
        BuildUI();
        Hide();
    }

    public void Show(GameManager owner)
    {
        gameManager = owner;
        BuildUI();
        canvas.gameObject.SetActive(true);
        Refresh();
    }

    public void Hide()
    {
        if (canvas != null)
        {
            canvas.gameObject.SetActive(false);
        }
    }

    public void Refresh()
    {
        if (gameManager == null || difficultyTitle == null)
        {
            return;
        }

        RunDifficulty difficulty = gameManager.Difficulty;
        difficultyTitle.text = difficulty == RunDifficulty.Cadet ? "CADET // RECOMMENDED" : difficulty == RunDifficulty.Veteran ? "VETERAN" : "STANDARD";
        difficultyDetails.text = GetDifficultyDetails(difficulty);

        for (int i = 0; i < difficultyButtons.Length; i++)
        {
            Button button = difficultyButtons[i];
            if (button == null)
            {
                continue;
            }

            bool selected = i == (int)difficulty;
            Image image = button.GetComponent<Image>();
            if (image != null)
            {
                image.color = selected ? new Color(0.04f, 0.55f, 0.76f, 1f) : new Color(0.075f, 0.12f, 0.16f, 0.98f);
            }

            Text label = button.GetComponentInChildren<Text>();
            if (label != null)
            {
                label.color = selected ? Color.white : new Color(0.65f, 0.72f, 0.78f);
            }
        }
    }

    private void BuildUI()
    {
        if (canvas != null)
        {
            return;
        }

        canvas = RuntimeUIFactory.CreateCanvas("HangarDeploymentCanvas");
        canvas.sortingOrder = 70;

        RectTransform rightBand = RuntimeUIFactory.CreatePanel(canvas.transform, "DeploymentBand", new Vector2(0.62f, 0.04f), new Vector2(0.98f, 0.96f), Vector2.zero, Vector2.zero, new Color(0.015f, 0.03f, 0.045f, 0.96f));
        RectTransform cyanLine = RuntimeUIFactory.CreatePanel(rightBand, "CyanLine", new Vector2(0f, 0f), new Vector2(0.008f, 1f), Vector2.zero, Vector2.zero, new Color(0.08f, 0.75f, 1f, 1f));
        cyanLine.GetComponent<Image>().raycastTarget = false;

        Text eyebrow = RuntimeUIFactory.CreateText(rightBand, "Eyebrow", "SORTIE CONTROL // 01", 18, TextAnchor.MiddleLeft, new Color(0.12f, 0.82f, 1f));
        SetRect(eyebrow.rectTransform, new Vector2(0.08f, 0.88f), new Vector2(0.92f, 0.94f));

        Text title = RuntimeUIFactory.CreateText(rightBand, "MechTitle", "ROSE GOLD SENTINEL", 36, TextAnchor.MiddleLeft, Color.white);
        SetRect(title.rectTransform, new Vector2(0.08f, 0.79f), new Vector2(0.92f, 0.88f));

        Text route = RuntimeUIFactory.CreateText(rightBand, "MissionRoute", "STAGE 01   >   UPGRADE   >   STAGE 02   >   BOSS", 17, TextAnchor.MiddleLeft, new Color(0.65f, 0.72f, 0.78f));
        SetRect(route.rectTransform, new Vector2(0.08f, 0.72f), new Vector2(0.92f, 0.78f));

        Text section = RuntimeUIFactory.CreateText(rightBand, "DifficultySection", "OPERATION PROFILE", 18, TextAnchor.MiddleLeft, new Color(0.12f, 0.82f, 1f));
        SetRect(section.rectTransform, new Vector2(0.08f, 0.63f), new Vector2(0.92f, 0.69f));

        string[] labels = { "CADET", "STANDARD", "VETERAN" };
        for (int i = 0; i < difficultyButtons.Length; i++)
        {
            Button button = RuntimeUIFactory.CreateButton(rightBand, "Difficulty" + labels[i], labels[i]);
            RectTransform rect = button.GetComponent<RectTransform>();
            float left = 0.08f + i * 0.285f;
            SetRect(rect, new Vector2(left, 0.54f), new Vector2(left + 0.265f, 0.62f));
            Text buttonLabel = button.GetComponentInChildren<Text>();
            if (buttonLabel != null)
            {
                buttonLabel.fontSize = 18;
            }

            RunDifficulty captured = (RunDifficulty)i;
            button.onClick.AddListener(delegate { SelectDifficulty(captured); });
            difficultyButtons[i] = button;
        }

        difficultyTitle = RuntimeUIFactory.CreateText(rightBand, "DifficultyTitle", "", 25, TextAnchor.MiddleLeft, Color.white);
        SetRect(difficultyTitle.rectTransform, new Vector2(0.08f, 0.44f), new Vector2(0.92f, 0.51f));

        difficultyDetails = RuntimeUIFactory.CreateText(rightBand, "DifficultyDetails", "", 20, TextAnchor.UpperLeft, new Color(0.78f, 0.84f, 0.88f));
        SetRect(difficultyDetails.rectTransform, new Vector2(0.08f, 0.25f), new Vector2(0.92f, 0.44f));

        Button deployButton = RuntimeUIFactory.CreateButton(rightBand, "DeployButton", "DEPLOY");
        SetRect(deployButton.GetComponent<RectTransform>(), new Vector2(0.08f, 0.09f), new Vector2(0.92f, 0.20f));
        Text deployLabel = deployButton.GetComponentInChildren<Text>();
        if (deployLabel != null)
        {
            deployLabel.fontSize = 28;
        }

        Image deployImage = deployButton.GetComponent<Image>();
        if (deployImage != null)
        {
            deployImage.color = new Color(0.04f, 0.48f, 0.68f, 1f);
        }

        deployButton.onClick.AddListener(Deploy);
    }

    private void SelectDifficulty(RunDifficulty difficulty)
    {
        if (gameManager != null)
        {
            gameManager.SetDifficulty(difficulty);
        }
    }

    private void Deploy()
    {
        if (gameManager != null)
        {
            gameManager.BeginRun();
        }
    }

    private static string GetDifficultyDetails(RunDifficulty difficulty)
    {
        if (difficulty == RunDifficulty.Veteran)
        {
            return "ENEMY ARMOR     125%\nINCOMING DAMAGE  120%\nFIELD REPAIR       8%\n\nFor experienced pilots seeking pressure.";
        }

        if (difficulty == RunDifficulty.Standard)
        {
            return "ENEMY ARMOR     100%\nINCOMING DAMAGE  100%\nFIELD REPAIR      12%\n\nThe intended baseline combat balance.";
        }

        return "ENEMY ARMOR      78%\nINCOMING DAMAGE   65%\nFIELD REPAIR      20%\n\nMore room to learn weapons and Boss patterns.";
    }

    private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
