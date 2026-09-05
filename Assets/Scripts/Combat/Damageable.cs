using System;
using System.Collections.Generic;
using UnityEngine;

public class DamageInfo
{
    public GameObject SourceObject;
    public Vector3 SourcePosition;
    public Damageable Source;
    public float Amount;

    public DamageInfo(GameObject sourceObject, Vector3 sourcePosition, Damageable source, float amount)
    {
        SourceObject = sourceObject;
        SourcePosition = sourcePosition;
        Source = source;
        Amount = amount;
    }
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

    public float CurrentHealth { get; private set; }
    public bool IsDead { get; private set; }
    public bool IsInvulnerable { get { return Time.time < invulnerableUntil; } }

    public event Action<Damageable, DamageInfo> OnDamaged;
    public event Action<Damageable> OnDied;

    private PlayerStats playerStats;
    private float invulnerableUntil;

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
        invulnerableUntil = 0f;
        if (playerStats != null) playerStats.SyncHealthFromDamageable(CurrentHealth);
    }

    public void TakeDamage(float amount, DamageInfo info)
    {
        if (GameManager.Instance != null && !GameManager.Instance.IsCombatActive) return;
        if (IsDead || IsInvulnerable || amount <= 0f)
        {
            return;
        }

        if (info == null)
        {
            info = new DamageInfo(null, transform.position, null, amount);
        }

        info.Amount = amount;
        float finalDamage = playerStats != null ? playerStats.ModifyIncomingDamage(info) : amount;
        info.Amount = finalDamage;
        CurrentHealth = Mathf.Max(0f, CurrentHealth - finalDamage);

        if (hitInvulnerabilityDuration > 0f)
        {
            invulnerableUntil = Mathf.Max(invulnerableUntil, Time.time + hitInvulnerabilityDuration);
        }

        if (playerStats != null)
        {
            playerStats.SyncHealthFromDamageable(CurrentHealth);
        }

        if (OnDamaged != null)
        {
            OnDamaged.Invoke(this, info);
        }

        if (CurrentHealth <= 0f)
        {
            Kill(info);
        }
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

        IsDead = true;
        CurrentHealth = 0f;
        if (playerStats != null) playerStats.SyncHealthFromDamageable(0f);
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
