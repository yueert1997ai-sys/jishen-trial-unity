using System.Collections.Generic;
using UnityEngine;

public class EquipmentShop : MonoBehaviour
{
    public EquipmentManager equipmentManager;
    public List<EquipmentData> shopItems = new List<EquipmentData>();
    public int repairPrice = 40;
    public float repairAmount = 60f;

    public int GetPrice(EquipmentData data)
    {
        if (data == null || equipmentManager == null)
        {
            return 9999;
        }

        int currentLevel = equipmentManager.GetLevel(data.equipmentType);
        if (currentLevel <= 0)
        {
            return data.price;
        }

        if (currentLevel >= data.maxLevel)
        {
            return 9999;
        }

        return Mathf.RoundToInt(data.price * (1.2f + currentLevel * 0.55f));
    }

    public string GetActionLabel(EquipmentData data)
    {
        if (data == null || equipmentManager == null)
        {
            return "Unavailable";
        }

        int currentLevel = equipmentManager.GetLevel(data.equipmentType);
        if (currentLevel <= 0)
        {
            return "Buy";
        }

        if (currentLevel >= data.maxLevel)
        {
            return "Max";
        }

        return "Upgrade to Lv" + (currentLevel + 1);
    }

    public bool TryBuyOrUpgrade(GameManager gameManager, EquipmentData data)
    {
        if (gameManager == null || equipmentManager == null || data == null)
        {
            return false;
        }

        int currentLevel = equipmentManager.GetLevel(data.equipmentType);
        if (currentLevel >= data.maxLevel)
        {
            return false;
        }

        int price = GetPrice(data);
        if (!gameManager.SpendCoins(price))
        {
            return false;
        }

        if (currentLevel <= 0)
        {
            equipmentManager.Equip(data, 1);
        }
        else
        {
            equipmentManager.Upgrade(data.equipmentType);
        }

        return true;
    }

    public bool TryRepair(GameManager gameManager)
    {
        if (gameManager == null || !gameManager.SpendCoins(repairPrice))
        {
            return false;
        }

        PlayerStats stats = equipmentManager != null ? equipmentManager.GetComponent<PlayerStats>() : null;
        if (stats != null)
        {
            stats.Heal(repairAmount);
        }

        return true;
    }
}
