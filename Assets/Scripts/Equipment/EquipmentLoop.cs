using System.Collections.Generic;
using System.IO;
using UnityEngine;

public sealed class EquipmentLoop : MonoBehaviour
{
    public SalvageWarehouse Warehouse { get; private set; }
    public SalvageGear Weapon => SalvageGear.Find(Warehouse.Profile.weapon);
    public SalvageGear Backpack => SalvageGear.Find(Warehouse.Profile.backpack);
    public EquipmentWarehouseUI UI { get; private set; }
    public GameManager Owner { get; private set; }
    public int NewThisRun { get; private set; }
    public readonly List<SalvagePickup> Pickups = new List<SalvagePickup>();
    public bool CanEquip => Owner != null && !Owner.IsPaused && (Owner.Phase == GamePhase.Hangar || Owner.Phase == GamePhase.Loadout);
    private GameObject weaponVisual, backpackVisual;
    private int eliteIndex;

    public void Initialize(GameManager owner)
    {
        Owner = owner;
        string profile = System.Environment.GetEnvironmentVariable("MECH_EQUIPMENT_PROFILE");
        if (string.IsNullOrEmpty(profile)) profile = Path.Combine(Application.persistentDataPath, "equipment-warehouse-v1.json");
        Warehouse = new SalvageWarehouse(profile);
        UI = gameObject.AddComponent<EquipmentWarehouseUI>();
        UI.Initialize(this);
    }
    private void Update()
    {
        if (Owner == null || Owner.IsPaused) return;
        if (Owner.IsCombatActive && Input.GetKeyDown(KeyCode.F)) AbsorbNearby();
        if (Owner.Phase == GamePhase.Hangar && Input.GetKeyDown(KeyCode.Tab)) OpenWarehouse();
    }
    public void BeginRun()
    {
        NewThisRun = 0;
        eliteIndex = 0;
        ClearPickups();
        ApplyLoadout();
    }
    public bool TryEquip(string id)
    {
        if (!CanEquip || !Warehouse.Owns(id)) return false;
        if (!Warehouse.Equip(id)) { SaveFailure(); return false; }
        ApplyLoadout();
        UI.Refresh();
        return true;
    }
    public void ApplyLoadout()
    {
        Owner.playerStats.SetBackpackBonuses(Backpack.dashDistance, Backpack.dashCooldown, Backpack.moveSpeed);
        if (weaponVisual != null) Destroy(weaponVisual);
        if (backpackVisual != null) Destroy(backpackVisual);
        var heldRifle=Owner.playerController.GetComponent<E01PlayerRifle>()??Owner.playerController.gameObject.AddComponent<E01PlayerRifle>();
        heldRifle.SetEquipped(Weapon.id=="e01_rifle");
        var hardpoints = Owner.playerController.GetComponent<MechHardpointManager>();
        if (hardpoints == null) return;
        // Additive preview modules. No source model, skeleton, sword, or prefab is rewritten.
        if (Weapon.id != "pulse" && Weapon.id != "e01_rifle")
        {
            weaponVisual = SalvageModuleVisual.Create(Weapon, hardpoints.GetSocket("RightShoulderSocket"));
            weaponVisual.name = "EquipmentPreview_" + Weapon.id;
            weaponVisual.transform.localPosition = new Vector3(.12f, .2f, .04f);
            weaponVisual.transform.localScale = Vector3.one * .65f;
        }
        if (Backpack.id != "standard")
        {
            backpackVisual = SalvageModuleVisual.Create(Backpack, hardpoints.GetSocket("BackSocket"));
            backpackVisual.name = "EquipmentPreview_" + Backpack.id;
            backpackVisual.transform.localPosition = new Vector3(0, .1f, -.16f);
            backpackVisual.transform.localScale = Vector3.one * .9f;
        }
    }
    public void OpenWarehouse()
    {
        if (!CanEquip) return;
        Owner.playerController.CancelMovement();
        Owner.playerController.InputRouter.Clear();
        Owner.hangarUI.Hide();
        UI.Show();
    }
    public void CloseWarehouse()
    {
        if (Owner.Phase == GamePhase.Loadout) Owner.ContinueFromLoadout();
        else { UI.Hide(); Owner.hangarUI.Show(Owner); }
    }
    public void AttachCarrier(EnemyBase enemy)
    {
        if(enemy.TrainingTarget)return;
        var soldier=enemy.GetComponent<E01SoldierMotion>();
        if(soldier!=null)
        {
            var rifleCarrier=enemy.GetComponent<SalvageCarrier>()??enemy.gameObject.AddComponent<SalvageCarrier>();
            rifleCarrier.gear=SalvageGear.Find("e01_rifle");
            rifleCarrier.visual=soldier.rifle.gameObject;
            return;
        }
        string id = enemy.kind == EnemyKind.Ranged ? "scatter" : enemy.kind == EnemyKind.Drone ? "vector"
            : enemy.kind == EnemyKind.Elite ? (eliteIndex++ % 2 == 0 ? "lance" : "salvo") : null;
        if (id == null) return;
        var carrier = enemy.gameObject.AddComponent<SalvageCarrier>();
        carrier.gear = SalvageGear.Find(id);
        carrier.visual = SalvageModuleVisual.Create(carrier.gear, enemy.transform);
        carrier.visual.transform.localPosition = carrier.gear.slot == SalvageSlot.Weapon ? new Vector3(.65f, 1.4f, .2f) : new Vector3(0, 1.1f, -.55f);
        carrier.visual.transform.localScale = Vector3.one * .75f;
    }
    public void DropFrom(EnemyBase enemy)
    {
        if (!Owner.IsCombatActive || enemy.TrainingTarget || Owner.WeaponTrialActive) return;
        var carrier = enemy.GetComponent<SalvageCarrier>();
        if (carrier == null || Warehouse.Owns(carrier.gear.id) || Pickups.Exists(p => p != null && p.Gear.id == carrier.gear.id)) return;
        var root = new GameObject("Recoverable_" + carrier.gear.id);
        root.transform.position = enemy.transform.position + Vector3.up * .6f;
        var pickup = root.AddComponent<SalvagePickup>();
        pickup.Initialize(this, carrier.gear, carrier.visual);
        Pickups.Add(pickup);
        UI.Notify(EquipmentWarehouseUI.T("部件脱落 · 靠近后按 F 吸收", "Part released · approach and press F"));
    }
    public int AbsorbNearby(bool all = false)
    {
        if (!Owner.IsCombatActive && !all) return 0;
        int count = 0;
        for (int i = Pickups.Count - 1; i >= 0; i--)
        {
            var pickup = Pickups[i];
            if (pickup == null) { Pickups.RemoveAt(i); continue; }
            if (!all && Vector3.Distance(pickup.transform.position, Owner.playerController.transform.position) > 10f) continue;
            bool fresh = !Warehouse.Owns(pickup.Gear.id);
            if (!Warehouse.Acquire(pickup.Gear.id)) { SaveFailure(); continue; }
            Pickups.RemoveAt(i);
            if (fresh) NewThisRun++;
            count++;
            pickup.Absorb(Owner.playerController.GetComponent<MechHardpointManager>().GetSocket("RightHandSocket"));
            UI.Notify(pickup.Gear.Title + EquipmentWarehouseUI.T(" · 已永久入库", " · added to permanent collection"));
            GameAudio.Play(GameAudioCue.Reward, .22f, 1.2f);
        }
        return count;
    }
    public void ClearPickups()
    {
        foreach (var pickup in Pickups) if (pickup != null) Destroy(pickup.gameObject);
        Pickups.Clear();
    }
    private void SaveFailure()
    {
        UI.Notify(EquipmentWarehouseUI.T("仓库写入失败，部件仍可再次吸收。", "Save failed. The part can still be absorbed."));
        UI.Refresh();
    }
}

