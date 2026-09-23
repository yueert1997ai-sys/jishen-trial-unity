using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

// Group coordination so enemy packs read as organized pressure instead of a zerg:
// a bounded number of simultaneous attackers and stable surround spacing.
// Slots are requested right before a windup starts and released when the cycle
// commits, is cancelled, or the enemy leaves play.
public static class EnemyTactics
{
    static readonly HashSet<EnemyBase> attackers = new HashSet<EnemyBase>();
    static readonly Dictionary<EnemyBase, int> ringSlices = new Dictionary<EnemyBase, int>();
    static readonly RaycastHit[] sightHits = new RaycastHit[48];
    static readonly Vector3[] corners = new Vector3[32];
    static NavMeshPath route;

    public static int AttackersActive => attackers.Count;
    public static int RingPopulation => ringSlices.Count;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Reset()
    {
        attackers.Clear();
        ringSlices.Clear();
    }

    public static bool HasAttackSlot(EnemyBase enemy) => attackers.Contains(enemy);
    public static bool CanAttack(EnemyBase enemy) => attackers.Contains(enemy) || attackers.Count < CombatRules.Current.MaxConcurrentAttackers;

    public static bool TryAcquireAttackSlot(EnemyBase enemy)
    {
        if (enemy == null || !enemy.isActiveAndEnabled || enemy.GetComponent<Damageable>().IsDead) return false;
        if (attackers.Contains(enemy)) return true;
        if (attackers.Count >= CombatRules.Current.MaxConcurrentAttackers) return false;
        attackers.Add(enemy);
        return true;
    }

    public static void ReleaseAttackSlot(EnemyBase enemy) => attackers.Remove(enemy);

    // A slot is an absolute direction, anchored when an actor joins the pack.
    // Rotating an actor's CURRENT bearing here made the old destination orbit
    // endlessly. Select a free bearing nearest its approach, even after deaths.
    public static Vector3 Anchor(EnemyBase enemy, Vector3 playerPosition, Vector3 fromEnemy)
    {
        if (!ringSlices.TryGetValue(enemy, out int slice))
        {
            Vector3 approach = fromEnemy - playerPosition;
            float bearing = Mathf.Atan2(approach.x, approach.z) * Mathf.Rad2Deg;
            float best = float.PositiveInfinity; slice = 0;
            for (int i = 0; i < 8; i++)
            {
                float score = Mathf.Abs(Mathf.DeltaAngle(bearing, i * 45f));
                foreach (var pair in ringSlices)
                    if (pair.Key != null && pair.Value == i) score += 1000;
                if (score < best) { best = score; slice = i; }
            }
            ringSlices[enemy] = slice;
        }
        bool ranged = enemy.kind == EnemyKind.Ranged || enemy.kind == EnemyKind.Elite;
        float radius = ranged ? CombatRules.Current.SurroundRadiusRanged : CombatRules.Current.SurroundRadiusMelee;
        Vector3 offset = Quaternion.Euler(0f, slice * 45f, 0f) * Vector3.forward;
        return playerPosition + offset * radius;
    }

    // Weapons collide on the combat plane; a high cosmetic muzzle must not
    // let an enemy see or fire through cover that blocks its real projectile.
    public static bool ClearSight(Vector3 from, Vector3 to)
    {
        from.y = to.y = PlanarCombat.Height;
        Vector3 delta = to - from;
        if (delta.sqrMagnitude < .01f) return true;
        int count = Physics.SphereCastNonAlloc(from, .12f, delta.normalized, sightHits,
            delta.magnitude, ~0, QueryTriggerInteraction.Ignore);
        if (count == sightHits.Length) return false;
        for (int i = 0; i < count; i++)
            if (sightHits[i].collider.GetComponentInParent<Damageable>() == null) return false;
        return true;
    }

    // Bounded tactical search: actual reachable routes, useful range, sight and
    // teammate spacing. It runs on a decision timer, never on every frame.
    public static bool FindFiringPosition(EnemyBase enemy, Vector3 contact,
        float minimumMove, out Vector3 destination)
    {
        destination = enemy.transform.position;
        Vector3 anchor = Anchor(enemy, contact, destination);
        float radius = CombatRules.Current.SurroundRadiusRanged;
        float best = float.PositiveInfinity;
        if (route == null) route = new NavMeshPath();
        for (int ring = 0; ring < 2; ring++)
        for (int i = 0; i < 12; i++)
        {
            Vector3 candidate = contact + Quaternion.Euler(0, i * 30, 0) * Vector3.forward * (radius - ring * 2f);
            if (!NavMesh.SamplePosition(candidate, out var hit, .8f, NavMesh.AllAreas)
                || Vector3.Distance(hit.position, enemy.transform.position) < minimumMove
                || !ClearSight(hit.position, contact)
                || !NavMesh.CalculatePath(enemy.transform.position, hit.position, NavMesh.AllAreas, route)
                || route.status != NavMeshPathStatus.PathComplete) continue;
            int n = route.GetCornersNonAlloc(corners);
            if (n == corners.Length) continue;
            float length = 0;
            for (int c = 1; c < n; c++) length += Vector3.Distance(corners[c-1], corners[c]);
            float score = length + Vector3.Distance(hit.position, anchor) * .45f;
            foreach (var pair in ringSlices)
            {
                if (pair.Key == null || pair.Key == enemy) continue;
                float separation = Vector3.Distance(pair.Key.transform.position, hit.position);
                score += Mathf.Max(0, 3.2f - separation) * 4f;
            }
            if (score < best) { best = score; destination = hit.position; }
        }
        return !float.IsPositiveInfinity(best);
    }

    public static void Forget(EnemyBase enemy)
    {
        attackers.Remove(enemy);
        ringSlices.Remove(enemy);
    }
}
