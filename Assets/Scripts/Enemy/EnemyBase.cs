using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public enum EnemyKind
{
    Melee,
    Ranged,
    Drone,
    Elite
}

public class EnemyBase : MonoBehaviour
{
    public EnemyKind kind = EnemyKind.Melee;
    public Transform target;
    public StageManager stageManager;
    public float moveSpeed = 3.3f;
    public float attackRange = 1.35f;
    public float contactDamage = 12f;
    public float fireInterval = 1.25f;
    public int killReward = 5;
    public Transform rifleMuzzle;
    public bool TrainingTarget { get; set; }

    private Damageable damageable;
    private float nextAttackTime;
    private bool detonating;
    private NavMeshAgent navigation;
    private float nextPathTime;
    private readonly EnemyAttackCycle attackCycle = new EnemyAttackCycle();
    private bool attacking => attackCycle.Phase==EnemyAttackPhase.Windup || attackCycle.Phase==EnemyAttackPhase.Commit;
    public EnemyAttackPhase AttackPhase => attackCycle.Phase;
    public int AttackGeneration => attackCycle.Generation;
    private float meleeStagger;
    private Vector3 meleePush;
    private float pushDuration;
    private TelegraphVisual attackWarning;
    private int warningGeneration;
    public float HitStaggerRemaining => meleeStagger;
    public bool HasAttackWarning => attackWarning!=null&&attackWarning.gameObject.activeSelf&&attackWarning.Generation==warningGeneration;
    private void TrackWarning(TelegraphVisual warning){attackWarning=warning;warningGeneration=warning.Generation;}
    private void ClearAttackWarning(){if(attackWarning!=null)attackWarning.Cancel(warningGeneration);attackWarning=null;}
    public ImpactStability Stability { get; private set; }
    public ArmorHealth Armor {get;private set;}
    private float recoveryUntil, evasionUntil, nextEvasionTime;
    private float nextStaggerTime,nextMeleeStaggerTime,nextHeavyStaggerTime;
    private int hitReactionPriority;
    private int evasionSide=1;
    private Vector3 repositionGoal; private float repositionUntil, nextLungeTime;
    private Vector3 observedTargetVelocity; private Vector3 lastTargetPosition; private float lastTargetSample;
    private Vector3 knownTarget, tacticalGoal;
    private float nextSenseTime, nextDecisionTime;
    private bool hasContact, hasTacticalGoal;
    private bool disengaging;
    public EnemyCombatBrain CombatBrain {get;set;}
    public Vector3 LastKnownTarget=>knownTarget;
    private Vector3 goalContact;
    private readonly HashSet<Damageable> lungeVictims = new HashSet<Damageable>();
    public Vector3 WeaponAimDirection { get; private set; }
    public bool IsDetonating => detonating;
    public int MeleeStrikesCompleted { get; private set; }
    private float strikePoseUntil;
    public float StrikePose => Mathf.Clamp01((strikePoseUntil-Time.time)/.24f);
    private TelegraphVisual lungeLandingWarning;
    private int lungeWarningGeneration;
    public bool TargetVisible { get; private set; }
    public Vector3 ObservedTargetVelocity => observedTargetVelocity;
    public Vector3 TacticalGoal => CombatBrain!=null&&CombatBrain.HasGoal?CombatBrain.Destination:tacticalGoal;
    public int LungesCompleted { get; private set; }
    private readonly RaycastHit[] sightHits=new RaycastHit[16];
    public bool UsesDirectionalArmor => kind==EnemyKind.Elite;
    public bool IsEvading => Time.time<evasionUntil;
    public bool IsInRecovery => Time.time<recoveryUntil || attackCycle.Phase==EnemyAttackPhase.Recovery;
    public bool GuardActive => UsesDirectionalArmor && damageable!=null && !damageable.IsDead
        && Armor!=null && Armor.Intact;
    public int EvasionsStarted { get; private set; }
    public Vector3 CommittedShotDirection { get; private set; }
    public int ShotsCommitted { get; private set; }
    public int ShotsEmitted { get; private set; }

    public float ResolveDirectionalArmor(float amount, DamageInfo info)
    {
        return amount; // Compatibility entry; finite armor resolves once in Damageable.
    }

