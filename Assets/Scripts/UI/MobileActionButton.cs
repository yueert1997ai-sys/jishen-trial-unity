using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MobileActionButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public PlayerInputRouter input;
    public bool isDash;
    public bool isMelee;
    private int? boostPointer;
    public bool IsHoldingBoost => boostPointer.HasValue;
    public void OnPointerDown(PointerEventData data)
    {
        if (data.button != PointerEventData.InputButton.Left) return;
        if (!GetComponent<Button>().IsInteractable()) return;
        if (isMelee) input.QueueMelee();
        else if (isDash)
        {
            if (boostPointer.HasValue) return;
            boostPointer = data.pointerId;
            input.QueueDash();
            input.SetBoostHeld(true);
        }
        else input.QueueSkill();
    }
    public void OnPointerUp(PointerEventData data)
    {
        if (boostPointer != data.pointerId) return;
        ReleaseBoost();
    }
    private void ReleaseBoost()
    {
        if (!boostPointer.HasValue) return;
        boostPointer = null;
        if (input != null) input.SetBoostHeld(false);
    }
    private void OnDisable() { ReleaseBoost(); }
}
