using UnityEngine;
using UnityEngine.UI;

public class PauseUI : MonoBehaviour
{
    private Canvas canvas;
    private GameManager gameManager;
    public bool IsVisible => canvas != null && canvas.gameObject.activeSelf;

    private void Awake() { BuildUI(); Hide(); }
    public void Show(GameManager owner) { gameManager = owner; BuildUI(); canvas.gameObject.SetActive(true); }
    public void Hide() { if (canvas != null) canvas.gameObject.SetActive(false); }

    private void BuildUI()
    {
        if (canvas != null) return;
        canvas = RuntimeUIFactory.CreateCanvas("PauseCanvas", 960);
        canvas.sortingOrder = 120;
        var panel = RuntimeUIFactory.CreateMenuSurface(canvas, "PausePanel", new Vector2(360, 450));
        RuntimeUIFactory.MenuText(panel, "PauseTitle", "MISSION PAUSED", 26, new Vector2(180, -48), new Vector2(320, 44), TextAnchor.MiddleCenter);
        RuntimeUIFactory.MenuButton(panel, "ResumeButton", "Resume", new Vector2(180, -136), new Vector2(280, 56))
            .onClick.AddListener(() => gameManager.SetPaused(false));
        RuntimeUIFactory.MenuButton(panel, "PauseSettingsButton", "Settings", new Vector2(180, -210), new Vector2(280, 56))
            .onClick.AddListener(() => gameManager.settingsUI.Show(gameManager));
        RuntimeUIFactory.MenuButton(panel, "RecoverButton", "脱困 / F4", new Vector2(180, -286), new Vector2(280, 56))
            .onClick.AddListener(() => gameManager.playerController.GetComponent<CombatRecovery>().TryRecover(true));
        RuntimeUIFactory.MenuButton(panel, "RestartButton", "Return to hangar", new Vector2(180, -360), new Vector2(280, 56))
            .onClick.AddListener(() => gameManager.RestartRun());
    }
}