    public bool TryEvadeMissile(Vector3 origin, Vector3 heading)
    {
        if(!isActiveAndEnabled || TrainingTarget || target==null
            || (kind!=EnemyKind.Ranged && kind!=EnemyKind.Elite) || damageable==null || damageable.IsDead
            || (GameManager.Instance!=null && !GameManager.Instance.IsCombatActive)
            || IsEvading || IsInRecovery || Time.time<nextEvasionTime || meleeStagger>0
            || (Stability!=null && Stability.IsBroken) || (attacking && AttackWindup>=.65f)
            || navigation==null || !navigation.enabled || !navigation.isOnNavMesh)return false;
        Vector3 incoming=damageable.AimCenter-origin;
        if(incoming.sqrMagnitude>6.5f*6.5f || incoming.sqrMagnitude<.04f
            || Vector3.Dot(heading.normalized,incoming.normalized)<.5f)return false;
        int count=Physics.RaycastNonAlloc(origin,incoming.normalized,sightHits,incoming.magnitude,~0,QueryTriggerInteraction.Ignore);
        if(count==sightHits.Length)return false;
        for(int i=0;i<count;i++)if(sightHits[i].collider.GetComponentInParent<Damageable>()==null)return false;
        Vector3 side=Vector3.Cross(Vector3.up,Vector3.ProjectOnPlane(heading,Vector3.up).normalized);
        if(side.sqrMagnitude<.1f)return false;
        for(int attempt=0;attempt<2;attempt++)
        {
            Vector3 desired=transform.position+side*(attempt==0?evasionSide:-evasionSide)*2.1f;
            if(!NavMesh.SamplePosition(desired,out var point,.35f,NavMesh.AllAreas)
                || navigation.Raycast(point.position,out _) || Vector3.Distance(transform.position,point.position)<1f)continue;
            CancelAttackWork();detonating=false;StopMoving();
            evasionUntil=Time.time+CombatLoopV2.MissileEvadeSeconds;
            recoveryUntil=evasionUntil+.55f;nextEvasionTime=Time.time+CombatLoopV2.MissileEvadeCooldown;
            nextAttackTime=Mathf.Max(nextAttackTime,recoveryUntil+.1f);
            navigation.isStopped=false;navigation.speed=6f;navigation.SetDestination(point.position);
            evasionSide=-evasionSide;EvasionsStarted++;
            return true;
        }
        return false;
    }

    private void ResetTactics()
    {CancelAttackWork();evasionUntil=recoveryUntil=nextEvasionTime=nextStaggerTime=nextMeleeStaggerTime=nextHeavyStaggerTime=0;hitReactionPriority=0;meleeStagger=0;meleePush=Vector3.zero;EvasionsStarted=ShotsEmitted=ShotsCommitted=MeleeStrikesCompleted=0;strikePoseUntil=0;evasionSide=1;
     repositionUntil=nextLungeTime=0;repositionGoal=Vector3.zero;observedTargetVelocity=Vector3.zero;lastTargetSample=-1;
     nextSenseTime=nextDecisionTime=0;hasContact=hasTacticalGoal=TargetVisible=false;LungesCompleted=0;EnemyTactics.ReleaseAttackSlot(this);}
    private void CancelAttackWork()
    {attackCycle.Cancel();ClearAttackWarning();detonating=false;disengaging=false;repositionUntil=0;hasTacticalGoal=false;nextDecisionTime=nextPathTime=0;WeaponAimDirection=Vector3.zero;
     CombatBrain?.CancelMovement();
     if(lungeLandingWarning!=null)lungeLandingWarning.Cancel(lungeWarningGeneration);lungeLandingWarning=null;
     StopAllCoroutines();EnemyTactics.ReleaseAttackSlot(this);}
    public void ConfigureP0Role(EnemyKind role)
    {
        kind = role;
        ResetTactics();
        Armor=GetComponent<ArmorHealth>();
        if(role==EnemyKind.Elite)Armor=Armor??gameObject.AddComponent<ArmorHealth>();
        if(Armor!=null)Armor.Configure(role==EnemyKind.Elite?CombatRules.Current.EliteCapacity:0);
        if (role != EnemyKind.Drone)
        {
            var bar = GetComponent<WorldHealthBar>() ?? gameObject.AddComponent<WorldHealthBar>();
            bar.height = GetComponent<E01SoldierMotion>()!=null?4.9f:3.3f; bar.width = role == EnemyKind.Elite ? 2.35f : 2.0f;
            bar.alwaysVisible = true;
        }
        if (navigation != null) navigation.radius = GetComponent<E01SoldierMotion>()!=null?(role==EnemyKind.Elite?.95f:E01SoldierMotion.CombatRadius):(role==EnemyKind.Elite?.8f:.6f);
        if (role == EnemyKind.Melee) { moveSpeed = 5.4f; attackRange = 2.1f; contactDamage = 12f; }
        else if (role == EnemyKind.Elite) { moveSpeed = 3.72f; fireInterval = 1.8f; }
        if(role==EnemyKind.Ranged)moveSpeed=3.5f;
        if(UsesDirectionalArmor && GetComponent<EliteArmorPresentation>()==null)gameObject.AddComponent<EliteArmorPresentation>();
    }
    public void InterruptForStagger()
    {
        CancelAttackWork();detonating=false;
        evasionUntil=recoveryUntil=0;
        hitReactionPriority=3;meleeStagger = .24f; meleePush = Vector3.zero;
        StopMoving();
        nextAttackTime = Mathf.Max(nextAttackTime, Time.time + .4f);
    }
    public void ReceiveMeleeImpact(Vector3 direction,bool heavy,bool guarded=false)
    {
        if(damageable==null||damageable.IsDead)return;
        if(Armor!=null && Armor.Intact)return;
        if(guarded && GuardActive)return;
        evasionUntil=0;
        hitReactionPriority=3;meleeStagger=heavy?.30f:.18f;
        pushDuration=meleeStagger;
        direction=Vector3.ProjectOnPlane(direction,Vector3.up).normalized;
        meleePush=direction*(heavy?10f:5.6f);
        {
            if(kind==EnemyKind.Elite)meleePush*=.65f;
            ClearAttackWarning();StopMoving();
            nextAttackTime=Mathf.Max(nextAttackTime,Time.time+meleeStagger+.12f);
        }
        CancelAttackWork();
    }
    public bool ResolveHitReaction(DamageInfo info)
    {
        if(damageable==null || damageable.IsDead || Armor!=null&&Armor.Intact)return false;
        if(info.MeleeStrike)
        {
            if(Time.time<nextMeleeStaggerTime)return false;
            var forward=(transform.position-info.SourcePosition).normalized;
            ReceiveMeleeImpact(info.ContactTangent.sqrMagnitude>.01f?(forward*.6f+info.ContactTangent*.4f).normalized:forward,info.HeavyImpact);
            nextMeleeStaggerTime=Time.time+.12f;
        }
        else
        {
            bool heavy=info.HeavyImpact||info.Kind==CombatHitKind.HeavyRifle;
            if(Time.time<(heavy?nextHeavyStaggerTime:nextStaggerTime)||
                meleeStagger>0&&!(heavy&&hitReactionPriority==1))return false;
            CancelAttackWork();StopMoving();
            // Gunfire rocks the chassis: a short triangular push away from the shooter.
            Vector3 away=transform.position-info.SourcePosition;away.y=0;
            meleePush=away.sqrMagnitude>.01f?away.normalized*(heavy?2.4f:4.2f):Vector3.zero;
            hitReactionPriority=heavy?2:1;
            meleeStagger=heavy?.18f:.07f;pushDuration=meleeStagger;
            nextAttackTime=Time.time+meleeStagger+.12f;
            // More than a complete rifle windup: continuous fire cannot indefinitely deny attacks.
            nextStaggerTime=Time.time+1.15f;
            if(heavy)nextHeavyStaggerTime=nextStaggerTime;
        }
        return true;
    }
    private float attackDuration => attackCycle.Duration;
    public float AttackWindup => attackCycle.Progress(Time.time);

