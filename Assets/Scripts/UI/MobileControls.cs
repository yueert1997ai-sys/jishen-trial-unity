using UnityEngine;
using UnityEngine.UI;

public class MobileControls : MonoBehaviour
{
    public VirtualJoystick Joystick { get; private set; }
    public VirtualJoystick AimJoystick { get; private set; }
    private Button melee;
    private Text meleeLabel;
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
        baseRect.GetComponent<ControlRingGraphic>().color = new Color(0.72f, 0.77f, 0.76f, 0.55f);
        var handle = new GameObject("Handle", typeof(RectTransform), typeof(ControlRingGraphic)).GetComponent<RectTransform>();
        handle.SetParent(baseRect, false);
        Place(handle, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(44, 44));
        var graphic = handle.GetComponent<ControlRingGraphic>();
        graphic.thickness = 22f;
        graphic.color = new Color(0.57f, 0.67f, 0.68f, 0.8f);
        graphic.raycastTarget = false;
        Joystick = baseRect.GetComponent<VirtualJoystick>();
        Joystick.input = player.InputRouter;
        Joystick.handle = handle;
        var aimRect = Instantiate(baseRect, root);
        aimRect.name = "AimStick";
        Place(aimRect, new Vector2(1, 0), new Vector2(-94, 92), new Vector2(128, 128));
        AimJoystick = aimRect.GetComponent<VirtualJoystick>();
        AimJoystick.controlsAim = true;
        AimJoystick.handle = (RectTransform)aimRect.Find("Handle");
        dash = CreateAction(root, "DashButton", "DASH", new Vector2(-206, 67), 68, true);
        skill = CreateAction(root, "SalvoButton", "SALVO", new Vector2(-74, 216), 60, false);
        melee = CreateAction(root, "MeleeButton", "SLASH", new Vector2(-192, 166), 76, false);
        melee.GetComponent<MobileActionButton>().isMelee = true;
        meleeLabel = melee.GetComponentInChildren<Text>();
        dashLabel = dash.GetComponentInChildren<Text>();
        skillLabel = skill.GetComponentInChildren<Text>();
    }

    private Button CreateAction(Transform root, string name, string label, Vector2 position, float size, bool isDash)
    {
        var button = RuntimeUIFactory.CreateButton(root, name, label);
        Place(button.GetComponent<RectTransform>(), new Vector2(1, 0), position, new Vector2(size, size));
        button.GetComponent<Image>().color = isDash ? new Color(0.25f, 0.3f, 0.3f, 0.9f) : new Color(0.2f, 0.22f, 0.22f, 0.9f);
        var circle = new GameObject("Surface", typeof(RectTransform), typeof(ControlRingGraphic)).GetComponent<RectTransform>();
        circle.SetParent(button.transform, false);
        circle.SetAsFirstSibling();
        Place(circle, new Vector2(.5f, .5f), Vector2.zero, new Vector2(size, size));
        var ring = circle.GetComponent<ControlRingGraphic>();
        ring.thickness = size * 0.5f;
        ring.color = button.GetComponent<Image>().color;
        button.GetComponent<Image>().enabled = false;
        button.targetGraphic = ring;
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
        melee.interactable = player.Melee.CooldownRemaining <= 0 && !player.IsDashing;
        meleeLabel.text = player.Melee.CooldownRemaining > 0 ? player.Melee.CooldownRemaining.ToString("0.0") : GameText.T("SLASH");
        skill.interactable = player.weaponController.SkillCooldownRemaining <= 0f && player.AutoAim.CurrentTarget != null;
        dashLabel.text = player.IsDashReady ? GameText.T("DASH") : player.DashCooldownRemaining.ToString("0.0");
        float cooldown = player.weaponController.SkillCooldownRemaining;
        skillLabel.text = cooldown > 0f ? cooldown.ToString("0.0") : GameText.T("SALVO");
    }

    public void SetVisible(bool visible)
    {
        if (canvas != null && canvas.gameObject.activeSelf != visible) canvas.gameObject.SetActive(visible);
        if (!visible && Joystick != null) Joystick.ResetInput();
        if (!visible && AimJoystick != null) AimJoystick.ResetInput();
    }

    private static void Place(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
    {
        rect.anchorMin = rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }
}
