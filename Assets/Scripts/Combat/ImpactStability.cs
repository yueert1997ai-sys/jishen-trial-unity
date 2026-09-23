using System;
using UnityEngine;

// Serialized V2/V3 compatibility only. V4 damage never passes through this component.
// Old scene references remain readable; armor lives in ArmorHealth, Boss openings in BossController.
[DisallowMultipleComponent]
public sealed class ImpactStability : MonoBehaviour
{
    public const float BreakDuration=1.35f,RecoveryDelay=1.1f,RecoveryRate=42f;
    public float Capacity=>0;
    public float Pressure=>0;
    public float Ratio=>0;
    public bool IsBroken=>false;
    public float WindowRemaining=>0;
    public int BreakCount=>0;
    public bool LastHitWasFinisher=>false;
    public float WindowDuration=>0;
    public bool IsRecovering=>false;
#pragma warning disable 0067
    public event Action<DamageInfo> BreakStarted;
    public event Action BreakEnded;
#pragma warning restore 0067
    public void Configure(bool elite){}
    public void ConfigureBoss(){}
    public void ResetState(){}
    public float Resolve(float damage,DamageInfo info)=>damage;
    public void PublishBreak(DamageInfo info){}
}
