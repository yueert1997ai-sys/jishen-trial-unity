using System;
using UnityEngine;

public enum PrimaryWeapon { None, M7, Greatsword, M14 }

[CreateAssetMenu(menuName = "MECH ROUGE/Hangar Armory")]
public sealed class HangarArmory : ScriptableObject
{
    [Serializable] public sealed class Entry
    {
        public PrimaryWeapon weapon;
        public GameObject prefab;
        public float damage, interval, speed;
        public int pierce;
    }
    public GameObject heroPrefab, roomPrefab;
    public Entry[] weapons = Array.Empty<Entry>();
    public Entry Find(PrimaryWeapon weapon) => Array.Find(weapons, e => e.weapon == weapon);
}
