using UnityEngine;

[DefaultExecutionOrder(110)]
public sealed class MechRivalPresentation : MonoBehaviour
{
    public RiggedMechAnimator visual;
    public Transform Muzzle => visual.muzzle;
    private BossController boss;
    private Damageable health;
    private string state;
    private Vector3 previous;
    private float slashUntil;
    private void Awake() { boss = GetComponent<BossController>(); health = GetComponent<Damageable>(); previous = transform.position; }
    public void PlaySlash() { slashUntil = Time.time + .6f; }
    private void Update()
    {
        if (visual == null) return;
        var animator = visual.animator;
        animator.applyRootMotion = false;
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        float speed = (transform.position - previous).magnitude / Mathf.Max(Time.deltaTime, .001f);
        previous = transform.position;
        string next = health.IsDead ? "Death" : Time.time < slashUntil ? "Slash" : boss.ActionRunning && boss.CurrentPattern == BossPattern.Charge && speed > 7 ? "Dash" : "Locomotion";
        if (next != state) { animator.CrossFadeInFixedTime(next, .07f); state = next; }
        animator.SetFloat("Speed", Mathf.Clamp01(speed / 3));
        animator.SetFloat("Stride", 1.1f);
        animator.SetLayerWeight(1, 0);
        if (visual.bladeTrail != null) visual.bladeTrail.emitting = next == "Slash";
    }
    private void LateUpdate()
    {
        if (visual == null || visual.rigidPose == null) return;
        visual.rigidPose.ApplyPose(!health.IsDead);
        if (boss.target != null && !health.IsDead)
        {
            Vector3 direction = boss.target.position + Vector3.up * 1.6f - Muzzle.position;
            visual.rigidPose.cannon.rotation = Quaternion.FromToRotation(Muzzle.forward, direction.normalized) * visual.rigidPose.cannon.rotation;
        }
    }
}
