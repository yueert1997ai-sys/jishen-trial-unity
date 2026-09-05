using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MobileActionButton : MonoBehaviour, IPointerDownHandler
{
    public PlayerInputRouter input;
    public bool isDash;
    public void OnPointerDown(PointerEventData data)
    {
        if (!GetComponent<Button>().IsInteractable()) return;
        if (isDash) input.QueueDash();
        else input.QueueSkill();
    }
}
