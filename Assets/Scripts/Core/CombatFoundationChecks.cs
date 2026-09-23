using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

// Contract tests for the production foundation, plus live motor/spawner/health integration.
// Fixtures deliberately isolate mechanisms. Full natural combat runs use the existing play/replay suites.
public static class CombatFoundationChecks
{
    static void Drive(PlayerController p, bool dash=false, Vector2 move=default)
    {p.Simulate(new PlayerCommand{Dash=dash,Move=move,HasAim=true,AimPoint=new Vector3(0,1,20)},Time.deltaTime);}
    public static IEnumerator Run(GameManager gm, PlayerController p, string output, Action<bool,string> check, Action<string> capture)
    {
        var rules=CombatRules.Current;
        var facts=new List<string>();
        var rulesText=Resources.Load<TextAsset>("Foundation/CombatRules").text;
        int total=0;foreach(var beat in rules.Encounters)total+=beat.Roles.Count;
        check(rules.Encounters.Count==9 && total==34 && rules.MaxHostiles==5,"shorter trial has nine groups, 34 enemies and at most five live plus reserved hostiles");
        check(Mathf.Abs(rules.MeleeRecovery-.18f)<.001f && Mathf.Abs(rules.RangedRecovery-.22f)<.001f,"enemy recovery is explicit authored timing");
        bool invalid=false;
        try{CombatRules.Parse(rulesText.Replace("\"version\": 4","\"version\": 99"));}catch(InvalidOperationException){invalid=true;}
        check(invalid,"unknown definition versions fail explicitly");
        invalid=false;
        try{CombatRules.Parse(rulesText.Replace("\"maxHostiles\": 5","\"maxHostiles\": 1"));}catch(InvalidOperationException){invalid=true;}
        check(invalid,"a batch larger than the occupancy budget is rejected");

        foreach(int fps in new[]{30,60,120})
        {
            float dt=1f/fps;
            var flow=new EncounterFlow(rules,false);
            for(int i=0;i<fps*2;i++)flow.Tick(dt,rules.MaxHostiles,false);
            check(flow.Groups==0,"reserved hostile slots prevent oversubscription at "+fps);
            for(int i=0;i<fps*3;i++)flow.Tick(dt,0,true);
            check(flow.Groups==0 && flow.Phase==EncounterPhase.RewardHold,"absorption blocks encounter advancement at "+fps);
            for(int i=0;i<fps*25 && !flow.Finished;i++)flow.Tick(dt,0,false);
            check(flow.Groups==9 && flow.Spawned==34 && flow.Phase==EncounterPhase.Victory,"short encounter finishes without forcing a Boss at "+fps);
            flow=new EncounterFlow(rules,false,null,true);
            check(flow.BeginBoss() && !flow.BeginBoss(),"Boss entry commits exactly once at "+fps);
            flow.Tick(rules.EncounterLimit-flow.Elapsed-.1f,0,false);
            flow.BossDefeated(1.5f);
            for(int i=0;i<fps*2;i++)flow.Tick(dt,0,false);
            check(flow.Won && flow.Tick(dt,0,false)==EncounterStep.None,"earned Boss victory survives time cap and emits once at "+fps);
            var timedOut=new EncounterFlow(rules,false);
            timedOut.Tick(rules.EncounterLimit,7,false);
            check(!timedOut.Finished&&!timedOut.Won,"combat does not time out or award victory with enemies alive at "+fps);
            var fresh=new EncounterFlow(rules,false);
            check(fresh.Groups==0&&fresh.Spawned==0&&fresh.Elapsed==0&&!fresh.Finished,"new run has no prior terminal state at "+fps);
        }

        var cycle=new EnemyAttackCycle();
        int token=cycle.Begin(10,.5f,Vector3.right);
        check(!cycle.TryCommit(token,10.49f,true),"enemy cannot emit before telegraph completes");
        cycle.Cancel();
        check(!cycle.TryCommit(token,11,true),"canceled attack generation cannot emit late shots");
        token=cycle.Begin(11,.5f,Vector3.left);
        check(cycle.TryCommit(token,11.5f,true) && !cycle.TryCommit(token,11.5f,true),"enemy emission commits once");
        check(cycle.Direction==Vector3.left,"attack retains its advertised direction");
        cycle.Complete(11.5f,.7f);cycle.Tick(11.9f);
        check(cycle.Phase==EnemyAttackPhase.Recovery,"enemy recovery is a real action phase");
        cycle.Tick(12.21f);check(cycle.Phase==EnemyAttackPhase.Ready,"recovery returns to ready");

        var fixture=new GameObject("FoundationDamageFixture");
        var hp=fixture.AddComponent<Damageable>();hp.destroyOnDeath=false;hp.RestoreLife(100,100);
        int resolved=0, damaged=0, deaths=0;DamageResult nested=default;bool lethalWasCommitted=false;
        hp.OnResolved+=(target,result)=>{resolved++;lethalWasCommitted|=result.Lethal&&target.IsDead&&target.CurrentHealth==0;};
        hp.OnDamaged+=(target,info)=>
        {
            damaged++;nested=target.ApplyDamage(1,new DamageInfo(null,Vector3.zero,null,1));
            if(target.IsDead)target.Kill(info);
        };
        hp.OnDied+=target=>deaths++;
        var request=new DamageInfo(p.gameObject,p.transform.position,p.GetComponent<Damageable>(),20){Kind=CombatHitKind.Rifle};
        var first=hp.ApplyDamage(20,request);
        check(first.Applied&&first.AppliedDamage==20&&hp.CurrentHealth==80&&nested.Rejection==DamageRejection.Reentrant,"HP commits before callbacks and rejects recursive same-target damage");
        request.Amount=999;request.Kind=CombatHitKind.Missile;
        check(first.ToDamageInfo().Amount==20&&hp.LastHit.Amount==20&&hp.LastHit.Kind==CombatHitKind.Rifle,"caller mutation cannot rewrite committed hit history");
        var read=first.ToDamageInfo();read.Amount=-1;
        check(first.ToDamageInfo().Amount==20,"each presentation listener receives its own snapshot");
        var lethal=hp.ApplyDamage(250,request);
        check(lethal.AppliedDamage==80&&lethal.ResolvedDamage==250&&lethal.Overkill==170,"result separates resolved damage, actual HP loss and overkill");
        check(lethalWasCommitted&&deaths==1&&resolved==2&&damaged==2,"lethal state commits before listeners and death publishes once");
        hp.ApplyDamage(1,request);hp.Kill(request);
        check(deaths==1&&resolved==2,"dead targets cannot emit another hit or kill");
        hp.RestoreLife(100,100);hp.SetInvulnerable(.1f);
        var rejected=hp.ApplyDamage(10,request);
        check(rejected.Rejection==DamageRejection.Invulnerable&&!rejected.Applied&&request.Amount==0&&hp.LastHit==null,"rejected hit clears stale result flags without changing health");
        hp.RestoreLife(100,100);
        check(hp.ApplyDamage(float.NaN,request).Rejection==DamageRejection.InvalidAmount&&hp.CurrentHealth==100,"invalid damage cannot corrupt health");
        UnityEngine.Object.Destroy(fixture);yield return null;

        var spawner=gm.stageManager.enemySpawner;
        var source=p.GetComponent<Damageable>();
        try
        {
            foreach(int fps in new[]{30,60,120})
            {
                Time.captureDeltaTime=1f/fps;gm.stageManager.StopStage();p.RestoreAt(new Vector3(0,.1f,0));
                p.stats.ResetStats();source.RestoreLife(p.stats.MaxHp,p.stats.MaxHp);source.SetInvulnerable(20);
                yield return null;
                int kills=gm.Kills;
                var live=spawner.SpawnEnemy(EnemyKind.Melee,new Vector3(10,0,10));
                live.TrainingTarget=true;
                for(int i=0;i<3;i++)spawner.SpawnSliceEntry(EnemyKind.Ranged,new Vector3(-10+i*2,0,9),i*.12f);
                check(spawner.PendingSpawns==3&&gm.stageManager.EnemiesAlive==4,"pending enemies reserve capacity immediately at "+fps);
                int generation=spawner.SpawnGeneration;
                gm.SetPaused(true);for(int i=0;i<5;i++)yield return null;
                check(spawner.PendingSpawns==3,"pause cannot complete a spawn reservation at "+fps);
                spawner.CancelPendingSpawns();gm.SetPaused(false);
                check(spawner.SpawnGeneration!=generation&&spawner.PendingSpawns==0&&gm.stageManager.EnemiesAlive==1&&gm.Kills==kills,
                    "cancel returns pending slots, preserves live enemy and grants no kills at "+fps);
                spawner.SpawnSliceEntry(EnemyKind.Ranged,new Vector3(-8,0,9),0);
                for(int i=0;i<fps;i++)yield return null;
                check(spawner.PendingSpawns==0&&gm.stageManager.EnemiesAlive==2
                    && UnityEngine.Object.FindObjectsByType<EnemyBase>(FindObjectsSortMode.None).Length==2,
                    "only fresh generation spawns after cancellation at "+fps);
                gm.stageManager.StopStage();yield return null;

                // First dash establishes cooldown; second press occurs before ready, then movement is released.
                p.RestoreAt(new Vector3(0,.1f,0));p.stats.ResetStats();
                var directions=new List<Vector3>();Action<Vector3> onDash=d=>directions.Add(d);p.Dashed+=onDash;
                try
                {
                    Drive(p,true,Vector2.up);yield return null;
                    while(p.DashCooldownRemaining>.12f){Drive(p);yield return null;}
                    Drive(p,true,Vector2.right);yield return null;
                    for(int i=0;i<fps/2;i++){Drive(p);yield return null;}
                    check(directions.Count==2&&Vector3.Dot(directions[1],Vector3.right)>.99f,"buffered dash preserves pressed direction after release at "+fps);
                    facts.Add("fps="+fps+" bufferedDashDirection="+directions[1]);
                    p.RestoreAt(new Vector3(0,.1f,0));p.stats.ResetStats();directions.Clear();
                    Drive(p,true,Vector2.up);yield return null;
                    Drive(p,true,Vector2.right);yield return null;
                    for(int i=0;i<fps;i++){Drive(p);yield return null;}
                    check(directions.Count==1,"expired early dash does not fire when cooldown later ends at "+fps);
                    p.RestoreAt(new Vector3(0,.1f,0));p.stats.ResetStats();directions.Clear();
                    Drive(p,true,Vector2.up);yield return null;
                    while(p.DashCooldownRemaining>.12f){Drive(p);yield return null;}
                    Drive(p,true,Vector2.right);yield return null;
                    gm.SetPaused(true);yield return null;gm.SetPaused(false);
                    for(int i=0;i<fps/2;i++){Drive(p);yield return null;}
                    check(directions.Count==1,"pause flushes pending input instead of firing a ghost action at "+fps);
                }
                finally{p.Dashed-=onDash;}
            }
            capture("foundation_scene.png");
            File.WriteAllLines(Path.Combine(output,"foundation-contracts.txt"),facts);
        }
        finally
        {gm.SetPaused(false);gm.stageManager.StopStage();p.RestoreAt(new Vector3(0,.1f,0));Time.captureDeltaTime=1f/60;}
    }
}
