using System;
using System.Collections.Generic;
using UnityEngine;
#pragma warning disable 0649 // JSON fields are assigned by Unity's serializer.

// Authored definitions are loaded once, validated, then copied into immutable runtime values.
// No per-run health, cooldown, progress or collection data belongs in this object.
public sealed class CombatRules
{
    [Serializable] sealed class Definition
    {
        public int version;
        public float dashBuffer, slashBuffer, comboBuffer, supportBuffer, dashSlashWindow, dashSlashAdvance;
        public float stowSeconds, drawSeconds, spawnWarning, spawnStagger, clearDelay, groupInterval;
        public float encounterLimit, labLimit;
        public int maxHostiles;
        public float bossHealth, bossCapacity, bossBreakSeconds, bossArmor;
        public float rifleImpact, heavyRifleImpact, heavyRifleInterval, heavyRiflePunish, missileImpact;
        public float eliteCapacity, eliteFrontDamage, eliteFlankDamage, eliteTurnSpeed, eliteRecovery;
        public float missileEvadeSeconds, missileEvadeCooldown, meleeWindup, rangedWindup, eliteWindup, meleeRecovery, rangedRecovery;
        public int maxConcurrentAttackers, rangedBurstShots;
        public float surroundRadiusMelee, surroundRadiusRanged, rangedBurstInterval, rangedRepositionSeconds, rangedAimLead;
        public float meleeLungeRange, meleeLungeDistance, meleeLungeWindup, meleeLungeDamage, meleeLungeCooldown;
        public BeatDefinition[] encounters;
        public BeatDefinition lab;
    }
    [Serializable] sealed class BeatDefinition
    {
        public string id;
        public Vector3 center;
        public EnemyKind[] roles;
        public Vector3[] positions;
    }
    public sealed class Beat
    {
        public readonly string Id;
        public readonly Vector3 Center;
        public readonly IReadOnlyList<EnemyKind> Roles;
        public readonly IReadOnlyList<Vector3> Positions;
        internal Beat(string id, Vector3 center, EnemyKind[] roles, Vector3[] positions)
        {
            Id = id; Center = center;
            Roles = Array.AsReadOnly((EnemyKind[])roles.Clone());
            Positions = Array.AsReadOnly(positions == null ? Array.Empty<Vector3>() : (Vector3[])positions.Clone());
        }
    }
    public readonly float DashBuffer, SlashBuffer, ComboBuffer, SupportBuffer, DashSlashWindow, DashSlashAdvance;
    public readonly float StowSeconds, DrawSeconds, SpawnWarning, SpawnStagger, ClearDelay, GroupInterval;
    public readonly float EncounterLimit, LabLimit;
    public readonly int MaxHostiles;
    public readonly float BossHealth, BossCapacity, BossBreakSeconds, BossArmor;
    public readonly float RifleImpact, HeavyRifleImpact, HeavyRifleInterval, HeavyRiflePunish, MissileImpact;
    public readonly float EliteCapacity, EliteFrontDamage, EliteFlankDamage, EliteTurnSpeed, EliteRecovery;
    public readonly float MissileEvadeSeconds, MissileEvadeCooldown, MeleeWindup, RangedWindup, EliteWindup;
    public readonly float MeleeRecovery, RangedRecovery;
    public readonly int MaxConcurrentAttackers, RangedBurstShots;
    public readonly float SurroundRadiusMelee, SurroundRadiusRanged, RangedBurstInterval, RangedRepositionSeconds;
    public readonly float RangedAimLead, MeleeLungeRange, MeleeLungeDistance, MeleeLungeWindup, MeleeLungeDamage, MeleeLungeCooldown;
    public readonly IReadOnlyList<Beat> Encounters;
    public readonly Beat Lab;
    static CombatRules current;
    public static CombatRules Current => current ?? (current = Load());
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Reset() { current = null; }

