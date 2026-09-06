using System.Collections.Generic;
using UnityEngine;

// Visual-only adapter. Movement, damage, fire cadence and dash authority stay on the player.
[DisallowMultipleComponent]
public sealed class RiggedMechAnimator : MonoBehaviour
{
    public Animator animator;
    public Transform hips, chest, head, leftArm, leftForearm, leftHand, rightArm, rightForearm, rightHand;
    public Transform muzzle, bladeTip;
    public TrailRenderer bladeTrail;
    public RigidMechPoseDriver rigidPose;

    private PlayerController player;
    private WeaponController weapon;
    private Damageable health;
    private MechMotionAnimator legacyMotion;
    private Transform previousMuzzle;
    private bool legacyWasEnabled, bound;
    private float firingUntil, slashUntil, recoil, hitRecoil, cannonWeight, legYaw, nextThrust;
    private string currentState;
    private readonly List<SocketBinding> sockets = new List<SocketBinding>();
    private static readonly int Speed = Animator.StringToHash("Speed");
    private static readonly int Stride = Animator.StringToHash("Stride");

    private sealed class SocketBinding
    {
        public Transform socket, bone;
        public Vector3 position;
        public Quaternion rotation;
    }

    private void Start() => Bind();

    private void OnEnable()
    {
        if (player != null && !bound) Bind();
    }

    private void Bind()
    {
        player = GetComponentInParent<PlayerController>();
        weapon = GetComponentInParent<WeaponController>();
        health = GetComponentInParent<Damageable>();
        if (player == null || weapon == null || animator == null || animator.runtimeAnimatorController == null)
        {
            Debug.LogError("Rigged hero is missing its player/weapon/animation binding.", this);
            enabled = false;
            return;
        }
        animator.applyRootMotion = false;
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        legacyMotion = player.GetComponent<MechMotionAnimator>();
        if (legacyMotion != null)
        {
            legacyWasEnabled = legacyMotion.enabled;
            legacyMotion.enabled = false;
            legacyMotion.visualRoot.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        }
        previousMuzzle = weapon.muzzle;
        weapon.muzzle = muzzle;
        weapon.BeamFired += OnBeam;
        weapon.SkillFired += OnSkill;
        player.Melee.AttackStarted += PreviewSlash;
        player.Melee.AttackCancelled += CancelSlash;
        if (health != null) health.OnDamaged += OnHit;
        var hardpoints = player.GetComponent<MechHardpointManager>();
        if (hardpoints != null)
        {
            BindSocket(hardpoints, "HeadSocket", head);
            BindSocket(hardpoints, "ChestSocket", chest);
            BindSocket(hardpoints, "BackSocket", chest);
            BindSocket(hardpoints, "WaistSocket", hips);
            BindSocket(hardpoints, "LeftShoulderSocket", leftArm);
            BindSocket(hardpoints, "RightShoulderSocket", rightArm);
            BindSocket(hardpoints, "LeftArmSocket", leftForearm);
            BindSocket(hardpoints, "RightArmSocket", rightForearm);
            BindSocket(hardpoints, "LeftHandSocket", leftHand);
            BindSocket(hardpoints, "RightHandSocket", rightHand);
        }
        bound = true;
        currentState = null;
    }

    private void BindSocket(MechHardpointManager manager, string name, Transform bone)
    {
        if (rigidPose != null) bone = rigidPose.Resolve(bone);
        var socket = manager.GetSocket(name);
        sockets.Add(new SocketBinding { socket = socket, bone = bone, position = socket.localPosition, rotation = socket.localRotation });
    }

    private void OnDisable()
    {
        if (!bound) return;
        if (weapon != null)
        {
            weapon.BeamFired -= OnBeam;
            weapon.SkillFired -= OnSkill;
            if (weapon.muzzle == muzzle) weapon.muzzle = previousMuzzle;
        }
        if (health != null) health.OnDamaged -= OnHit;
        player.Melee.AttackStarted -= PreviewSlash;
        player.Melee.AttackCancelled -= CancelSlash;
        if (legacyMotion != null) legacyMotion.enabled = legacyWasEnabled;
        foreach (var item in sockets)
            if (item.socket != null) item.socket.SetLocalPositionAndRotation(item.position, item.rotation);
        if (bladeTrail != null) bladeTrail.emitting = false;
        sockets.Clear();
        bound = false;
    }

