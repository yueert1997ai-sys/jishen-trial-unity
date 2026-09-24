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
    private RectTransform impactFill;
    private Image impactImage;
    private ArmorHealth armor;
    private Text postureLabel;
    private EnemyBase enemy;

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

        fill = CreateImage(background, "Fill", GameUITheme.Danger);
        fill.anchorMin = new Vector2(0f, 0f);
        fill.anchorMax = Vector2.one;
        fill.offsetMin = new Vector2(0f, 2f);
        fill.offsetMax = new Vector2(0f, -2f);
        armor = GetComponent<ArmorHealth>();
        if (armor != null && armor.Maximum>0)
        {
            enemy = GetComponent<EnemyBase>();
            var textObject = new GameObject("PostureStatus", typeof(RectTransform));
            textObject.transform.SetParent(canvasRect, false);
            postureLabel = textObject.AddComponent<Text>();
            postureLabel.font = RuntimeUIFactory.DefaultFont;
            postureLabel.fontSize = 16; postureLabel.alignment = TextAnchor.MiddleCenter;
            postureLabel.raycastTarget = false;
            var labelRect = textObject.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0,1); labelRect.anchorMax = new Vector2(1,1);
            labelRect.offsetMin = new Vector2(0,1); labelRect.offsetMax = new Vector2(0,29);
            var bar = CreateImage(canvasRect, "ImpactBackground", new Color(.02f,.03f,.04f,.95f));
            bar.anchorMin = new Vector2(0, -1.1f); bar.anchorMax = new Vector2(1, -.35f);
            bar.offsetMin = bar.offsetMax = Vector2.zero;
            impactFill = CreateImage(bar, "ImpactPressure", GameUITheme.Armor);
            impactFill.anchorMin = Vector2.zero; impactFill.anchorMax = Vector2.one;
            impactFill.offsetMin = Vector2.zero; impactFill.offsetMax = Vector2.zero;
            impactImage = impactFill.GetComponent<Image>();
        }
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
        fill.offsetMin = new Vector2(0f, 2f);
        fill.offsetMax = new Vector2(0f, -2f);
        if (armor != null && impactFill != null)
        {
            impactFill.parent.gameObject.SetActive(armor.Intact);
            impactFill.anchorMax = new Vector2(armor.Ratio, 1);
            impactImage.color = GameUITheme.Armor;
            postureLabel.text=armor.Intact?EquipmentWarehouseUI.T("装甲","ARMOR"):EquipmentWarehouseUI.T("破甲","EXPOSED");
            postureLabel.color=armor.Intact?GameUITheme.Armor:GameUITheme.Energy;
        }
        canvas.gameObject.SetActive(!damageable.IsDead && (alwaysVisible || ratio < 0.999f || (armor != null && armor.Intact)));
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