    static CombatRules Load()
    {
        var asset = Resources.Load<TextAsset>("Foundation/CombatRules");
        if (asset == null) throw new InvalidOperationException("Missing Foundation/CombatRules definition");
        return Parse(asset.text);
    }
    public static CombatRules Parse(string json)
    {
        var d = JsonUtility.FromJson<Definition>(json);
        if (d == null || d.version != 4) throw new InvalidOperationException("Unsupported combat rules version");
        foreach (var field in typeof(Definition).GetFields())
            if (field.FieldType == typeof(float))
            {
                float value = (float)field.GetValue(d);
                if (float.IsNaN(value) || float.IsInfinity(value) || value <= 0)
                    throw new InvalidOperationException("Invalid combat rule: " + field.Name);
            }
        if (d.maxHostiles < 1 || d.maxHostiles > 32 || d.encounters == null || d.encounters.Length == 0)
            throw new InvalidOperationException("Invalid encounter budget");
        if (d.maxConcurrentAttackers < 1 || d.maxConcurrentAttackers > 8
            || d.rangedBurstShots < 1 || d.rangedBurstShots > 6)
            throw new InvalidOperationException("Invalid enemy tactics budget");
        var ids = new HashSet<string>();
        foreach (var beat in d.encounters) ValidateBeat(beat, d.maxHostiles, ids, false);
        ValidateBeat(d.lab, d.maxHostiles, ids, true);
        return new CombatRules(d);
    }
    static void ValidateBeat(BeatDefinition b, int limit, HashSet<string> ids, bool lab)
    {
        if (b == null || string.IsNullOrWhiteSpace(b.id) || !ids.Add(b.id) || b.roles == null
            || b.roles.Length < 1 || b.roles.Length > limit || (lab && (b.positions == null || b.positions.Length != b.roles.Length)))
            throw new InvalidOperationException("Invalid encounter definition");
        foreach (var role in b.roles)
            if (!Enum.IsDefined(typeof(EnemyKind), role)) throw new InvalidOperationException("Invalid enemy role");
    }
    CombatRules(Definition d)
    {
        DashBuffer=d.dashBuffer; SlashBuffer=d.slashBuffer; ComboBuffer=d.comboBuffer; SupportBuffer=d.supportBuffer;
        DashSlashWindow=d.dashSlashWindow; DashSlashAdvance=d.dashSlashAdvance;
        StowSeconds=d.stowSeconds; DrawSeconds=d.drawSeconds; SpawnWarning=d.spawnWarning; SpawnStagger=d.spawnStagger;
        ClearDelay=d.clearDelay; GroupInterval=d.groupInterval; EncounterLimit=d.encounterLimit; LabLimit=d.labLimit;
        MaxHostiles=d.maxHostiles; BossHealth=d.bossHealth; BossCapacity=d.bossCapacity;
        BossBreakSeconds=d.bossBreakSeconds; BossArmor=d.bossArmor; RifleImpact=d.rifleImpact;
        HeavyRifleImpact=d.heavyRifleImpact; HeavyRifleInterval=d.heavyRifleInterval; HeavyRiflePunish=d.heavyRiflePunish;
        MissileImpact=d.missileImpact; EliteCapacity=d.eliteCapacity; EliteFrontDamage=d.eliteFrontDamage;
        EliteFlankDamage=d.eliteFlankDamage; EliteTurnSpeed=d.eliteTurnSpeed; EliteRecovery=d.eliteRecovery;
        MissileEvadeSeconds=d.missileEvadeSeconds; MissileEvadeCooldown=d.missileEvadeCooldown;
        MeleeWindup=d.meleeWindup; RangedWindup=d.rangedWindup; EliteWindup=d.eliteWindup;
        MeleeRecovery=d.meleeRecovery;RangedRecovery=d.rangedRecovery;
        MaxConcurrentAttackers=d.maxConcurrentAttackers; RangedBurstShots=d.rangedBurstShots;
        SurroundRadiusMelee=d.surroundRadiusMelee; SurroundRadiusRanged=d.surroundRadiusRanged;
        RangedBurstInterval=d.rangedBurstInterval; RangedRepositionSeconds=d.rangedRepositionSeconds;
        RangedAimLead=d.rangedAimLead; MeleeLungeRange=d.meleeLungeRange; MeleeLungeDistance=d.meleeLungeDistance;
        MeleeLungeWindup=d.meleeLungeWindup; MeleeLungeDamage=d.meleeLungeDamage; MeleeLungeCooldown=d.meleeLungeCooldown;
        var beats = new Beat[d.encounters.Length];
        for (int i=0; i<beats.Length; i++) { var b=d.encounters[i]; beats[i]=new Beat(b.id,b.center,b.roles,b.positions); }
        Encounters=Array.AsReadOnly(beats);
        Lab=new Beat(d.lab.id,d.lab.center,d.lab.roles,d.lab.positions);
    }
}
