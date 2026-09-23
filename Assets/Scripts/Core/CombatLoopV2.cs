using UnityEngine;

// Compatibility names for existing components. One immutable authored definition owns tuning.
public static class CombatLoopV2
{
    public static bool Enabled => CombatSliceSettings.Enabled;
    public static float BossHealth => CombatRules.Current.BossHealth;
    public static float BossCapacity => CombatRules.Current.BossCapacity;
    public static float BossBreakSeconds => CombatRules.Current.BossBreakSeconds;
    public static float BossArmor => CombatRules.Current.BossArmor;
    public static float RifleImpact => CombatRules.Current.RifleImpact;
    public static float HeavyRifleImpact => CombatRules.Current.HeavyRifleImpact;
    public static float HeavyRifleInterval => CombatRules.Current.HeavyRifleInterval;
    public static float HeavyRiflePunish => CombatRules.Current.HeavyRiflePunish;
    public static float EliteCapacity => CombatRules.Current.EliteCapacity;
    public static float EliteFrontDamage => CombatRules.Current.EliteFrontDamage;
    public static float EliteFlankDamage => CombatRules.Current.EliteFlankDamage;
    public static float EliteTurnSpeed => CombatRules.Current.EliteTurnSpeed;
    public static float EliteRecovery => CombatRules.Current.EliteRecovery;
    public static float MissileEvadeSeconds => CombatRules.Current.MissileEvadeSeconds;
    public static float MissileEvadeCooldown => CombatRules.Current.MissileEvadeCooldown;
    public static float MissileImpact => CombatRules.Current.MissileImpact;
    public static int EncounterGroups => CombatRules.Current.Encounters.Count;
    public static float EncounterLimit => CombatRules.Current.EncounterLimit;
}
