using UnityEngine;

// Fixed-size desktop menus also have to fit the phone's safe area and home indicator.
public sealed class MobileMenuFit : MonoBehaviour
{
    void LateUpdate() { Fit(); }
    public void Fit()
    {
        var rect = (RectTransform)transform;
        var parent = rect.parent as RectTransform;
        if (parent == null || rect.rect.width <= 0 || rect.rect.height <= 0) return;
        float scale = Mathf.Clamp01(Mathf.Min((parent.rect.width - 16) / rect.rect.width,
            (parent.rect.height - 16) / rect.rect.height));
        rect.localScale = Vector3.one * Mathf.Max(.1f, scale);
    }
}