    public float DifficultyHealthMultiplier { get; private set; } = 1f;

    private void Awake()
    {
        damageable = GetComponent<Damageable>();
        navigation = gameObject.AddComponent<NavMeshAgent>();
        navigation.radius = kind == EnemyKind.Elite ? 0.8f : 0.6f;
        navigation.height = rifleMuzzle != null ? 2.8f : 2.5f;
        navigation.updateRotation = false;
        navigation.acceleration = 32f;
        navigation.angularSpeed = 540f;
        navigation.stoppingDistance = 0.15f;
        navigation.obstacleAvoidanceType = ObstacleAvoidanceType.MedQualityObstacleAvoidance;
    }

    private void OnEnable()
    {
        if (damageable != null)
        {
            damageable.OnDied += OnDied;
        }
    }

    private void OnDisable()
    {
        CancelAttackWork();
        ResetTactics();
        EnemyTactics.Forget(this);
        if (navigation != null && navigation.isOnNavMesh) navigation.ResetPath();
        if (damageable != null)
        {
            damageable.OnDied -= OnDied;
        }
    }

    public void Init(Transform targetTransform, StageManager ownerStage)
    {
        EnemyArmorPalette.Apply(gameObject);
        target = targetTransform;
        // Reinforcements enter alerted to the player's last reported position.
        knownTarget = target != null ? target.position : transform.position;
        hasContact = target != null;
        stageManager = ownerStage;
        if (GameManager.Instance != null && GameManager.Instance.equipmentLoop != null) GameManager.Instance.equipmentLoop.AttachCarrier(this);
        if (damageable != null && GameManager.Instance != null)
        {
            DifficultyHealthMultiplier = GameManager.Instance.EnemyHealthMultiplier;
            damageable.SetMaxHealth(damageable.maxHealth * DifficultyHealthMultiplier, true);
        }
        damageable.SetMaxHealth((kind==EnemyKind.Elite?156f:kind==EnemyKind.Ranged?86f:kind==EnemyKind.Drone?32f:110f)*DifficultyHealthMultiplier,true);
    }

