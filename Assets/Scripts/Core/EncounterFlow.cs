using System;
using System.Collections.Generic;

public enum EncounterPhase { Ready, Combat, Recovery, RewardHold, BossReady, Boss, BossDefeat, Victory, Defeat, RewardChoice, RoomComplete }
public enum EncounterStep { None, SpawnGroup, SpawnBoss, Victory, Defeat, Clear }

// Pure run state. No scene objects, coroutines, RNG globals, UI or permanent save writes.
public sealed class EncounterFlow
{
    readonly CombatRules rules;
    readonly bool lab;readonly bool roomBoss;bool bossStarted;
    readonly IReadOnlyList<CombatRules.Beat> beats;
    public bool RoomMode {get;}
    public bool BossOnly {get;}
    float quiet, nextGroupAt=.2f, refillAt=float.PositiveInfinity, defeatAge, defeatWait;
    int previousOccupied;
    public EncounterPhase Phase { get; private set; } = EncounterPhase.Ready;
    public float Elapsed { get; private set; }
    public int Groups { get; private set; }
    public int Spawned { get; private set; }
    public bool Finished => Phase==EncounterPhase.Victory || Phase==EncounterPhase.Defeat || Phase==EncounterPhase.RoomComplete;
    public bool Won => Phase==EncounterPhase.Victory;
    public event Action<EncounterPhase,EncounterPhase> Changed;
    public EncounterFlow(CombatRules rules, bool lab,IReadOnlyList<CombatRules.Beat> beats=null,bool bossOnly=false,bool roomBoss=false)
    { this.rules=rules; this.lab=lab;RoomMode=beats!=null;this.beats=beats??rules.Encounters;BossOnly=bossOnly;this.roomBoss=roomBoss; }
    public bool ContinueRoom(){if(!RoomMode||Phase!=EncounterPhase.RewardHold)return false;SetPhase(EncounterPhase.RewardChoice);return true;}
    public bool CompleteReward(){if(Phase!=EncounterPhase.RewardChoice)return false;SetPhase(EncounterPhase.RoomComplete);return true;}
    void SetPhase(EncounterPhase phase)
    { if(phase==Phase)return; var previous=Phase;Phase=phase;Changed?.Invoke(previous,phase); }
    public EncounterStep Tick(float dt, int occupied, bool rewardPending)
    {
        if(Finished || dt<=0)return EncounterStep.None;
        float limit=lab?rules.LabLimit:float.PositiveInfinity;
        Elapsed=Math.Min(limit,Elapsed+dt);
        // A kill releases a reserved/live slot immediately; presentation of the
        // wreck must never delay reinforcements. Capacity is still checked below.
        if(occupied<previousOccupied&&Groups>0)refillAt=Elapsed+.18f;
        previousOccupied=occupied;
        // A defeated boss owns its exit animation; the time cap cannot revoke an earned win.
        if(Phase==EncounterPhase.BossDefeat)
        {
            defeatAge+=dt;
            if(defeatAge>=defeatWait){if(RoomMode&&roomBoss){SetPhase(EncounterPhase.RewardHold);return EncounterStep.Clear;}SetPhase(EncounterPhase.Victory);return EncounterStep.Victory;}
            return EncounterStep.None;
        }
        if(Elapsed>=limit){Fail();return EncounterStep.Defeat;}
        if(RoomMode && (Phase==EncounterPhase.RewardHold||Phase==EncounterPhase.RewardChoice))return EncounterStep.None;
        if(Phase==EncounterPhase.BossReady)return EncounterStep.SpawnBoss;
        if(Phase==EncounterPhase.Boss)return EncounterStep.None;
        if(!RoomMode && !roomBoss && occupied==0 && rewardPending){SetPhase(EncounterPhase.RewardHold);return EncounterStep.None;}
        if(!lab && Groups>=beats.Count && occupied==0)
        {
            if(roomBoss&&!bossStarted){SetPhase(EncounterPhase.BossReady);return EncounterStep.SpawnBoss;}
            if(RoomMode){SetPhase(EncounterPhase.RewardHold);return EncounterStep.Clear;}
            SetPhase(EncounterPhase.Victory);return EncounterStep.Victory;
        }
        quiet=occupied==0?quiet+dt:0;
        SetPhase(occupied==0?EncounterPhase.Recovery:EncounterPhase.Combat);
        if(!lab && Groups>=beats.Count)return EncounterStep.None;
        var beat=lab?rules.Lab:beats[Groups];
        if(occupied+beat.Roles.Count>rules.MaxHostiles)return EncounterStep.None;
        if(lab ? occupied>0 || quiet<rules.ClearDelay : Elapsed<nextGroupAt && quiet<rules.ClearDelay && Elapsed<refillAt)
            return EncounterStep.None;
        Groups++; Spawned+=beat.Roles.Count;quiet=0;nextGroupAt=Elapsed+rules.GroupInterval;refillAt=float.PositiveInfinity;
        SetPhase(EncounterPhase.Combat);
        return EncounterStep.SpawnGroup;
    }
    public bool BeginBoss()
    {
        if(lab || Finished || Phase==EncounterPhase.Boss || Phase==EncounterPhase.BossDefeat)return false;
        bossStarted=true;Groups=beats.Count;SetPhase(EncounterPhase.Boss);return true;
    }
    public void BossDefeated(float delay)
    {if(Phase!=EncounterPhase.Boss)return;defeatWait=Math.Max(1,delay);defeatAge=0;SetPhase(EncounterPhase.BossDefeat);}
    public void Fail() { if(!Finished && Phase!=EncounterPhase.BossDefeat)SetPhase(EncounterPhase.Defeat); }
}
