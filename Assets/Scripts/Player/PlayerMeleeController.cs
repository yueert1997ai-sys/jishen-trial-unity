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
    public float AttackSpeed=>player?.weaponController?.upgradeSystem?.BladeTempoMultiplier??1f;
    public int ComboStage { get; private set; }
    public bool NextQueued => queuedNext;
    public ValkyrComboProfile MotionProfile {get;private set;}
    public void ConfigureMotionProfile(ValkyrComboProfile profile)
    {if(IsAttacking)throw new InvalidOperationException("Cannot replace a live attack profile");MotionProfile=profile;}
    public ValkyrComboProfile.Stroke CurrentStroke => MotionProfile.Get(ComboStage);
    public bool IsCutting => IsAttacking && elapsed>=CurrentStroke.contactStart && elapsed<=CurrentStroke.contactEnd;
    public bool CanSheathe => !IsAttacking || elapsed>=CurrentStroke.contactEnd+CurrentStroke.sheatheDelay;
    public Vector3 AttackForward => forward;
    public float MovementScale
    {
        get
        {
            if(!IsAttacking)return 1;
            if(ImpactHeld)return 0;
            if(elapsed<CurrentStroke.contactStart)return ComboStage==2?.48f:.72f;
            if(elapsed<=CurrentStroke.contactEnd)return ComboStage==2?.24f:.40f;
            return Mathf.Lerp(.40f,1,Mathf.SmoothStep(0,1,Mathf.InverseLerp(CurrentStroke.contactEnd,CurrentStroke.contactEnd+.15f,elapsed)));
        }
    }
    public bool CanDashCancel => !IsAttacking || elapsed>=CurrentStroke.dashCancelFrom;
    public int ActionIdentity {get;private set;}
    private int cancellation;
    public bool IsDashSlash {get;private set;}
    public bool ImpactHeld => impactHold > 0 || heldFrame==Time.frameCount;
    public bool CanSteer => IsAttacking && !ImpactHeld && (elapsed<CurrentStroke.contactStart || lastPresentedElapsed>=CurrentStroke.contactEnd);
    public float CooldownRemaining => Mathf.Max(0, nextAttack - Time.time);
    public event Action AttackStarted;
    public event Action AttackCancelled;
    public event Action<Damageable, float> StrikeHit;
    private RaikenBladePresentation raiken;
    private PlayerController player;
    private Damageable owner;
    private float elapsed, nextAttack, impactHold, consumedAdvance;
    private float lastPresentedElapsed;
    private int heldFrame=-1;
    private Vector3 forward;
    private bool cutSoundPlayed;
    private bool queuedNext
    {
        get => player!=null && player.Actions.Pending(BufferedCombatAction.Combo,Time.time);
        set
        {
            if(player==null)return;
            if(value)player.Actions.Enqueue(BufferedCombatAction.Combo,Time.time,CombatRules.Current.ComboBuffer);
            else player.Actions.Cancel(BufferedCombatAction.Combo);
        }
    }
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
            if(ComboStage>=2)return false;
            if(elapsed>CurrentStroke.linkTime+.065f)return false;
            queuedNext=true;return true;
        }
        if(CooldownRemaining>0)return false;
        if (raiken == null) raiken = GetComponentInChildren<RaikenBladePresentation>();
        IsDashSlash=player.DashAttackReady;player.ConsumeDashAttack();
        BeginStage(0,0);return true;
    }
    private void BeginStage(int stage,float carry)
    {
        if(stage>0)IsDashSlash=false;
        ComboStage=stage;elapsed=carry;impactHold=consumedAdvance=lastPresentedElapsed=0;heldFrame=-1;queuedNext=false;cutSoundPlayed=false;
        nextAttack=0;
        forward = player.AimDirection;
        hit.Clear();
        IsAttacking = true;
        ActionIdentity++;cancellation=CombatRuntime.ActionGeneration;
        AttackStarted?.Invoke();
        if(stage==2)
            GameAudio.Play(raiken!=null?GameAudioCue.SwordWindup:GameAudioCue.Slash,raiken!=null?(stage==2?.38f:.32f):.28f,1);
    }
    private void Update()
    {
        if (!IsAttacking) return;
        if (cancellation!=CombatRuntime.ActionGeneration || owner.IsDead || player.IsDashing || (GameManager.Instance != null && !GameManager.Instance.IsCombatActive))
        { CancelAttack(); return; }
        // Local contact hold owns both the displayed pose and the attacker's motor.
        // Render/capture workload must not stretch or shorten its simulated duration.
        if (impactHold > 0)
        {
            heldFrame=Time.frameCount;
            player.Actions.Extend(BufferedCombatAction.Combo,Time.deltaTime);
            impactHold = Mathf.Max(0, impactHold - Time.deltaTime); return;
        }
        float previous = elapsed;
        float stageEnd = raiken != null ? CurrentStroke.duration : Duration;
        // Land exactly on the shared key, even on a long frame; LateUpdate presents it before linking.
        if(raiken!=null && queuedNext && elapsed<CurrentStroke.linkTime)stageEnd=CurrentStroke.linkTime;
        elapsed = Mathf.Min(elapsed + Time.deltaTime*AttackSpeed, stageEnd);
        if(raiken!=null)
        {
            float soundLead=ComboStage==2?.034f:.020f;
            if(!cutSoundPlayed&&elapsed>=CurrentStroke.contactStart-soundLead)
            {cutSoundPlayed=true;GameAudio.StopSwordPreparation();GameAudio.Play((GameAudioCue)((int)GameAudioCue.SwordCut1+ComboStage),ComboStage==2?.78f:.65f,AttackSpeed);}
        }
        // Test the swept time interval so a long frame cannot skip the damage window.
        if (raiken == null && previous <= .42f && elapsed >= .17f) ApplyHits(previous, elapsed);
        if (raiken == null && elapsed >= Duration) IsAttacking = false;
    }
    public void HoldImpact(float seconds) { impactHold = Mathf.Max(impactHold, seconds); }
    public void Steer(Vector3 point,float dt)
    {
        if(!CanSteer)return;
        Vector3 desired=Vector3.ProjectOnPlane(point-transform.position,Vector3.up);
        if(desired.sqrMagnitude<1.44f)return;
        player.AimAt(point,dt,elapsed<CurrentStroke.contactStart?540:360);
        forward=player.AimDirection;
    }
    // The sweep may discover contact between rendered frames. Hold that sampled pose, not the frame-end overshoot.
    public void LockContactPose(float time){if(ImpactHeld)elapsed=Mathf.Min(elapsed,Mathf.Max(CurrentStroke.contactStart,time));}
    public Vector3 ConsumeRootAdvance()
    {
        if(!IsAttacking || raiken==null || ImpactHeld) return Vector3.zero;
        float advance=(CurrentStroke.advance+(IsDashSlash?CombatRules.Current.DashSlashAdvance:0))*Mathf.SmoothStep(0,1,Mathf.InverseLerp(CurrentStroke.contactStart-.04f,CurrentStroke.contactEnd,elapsed));
        float delta=advance-consumedAdvance;consumedAdvance=Mathf.Max(consumedAdvance,advance);
        return forward*Mathf.Max(0,delta);
    }
    // Called after the full-body pose has moved the actual held blade.
    public void SampleBladeSweep(Vector3 previousGrip, Vector3 previousTip, Vector3 grip, Vector3 tip)
    {
        if (cancellation!=CombatRuntime.ActionGeneration || !IsAttacking || raiken == null || (player.Stance != null && !player.Stance.CanMelee)
            || (GameManager.Instance != null && !GameManager.Instance.IsCombatActive)) return;
        int steps = Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(Vector3.Distance(previousTip,tip),Vector3.Distance(previousGrip,grip))/.24f),1,24);
        for (int step=1;step<=steps;step++)
        {
            float t=step/(float)steps;
            Vector3 a=Vector3.Lerp(previousGrip,grip,t), b=Vector3.Lerp(previousTip,tip,t);
            Vector3 rawA=a,rawB=b;
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
                Vector3 travel=(grip+tip-previousGrip-previousTip)*.5f;travel.y=0;
                Vector3 tangent=travel.sqrMagnitude>.0001f?travel.normalized:Vector3.Cross(Vector3.up,forward);
                Vector3 normal=(transform.position-actor.transform.position).normalized;normal.y=0;
                Vector3 flatBlade=rawB-rawA;flatBlade.y=0;Vector3 toActor=actor.AimCenter-rawA;toActor.y=0;
                float along=Mathf.Clamp01(Vector3.Dot(toActor,flatBlade)/Mathf.Max(.001f,flatBlade.sqrMagnitude));
                Vector3 bladePoint=Vector3.Lerp(rawA,rawB,along);
                Vector3 outside=actor.AimCenter+normal*(shape.bounds.extents.magnitude+1);outside.y=Mathf.Clamp(bladePoint.y,shape.bounds.min.y+.15f,shape.bounds.max.y-.15f);
                Vector3 contact=shape.ClosestPoint(outside)+normal*.035f;
                Strike(actor,78f,contact,normal,tangent);
            }
        }
    }
    private static Vector3 ClosestOnSegment(Vector3 a,Vector3 b,Vector3 point)
    { Vector3 ab=b-a;point.y=a.y;return a+ab*Mathf.Clamp01(Vector3.Dot(point-a,ab)/Mathf.Max(.0001f,ab.sqrMagnitude)); }
    public void CompletePoseFrame()
    {
        if(!IsAttacking)return;
        lastPresentedElapsed=elapsed;
        if(queuedNext && ComboStage<2 && elapsed>=CurrentStroke.linkTime)
        {
            float carry=0;
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
    private void Strike(Damageable target,float basis,Vector3 contact=default,Vector3 normal=default,Vector3 tangent=default)
    {
        hit.Add(target);
        var upgrades=player.weaponController.upgradeSystem;
        float scale=(player.stats!=null?player.stats.DamageMultiplier:1f)*(upgrades!=null?upgrades.DamageMultiplier:1f);
        float amount=basis*scale*(ComboStage==2?1.6f:ComboStage==1?.95f:1);
        var damageInfo = new DamageInfo(gameObject,transform.position,owner,amount)
        { Kind=CombatHitKind.Melee,HasContact=normal.sqrMagnitude>.01f,ContactPoint=contact,ContactNormal=normal,ContactTangent=tangent,MeleeStrike = true, Impact = ComboStage == 2 ? 95f : ComboStage == 1 ? 38f : 42f, HeavyImpact = ComboStage == 2 };
        var result=target.ApplyDamage(amount,damageInfo);
        if(!result.Applied)return;
        StrikeHit?.Invoke(target,damageInfo.Amount);
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
        IsDashSlash=false;
        queuedNext=false;
        impactHold=consumedAdvance=0;
        heldFrame=-1;
        if (!IsAttacking) return;
        IsAttacking = false;
        GameAudio.StopSwordSwings();
        AttackCancelled?.Invoke();
    }
    public void ClearComboQueue(){queuedNext=false;}
    public void ResetCooldown() { CancelAttack(); nextAttack = 0; }
    private void OnDisable() { CancelAttack(); }
}
