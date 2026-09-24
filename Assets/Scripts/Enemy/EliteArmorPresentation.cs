using UnityEngine;
// Retained component identity for existing prefabs. Armor is shown by WorldHealthBar.
[DisallowMultipleComponent]
public sealed class EliteArmorPresentation : MonoBehaviour
{
    void Awake()
    {
        var old=transform.Find("FrontArmorArc");
        if(old!=null)old.gameObject.SetActive(false);
    }
}
