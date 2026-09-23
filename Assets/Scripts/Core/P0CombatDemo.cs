using System;
using System.Collections;
using UnityEngine;

// Explicit local launch mode; no campaign expansion or persistent progression.
public sealed class P0CombatDemo : MonoBehaviour
{
    public static bool Enabled {get;}=DetectMode();
    static bool DetectMode()
    {
        var args=Environment.GetCommandLineArgs();
        if(Array.IndexOf(args,"-legacyCombat")>=0||Array.IndexOf(args,"-fullDemo")>=0)return false;
#if UNITY_EDITOR
        return CombatSliceSettings.Enabled || Array.IndexOf(args,"-p0Check")>=0;
#else
        return CombatSliceSettings.Enabled || Array.IndexOf(args,"-p0Demo")>=0 || Array.IndexOf(args,"-p0Check")>=0;
#endif
    }
    public const float Duration=300;
    public float Elapsed {get;private set;}
    public bool Training {get;private set;}
    public bool Finished {get;private set;}
    public int LabGroups => ownsLab && director!=null ? director.Flow.Groups : 0;
    bool ownsLab;
    CombatEncounterDirector director;
    public EncounterFlow Encounter => director?.Flow;
    public int SliceGroup => !ownsLab && director!=null ? director.Flow.Groups : 0;
    public static int SliceGroupCount => CombatLoopV2.Enabled?CombatLoopV2.EncounterGroups:12;
    public PrimaryWeapon SliceWeapon {get;private set;}=PrimaryWeapon.M7;
    public BossController SliceBoss => director?.Boss;
    public void SetSliceWeapon(PrimaryWeapon value)
    {if(value==PrimaryWeapon.M7||value==PrimaryWeapon.M14)SliceWeapon=value;}
    public void BeginSliceBoss()
    {
        if(CombatLoopV2.Enabled && gm!=null)director?.BeginBoss();
    }
    void OnDestroy()
    {
        director?.Dispose();
        if(ownsLab){CombatLabTelemetry.End("closed",Elapsed);CombatLabSettings.Exit();}
    }
    void OnApplicationQuit(){if(ownsLab)CombatLabTelemetry.End("quit",Elapsed);}

    public void StartLab(CombatLabVariant variant)
    {
        if(!CombatLoopV2.Enabled)return;
        CombatLabTelemetry.End("variant_changed",Elapsed);
        CombatLabSettings.Select(variant);ownsLab=true;
        CombatSliceSettings.SelectView(2);
        Restart();
    }
    public void LeaveLab()
    {
        CombatLabTelemetry.End("exit",Elapsed);
        CombatLabSettings.Exit();ownsLab=false;Restart();
    }

