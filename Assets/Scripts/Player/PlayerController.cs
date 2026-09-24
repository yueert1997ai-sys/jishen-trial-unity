using System;
using UnityEngine;

[DisallowMultipleComponent]
public class PlayerController : MonoBehaviour
{
    public PlayerStats stats;
    public WeaponController weaponController;
    public float acceleration = 90f;
    public float braking = 125f;
    public float dashDuration = 0.16f;
    public float DashStartedAt {get;private set;}=-10;
    // Retained for serialized compatibility only. All live beam input is manual.
    [HideInInspector] public bool automaticFire;
    public PlayerMeleeController Melee { get; private set; }
    public PlayerWeaponStance Stance { get; private set; }
    public PlayerLoadout Loadout { get; private set; }
    public CombatActionQueue Actions { get; } = new CombatActionQueue();

    public PlayerInputRouter InputRouter { get; private set; }
    public AutoAimController AutoAim { get; private set; }
    public CharacterController Motor { get; private set; }
    public Vector3 AimDirection { get; private set; }
    public Vector3 AimPoint { get; private set; }
    public bool HasAimPoint { get; private set; }
    public Vector3 MoveDirection { get; private set; }
    public Vector3 Velocity { get; private set; }
    public bool IsDashing => dashRemaining > 0f;
    public float DashTimeRemaining => dashRemaining;
    public bool DashAttackReady => !IsDashing && Time.time<=dashAttackUntil;
    float dashAttackUntil=-10;
    public void ConsumeDashAttack(){dashAttackUntil=-10;}
    public bool IsBoosting { get; private set; }
    public float DashCooldownRemaining => Mathf.Max(0f, nextDashTime - Time.time);
    public bool IsDashReady => DashCooldownRemaining <= 0f;
    public event Action<Vector3> Dashed;

    private Damageable damageable;
    private Vector3 planarVelocity;
    private Vector3 dashVelocity;
    private float dashRemaining;
    private float dashTotalDuration;
    private float dashExitFraction;
    private float nextDashTime;
    private float boostHeldTime;
    private bool boostExhausted;
    private float ThrusterCost=>weaponController?.upgradeSystem?.ThrusterCostMultiplier??1f;
    private readonly RaycastHit[] aimHits = new RaycastHit[32];

    private void Awake()
    {
        if (stats == null) stats = GetComponent<PlayerStats>();
        if (weaponController == null) weaponController = GetComponent<WeaponController>();
        damageable = GetComponent<Damageable>();
        InputRouter = GetComponent<PlayerInputRouter>() ?? gameObject.AddComponent<PlayerInputRouter>();
        AutoAim = GetComponent<AutoAimController>() ?? gameObject.AddComponent<AutoAimController>();
        Motor = GetComponent<CharacterController>();
        if (Motor == null)
        {
            foreach (var oldCollider in GetComponentsInChildren<Collider>(true))
                if (oldCollider.GetComponentInParent<Damageable>() == damageable) oldCollider.enabled = false;
            Motor = gameObject.AddComponent<CharacterController>();
            Motor.height = 4.45f;
            Motor.center = new Vector3(0f, 2.25f, 0f);
            Motor.radius = 0.72f;
            Motor.stepOffset = 0.25f;
            Motor.skinWidth = 0.04f;
            Motor.minMoveDistance = 0f;
        }
        // Shared traversal clearance for both routes through the lunar yard.
        Motor.stepOffset=.45f;
        Motor.skinWidth=.08f;
        AimDirection = Vector3.forward;
        Loadout = GetComponent<PlayerLoadout>() ?? gameObject.AddComponent<PlayerLoadout>();
        Melee = GetComponent<PlayerMeleeController>() ?? gameObject.AddComponent<PlayerMeleeController>();
        Stance = GetComponent<PlayerWeaponStance>() ?? gameObject.AddComponent<PlayerWeaponStance>();
        if(GetComponent<CombatRecovery>()==null)gameObject.AddComponent<CombatRecovery>();
    }

    private void Start()
    {
        if (GetComponent<MobileControls>() == null) gameObject.AddComponent<MobileControls>();
        if (GetComponent<MechDashPresentation>() == null) gameObject.AddComponent<MechDashPresentation>();
    }

    private void Update() { Simulate(InputRouter.ReadCommand(), Time.deltaTime); }

