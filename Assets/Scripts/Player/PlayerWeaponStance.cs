using UnityEngine;

public enum WeaponStance { Sword, Sheathing, Ranged, Drawing }

// The only owner of weapon requests. Executors consume permissions and one shared action clock.
[DisallowMultipleComponent]
public sealed class PlayerWeaponStance : MonoBehaviour
{
    public const float TransitionDuration=.16f;
    public WeaponStance State {get;private set;}=WeaponStance.Ranged;
    public float StowBlend {get;private set;}=1;
    PlayerController player;
    bool wasFire;
    bool Absorbing=>GameManager.Instance?.equipmentLoop?.Absorption?.Busy==true;
    bool Allowed=>!Absorbing && GameManager.Instance?.CanPlayerControl!=false && !player.GetComponent<Damageable>().IsDead;
    public bool CanMelee=>Allowed && !player.IsDashing && State==WeaponStance.Sword && player.Loadout.CanUseSword;
    public bool CanFire=>Allowed && State==WeaponStance.Ranged && !player.IsDashing && !player.Melee.IsAttacking;
    void Awake(){player=GetComponent<PlayerController>();}

    public void Process(PlayerCommand command,Damageable target,float dt)
    {
        if(!Allowed || !player.Loadout.CanDeploy){ClearRequests();return;}
        bool pressed=command.Fire&&!wasFire;wasFire=command.Fire;
        var queue=player.Actions;
        if(command.Skill)queue.Enqueue(BufferedCombatAction.Support,Time.time,CombatRules.Current.SupportBuffer);
        if(!player.IsDashing && queue.Pending(BufferedCombatAction.Support,Time.time)
            && player.weaponController.TryFireSkill(target))queue.Cancel(BufferedCombatAction.Support);
        if(command.Melee)
        {
            queue.Cancel(BufferedCombatAction.Primary);
            if(player.Melee.IsAttacking)player.Melee.TryAttack();
            else queue.Enqueue(BufferedCombatAction.Slash,Time.time,CombatRules.Current.SlashBuffer,
                command.HasAim?command.AimPoint:player.AimPoint);
        }
        else if(pressed && command.HasAim && !(player.IsDashing&&player.DashStartedAt==Time.time))
            queue.Enqueue(BufferedCombatAction.Primary,Time.time,CombatRules.Current.SlashBuffer,command.AimPoint);
        bool slash=queue.TryPeek(BufferedCombatAction.Slash,Time.time,out var slashAim);
        bool shot=queue.TryPeek(BufferedCombatAction.Primary,Time.time,out var shotAim);
        // Stow/draw may prepare during travel; only the dash owns movement and no attack can emit until its exit.
        if(slash && !player.Melee.IsAttacking)
        {
            if(State!=WeaponStance.Sword && State!=WeaponStance.Drawing)
            {State=WeaponStance.Drawing;GameAudio.Play(GameAudioCue.WeaponDraw,.34f);}
        }
        else if(!command.Melee && !slash && (shot || command.Fire) && player.Melee.CanSheathe && !player.Melee.NextQueued)
        {
            player.Melee.CancelAttack();
            if(State!=WeaponStance.Ranged && State!=WeaponStance.Sheathing)
            {State=WeaponStance.Sheathing;GameAudio.Play(GameAudioCue.WeaponStow,.30f);}
        }
        if(State==WeaponStance.Sheathing)
        {StowBlend=Mathf.MoveTowards(StowBlend,1,dt/CombatRules.Current.StowSeconds);if(StowBlend>=1)State=WeaponStance.Ranged;}
        if(State==WeaponStance.Drawing)
        {StowBlend=Mathf.MoveTowards(StowBlend,0,dt/CombatRules.Current.DrawSeconds);if(StowBlend<=0)State=WeaponStance.Sword;}
        if(slash && CanMelee && player.Melee.CooldownRemaining<=0)
        {
            player.AimAt(slashAim,0,720);
            if(player.Melee.TryAttack())queue.Cancel(BufferedCombatAction.Slash);
        }
        if(!command.Melee && !slash && CanFire && (shot || command.Fire&&command.HasAim))
        {
            if(shot && !command.Fire)player.AimAt(shotAim);
            player.weaponController.RequestPrimary(command.Fire&&!pressed&&!shot);
            queue.Cancel(BufferedCombatAction.Primary);
        }
    }
    public void ClearRequests()
    {
        wasFire=false;
        if(player==null)return;
        player.Actions.Cancel(BufferedCombatAction.Slash);player.Actions.Cancel(BufferedCombatAction.Primary);
        player.Actions.Cancel(BufferedCombatAction.Support);player.Melee?.ClearComboQueue();
    }
    public void BeginAbsorptionPose(){ClearRequests();State=WeaponStance.Sheathing;StowBlend=1;}
    public void FinishAbsorptionPose(){ClearRequests();State=WeaponStance.Ranged;StowBlend=1;}
    public void ResetStance(){ClearRequests();State=WeaponStance.Ranged;StowBlend=1;}
}
