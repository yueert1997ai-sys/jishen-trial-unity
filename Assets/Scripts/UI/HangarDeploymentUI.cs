using UnityEngine;
using UnityEngine.UI;

public class HangarDeploymentUI : MonoBehaviour
{
    private readonly Button[] difficultyButtons = new Button[3];
    private readonly Button[] weaponButtons = new Button[3];
    private readonly PrimaryWeapon[] weapons = { PrimaryWeapon.M7, PrimaryWeapon.Greatsword, PrimaryWeapon.M14 };
    private Canvas canvas;
    private Text details;
    private Button deploy, unload;
    private GameManager gameManager;
    private PlayerLoadout loadout;
    public bool IsVisible => canvas != null && canvas.gameObject.activeSelf;
    private void Awake() { BuildUI(); Hide(); }
    public void Show(GameManager owner)
    {
        gameManager = owner;
        if (loadout != null) loadout.Changed -= Refresh;
        loadout = owner.playerController.GetComponent<PlayerLoadout>();
        loadout.Changed += Refresh;
        BuildUI(); canvas.gameObject.SetActive(true); Refresh();
    }
    public void Hide() { if (canvas != null) canvas.gameObject.SetActive(false); }
    private void OnDestroy() { if (loadout != null) loadout.Changed -= Refresh; }
    public void Refresh()
    {
        if (gameManager == null || loadout == null) return;
        for (int i = 0; i < weaponButtons.Length; i++)
            weaponButtons[i].GetComponent<Image>().color = loadout.Selected == weapons[i] ? new Color(.13f,.43f,.51f) : new Color(.11f,.16f,.21f);
        for (int i = 0; i < difficultyButtons.Length; i++)
            difficultyButtons[i].GetComponent<Image>().color = i == (int)gameManager.Difficulty ? new Color(.13f,.43f,.51f) : new Color(.11f,.16f,.21f);
        details.text = GameText.T(loadout.Selected == PrimaryWeapon.None ? "Unarmed. Select a weapon to deploy."
            : loadout.Selected == PrimaryWeapon.M7 ? "M7 equipped / automatic fire"
            : loadout.Selected == PrimaryWeapon.M14 ? "M14 equipped / heavy piercing rounds"
            : "RAIKEN equipped / close-range slash");
        deploy.interactable = loadout.CanDeploy;
        unload.interactable = loadout.Selected != PrimaryWeapon.None;
    }
    private void BuildUI()
    {
        if (canvas != null) return;
        canvas = RuntimeUIFactory.CreateCanvas("HangarDeploymentCanvas", 960); canvas.sortingOrder = 70;
        var safe = SafeAreaLayout.Create(canvas);
        var title = RuntimeUIFactory.MenuText(safe,"GameTitle","VALKYR / BAY 07",24,new Vector2(180,-40),new Vector2(310,40));
        title.fontStyle = FontStyle.Bold;
        var settings = RuntimeUIFactory.MenuButton(safe,"HangarSettingsButton","Settings",new Vector2(69,-85),new Vector2(92,34));
        settings.onClick.AddListener(() => gameManager.settingsUI.Show(gameManager));
        var body = RuntimeUIFactory.MenuButton(safe,"InspectBody","Full body",new Vector2(82,-448),new Vector2(118,36));
        body.onClick.AddListener(() => gameManager.Hangar.Closeup(false));
        var close = RuntimeUIFactory.MenuButton(safe,"InspectHead","Close-up",new Vector2(210,-448),new Vector2(118,36));
        close.onClick.AddListener(() => gameManager.Hangar.Closeup(true));
        RuntimeUIFactory.MenuText(safe,"InspectHelp","Drag to rotate / scroll to zoom",15,new Vector2(215,-493),new Vector2(385,28));
        var band = RuntimeUIFactory.CreatePanel(safe,"DeploymentBand",Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero,new Color(.025f,.043f,.062f,.97f));
        RuntimeUIFactory.Place(band,new Vector2(1,.5f),new Vector2(-171,0),new Vector2(306,490));
        RuntimeUIFactory.MenuText(band,"LoadoutTitle","PRE-SORTIE LOADOUT",20,new Vector2(153,-35),new Vector2(262,36));
        RuntimeUIFactory.MenuText(band,"LoadoutSubtitle","Choose your primary weapon",12,new Vector2(117,-72),new Vector2(190,26));
        unload = RuntimeUIFactory.MenuButton(band,"UnequipButton","Unequip",new Vector2(250,-72),new Vector2(73,28));
        unload.GetComponentInChildren<Text>().fontSize = 13;
        unload.onClick.AddListener(() => loadout.Select(PrimaryWeapon.None));
        string[] labels = { "M7 / ASSAULT RIFLE\nFast, sustained fire", "RAIKEN / GREATSWORD\nClose-range sweeping attacks", "M14 / BATTLE RIFLE\nSlower, powerful piercing rounds" };
        for (int i = 0; i < 3; i++)
        {
            var choice = weapons[i];
            weaponButtons[i] = RuntimeUIFactory.MenuButton(band,"Equip" + choice,labels[i],new Vector2(153,-131-i*70),new Vector2(262,61));
            weaponButtons[i].GetComponentInChildren<Text>().fontSize = 16;
            weaponButtons[i].onClick.AddListener(() => loadout.Select(choice));
        }
        details = RuntimeUIFactory.MenuText(band,"EquippedDetails","",15,new Vector2(153,-330),new Vector2(262,46));
        string[] difficulties = { "DEMO", "STANDARD", "VETERAN" };
        for (int i = 0; i < 3; i++)
        {
            var difficulty = (RunDifficulty)i;
            var button = RuntimeUIFactory.MenuButton(band,"Difficulty"+difficulties[i],difficulties[i],new Vector2(64+i*89,-386),new Vector2(83,32));
            button.GetComponentInChildren<Text>().fontSize = 12;
            button.onClick.AddListener(() => gameManager.SetDifficulty(difficulty)); difficultyButtons[i] = button;
        }
        deploy = RuntimeUIFactory.MenuButton(band,"DeployButton","DEPLOY",new Vector2(153,-444),new Vector2(262,49));
        deploy.onClick.AddListener(() => gameManager.BeginRun());
    }
}
