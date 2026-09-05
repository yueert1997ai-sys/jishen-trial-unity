using System.Collections.Generic;
using UnityEngine;

public enum RunUpgradeKind
{
    BeamDamage, FireRate, SplitterBeam, PiercingRounds,
    BurstCore, ArmorPlating, DashCapacitor, Nanorepair
}

public class RunUpgradeOption
{
    public RunUpgradeKind kind;
    public int rank;
    public string title;
    public string description;
}

public class RunUpgradeSystem : MonoBehaviour
{
    public PlayerStats playerStats;
    public WeaponController weaponController;
    public float DamageMultiplier => 1f + Rank(RunUpgradeKind.BeamDamage) * 0.28f;
    public float FireRateMultiplier => 1f + Rank(RunUpgradeKind.FireRate) * 0.25f;
    public int BonusBeamProjectiles => Rank(RunUpgradeKind.SplitterBeam) * 2;
    public float SplitShotMultiplier => BonusBeamProjectiles == 0 ? 1f : BonusBeamProjectiles == 2 ? 0.55f : 0.4f;
    public int BonusPierce => Rank(RunUpgradeKind.PiercingRounds) * 2;
    public float BeamExplosionRadius => Rank(RunUpgradeKind.BurstCore) == 0 ? 0f : 0.8f + 0.65f * Rank(RunUpgradeKind.BurstCore);
    public float KillHeal => Rank(RunUpgradeKind.Nanorepair) * 1.2f;
    public int Count => acquired.Count;
    public int Seed { get; private set; }
    public IReadOnlyList<RunUpgradeKind> Acquired => acquired;
    private readonly List<RunUpgradeKind> acquired = new List<RunUpgradeKind>();
    private readonly int[] ranks = new int[8];
    private string summary = "SYSTEMS  0 / 6";
    private bool summaryChinese;

    private void Awake() { FindReferences(); }
    public int Rank(RunUpgradeKind kind) => ranks[(int)kind];
    public static int MaxRank(RunUpgradeKind kind) => kind == RunUpgradeKind.SplitterBeam || kind == RunUpgradeKind.BurstCore ? 2 : 3;

    public void ResetUpgrades(int seed = 0)
    {
        acquired.Clear();
        System.Array.Clear(ranks, 0, ranks.Length);
        Seed = seed == 0 ? System.Environment.TickCount : seed;
        RefreshSummary();
    }

    public List<RunUpgradeOption> GenerateOptions()
    {
        // Choice RNG is independent of particles/audio and reproducible after a checkpoint.
        var random = new System.Random(unchecked(Seed + Count * 7919));
        var candidates = new List<RunUpgradeKind>();
        for (int i = 0; i < ranks.Length; i++)
            if (ranks[i] < MaxRank((RunUpgradeKind)i)) candidates.Add((RunUpgradeKind)i);
        var options = new List<RunUpgradeOption>();
        RunUpgradeKind synergy = Rank(RunUpgradeKind.SplitterBeam) > 0 ? RunUpgradeKind.BurstCore
            : Rank(RunUpgradeKind.PiercingRounds) > 0 ? RunUpgradeKind.FireRate
            : Rank(RunUpgradeKind.FireRate) > 0 ? RunUpgradeKind.PiercingRounds
            : Rank(RunUpgradeKind.BurstCore) > 0 ? RunUpgradeKind.SplitterBeam
            : random.Next(2) == 0 ? RunUpgradeKind.FireRate : RunUpgradeKind.SplitterBeam;
        if (candidates.Remove(synergy)) options.Add(CreateOption(synergy));
        while (options.Count < 3 && candidates.Count > 0)
        {
            int index = random.Next(candidates.Count);
            options.Add(CreateOption(candidates[index]));
            candidates.RemoveAt(index);
        }
        return options;
    }

