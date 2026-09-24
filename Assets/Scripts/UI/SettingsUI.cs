using UnityEngine;
using UnityEngine.UI;

public class SettingsUI : MonoBehaviour
{
    public bool IsVisible => canvas != null && canvas.gameObject.activeSelf;
    private Canvas canvas;
    private RectTransform fields, credits;
    private GameManager owner;
    private bool previouslyPaused;
    private Slider master, music, effects;
    private Toggle shake;
    private Button english, chinese, balanced, high, creditsButton;
    private Text masterValue, musicValue, effectsValue, title;
    private static readonly Color selected = GameUITheme.Accent;
    private static readonly Color unselected = GameUITheme.Button;

    public void Show(GameManager gameManager)
    {
        if (IsVisible) return;
        owner = gameManager;
        previouslyPaused = owner.IsPaused;
        owner.SetPaused(true);
        BuildUI();
        fields.gameObject.SetActive(true);
        credits.gameObject.SetActive(false);
        canvas.gameObject.SetActive(true);
        Refresh();
    }

    public void Hide()
    {
        if (!IsVisible) return;
        canvas.gameObject.SetActive(false);
        GamePreferences.Save();
        if (!previouslyPaused) owner.SetPaused(false);
    }

    private void SetLanguage(bool value)
    {
        GamePreferences.SetLanguage(value);
        if (owner.hangarUI != null) owner.hangarUI.Refresh();
        Refresh();
    }

    private void Refresh()
    {
        master.SetValueWithoutNotify(GamePreferences.Master);
        music.SetValueWithoutNotify(GamePreferences.Music);
        effects.SetValueWithoutNotify(GamePreferences.Effects);
        shake.SetIsOnWithoutNotify(GamePreferences.Shake);
        masterValue.text = Mathf.RoundToInt(GamePreferences.Master * 100) + "%";
        musicValue.text = Mathf.RoundToInt(GamePreferences.Music * 100) + "%";
        effectsValue.text = Mathf.RoundToInt(GamePreferences.Effects * 100) + "%";
        english.GetComponent<Image>().color = GamePreferences.Chinese ? unselected : selected;
        chinese.GetComponent<Image>().color = GamePreferences.Chinese ? selected : unselected;
        balanced.GetComponent<Image>().color = GamePreferences.Quality == 0 ? selected : unselected;
        high.GetComponent<Image>().color = GamePreferences.Quality == 1 ? selected : unselected;
        title.text = GameText.T(credits.gameObject.activeSelf ? "Credits" : "SETTINGS");
        creditsButton.GetComponentInChildren<Text>().text = GameText.T(credits.gameObject.activeSelf ? "Settings" : "Credits");
    }

