using System;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class PlayerLoadout : MonoBehaviour
{
    public PrimaryWeapon Selected { get; private set; }
    public HangarArmory Armory { get; private set; }
    public bool IsNemesis=>GetComponent<PlayerMechLoader>().SelectedHero==HeroMech.Nemesis;
    public PrimaryWeapon EffectiveWeapon=>Selected==PrimaryWeapon.Collection?GameManager.Instance?.equipmentLoop?.Weapon?.primaryWeapon??PrimaryWeapon.Collection:Selected;
    public HangarArmory.Entry Equipped => Selected==PrimaryWeapon.Collection?GameManager.Instance?.equipmentLoop?.Weapon?.PrimaryEntry:Armory==null?null:!IsNemesis&&Selected==PrimaryWeapon.M7?Armory.valkyrRifle:Armory.Find(Selected);
    public bool IsRifle => Selected==PrimaryWeapon.Collection&&Equipped!=null || Selected == PrimaryWeapon.M7 || Selected == PrimaryWeapon.M14 || Selected == PrimaryWeapon.Type08 || Selected == PrimaryWeapon.Halbreaker || Selected == PrimaryWeapon.NemesisLauncher;
    public bool CanUseSword => CanDeploy;
    public bool CanUseRifle => IsRifle || Selected == PrimaryWeapon.Collection;
    public bool CanDeploy => !GetComponent<PlayerMechLoader>().SelectionBusy&&(Selected == PrimaryWeapon.Collection || (Selected != PrimaryWeapon.None && Equipped != null && Equipped.prefab != null));
    public event Action Changed;
    private void Awake() => Armory = Resources.Load<HangarArmory>("Hangar/Armory");
    public bool Select(PrimaryWeapon weapon)
    {
        if(weapon==PrimaryWeapon.Greatsword)weapon=PrimaryWeapon.M7;
        var gm = GameManager.Instance;
        if (gm != null && ((gm.Phase != GamePhase.Hangar && !(weapon == PrimaryWeapon.Collection && gm.Phase == GamePhase.Loadout)) || gm.IsPaused)) return false;
        if (weapon != PrimaryWeapon.None && weapon != PrimaryWeapon.Collection && (Armory == null || Armory.Find(weapon)?.prefab == null)) return false;
        Selected = weapon;
        GetComponent<WeaponController>()?.ResetCooldowns();
        GetComponent<PlayerMeleeController>()?.ResetCooldown();
        GetComponent<PlayerWeaponStance>()?.ResetStance();
        Changed?.Invoke();
        gm?.equipmentLoop?.ApplyLoadout();
        return true;
    }
    public void InstallRecoveredRifle()
    {
        var gm=GameManager.Instance;
        if(gm?.equipmentLoop?.Absorption?.Installed!=true)return;
        Selected=PrimaryWeapon.Collection;
        GetComponent<WeaponController>()?.ResetCooldowns();
        Changed?.Invoke();
        gm.equipmentLoop.ApplyLoadout();
    }
    public void EnterHangar()
    {
        Selected = Armory != null && Armory.Find(PrimaryWeapon.M7)?.prefab != null
            ? PrimaryWeapon.M7 : Armory != null ? Armory.startingWeapon : PrimaryWeapon.None;
        if(Selected==PrimaryWeapon.None||Selected==PrimaryWeapon.Greatsword)Selected=PrimaryWeapon.M7;
        GetComponent<WeaponController>()?.ResetCooldowns();
        GetComponent<PlayerMeleeController>()?.ResetCooldown();
        GetComponent<PlayerWeaponStance>()?.ResetStance();
        Changed?.Invoke();
    }
}