    public bool ApplyOption(RunUpgradeOption option)
    {
        if (option == null || (int)option.kind < 0 || (int)option.kind >= ranks.Length) return false;
        int current = Rank(option.kind);
        if (current >= MaxRank(option.kind) || (option.rank > 0 && option.rank != current + 1)) return false;
        FindReferences();
        acquired.Add(option.kind);
        ranks[(int)option.kind]++;
        if (playerStats != null)
        {
            if (option.kind == RunUpgradeKind.ArmorPlating) playerStats.AddMaxHealth(35f, true);
            if (option.kind == RunUpgradeKind.DashCapacitor) playerStats.AddDashBonus(0.6f, 0.18f);
            if (option.kind == RunUpgradeKind.Nanorepair) playerStats.Heal(24f);
        }
        RefreshSummary();
        return true;
    }

    public void Restore(RunUpgradeKind[] saved, int seed)
    {
        ResetUpgrades(seed);
        foreach (var kind in saved) ApplyOption(new RunUpgradeOption { kind = kind });
    }

    public string GetSummary()
    {
        if (summaryChinese != GamePreferences.Chinese) RefreshSummary();
        return summary;
    }

    private void RefreshSummary()
    {
        var names = new List<string>();
        string[] shortNames = { "AMP", "RATE", "SPLIT", "PIERCE", "BLAST", "ARMOR", "DASH", "REPAIR" };
        summaryChinese = GamePreferences.Chinese;
        for (int i = 0; i < ranks.Length; i++) if (ranks[i] > 0) names.Add(GameText.T(shortNames[i]) + " " + ranks[i]);
        summary = GameText.T("SYSTEMS") + "  " + Count + " / 6" + (names.Count > 0 ? "   |   " + string.Join(" / ", names) : "");
    }

    private RunUpgradeOption CreateOption(RunUpgradeKind kind)
    {
        int rank = Rank(kind) + 1;
        return new RunUpgradeOption { kind = kind, rank = rank, title = GameText.T(GetTitle(kind)) + "  " + rank + "/" + MaxRank(kind), description = GameText.T(GetDescription(kind, rank)) };
    }

    public static string GetTitle(RunUpgradeKind kind)
    {
        switch (kind)
        {
            case RunUpgradeKind.FireRate: return "Overclocked Trigger";
            case RunUpgradeKind.SplitterBeam: return "Splitter Beam";
            case RunUpgradeKind.PiercingRounds: return "Piercing Rounds";
            case RunUpgradeKind.BurstCore: return "Burst Core";
            case RunUpgradeKind.ArmorPlating: return "Reactive Armor";
            case RunUpgradeKind.DashCapacitor: return "Dash Capacitor";
            case RunUpgradeKind.Nanorepair: return "Nanorepair Loop";
            default: return "Beam Amplifier";
        }
    }

    private static string GetDescription(RunUpgradeKind kind, int rank)
    {
        switch (kind)
        {
            case RunUpgradeKind.FireRate: return "+25% beam fire rate.\n\nPairs with Piercing Rounds.";
            case RunUpgradeKind.SplitterBeam: return rank == 1 ? "3 beams at 55% damage each.\n\nEach beam carries Blast and Pierce." : "5 beams at 40% damage each.\n\nWider crowd coverage.";
            case RunUpgradeKind.PiercingRounds: return "+2 pierced enemies per shot.\n\nPairs with Overclocked Trigger.";
            case RunUpgradeKind.BurstCore: return "Blast radius " + (0.8f + 0.65f * rank).ToString("0.00") + " m.\n\nFull direct damage; splash falls off.";
            case RunUpgradeKind.ArmorPlating: return "+35 max HP.\n\nRepair the added armor immediately.";
            case RunUpgradeKind.DashCapacitor: return "+0.6 m dash distance.\n-0.18 s dash cooldown.";
            case RunUpgradeKind.Nanorepair: return "+1.2 HP per kill.\n\nRepair 24 HP now.";
            default: return "+28% beam damage.\n\nAlso amplifies the missile salvo.";
        }
    }

    private void FindReferences()
    {
        if (playerStats == null) playerStats = FindFirstObjectByType<PlayerStats>();
        if (weaponController == null && playerStats != null) weaponController = playerStats.GetComponent<WeaponController>();
        if (weaponController != null && weaponController.upgradeSystem == null) weaponController.upgradeSystem = this;
    }
}
