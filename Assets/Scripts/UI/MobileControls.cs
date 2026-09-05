using UnityEngine;
using UnityEngine.UI;

public class MobileControls : MonoBehaviour
{
    public VirtualJoystick Joystick { get; private set; }
    private PlayerController player;
    private Canvas canvas;
    private Button dash;
    private Button skill;
    private Text dashLabel;
    private Text skillLabel;
    private float nextRefresh;

    private void Start()
    {
        player = GetComponent<PlayerController>();
        canvas = RuntimeUIFactory.CreateCanvas("MobileControlsCanvas", 960f);
        canvas.sortingOrder = 5;
        var root = SafeAreaLayout.Create(canvas);
        var baseRect = new GameObject("MoveStick", typeof(RectTransform), typeof(ControlRingGraphic), typeof(VirtualJoystick)).GetComponent<RectTransform>();
        baseRect.SetParent(root, false);
        Place(baseRect, new Vector2(0, 0), new Vector2(94, 90), new Vector2(128, 128));
        baseRect.GetComponent<ControlRingGraphic>().color = new Color(0.65f, 0.89f, 0.95f, 0.55f);
        var handle = new GameObject("Handle", typeof(RectTransform), typeof(ControlRingGraphic)).GetComponent<RectTransform>();
        handle.SetParent(baseRect, false);
        Place(handle, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(44, 44));
        var graphic = handle.GetComponent<ControlRingGraphic>();
        graphic.thickness = 22f;
        graphic.color = new Color(0.25f, 0.8f, 0.94f, 0.8f);
        graphic.raycastTarget = false;
        Joystick = baseRect.GetComponent<VirtualJoystick>();
        Joystick.input = player.InputRouter;
        Joystick.handle = handle;
        dash = CreateAction(root, "DashButton", "DASH", new Vector2(-70, 86), 76, true);
        skill = CreateAction(root, "SalvoButton", "SALVO", new Vector2(-164, 60), 68, false);
        dashLabel = dash.GetComponentInChildren<Text>();
        skillLabel = skill.GetComponentInChildren<Text>();
    }

    private Button CreateAction(Transform root, string name, string label, Vector2 position, float size, bool isDash)
    {
        var button = RuntimeUIFactory.CreateButton(root, name, label);
        Place(button.GetComponent<RectTransform>(), new Vector2(1, 0), position, new Vector2(size, size));
        button.GetComponent<Image>().color = isDash ? new Color(0.04f, 0.42f, 0.58f, 0.9f) : new Color(0.13f, 0.23f, 0.29f, 0.9f);
        button.GetComponentInChildren<Text>().fontSize = 15;
        var action = button.gameObject.AddComponent<MobileActionButton>();
        action.input = player.InputRouter;
        action.isDash = isDash;
        return button;
    }

    private void Update()
    {
        if (canvas == null) return;
        var gm = GameManager.Instance;
        bool show = gm != null && gm.IsCombatActive;
        SetVisible(show);
        if (!show || Time.unscaledTime < nextRefresh) return;
        nextRefresh = Time.unscaledTime + 0.1f;
        dash.interactable = player.IsDashReady && player.stats.CurrentEnergy >= 25f;
        skill.interactable = player.weaponController.SkillCooldownRemaining <= 0f && player.AutoAim.CurrentTarget != null;
        dashLabel.text = player.IsDashReady ? "DASH" : player.DashCooldownRemaining.ToString("0.0");
        float cooldown = player.weaponController.SkillCooldownRemaining;
        skillLabel.text = cooldown > 0f ? cooldown.ToString("0.0") : "SALVO";
    }

    public void SetVisible(bool visible)
    {
        if (canvas != null && canvas.gameObject.activeSelf != visible) canvas.gameObject.SetActive(visible);
        if (!visible && Joystick != null) Joystick.ResetInput();
    }

    private static void Place(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
    {
        rect.anchorMin = rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }
}
