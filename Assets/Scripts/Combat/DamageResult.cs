public enum DamageRejection { None, NotInCombat, Dead, Invulnerable, InvalidAmount, NoDamage, Reentrant }
public enum HitPresentation { None, Health, Armor, ArmorBreak, Kill }

// The committed result is a value, separate from the caller's mutable attack request.
// Presentation and telemetry can inspect it without re-running damage or changing another listener's data.
public readonly struct DamageResult
{
    public readonly DamageRejection Rejection;
    public readonly float HealthBefore, HealthAfter, ResolvedDamage, AppliedDamage, ArmorDamage;
    public float HealthDamage=>AppliedDamage;
    public readonly bool Staggered;
    public readonly bool Lethal, BrokeArmor, BreakFinisher, DirectHit;
    public readonly CombatHitKind Kind;
    readonly DamageInfo snapshot;
    public bool Applied => Rejection==DamageRejection.None && AppliedDamage+ArmorDamage>0;
    public HitPresentation Presentation=>!Applied?HitPresentation.None:Lethal?HitPresentation.Kill:BrokeArmor?HitPresentation.ArmorBreak:ArmorDamage>0?HitPresentation.Armor:HitPresentation.Health;
    public float Overkill => UnityEngine.Mathf.Max(0,ResolvedDamage-AppliedDamage-ArmorDamage);
    public DamageResult(DamageRejection rejection, float before, float after, DamageInfo info, bool lethal=false)
    {
        Rejection=rejection;HealthBefore=before;HealthAfter=after;Lethal=lethal;
        ResolvedDamage=rejection==DamageRejection.None && info!=null?info.Amount:0;
        AppliedDamage=rejection==DamageRejection.None?UnityEngine.Mathf.Max(0,before-after):0;
        ArmorDamage=rejection==DamageRejection.None&&info!=null?info.ArmorDamage:0;
        Staggered=info!=null&&info.Staggered;
        snapshot=info?.Snapshot();
        BrokeArmor=ArmorDamage>0 && info!=null && info.BrokeArmor;
        BreakFinisher=AppliedDamage>0 && info!=null && info.BreakFinisher;
        DirectHit=AppliedDamage>0 && info!=null && info.DirectHit;
        Kind=info!=null?info.Kind:CombatHitKind.Generic;
    }
    public DamageInfo ToDamageInfo() => snapshot?.Snapshot();
}
