using System;
using UnityEngine;

// Scene adapter. P0CombatDemo owns hotkeys/HUD; EncounterFlow owns progression.
public sealed class CombatEncounterDirector : IDisposable
{
    readonly GameManager gm;
    readonly CombatRules rules;
    readonly bool lab;
    bool disposed;
    readonly int roomIndex,runGeneration;
    readonly GameObject bossOverride;
    Damageable bossHealth;readonly EncounterRoster roster;
    public EncounterFlow Flow { get; }
    public BossController Boss { get; private set; }
    public CombatEncounterDirector(GameManager gm, bool lab,int roomIndex=-1,GameObject bossOverride=null)
    {
        this.gm=gm;this.lab=lab;this.roomIndex=roomIndex;this.bossOverride=bossOverride;
        runGeneration=CombatRuntime.Run.Generation;rules=CombatRules.Current;
        var beats=roomIndex>=0&&roomIndex<6?EncounterCatalog.Rooms[roomIndex].Beats:rules.Encounters;
        if(!lab&&roomIndex!=6)roster=new EncounterRoster(CombatRuntime.Run.Seed,roomIndex,beats);
        Flow=new EncounterFlow(rules,lab,roomIndex>=0&&roomIndex<6?beats:null,roomIndex==6,!lab&&roomIndex<3);
        if(roomIndex==6)BeginBoss();
    }
    public EncounterStep Tick(float dt)
    {
        if(disposed || !gm.IsCombatActive || !CombatRuntime.Owns(runGeneration))return EncounterStep.None;
        var absorption=gm.equipmentLoop.Absorption;
        bool waiting=!lab && absorption!=null && (absorption.Busy||absorption.Offering);
        var step=Flow.Tick(dt,gm.stageManager.EnemiesAlive,waiting);
        if(step==EncounterStep.SpawnGroup) Spawn(lab?rules.Lab:roomIndex>=0&&roomIndex<6?EncounterCatalog.Rooms[roomIndex].Beats[Flow.Groups-1]:rules.Encounters[Flow.Groups-1]);
        else if(step==EncounterStep.SpawnBoss) BeginBoss();
        return step;
    }
    void Spawn(CombatRules.Beat beat)
    {
        var spawner=gm.stageManager.enemySpawner;
        var player=gm.playerController.transform.position;
        if(lab)
        {
            float side=1;
            foreach(var point in beat.Positions) if(Vector3.Distance(point,player)<4){side=-1;break;}
            for(int i=0;i<beat.Roles.Count;i++) spawner.SpawnSliceEntry(beat.Roles[i],beat.Positions[i]*side,i*rules.SpawnStagger);
            return;
        }
        Vector3 center=roster!=null?roster.Entry(Flow.Groups-1):beat.Center;
        if(gm.arenaSector!=null&&gm.arenaSector.ActiveLunarLayout!=null)
            center=gm.arenaSector.ActiveLunarLayout.SelectEntry(center,player);
        if(Vector3.Distance(center,player)<6)center=-center;
        Vector3 tangent=Vector3.Cross(center.normalized,Vector3.up);
        int front=0, rear=0;
        for(int i=0;i<beat.Roles.Count;i++)
        {
            bool ranged=beat.Roles[i]==EnemyKind.Ranged||beat.Roles[i]==EnemyKind.Elite;
            int lane=ranged?rear++:front++;
            float side=lane%2==0?-1:1;
            // Melee advances from the entry; rifles take its two wings. The
            // same five-hostile occupancy and warning budget still applies.
            Vector3 point=center+tangent*side*(ranged?3.4f:1.1f)
                -center.normalized*(ranged?0:1.1f);
            if(roster!=null)spawner.SpawnArsenalEntry(roster.Group(Flow.Groups-1)[i],point,i*rules.SpawnStagger);
            else spawner.SpawnSliceEntry(beat.Roles[i],point,i*rules.SpawnStagger);
        }
    }
    public void BeginBoss()
    {
        if(disposed)return;
        if(gm.equipmentLoop.Absorption.Busy)return;
        if(roomIndex<0&&!gm.equipmentLoop.Absorption.CollectWithoutInstalling())return;
        if(!Flow.BeginBoss())return;
        if(roomIndex<0)gm.stageManager.StopStage();
        foreach(var shot in UnityEngine.Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None))shot.Despawn();
        if(roomIndex<0)gm.equipmentLoop.ClearPickups();
        Boss=roomIndex<3&&bossOverride==null?gm.stageManager.enemySpawner.SpawnFazz(Mathf.Max(0,roomIndex)):gm.stageManager.enemySpawner.SpawnBoss(bossOverride);
        if(Boss==null){Flow.Fail();Debug.LogError("Combat encounter Boss is missing");return;}
        bossHealth=Boss.GetComponent<Damageable>();bossHealth.OnDied+=OnBossDied;
    }
    void OnBossDied(Damageable health)
    {if(!disposed)Flow.BossDefeated(Boss!=null?Boss.defeatDelay+.1f:1);}
    public void Dispose()
    {if(disposed)return;disposed=true;if(bossHealth!=null)bossHealth.OnDied-=OnBossDied;}
}
