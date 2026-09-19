using System;
using UnityEngine;

public class WeaponController : MonoBehaviour
{
    public Transform muzzle;
    public EquipmentManager equipmentManager;
    public PlayerStats playerStats;
    public PlayerController playerController;
    public Damageable damageable;
    public RunUpgradeSystem upgradeSystem;
    public int team = 0;

    public float beamFireInterval = 0.18f;
    public float missileFireInterval = 1.2f;
    public event Action BeamFired;
    public event Action SkillFired;
    public float SkillCooldownRemaining => Mathf.Max(0f, nextSkillTime - Time.time);
    private float nextSkillTime;

    private float nextBeamTime;
    private float nextMissileTime;
    private float temporaryFireRateMultiplier = 1f;
    private float temporaryFireRateTimer;

    private void Awake()
    {
        if (equipmentManager == null)
        {
            equipmentManager = GetComponent<EquipmentManager>();
        }

        if (playerStats == null)
        {
            playerStats = GetComponent<PlayerStats>();
        }

        if (playerController == null)
        {
            playerController = GetComponent<PlayerController>();
        }

        if (damageable == null)
        {
            damageable = GetComponent<Damageable>();
        }

        if (upgradeSystem == null && GameManager.Instance != null)
        {
            upgradeSystem = GameManager.Instance.upgradeSystem;
        }
    }

    private void Update()
    {
        if (temporaryFireRateTimer > 0f)
        {
            temporaryFireRateTimer -= Time.deltaTime;
            if (temporaryFireRateTimer <= 0f)
            {
                temporaryFireRateMultiplier = 1f;
            }
        }

    }

    public void RefreshEquipment()
    {
    }

    public void ResetCooldowns()
    {
        nextBeamTime = nextMissileTime = nextSkillTime = 0f;
        temporaryFireRateMultiplier = 1f;
        temporaryFireRateTimer = 0f;
    }

    public void SetTemporaryFireRateBonus(float multiplier, float duration)
    {
        temporaryFireRateMultiplier = Mathf.Max(1f, multiplier);
        temporaryFireRateTimer = Mathf.Max(temporaryFireRateTimer, duration);
    }

    public void TryFireBeam()
    {
        if (!CanFire() || playerController == null || playerController.Loadout == null || !playerController.Loadout.IsRifle) return;
        var profile = playerController.Loadout.Equipped;
        if (profile == null) return;
        float fireRate = GetFireRateMultiplier();
        if (Time.time < nextBeamTime)
        {
            return;
        }

        int level = GetWeaponLevel(EquipmentType.RightHandWeapon, 1);
        nextBeamTime = Time.time + profile.interval / fireRate;

        int shotCount = (level == 1 ? 1 : level == 2 ? 2 : 3) + (upgradeSystem != null ? upgradeSystem.BonusBeamProjectiles : 0);
        float baseDamage = profile.damage * (1f + (level - 1) * .1f);
        int pierce = profile.pierce + (level >= 3 ? 1 : 0) + (upgradeSystem != null ? upgradeSystem.BonusPierce : 0);
        float explosionRadius = Mathf.Max(profile.blastRadius, upgradeSystem != null ? upgradeSystem.BeamExplosionRadius : 0f);

        for (int i = 0; i < shotCount; i++)
        {
            float spread = shotCount == 1 ? 0f : Mathf.Lerp(-6f, 6f, shotCount == 1 ? 0f : i / (float)(shotCount - 1));
            Vector3 origin = GetMuzzlePosition() + transform.right * ((i - (shotCount - 1) * 0.5f) * 0.16f);
            // The offset muzzle must converge on the cursor target, not fire parallel to the torso.
            Vector3 aim = playerController != null && playerController.HasAimPoint
                ? playerController.AimPoint - origin
                : GetAimDirection();
            Vector3 direction = Quaternion.AngleAxis(spread, Vector3.up) * aim.normalized;
            float splitScale = upgradeSystem != null ? upgradeSystem.SplitShotMultiplier : 1f;
            CreateBeamProjectile(origin, direction, baseDamage * GetDamageMultiplier() * splitScale, pierce, explosionRadius);
        }

        if (BeamFired != null)
        {
            BeamFired.Invoke();
        }

        if (playerController.Loadout.Selected != PrimaryWeapon.Type08)
            GameAudio.Play(GameAudioCue.Beam, 0.2f, UnityEngine.Random.Range(0.96f, 1.04f));
    }

    public void TryFireMissiles()
    {
        if (!CanFire()) return;
        int missileLevel = equipmentManager != null ? equipmentManager.GetHighestShoulderMissileLevel() : 0;
        if (missileLevel <= 0 || Time.time < nextMissileTime)
        {
            return;
        }

        nextMissileTime = Time.time + missileFireInterval / GetFireRateMultiplier();

        int missileCount = missileLevel == 1 ? 2 : missileLevel == 2 ? 4 : 6;
        float explosionRadius = missileLevel == 1 ? 1.5f : missileLevel == 2 ? 2.1f : 2.8f;
        Damageable target = FindNearestEnemy();

        for (int i = 0; i < missileCount; i++)
        {
            float spread = Mathf.Lerp(-35f, 35f, missileCount == 1 ? 0f : i / (float)(missileCount - 1));
            Vector3 direction = Quaternion.AngleAxis(spread, Vector3.up) * GetAimDirection();
            Vector3 origin = GetMuzzlePosition() + transform.up * 0.5f + transform.right * ((i % 2 == 0 ? -1f : 1f) * 0.45f);
            CreateMissileProjectile(origin, direction, 18f * GetDamageMultiplier(), explosionRadius, target);
        }

        GameAudio.Play(GameAudioCue.Missile, 0.3f, UnityEngine.Random.Range(0.93f, 1.02f));
    }

