using System;
using System.Collections.Generic;
using UnityEngine;

public class EquipmentManager : MonoBehaviour
{
    [Serializable]
    public class EquippedItem
    {
        public EquipmentData data;
        public int level;
        public GameObject instance;
    }

    public List<EquipmentData> startingEquipment = new List<EquipmentData>();
    public PlayerStats playerStats;
    public WeaponController weaponController;
    public MechHardpointManager hardpointManager;

    public event Action OnEquipmentChanged;

    private readonly Dictionary<EquipmentType, EquippedItem> equippedByType = new Dictionary<EquipmentType, EquippedItem>();

    private void Awake()
    {
        if (playerStats == null)
        {
            playerStats = GetComponent<PlayerStats>();
        }

        if (weaponController == null)
        {
            weaponController = GetComponent<WeaponController>();
        }

        if (hardpointManager == null)
        {
            hardpointManager = GetComponent<MechHardpointManager>();
        }
    }

    private void Start()
    {
        for (int i = 0; i < startingEquipment.Count; i++)
        {
            if (startingEquipment[i] != null)
            {
                Equip(startingEquipment[i], Mathf.Max(1, startingEquipment[i].level));
            }
        }
    }

    public void Equip(EquipmentData data, int requestedLevel)
    {
        if (data == null)
        {
            return;
        }

        int targetLevel = Mathf.Clamp(requestedLevel, 1, data.maxLevel);
        Unequip(data.equipmentType);

        Transform socket = GetTargetSocket(data);
        GameObject prefab = data.GetPrefabForLevel(targetLevel);
        GameObject instance = prefab != null ? Instantiate(prefab, socket) : CreateRuntimePlaceholder(data, socket);
        instance.transform.localPosition = Vector3.zero;
        instance.transform.localRotation = Quaternion.identity;
        instance.transform.localScale = Vector3.one;

        EquipmentInstance equipmentInstance = instance.GetComponent<EquipmentInstance>();
        if (equipmentInstance == null)
        {
            equipmentInstance = instance.AddComponent<EquipmentInstance>();
        }

        equipmentInstance.Configure(data, targetLevel);

        EquippedItem item = new EquippedItem
        {
            data = data,
            level = targetLevel,
            instance = instance
        };
        equippedByType[data.equipmentType] = item;

        RecalculateStats();
        NotifyEquipmentChanged();
    }

    public void Unequip(EquipmentType equipmentType)
    {
        if (!equippedByType.ContainsKey(equipmentType))
        {
            return;
        }

        EquippedItem item = equippedByType[equipmentType];
        if (item.instance != null)
        {
            Destroy(item.instance);
        }

        equippedByType.Remove(equipmentType);
        RecalculateStats();
        NotifyEquipmentChanged();
    }

    public bool Upgrade(EquipmentType equipmentType)
    {
        if (!equippedByType.ContainsKey(equipmentType))
        {
            return false;
        }

        EquippedItem current = equippedByType[equipmentType];
        if (current.data == null || current.level >= current.data.maxLevel)
        {
            return false;
        }

        Equip(current.data, current.level + 1);
        return true;
    }

    public bool HasEquipment(EquipmentType equipmentType)
    {
        return equippedByType.ContainsKey(equipmentType);
    }

    public int GetLevel(EquipmentType equipmentType)
    {
        return equippedByType.ContainsKey(equipmentType) ? equippedByType[equipmentType].level : 0;
    }

    public EquipmentData GetEquipmentData(EquipmentType equipmentType)
    {
        return equippedByType.ContainsKey(equipmentType) ? equippedByType[equipmentType].data : null;
    }

    public List<EquippedItem> GetEquippedItems()
    {
        return new List<EquippedItem>(equippedByType.Values);
    }

    public int GetHighestShoulderMissileLevel()
    {
        int level = 0;
        if (equippedByType.ContainsKey(EquipmentType.LeftShoulder))
        {
            level = Mathf.Max(level, equippedByType[EquipmentType.LeftShoulder].level);
        }

        if (equippedByType.ContainsKey(EquipmentType.RightShoulder))
        {
            level = Mathf.Max(level, equippedByType[EquipmentType.RightShoulder].level);
        }

        return level;
    }

    private Transform GetTargetSocket(EquipmentData data)
    {
        if (hardpointManager == null)
        {
            hardpointManager = GetComponent<MechHardpointManager>();
        }

        hardpointManager.EnsureDefaultHardpoints();
        if (!string.IsNullOrEmpty(data.targetSocketName))
        {
            return hardpointManager.GetSocket(data.targetSocketName);
        }

        return hardpointManager.GetSocketForEquipment(data.equipmentType);
    }

    private void RecalculateStats()
    {
        if (playerStats == null)
        {
            return;
        }

        float damageBonus = 0f;
        float fireRateBonus = 0f;
        float hpBonus = 0f;
        float energyBonus = 0f;
        float dashBonus = 0f;
        int shieldLevel = 0;

        foreach (EquippedItem item in equippedByType.Values)
        {
            if (item.data == null)
            {
                continue;
            }

            float levelScale = Mathf.Max(1, item.level);
            damageBonus += item.data.damageBonus * levelScale;
            fireRateBonus += item.data.fireRateBonus * levelScale;
            hpBonus += item.data.hpBonus * levelScale;
            energyBonus += item.data.energyBonus * levelScale;
            dashBonus += item.data.dashBonus * levelScale;
            if (item.data.equipmentType == EquipmentType.Shield)
            {
                shieldLevel = Mathf.Max(shieldLevel, item.level);
            }
        }

        playerStats.ApplyEquipmentBonuses(damageBonus, fireRateBonus, hpBonus, energyBonus, dashBonus, shieldLevel);
    }

    private static GameObject CreateRuntimePlaceholder(EquipmentData data, Transform socket)
    {
        GameObject placeholder = GameObject.CreatePrimitive(PrimitiveType.Cube);
        placeholder.name = data.displayName + "_RuntimePlaceholder";
        placeholder.transform.SetParent(socket, false);
        placeholder.transform.localScale = new Vector3(0.35f, 0.35f, 0.85f);
        return placeholder;
    }

    private void NotifyEquipmentChanged()
    {
        if (weaponController != null)
        {
            weaponController.RefreshEquipment();
        }

        if (OnEquipmentChanged != null)
        {
            OnEquipmentChanged.Invoke();
        }
    }
}
