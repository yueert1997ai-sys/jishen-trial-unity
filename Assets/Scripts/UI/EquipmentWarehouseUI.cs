using UnityEngine;
using UnityEngine.UI;

public sealed class EquipmentWarehouseUI : MonoBehaviour
{
    private EquipmentLoop loop;
    private Canvas canvas, hud;
    private RectTransform panel, cards;
    private Text subtitle, notification, loadout, absorptionStatus;
    private Button continueButton, absorbButton;
    private float notificationUntil;
    private bool lastChinese;
    public bool IsVisible => canvas != null && canvas.gameObject.activeSelf;
    public static string T(string chinese, string english) => GamePreferences.Chinese ? chinese : english;
    public void Initialize(EquipmentLoop owner)
    {
        loop = owner;
        hud = RuntimeUIFactory.CreateCanvas("EquipmentLoopHUD", 960);
        hud.sortingOrder = 28;
        notification = RuntimeUIFactory.MenuText(hud.transform, "AbsorptionNotice", "", 19, Vector2.zero, new Vector2(660, 38), TextAnchor.MiddleCenter);
        RuntimeUIFactory.Place(notification.rectTransform, new Vector2(.5f, 1), new Vector2(0, -114), new Vector2(660, 38));
        loadout = RuntimeUIFactory.MenuText(hud.transform, "CurrentLoadout", "", 15, Vector2.zero, new Vector2(610, 30), TextAnchor.MiddleCenter);
        RuntimeUIFactory.Place(loadout.rectTransform, new Vector2(.5f, 1), new Vector2(0, -80), new Vector2(560, 28));
        absorbButton = RuntimeUIFactory.MenuButton(hud.transform, "AbsorbButton", "", Vector2.zero, new Vector2(220, 46));
        RuntimeUIFactory.Place(absorbButton.GetComponent<RectTransform>(), new Vector2(.5f, 0), new Vector2(0, 84), new Vector2(220, 46));
        absorbButton.onClick.AddListener(() => loop.AbsorbNearby());
        absorptionStatus=RuntimeUIFactory.MenuText(hud.transform,"AbsorptionStage","",18,Vector2.zero,new Vector2(660,36),TextAnchor.MiddleCenter);
        RuntimeUIFactory.Place(absorptionStatus.rectTransform,new Vector2(.5f,0),new Vector2(0,194),new Vector2(660,36));
        absorptionStatus.raycastTarget=false;
    }
    private void Update()
    {
        if (loop == null) return;
        bool combat = loop.Owner.IsCombatActive;
        hud.gameObject.SetActive(combat);
        if (combat)
        {
            loadout.gameObject.SetActive(!loop.Owner.combatHUD.IsVisible && loop.Owner.stageManager.CurrentEncounter != 6 && !loop.Owner.WeaponTrialActive);
            notification.gameObject.SetActive(Time.unscaledTime < notificationUntil);
            var primary = loop.Owner.playerController.Loadout;
            string weapon = primary == null || primary.Selected == PrimaryWeapon.Collection ? loop.Weapon.Title
                : primary.Selected == PrimaryWeapon.Greatsword ? "RAIKEN Mk-II" : primary.Selected == PrimaryWeapon.Halbreaker ? "AX-01 HALBREAKER" : primary.Selected.ToString();
            loadout.text = weapon + "  /  " + loop.Backpack.Title;
            var intake=loop.Absorption;
            absorbButton.gameObject.SetActive(intake.Offering);
            absorbButton.interactable=intake.CanAbsorb;
            absorbButton.GetComponentInChildren<Text>().text=intake.CanAbsorb?T("F  夺取并试装", "F  RECOVER AND INSTALL"):T("靠近步枪残骸 · 5.5m", "APPROACH RIFLE · 5.5m");
            absorptionStatus.text=intake.StatusText;
            absorptionStatus.gameObject.SetActive(!string.IsNullOrEmpty(intake.StatusText));
        }
        if (IsVisible && lastChinese != GamePreferences.Chinese) Refresh();
    }
    public void Notify(string message) { notification.text = message; notificationUntil = Time.unscaledTime + 4f; }
    public void Show()
    {
        if (canvas == null)
        {
            canvas = RuntimeUIFactory.CreateCanvas("EquipmentWarehouseCanvas", 960);
            canvas.sortingOrder = 80;
            panel = RuntimeUIFactory.CreateMenuSurface(canvas, "EquipmentWarehousePanel", new Vector2(890, 528));
            subtitle = RuntimeUIFactory.MenuText(panel, "WarehouseSubtitle", "", 15, new Vector2(445, -70), new Vector2(834, 30));
            continueButton = RuntimeUIFactory.MenuButton(panel, "WarehouseContinueButton", "", new Vector2(734, -484), new Vector2(254, 48));
            continueButton.onClick.AddListener(() => loop.CloseWarehouse());
        }
        canvas.gameObject.SetActive(true);
        Refresh();
    }
    public void Hide() { if (canvas != null) canvas.gameObject.SetActive(false); }
    public void Refresh()
    {
        if (panel == null) return;
        lastChinese = GamePreferences.Chinese;
        if (cards != null) { cards.gameObject.SetActive(false); Destroy(cards.gameObject); }
        cards = new GameObject("GearCards", typeof(RectTransform)).GetComponent<RectTransform>();
        cards.SetParent(panel, false);
        cards.anchorMin = Vector2.zero; cards.anchorMax = Vector2.one; cards.offsetMin = cards.offsetMax = Vector2.zero;
        RuntimeUIFactory.MenuText(cards, "WarehouseTitle", T("装备仓库", "EQUIPMENT COLLECTION"), 27, new Vector2(445, -34), new Vector2(834, 40));
        subtitle.text = T("永久收藏 ", "OWNED ") + loop.Warehouse.Profile.owned.Count + " / " + SalvageGear.All.Length + "  ·  " + T("本局新获得 ", "NEW THIS RUN ") + loop.NewThisRun;
        if (!string.IsNullOrEmpty(loop.Warehouse.Error)) subtitle.text = T("保存失败，当前装备保持不变。请检查磁盘空间后重试。", "Save failed. Equipment unchanged. Check free disk space and retry.");
        RuntimeUIFactory.MenuText(cards, "WeaponColumn", T("射击武器", "CANNON"), 16, new Vector2(228, -106), new Vector2(398, 26));
        RuntimeUIFactory.MenuText(cards, "BackpackColumn", T("背包", "BACKPACK"), 16, new Vector2(661, -106), new Vector2(398, 26));
        int weaponRow = 0, backpackRow = 0;
        for (int i = 0; i < SalvageGear.All.Length; i++)
        {
            var gear = SalvageGear.All[i];
            bool owned = loop.Warehouse.Owns(gear.id);
            bool selected = (gear.id == loop.Warehouse.Profile.weapon && loop.Owner.playerController.Loadout.Selected == PrimaryWeapon.Collection) || gear.id == loop.Backpack.id;
            var card = RuntimeUIFactory.CreatePanel(cards, "GearCard_" + gear.id, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
                selected ? new Color(.11f, .24f, .25f) : owned ? new Color(.13f, .16f, .18f) : new Color(.09f, .11f, .13f));
            bool isWeapon = gear.slot == SalvageSlot.Weapon;
            int row = isWeapon ? weaponRow++ : backpackRow++;
            RuntimeUIFactory.Place(card, new Vector2(0, 1), new Vector2(isWeapon ? 228 : 661, -165 - row * 82), new Vector2(398, 74));
            var heading = RuntimeUIFactory.MenuText(card, "GearName", gear.Title, 19, new Vector2(150, -22), new Vector2(270, 28));
            heading.color = owned ? Color.white : new Color(.55f, .59f, .62f);
            RuntimeUIFactory.MenuText(card, "GearDescription", owned ? gear.Description : gear.Source, 13, new Vector2(150, -52), new Vector2(270, 34));
            var button = RuntimeUIFactory.MenuButton(card, "Equip_" + gear.id, !isWeapon ? selected ? T("固定", "FIXED") : owned ? T("已收藏", "OWNED") : T("未获取", "LOCKED") : selected ? T("已装备", "EQUIPPED") : owned ? T("装备", "EQUIP") : T("未获取", "LOCKED"), new Vector2(340, -37), new Vector2(90, 44));
            button.GetComponentInChildren<Text>().fontSize = 13;
            button.interactable = isWeapon && owned && !selected;
            button.onClick.AddListener(() => loop.TryEquip(gear.id));
        }
        RuntimeUIFactory.MenuText(cards, "PersistenceNote", T("收藏永久保留 · 当前机体使用固定背包\n强化仅本局生效，结算后清空", "Collection persists. This mech uses a fixed backpack.\nUpgrades expire when this run ends."), 15, new Vector2(296, -484), new Vector2(534, 46));
        continueButton.GetComponentInChildren<Text>().text = loop.Owner.Phase == GamePhase.Loadout ? T("保持当前搭配 · 继续", "CONTINUE WITH LOADOUT") : T("返回出击准备", "BACK TO DEPLOYMENT");
        continueButton.transform.SetAsLastSibling();
    }
    private void OnDestroy()
    {
        if (canvas != null) Destroy(canvas.gameObject);
        if (hud != null) Destroy(hud.gameObject);
    }
}
