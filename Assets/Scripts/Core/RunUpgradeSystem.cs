using System.Collections.Generic;
using UnityEngine;

public enum RunUpgradeKind
{
    BeamDamage,
    FireRate,
    SplitterBeam,
    PiercingRounds,
    BurstCore,
    ArmorPlating,
    DashCapacitor,
    Nanorepair
}

public class RunUpgradeOption
{
    public RunUpgradeKind kind;
    public string title;
    public string description;
}

public class RunUpgradeSystem : MonoBehaviour
{
    public PlayerStats playerStats;
    public WeaponController weaponController;

    public float DamageMultiplier { get { return 1f + damageBonus; } }
    public float FireRateMultiplier { get { return 1f + fireRateBonus; } }
    public int BonusBeamProjectiles { get { return bonusBeamProjectiles; } }
    public int BonusPierce { get { return bonusPierce; } }
    public float BeamExplosionRadius { get { return beamExplosionRadius; } }
    public float KillHeal { get { return killHeal; } }

    private readonly List<RunUpgradeKind> acquired = new List<RunUpgradeKind>();
    private float damageBonus;
    private float fireRateBonus;
    private int bonusBeamProjectiles;
    private int bonusPierce;
    private float beamExplosionRadius;
    private float killHeal;

    private void Awake()
    {
        FindReferences();
    }

    public void ResetUpgrades()
    {
        acquired.Clear();
        damageBonus = 0f;
        fireRateBonus = 0f;
        bonusBeamProjectiles = 0;
        bonusPierce = 0;
        beamExplosionRadius = 0f;
        killHeal = 0f;
    }

    public List<RunUpgradeOption> GenerateOptions()
    {
        RunUpgradeKind[] pool =
        {
            RunUpgradeKind.BeamDamage,
            RunUpgradeKind.FireRate,
            RunUpgradeKind.SplitterBeam,
            RunUpgradeKind.PiercingRounds,
            RunUpgradeKind.BurstCore,
            RunUpgradeKind.ArmorPlating,
            RunUpgradeKind.DashCapacitor,
            RunUpgradeKind.Nanorepair
        };

        List<RunUpgradeKind> candidates = new List<RunUpgradeKind>();
        for (int i = 0; i < pool.Length; i++)
        {
            if (!acquired.Contains(pool[i]))
            {
                candidates.Add(pool[i]);
            }
        }

        List<RunUpgradeOption> options = new List<RunUpgradeOption>();
        while (options.Count < 3 && candidates.Count > 0)
        {
            int index = Random.Range(0, candidates.Count);
            options.Add(CreateOption(candidates[index]));
            candidates.RemoveAt(index);
        }

        while (options.Count < 3)
        {
            options.Add(CreateOption(RunUpgradeKind.BeamDamage));
        }

        return options;
    }

    public void ApplyOption(RunUpgradeOption option)
    {
        if (option == null)
        {
            return;
        }

        FindReferences();
        acquired.Add(option.kind);
        switch (option.kind)
        {
            case RunUpgradeKind.BeamDamage:
                damageBonus += 0.28f;
                break;
            case RunUpgradeKind.FireRate:
                fireRateBonus += 0.25f;
                break;
            case RunUpgradeKind.SplitterBeam:
                bonusBeamProjectiles += 2;
                damageBonus += 0.08f;
                break;
            case RunUpgradeKind.PiercingRounds:
                bonusPierce += 2;
                damageBonus += 0.1f;
                break;
            case RunUpgradeKind.BurstCore:
                beamExplosionRadius = Mathf.Max(beamExplosionRadius, 1.45f);
                break;
            case RunUpgradeKind.ArmorPlating:
                if (playerStats != null)
                {
                    playerStats.AddMaxHealth(45f, true);
                }

                break;
            case RunUpgradeKind.DashCapacitor:
                if (playerStats != null)
                {
                    playerStats.AddDashBonus(1.1f, 0.22f);
                }

                break;
            case RunUpgradeKind.Nanorepair:
                killHeal += 4f;
                if (playerStats != null)
                {
                    playerStats.Heal(28f);
                }

                break;
        }
    }

    public string GetSummary()
    {
        if (acquired.Count == 0)
        {
            return "Upgrades: none";
        }

        List<string> names = new List<string>();
        for (int i = 0; i < acquired.Count; i++)
        {
            names.Add(GetTitle(acquired[i]));
        }

        return "Upgrades: " + string.Join(", ", names.ToArray());
    }

    private RunUpgradeOption CreateOption(RunUpgradeKind kind)
    {
        return new RunUpgradeOption
        {
            kind = kind,
            title = GetTitle(kind),
            description = GetDescription(kind)
        };
    }

    private static string GetTitle(RunUpgradeKind kind)
    {
        switch (kind)
        {
            case RunUpgradeKind.FireRate:
                return "Overclocked Trigger";
            case RunUpgradeKind.SplitterBeam:
                return "Splitter Beam";
            case RunUpgradeKind.PiercingRounds:
                return "Piercing Rounds";
            case RunUpgradeKind.BurstCore:
                return "Burst Core";
            case RunUpgradeKind.ArmorPlating:
                return "Reactive Armor";
            case RunUpgradeKind.DashCapacitor:
                return "Dash Capacitor";
            case RunUpgradeKind.Nanorepair:
                return "Nanorepair Loop";
            default:
                return "Beam Amplifier";
        }
    }

    private static string GetDescription(RunUpgradeKind kind)
    {
        switch (kind)
        {
            case RunUpgradeKind.FireRate:
                return "+25% beam fire rate.";
            case RunUpgradeKind.SplitterBeam:
                return "Main beam fires two extra angled shots.";
            case RunUpgradeKind.PiercingRounds:
                return "Beam shots pierce two extra enemies.";
            case RunUpgradeKind.BurstCore:
                return "Beam shots create a small blast on hit.";
            case RunUpgradeKind.ArmorPlating:
                return "+45 max HP and heal the added armor immediately.";
            case RunUpgradeKind.DashCapacitor:
                return "Longer dash and shorter dash cooldown.";
            case RunUpgradeKind.Nanorepair:
                return "Heal on every kill and gain an immediate repair burst.";
            default:
                return "+28% beam damage.";
        }
    }

    private void FindReferences()
    {
        if (playerStats == null)
        {
            playerStats = FindFirstObjectByType<PlayerStats>();
        }

        if (weaponController == null && playerStats != null)
        {
            weaponController = playerStats.GetComponent<WeaponController>();
        }

        if (weaponController != null && weaponController.upgradeSystem == null)
        {
            weaponController.upgradeSystem = this;
        }
    }
}
