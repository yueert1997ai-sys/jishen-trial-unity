using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class RuntimeUIFactory
{
    private static Font cachedFont;

    public static Font DefaultFont
    {
        get
        {
            if (cachedFont == null)
            {
                cachedFont = Resources.Load<Font>("Fonts/NotoSansCJKsc-Regular");
            }

            if (cachedFont == null)
            {
                cachedFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }

            if (cachedFont == null)
            {
                cachedFont = Font.CreateDynamicFontFromOSFont("Arial", 14);
            }

            return cachedFont;
        }
    }

    public static Canvas CreateCanvas(string name, float referenceWidth = 1920f)
    {
        EnsureEventSystem();
        GameObject canvasObject = new GameObject(name, typeof(RectTransform));
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(referenceWidth, referenceWidth * 9f / 16f);
        scaler.matchWidthOrHeight = 1f;
        canvasObject.AddComponent<GraphicRaycaster>();
        return canvas;
    }

    private static void EnsureEventSystem()
    {
        if (Object.FindFirstObjectByType<EventSystem>() != null)
        {
            return;
        }

        GameObject eventSystemObject = new GameObject("EventSystem");
        eventSystemObject.AddComponent<EventSystem>();
        eventSystemObject.AddComponent<StandaloneInputModule>();
    }

    public static RectTransform CreatePanel(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax, Color color)
    {
        GameObject panelObject = new GameObject(name, typeof(RectTransform));
        panelObject.transform.SetParent(parent, false);
        Image image = panelObject.AddComponent<Image>();
        image.color = color;
        RectTransform rect = panelObject.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
        return rect;
    }

    public static Text CreateText(Transform parent, string name, string value, int size, TextAnchor anchor, Color color)
    {
        GameObject textObject = new GameObject(name, typeof(RectTransform));
        textObject.transform.SetParent(parent, false);
        Text text = textObject.AddComponent<Text>();
        text.font = DefaultFont;
        text.fontSize = size;
        text.alignment = anchor;
        text.color = color;
        text.text = value;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        if (!string.IsNullOrEmpty(value)) textObject.AddComponent<LocalizedLabel>().SetKey(value);
        return text;
    }

    public static Button CreateButton(Transform parent, string name, string label)
    {
        GameObject buttonObject = new GameObject(name, typeof(RectTransform));
        buttonObject.transform.SetParent(parent, false);
        Image image = buttonObject.AddComponent<Image>();
        image.color = GameUITheme.Button;
        Button button = buttonObject.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.highlightedColor = new Color(1.3f,1.3f,1.3f,1);
        colors.selectedColor=colors.highlightedColor;
        colors.pressedColor = new Color(.7f,.8f,.9f,1);
        colors.disabledColor=new Color(.55f,.55f,.55f,.75f);
        button.colors = colors;

        Text text = CreateText(buttonObject.transform, "Label", label, 24, TextAnchor.MiddleCenter, Color.white);
        RectTransform textRect = text.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        text.color=GameUITheme.Text;text.raycastTarget=false;

        return button;
    }

    public static void Place(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
    {
        rect.anchorMin = rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    public static RectTransform CreateMenuSurface(Canvas canvas, string name, Vector2 size)
    {
        CreatePanel(canvas.transform, name + "Shade", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(0.01f, 0.025f, 0.03f, 0.8f));
        var safe = SafeAreaLayout.Create(canvas);
        var surface = CreatePanel(safe, name, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, GameUITheme.Surface);
        Place(surface, Vector2.one * 0.5f, Vector2.zero, size);
        if (MobilePlatform.UsesTouch) surface.gameObject.AddComponent<MobileMenuFit>();
        var rule=CreatePanel(surface,"MenuAccent",new Vector2(0,1),Vector2.one,new Vector2(24,-3),new Vector2(-24,-1),GameUITheme.Accent);
        rule.GetComponent<Image>().raycastTarget=false;
        return surface;
    }

    public static Text MenuText(Transform parent, string name, string value, int size, Vector2 position, Vector2 dimensions, TextAnchor alignment = TextAnchor.MiddleLeft)
    {
        var text = CreateText(parent, name, value, size, alignment, GameUITheme.Text);
        text.raycastTarget = false;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        Place(text.rectTransform, new Vector2(0, 1), position, dimensions);
        return text;
    }

    public static Button MenuButton(Transform parent, string name, string label, Vector2 position, Vector2 size)
    {
        var button = CreateButton(parent, name, label);
        Place(button.GetComponent<RectTransform>(), new Vector2(0, 1), position, size);
        button.GetComponentInChildren<Text>().fontSize = 18;
        return button;
    }
}
