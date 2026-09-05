using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MobileActionButton : MonoBehaviour, IPointerDownHandler
{
    public PlayerInputRouter input;
    public bool isDash;
    public bool isMelee;
    public void OnPointerDown(PointerEventData data)
    {
        if (data.button != PointerEventData.InputButton.Left) return;
        if (!GetComponent<Button>().IsInteractable()) return;
        if (isMelee) input.QueueMelee();
        else if (isDash) input.QueueDash();
        else input.QueueSkill();
    }
}
