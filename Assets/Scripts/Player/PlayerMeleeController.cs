using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[DefaultExecutionOrder(-10)]
public sealed class PlayerMeleeController : MonoBehaviour
{
    public const float Duration = 0.6f;
    public bool IsAttacking { get; private set; }
    public float AttackElapsed => elapsed;
    public int ComboStage { get; private set; }
    public bool NextQueued => queuedNext;
    public ValkyrComboProfile MotionProfile {get;private set;}
    public ValkyrComboProfile.Stroke CurrentStroke => MotionProfile.Get(ComboStage);
    public bool IsCutting => IsAttacking && elapsed>=CurrentStroke.contactStart && elapsed<=CurrentStroke.contactEnd;
    public bool CanSheathe => !IsAttacking || elapsed>=CurrentStroke.contactEnd+.05f;
    public Vector3 AttackForward => forward;
    public float MovementScale => IsAttacking && raiken != null ? (elapsed<CurrentStroke.contactEnd?0:.45f) : 1f;
    public bool ImpactHeld => impactHold > 0;
    public float CooldownRemaining => Mathf.Max(0, nextAttack - Time.time);
    public event Action AttackStarted;
    public event Action AttackCancelled;
    public event Action<Damageable, float> StrikeHit;
    private RaikenBladePresentation raiken;
    private PlayerController player;
    private Damageable owner;
    private float elapsed, nextAttack, impactHold, consumedAdvance;
    private Vector3 forward;
    private bool queuedNext,cutSoundPlayed,regripSoundPlayed;
    private readonly HashSet<Damageable> hit = new HashSet<Damageable>();
    private readonly RaycastHit[] blockers = new RaycastHit[32];
    private readonly Collider[] bladeContacts = new Collider[48];

