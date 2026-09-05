using UnityEngine;
using UnityEngine.UI;

public class PauseUI : MonoBehaviour
{
    private Canvas canvas;
    private RectTransform panel;
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
    }

    public void Hide()
    {
        if (canvas != null)
        {
            canvas.gameObject.SetActive(false);
        }
    }

    private void Resume()
    {
        if (gameManager != null)
        {
            gameManager.SetPaused(false);
        }
    }

    private void Restart()
    {
        if (gameManager != null)
        {
            gameManager.RestartRun();
        }
    }

    private void BuildUI()
    {
        if (canvas != null)
        {
            return;
        }

        canvas = RuntimeUIFactory.CreateCanvas("PauseCanvas");
        canvas.sortingOrder = 120;
        RectTransform shade = RuntimeUIFactory.CreatePanel(canvas.transform, "PauseShade", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(0.005f, 0.012f, 0.02f, 0.78f));
        shade.GetComponent<Image>().raycastTarget = true;

        panel = RuntimeUIFactory.CreatePanel(shade, "PausePanel", new Vector2(0.35f, 0.27f), new Vector2(0.65f, 0.73f), Vector2.zero, Vector2.zero, new Color(0.025f, 0.055f, 0.08f, 0.98f));
        Text title = RuntimeUIFactory.CreateText(panel, "PauseTitle", "MISSION PAUSED", 38, TextAnchor.MiddleCenter, Color.white);
        RectTransform titleRect = title.rectTransform;
        titleRect.anchorMin = new Vector2(0.08f, 0.72f);
        titleRect.anchorMax = new Vector2(0.92f, 0.92f);
        titleRect.offsetMin = Vector2.zero;
        titleRect.offsetMax = Vector2.zero;

        Text status = RuntimeUIFactory.CreateText(panel, "PauseStatus", "TACTICAL LINK ON HOLD", 19, TextAnchor.MiddleCenter, new Color(0.2f, 0.82f, 1f));
        RectTransform statusRect = status.rectTransform;
        statusRect.anchorMin = new Vector2(0.08f, 0.61f);
        statusRect.anchorMax = new Vector2(0.92f, 0.72f);
        statusRect.offsetMin = Vector2.zero;
        statusRect.offsetMax = Vector2.zero;

        Button resumeButton = RuntimeUIFactory.CreateButton(panel, "ResumeButton", "Resume");
        RectTransform resumeRect = resumeButton.GetComponent<RectTransform>();
        resumeRect.anchorMin = new Vector2(0.18f, 0.35f);
        resumeRect.anchorMax = new Vector2(0.82f, 0.49f);
        resumeRect.offsetMin = Vector2.zero;
        resumeRect.offsetMax = Vector2.zero;
        resumeButton.onClick.AddListener(Resume);

        Button restartButton = RuntimeUIFactory.CreateButton(panel, "RestartButton", "Restart Run");
        RectTransform restartRect = restartButton.GetComponent<RectTransform>();
        restartRect.anchorMin = new Vector2(0.18f, 0.16f);
        restartRect.anchorMax = new Vector2(0.82f, 0.30f);
        restartRect.offsetMin = Vector2.zero;
        restartRect.offsetMax = Vector2.zero;
        restartButton.onClick.AddListener(Restart);
    }
}
