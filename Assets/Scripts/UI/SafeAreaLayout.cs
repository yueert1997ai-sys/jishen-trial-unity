using UnityEngine;

[DisallowMultipleComponent]
public class SafeAreaLayout : MonoBehaviour
{
    private Rect lastSafe;
    private Vector2 lastSize;
    public bool useScreen = true;

    private void Update()
    {
        if (!useScreen) return;
        Vector2 size = new Vector2(Screen.width, Screen.height);
        if (size == lastSize && Screen.safeArea == lastSafe) return;
        lastSize = size;
        lastSafe = Screen.safeArea;
        Apply(size, lastSafe);
    }

    public void Apply(Vector2 size, Rect safe)
    {
        var canvas = GetComponentInParent<Canvas>();
        var scaler = canvas != null ? canvas.GetComponent<UnityEngine.UI.CanvasScaler>() : null;
        if (scaler != null) scaler.matchWidthOrHeight = size.x < size.y * 16f / 9f ? 0f : 1f;
        var rect = (RectTransform)transform;
        Rect normalized = Calculate(size, safe);
        rect.anchorMin = normalized.min;
        rect.anchorMax = normalized.max;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }

    public static Rect Calculate(Vector2 size, Rect safe)
    {
        if (size.x <= 0f || size.y <= 0f) return new Rect(0, 0, 1, 1);
        float height = Mathf.Min(size.y, size.x * 9f / 16f);
        float bottom = (size.y - height) * 0.5f;
        float xMin = Mathf.Clamp(safe.xMin, 0f, size.x);
        float xMax = Mathf.Clamp(safe.xMax, xMin, size.x);
        float yMin = Mathf.Clamp(safe.yMin, bottom, bottom + height);
        float yMax = Mathf.Clamp(safe.yMax, yMin, bottom + height);
        return Rect.MinMaxRect(xMin / size.x, yMin / size.y, xMax / size.x, yMax / size.y);
    }

    public static RectTransform Create(Canvas canvas)
    {
        var root = new GameObject("SafeArea", typeof(RectTransform), typeof(SafeAreaLayout)).GetComponent<RectTransform>();
        root.SetParent(canvas.transform, false);
        root.GetComponent<SafeAreaLayout>().Apply(new Vector2(Screen.width, Screen.height), Screen.safeArea);
        return root;
    }
}
