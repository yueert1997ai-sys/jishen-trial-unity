using UnityEngine;

public class FloatingCombatText : MonoBehaviour
{
    public TextMesh textMesh;
    public float lifetime = 0.65f;
    public float riseSpeed = 1.2f;

    private float remaining;
    private Color baseColor;

    public void Init(float amount, Color color)
    {
        textMesh = GetComponent<TextMesh>();
        if (textMesh == null)
        {
            textMesh = gameObject.AddComponent<TextMesh>();
        }

        textMesh.text = Mathf.Max(1, Mathf.RoundToInt(amount)).ToString();
        textMesh.fontSize = 48;
        textMesh.characterSize = 0.035f;
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.alignment = TextAlignment.Center;
        textMesh.color = color;
        baseColor = color;
        remaining = lifetime;

        Renderer renderer = textMesh.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.sortingOrder = 80;
        }
    }

    private void Update()
    {
        transform.position += Vector3.up * riseSpeed * Time.deltaTime;
        Camera camera = Camera.main;
        if (camera != null)
        {
            transform.rotation = camera.transform.rotation;
        }

        remaining -= Time.deltaTime;
        if (textMesh != null)
        {
            Color color = baseColor;
            color.a = Mathf.Clamp01(remaining / Mathf.Max(0.01f, lifetime));
            textMesh.color = color;
        }

        if (remaining <= 0f)
        {
            Destroy(gameObject);
        }
    }
}
