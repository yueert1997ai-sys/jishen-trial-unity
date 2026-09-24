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
    GameManager gm;float spawnTimer;int groups;
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
    public void Restart(bool retry=false)
    {
        if(retry)CombatRuntime.RetryCurrentSeed();
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
            if(!gm.IsPaused && Input.GetKeyDown(KeyCode.R)){Restart(true);return;}
            
            if(ownsLab){TickLab();return;}
            if(!Finished && (gm.playerStats.CurrentHp<=0 || gm.Phase==GamePhase.Result))
            {director?.Flow.Fail();Finished=true;}
            if(!gm.IsCombatActive || Finished)return;
            TickSlice();return;
        }
        if(Input.GetKeyDown(KeyCode.F2))OverdriveVfx.ToggleIntensity();
        if(Input.GetKeyDown(KeyCode.F1)){Training=!Training;Restart();}
        if(!gm.IsPaused && Input.GetKeyDown(KeyCode.R)){Restart(true);return;}
        
        if(!gm.IsCombatActive || Finished)return;
        Elapsed+=Time.deltaTime;
        if(!Training && Elapsed>=Duration)
        {Finished=true;gm.EnterResult(true);return;}
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
            gm.EnterResult(false);return;
        }
    }
    void TickSlice()
    {
        if(gm.stageManager.EnemiesAlive==0 && gm.equipmentLoop.Absorption.Offering && Input.GetKeyDown(KeyCode.Return))gm.equipmentLoop.Absorption.CollectWithoutInstalling();
        director.Tick(Time.deltaTime);Elapsed=director.Flow.Elapsed;
        if(!director.Flow.Finished)return;
        Finished=true;SliceWon=director.Flow.Won;
        gm.EnterResult(SliceWon);
    }

}
