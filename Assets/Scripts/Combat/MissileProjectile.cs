using UnityEngine;

public class MissileProjectile : Projectile
{
    public Damageable target;
    public float turnRate = 8f;
    public float searchRadius = 32f;
    private float nextThreatCheck;
    private void OnEnable(){target=null;nextThreatCheck=0;}

    protected override void Update()
    {
        if(GameManager.Instance!=null&&!GameManager.Instance.IsCombatActive)return;
        if (target == null || target.IsDead)
        {
            target = FindNearestTarget();
        }

        if (target != null)
        {
            if(team==0 && HitKind==CombatHitKind.Missile && Time.time>=nextThreatCheck)
            {
                nextThreatCheck=Time.time+.08f;
                target.GetComponent<EnemyBase>()?.TryEvadeMissile(transform.position,direction);
            }
            Vector3 desired = target.AimCenter - transform.position;
            if (desired.sqrMagnitude > 0.01f)
            {
                direction = Vector3.RotateTowards(direction, desired.normalized, turnRate * Time.deltaTime, 0f).normalized;
            }
        }

        base.Update();
    }

    private Damageable FindNearestTarget()
    {
        var candidates = Damageable.Active;
        Damageable best = null;
        float bestDistance = searchRadius * searchRadius;

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
