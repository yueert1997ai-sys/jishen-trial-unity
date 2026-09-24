using UnityEngine;

// Scene ownership and occupancy service. EncounterFlow alone decides spawning/clear/reward/Boss states.
public class StageManager : MonoBehaviour
{
    public EnemySpawner enemySpawner;
    public Transform player;
    public float waveRepairFraction=.12f;
    public EncounterDefinition maintenanceEncounter;
    public EncounterDefinition[] encounters=new EncounterDefinition[6]; // Serialized V3 references remain loadable.
    public float FirstEncounterSeconds {get;private set;}
    public int CurrentEncounter {get;private set;}
    public int CurrentStage=>Mathf.Min(2,CurrentEncounter/3+1);
    public int CurrentWave=>CurrentEncounter%3+1;
    public int EnemiesAlive=>enemiesAlive;
    public int WaveRepairsGranted {get;private set;}
    public float[] EncounterSeconds {get;}=new float[6];
    public CombatEncounterDirector Director {get;private set;}
    public EncounterFlow Flow=>Director?.Flow;
    int enemiesAlive;
    public bool BossAlive {get;private set;}
    public void StartStage(int stageIndex){StartEncounter((stageIndex-1)*3);}
    public void StartEncounter(int index,GameObject bossOverride=null)
    {
        StopStage();CurrentEncounter=Mathf.Clamp(index,0,6);
        GameManager.Instance.equipmentLoop.Absorption.BeginEncounter();
        if(index==0){WaveRepairsGranted=0;System.Array.Clear(EncounterSeconds,0,6);}
        Director=new CombatEncounterDirector(GameManager.Instance,false,CurrentEncounter,bossOverride);
        GameManager.Instance.SetProgress(index<6?EncounterCatalog.Rooms[index].Title+"  /  "+(index+1)+" / 6":"液态核心 / 首领战");
        GameAudio.Play(index<6?GameAudioCue.Wave:GameAudioCue.Warning,.34f);
    }
    void Update()
    {
        if(Director==null)return;
        var step=Director.Tick(Time.deltaTime);
        if(step==EncounterStep.Clear)
        {
            EncounterSeconds[CurrentEncounter]=Flow.Elapsed;
            if(CurrentEncounter==0)FirstEncounterSeconds=Flow.Elapsed;
            ApplyWaveRepair();GameManager.Instance.OnEncounterCleared(CurrentEncounter);
        }
        else if(step==EncounterStep.Victory)GameManager.Instance.OnStageCleared(2);
        else if(step==EncounterStep.Defeat)GameManager.Instance.EnterResult(false);
    }
    public void NotifyEnemySpawned(){enemiesAlive++;}
    public void NotifyEnemyKilled(){enemiesAlive=Mathf.Max(0,enemiesAlive-1);}
    public void NotifyBossSpawned(){BossAlive=true;}
    public void NotifyBossKilled(){BossAlive=false;}
    public void StopStage()
    {
        Director?.Dispose();Director=null;StopAllCoroutines();
        if(enemySpawner!=null)enemySpawner.CancelPendingSpawns();
        foreach(var actor in FindObjectsByType<Damageable>(FindObjectsSortMode.None))
            if(actor!=null&&actor.team==1){actor.gameObject.SetActive(false);Destroy(actor.gameObject);}
        enemiesAlive=0;BossAlive=false;
    }
    void OnDisable(){StopStage();}
    void ApplyWaveRepair()
    {
        var stats=player!=null?player.GetComponent<PlayerStats>():null;
        if(stats==null||stats.CurrentHp<=0||stats.CurrentHp>=stats.MaxHp)return;
        float before=stats.CurrentHp;
        stats.Heal(stats.MaxHp*Mathf.Clamp01(waveRepairFraction*GameManager.Instance.WaveRepairMultiplier));
        if(stats.CurrentHp>before)WaveRepairsGranted++;
    }
}