    private void Update()
    {
        attackCycle.Tick(Time.time);
        if(attacking && !attackCycle.CurrentActionValid)
        {
            CancelAttackWork();StopMoving();nextAttackTime=Mathf.Max(nextAttackTime,Time.time+.35f);
        }
        if (navigation != null && navigation.enabled && navigation.isOnNavMesh)
            navigation.isStopped = target == null || damageable.IsDead || (Stability != null && Stability.IsBroken) || (GameManager.Instance != null && !GameManager.Instance.IsCombatActive);
        if (target == null || damageable == null || damageable.IsDead)
        {
            if(attacking || EnemyTactics.HasAttackSlot(this) || detonating) CancelAttackWork();
            StopMoving();
            return;
        }

        if (GameManager.Instance != null && !GameManager.Instance.IsCombatActive)
        {
            return;
        }

        TrackTargetMotion();

        if(meleeStagger>0)
        {
            float before=meleeStagger,step=Mathf.Min(before,Time.deltaTime);
            meleeStagger=Mathf.Max(0,before-step);
            float integrated=step*(before+meleeStagger)/(2*Mathf.Max(.001f,pushDuration));
            if(navigation!=null && navigation.enabled && navigation.isOnNavMesh)
            {navigation.isStopped=true;navigation.Move(meleePush*integrated);}
            return;
        }
        if (Stability != null && Stability.IsBroken) return;
        if(IsEvading)return;
        if(evasionUntil>0){evasionUntil=0;StopMoving();}
        if(IsInRecovery)
        {
            if(CombatBrain!=null)CombatBrain.RecoveryStep();
            else if(Time.time<repositionUntil)MoveToward(repositionGoal,.9f);
            else StopMoving();
            return;
        }
        if (attacking) return;

        Vector3 toTarget = knownTarget - transform.position;
        toTarget.y = 0f;
        float distance = toTarget.magnitude;
        if (distance > 0.05f)
        {
            Quaternion facing=Quaternion.LookRotation(toTarget.normalized,Vector3.up);
            if(!TargetVisible && navigation!=null && navigation.hasPath && navigation.desiredVelocity.sqrMagnitude>.2f)
                facing=Quaternion.LookRotation(navigation.desiredVelocity.normalized,Vector3.up);
            transform.rotation=Quaternion.RotateTowards(transform.rotation,facing,(UsesDirectionalArmor?CombatLoopV2.EliteTurnSpeed:540f)*Time.deltaTime);
        }

        if(CombatBrain!=null){CombatBrain.Tick();return;}
        if (kind == EnemyKind.Ranged || kind == EnemyKind.Elite)
        {
            UpdateRanged(toTarget, distance);
        }
        else
        {
            UpdateMelee(toTarget, distance);
        }
    }

    private void UpdateMelee(Vector3 toTarget, float distance)
    {
        if(kind==EnemyKind.Melee&&GetComponent<EnemyArsenal>()!=null&&TargetVisible&&distance>7&&distance<17&&Time.time>=nextAttackTime&&EnemyTactics.TryAcquireAttackSlot(this))
        {nextAttackTime=Time.time+3;StartCoroutine(RangedStrike());return;}
        if (kind == EnemyKind.Drone)
        {
            if (detonating)
            {
                return;
            }

            if (distance > attackRange || !TargetVisible)
            {
                MoveWithSeparation(toTarget.normalized, 1f);
            }
            else
            {
                StopMoving();
                if (EnemyTactics.TryAcquireAttackSlot(this)) StartCoroutine(DroneDetonation());
            }

            return;
        }

        if (distance > attackRange)
        {
            // Telegraphed gap closer when the player keeps basic melee out of reach.
            if (kind == EnemyKind.Melee && !EnemyTactics.HasAttackSlot(this)
                && TargetVisible && distance <= CombatRules.Current.MeleeLungeRange
                && Vector3.Dot(transform.forward,toTarget.normalized)>.88f
                && HasClearLunge(toTarget.normalized)
                && Time.time >= nextAttackTime && Time.time >= nextLungeTime
                && EnemyTactics.TryAcquireAttackSlot(this))
            {
                nextAttackTime = Time.time + 1.2f;
                nextLungeTime = Time.time + CombatRules.Current.MeleeLungeCooldown;
                StartCoroutine(MeleeLunge());
                return;
            }
            bool holdsSlot = EnemyTactics.CanAttack(this) && Time.time >= nextAttackTime;
            if (holdsSlot || !TargetVisible || distance > CombatRules.Current.SurroundRadiusMelee + attackRange)
            {
                Vector3 intercept = knownTarget + (TargetVisible ? observedTargetVelocity * .25f : Vector3.zero);
                Vector3 flank = EnemyTactics.Anchor(this, knownTarget, transform.position) - knownTarget;
                // A free attacker closes inside strike range even while its lunge is cooling down.
                // The outer surround anchor is only a waiting position, never an attack destination.
                Vector3 offset=TargetVisible && holdsSlot ? flank.normalized * attackRange * .45f : flank * .5f;
                MoveToward(intercept + offset, 1f);
            }
            else
            {
                MoveToAnchor();
            }

            return;
        }

        StopMoving();
        if (TargetVisible && Vector3.Dot(transform.forward,toTarget.normalized)>.7f
            && Time.time >= nextAttackTime && EnemyTactics.TryAcquireAttackSlot(this))
        {
            nextAttackTime = Time.time + 1.2f;
            StartCoroutine(MeleeStrike());
        }
    }

