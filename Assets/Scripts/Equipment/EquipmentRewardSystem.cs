using System.Collections.Generic;
using UnityEngine;

public enum RewardOptionType
{
    NewEquipment,
    UpgradeEquipment,
    Coins
}

public class EquipmentRewardOption
{
    public RewardOptionType optionType;
    public EquipmentData equipment;
    public int coinAmount;
    public string title;
    public string description;
}

public class EquipmentRewardSystem : MonoBehaviour
{
    public EquipmentManager equipmentManager;
    public List<EquipmentData> rewardPool = new List<EquipmentData>();

    public List<EquipmentRewardOption> GenerateOptions()
    {
        List<EquipmentRewardOption> options = new List<EquipmentRewardOption>();
        EquipmentData newEquipment = FindNewEquipment();
        if (newEquipment != null)
        {
            options.Add(new EquipmentRewardOption
            {
                optionType = RewardOptionType.NewEquipment,
                equipment = newEquipment,
                title = "New equipment: " + newEquipment.displayName,
                description = newEquipment.description + "\nMounts on " + newEquipment.targetSocketName
            });
        }

        EquipmentData upgrade = FindUpgradeableEquipment();
        if (upgrade != null)
        {
            int currentLevel = equipmentManager != null ? equipmentManager.GetLevel(upgrade.equipmentType) : 1;
            options.Add(new EquipmentRewardOption
            {
                optionType = RewardOptionType.UpgradeEquipment,
                equipment = upgrade,
                title = "Upgrade: " + upgrade.displayName + " Lv" + (currentLevel + 1),
                description = "Improves stats and swaps to a larger visible model."
            });
        }

        while (options.Count < 2)
        {
            EquipmentData fallback = rewardPool.Count > 0 ? rewardPool[Random.Range(0, rewardPool.Count)] : null;
            if (fallback == null)
            {
                break;
            }

            options.Add(new EquipmentRewardOption
            {
                optionType = RewardOptionType.NewEquipment,
                equipment = fallback,
                title = "Equipment: " + fallback.displayName,
                description = fallback.description
            });
        }

        options.Add(new EquipmentRewardOption
        {
            optionType = RewardOptionType.Coins,
            coinAmount = 90,
            title = "Salvage cache",
            description = "Gain 90 coins for the shop."
        });

        return options;
    }

    public void ApplyOption(EquipmentRewardOption option)
    {
        if (option == null)
        {
            return;
        }

        if (option.optionType == RewardOptionType.Coins)
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.AddCoins(option.coinAmount);
            }

            return;
        }

        if (equipmentManager == null || option.equipment == null)
        {
            return;
        }

        if (option.optionType == RewardOptionType.UpgradeEquipment && equipmentManager.HasEquipment(option.equipment.equipmentType))
        {
            equipmentManager.Upgrade(option.equipment.equipmentType);
            return;
        }

        equipmentManager.Equip(option.equipment, 1);
    }

    private EquipmentData FindNewEquipment()
    {
        if (equipmentManager == null)
        {
            return rewardPool.Count > 0 ? rewardPool[0] : null;
        }

        for (int i = 0; i < rewardPool.Count; i++)
        {
            EquipmentData candidate = rewardPool[i];
            if (candidate != null && !equipmentManager.HasEquipment(candidate.equipmentType))
            {
                return candidate;
            }
        }

        return null;
    }

    private EquipmentData FindUpgradeableEquipment()
    {
        if (equipmentManager == null)
        {
            return null;
        }

        for (int i = 0; i < rewardPool.Count; i++)
        {
            EquipmentData candidate = rewardPool[i];
            if (candidate == null)
            {
                continue;
            }

            int level = equipmentManager.GetLevel(candidate.equipmentType);
            if (level > 0 && level < candidate.maxLevel)
            {
                return candidate;
            }
        }

        return null;
    }
}
