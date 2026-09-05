using System;
using UnityEngine;

[DisallowMultipleComponent]
public class PlayerController : MonoBehaviour
{
    public PlayerStats stats;
    public WeaponController weaponController;
    public float acceleration = 42f;
    public float braking = 65f;
    public float dashDuration = 0.2f;
    public bool automaticFire = true;

    public PlayerInputRouter InputRouter { get; private set; }
    public AutoAimController AutoAim { get; private set; }
    public CharacterController Motor { get; private set; }
    public Vector3 AimDirection { get; private set; }
    public Vector3 AimPoint { get; private set; }
    public bool HasAimPoint { get; private set; }
    public Vector3 MoveDirection { get; private set; }
    public Vector3 Velocity { get; private set; }
    public bool IsDashing => dashRemaining > 0f;
    public float DashCooldownRemaining => Mathf.Max(0f, nextDashTime - Time.time);
    public bool IsDashReady => DashCooldownRemaining <= 0f;
    public event Action<Vector3> Dashed;

    private Damageable damageable;
    private Vector3 planarVelocity;
    private Vector3 dashVelocity;
    private float dashRemaining;
    private float dashTotalDuration;
    private float nextDashTime;

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
            Motor.height = 3.25f;
            Motor.center = new Vector3(0f, 1.65f, 0f);
            Motor.radius = 0.72f;
            Motor.stepOffset = 0.25f;
            Motor.skinWidth = 0.04f;
            Motor.minMoveDistance = 0f;
        }
        AimDirection = Vector3.forward;
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
        command.Move = Vector2.ClampMagnitude(command.Move, 1f);
        if (command.Dash) TryDash(command.Move);
        float speed = stats != null ? stats.MoveSpeed : 7.4f;
        Vector3 desired = new Vector3(command.Move.x, 0f, command.Move.y) * speed;
        planarVelocity = Vector3.MoveTowards(planarVelocity, desired, (desired.sqrMagnitude > 0f ? acceleration : braking) * deltaTime);
        float dashStep = Mathf.Min(dashRemaining, deltaTime);
        // Integrate the launch-heavy speed curve over the frame, preserving distance at any FPS.
        float dashTravelTime = 0;
        if (dashStep > 0)
        {
            float from = 1 - dashRemaining / dashTotalDuration;
            float to = Mathf.Min(1, from + dashStep / dashTotalDuration);
            dashTravelTime = dashTotalDuration * ((2 * to - to * to) - (2 * from - from * from));
        }
        Vector3 displacement = dashVelocity * dashTravelTime + planarVelocity * (deltaTime - dashStep);
        dashRemaining = Mathf.Max(0f, dashRemaining - deltaTime);
        Vector3 before = transform.position;
        Vector3 bounded = before + displacement;
        bounded.x = Mathf.Clamp(bounded.x, -27f, 27f);
        bounded.z = Mathf.Clamp(bounded.z, -27f, 27f);
        var collision = Motor.Move(bounded - before + Vector3.down * 2f * deltaTime);
        if ((collision & CollisionFlags.Sides) != 0) dashRemaining = 0f;
        Velocity = (transform.position - before) / deltaTime;
        Velocity = new Vector3(Velocity.x, 0f, Velocity.z);
        MoveDirection = Vector3.ClampMagnitude(Velocity / speed, 1f);

        if (gm == null || gm.IsCombatActive)
        {
            AutoAim.Tick();
            var target = AutoAim.CurrentTarget;
            if (target != null)
            {
                AimAt(target.AimCenter);
                if (automaticFire && AutoAim.IsValidTarget(target)) weaponController.TryFireBeam();
                if (command.Skill && AutoAim.IsValidTarget(target)) weaponController.TryFireSkill(target);
            }
        }
        else AutoAim.Clear();
        if (AutoAim.CurrentTarget == null && command.Move.sqrMagnitude > 0.02f)
            AimAt(transform.position + new Vector3(command.Move.x, 0.1f, command.Move.y) * 10f);
    }

    public void AimAt(Vector3 worldPoint)
    {
        Vector3 direction = worldPoint - transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.01f) return;
        AimPoint = worldPoint;
        HasAimPoint = true;
        AimDirection = direction.normalized;
        transform.rotation = Quaternion.LookRotation(AimDirection, Vector3.up);
    }

    public bool TryDash(Vector2 movement)
    {
        var gm = GameManager.Instance;
        if ((gm != null && !gm.CanPlayerControl) || (damageable != null && damageable.IsDead) || Time.time < nextDashTime) return false;
        if (stats != null && !stats.TrySpendEnergy(25f)) return false;
        Vector3 direction = movement.sqrMagnitude > 0.01f ? new Vector3(movement.x, 0f, movement.y) : AimDirection;
        if (direction.sqrMagnitude < 0.01f) direction = transform.forward;
        direction.Normalize();
        dashRemaining = Mathf.Max(0.05f, dashDuration);
        dashTotalDuration = dashRemaining;
        dashVelocity = direction * (stats != null ? stats.DashDistance : 5f) / dashRemaining;
        nextDashTime = Time.time + (stats != null ? stats.DashCooldown : 1.25f);
        if (damageable != null) damageable.SetInvulnerable(0.16f);
        Dashed?.Invoke(direction);
        GameAudio.Play(GameAudioCue.Dash, 0.35f);
        return true;
    }

    public void CancelMovement()
    {
        planarVelocity = Velocity = MoveDirection = Vector3.zero;
        dashRemaining = 0f;
        if (InputRouter != null) InputRouter.Clear();
    }

    public void RestoreAt(Vector3 position)
    {
        CancelMovement();
        nextDashTime = 0f;
        AutoAim.Clear();
        HasAimPoint = false;
        Motor.enabled = false;
        transform.SetPositionAndRotation(position, Quaternion.identity);
        Motor.enabled = true;
        weaponController.ResetCooldowns();
    }

    private void OnDisable() { CancelMovement(); }
}