    private void UpdateRanged(Vector3 toTarget, float distance)
    {
        if (Time.time < repositionUntil)
        {
            MoveToward(repositionGoal, 0.9f);
            return;
        }

        float retreatRange=CombatRules.Current.SurroundRadiusRanged - 3f;
        disengaging=distance < (disengaging ? retreatRange+1.4f : retreatRange);
        bool tooClose = disengaging;
        if (!TargetVisible || tooClose || distance > 12f)
        {
            if (Time.time >= nextDecisionTime && (!hasTacticalGoal
                || Vector3.Distance(goalContact,knownTarget)>2f
                || Vector3.Distance(transform.position,tacticalGoal)<.8f
                || navigation!=null && !navigation.pathPending && navigation.pathStatus==NavMeshPathStatus.PathInvalid))
            {
                nextDecisionTime = Time.time + .45f;
                goalContact=knownTarget;
                hasTacticalGoal = EnemyTactics.FindFiringPosition(this, knownTarget, 1.5f, out tacticalGoal);
            }
            MoveToward(hasTacticalGoal ? tacticalGoal : knownTarget, 1f);
            // A pressured rifleman creates space; an armored elite can hold ground.
            if (!TargetVisible || distance>12f || !UsesDirectionalArmor && tooClose) return;
        }
        else if (hasTacticalGoal && Vector3.Distance(transform.position,tacticalGoal)>1.1f)
            MoveToward(tacticalGoal, 1f);
        else { hasTacticalGoal=false; StopMoving(); }

        if (TargetVisible && distance <= 15f && Time.time >= nextAttackTime
            && Vector3.Dot(transform.forward,toTarget.normalized)>(UsesDirectionalArmor?.9f:.75f)
            && EnemyTactics.ClearSight(transform.position,target.position)
            && EnemyTactics.TryAcquireAttackSlot(this))
        {
            nextAttackTime = Time.time + Mathf.Max(1.6f, fireInterval);
            StopMoving();
            StartCoroutine(RangedStrike());
        }
    }

    private void MoveToAnchor()
    {
        Vector3 anchor = EnemyTactics.Anchor(this, knownTarget, transform.position);
        Vector3 toAnchor = anchor - transform.position;
        toAnchor.y = 0f;
        if (toAnchor.sqrMagnitude < 0.25f)
        {
            StopMoving();
            return;
        }

        MoveToward(anchor, 0.8f);
    }

    private void MoveToward(Vector3 destination, float speedScale)
    {
        if (navigation == null || !navigation.enabled || !navigation.isOnNavMesh) return;
        navigation.isStopped = false;
        navigation.speed = moveSpeed * speedScale;
        if (Time.time < nextPathTime && navigation.hasPath) return;
        nextPathTime = Time.time + 0.2f;
        if (NavMesh.SamplePosition(destination, out var point, 3f, NavMesh.AllAreas)) navigation.SetDestination(point.position);
    }

    public void TacticalMove(Vector3 destination,float speedScale)=>MoveToward(destination,speedScale);
    public void TacticalStop()=>StopMoving();
    public bool TryTacticalAttack(bool melee)
    {
        if(!TargetVisible||attacking||IsInRecovery||meleeStagger>0||Time.time<nextAttackTime||!EnemySquad.CanBegin)return false;
        Vector3 delta=knownTarget-transform.position;delta.y=0;float distance=delta.magnitude;
        if(distance<.01f||Vector3.Dot(transform.forward,delta.normalized)<(UsesDirectionalArmor?.9f:.78f))return false;
        bool lunge=melee&&distance>attackRange;
        if(melee&&(lunge&&(distance>CombatRules.Current.MeleeLungeRange||Time.time<nextLungeTime||!HasClearLunge(delta.normalized))))return false;
        if(!EnemyTactics.ClearSight(transform.position,knownTarget)||!EnemyTactics.TryAcquireAttackSlot(this))return false;
        CombatBrain?.AttackStarted();StopMoving();
        nextAttackTime=Time.time+(melee?1.2f:Mathf.Max(1.6f,fireInterval));
        if(lunge){nextLungeTime=Time.time+CombatRules.Current.MeleeLungeCooldown;StartCoroutine(MeleeLunge());}
        else if(melee)StartCoroutine(MeleeStrike());else StartCoroutine(RangedStrike());
        return true;
    }

    private void TrackTargetMotion()
    {
        if (target == null || Time.time < nextSenseTime) return;
        nextSenseTime = Time.time + .1f;
        bool previouslyVisible = TargetVisible;
        TargetVisible = EnemyTactics.ClearSight(transform.position, target.position);
        if (!TargetVisible) { observedTargetVelocity=Vector3.zero; lastTargetSample=-1; return; }
        if (previouslyVisible && lastTargetSample >= 0)
        {
            float dt = Mathf.Max(.01f, Time.time - lastTargetSample);
            Vector3 delta = target.position - lastTargetPosition; delta.y=0;
            // Warps/restarts are observations, not velocities to extrapolate.
            observedTargetVelocity = delta.magnitude > 110f * dt ? Vector3.zero
                : Vector3.Lerp(observedTargetVelocity, Vector3.ClampMagnitude(delta / dt, 22f), .7f);
        }
        else observedTargetVelocity = Vector3.zero;
        knownTarget = lastTargetPosition = target.position;
        hasContact = true; lastTargetSample = Time.time;
    }