    public void Simulate(PlayerCommand command, float deltaTime)
    {
        var gm = GameManager.Instance;
        if ((gm != null && !gm.CanPlayerControl) || (damageable != null && damageable.IsDead))
        {
            CancelMovement();
            AutoAim.Clear();
            return;
        }
        if (deltaTime <= 0f) return;
        if(command.Identity!=0 && (command.Cancellation!=CombatRuntime.ActionGeneration || Time.time-command.InputTime>.5f))return;
        command.Move = Vector2.ClampMagnitude(command.Move, 1f);
        CombatLabTelemetry.Command(command);
        boostHeldTime = command.BoostHeld ? boostHeldTime + deltaTime : 0;
        if (!command.BoostHeld) boostExhausted = false;
        IsBoosting = !boostExhausted && command.BoostHeld && boostHeldTime > .12f && gm != null && gm.IsCombatActive;
        if (IsBoosting && stats != null && !stats.TrySpendEnergy(24 * ThrusterCost * deltaTime)) { IsBoosting = false; boostExhausted = true; }
        {
            if(command.Dash)
            {
                Vector3 direction=new Vector3(command.Move.x,0,command.Move.y);
                if(direction.sqrMagnitude<.01f)
                    direction=command.HasAim?Vector3.ProjectOnPlane(command.AimPoint-transform.position,Vector3.up).normalized:AimDirection;
                Actions.Enqueue(BufferedCombatAction.Dash,Time.time,CombatRules.Current.DashBuffer,direction,command.Move.sqrMagnitude>.01f);
            }
            if(Actions.TryPeek(BufferedCombatAction.Dash,Time.time,out var requestedDirection)
                && TryDash(new Vector2(requestedDirection.x,requestedDirection.z),Actions.WasMoving(BufferedCombatAction.Dash)))
                Actions.Cancel(BufferedCombatAction.Dash);
        }
        float speed = stats != null ? stats.MoveSpeed : 7.4f;
        if(Melee.IsAttacking && command.HasAim)Melee.Steer(ResolveManualAim(command.AimPoint),deltaTime);
        if (IsBoosting) speed *= 1.6f;
        Vector3 desired = new Vector3(command.Move.x, 0f, command.Move.y) * speed * Melee.MovementScale;
        planarVelocity = Vector3.MoveTowards(planarVelocity, desired, (desired.sqrMagnitude > 0f ? (Vector3.Dot(planarVelocity, desired) < 0 ? MechMovementProfile.For(GetComponent<PlayerMechLoader>().SelectedHero).reversal : acceleration) : braking) * deltaTime);
        if(Melee.IsAttacking && Melee.MovementScale==0) planarVelocity=Vector3.zero;
        float dashStep = Mathf.Min(dashRemaining, deltaTime);
        // Integrate the launch-heavy speed curve over the frame, preserving distance at any FPS.
        float dashTravelTime = 0;
        if (dashStep > 0)
        {
            float from = 1 - dashRemaining / dashTotalDuration;
            float to = Mathf.Min(1, from + dashStep / dashTotalDuration);
            // The integral remains one dash distance. Held movement exits at running speed,
            // instead of braking to zero then snapping back to the locomotion velocity.
            float Curve(float t)=>(2-dashExitFraction)*t-(1-dashExitFraction)*t*t;
            dashTravelTime = dashTotalDuration * (Curve(to)-Curve(from));
        }
        bool wasDashing=dashRemaining>0;
        Vector3 displacement = dashVelocity * dashTravelTime + planarVelocity * (deltaTime - dashStep);
        if(!IsDashing) displacement+=Melee.ConsumeRootAdvance();
        dashRemaining = Mathf.Max(0f, dashRemaining - deltaTime);
        Vector3 before = transform.position;
        Vector3 bounded = before + displacement;
        bounded.x = Mathf.Clamp(bounded.x, -27f, 27f);
        bounded.z = Mathf.Clamp(bounded.z, -27f, 27f);
        var collision = Motor.Move(bounded - before + Vector3.down * 2f * deltaTime);
        if ((collision & CollisionFlags.Sides) != 0)
        {
            Vector3 planarTravel=Vector3.ProjectOnPlane(transform.position-before,Vector3.up);
            // Let the controller slide along a wall or step past a small lunar detail.
            // Stop a frontal blocked dash, not every grazing side contact.
            if(dashStep>0 && planarTravel.sqrMagnitude<displacement.sqrMagnitude*.04f)dashRemaining=0f;
        }
        if(wasDashing && !IsDashing)dashAttackUntil=Time.time+CombatRules.Current.DashSlashWindow;
        Velocity = (transform.position - before) / deltaTime;
        Velocity = new Vector3(Velocity.x, 0f, Velocity.z);
        MoveDirection = Vector3.ClampMagnitude(Velocity / speed, 1f);

        if (!Melee.IsAttacking && command.HasAim) AimAt(ResolveManualAim(command.AimPoint),deltaTime,command.Melee?720f:0);
        else if (!Melee.IsAttacking && HasAimPoint)
            AimAt(transform.position + AimDirection * 14f + Vector3.up * 1.1f);
        else if (!Melee.IsAttacking && !HasAimPoint && command.Move.sqrMagnitude > 0.02f)
            AimAt(transform.position + new Vector3(command.Move.x, 0f, command.Move.y) * 14f + Vector3.up * 1.1f);
        if (gm == null || gm.IsCombatActive)
        {
            AutoAim.Tick();
            var target = AutoAim.CurrentTarget;
            Stance.Process(command, target != null && AutoAim.IsValidTarget(target) ? target : null, deltaTime);
        }
        else AutoAim.Clear();
        GetComponent<CombatRecovery>().Observe(command.Move,before,deltaTime);
    }