public sealed class SalvageCarrier : MonoBehaviour
{
    public SalvageGear gear;
    public GameObject visual;
}

public sealed class SalvagePickup : MonoBehaviour
{
    public SalvageGear Gear { get; private set; }
    private Transform destination;
    private Vector3 start;
    private float elapsed;
    private GameObject visual;
    private LineRenderer tether;
    public void Initialize(EquipmentLoop owner, SalvageGear gear, GameObject sourceVisual)
    {
        Gear = gear;
        visual = sourceVisual != null ? sourceVisual : SalvageModuleVisual.Create(gear, transform);
        visual.transform.SetParent(transform, false);
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = Quaternion.Euler(10, 25, 0);
    }
    public void Absorb(Transform target)
    {
        destination = target;
        start = transform.position;
        elapsed = 0;
        tether = gameObject.AddComponent<LineRenderer>();
        tether.sharedMaterial = SalvageModuleVisual.Glow;
        tether.startColor = tether.endColor = Gear.color;
        tether.startWidth = .035f;
        tether.endWidth = .008f;
        tether.positionCount = 2;
    }
    private void Update()
    {
        if (destination == null) { visual.transform.Rotate(0, 45f * Time.deltaTime, 0); return; }
        elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(elapsed / .48f);
        transform.position = Vector3.Lerp(start, destination.position, t * t) + Vector3.up * Mathf.Sin(t * Mathf.PI) * .6f;
        transform.localScale = Vector3.one * (1f - t * .85f);
        tether.SetPosition(0, transform.position);
        tether.SetPosition(1, destination.position);
        if (t >= 1f) { CombatEffects.Impact(destination.position, Gear.color, .35f); Destroy(gameObject); }
    }
}

