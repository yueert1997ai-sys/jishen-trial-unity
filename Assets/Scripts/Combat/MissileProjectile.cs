using UnityEngine;

public class MissileProjectile : Projectile
{
    public Damageable target;
    public float turnRate = 8f;
    public float searchRadius = 32f;

    protected override void Update()
    {
        if (target == null || target.IsDead)
        {
            target = FindNearestTarget();
        }

        if (target != null)
        {
            Vector3 desired = target.transform.position + Vector3.up * 0.8f - transform.position;
            desired.y = 0f;
            if (desired.sqrMagnitude > 0.01f)
            {
                direction = Vector3.RotateTowards(direction, desired.normalized, turnRate * Time.deltaTime, 0f).normalized;
            }
        }

        base.Update();
    }

    private Damageable FindNearestTarget()
    {
        Damageable[] candidates = FindObjectsByType<Damageable>(FindObjectsSortMode.None);
        Damageable best = null;
        float bestDistance = searchRadius * searchRadius;

        for (int i = 0; i < candidates.Length; i++)
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
