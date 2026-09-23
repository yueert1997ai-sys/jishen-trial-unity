using UnityEngine;
using UnityEngine.UI;

// Keep the machine visible. Open equipment and challenge controls only when requested.
public class HangarDeploymentUI : MonoBehaviour
{
    Canvas canvas; GameManager gameManager; PlayerLoadout loadout;
    RectTransform equipmentPanel, morePanel, heroPanel; Text equipped, details;
    Button[] weapons;
    readonly Button[] difficulties = new Button[3];
    PrimaryWeapon[] choices;
    bool nemesis;
    Button deploy, unload, liquid, veteran, range;
    static readonly Color PanelColor = new Color(.035f,.055f,.079f,.95f);
    static readonly Color ButtonColor = new Color(.12f,.17f,.22f,.94f);
    static readonly Color Accent = new Color(.17f,.54f,.79f);
    public bool IsVisible => canvas != null && canvas.gameObject.activeSelf;
    public bool EquipmentExpanded => equipmentPanel != null && equipmentPanel.gameObject.activeSelf;
    public bool MoreExpanded => morePanel != null && morePanel.gameObject.activeSelf;
    static string T(string zh,string en) => EquipmentWarehouseUI.T(zh,en);
    void Awake() { BuildUI(); Hide(); }
    public void Show(GameManager owner)
    {
        gameManager=owner; if(loadout!=null) loadout.Changed-=Refresh;
        loadout=owner.playerController.Loadout; loadout.Changed+=Refresh;
        BuildUI(); ClosePanels(); canvas.gameObject.SetActive(true); Refresh();
    }
    public void Hide() { if(canvas!=null) canvas.gameObject.SetActive(false); }
    void OnDestroy() { if(loadout!=null) loadout.Changed-=Refresh; }
    void Update() { if(IsVisible && Input.GetKeyDown(KeyCode.Escape)) ClosePanels(); }
    public void ClosePanels() { equipmentPanel.gameObject.SetActive(false); morePanel.gameObject.SetActive(false);heroPanel.gameObject.SetActive(false); }
    void Toggle(RectTransform panel) { bool open=!panel.gameObject.activeSelf; ClosePanels(); panel.gameObject.SetActive(open); }
    public void Refresh()
    {
        if(gameManager==null || loadout==null) return;
        if(nemesis!=loadout.IsNemesis)
        {
            bool shown=IsVisible;canvas.gameObject.SetActive(false);Destroy(canvas.gameObject);canvas=null;
            BuildUI();canvas.gameObject.SetActive(shown);
        }
        string[] zh={"M7  突击步枪","M14  战斗步枪","TYPE-08  重型加农炮","AX-01  肩扛粒子炮"};
        string[] en={"M7  ASSAULT RIFLE","M14  BATTLE RIFLE","TYPE-08  HEAVY CANNON","AX-01  HALBREAKER"};
        if(nemesis){zh=new[]{"J-01  光束步枪","J-01  火箭炮"};en=new[]{"J-01  BEAM RIFLE","J-01  ROCKET LAUNCHER"};}
        string selected=loadout.Selected==PrimaryWeapon.Collection ? gameManager.equipmentLoop.Weapon.Title : T("未装备武器","NO WEAPON");
        for(int i=0;i<choices.Length;i++)
        {
            bool active=choices[i]==loadout.Selected;
            weapons[i].GetComponentInChildren<Text>().text=(active?"●  ":"")+T(zh[i],en[i]);
            weapons[i].GetComponent<Image>().color=active ? new Color(.12f,.34f,.49f) : ButtonColor;
            if(active) selected=T(zh[i],en[i]);
        }
        equipped.text=selected+(nemesis?T(" + 光束剑"," + BEAM BLADE"):T(" + 斩舰刀"," + RAIKEN"));
        details.text=nemesis?T("左键枪 · 右键三连斩 · 空格冲刺 · E 浮游炮","LMB gun · RMB combo · Space dash · E drones"):loadout.Selected==PrimaryWeapon.Halbreaker ? T("蓝色贯穿激光 · 右手肩扛","Blue piercing laser · shoulder mounted") :
            loadout.Selected==PrimaryWeapon.Type08 ? T("粉色贯穿光束","Pink piercing beam") :
            loadout.Selected==PrimaryWeapon.Greatsword ? T("逐刀三连斩","Three-hit blade combo") :
            loadout.CanDeploy ? T("左键枪 · 右键刀 · 空格冲刺 · E 支援","LMB gun · RMB blade · Space dash · E support") : T("请先选择武器","Select a weapon first");
        for(int i=0;i<3;i++) difficulties[i].GetComponent<Image>().color=i==(int)gameManager.Difficulty ? Accent : ButtonColor;
        deploy.interactable=range.interactable=veteran.interactable=loadout.CanDeploy;
        liquid.interactable=loadout.CanDeploy && gameManager.stageManager.enemySpawner.liquidBossPrefab!=null;
        unload.interactable=loadout.Selected!=PrimaryWeapon.None;
    }
    Button Button(Transform parent,string name,string zh,string en,Vector2 at,Vector2 size,UnityEngine.Events.UnityAction action,int font=16)
    {
        var b=RuntimeUIFactory.MenuButton(parent,name,T(zh,en),at,size); b.GetComponent<Image>().color=ButtonColor;
        b.GetComponentInChildren<Text>().fontSize=font; b.onClick.AddListener(action); return b;
    }
    RectTransform Panel(Transform parent,string name,Vector2 anchor,Vector2 at,Vector2 size)
    {
        var p=RuntimeUIFactory.CreatePanel(parent,name,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero,PanelColor);
        RuntimeUIFactory.Place(p,anchor,at,size); return p;
    }
    void BuildUI()
    {
        if(canvas!=null) return;
        nemesis=loadout==null||loadout.IsNemesis;
        choices=nemesis?new[]{PrimaryWeapon.M7,PrimaryWeapon.NemesisLauncher}:new[]{PrimaryWeapon.M7,PrimaryWeapon.M14,PrimaryWeapon.Type08,PrimaryWeapon.Halbreaker};weapons=new Button[choices.Length];
        canvas=RuntimeUIFactory.CreateCanvas("HangarDeploymentCanvas",960); canvas.sortingOrder=70; canvas.GetComponent<CanvasScaler>().screenMatchMode=CanvasScaler.ScreenMatchMode.Expand;
        var safe=SafeAreaLayout.Create(canvas);
        var header=Panel(safe,"QuietHeader",new Vector2(0,1),new Vector2(122,-39),new Vector2(200,44));
        var title=RuntimeUIFactory.MenuText(header,"GameTitle",nemesis?"NEMESIS / J-01":"VALKYR  /  BAY 07",17,new Vector2(100,-22),new Vector2(174,30)); title.fontStyle=FontStyle.Bold;
        var heroButton=Button(safe,"ChooseHeroButton","切换机体","Change mech",Vector2.zero,new Vector2(108,38),()=>Toggle(heroPanel),15);
        RuntimeUIFactory.Place(heroButton.GetComponent<RectTransform>(),new Vector2(0,1),new Vector2(286,-39),new Vector2(108,38));
        heroPanel=Panel(safe,"HeroDrawer",new Vector2(0,1),new Vector2(184,-166),new Vector2(324,186));
        RuntimeUIFactory.MenuText(heroPanel,"HeroTitle",T("选择出击机体","Select your mech"),19,new Vector2(126,-25),new Vector2(224,32));
        Button(heroPanel,"CloseHero","关闭","Close",new Vector2(279,-25),new Vector2(64,28),ClosePanels,13);
        Button(heroPanel,"SelectNemesis",(nemesis?"●  ":"")+"冥隼  ·  J-01 NEMESIS",(nemesis?"●  ":"")+"J-01 NEMESIS",new Vector2(162,-78),new Vector2(292,45),()=>SelectHero(HeroMech.Nemesis),16);
        Button(heroPanel,"SelectValkyr",(!nemesis?"●  ":"")+"瓦尔基里  ·  VALKYR",(!nemesis?"●  ":"")+"VALKYR",new Vector2(162,-134),new Vector2(292,45),()=>SelectHero(HeroMech.Valkyr),16);
        var settings=Button(safe,"HangarSettingsButton","设置","Settings",Vector2.zero,new Vector2(70,34),()=>gameManager.settingsUI.Show(gameManager),14);
        RuntimeUIFactory.Place(settings.GetComponent<RectTransform>(),Vector2.one,new Vector2(-58,-39),new Vector2(70,34));
        var bar=Panel(safe,"CompactDeployment",new Vector2(.5f,0),new Vector2(0,59),new Vector2(910,84));
        equipped=RuntimeUIFactory.MenuText(bar,"EquippedName","",19,new Vector2(176,-27),new Vector2(316,30)); equipped.fontStyle=FontStyle.Bold;
        details=RuntimeUIFactory.MenuText(bar,"EquippedDetails","",13,new Vector2(176,-56),new Vector2(316,24)); details.color=new Color(.65f,.78f,.87f);
        Button(bar,"ToggleEquipment","配装","Loadout",new Vector2(411,-42),new Vector2(92,42),()=>Toggle(equipmentPanel));
        range=Button(bar,"WeaponTrialButton","零强化试场","Practice",new Vector2(514,-42),new Vector2(92,42),()=>gameManager.BeginShortCombat(),13);
        Button(bar,"ToggleMore","更多","More",new Vector2(617,-42),new Vector2(92,42),()=>Toggle(morePanel));
        deploy=Button(bar,"DeployButton","完整流程 [E]","FULL RUN [E]",new Vector2(786,-42),new Vector2(202,48),()=>gameManager.BeginFullDemo(),20);
        deploy.GetComponent<Image>().color=Accent;
        equipmentPanel=Panel(safe,"EquipmentDrawer",new Vector2(1,0),new Vector2(-183,301),new Vector2(318,380));
        RuntimeUIFactory.MenuText(equipmentPanel,"LoadoutTitle",T("远程武器 · 固定携刀","Gun + fixed blade"),20,new Vector2(120,-28),new Vector2(206,34));
        Button(equipmentPanel,"CloseEquipment","关闭","Close",new Vector2(274,-28),new Vector2(64,28),ClosePanels,13);
        for(int i=0;i<choices.Length;i++)
        {
            var choice=choices[i]; weapons[i]=Button(equipmentPanel,"Equip"+choice,"","",new Vector2(159,-76-i*49),new Vector2(284,43),()=>{loadout.Select(choice);ClosePanels();});
        }
        unload=Button(equipmentPanel,"UnequipButton","卸下","Unequip",new Vector2(67,-340),new Vector2(98,34),()=>loadout.Select(PrimaryWeapon.None),14);
        Button(equipmentPanel,"OpenWarehouseButton","装备仓库 [Tab]","Collection [Tab]",new Vector2(216,-340),new Vector2(169,34),()=>{ClosePanels();gameManager.equipmentLoop.OpenWarehouse();},14);
        morePanel=Panel(safe,"MoreDrawer",new Vector2(1,0),new Vector2(-183,270),new Vector2(318,318));
        RuntimeUIFactory.MenuText(morePanel,"MoreTitle",T("挑战与查看","Challenges & view"),20,new Vector2(122,-28),new Vector2(206,34));
        Button(morePanel,"CloseMore","关闭","Close",new Vector2(274,-28),new Vector2(64,28),ClosePanels,13);
        liquid=Button(morePanel,"LiquidBossChallengeButton","挑战液态 Boss","LIQUID BOSS",new Vector2(159,-77),new Vector2(284,40),()=>gameManager.BeginLiquidBossChallenge());
        veteran=Button(morePanel,"BossTrialButton","苍钢卫士 · 试战","STEEL VETERAN",new Vector2(159,-125),new Vector2(284,40),()=>gameManager.BeginBossPreview());
        for(int i=0;i<3;i++)
        {
            var d=(RunDifficulty)i;
            difficulties[i]=Button(morePanel,"Difficulty"+d,new[]{"体验","标准","老兵"}[i],new[]{"Cadet","Standard","Veteran"}[i],new Vector2(64+i*95,-179),new Vector2(88,32),()=>gameManager.SetDifficulty(d),14);
        }
        Button(morePanel,"InspectBody","全身","Full body",new Vector2(89,-232),new Vector2(138,34),()=>{gameManager.Hangar.Closeup(false);ClosePanels();},14);
        Button(morePanel,"InspectHead","近看","Close-up",new Vector2(234,-232),new Vector2(138,34),()=>{gameManager.Hangar.Closeup(true);ClosePanels();},14);
        RuntimeUIFactory.MenuText(morePanel,"InspectHelp",T("拖动旋转 · 滚轮缩放","Drag to orbit · Scroll to zoom"),13,new Vector2(159,-286),new Vector2(284,24));
        ClosePanels();
    }
    void SelectHero(HeroMech hero)
    {
        if(gameManager.playerController.GetComponent<PlayerMechLoader>().SelectHero(hero))ClosePanels();
    }
}