    public void AimAt(Vector3 worldPoint,float dt=-1,float rate=0)
    {
        Vector3 direction = worldPoint - transform.position;
        direction.y = 0f;
        bool sword=rate>0||Stance!=null&&Stance.State!=WeaponStance.Ranged;
        if(direction.sqrMagnitude<(sword?1.44f:.01f))return;
        if(Melee!=null&&Melee.IsAttacking&&!Melee.CanSteer)return;
        AimPoint = worldPoint;
        HasAimPoint = true;
        if(sword)
        {
            if(rate<=0)rate=Melee!=null&&Melee.IsAttacking?(Melee.AttackElapsed<Melee.CurrentStroke.contactStart?540:360):720;
            direction=Vector3.RotateTowards(transform.forward,direction.normalized,rate*Mathf.Deg2Rad*Mathf.Max(0,dt<0?Time.deltaTime:dt),0);
        }
        AimDirection = direction.normalized;
        transform.rotation = Quaternion.LookRotation(AimDirection, Vector3.up);
    }

    private Vector3 ResolveManualAim(Vector3 point)
    {
        return PlanarCombat.Point(point);

    }

    public bool TryDash(Vector2 movement) => TryDash(movement,movement.sqrMagnitude>.01f);

    private bool TryDash(Vector2 movement, bool moving)
    {
        var gm = GameManager.Instance;
        if ((gm != null && !gm.CanPlayerControl) || (damageable != null && damageable.IsDead) || Time.time < nextDashTime) return false;
        if (!Melee.CanDashCancel) return false;
        if (stats != null && !stats.TrySpendEnergy(25f*ThrusterCost)) return false;
        Melee.CancelAttack();
        Stance.ClearRequests();
        Vector3 direction = movement.sqrMagnitude > 0.01f ? new Vector3(movement.x, 0f, movement.y) : AimDirection;
        if (direction.sqrMagnitude < 0.01f) direction = transform.forward;
        direction.Normalize();
        DashStartedAt=Time.time;
        dashRemaining = Mathf.Max(0.05f, dashDuration);
        dashTotalDuration = dashRemaining;
        dashVelocity = direction * (stats != null ? stats.DashDistance : 5f) / dashRemaining;
        dashExitFraction=moving
            ?Mathf.Clamp((stats!=null?stats.MoveSpeed:7.4f)*(IsBoosting ? 1.6f : 1f)/Mathf.Max(.01f,dashVelocity.magnitude),0,.6f):0;
        nextDashTime = Time.time + (stats != null ? stats.DashCooldown : 1.25f);
        if (damageable != null) damageable.SetInvulnerable(0.16f);
        Dashed?.Invoke(direction);
        GameAudio.Play(GameAudioCue.Dash, 0.35f);
        return true;
    }

    public void CancelMovement()
    {
        dashAttackUntil=-10;
        planarVelocity = Velocity = MoveDirection = Vector3.zero;
        IsBoosting = false;
        boostHeldTime = 0;
        Actions.Clear();
        dashRemaining = 0f;
        if (InputRouter != null) InputRouter.Clear();
        if (Melee != null) Melee.CancelAttack();
        if (Stance != null) Stance.ClearRequests();
    }

    public void RestoreAt(Vector3 position)
    {
        CancelMovement();
        GetComponentInChildren<ValkyrBackCannon>()?.Cancel();
        nextDashTime = 0f;
        GetComponentInChildren<ValkyrMotionDriver>()?.ResetHitReaction();
        GetComponentInChildren<NemesisMotionRig>()?.ResetDeployment();
        AutoAim.Clear();
        HasAimPoint = false;
        AimDirection = Vector3.forward;
        Motor.enabled = false;
        transform.SetPositionAndRotation(position, Quaternion.identity);
        Motor.enabled = true;
        weaponController.ResetCooldowns();
        Melee.ResetCooldown();
        Stance.ResetStance();
    }

    private void OnDisable() { CancelMovement(); }
}
