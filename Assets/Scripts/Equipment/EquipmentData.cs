using UnityEngine;

public enum EquipmentType
{
    RightHandWeapon,
    LeftHandWeapon,
    LeftShoulder,
    RightShoulder,
    Backpack,
    ChestCore,
    Shield
}

[CreateAssetMenu(menuName = "MECH ROUGE/Equipment Data", fileName = "EquipmentData")]
public class EquipmentData : ScriptableObject
{
    public string equipmentId;
    public string displayName;
    public EquipmentType equipmentType;
    public string targetSocketName;
    [Min(1)] public int level = 1;
    [Min(1)] public int maxLevel = 3;
    public int price = 50;
    public float damageBonus;
    public float fireRateBonus;
    public float hpBonus;
    public float energyBonus;
    public float dashBonus;
    public GameObject prefabLevel1;
    public GameObject prefabLevel2;
    public GameObject prefabLevel3;
    [TextArea(2, 5)] public string description;

    public GameObject GetPrefabForLevel(int requestedLevel)
    {
        int clampedLevel = Mathf.Clamp(requestedLevel, 1, maxLevel);
        if (clampedLevel >= 3 && prefabLevel3 != null)
        {
            return prefabLevel3;
        }

        if (clampedLevel >= 2 && prefabLevel2 != null)
        {
            return prefabLevel2;
        }

        return prefabLevel1;
    }
}
