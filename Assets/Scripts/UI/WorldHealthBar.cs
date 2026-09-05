using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Damageable))]
public class WorldHealthBar : MonoBehaviour
{
    public float height = 2.1f;
    public float width = 1.5f;
    public bool alwaysVisible;

    private Damageable damageable;
    private Canvas canvas;
    private RectTransform fill;

    private void Awake()
    {
        damageable = GetComponent<Damageable>();
    }

    private void Start()
    {
        Build();
        Refresh();
    }

    private void Update()
    {
        if (canvas == null)
        {
            return;
        }

        Camera camera = Camera.main;
        if (camera != null)
        {
            canvas.transform.rotation = camera.transform.rotation;
        }

        Refresh();
    }

    private void Build()
    {
        if (canvas != null)
        {
            return;
        }

        GameObject canvasObject = new GameObject("WorldHealthCanvas", typeof(RectTransform));
        canvasObject.transform.SetParent(transform, false);
        canvasObject.transform.localPosition = new Vector3(0f, height, 0f);
        canvasObject.transform.localScale = Vector3.one * 0.01f;
        canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 20;
        RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(width * 100f, 12f);

        RectTransform background = CreateImage(canvasRect, "Background", new Color(0.025f, 0.035f, 0.045f, 0.92f));
        background.anchorMin = Vector2.zero;
        background.anchorMax = Vector2.one;
        background.offsetMin = Vector2.zero;
        background.offsetMax = Vector2.zero;

        fill = CreateImage(background, "Fill", alwaysVisible ? new Color(1f, 0.18f, 0.08f, 1f) : new Color(1f, 0.32f, 0.12f, 1f));
        fill.anchorMin = new Vector2(0f, 0f);
        fill.anchorMax = Vector2.one;
        fill.offsetMin = new Vector2(2f, 2f);
        fill.offsetMax = new Vector2(-2f, -2f);
    }

    private void Refresh()
    {
        if (damageable == null || fill == null || canvas == null)
        {
            return;
        }

        float ratio = damageable.maxHealth > 0f ? damageable.CurrentHealth / damageable.maxHealth : 0f;
        Vector2 anchorMax = fill.anchorMax;
        anchorMax.x = Mathf.Clamp01(ratio);
        fill.anchorMax = anchorMax;
        fill.offsetMin = new Vector2(2f, 2f);
        fill.offsetMax = new Vector2(-2f, -2f);
        canvas.gameObject.SetActive(alwaysVisible || ratio < 0.999f);
    }

    private static RectTransform CreateImage(Transform parent, string objectName, Color color)
    {
        GameObject imageObject = new GameObject(objectName, typeof(RectTransform));
        imageObject.transform.SetParent(parent, false);
        Image image = imageObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return imageObject.GetComponent<RectTransform>();
    }
}