    // Burst shots re-aim between rounds with bounded motion lead so a moving
    // player still faces real pressure while a standing still one is untouched.
    private Vector3 AimWithLead(Vector3 origin)
    {
        Vector3 toTarget = target.position - origin;
        toTarget.y = 0f;
        Vector3 direction = toTarget.sqrMagnitude > 0.01f ? toTarget.normalized : transform.forward;
        float distance = toTarget.magnitude;
        float shotSpeed = GetComponent<EnemyArsenal>()?.ShotSpeed??(kind == EnemyKind.Elite ? 12f : 10f);
        if (distance > 0.5f)
        {
            float flightTime = distance / shotSpeed;
            Vector3 predicted = target.position
                + Vector3.ClampMagnitude(observedTargetVelocity, 8f) * (CombatRules.Current.RangedAimLead * flightTime);
            Vector3 toPredicted = predicted - origin;
            toPredicted.y = 0f;
            if (toPredicted.sqrMagnitude > 0.01f) direction = toPredicted.normalized;
        }
        if (UsesDirectionalArmor) direction = transform.forward;
        return direction;
    }

    private void BeginReposition()
    {
        if(CombatBrain!=null){CombatBrain.AttackCompleted();return;}
        if (navigation == null || !navigation.isOnNavMesh || target == null) return;
        if (hasContact && EnemyTactics.FindFiringPosition(this, knownTarget, 2f, out var point))
        {
            repositionGoal = tacticalGoal = point;
            hasTacticalGoal = true;
            repositionUntil = Time.time + CombatRules.Current.RangedRepositionSeconds;
        }
    }

    private bool HasClearLunge(Vector3 direction)
    {
        if (navigation==null || !navigation.isOnNavMesh) return false;
        Vector3 landing=transform.position+direction*CombatRules.Current.MeleeLungeDistance;
        return NavMesh.SamplePosition(landing,out var hit,.3f,NavMesh.AllAreas)
            && !navigation.Raycast(hit.position,out _) && EnemyTactics.ClearSight(transform.position,landing);
    }

    private IEnumerator MeleeLunge()
    {
        var rules = CombatRules.Current;
        Vector3 start = transform.position;
        Vector3 locked = target.position - start;
        locked.y = 0f;
        locked = locked.sqrMagnitude > 0.01f ? locked.normalized : transform.forward;
        Vector3 landing = start + locked * rules.MeleeLungeDistance;
        float radius = 1.9f;
        int token = BeginWindup(rules.MeleeLungeWindup, locked);
        lungeVictims.Clear();
        GameAudio.PlayAt(GameAudioCue.Warning, transform.position, 0.3f);
        StopMoving();
        lungeLandingWarning=CombatEffects.Disc(landing, radius, attackDuration, new Color(1f, 0.4f, 0.06f));
        lungeWarningGeneration=lungeLandingWarning.Generation;
        TrackWarning(CombatEffects.Line(start, locked, rules.MeleeLungeDistance, 0.18f, attackDuration, new Color(1f, 0.32f, 0.1f)));
        yield return new WaitForSeconds(attackDuration);
        attackWarning=null;lungeLandingWarning=null;
        if(!CommitAttack(token))yield break;
        StopMoving();
        float elapsed = 0f;
        const float travel = 0.22f;
        while (elapsed < travel)
        {
            if (damageable.IsDead) yield break;
            if(GameManager.Instance!=null && GameManager.Instance.IsPaused){yield return null;continue;}
            if(!attackCycle.IsCommitted(token) || GameManager.Instance!=null&&!GameManager.Instance.IsCombatActive)
            {CancelAttackWork();yield break;}
            Vector3 previous=transform.position;
            elapsed += Time.deltaTime;
            Vector3 next = Vector3.Lerp(start, landing, elapsed / travel);
            if (navigation == null || !navigation.isOnNavMesh) break;
            if (navigation.Raycast(next,out var blocked)) next=blocked.position;
            navigation.Move(next-transform.position);
            // Resolve contact while the body passes the victim. An end-of-lunge test
            // incorrectly hit players who stepped into the already traversed trail.
            var contacts=Damageable.Active;
            for(int i=contacts.Count-1;i>=0;i--)
            {
                var victim=contacts[i];
                if(victim==null || victim.team==1 || victim.IsDead || lungeVictims.Contains(victim))continue;
                if(BossController.InsideSweptDisc(victim.transform.position,previous,transform.position,radius)
                    && EnemyTactics.ClearSight(transform.position,victim.transform.position))
                {lungeVictims.Add(victim);victim.TakeDamage(rules.MeleeLungeDamage,new DamageInfo(gameObject,transform.position,damageable,rules.MeleeLungeDamage));}
            }
            CombatEffects.Thrust(transform.position + Vector3.up * 0.6f, locked, true);
            yield return null;
        }

        CombatEffects.Impact(transform.position, new Color(1f, 0.55f, 0.15f), 1.6f, true);
        attackCycle.Complete(Time.time, rules.MeleeRecovery);
        EnemyTactics.ReleaseAttackSlot(this);LungesCompleted++;
        CombatBrain?.AttackCompleted();
    }

