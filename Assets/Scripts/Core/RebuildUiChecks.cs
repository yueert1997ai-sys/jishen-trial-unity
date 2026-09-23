using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public static class RebuildUiChecks
{
    public static IEnumerator Run(GameManager gm,PlayerController p,string output,Action<bool,string> check,Action<string> capture)
    {
        string legacy=Path.Combine(output,"legacy-v1.json");
        File.WriteAllText(legacy,"{\"version\":1,\"owned\":[\"pulse\",\"standard\",\"lance\",\"vector\"],\"weapon\":\"lance\",\"backpack\":\"vector\"}");
        var old=new SalvageWarehouse(legacy);
        check(old.Owns("lance")&&old.Owns("vector")&&old.Profile.weapon=="lance"&&old.Profile.backpack=="vector","old v1 collection and equipment identifiers remain readable");
        check(old.Acquire("e01_rifle"),"old collection accepts new rifle without changing schema");
        check(new SalvageWarehouse(legacy).Owns("e01_rifle"),"ownership survives disk reload");
        File.WriteAllText(legacy,"{broken");
        check(new SalvageWarehouse(legacy).Owns("lance"),"damaged primary profile recovers the prior atomic backup");
        gm.ExitPractice();yield return null;
        check(gm.Phase==GamePhase.Hangar&&gm.hangarUI.IsVisible,"hangar is an explicit return destination");
        check(GameObject.Find("DeployButton")!=null&&GameObject.Find("WeaponTrialButton")!=null,"formal run and zero-upgrade practice have separate visible entries");
        capture("v4-hangar.png");for(int i=0;i<3;i++)yield return null;
        GameObject.Find("WeaponTrialButton").GetComponent<Button>().onClick.Invoke();
        for(int i=0;i<8;i++)yield return null;
        check(CombatRuntime.Run.Mode==CombatMode.ShortCombat&&UnityEngine.Object.FindFirstObjectByType<P0CombatDemo>()!=null,"practice button enters shared short-combat mode");
        check(p.Loadout.CanUseSword&&p.Loadout.CanUseRifle&&gm.upgradeSystem.Count==0,"practice has both weapons at base strength");
        gm.ExitPractice();yield return null;
        check(UnityEngine.Object.FindFirstObjectByType<P0CombatDemo>()==null&&gm.stageManager.EnemiesAlive==0&&gm.stageManager.enemySpawner.PendingSpawns==0,"exiting practice removes its director, actors and pending spawns");
        GameObject.Find("DeployButton").GetComponent<Button>().onClick.Invoke();yield return null;
        check(CombatRuntime.Run.Mode==CombatMode.FullDemo&&!CombatSliceSettings.Enabled&&gm.stageManager.Flow!=null,"formal button creates the full director without a practice overlay");
        check(gm.equipmentLoop.Backpack.id=="standard"&&!gm.equipmentLoop.TryEquip("vector"),"fixed backpack cannot be changed through equipment actions");
        for(int round=0;round<3;round++)
        {
            int generation=CombatRuntime.Run.Generation;
            gm.upgradeSystem.Restore(new[]{RunUpgradeKind.FireRate,RunUpgradeKind.BladeTempo},123);
            for(int i=0;i<12;i++){p.Simulate(new PlayerCommand{Fire=true,HasAim=true,AimPoint=new Vector3(0,1,18)},Time.deltaTime);yield return null;}
            gm.EnterResult(false);yield return null;
            check(gm.upgradeSystem.Count==0&&gm.stageManager.EnemiesAlive==0&&gm.stageManager.enemySpawner.PendingSpawns==0,"failure clears run modifiers and outstanding spawns on restart "+round);
            check(UnityEngine.Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None).Length==0,"failure clears old bullets on restart "+round);
            GameObject.Find("ReturnHangarButton").GetComponent<Button>().onClick.Invoke();yield return null;
            check(gm.IsCombatActive&&CombatRuntime.Run.Generation>generation&&gm.CompletedEncounters==0&&gm.upgradeSystem.Count==0,"result retry button immediately starts a fresh full run "+round);
            check(gm.equipmentLoop.Warehouse.Owns("e01_rifle"),"retry preserves earned collection ownership");
        }
        capture("v4-full-hud.png");for(int i=0;i<3;i++)yield return null;gm.EnterResult(false);yield return null;capture("v4-result.png");for(int i=0;i<3;i++)yield return null;
    }
}
