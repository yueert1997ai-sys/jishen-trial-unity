using System;
using UnityEngine;

[DefaultExecutionOrder(130)]
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
    private NemesisDroneController activeSupport;
    private ValkyrBackCannon activeCannon;
    // Cooldown upgrades cannot label the same six occupied units "ready" before recall.
    public float SkillCooldownRemaining => Mathf.Max(0f, Mathf.Max(nextSkillTime - Time.time,
        Mathf.Max(activeSupport!=null&&activeSupport.Active?NemesisDroneController.Duration-activeSupport.Age:0f,
        activeCannon!=null&&activeCannon.Active?ValkyrBackCannon.Duration-activeCannon.Age:0f)));
    private float nextSkillTime;

    bool pendingPrimary,heldPrimary;int primaryGeneration;
    public void RequestPrimary(bool held)
    {pendingPrimary=true;heldPrimary=held;primaryGeneration=CombatRuntime.ActionGeneration;}
    void LateUpdate()
    {
        if(!pendingPrimary)return;pendingPrimary=false;
        if(primaryGeneration==CombatRuntime.ActionGeneration)TryFireBeam(heldPrimary);
    }
    private float nextBeamTime;
    private float nextMissileTime;
    private int primaryShots;
    private float primaryRecoveryUntil;
    public WeaponHandlingProfile Handling => WeaponHandling.For(playerController!=null&&playerController.Loadout!=null?playerController.Loadout.EffectiveWeapon:PrimaryWeapon.None);
    public float PrimaryRecoveryRemaining => Mathf.Max(0,primaryRecoveryUntil-Time.time);
    public float PrimaryCooldownRemaining => Mathf.Max(0,nextBeamTime-Time.time);
    public int PrimaryRoundsRemaining => Handling.Capacity==0?0:primaryRecoveryUntil>0&&Time.time>=primaryRecoveryUntil?Handling.Capacity:Mathf.Max(0,Handling.Capacity-primaryShots);
    private float temporaryFireRateMultiplier = 1f;
    private float temporaryFireRateTimer;
    private SalvageGear LoadoutWeapon => GameManager.Instance != null && GameManager.Instance.equipmentLoop != null ? GameManager.Instance.equipmentLoop.Weapon : null;
    private SalvageGear LoadoutPack => GameManager.Instance != null && GameManager.Instance.equipmentLoop != null ? GameManager.Instance.equipmentLoop.Backpack : null;

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
        pendingPrimary=false;
        nextBeamTime = nextMissileTime = nextSkillTime = 0f;
        primaryShots=0;primaryRecoveryUntil=0;
        temporaryFireRateMultiplier = 1f;
        temporaryFireRateTimer = 0f;
    }

    public void SetTemporaryFireRateBonus(float multiplier, float duration)
    {
        temporaryFireRateMultiplier = Mathf.Max(1f, multiplier);
        temporaryFireRateTimer = Mathf.Max(temporaryFireRateTimer, duration);
    }

    public void TryFireBeam(bool continuous=false)
    {
        if (!CanFire() || (playerController.Loadout != null && !playerController.Loadout.CanUseRifle)) return;
        if(Time.time<primaryRecoveryUntil)return;
        if(primaryRecoveryUntil>0){primaryRecoveryUntil=0;primaryShots=0;}
        float fireRate = GetFireRateMultiplier();
        if (Time.time < nextBeamTime)
        {
            return;
        }

        int level = GetWeaponLevel(EquipmentType.RightHandWeapon, 1);
        var gear = LoadoutWeapon;
        var primary = playerController.Loadout != null && playerController.Loadout.IsRifle ? playerController.Loadout.Equipped : null;
        float interval=(gear != null ? gear.interval : beamFireInterval) / fireRate;

        int shotCount = (level == 1 ? 1 : level == 2 ? 2 : 3) + (upgradeSystem != null ? upgradeSystem.BonusBeamProjectiles : 0);
        float baseDamage = level == 1 ? 18f : level == 2 ? 20f : 22f;
        int pierce = (level >= 3 ? 1 : 0) + (upgradeSystem != null ? upgradeSystem.BonusPierce : 0);
        float explosionRadius = upgradeSystem != null ? upgradeSystem.BeamExplosionRadius : 0f;
        if (gear != null)
        {
            shotCount = gear.projectiles * (1 + (upgradeSystem != null ? upgradeSystem.BonusBeamProjectiles : 0));
            baseDamage = gear.damage;
            pierce = gear.pierce + (upgradeSystem != null ? upgradeSystem.BonusPierce : 0);
        }
        if (primary != null)
        {
            interval=(primary.weapon==PrimaryWeapon.M14 ? CombatRules.Current.HeavyRifleInterval : primary.interval) / fireRate;
            shotCount = (LoadoutWeapon?.id=="missile_rack"&&playerController.Loadout.Selected==PrimaryWeapon.Collection?3:1) + (upgradeSystem != null ? upgradeSystem.BonusBeamProjectiles : 0);
            baseDamage = primary.damage;
            pierce = primary.pierce + (upgradeSystem != null ? upgradeSystem.BonusPierce : 0);
            explosionRadius = Mathf.Max(primary.blastRadius, explosionRadius);
            gear = null;
        }

        // Preserve cadence while held instead of adding another whole frame to every interval.
        // A fresh tap or a long interruption starts a fresh clock; never emit catch-up bursts.
        bool carryCadence=continuous&&nextBeamTime>0&&Time.time-nextBeamTime<Mathf.Min(interval,.1f);
        nextBeamTime=carryCadence?nextBeamTime+interval:Time.time+interval;
        var handling=Handling;
        // One trigger consumes one cycle unit, including split shots. Service never locks the blade or dash.
        if(handling.Capacity>0&&++primaryShots>=handling.Capacity)
        {
            primaryRecoveryUntil=Time.time+Mathf.Max(interval,handling.Recovery/fireRate);
            nextBeamTime=primaryRecoveryUntil;
        }

        GetComponentInChildren<LoadoutVisual>()?.SynchronizeForFire();
        var backMount=GetComponentInChildren<LoadoutVisual>()?.WeaponObject?.GetComponent<BackCannonMount>();
        if(backMount!=null)
        {
            foreach(var port in backMount.muzzles)ArsenalBeam.Fire(port.position,port.forward,damageable,baseDamage*GetDamageMultiplier()*.5f,40,.65f,.3f);
            BeamFired?.Invoke();GameAudio.Play(GameAudioCue.Beam,.5f,.84f);return;
        }
        for (int i = 0; i < shotCount; i++)
        {
            float angle = gear != null ? Mathf.Max(6f, gear.spread) : 6f;
            float spread = shotCount == 1 ? 0f : Mathf.Lerp(-angle, angle, i / (float)(shotCount - 1));
            Vector3 origin = GetMuzzlePosition() + transform.right * ((i - (shotCount - 1) * 0.5f) * 0.16f);
            // The offset muzzle must converge on the cursor target, not fire parallel to the torso.
            Vector3 aim = playerController != null && playerController.HasAimPoint
                ? playerController.AimPoint - origin
                : GetAimDirection();
            Vector3 direction = Quaternion.AngleAxis(spread, Vector3.up) * aim.normalized;
            direction=Quaternion.AngleAxis(spread,Vector3.up)*(muzzle!=null?muzzle.forward:PlanarCombat.AimDirection(origin,playerController.AimPoint,GetAimDirection()));
            float splitScale = upgradeSystem != null ? upgradeSystem.SplitShotMultiplier : 1f;
            CreateBeamProjectile(origin, direction, baseDamage * GetDamageMultiplier() * splitScale, pierce, explosionRadius, 1f/shotCount);
        }

        if (BeamFired != null)
        {
            BeamFired.Invoke();
        }

        if(primary!=null && primary.weapon==PrimaryWeapon.M7)
            GameAudio.Play(GameAudioCue.RifleShot,.82f,1f);
        else if (primary == null || (primary.weapon != PrimaryWeapon.Type08 && primary.weapon != PrimaryWeapon.Halbreaker))
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
            Vector3 origin = GetSkillMuzzlePosition() + transform.up * 0.5f + transform.right * ((i % 2 == 0 ? -1f : 1f) * 0.45f);
            CreateMissileProjectile(origin, direction, 18f * GetDamageMultiplier(), explosionRadius, target);
        }

        GameAudio.Play(GameAudioCue.Missile, 0.3f, UnityEngine.Random.Range(0.93f, 1.02f));
    }

    public bool TryFireSkill(Damageable target)
    {
        if (!CanFireSupport() || Time.time < nextSkillTime) return false;
        var pack = LoadoutPack;
        int count = Mathf.Max(1, pack != null ? pack.missiles : 4);
        var drones=GetComponentInChildren<NemesisDroneController>();
        if(drones!=null)
        {
            if(!drones.Deploy(18f*count*GetDamageMultiplier(),CombatLoopV2.MissileImpact*count))return false;
            activeSupport=drones;
            nextSkillTime=Time.time+(pack!=null?pack.skillCooldown:10f)*(upgradeSystem!=null?upgradeSystem.SupportCooldownMultiplier:1f);
            SkillFired?.Invoke();return true;
        }
        var cannon=GetComponentInChildren<ValkyrBackCannon>();
        if(cannon!=null)
        {
            if(!cannon.Deploy(target,18f*count*GetDamageMultiplier(),CombatLoopV2.MissileImpact*count))return false;
            activeCannon=cannon;
            nextSkillTime=Time.time+(pack!=null?pack.skillCooldown:10f)*(upgradeSystem!=null?upgradeSystem.SupportCooldownMultiplier:1f);
            SkillFired?.Invoke();return true;
        }
        if(target==null||target.IsDead)return false;
        nextSkillTime = Time.time + (pack != null ? pack.skillCooldown : 10f)*(upgradeSystem!=null?upgradeSystem.SupportCooldownMultiplier:1f);
        for (int i = 0; i < count; i++)
        {
            Vector3 origin = GetSkillMuzzlePosition() + Vector3.up * 0.2f + transform.right * (i % 2 == 0 ? -0.4f : 0.4f);
            Vector3 direction = Quaternion.AngleAxis(Mathf.Lerp(-24f, 24f, count==1?.5f:i / (float)(count - 1)), Vector3.up) * (target.AimCenter - origin).normalized;
            CreateMissileProjectile(origin, direction, 18f * GetDamageMultiplier(), 1.6f, target);
        }
        GameAudio.Play(GameAudioCue.Missile, 0.35f);
        SkillFired?.Invoke();
        return true;
    }

    private bool CanFireSupport()
    {
        return (damageable==null||!damageable.IsDead)
            && (GameManager.Instance==null||GameManager.Instance.IsCombatActive)
            && GameManager.Instance?.equipmentLoop?.Absorption?.Busy!=true;
    }
    private bool CanFire()
    {
        return (damageable == null || !damageable.IsDead)
            && (playerController == null || playerController.Stance == null || playerController.Stance.CanFire)
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
    private Vector3 GetSkillMuzzlePosition()
    {
        var rig=GetComponentInChildren<RiggedMechAnimator>();
        return rig!=null&&rig.muzzle!=null?rig.muzzle.position:GetMuzzlePosition();
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

    private void CreateBeamProjectile(Vector3 origin, Vector3 direction, float damage, int pierce, float explosionRadius, float impactShare=1f)
    {
        var nemesis=GetComponentInChildren<NemesisMotionRig>();
        if(playerController.Loadout.Selected==PrimaryWeapon.Collection&&LoadoutWeapon?.id=="missile_rack")
        {CreateMissileProjectile(origin,direction,damage,1.3f,FindNearestEnemy());return;}
        if(nemesis!=null&&playerController.Loadout!=null&&
            (playerController.Loadout.EffectiveWeapon==PrimaryWeapon.M7||playerController.Loadout.EffectiveWeapon==PrimaryWeapon.NemesisLauncher))
        {
            bool rocket=playerController.Loadout.EffectiveWeapon==PrimaryWeapon.NemesisLauncher;
            var entry=playerController.Loadout.Equipped;
            var packet=ProjectilePool.Spawn(false,rocket?"J01_Rocket":"J01_BeamRifle",origin,NemesisMotionRig.Amethyst,rocket?.19f:.095f);
            packet.Init(team,damageable,PlanarCombat.Direction(direction,GetAimDirection()),damage,rocket?entry.speed:72f,2.1f,explosionRadius,pierce);
            packet.SetHandling(Handling);
            packet.SetImpact((rocket?CombatLoopV2.HeavyRifleImpact:CombatLoopV2.RifleImpact)*impactShare,rocket?CombatHitKind.HeavyRifle:CombatHitKind.Rifle);
            BeamFxKit.MuzzleBlast(origin,packet.direction,NemesisMotionRig.Amethyst,rocket?.7f:.42f);
            return;
        }
        if(playerController.Loadout!=null && playerController.Loadout.EffectiveWeapon==PrimaryWeapon.M7)
        {
            var bullet=ProjectilePool.SpawnKinetic(origin);
            bullet.Init(team,damageable,PlanarCombat.Direction(direction,GetAimDirection()),damage,72f,.8f,0,pierce);
            bullet.SetHandling(Handling);
            bullet.SetImpact(CombatLoopV2.RifleImpact*impactShare,CombatHitKind.Rifle);
            var feedback=GetComponent<KineticRifleFeedback>()??gameObject.AddComponent<KineticRifleFeedback>();
            feedback.Shot(origin,bullet.direction,playerController.GetComponentInChildren<LoadoutVisual>()?.EjectionPort);
            BeamFxKit.MuzzleBlast(origin,bullet.direction,new Color(.12f,1f,.56f),.42f);
            return;
        }
        if (playerController.Loadout != null && playerController.Loadout.EffectiveWeapon == PrimaryWeapon.Halbreaker)
        {
            Vector3 aim = playerController.HasAimPoint ? playerController.AimPoint : origin + direction * HalbreakerBeam.MaximumRange;
            if (Vector3.Angle(direction, (aim-origin).normalized) > .1f) aim = origin + direction * HalbreakerBeam.MaximumRange;
            HalbreakerBeam.Fire(muzzle, origin, direction, aim, team, damageable, damage, pierce, explosionRadius);
            return;
        }
        if (playerController.Loadout != null && playerController.Loadout.EffectiveWeapon == PrimaryWeapon.Type08)
        {
            Vector3 aim = playerController.HasAimPoint ? playerController.AimPoint : origin + direction * MinovskyBeam.MaximumRange;
            // Preserve split-shot directions while each beam starts at the cannon's visible muzzle.
            if (Vector3.Angle(direction, (aim-origin).normalized) > .1f) aim = origin + direction * MinovskyBeam.MaximumRange;
            MinovskyBeam.Fire(muzzle, origin, direction, aim, team, damageable, damage, pierce, explosionRadius);
            return;
        }
        var primary = playerController.Loadout != null && playerController.Loadout.IsRifle ? playerController.Loadout.Equipped : null;
        Color color = nemesis!=null?MechEnergyPalette.Nemesis:primary != null ? (primary.weapon == PrimaryWeapon.M14 ? new Color(1,.73f,.35f) : new Color(.72f,.9f,1)) : LoadoutWeapon != null ? LoadoutWeapon.color : new Color(0.1f, 0.8f, 1f);
        var projectile = ProjectilePool.Spawn(false, "BeamProjectile", origin, color);
        projectile.Init(team, damageable, direction, damage, primary != null ? primary.speed : 30f, 2.1f, explosionRadius, pierce);
        projectile.SetHandling(Handling);
        {
            bool heavy=primary!=null && primary.weapon==PrimaryWeapon.M14;
            projectile.SetImpact((heavy?CombatLoopV2.HeavyRifleImpact:CombatLoopV2.RifleImpact)*impactShare,
                heavy?CombatHitKind.HeavyRifle:CombatHitKind.Rifle,heavy?CombatLoopV2.HeavyRiflePunish:1.35f);
        }
        ProjectileVisuals.SpawnMuzzleFlash(origin, color, 0.26f);
    }

    private void CreateMissileProjectile(Vector3 origin, Vector3 direction, float damage, float explosionRadius, Damageable target)
    {
        Color tint=GetComponentInChildren<NemesisMotionRig>()!=null?NemesisMotionRig.Amethyst:new Color(1f, 0.65f, 0.12f);
        var missile = (MissileProjectile)ProjectilePool.Spawn(true, "MissileProjectile", origin, tint, 0.22f);
        missile.target = target;
        missile.Init(team, damageable, direction, damage, 18f, 4.5f, explosionRadius, 0);
        missile.SetImpact(CombatLoopV2.MissileImpact,CombatHitKind.Missile);
        ProjectileVisuals.SpawnMuzzleFlash(origin, tint, 0.3f);
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