    private IEnumerator MeleeStrike()
    {
        int token=BeginWindup(CombatRules.Current.MeleeWindup);
        GameAudio.PlayAt(GameAudioCue.Warning,transform.position,.24f);
        Vector3 center = transform.position;
        float radius = Mathf.Max(1.7f, attackRange + 0.2f);
        TrackWarning(CombatEffects.Disc(center, radius, attackDuration, new Color(1f, 0.32f, 0.1f)));
        yield return new WaitForSeconds(attackDuration);
        attackWarning=null;
        if(!CommitAttack(token))yield break;
        Vector3 delta = target.position - center;
        strikePoseUntil=Time.time+.24f;
        delta.y = 0;
        if (!damageable.IsDead && delta.sqrMagnitude <= radius * radius
            && EnemyTactics.ClearSight(center,target.position))
            target.GetComponent<Damageable>().TakeDamage(contactDamage, new DamageInfo(gameObject, center, damageable, contactDamage));
        CombatEffects.Impact(center + transform.forward, new Color(1f, 0.6f, 0.2f), 0.45f);
        attackCycle.Complete(Time.time,CombatRules.Current.MeleeRecovery);
        EnemyTactics.ReleaseAttackSlot(this);MeleeStrikesCompleted++;
        CombatBrain?.AttackCompleted();
    }

    private IEnumerator RangedStrike()
    {
        var rules = CombatRules.Current;
        Vector3 direction = UsesDirectionalArmor ? transform.forward : AimWithLead(transform.position);
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.01f)
        {
            direction = transform.forward;
        }