    private void BuildUI()
    {
        if (canvas != null) return;
        canvas = RuntimeUIFactory.CreateCanvas("SettingsCanvas", 960);
        canvas.sortingOrder = 130;
        var panel = RuntimeUIFactory.CreateMenuSurface(canvas, "SettingsPanel", new Vector2(680, 476));
        title = RuntimeUIFactory.MenuText(panel, "SettingsTitle", "", 26, new Vector2(340, -38), new Vector2(596, 44));
        fields = Group(panel, "SettingsFields");
        credits = Group(panel, "CreditFields");
        Label("Language", -94);
        english = RuntimeUIFactory.MenuButton(fields, "EnglishButton", "English", new Vector2(350, -94), new Vector2(184, 48));
        chinese = RuntimeUIFactory.MenuButton(fields, "ChineseButton", "\u4e2d\u6587", new Vector2(546, -94), new Vector2(184, 48));
        english.onClick.AddListener(() => SetLanguage(false));
        chinese.onClick.AddListener(() => SetLanguage(true));
        master = Volume("Master", -150, out masterValue);
        music = Volume("Music", -202, out musicValue);
        effects = Volume("Effects", -254, out effectsValue);
        master.onValueChanged.AddListener(value => { GamePreferences.SetMaster(value); Refresh(); });
        music.onValueChanged.AddListener(value => { GamePreferences.SetMusic(value); Refresh(); });
        effects.onValueChanged.AddListener(value => { GamePreferences.SetEffects(value); Refresh(); });
        Label("Camera shake", -306);
        var toggleRect = RuntimeUIFactory.CreatePanel(fields, "ShakeToggle", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Color.clear);
        RuntimeUIFactory.Place(toggleRect, new Vector2(0, 1), new Vector2(610, -306), new Vector2(56, 48));
        var box = RuntimeUIFactory.CreatePanel(toggleRect, "Checkbox", Vector2.one * 0.5f, Vector2.one * 0.5f, new Vector2(-16, -16), new Vector2(16, 16), selected);
        var check = RuntimeUIFactory.CreateText(box, "Check", "\u2713", 21, TextAnchor.MiddleCenter, Color.white);
        check.rectTransform.anchorMin = Vector2.zero;
        check.rectTransform.anchorMax = Vector2.one;
        check.rectTransform.offsetMin = check.rectTransform.offsetMax = Vector2.zero;
        shake = toggleRect.gameObject.AddComponent<Toggle>();
        shake.targetGraphic = box.GetComponent<Image>();
        shake.graphic = check;
        shake.onValueChanged.AddListener(GamePreferences.SetShake);
        Label("Quality", -358);
        balanced = RuntimeUIFactory.MenuButton(fields, "BalancedButton", "Balanced", new Vector2(350, -358), new Vector2(184, 48));
        high = RuntimeUIFactory.MenuButton(fields, "HighButton", "High", new Vector2(546, -358), new Vector2(184, 48));
        balanced.onClick.AddListener(() => { GamePreferences.SetQuality(0); Refresh(); });
        high.onClick.AddListener(() => { GamePreferences.SetQuality(1); Refresh(); });
        RuntimeUIFactory.MenuText(credits, "CreditsText", GameText.Credits, 17,
            new Vector2(340, -232), new Vector2(596, 290), TextAnchor.UpperLeft);
        creditsButton = RuntimeUIFactory.MenuButton(panel, "CreditsButton", "Credits", new Vector2(145, -434), new Vector2(206, 52));
        creditsButton.onClick.AddListener(() =>
        {
            bool show = !credits.gameObject.activeSelf;
            credits.gameObject.SetActive(show);
            fields.gameObject.SetActive(!show);
            Refresh();
        });
        RuntimeUIFactory.MenuButton(panel, "CloseSettingsButton", "Done", new Vector2(533, -434), new Vector2(210, 52)).onClick.AddListener(Hide);
    }

    private void Label(string key, float y)
    {
        RuntimeUIFactory.MenuText(fields, key + "Label", key, 18, new Vector2(141, y), new Vector2(198, 44));
    }

    private Slider Volume(string key, float y, out Text percent)
    {
        Label(key, y);
        var rect = RuntimeUIFactory.CreatePanel(fields, key + "Slider", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Color.clear);
        RuntimeUIFactory.Place(rect, new Vector2(0, 1), new Vector2(403, y), new Vector2(290, 44));
        var track = RuntimeUIFactory.CreatePanel(rect, "Track", new Vector2(0, 0.5f), new Vector2(1, 0.5f), new Vector2(0, -3), new Vector2(0, 3), unselected);
        var fill = RuntimeUIFactory.CreatePanel(track, "Fill", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, selected);
        var thumb = new GameObject("Thumb", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
        thumb.SetParent(rect, false);
        RuntimeUIFactory.Place(thumb, Vector2.one * 0.5f, Vector2.zero, new Vector2(10, 24));
        var graphic = thumb.GetComponent<Image>();
        graphic.color = new Color(0.92f, 0.97f, 0.97f);
        var slider = rect.gameObject.AddComponent<Slider>();
        slider.fillRect = fill;
        slider.handleRect = thumb;
        slider.targetGraphic = graphic;
        slider.minValue = 0;
        slider.maxValue = 1;
        percent = RuntimeUIFactory.MenuText(fields, key + "Value", "", 16, new Vector2(602, y), new Vector2(72, 44), TextAnchor.MiddleRight);
        return slider;
    }

    private static RectTransform Group(Transform parent, string name)
    {
        var group = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        group.SetParent(parent, false);
        group.anchorMin = Vector2.zero;
        group.anchorMax = Vector2.one;
        group.offsetMin = group.offsetMax = Vector2.zero;
        return group;
    }
}
