using System;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class PlayerLoadout : MonoBehaviour
{
    public PrimaryWeapon Selected { get; private set; }
    public HangarArmory Armory { get; private set; }
    public HangarArmory.Entry Equipped => Armory != null ? Armory.Find(Selected) : null;
    public bool IsRifle => Selected == PrimaryWeapon.M7 || Selected == PrimaryWeapon.M14;
    public bool CanDeploy => Selected != PrimaryWeapon.None && Equipped != null && Equipped.prefab != null;
    public event Action Changed;
    private void Awake() => Armory = Resources.Load<HangarArmory>("Hangar/Armory");
    public bool Select(PrimaryWeapon weapon)
    {
        var gm = GameManager.Instance;
        if (gm != null && (gm.Phase != GamePhase.Hangar || gm.IsPaused)) return false;
        if (weapon != PrimaryWeapon.None && (Armory == null || Armory.Find(weapon)?.prefab == null)) return false;
        Selected = weapon;
        GetComponent<WeaponController>()?.ResetCooldowns();
        GetComponent<PlayerMeleeController>()?.ResetCooldown();
        Changed?.Invoke();
        return true;
    }
    public void EnterHangar() { Selected = PrimaryWeapon.None; Changed?.Invoke(); }
}