        Vector3 origin = rifleMuzzle!=null?rifleMuzzle.position:transform.position + Vector3.up * 0.9f + direction.normalized * 0.75f;
        if(UsesDirectionalArmor)direction=transform.forward;
        CommittedShotDirection=direction.normalized;
        WeaponAimDirection=CommittedShotDirection;
        Color color = kind == EnemyKind.Elite ? new Color(1f, 0.25f, 0.42f) : new Color(1f, 0.35f, 0.08f);
        var arsenal=GetComponent<EnemyArsenal>();
        int token=BeginWindup(arsenal?.Windup??(UsesDirectionalArmor?rules.EliteWindup:rules.RangedWindup),CommittedShotDirection);
        if(kind==EnemyKind.Elite)GameAudio.PlayAt(GameAudioCue.Warning,origin,.28f);
        TrackWarning(CombatEffects.Line(origin, direction, 18f, 0.24f, attackDuration, color));
        yield return new WaitForSeconds(attackDuration);
        attackWarning=null;
        if(!CommitAttack(token))yield break;
        if(!UsesDirectionalArmor && !EnemyTactics.ClearSight(transform.position,target.position))
        {attackCycle.Complete(Time.time,rules.RangedRecovery);EnemyTactics.ReleaseAttackSlot(this);BeginReposition();yield break;}
        direction=attackCycle.Direction;
        float damage = kind == EnemyKind.Elite ? 9f : 6f;
        var carrier = GetComponent<SalvageCarrier>();
        bool scatter = carrier != null && carrier.gear != null && carrier.gear.id == "scatter";
        bool swarm = carrier != null && carrier.gear != null && carrier.gear.id == "salvo";
        int shots = scatter ? 5 : swarm ? 4 : 1;
        // Salvaged multi-shot gear replaces the burst; base guns fire bursts instead.
        int bursts = arsenal?.BurstCount??(shots > 1 ? 1 : rules.RangedBurstShots);
        int fired=0;
        for (int burst = 0; burst < bursts; burst++)
        {
            if (burst > 0)
            {
                yield return new WaitForSeconds(rules.RangedBurstInterval);
                if (damageable.IsDead || target == null || (GameManager.Instance != null && !GameManager.Instance.IsCombatActive)) break;
                if (!UsesDirectionalArmor && !EnemyTactics.ClearSight(transform.position,target.position)) break;
                // Elites commit their facing for the whole volley: the announced
                // line stays honest and the flank remains the counterplay.
                if (!UsesDirectionalArmor) direction = AimWithLead(origin);
            }
            WeaponAimDirection=direction.normalized;
            var pose=GetComponent<E01SoldierMotion>();
            if(arsenal==null||arsenal.Model==null)pose?.SynchronizeForFire(WeaponAimDirection);
            if(rifleMuzzle!=null)origin=rifleMuzzle.position;
            pose?.Recoil();
            if(arsenal!=null)
            {arsenal.Fire(HeavyArsenal(arsenal)?CommittedShotDirection:direction);fired++;ShotsEmitted++;continue;}
            ProjectileVisuals.SpawnMuzzleFlash(origin,direction.normalized,color,.24f);
            GameAudio.PlayAt(GameAudioCue.EnemyShot,origin,kind==EnemyKind.Elite?.45f:.36f);
            for (int i = 0; i < shots; i++)
            {
                float angle = shots == 1 ? 0 : Mathf.Lerp(-18, 18, i / (float)(shots - 1));
                var projectile = ProjectilePool.Spawn(false, "EnemyProjectile", origin, color, .24f);
                projectile.Init(1, damageable, Quaternion.AngleAxis(angle, Vector3.up) * direction.normalized,
                    damage * (shots > 1 ? .55f : 1f), kind == EnemyKind.Elite ? 12f : 10f, 3f, 0f, 0);
                fired++;ShotsEmitted++;
            }
        }
        attackCycle.Complete(Time.time,UsesDirectionalArmor?rules.EliteRecovery:rules.RangedRecovery);
        EnemyTactics.ReleaseAttackSlot(this);
        ShotsCommitted+=fired;
        WeaponAimDirection=Vector3.zero;
        if (CombatBrain!=null||!UsesDirectionalArmor) BeginReposition();
    }

    static bool HeavyArsenal(EnemyArsenal a)=>a.Heavy||a.Spec.weapon==EnemyWeapon.M14;
    private int BeginWindup(float duration, Vector3 direction=default)
    {
        return attackCycle.Begin(Time.time,duration,direction);
    }
    private bool CommitAttack(int token)
    {
        bool canAct=damageable!=null&&!damageable.IsDead&&target!=null
            && (GameManager.Instance==null || GameManager.Instance.IsCombatActive)
            && (Stability==null || !Stability.IsBroken) && meleeStagger<=0;
        bool committed=attackCycle.TryCommit(token,Time.time,canAct);
        if(!committed){attackCycle.Cancel();EnemyTactics.ReleaseAttackSlot(this);}
        return committed;
    }

    private void Explode()
    {
        var actors = Damageable.Active;
        for (int i = actors.Count - 1; i >= 0; i--)
        {
            Damageable targetDamageable = actors[i];
            if (targetDamageable == null || targetDamageable.team == 1 || targetDamageable.IsDead)
            {
                continue;
            }

            Vector3 delta = targetDamageable.transform.position - transform.position;
            delta.y = 0;
            if (delta.sqrMagnitude <= 2.2f * 2.2f && EnemyTactics.ClearSight(transform.position,targetDamageable.transform.position))
                targetDamageable.TakeDamage(16f, new DamageInfo(gameObject, transform.position, damageable, 16f));
        }

        damageable.Kill(new DamageInfo(gameObject, transform.position, damageable, 999f));
    }

    private IEnumerator DroneDetonation()
    {
        detonating = true;
        int token=BeginWindup(.55f);
        TrackWarning(CombatEffects.Disc(transform.position, 2.2f, 0.55f, new Color(1f, 0.2f, 0.015f, 1f)));
        GameAudio.Play(GameAudioCue.Warning, 0.34f, 1.2f);
        yield return new WaitForSeconds(0.55f);
        if(CommitAttack(token))
        {
            Explode();
        }
        detonating=false;
    }

    private void MoveWithSeparation(Vector3 desiredDirection, float speedScale)
    {
        if (navigation == null || !navigation.isOnNavMesh) return;
        navigation.speed = moveSpeed * speedScale;
        if (Time.time < nextPathTime) return;
        nextPathTime = Time.time + 0.2f;
        bool approaching = Vector3.Dot(desiredDirection, target.position - transform.position) > 0f;
        Vector3 destination = approaching ? knownTarget : transform.position + desiredDirection * 3f;
        if (NavMesh.SamplePosition(destination, out var point, 3f, NavMesh.AllAreas)) navigation.SetDestination(point.position);
    }

    private void StopMoving() { if (navigation != null && navigation.enabled && navigation.isOnNavMesh) {navigation.isStopped=true;if(navigation.hasPath)navigation.ResetPath();} }

    public bool RecoverNavigation(Vector3 point)
    {
        if(damageable==null||damageable.IsDead||navigation==null||!navigation.enabled)return false;
        CancelAttackWork();StopMoving();detonating=false;meleePush=Vector3.zero;meleeStagger=0;
        evasionUntil=recoveryUntil=0;
        if(!navigation.Warp(point))return false;
        nextAttackTime=Time.time+1.1f;nextPathTime=Time.time+.7f;
        return true;
    }

    private void OnDied(Damageable dead)
    {
        {
            CancelAttackWork();detonating=false;
            meleeStagger=0;meleePush=Vector3.zero;StopMoving();
            evasionUntil=recoveryUntil=0;
            EnemyTactics.Forget(this);
        }
        if(TrainingTarget)return;
        foreach(var collider in GetComponents<Collider>())collider.enabled=false;
        if (GameManager.Instance != null && GameManager.Instance.equipmentLoop != null) GameManager.Instance.equipmentLoop.DropFrom(this);
        if (stageManager != null)
        {
            stageManager.NotifyEnemyKilled();
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.RegisterKill(killReward,transform.position);
        }
    }
}