    public int SliceSpawned => !ownsLab && director!=null ? director.Flow.Spawned : 0;
    public bool SliceWon { get; private set; }
    public float RunDuration => CombatLoopV2.Enabled ? CombatLoopV2.EncounterLimit : CombatSliceSettings.Enabled ? CombatSliceSettings.TimeLimit : Duration;
    GameManager gm;float spawnTimer;int groups;GUIStyle label;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        if(Enabled && Array.IndexOf(Environment.GetCommandLineArgs(),"-p0Check")<0)
            new GameObject("P0 Combat Demo").AddComponent<P0CombatDemo>();
    }
    IEnumerator Start()
    {
        while(GameManager.Instance==null || GameManager.Instance.playerController.GetComponentInChildren<LoadoutVisual>()==null)yield return null;
        yield return null;gm=GameManager.Instance;
        if(Array.IndexOf(Environment.GetCommandLineArgs(),"-p0Silent")>=0)AudioListener.volume=0;
        Training=Array.IndexOf(Environment.GetCommandLineArgs(),"-p0Training")>=0;
        Restart();
    }
    public void Restart()
    {
        if(gm==null)gm=GameManager.Instance;
        if(ownsLab)CombatLabTelemetry.End("restart",Elapsed);
        director?.Dispose();director=null;
        gm.stageManager.StopStage();gm.EnterHangar();gm.playerController.Loadout.Select(CombatLoopV2.Enabled?SliceWeapon:PrimaryWeapon.M7);
        foreach(var shot in FindObjectsByType<Projectile>(FindObjectsSortMode.None))shot.Despawn();
        CombatEffects.ClearTelegraphs();
        gm.BeginP0Combat();Elapsed=0;Finished=false;spawnTimer=3;groups=0;
        SliceWon=false;
        if(CombatSliceSettings.Enabled)director=new CombatEncounterDirector(gm,ownsLab);
        OverdriveVfx.Clear();
        gm.resultUI?.Hide();
        if(ownsLab&&CombatLabSettings.Active)CombatLabTelemetry.Begin(gm.playerController);
    }
    void Update()
    {
        if(gm==null)return;
        if(Input.GetKeyDown(KeyCode.Backspace)){gm.ExitPractice();return;}
        if(CombatSliceSettings.Enabled)
        {
            if(!gm.IsPaused&&CombatLoopV2.Enabled&&Input.GetKeyDown(KeyCode.F9))
            {if(ownsLab)LeaveLab();else StartLab(CombatLabVariant.FullFeedback);return;}
            if(ownsLab&&!gm.IsPaused)
            {
                if(Input.GetKeyDown(KeyCode.Alpha1)){StartLab(CombatLabVariant.BasicFeedback);return;}
                if(Input.GetKeyDown(KeyCode.Alpha2)){StartLab(CombatLabVariant.FullFeedback);return;}
                if(Input.GetKeyDown(KeyCode.Alpha3)){StartLab(CombatLabVariant.NoCameraShake);return;}
            }
            if(!gm.IsPaused&&CombatLoopV2.Enabled&&Input.GetKeyDown(KeyCode.F3)){SetSliceWeapon(SliceWeapon==PrimaryWeapon.M7?PrimaryWeapon.M14:PrimaryWeapon.M7);Restart();return;}
            if(!gm.IsPaused&&CombatLoopV2.Enabled&&Input.GetKeyDown(KeyCode.F8)){if(ownsLab)LeaveLab();else Restart();BeginSliceBoss();return;}
            if(Input.GetKeyDown(KeyCode.R)){Restart();return;}
            if(gm.Phase==GamePhase.Result)gm.resultUI?.Hide();
            if(ownsLab){TickLab();return;}
            if(!Finished && (gm.playerStats.CurrentHp<=0 || gm.Phase==GamePhase.Result))
            {director?.Flow.Fail();Finished=true;}
            if(!gm.IsCombatActive || Finished)return;
            TickSlice();return;
        }
        if(Input.GetKeyDown(KeyCode.F2))OverdriveVfx.ToggleIntensity();
        if(Input.GetKeyDown(KeyCode.F1)){Training=!Training;Restart();}
        if(Input.GetKeyDown(KeyCode.R)){Restart();return;}
        if(gm.Phase==GamePhase.Result)gm.resultUI?.Hide();
        if(!gm.IsCombatActive || Finished)return;
        Elapsed+=Time.deltaTime;
        if(!Training && Elapsed>=Duration)
        {Finished=true;gm.EnterResult(true);gm.resultUI?.Hide();return;}
        if(Training)return;
        spawnTimer-=Time.deltaTime;
        if(spawnTimer<=0 && gm.stageManager.EnemiesAlive<7)
        {
            var kind=Elapsed>35 && groups%4==3 ? EnemyKind.Elite : groups%3==2?EnemyKind.Ranged:EnemyKind.Melee;
            gm.stageManager.enemySpawner.SpawnEntry(kind,Mathf.Min(kind==EnemyKind.Elite?1:2,7-gm.stageManager.EnemiesAlive),groups++%4);
            spawnTimer=Elapsed<30?6:Elapsed<150?4.5f:3.5f;
        }
    }
    void TickLab()
    {
        if(Finished)return;
        CombatLabTelemetry.Frame(gm.IsPaused);
        if(gm.playerStats.CurrentHp<=0 || gm.Phase==GamePhase.Result)
        {director?.Flow.Fail();Finished=true;CombatLabTelemetry.End("down",Elapsed);return;}
        if(!gm.IsCombatActive)return;
        director.Tick(Time.deltaTime);Elapsed=director.Flow.Elapsed;
        if(director.Flow.Finished)
        {
            Finished=true;CombatLabTelemetry.End("time_limit",Elapsed);
            gm.EnterResult(false);gm.resultUI?.Hide();return;
        }
    }
    void OnGUI()
    {
        if(gm==null)return;
        if(CombatSliceSettings.Enabled){if(ownsLab)DrawLabHUD();else DrawSliceHUD();return;}
        if(label==null)label=new GUIStyle(GUI.skin.label){fontSize=18,normal={textColor=Color.white}};
        GUI.Box(new Rect(12,12,Mathf.Min(Screen.width-24,1100),98),GUIContent.none);
        var remaining=Mathf.CeilToInt(Mathf.Max(0,Duration-Elapsed));
        string title=Finished?"P0.10 COMPLETE · R to retry":gm.playerStats.CurrentHp<=0?"DOWN · R to retry":Training?"P0.10 M7 PRACTICE":$"P0.10 M7 · {remaining/60:00}:{remaining%60:00}";
        GUI.Label(new Rect(24,18,800,28),$"{title}   HP {Mathf.CeilToInt(gm.playerStats.CurrentHp)}   EN {Mathf.CeilToInt(gm.playerStats.CurrentEnergy)}   DASH {gm.playerController.DashCooldownRemaining:F1}s   KILLS {gm.Kills}",label);
        GUI.Label(new Rect(24,46,800,28),"WASD move · LMB rifle · Q/RMB combo · Space dash · F1 practice · R restart",label);
        GUI.Label(new Rect(24,74,1080,28),$"Rifle + blade · armor absorbs damage until depleted · F2 FX: {(OverdriveVfx.Intense?"OVERDRIVE":"STANDARD")}",label);
    }

    void DrawLabHUD()
    {
        if(label==null)label=new GUIStyle(GUI.skin.label){fontSize=18,normal={textColor=Color.white}};
        float scale=Mathf.Clamp(Screen.width/1600f,.65f,1.5f);
        var old=GUI.matrix;GUI.matrix=Matrix4x4.TRS(Vector3.zero,Quaternion.identity,Vector3.one*scale);
        GUI.Box(new Rect(14,12,760,118),GUIContent.none);
        string title=Finished?"本轮结束 · R 再来一次":"30 秒短战斗";
        GUI.Label(new Rect(26,18,735,26),$"{title}    方案 {CombatLabSettings.DisplayNumber}    {SliceWeapon}",label);
        GUI.Label(new Rect(26,46,735,26),$"耐久 {gm.playerStats.CurrentHp:0}   推进 {gm.playerStats.CurrentEnergy:0}   击破 {gm.Kills}   剩余 {Mathf.CeilToInt(CombatLabSettings.Duration-Elapsed)}s",label);
        GUI.Label(new Rect(26,74,735,26),"1 / 2 / 3 换方案重开 · F3 换枪重开 · R 重试 · F9 返回试场",label);
        string support=gm.playerController.weaponController.SkillCooldownRemaining<=0?"E 导弹就绪":"E 冷却 "+gm.playerController.weaponController.SkillCooldownRemaining.ToString("F1")+"s";
        GUI.Label(new Rect(26,100,735,26),support+(Finished?(CombatLabTelemetry.LastError==null?"    本轮记录已保存":"    记录保存失败，仍可重试"):"    基础装备 · 零强化"),label);
        GUI.Label(new Rect(22,Screen.height/scale-34,1300,28),"WASD 移动 · 左键射击 · 右键 / Q 斩击 · 空格冲刺 · E 导弹 · Esc 暂停",label);
        GUI.matrix=old;
    }

    void TickSlice()
    {
        if(gm.stageManager.EnemiesAlive==0 && gm.equipmentLoop.Absorption.Offering && Input.GetKeyDown(KeyCode.Return))gm.equipmentLoop.Absorption.CollectWithoutInstalling();
        director.Tick(Time.deltaTime);Elapsed=director.Flow.Elapsed;
        if(!director.Flow.Finished)return;
        Finished=true;SliceWon=director.Flow.Won;
        gm.EnterResult(SliceWon);gm.resultUI?.Hide();
    }

    void DrawSliceHUD()
    {
        if(label==null)label=new GUIStyle(GUI.skin.label){fontSize=18,normal={textColor=Color.white}};
        float scale=Mathf.Clamp(Screen.width/1600f,.65f,1.5f);
        var old=GUI.matrix;GUI.matrix=Matrix4x4.TRS(Vector3.zero,Quaternion.identity,Vector3.one*scale);
        bool ended=Finished||gm.Phase==GamePhase.Result;
        string title=ended?(SliceWon?"战斗完成 · R 再来一局":gm.playerStats.CurrentHp<=0?"机体损毁 · R 重试":"试战结束 · R 重试"):"机神试炼 · 基础战斗试场";
        GUI.Box(new Rect(14,12,670,88),GUIContent.none);
        GUI.Label(new Rect(26,18,650,26),title+"    "+CombatSliceSettings.ViewLabel,label);
        GUI.Label(new Rect(26,44,650,26),$"耐久 {gm.playerStats.CurrentHp:0}   推进 {gm.playerStats.CurrentEnergy:0}   击破 {gm.Kills}   敌群 {SliceGroup}/{SliceGroupCount}   {Elapsed:0}s",label);
        GUI.Label(new Rect(26,70,650,26),"基础装备 · 零强化 · R 重开 · F4 脱困 · Esc 暂停",label);
        if(GUI.Button(new Rect(Screen.width/scale-172,16,156,38),"返回出击准备"))gm.ExitPractice();
        GUI.Label(new Rect(22,Screen.height/scale-34,1000,28),"WASD 移动 · 左键射击 · 右键 / Q 斩击 · 空格闪击 / 按住推进",label);
        if(CombatLoopV2.Enabled)
        {
            string support=gm.playerController.weaponController.SkillCooldownRemaining<=0?"E 导弹就绪":"E 冷却 "+gm.playerController.weaponController.SkillCooldownRemaining.ToString("F1")+"s";
            GUI.Label(new Rect(26,105,650,28),support+"    枪刀均可击破 · 破甲后可打断重装",label);
            if(SliceBoss==null)GUI.Label(new Rect(26,136,650,28),gm.equipmentLoop.Absorption.Offering?"F 夺取并试装 · Enter 只收藏并继续":"F8 首领试战 · F9 30 秒对照场",label);
            if(SliceBoss!=null&&!SliceBoss.GetComponent<Damageable>().IsDead)
            {
                var health=SliceBoss.GetComponent<Damageable>();
                GUI.Box(new Rect(14,138,670,72),GUIContent.none);
                GUI.Label(new Rect(26,141,650,28),"首领耐久  "+health.CurrentHealth.ToString("0")+" / "+health.maxHealth.ToString("0"),label);
                GUI.Label(new Rect(26,170,650,26),SliceBoss.CoreExposed?"露核 · 枪刀均可全额输出":"观察前摇 · 躲开攻击 · 后摇反击",label);
            }
        }
        GUI.matrix=old;
    }
}