    private void Awake()
    {
        player=GetComponent<PlayerController>();owner=GetComponent<Damageable>();
        MotionProfile=ValkyrComboProfile.LoadActive();
    }
    public bool TryAttack()
    {
        if (player.IsDashing || owner.IsDead || (player.Stance != null && !player.Stance.CanMelee)
            || (GameManager.Instance != null && !GameManager.Instance.IsCombatActive)) return false;
        if(IsAttacking)
        {
            if(ComboStage>=2 || queuedNext || elapsed>CurrentStroke.duration-.15f)return false;
            queuedNext=true;return true;
        }
        if(CooldownRemaining>0)return false;
        if (raiken == null) raiken = GetComponentInChildren<RaikenBladePresentation>();
        BeginStage(0,0);return true;
    }
    private void BeginStage(int stage,float carry)
    {
        ComboStage=stage;elapsed=carry;impactHold=consumedAdvance=0;queuedNext=false;cutSoundPlayed=regripSoundPlayed=false;
        nextAttack=0;
        forward = player.AimDirection;
        hit.Clear();
        IsAttacking = true;
        AttackStarted?.Invoke();
        GameAudio.Play(raiken!=null?GameAudioCue.SwordWindup:GameAudioCue.Slash,raiken!=null?(stage==2?.53f:.32f):.28f,stage==2?.92f:1);
    }
    private void Update()
    {
        if (!IsAttacking) return;
        if (owner.IsDead || player.IsDashing || (GameManager.Instance != null && !GameManager.Instance.IsCombatActive))
        { CancelAttack(); return; }
        if (impactHold > 0) { impactHold = Mathf.Max(0, impactHold - Time.unscaledDeltaTime); return; }
        float previous = elapsed;
        elapsed = Mathf.Min(elapsed + Time.deltaTime, raiken != null ? CurrentStroke.duration : Duration);
        if(raiken!=null)
        {
            if(ComboStage==1&&!regripSoundPlayed&&elapsed>=.10f)
            {regripSoundPlayed=true;GameAudio.Play(GameAudioCue.SwordRegrip,.54f);}
            if(!cutSoundPlayed&&elapsed>=CurrentStroke.contactStart-.065f)
            {cutSoundPlayed=true;GameAudio.Play((GameAudioCue)((int)GameAudioCue.SwordCut1+ComboStage),ComboStage==2?.94f:.82f);}
        }
        // Test the swept time interval so a long frame cannot skip the damage window.
        if (raiken == null && previous <= .42f && elapsed >= .17f) ApplyHits(previous, elapsed);
        if (raiken == null && elapsed >= Duration) IsAttacking = false;
    }
    public void HoldImpact(float seconds) { impactHold = Mathf.Max(impactHold, seconds); }
    public Vector3 ConsumeRootAdvance()
    {
        if(!IsAttacking || raiken==null) return Vector3.zero;
        float advance=CurrentStroke.advance*Mathf.SmoothStep(0,1,Mathf.InverseLerp(CurrentStroke.contactStart-.04f,CurrentStroke.contactEnd,elapsed));
        float delta=advance-consumedAdvance;consumedAdvance=advance;
        return forward*Mathf.Max(0,delta);
    }
    // Called after the full-body pose has moved the actual held blade.
    public void SampleBladeSweep(Vector3 previousGrip, Vector3 previousTip, Vector3 grip, Vector3 tip)
    {
        if (!IsAttacking || raiken == null || (player.Stance != null && !player.Stance.CanMelee)
            || (GameManager.Instance != null && !GameManager.Instance.IsCombatActive)) return;
        int steps = Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(Vector3.Distance(previousTip,tip),Vector3.Distance(previousGrip,grip))/.24f),1,24);
        for (int step=1;step<=steps;step++)
        {
            float t=step/(float)steps;
            Vector3 a=Vector3.Lerp(previousGrip,grip,t), b=Vector3.Lerp(previousTip,tip,t);
            // Ground-plane combat: retain actual horizontal blade reach, independent of visual boost height.
            a=Vector3.Lerp(a,b,.12f); a.y=b.y=transform.position.y+.1f;
            var actors=Damageable.Active;
            for(int i=actors.Count-1;i>=0;i--)
            {
                if(i>=actors.Count)continue;
                var actor=actors[i];
                if(actor==null || actor.IsDead || actor.team==owner.team || hit.Contains(actor)) continue;
                var shape=actor.GetComponent<Collider>();
                if(shape==null || !shape.enabled || shape.bounds.min.y>transform.position.y+3.5f)continue;
                Vector3 point=ClosestOnSegment(a,b,actor.AimCenter);
                point.y=shape.bounds.center.y;
                Vector3 delta=shape.ClosestPoint(point)-point;delta.y=0;
                if(delta.sqrMagnitude>.22f*.22f || IsBlocked(actor))continue;
                Strike(actor,78f);
            }
        }
    }
    private static Vector3 ClosestOnSegment(Vector3 a,Vector3 b,Vector3 point)
    { Vector3 ab=b-a;point.y=a.y;return a+ab*Mathf.Clamp01(Vector3.Dot(point-a,ab)/Mathf.Max(.0001f,ab.sqrMagnitude)); }
    public void CompletePoseFrame()
    {
        if(!IsAttacking)return;
        if(queuedNext && ComboStage<2 && elapsed>=CurrentStroke.linkTime)
        {
            float carry=Mathf.Min(.08f,elapsed-CurrentStroke.linkTime);
            BeginStage(ComboStage+1,carry);
        }
        else if(elapsed>=CurrentStroke.duration){IsAttacking=false;queuedNext=false;nextAttack=Time.time+.02f;}
    }
    private bool IsBlocked(Damageable target)
    {
        Vector3 origin=transform.position+Vector3.up*1.6f, ray=target.AimCenter-origin;
        int count=Physics.RaycastNonAlloc(origin,ray.normalized,blockers,ray.magnitude,~0,QueryTriggerInteraction.Ignore);
        if(count==blockers.Length) return true;
        for(int i=0;i<count;i++) if(blockers[i].collider.GetComponentInParent<Damageable>()==null) return true;
        return false;
    }
    private void Strike(Damageable target,float basis)
    {
        hit.Add(target);
        var upgrades=player.weaponController.upgradeSystem;
        float scale=(player.stats!=null?player.stats.DamageMultiplier:1f)*(upgrades!=null?upgrades.DamageMultiplier:1f);
        float amount=basis*scale;
        target.TakeDamage(amount,new DamageInfo(gameObject,transform.position,owner,amount));
        StrikeHit?.Invoke(target,amount);
    }
    private void ApplyHits(float fromTime, float toTime)
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
            float reach = raiken != null ? raiken.Reach : 3.5f;
            if (offset.sqrMagnitude > reach * reach || Vector3.Dot(forward, offset.normalized) < (raiken != null ? .13f : .35f)) continue;
            Vector3 origin = transform.position + Vector3.up * 1.1f;
            Vector3 ray = target.AimCenter - origin;
            int count = Physics.RaycastNonAlloc(origin, ray.normalized, blockers, ray.magnitude, ~0, QueryTriggerInteraction.Ignore);
            bool blocked = count == blockers.Length;
            for (int j = 0; j < count; j++)
            {
                var actor = blockers[j].collider.GetComponentInParent<Damageable>();
                if (actor == null) { blocked = true; break; }
            }
            if (blocked) continue;
            Strike(target,42f);
        }
    }
    public void CancelAttack()
    {
        queuedNext=false;
        if (!IsAttacking) return;
        IsAttacking = false;
        GameAudio.StopSwordPreparation();
        AttackCancelled?.Invoke();
    }
    public void ClearComboQueue(){queuedNext=false;}
    public void ResetCooldown() { CancelAttack(); nextAttack = 0; }
    private void OnDisable() { CancelAttack(); }
}
