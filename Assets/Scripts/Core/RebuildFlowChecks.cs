using System;
using System.Collections;
using System.IO;
using UnityEngine;

public static class RebuildFlowChecks
{
    public static IEnumerator Run(GameManager gm,PlayerController p,string output,Action<bool,string> check,Action<string> capture)
    {
        gm.EnterResult(false);gm.EnterHangar();p.Loadout.Select(PrimaryWeapon.M7);gm.BeginFullDemo();
        check(CombatRuntime.Run.Mode==CombatMode.FullDemo&&gm.stageManager.Director!=null,"formal entry initializes the shared director and explicit full-run state");
        p.GetComponent<Damageable>().SetInvulnerable(1000);
        for(int room=0;room<6;room++)
        {
            check(gm.stageManager.CurrentEncounter==room,"formal encounter order "+(room+1));
            int maximum=0;
            for(int frame=0;frame<6000&&!gm.AwaitingContinue;frame++)
            {
                maximum=Mathf.Max(maximum,gm.stageManager.EnemiesAlive);
                foreach(var enemy in UnityEngine.Object.FindObjectsByType<EnemyBase>(FindObjectsSortMode.None))
                    if(!enemy.GetComponent<Damageable>().IsDead)enemy.GetComponent<Damageable>().Kill(new DamageInfo(p.gameObject,p.transform.position,p.GetComponent<Damageable>(),99999));
                yield return null;
            }
            check(gm.AwaitingContinue&&gm.CompletedEncounters==room+1,"all live and reserved enemies clear exactly once in room "+(room+1));
            check(maximum<=CombatRules.Current.MaxHostiles&&gm.stageManager.enemySpawner.PendingSpawns==0,"room "+(room+1)+" obeys combined live and pending capacity");
            for(int i=0;i<60;i++)yield return null;
            check(gm.Phase==GamePhase.Combat&&gm.upgradeSystem.Count==room&&gm.CanPlayerControl,"clear hold leaves movement and test firing available before reward "+(room+1));
            if(room==1)check(gm.equipmentLoop.Absorption.Pickup!=null,"second encounter preserves its designated rifle after clear");
            if(room==1)capture("full-clear-salvage.png");
            check(gm.ContinueAfterSalvage()&&gm.Phase==GamePhase.Reward,"explicit continue opens the reward once "+(room+1));
            check(!gm.ContinueAfterSalvage(),"duplicate continue cannot advance two encounters");
            if(room==1)check(gm.equipmentLoop.Warehouse.Owns("e01_rifle")&&p.Loadout.Selected==PrimaryWeapon.M7,"untried reward is collected while original gun stays installed");
            var options=gm.upgradeSystem.GenerateOptions();check(options.Count==3,"three reward options after room "+(room+1));
            check(gm.upgradeSystem.ApplyOption(options[0]),"room reward commits once "+(room+1));gm.FinishReward();yield return null;
        }
        check(gm.CompletedEncounters==6&&gm.upgradeSystem.Count==6&&gm.stageManager.CurrentEncounter==6,"six fights and six upgrades lead into the liquid Boss");
        var boss=gm.stageManager.Director.Boss;check(boss!=null,"formal director spawns the authored Boss");
        boss.GetComponent<Damageable>().Kill(new DamageInfo(p.gameObject,p.transform.position,p.GetComponent<Damageable>(),99999));
        check(gm.stageManager.Flow.Phase==EncounterPhase.BossDefeat,"Boss death owns the terminal presentation phase");
        gm.EnterResult(false);
        check(gm.Phase==GamePhase.Combat,"late defeat cannot overwrite earned Boss victory");
        for(int i=0;i<600&&gm.Phase!=GamePhase.Result;i++)yield return null;
        check(gm.Phase==GamePhase.Result&&gm.LastResultVictory,"Boss presentation finishes in one victory result");
        check(gm.upgradeSystem.Count==0&&gm.equipmentLoop.Warehouse.Owns("e01_rifle"),"run upgrades clear while permanent collection survives completion");
        capture("full-victory.png");
        File.WriteAllText(Path.Combine(output,"flow-scope.txt"),"Production spawning, clear/continue/reward, F skip and Boss terminal integration. Enemy damage is injected for lifecycle coverage; this is not a natural combat or duration result.");
    }
}
