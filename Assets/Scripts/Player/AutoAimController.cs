using UnityEngine;

[DisallowMultipleComponent]
public class AutoAimController : MonoBehaviour
{
    public float range = 14f;
    public Damageable CurrentTarget { get; private set; }
    private Damageable owner;
    private Camera view;
    private float nextScan;
    private readonly RaycastHit[] hits = new RaycastHit[16];

    private void Awake() { owner = GetComponent<Damageable>(); }

    public void Clear() { CurrentTarget = null; nextScan = 0f; }

    public void Tick()
    {
        if (view == null) view = Camera.main;
        if (Time.time < nextScan && CurrentTarget != null && !CurrentTarget.IsDead) return;
        nextScan = Time.time + 0.1f;
        if (!IsValidTarget(CurrentTarget)) CurrentTarget = null;
        float bestDistance = CurrentTarget != null ? (CurrentTarget.transform.position - transform.position).sqrMagnitude * 0.49f : range * range;
        var candidates = Damageable.Active;
        for (int i = 0; i < candidates.Count; i++)
        {
            var candidate = candidates[i];
            float distance = (candidate.transform.position - transform.position).sqrMagnitude;
            if (distance < bestDistance && IsValidTarget(candidate))
            {
                CurrentTarget = candidate;
                bestDistance = distance;
            }
        }
    }

    public bool IsValidTarget(Damageable target)
    {
        if (target == null || target.IsDead || !target.isActiveAndEnabled || owner == null || target.team == owner.team) return false;
        if ((target.transform.position - transform.position).sqrMagnitude > range * range) return false;
        if (view == null) view = Camera.main;
        if (view != null)
        {
            Vector3 point = view.WorldToViewportPoint(target.AimCenter);
            if (point.z <= 0f || point.x < 0.04f || point.x > 0.96f || point.y < 0.08f || point.y > 0.92f) return false;
        }
        Vector3 start = transform.position + Vector3.up * 1.1f;
        Vector3 line = target.AimCenter - start;
        int count = Physics.RaycastNonAlloc(start, line.normalized, hits, line.magnitude, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        if (count == hits.Length) return false;
        for (int i = 0; i < count; i++)
        {
            var actor = hits[i].collider.GetComponentInParent<Damageable>();
            if (actor != owner && actor != target) return false;
        }
        return true;
    }
}