    private void Update()
    {
        if (!bound) return;
        bool dead = health != null && health.IsDead;
        animator.updateMode = dead ? AnimatorUpdateMode.UnscaledTime : AnimatorUpdateMode.Normal;
        string state = dead ? "Death" : Time.time < slashUntil ? "Slash" : player.IsDashing ? "Dash" : "Locomotion";
        if (state != currentState)
        {
            animator.CrossFadeInFixedTime(state, state == "Dash" ? 0.06f : 0.1f, 0);
            currentState = state;
        }
        float movement = player.Velocity.magnitude;
        animator.SetFloat(Speed, Mathf.Clamp01(movement / 3f), 0.1f, Time.deltaTime);
        Vector3 local = player.transform.InverseTransformDirection(player.Velocity);
        animator.SetFloat(Stride, local.z < -0.1f ? -1.25f : 1.25f);
        float weight = !dead && Time.time < firingUntil && state != "Slash" ? 1 : 0;
        cannonWeight = Mathf.MoveTowards(cannonWeight, weight, Time.deltaTime * 12);
        animator.SetLayerWeight(1, rigidPose != null ? 0 : cannonWeight);
        if (bladeTrail != null)
            bladeTrail.emitting = state == "Slash" && slashUntil - Time.time < 0.43f && slashUntil - Time.time > 0.18f;
    }

    private void LateUpdate()
    {
        if (!bound) return;
        bool alive = health == null || !health.IsDead;
        float dt = alive ? Time.deltaTime : Time.unscaledDeltaTime;
        if (dt <= 0) return;
        if (alive)
        {
            Vector3 velocity = player.Velocity;
            Vector3 local = player.transform.InverseTransformDirection(velocity);
            float yaw = velocity.sqrMagnitude > 0.1f && !player.IsDashing && Time.time >= slashUntil
                ? Mathf.Atan2(local.x * (local.z < 0 ? -1 : 1), Mathf.Abs(local.z) + 0.01f) * Mathf.Rad2Deg : 0;
            legYaw = Mathf.Lerp(legYaw, Mathf.Clamp(yaw, -70, 70), 1 - Mathf.Exp(-14 * dt));
            Quaternion upperRotation = chest.rotation;
            hips.rotation = Quaternion.AngleAxis(legYaw, Vector3.up) * hips.rotation;
            chest.rotation = upperRotation;
            chest.rotation = Quaternion.AngleAxis(-hitRecoil * 8 + recoil * 3, player.transform.right) * chest.rotation;
            if (rigidPose == null && cannonWeight > 0.01f)
            {
                Vector3 aim = player.HasAimPoint ? player.AimPoint - muzzle.position : player.AimDirection;
                Quaternion aligned = Quaternion.FromToRotation(muzzle.forward, aim.normalized) * leftForearm.rotation;
                leftForearm.rotation = Quaternion.Slerp(leftForearm.rotation, aligned, cannonWeight);
                leftForearm.rotation = Quaternion.AngleAxis(-recoil * 4, player.transform.right) * leftForearm.rotation;
            }
            if (velocity.sqrMagnitude > 0.1f && Time.time >= nextThrust && GameManager.Instance != null && GameManager.Instance.IsCombatActive)
            {
                nextThrust = Time.time + 0.035f;
                Vector3 exhaust = -velocity.normalized - Vector3.up;
                CombatEffects.Thrust(player.transform.position + Vector3.up * 0.6f + player.transform.right * 0.35f, exhaust, player.IsDashing);
                CombatEffects.Thrust(player.transform.position + Vector3.up * 0.6f - player.transform.right * 0.35f, exhaust, player.IsDashing);
            }
        }
        if (rigidPose != null)
        {
            rigidPose.ApplyPose(alive);
            if (alive && player.HasAimPoint)
            {
                Vector3 aim = player.AimPoint - muzzle.position;
                Quaternion aligned = Quaternion.FromToRotation(muzzle.forward, aim.normalized) * rigidPose.cannon.rotation;
                rigidPose.cannon.rotation = aligned;
                rigidPose.cannon.rotation = Quaternion.AngleAxis(-recoil * 2, player.transform.right) * rigidPose.cannon.rotation;
            }
        }
        recoil = Mathf.MoveTowards(recoil, 0, dt * 9);
        hitRecoil = Mathf.MoveTowards(hitRecoil, 0, dt * 6);
        RefreshSockets();
    }

    public void RefreshSockets()
    {
        foreach (var item in sockets)
            item.socket.SetPositionAndRotation(item.bone.position, item.bone.rotation);
    }

    private void OnBeam() { firingUntil = Time.time + 0.35f; recoil = 0.7f; }
    private void OnSkill() { firingUntil = Time.time + 0.65f; recoil = 1.6f; }
    private void OnHit(Damageable target, DamageInfo damage) { hitRecoil = 1; }

    private void CancelSlash() { slashUntil = 0; if (bladeTrail != null) { bladeTrail.emitting = false; bladeTrail.Clear(); } }

    // Damage authority remains in PlayerMeleeController; the context menu is visual-only.
    [ContextMenu("Preview slash (visual only)")]
    public void PreviewSlash()
    {
        if (!Application.isPlaying || !bound || (health != null && health.IsDead)) return;
        slashUntil = Time.time + 0.6f;
        currentState = null;
        if (bladeTrail != null) bladeTrail.Clear();
    }
}
