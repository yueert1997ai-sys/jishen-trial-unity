using System;
using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    public struct Snapshot
    {
        public float hp, maxHp, energy, maxEnergy, speed, dash, cooldown, damage, rate;
        public int shield;
    }

    public Snapshot Capture() => new Snapshot { hp = CurrentHp, maxHp = MaxHp, energy = CurrentEnergy,
        maxEnergy = MaxEnergy, speed = MoveSpeed, dash = DashDistance, cooldown = DashCooldown,
        damage = DamageMultiplier, rate = FireRateMultiplier, shield = ShieldLevel };

    public void Restore(Snapshot state)
    {
        MaxHp = state.maxHp;
        CurrentHp = Mathf.Clamp(state.hp, 1f, MaxHp);
        MaxEnergy = state.maxEnergy;
        CurrentEnergy = state.energy;
        MoveSpeed = state.speed;
        DashDistance = state.dash;
        DashCooldown = state.cooldown;
        DamageMultiplier = state.damage;
        FireRateMultiplier = state.rate;
        ShieldLevel = state.shield;
        damageable.RestoreLife(MaxHp, CurrentHp);
        RaiseChanged();
    }
    public float baseMaxHp = 180f;
    public float baseEnergy = 100f;
    public float energyRegenPerSecond = 34f;
    public float baseMoveSpeed = 7.4f;
    public float baseDashDistance = 5f;
    public float baseDashCooldown = 1.25f;
    public float incomingDamageMultiplier = 0.85f;

    public float CurrentHp { get; private set; }
    public float MaxHp { get; private set; }
    public float CurrentEnergy { get; private set; }
    public float MaxEnergy { get; private set; }
    public float MoveSpeed { get; private set; }
    public float DashDistance { get; private set; }
    public float DashCooldown { get; private set; }
    public float DamageMultiplier { get; private set; }
    public float FireRateMultiplier { get; private set; }
    public int ShieldLevel { get; private set; }

    public event Action OnStatsChanged;

    private Damageable damageable;
    private float runHealthBonus, runDashBonus, runCooldownBonus;
    private float packDashBonus, packCooldownBonus, packSpeedBonus;
    private PlayerController controller;

    private void Awake()
    {
        damageable = GetComponent<Damageable>();
        controller = GetComponent<PlayerController>();
        ResetStats();
    }

    private void Update()
    {
        if (controller == null || (!controller.IsBoosting && !controller.IsDashing))
            CurrentEnergy = Mathf.Min(MaxEnergy, CurrentEnergy + energyRegenPerSecond * Time.deltaTime);
        RaiseChanged();
    }

    public void ResetStats()
    {
        runHealthBonus = runDashBonus = runCooldownBonus = 0f;
        packDashBonus = packCooldownBonus = packSpeedBonus = 0f;
        MaxHp = baseMaxHp;
        MaxEnergy = baseEnergy;
        MoveSpeed = baseMoveSpeed;
        DashDistance = baseDashDistance;
        DashCooldown = baseDashCooldown;
        DamageMultiplier = 1f;
        FireRateMultiplier = 1f;
        ShieldLevel = 0;
        CurrentHp = MaxHp;
        CurrentEnergy = MaxEnergy;

        if (damageable != null)
        {
            damageable.SetMaxHealth(MaxHp, true);
        }

        RaiseChanged();
    }

    public void ApplyEquipmentBonuses(float damageBonus, float fireRateBonus, float hpBonus, float energyBonus, float dashBonus, int shieldLevel)
    {
        float previousMaxHp = MaxHp <= 0f ? baseMaxHp : MaxHp;
        float previousMaxEnergy = MaxEnergy <= 0f ? baseEnergy : MaxEnergy;

        MaxHp = baseMaxHp + hpBonus;
        MaxEnergy = baseEnergy + energyBonus;
        MoveSpeed = baseMoveSpeed + dashBonus * 0.15f;
        DashDistance = baseDashDistance + dashBonus;
        DashCooldown = Mathf.Max(0.35f, baseDashCooldown - dashBonus * 0.08f);
        DamageMultiplier = 1f + damageBonus * 0.01f;
        FireRateMultiplier = 1f + fireRateBonus * 0.01f;
        ShieldLevel = shieldLevel;

        CurrentHp = Mathf.Clamp(CurrentHp + (MaxHp - previousMaxHp), 1f, MaxHp);
        CurrentEnergy = Mathf.Clamp(CurrentEnergy + (MaxEnergy - previousMaxEnergy), 0f, MaxEnergy);

        if (damageable != null)
        {
            damageable.SetMaxHealth(MaxHp, false);
            damageable.SetCurrentHealth(CurrentHp);
        }

        RaiseChanged();
    }

    public void AddMaxHealth(float amount, bool healAddedAmount)
    {
        float bonus = Mathf.Max(0f, amount);
        runHealthBonus += bonus;
        MaxHp += bonus;
        CurrentHp = Mathf.Clamp(CurrentHp + (healAddedAmount ? bonus : 0f), 1f, MaxHp);
        if (damageable != null)
        {
            damageable.SetMaxHealth(MaxHp, false);
            damageable.SetCurrentHealth(CurrentHp);
        }

        RaiseChanged();
    }

    public void AddDashBonus(float distanceBonus, float cooldownReduction)
    {
        runDashBonus += Mathf.Max(0f, distanceBonus);
        runCooldownBonus += Mathf.Max(0f, cooldownReduction);
        DashDistance += Mathf.Max(0f, distanceBonus);
        DashCooldown = Mathf.Max(0.35f, DashCooldown - Mathf.Max(0f, cooldownReduction));
        RaiseChanged();
    }

    public void SetBackpackBonuses(float distance, float cooldown, float speed)
    {
        DashDistance += distance - packDashBonus;
        MoveSpeed += speed - packSpeedBonus;
        packDashBonus = distance;
        packCooldownBonus = cooldown;
        packSpeedBonus = speed;
        DashCooldown = Mathf.Max(.35f, baseDashCooldown - runCooldownBonus - packCooldownBonus);
        RaiseChanged();
    }

    public void ClearRunUpgradeBonuses()
    {
        MaxHp = Mathf.Max(baseMaxHp, MaxHp - runHealthBonus);
        CurrentHp = Mathf.Clamp(CurrentHp, 0, MaxHp);
        DashDistance -= runDashBonus;
        runHealthBonus = runDashBonus = runCooldownBonus = 0f;
        DashCooldown = Mathf.Max(.35f, baseDashCooldown - packCooldownBonus);
        if (damageable != null)
        {
            damageable.SetMaxHealth(MaxHp, false);
            damageable.SetCurrentHealth(CurrentHp);
        }
        RaiseChanged();
    }

    public bool TrySpendEnergy(float amount)
    {
        if (CurrentEnergy < amount)
        {
            return false;
        }

        CurrentEnergy -= amount;
        RaiseChanged();
        return true;
    }

    public void Heal(float amount)
    {
        CurrentHp = Mathf.Min(MaxHp, CurrentHp + amount);
        if (damageable != null)
        {
            damageable.SetCurrentHealth(CurrentHp);
        }

        RaiseChanged();
    }

    public float ModifyIncomingDamage(DamageInfo info)
    {
        float finalDamage = info.Amount * Mathf.Clamp(incomingDamageMultiplier, 0.1f, 1f);
        if (GameManager.Instance != null)
        {
            finalDamage *= GameManager.Instance.EnemyDamageMultiplier;
        }

        if (ShieldLevel <= 0)
        {
            return finalDamage;
        }

        Vector3 toSource = info.SourcePosition - transform.position;
        toSource.y = 0f;
        if (toSource.sqrMagnitude < 0.01f)
        {
            return finalDamage;
        }

        float frontDot = Vector3.Dot(transform.forward, toSource.normalized);
        if (frontDot < 0.15f)
        {
            return finalDamage;
        }

        float reduction = ShieldLevel == 1 ? 0.35f : ShieldLevel == 2 ? 0.55f : 0.7f;
        finalDamage *= 1f - reduction;

        if (ShieldLevel >= 3 && info.Source != null)
        {
            info.Source.TakeDamage(info.Amount * 0.2f, new DamageInfo(gameObject, transform.position, null, info.Amount * 0.2f));
        }

        return finalDamage;
    }

    public void SyncHealthFromDamageable(float health)
    {
        CurrentHp = Mathf.Clamp(health, 0f, MaxHp);
        RaiseChanged();
    }

    private void RaiseChanged()
    {
        if (OnStatsChanged != null)
        {
            OnStatsChanged.Invoke();
        }
    }
}
