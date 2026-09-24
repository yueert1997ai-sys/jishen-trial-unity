using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class CombatHUD : MonoBehaviour
{
    public GameManager gameManager;
    public PlayerStats playerStats;
    public EquipmentManager equipmentManager;
    public RunUpgradeSystem upgradeSystem;
    public bool IsVisible => canvas != null && canvas.gameObject.activeSelf;
    Canvas canvas;
    RectTransform safeRoot, hpFill, hpTrail, energyFill, bossFill, bossPanel, crosshair, dashFill, supportFill;
    Text hpText, energyText, statusText, objectiveText, buildText, bossText, mechText, dashText, supportText, timerText, weaponStateText;
    readonly System.Collections.Generic.List<Image> aimMarks=new System.Collections.Generic.List<Image>(9);
    Damageable playerDamageable, bossDamageable;
    PlayerController player;
    BossController boss;
    P0CombatDemo demo;
    Image damageOverlay;
    Button continueButton;
    float flash, nextRefresh, nextLookup, trailingHp=1, trailHold, supportDuration=1;
    float nextWeaponRefresh;int lastWeaponRounds=-1,lastRangeBand=-1;bool lastServicing;
    static string T(string zh,string en) => EquipmentWarehouseUI.T(zh,en);

    void Start()
    {
        BuildUI();player=playerStats!=null?playerStats.GetComponent<PlayerController>():null;
        playerDamageable=playerStats!=null?playerStats.GetComponent<Damageable>():null;
        if(playerDamageable!=null)playerDamageable.OnDamaged+=OnDamage;
        SetVisible(gameManager!=null && gameManager.Phase==GamePhase.Combat);
    }
    void OnDestroy(){if(playerDamageable!=null)playerDamageable.OnDamaged-=OnDamage;Cursor.visible=true;}
    public void SetVisible(bool value)
    {
        BuildUI();canvas.gameObject.SetActive(value);nextRefresh=nextLookup=nextWeaponRefresh=0;lastWeaponRounds=-1;
        if(!value){flash=0;trailingHp=1;supportDuration=1;Cursor.visible=true;}
    }
    void Update()
    {
        if(!IsVisible)return;
        if(gameManager==null)gameManager=GameManager.Instance;
        if(player==null && gameManager!=null)player=gameManager.playerController;
        if(player==null || gameManager==null || playerStats==null)return;
        flash=Mathf.MoveTowards(flash,0,Time.unscaledDeltaTime*3);
        damageOverlay.color=new Color(.8f,.13f,.08f,flash*.08f);UpdateAim();
        float hp=Mathf.Clamp01(playerStats.CurrentHp/Mathf.Max(1,playerStats.MaxHp));
        if(hp>=trailingHp)trailingHp=hp;
        else if(Time.unscaledTime>trailHold)trailingHp=Mathf.MoveTowards(trailingHp,hp,Time.unscaledDeltaTime*.65f);
        SetFill(hpFill,hp);SetFill(hpTrail,trailingHp);
        hpFill.GetComponent<Image>().color=hp<=.25f?GameUITheme.Danger:GameUITheme.Health;
        SetFill(energyFill,playerStats.CurrentEnergy/Mathf.Max(1,playerStats.MaxEnergy));
        float dash=player.DashCooldownRemaining,support=player.weaponController.SkillCooldownRemaining;
        supportDuration=Mathf.Max(supportDuration,support);
        SetFill(dashFill,1-dash/Mathf.Max(.01f,playerStats.DashCooldown));SetFill(supportFill,1-support/supportDuration);
        bool collect=demo!=null && gameManager.stageManager.EnemiesAlive==0 && gameManager.equipmentLoop.Absorption.Offering;
        continueButton.gameObject.SetActive((gameManager.AwaitingContinue||collect)&&!gameManager.IsPaused);
        continueButton.interactable=!gameManager.equipmentLoop.Absorption.Busy;
        continueButton.GetComponentInChildren<Text>().text=collect?T("只收藏并继续  [Enter]","Collect & continue  [Enter]"):T("继续 · 选择强化  [Enter]","Continue · upgrade  [Enter]");
        UpdateWeaponStatus();
        if(Time.unscaledTime<nextRefresh)return;
        nextRefresh=Time.unscaledTime+.1f;
        if(Time.unscaledTime>=nextLookup)
        {
            nextLookup=Time.unscaledTime+.3f;demo=FindFirstObjectByType<P0CombatDemo>();
            boss=FindFirstObjectByType<BossController>();bossDamageable=boss!=null?boss.GetComponent<Damageable>():null;
        }
        mechText.text=(player.Loadout.IsNemesis?"J-01  NEMESIS":"VALKYR")+"  /  "+T("机体状态","SYSTEM STATUS");
        hpText.text=Mathf.CeilToInt(playerStats.CurrentHp)+" <size=12>/ "+Mathf.CeilToInt(playerStats.MaxHp)+"</size>";
        energyText.text=T("推进能量  ","ENERGY  ")+Mathf.CeilToInt(playerStats.CurrentEnergy);
        statusText.text=T("击破  ","KILLS  ")+gameManager.Kills+"     "+T("敌机  ","HOSTILES  ")+gameManager.stageManager.EnemiesAlive;
        bool shortCombat=CombatRuntime.Run!=null&&CombatRuntime.Run.Mode==CombatMode.ShortCombat;
        objectiveText.text=shortCombat?(CombatLabSettings.Active?T("对照试场","COMBAT LAB"):T("基础战斗试场","COMBAT TRIAL"))+(demo!=null?"   "+Mathf.Min(demo.SliceGroup,P0CombatDemo.SliceGroupCount)+" / "+P0CombatDemo.SliceGroupCount:""):GameText.Progress(gameManager.ProgressText);
        int seconds=Mathf.FloorToInt(demo!=null?demo.Elapsed:gameManager.GetRunTime());
        timerText.text=(seconds/60).ToString("00")+":"+(seconds%60).ToString("00");
        string gun=player.Loadout.Selected==PrimaryWeapon.Collection?gameManager.equipmentLoop.Weapon.Title:player.Loadout.Selected.ToString();
        if(player.Loadout.IsNemesis&&player.Loadout.Selected==PrimaryWeapon.M7)gun=T("光束步枪","BEAM RIFLE");
        buildText.text=T("左键  ","LMB  ")+gun+"    ·    "+T("右键  ","RMB  ")+T("斩舰刀","RAIKEN");
        dashText.text=T("空格 · 冲刺  ","SPACE · DASH  ")+(dash>.01f?dash.ToString("F1")+"s":T("就绪","READY"));
        supportText.text="E · "+(player.Loadout.IsNemesis?T("浮游炮  ","DRONES  "):T("背炮齐射  ","BACK CANNON  "))+(support>.01f?support.ToString("F1")+"s":T("就绪","READY"));
        bool showBoss=bossDamageable!=null&&!bossDamageable.IsDead;bossPanel.gameObject.SetActive(showBoss);
        if(showBoss)
        {
            bossText.text=boss.DisplayName+"   ·   "+(boss.CoreExposed?T("露核","CORE EXPOSED"):T("装甲","ARMORED"));
            SetFill(bossFill,bossDamageable.CurrentHealth/Mathf.Max(1,bossDamageable.maxHealth));
            bossFill.GetComponent<Image>().color=boss.CoreExposed?GameUITheme.Energy:GameUITheme.Danger;
        }
    }
    void UpdateWeaponStatus()
    {
        var weapon=player.weaponController;var handling=weapon.Handling;
        float recovery=weapon.PrimaryRecoveryRemaining;
        float distance=Vector3.Distance(PlanarCombat.Point(player.transform.position),PlanarCombat.Point(player.AimPoint));
        int band=!handling.HasRange||!player.HasAimPoint||distance<=handling.OptimalRange?0:distance>handling.MaximumRange?2:1;
        int rounds=weapon.PrimaryRoundsRemaining;bool servicing=recovery>0;
        // State changes are immediate; countdowns share the simulation clock they describe.
        if(rounds==lastWeaponRounds&&band==lastRangeBand&&servicing==lastServicing&&Time.time<nextWeaponRefresh)return;
        lastWeaponRounds=rounds;lastRangeBand=band;lastServicing=servicing;nextWeaponRefresh=Time.time+.05f;
        weaponStateText.color=servicing||band>0?GameUITheme.Armor:GameUITheme.Muted;
        weaponStateText.text=servicing?T("自动整备  ","AUTO RELOAD  ")+recovery.ToString("F1")+"s":
            handling.Capacity>0?rounds+" / "+handling.Capacity+"   ·   "+
                (band>0?(band==2?T("超出射程","OUT OF RANGE"):T("远距衰减","RANGE FALLOFF")):T("有效射程 ","OPTIMAL ")+handling.OptimalRange.ToString("F0")+"m"):
                weapon.PrimaryCooldownRemaining>.01f?T("冷却  ","COOLDOWN  ")+weapon.PrimaryCooldownRemaining.ToString("F1")+"s":T("射击就绪","WEAPON READY");
    }
    void BuildUI()
    {
        if(canvas!=null)return;
        canvas=RuntimeUIFactory.CreateCanvas("CombatHUDCanvas",960);canvas.sortingOrder=3;
        damageOverlay=RuntimeUIFactory.CreatePanel(canvas.transform,"DamageOverlay",Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero,Color.clear).GetComponent<Image>();damageOverlay.raycastTarget=false;
        safeRoot=SafeAreaLayout.Create(canvas);
        var mission=Panel(safeRoot,"MissionPanel",new Vector2(0,1),new Vector2(150,-48),new Vector2(260,58));
        objectiveText=Label(mission,"ObjectiveText",15,new Vector2(12,-10),new Vector2(238,23));
        statusText=Label(mission,"StatusText",11,new Vector2(12,-35),new Vector2(178,17));statusText.color=GameUITheme.Muted;
        timerText=Label(mission,"TimerText",12,new Vector2(204,-34),new Vector2(44,19));timerText.alignment=TextAnchor.MiddleRight;
        var vitality=Panel(safeRoot,"StatusPanel",Vector2.zero,new Vector2(166,64),new Vector2(292,116));
        mechText=Label(vitality,"MechText",11,new Vector2(14,-9),new Vector2(264,22));mechText.color=GameUITheme.Muted;
        var hpLabel=Label(vitality,"HealthLabel",12,new Vector2(14,-36),new Vector2(70,24));hpLabel.text=T("耐久","HULL");
        hpText=Label(vitality,"HpText",24,new Vector2(84,-19),new Vector2(194,48));hpText.alignment=TextAnchor.MiddleRight;
        var healthTrack=Track(vitality,"HpBar",new Vector2(14,-70),new Vector2(264,10));
        hpTrail=Fill(healthTrack,"DamageTrail",GameUITheme.Armor);hpFill=Fill(healthTrack,"Fill",GameUITheme.Health);
        energyText=Label(vitality,"EnergyText",11,new Vector2(14,-78),new Vector2(264,17));energyText.color=GameUITheme.Muted;
        energyFill=Fill(Track(vitality,"EnergyBar",new Vector2(14,-104),new Vector2(264,4)),"Fill",GameUITheme.Energy);
        var actions=Panel(safeRoot,"ActionPanel",new Vector2(1,0),new Vector2(-183,78),new Vector2(326,112));
        buildText=Label(actions,"BuildText",12,new Vector2(14,-10),new Vector2(298,23));
        weaponStateText=Label(actions,"WeaponStateText",11,new Vector2(14,-34),new Vector2(298,18));
        dashText=Label(actions,"DashText",11,new Vector2(14,-63),new Vector2(144,23));
        supportText=Label(actions,"SupportText",11,new Vector2(176,-63),new Vector2(138,23));
        dashFill=Fill(Track(actions,"DashBar",new Vector2(14,-96),new Vector2(132,3)),"Fill",GameUITheme.Energy);
        supportFill=Fill(Track(actions,"SupportBar",new Vector2(176,-96),new Vector2(136,3)),"Fill",GameUITheme.Health);
        bossPanel=Panel(safeRoot,"BossPanel",new Vector2(.5f,1),new Vector2(0,-110),new Vector2(354,55));
        bossText=Label(bossPanel,"BossText",12,new Vector2(12,-8),new Vector2(330,22));bossText.alignment=TextAnchor.MiddleCenter;
        bossFill=Fill(Track(bossPanel,"BossHealth",new Vector2(12,-42),new Vector2(330,6)),"Fill",GameUITheme.Danger);bossPanel.gameObject.SetActive(false);
        var pause=RuntimeUIFactory.CreateButton(safeRoot,"PauseButton",T("暂停  Esc","Pause  Esc"));
        RuntimeUIFactory.Place(pause.GetComponent<RectTransform>(),Vector2.one,new Vector2(-64,-35),new Vector2(88,30));pause.GetComponentInChildren<Text>().fontSize=12;pause.onClick.AddListener(()=>gameManager.TogglePause());
        continueButton=RuntimeUIFactory.CreateButton(safeRoot,"ContinueAfterSalvage","");
        RuntimeUIFactory.Place(continueButton.GetComponent<RectTransform>(),new Vector2(.5f,0),new Vector2(0,139),new Vector2(240,34));continueButton.GetComponentInChildren<Text>().fontSize=13;
        continueButton.onClick.AddListener(()=>{if(gameManager.AwaitingContinue)gameManager.ContinueAfterSalvage();else gameManager.equipmentLoop.Absorption.CollectWithoutInstalling();});continueButton.gameObject.SetActive(false);
        crosshair=new GameObject("CombatCrosshair",typeof(RectTransform)).GetComponent<RectTransform>();crosshair.SetParent(safeRoot,false);
        RuntimeUIFactory.Place(crosshair,Vector2.one*.5f,Vector2.zero,new Vector2(16,16));
        for(int y=-1;y<=1;y+=2)for(int x=-1;x<=1;x+=2)
        {Mark(new Vector2(x*6,y*8),new Vector2(5,1.5f));Mark(new Vector2(x*8,y*6),new Vector2(1.5f,5));}
        Mark(Vector2.zero,new Vector2(2,2));
    }
    void Mark(Vector2 at,Vector2 size)
    {
        var mark=RuntimeUIFactory.CreatePanel(crosshair,"AimMark",Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero,GameUITheme.Text);
        RuntimeUIFactory.Place(mark,Vector2.one*.5f,at,size);mark.GetComponent<Image>().raycastTarget=false;
        aimMarks.Add(mark.GetComponent<Image>());
        var shadow=mark.gameObject.AddComponent<Shadow>();shadow.effectColor=new Color(0,0,0,.8f);shadow.effectDistance=new Vector2(1,-1);
    }
    static RectTransform Panel(Transform parent,string name,Vector2 anchor,Vector2 at,Vector2 size)
    {
        var panel=RuntimeUIFactory.CreatePanel(parent,name,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero,GameUITheme.Panel);
        RuntimeUIFactory.Place(panel,anchor,at,size);panel.GetComponent<Image>().raycastTarget=false;return panel;
    }
    static Text Label(Transform parent,string name,int size,Vector2 topLeft,Vector2 dimensions)
    {
        var text=RuntimeUIFactory.CreateText(parent,name,"",size,TextAnchor.MiddleLeft,GameUITheme.Text);text.raycastTarget=false;
        text.horizontalOverflow=HorizontalWrapMode.Wrap;text.verticalOverflow=VerticalWrapMode.Truncate;
        RuntimeUIFactory.Place(text.rectTransform,new Vector2(0,1),topLeft+new Vector2(dimensions.x*.5f,-dimensions.y*.5f),dimensions);return text;
    }
    static RectTransform Track(Transform parent,string name,Vector2 topLeft,Vector2 size)
    {
        var rect=Panel(parent,name,new Vector2(0,1),topLeft+new Vector2(size.x*.5f,-size.y*.5f),size);rect.GetComponent<Image>().color=GameUITheme.Track;return rect;
    }
    static RectTransform Fill(Transform parent,string name,Color color)
    {
        var rect=RuntimeUIFactory.CreatePanel(parent,name,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero,color);rect.GetComponent<Image>().raycastTarget=false;return rect;
    }
    void UpdateAim()
    {
        bool show=player.HasAimPoint&&Camera.main!=null&&!gameManager.IsPaused
            && !(EventSystem.current!=null&&EventSystem.current.IsPointerOverGameObject());
        Vector3 screen=show?Camera.main.WorldToScreenPoint(player.AimPoint):Vector3.zero;
        show=show&&screen.z>0&&screen.x>=0&&screen.x<=Screen.width&&screen.y>=0&&screen.y<=Screen.height;
        crosshair.gameObject.SetActive(show);Cursor.visible=!show;
        if(show)
        {
            var handling=player.weaponController.Handling;
            float distance=Vector3.Distance(PlanarCombat.Point(player.transform.position),PlanarCombat.Point(player.AimPoint));
            Color tint=player.weaponController.PrimaryRecoveryRemaining>0?GameUITheme.Muted:
                handling.HasRange&&distance>handling.OptimalRange?GameUITheme.Armor:GameUITheme.Text;
            foreach(var mark in aimMarks)mark.color=tint;
        }
        if(show&&RectTransformUtility.ScreenPointToLocalPointInRectangle(safeRoot,screen,null,out Vector2 point))crosshair.anchoredPosition=point;
    }
    void OnDamage(Damageable target,DamageInfo info){flash=1;trailHold=Time.unscaledTime+.3f;}
    static void SetFill(RectTransform fill,float ratio){fill.anchorMax=new Vector2(Mathf.Clamp01(ratio),1);fill.offsetMin=fill.offsetMax=Vector2.zero;}
}
