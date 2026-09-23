using System;
using System.Collections.Generic;
using UnityEngine;

public enum CombatHitKind { Generic, Rifle, HeavyRifle, Missile, Melee }

public class DamageInfo
{
    public GameObject SourceObject;
    public Vector3 SourcePosition;
    public Damageable Source;
    public float Amount;
    public float ArmorDamage;
    public bool Staggered;
    public float Impact = -1f;
    public CombatHitKind Kind;
    public float DirectHitMultiplier = -1f;
    public bool HeavyImpact;
    public bool DirectHit;
    public bool BrokeArmor, MeleeStrike, BreakFinisher;
    public bool HasContact, KineticRound;
    public bool FrontGuarded, FlankHit;
    public Vector3 ContactPoint,ContactNormal,ContactTangent;

    public DamageInfo(GameObject sourceObject, Vector3 sourcePosition, Damageable source, float amount)
    {
        SourceObject = sourceObject;
        SourcePosition = sourcePosition;
        Source = source;
        Amount = amount;
    }
    public DamageInfo Snapshot() => (DamageInfo)MemberwiseClone();
    public void ClearOutcome()
    { Amount=ArmorDamage=0;Staggered=DirectHit=BrokeArmor=BreakFinisher=FrontGuarded=FlankHit=false; }
}

public class Damageable : MonoBehaviour
{
    private static readonly List<Damageable> active = new List<Damageable>();
    public static IReadOnlyList<Damageable> Active => active;
    private Collider aimCollider;
    public Vector3 AimCenter
    {
        get
        {
            if (aimCollider == null || !aimCollider.enabled) aimCollider = GetComponent<CharacterController>() as Collider ?? GetComponent<Collider>();
            return aimCollider != null && aimCollider.enabled ? aimCollider.bounds.center : transform.position + Vector3.up * 0.8f;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetRegistry() { active.Clear(); }
    private void OnEnable() { if (!active.Contains(this)) active.Add(this); }
    private void OnDisable() { active.Remove(this); }
    public int team = 1;
    public float maxHealth = 50f;
    public bool destroyOnDeath = true;
    public float hitInvulnerabilityDuration;
    public float IncomingDamageScale { get; set; } = 1f;

    public float CurrentHealth { get; private set; }
    public bool IsDead { get; private set; }
    public DamageInfo LastHit { get; private set; }
    public DamageResult LastResult { get; private set; }
    public bool IsInvulnerable { get { return Time.time < invulnerableUntil; } }

    public event Action<Damageable, DamageInfo> OnDamaged;
    public event Action<Damageable, DamageResult> OnResolved;
    public event Action<Damageable> OnDied;

    private PlayerStats playerStats;
    private float invulnerableUntil;
    private bool resolvingDamage, deathNotified;

    private void Awake()
    {
        playerStats = GetComponent<PlayerStats>();
        CurrentHealth = maxHealth;
    }

    public void SetMaxHealth(float value, bool fillHealth)
    {
        maxHealth = Mathf.Max(1f, value);
        if (fillHealth)
        {
            CurrentHealth = maxHealth;
        }
        else
        {
            CurrentHealth = Mathf.Clamp(CurrentHealth, 0f, maxHealth);
        }
    }

    public void SetCurrentHealth(float value)
    {
        CurrentHealth = Mathf.Clamp(value, 0f, maxHealth);
    }

    public void RestoreLife(float maximum, float health)
    {
        maxHealth = Mathf.Max(1f, maximum);
        CurrentHealth = Mathf.Clamp(health, 1f, maxHealth);
        IsDead = false;
        deathNotified=false;LastHit=null;LastResult=default;
        invulnerableUntil = 0f;
        GetComponent<ImpactStability>()?.ResetState();
        GetComponent<ArmorHealth>()?.ResetState();
        if (playerStats != null) playerStats.SyncHealthFromDamageable(CurrentHealth);
    }

    public void TakeDamage(float amount, DamageInfo info) { ApplyDamage(amount,info); }

    public DamageResult ApplyDamage(float amount, DamageInfo info)
    {
        if(resolvingDamage)return new DamageResult(DamageRejection.Reentrant,CurrentHealth,CurrentHealth,null);
        if(info==null)info=new DamageInfo(null,transform.position,null,amount);
        info.ClearOutcome();
        var rejected=GameManager.Instance!=null&&!GameManager.Instance.IsCombatActive?DamageRejection.NotInCombat:
            IsDead?DamageRejection.Dead:IsInvulnerable?DamageRejection.Invulnerable:
            amount<=0 || float.IsNaN(amount) || float.IsInfinity(amount)?DamageRejection.InvalidAmount:DamageRejection.None;
        if(rejected!=DamageRejection.None)return new DamageResult(rejected,CurrentHealth,CurrentHealth,info);
        info.Amount = amount;
        float finalDamage = (playerStats != null ? playerStats.ModifyIncomingDamage(info) : amount) * Mathf.Max(0f, IncomingDamageScale);
        if(finalDamage<=0 || float.IsNaN(finalDamage) || float.IsInfinity(finalDamage))
        {info.ClearOutcome();return new DamageResult(DamageRejection.NoDamage,CurrentHealth,CurrentHealth,info);}
        var enemy = GetComponent<EnemyBase>();
        var armor=GetComponent<ArmorHealth>();
        if(armor!=null)finalDamage=armor.Absorb(finalDamage,info);
        info.Amount = finalDamage+info.ArmorDamage;
        float before=CurrentHealth;
        CurrentHealth = Mathf.Max(0f, CurrentHealth - finalDamage);
        bool lethal=CurrentHealth<=0;
        // Commit terminal state before any callback; one lethal impact can award only one death.
        if(lethal)IsDead=true;
        if(info.BrokeArmor && !lethal && enemy!=null){enemy.InterruptForStagger();info.Staggered=true;}
        else if(!lethal && enemy!=null)info.Staggered=enemy.ResolveHitReaction(info);
        LastHit=info.Snapshot();
        var result=new DamageResult(DamageRejection.None,before,CurrentHealth,info,lethal);
        LastResult=result;
        if(hitInvulnerabilityDuration>0)
            invulnerableUntil=Mathf.Max(invulnerableUntil,Time.time+hitInvulnerabilityDuration);
        if(playerStats!=null)playerStats.SyncHealthFromDamageable(CurrentHealth);
        resolvingDamage=true;
        try
        {
            CombatLabTelemetry.Hit(this,info,result.AppliedDamage);
            // Rules publish gameplay state first (Boss armor / interrupted attack), then presentation.
            OnResolved?.Invoke(this,result);
            OnDamaged?.Invoke(this,info);
        }
        finally
        {
            try { if(lethal && IsDead)PublishDeath(); }
            finally { resolvingDamage=false; }
        }
        return result;
    }

    public void SetInvulnerable(float duration)
    {
        invulnerableUntil = Mathf.Max(invulnerableUntil, Time.time + Mathf.Max(0f, duration));
    }

    public void Kill(DamageInfo info)
    {
        if (IsDead)
        {
            return;
        }

        IsDead = true;LastHit=info?.Snapshot();
        CurrentHealth = 0f;
        if (playerStats != null) playerStats.SyncHealthFromDamageable(0f);
        PublishDeath();
    }

    private void PublishDeath()
    {
        if(deathNotified)return;
        deathNotified=true;
        if (OnDied != null)
        {
            OnDied.Invoke(this);
        }

        if (destroyOnDeath)
        {
            Destroy(gameObject);
        }
    }
}
