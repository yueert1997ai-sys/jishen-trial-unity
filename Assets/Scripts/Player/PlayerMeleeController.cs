using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class PlayerMeleeController : MonoBehaviour
{
    public const float Duration = 0.6f;
    public bool IsAttacking { get; private set; }
    public float CooldownRemaining => Mathf.Max(0, nextAttack - Time.time);
    public event Action AttackStarted;
    public event Action AttackCancelled;
    private PlayerController player;
    private Damageable owner;
    private float elapsed, nextAttack;
    private Vector3 forward;
    private readonly HashSet<Damageable> hit = new HashSet<Damageable>();
    private readonly RaycastHit[] blockers = new RaycastHit[32];

    private void Awake() { player = GetComponent<PlayerController>(); owner = GetComponent<Damageable>(); }
    public bool TryAttack()
    {
        if (player.Loadout == null || player.Loadout.Selected != PrimaryWeapon.Greatsword
            || IsAttacking || CooldownRemaining > 0 || player.IsDashing || owner.IsDead
            || (GameManager.Instance != null && !GameManager.Instance.IsCombatActive)) return false;
        elapsed = 0;
        nextAttack = Time.time + 0.68f;
        forward = player.AimDirection;
        hit.Clear();
        IsAttacking = true;
        AttackStarted?.Invoke();
        GameAudio.Play(GameAudioCue.Slash, .28f, 1.15f);
        return true;
    }
    private void Update()
    {
        if (!IsAttacking) return;
        if (owner.IsDead || player.IsDashing || (GameManager.Instance != null && !GameManager.Instance.IsCombatActive))
        { CancelAttack(); return; }
        float previous = elapsed;
        elapsed += Time.deltaTime;
        // Test the swept time interval so a long frame cannot skip the damage window.
        if (previous <= 0.42f && elapsed >= 0.17f) ApplyHits();
        if (elapsed >= Duration) IsAttacking = false;
    }
    private void ApplyHits()
    {
        var targets = Damageable.Active;
        for (int i = targets.Count - 1; i >= 0; i--)
        {
            if (i >= targets.Count) continue;
            var target = targets[i];
            if (target == null || target.IsDead || target.team == owner.team || hit.Contains(target)) continue;
            Vector3 offset = target.AimCenter - transform.position;
            if (Mathf.Abs(offset.y - 1.1f) > 2.5f) continue;
            offset.y = 0;
            if (offset.sqrMagnitude > 3.5f * 3.5f || Vector3.Dot(forward, offset.normalized) < 0.35f) continue;
            Vector3 origin = transform.position + Vector3.up * 1.1f;
            Vector3 ray = target.AimCenter - origin;
            int count = Physics.RaycastNonAlloc(origin, ray.normalized, blockers, ray.magnitude, ~0, QueryTriggerInteraction.Ignore);
            bool blocked = count == blockers.Length;
            for (int j = 0; j < count; j++)
            {
                var actor = blockers[j].collider.GetComponentInParent<Damageable>();
                if (actor != owner && actor != target) { blocked = true; break; }
            }
            if (blocked) continue;
            hit.Add(target);
            var upgrades = player.weaponController.upgradeSystem;
            float scale = (player.stats != null ? player.stats.DamageMultiplier : 1f)
                * (upgrades != null ? upgrades.DamageMultiplier : 1f);
            target.TakeDamage(42f * scale, new DamageInfo(gameObject, transform.position, owner, 42f * scale));
        }
    }
    public void CancelAttack()
    {
        if (!IsAttacking) return;
        IsAttacking = false;
        AttackCancelled?.Invoke();
    }
    public void ResetCooldown() { CancelAttack(); nextAttack = 0; }
    private void OnDisable() { CancelAttack(); }
}