    public bool TryFireSkill(Damageable target)
    {
        if (!CanFire() || Time.time < nextSkillTime || target == null || target.IsDead) return false;
        nextSkillTime = Time.time + 10f;
        for (int i = 0; i < 4; i++)
        {
            Vector3 origin = GetMuzzlePosition() + Vector3.up * 0.2f + transform.right * (i % 2 == 0 ? -0.4f : 0.4f);
            Vector3 direction = Quaternion.AngleAxis(Mathf.Lerp(-24f, 24f, i / 3f), Vector3.up) * (target.AimCenter - origin).normalized;
            CreateMissileProjectile(origin, direction, 18f * GetDamageMultiplier(), 1.6f, target);
        }
        GameAudio.Play(GameAudioCue.Missile, 0.35f);
        SkillFired?.Invoke();
        return true;
    }

    private bool CanFire()
    {
        return (damageable == null || !damageable.IsDead)
            && (playerController == null || playerController.Melee == null || !playerController.Melee.IsAttacking)
            && (GameManager.Instance == null || GameManager.Instance.IsCombatActive);
    }

    private Vector3 GetAimDirection()
    {
        if (playerController != null && playerController.AimDirection.sqrMagnitude > 0.01f)
        {
            return playerController.AimDirection.normalized;
        }

        Vector3 forward = transform.forward;
        forward.y = 0f;
        return forward.sqrMagnitude > 0.01f ? forward.normalized : Vector3.forward;
    }

    private Vector3 GetMuzzlePosition()
    {
        if (muzzle != null)
        {
            return muzzle.position;
        }

        return transform.position + Vector3.up * 1.1f + GetAimDirection() * 1f;
    }

    private int GetWeaponLevel(EquipmentType equipmentType, int fallback)
    {
        if (equipmentManager == null)
        {
            return fallback;
        }

        int level = equipmentManager.GetLevel(equipmentType);
        return level > 0 ? level : fallback;
    }

    private float GetDamageMultiplier()
    {
        float statMultiplier = playerStats != null ? playerStats.DamageMultiplier : 1f;
        float upgradeMultiplier = upgradeSystem != null ? upgradeSystem.DamageMultiplier : 1f;
        return statMultiplier * upgradeMultiplier;
    }

    private float GetFireRateMultiplier()
    {
        float statMultiplier = playerStats != null ? playerStats.FireRateMultiplier : 1f;
        float upgradeMultiplier = upgradeSystem != null ? upgradeSystem.FireRateMultiplier : 1f;
        return Mathf.Max(0.1f, statMultiplier * upgradeMultiplier * temporaryFireRateMultiplier);
    }

    private void CreateBeamProjectile(Vector3 origin, Vector3 direction, float damage, int pierce, float explosionRadius)
    {
        if (playerController.Loadout.Selected == PrimaryWeapon.Type08)
        {
            Vector3 aim = playerController.HasAimPoint ? playerController.AimPoint : origin + direction * MinovskyBeam.MaximumRange;
            // A spread upgrade retains separate beam directions instead of collapsing every shot onto the same point.
            if (Vector3.Angle(direction, (aim-origin).normalized) > .1f) aim=origin+direction*MinovskyBeam.MaximumRange;
            MinovskyBeam.Fire(muzzle, origin, direction, aim, team, damageable, damage, pierce, explosionRadius);
            return;
        }
        bool m14 = playerController.Loadout.Selected == PrimaryWeapon.M14;
        Color color = m14 ? new Color(1f,.73f,.35f) : new Color(.72f,.9f,1f);
        var projectile = ProjectilePool.Spawn(false, "BeamProjectile", origin, color);
        projectile.Init(team, damageable, direction, damage, playerController.Loadout.Equipped.speed, 2.1f, explosionRadius, pierce);
        ProjectileVisuals.SpawnMuzzleFlash(origin, color, .26f);
    }

    private void CreateMissileProjectile(Vector3 origin, Vector3 direction, float damage, float explosionRadius, Damageable target)
    {
        var missile = (MissileProjectile)ProjectilePool.Spawn(true, "MissileProjectile", origin, new Color(1f, 0.65f, 0.12f), 0.22f);
        missile.target = target;
        missile.Init(team, damageable, direction, damage, 18f, 4.5f, explosionRadius, 0);
        ProjectileVisuals.SpawnMuzzleFlash(origin, new Color(1f, 0.7f, 0.2f), 0.3f);
    }

    private Damageable FindNearestEnemy()
    {
        var candidates = Damageable.Active;
        Damageable best = null;
        float bestDistance = 999999f;
        for (int i = 0; i < candidates.Count; i++)
        {
            Damageable candidate = candidates[i];
            if (candidate == null || candidate.team == team || candidate.IsDead)
            {
                continue;
            }

            float distance = (candidate.transform.position - transform.position).sqrMagnitude;
            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = candidate;
            }
        }

        return best;
    }
}
