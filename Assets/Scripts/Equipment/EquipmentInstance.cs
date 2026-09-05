using UnityEngine;

public class EquipmentInstance : MonoBehaviour
{
    public EquipmentData data;
    public int level = 1;

    public void Configure(EquipmentData sourceData, int sourceLevel)
    {
        data = sourceData;
        level = Mathf.Max(1, sourceLevel);
        gameObject.name = data != null ? data.displayName + "_Lv" + level : gameObject.name;
    }
}