public static class SalvageModuleVisual
{
    private static Material armor, glow;
    public static Material Glow => glow != null ? glow : glow = new Material(Shader.Find("Sprites/Default"));
    public static GameObject Create(SalvageGear gear, Transform parent)
    {
        if(gear.id=="e01_rifle")return Object.Instantiate(Resources.Load<GameObject>("E01/Rifle"),parent,false);
        if (armor == null) armor = new Material(Shader.Find("Standard")) { color = new Color(.12f, .17f, .23f) };
        var root = new GameObject("Module_" + gear.id);
        root.transform.SetParent(parent, false);
        Part(root.transform, new Vector3(0, 0, 0), new Vector3(.65f, .36f, .4f), false, gear.color);
        if (gear.slot == SalvageSlot.Weapon)
        {
            int barrels = gear.id == "scatter" ? 3 : 1;
            for (int i = 0; i < barrels; i++)
            {
                float x = (i - (barrels - 1) * .5f) * .22f;
                Part(root.transform, new Vector3(x, 0, .35f), new Vector3(.15f, .16f, gear.id == "lance" ? .72f : .4f), false, gear.color);
                Part(root.transform, new Vector3(x, .09f, .39f), new Vector3(.10f, .035f, .26f), true, gear.color);
            }
        }
        else
        {
            for (int i = -1; i <= 1; i += 2)
            {
                Part(root.transform, new Vector3(i * .4f, -.06f, -.06f), new Vector3(.28f, gear.id == "vector" ? .85f : .55f, .35f), false, gear.color);
                Part(root.transform, new Vector3(i * .4f, -.32f, -.25f), new Vector3(.22f, .12f, .055f), true, gear.color);
            }
        }
        return root;
    }
    private static void Part(Transform root, Vector3 position, Vector3 size, bool emissive, Color color)
    {
        var obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        obj.transform.SetParent(root, false);
        obj.transform.localPosition = position;
        obj.transform.localScale = size;
        var collider = obj.GetComponent<Collider>();
        collider.enabled = false;
        Object.Destroy(collider);
        var renderer = obj.GetComponent<Renderer>();
        renderer.sharedMaterial = emissive ? Glow : armor;
        if (emissive) { var properties = new MaterialPropertyBlock(); properties.SetColor("_Color", color); renderer.SetPropertyBlock(properties); }
    }
}
