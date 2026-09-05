using UnityEngine;
using UnityEngine.EventSystems;

public class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler, ICancelHandler
{
    public PlayerInputRouter input;
    public RectTransform handle;
    public float radius = 44f;
    public bool controlsAim;
    public int PointerId { get; private set; } = int.MinValue;
    public Vector2 Value { get; private set; }

    public void OnPointerDown(PointerEventData data)
    {
        if (data.button != PointerEventData.InputButton.Left) return;
        if (PointerId != int.MinValue) return;
        PointerId = data.pointerId;
        OnDrag(data);
    }

    public void OnDrag(PointerEventData data)
    {
        if (PointerId != data.pointerId) return;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)transform, data.position, data.pressEventCamera, out Vector2 position)) return;
        Vector2 displacement = Vector2.ClampMagnitude(position, radius);
        handle.anchoredPosition = displacement;
        float amount = displacement.magnitude / radius;
        Value = amount <= 0.15f ? Vector2.zero : displacement.normalized * ((amount - 0.15f) / 0.85f);
        if (controlsAim) input.SetTouchAim(Value, true);
        else input.SetTouchMove(Value);
    }

    public void OnPointerUp(PointerEventData data) { if (data.pointerId == PointerId) ResetInput(); }
    public void OnCancel(BaseEventData data) { ResetInput(); }
    private void OnDisable() { ResetInput(); }
    private void OnApplicationFocus(bool focus) { if (!focus) ResetInput(); }
    public void ResetInput()
    {
        PointerId = int.MinValue;
        Value = Vector2.zero;
        if (handle != null) handle.anchoredPosition = Vector2.zero;
        if (input != null)
        {
            if (controlsAim) input.SetTouchAim(Vector2.zero, false);
            else input.SetTouchMove(Vector2.zero);
        }
    }
}
