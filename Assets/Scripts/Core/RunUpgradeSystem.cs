using System;
using System.Collections.Generic;
using UnityEngine;

public enum RunUpgradeKind
{
    BladeReach, BladeTempo, FireRate, PiercingRounds, ThrusterEfficiency, SupportCooldown,
    // Retired source identifiers only; never offered, applied, or serialized to the warehouse.
    BeamDamage=100, SplitterBeam, BurstCore, ArmorPlating, DashCapacitor, Nanorepair
}
public class RunUpgradeOption
{
    public RunUpgradeKind kind;
    public int rank, generation, choiceCount;
    public string title,description;
}
public class RunUpgradeSystem : MonoBehaviour
{
    public PlayerStats playerStats;
    public WeaponController weaponController;
    public float BladeReachMultiplier=>1+.10f*Rank(RunUpgradeKind.BladeReach);
    public float BladeTempoMultiplier=>1+.08f*Rank(RunUpgradeKind.BladeTempo);
    public float FireRateMultiplier=>1+.10f*Rank(RunUpgradeKind.FireRate);
    public int BonusPierce=>Rank(RunUpgradeKind.PiercingRounds);
    public float ThrusterCostMultiplier=>1-.10f*Rank(RunUpgradeKind.ThrusterEfficiency);
    public float SupportCooldownMultiplier=>1-.10f*Rank(RunUpgradeKind.SupportCooldown);
    public float DamageMultiplier=>1;
    public int BonusBeamProjectiles=>0;
    public float SplitShotMultiplier=>1;
    public float BeamExplosionRadius=>0;
    public float KillHeal=>0;
    public int Count=>acquired.Count;
    public int Seed {get;private set;}
    public IReadOnlyList<RunUpgradeKind> Acquired=>acquired.AsReadOnly();
    readonly List<RunUpgradeKind> acquired=new List<RunUpgradeKind>();
    readonly int[] ranks=new int[6];
    int generation;
    void Awake(){FindReferences();}
    public int Rank(RunUpgradeKind kind)=>(int)kind>=0&&(int)kind<6?ranks[(int)kind]:0;
    public static int MaxRank(RunUpgradeKind kind)=>(int)kind>=0&&(int)kind<6?3:0;
    public void ResetUpgrades(int seed=0)
    {
        FindReferences();playerStats?.ClearRunUpgradeBonuses();acquired.Clear();Array.Clear(ranks,0,6);
        Seed=seed==0?CombatRuntime.Run?.Seed??Environment.TickCount:seed;generation++;
    }
    public bool IsEffective(RunUpgradeKind kind)
    {
        var loadout=weaponController!=null?weaponController.playerController?.Loadout:null;
        if(kind==RunUpgradeKind.BladeReach||kind==RunUpgradeKind.BladeTempo)return loadout==null||loadout.CanUseSword;
        if(kind==RunUpgradeKind.FireRate||kind==RunUpgradeKind.PiercingRounds)return loadout==null||loadout.CanUseRifle;
        return kind==RunUpgradeKind.ThrusterEfficiency||kind==RunUpgradeKind.SupportCooldown;
    }
    public List<RunUpgradeOption> GenerateOptions()
    {
        FindReferences();var random=new System.Random(unchecked(Seed+Count*7919));
        var candidates=new List<RunUpgradeKind>();
        for(int i=0;i<6;i++)if(ranks[i]<3&&IsEffective((RunUpgradeKind)i))candidates.Add((RunUpgradeKind)i);
        var options=new List<RunUpgradeOption>();
        while(options.Count<3&&candidates.Count>0)
        {
            int index=random.Next(candidates.Count);var kind=candidates[index];candidates.RemoveAt(index);
            options.Add(new RunUpgradeOption{kind=kind,rank=Rank(kind)+1,generation=generation,choiceCount=Count,
                title=GetTitle(kind)+"  "+(Rank(kind)+1)+"/3",description=GetDescription(kind)});
        }
        return options;
    }
    public bool ApplyOption(RunUpgradeOption option)
    {
        if(option==null||Count>=6||option.generation!=generation||option.choiceCount!=Count||!IsEffective(option.kind)
            ||MaxRank(option.kind)==0||Rank(option.kind)>=3||option.rank!=Rank(option.kind)+1)return false;
        ranks[(int)option.kind]++;acquired.Add(option.kind);return true;
    }
    public void Restore(RunUpgradeKind[] saved,int seed)
    {
        ResetUpgrades(seed);if(saved==null)return;
        foreach(var kind in saved)if(Count<6&&MaxRank(kind)>0&&Rank(kind)<3){ranks[(int)kind]++;acquired.Add(kind);}
    }
    public string GetSummary()
    {
        var names=new List<string>();for(int i=0;i<6;i++)if(ranks[i]>0)names.Add(GetTitle((RunUpgradeKind)i)+" "+ranks[i]);
        return "局内强化 "+Count+" / 6"+(names.Count>0?"  ·  "+string.Join(" / ",names):" · 基础装备");
    }
    public static string GetTitle(RunUpgradeKind kind)
    {
        string[] cn={"刀距延伸","挥刀加速","射速提升","贯穿弹","推进效率","E 回转"};
        string[] en={"Blade reach","Blade tempo","Fire rate","Penetration","Thruster efficiency","Support recharge"};
        return MaxRank(kind)==0?"Retired":(GamePreferences.Chinese?cn:en)[(int)kind];
    }
    static string GetDescription(RunUpgradeKind kind)
    {
        string[] cn={"每级刀刃长度 +10%\n实际刀路与命中范围一起延伸。","每级攻击速度 +8%\n起手、命中与收招共同加速。","每级射速 +10%\n更换远程武器后自动按基础射速计算。","每级穿透目标 +1\n同发弹体仍只伤害每个目标一次。","每级冲刺与推进消耗 -10%\n从基础消耗加算，最多降低 30%。","每级 E 冷却 -10%\n固定肩部支援更快就绪。"};
        string[] en={"+10% blade reach per rank. Visible blade and collision extend together.","+8% attack speed per rank. All blade phases share the same clock.","+10% fire rate per rank, recalculated from the equipped gun.","+1 pierced target per rank. One contact per target.","-10% dash and boost cost per rank. Additive, capped at 30%.","-10% fixed shoulder support cooldown per rank."};
        return (GamePreferences.Chinese?cn:en)[(int)kind];
    }
    void FindReferences()
    {
        if(playerStats==null)playerStats=FindFirstObjectByType<PlayerStats>();
        if(weaponController==null&&playerStats!=null)weaponController=playerStats.GetComponent<WeaponController>();
        if(weaponController!=null)weaponController.upgradeSystem=this;
    }
}
