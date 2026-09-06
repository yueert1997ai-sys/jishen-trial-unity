using UnityEngine;

public enum WeaponStance { Sword, Sheathing, Ranged, Drawing }

// One owner arbitrates input, fire permission, presentation and contact damage.
[DisallowMultipleComponent]
public sealed class PlayerWeaponStance : MonoBehaviour
{
    public const float TransitionDuration = .16f;
    public WeaponStance State { get; private set; } = WeaponStance.Sword;
    public float StowBlend { get; private set; }
    public bool CanMelee => State == WeaponStance.Sword;
    public bool CanFire => State == WeaponStance.Ranged && !player.Melee.IsAttacking;
    private PlayerController player;
    private bool wasFire, requireFireRelease, pendingSlash, pendingRanged, pendingShot;
    private Damageable pendingSkill;

    private void Awake() { player = GetComponent<PlayerController>(); }
    public void Process(PlayerCommand command, Damageable target, float dt)
    {
        bool alreadyRanged=State==WeaponStance.Ranged;
        bool pressed = command.Fire && !wasFire;
        wasFire = command.Fire;
        if (!command.Fire) requireFireRelease = false;
        if (command.Melee)
        {
            requireFireRelease = command.Fire;
            pendingRanged = pendingShot = false; pendingSkill = null;
            if (player.Melee.IsAttacking)
                player.Melee.TryAttack();
            else
            {
                pendingSlash = true;
                if (State != WeaponStance.Sword) State = WeaponStance.Drawing;
            }
        }
        else if ((pressed && !requireFireRelease) || command.Skill)
        {
            // There is only one queued ranged request. A skill replaces the beam request.
            pendingRanged = true;
            player.Melee.ClearComboQueue();
            if (command.Skill) { pendingSkill = target; pendingShot = false; }
            else if (pendingSkill == null) pendingShot = command.HasAim;
            pendingSlash = false;
        }
        if (pendingRanged && player.Melee.CanSheathe)
        {
            player.Melee.CancelAttack();
            if (State != WeaponStance.Ranged) State = WeaponStance.Sheathing;
            pendingRanged = false;
        }
        if (State == WeaponStance.Sheathing)
        {
            StowBlend = Mathf.MoveTowards(StowBlend, 1, dt / TransitionDuration);
            if (StowBlend >= 1) State = WeaponStance.Ranged;
        }
        else if (State == WeaponStance.Drawing)
        {
            StowBlend = Mathf.MoveTowards(StowBlend, 0, dt / TransitionDuration);
            if (StowBlend <= 0) State = WeaponStance.Sword;
        }
        if (pendingSlash && CanMelee && !player.IsDashing && player.Melee.CooldownRemaining <= 0)
        { pendingSlash = false; player.Melee.TryAttack(); }
        if (!CanFire || !alreadyRanged) return; // The completed back pose is applied before firing next frame.
        if (pendingSkill != null)
        { player.weaponController.TryFireSkill(pendingSkill); pendingSkill = null; }
        if (pendingShot || (command.Fire && command.HasAim && !requireFireRelease))
        { player.weaponController.TryFireBeam(); pendingShot = false; }
    }
    public void ClearRequests()
    { wasFire = requireFireRelease = pendingSlash = pendingRanged = pendingShot = false; pendingSkill = null; if(player!=null&&player.Melee!=null)player.Melee.ClearComboQueue(); }
    public void ResetStance()
    { ClearRequests(); State = WeaponStance.Sword; StowBlend = 0; }
}
