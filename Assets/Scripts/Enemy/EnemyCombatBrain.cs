using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public enum EnemyIntent { Approach, Flank, Fire, Relocate, Disengage, TakeCover, Peek, Search, Recover }
public enum EnemyBattleRole { Assault, Marksman, Raider, Skirmisher, Artillery }

public readonly struct EnemyBattleProfile
{
    public readonly EnemyBattleRole role;
    public readonly float minimum,preferred,maximum,movement;
    public EnemyBattleProfile(EnemyBattleRole role,float minimum,float preferred,float maximum,float movement)
    {this.role=role;this.minimum=minimum;this.preferred=preferred;this.maximum=maximum;this.movement=movement;}
    public static EnemyBattleProfile For(EnemySpawnSpec spec)
    {
        if(spec.closeAssault||spec.role==EnemyKind.Melee)return new EnemyBattleProfile(EnemyBattleRole.Raider,0,2.1f,16,1);
        if(spec.body=="DOM")return new EnemyBattleProfile(EnemyBattleRole.Skirmisher,5,10.5f,17,1.10f);
        if(spec.Heavy)return new EnemyBattleProfile(EnemyBattleRole.Artillery,7,14.5f,21,.90f);
        if(spec.weapon==EnemyWeapon.M14)return new EnemyBattleProfile(EnemyBattleRole.Marksman,6,12.5f,19,1);
        if(spec.weapon==EnemyWeapon.Rocket||spec.weapon==EnemyWeapon.Missiles)return new EnemyBattleProfile(EnemyBattleRole.Skirmisher,5,10,17,1.08f);
        return new EnemyBattleProfile(EnemyBattleRole.Assault,3.8f,8.2f,14,1.08f);
    }
}

// Shared observations and destination reservations, not shared omniscient player tracking.
public static class EnemySquad
{
    static readonly List<EnemyCombatBrain> members=new List<EnemyCombatBrain>();
    static int generation=-1,serial;static float nextAttack;
    public static int Count=>members.Count;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Reset(){members.Clear();generation=-1;serial=0;nextAttack=0;}
    public static int Join(EnemyCombatBrain member)
    {
        if(generation!=CombatRuntime.Generation){Reset();generation=CombatRuntime.Generation;}
        if(!members.Contains(member))members.Add(member);
        return ++serial;
    }
    public static void Leave(EnemyCombatBrain member){members.Remove(member);}
    public static bool CanBegin=>Time.time>=nextAttack;
    public static void Committed(){nextAttack=Time.time+.16f;}
    public static void Pressure(EnemyCombatBrain victim,Vector3 shooter)
    {
        foreach(var ally in members)
            if(ally!=null&&ally!=victim&&ally.Actor.TargetVisible
                &&Vector3.Distance(ally.transform.position,victim.transform.position)<19
                &&EnemyTactics.ClearSight(ally.transform.position,victim.transform.position))ally.ObserveAllyPressure(shooter);
    }
    public static float PositionCost(EnemyCombatBrain self,Vector3 point,Vector3 contact)
    {
        float cost=0;var bearing=(point-contact).normalized;
        foreach(var other in members)
        {
            if(other==null||other==self||other.Actor==null)continue;
            var occupied=other.HasGoal?other.Destination:other.transform.position;
            cost+=Mathf.Max(0,3.5f-Vector3.Distance(occupied,point))*4;
            if(other.Actor.AttackPhase==EnemyAttackPhase.Windup||other.Actor.AttackPhase==EnemyAttackPhase.Commit)
                cost+=Mathf.Max(0,Vector3.Dot(bearing,(occupied-contact).normalized)-.6f)*7;
        }
        return cost;
    }
}

// Original movement/decision implementation informed by Hades' readable attack/retreat/peek
// state sequencing. Actual damage, telegraphs, stun and attack cancellation stay in EnemyBase.
public sealed class EnemyCombatBrain : MonoBehaviour
{
    public EnemyBase Actor {get;private set;}
    public EnemyBattleProfile Profile {get;private set;}
    public EnemyIntent Intent {get;private set;}
    public Vector3 Destination {get;private set;}
    public bool HasGoal {get;private set;}
    public int Plans {get;private set;}
    public int PressureReactions {get;private set;}
    public int AllyReactions {get;private set;}
    public int Repaths {get;private set;}
    public int Retreats {get;private set;}
    public int Volleys {get;private set;}
    NavMeshAgent agent;Damageable health;PlayerController player;
    System.Random random;int side;float angleBias,nextThink,goalUntil,holdUntil,pressureAt=-10;
    float nextDefense,nextReport,supportUntil,nextSupport,plantUntil,nextProgress;Vector3 lastProgress,plannedContact;
    bool pendingPressure,holdingCover;int lastActionGeneration;
    static NavMeshPath path;static readonly Vector3[] corners=new Vector3[32];
    static float Distance(Vector3 a,Vector3 b){a.y=b.y=0;return Vector3.Distance(a,b);}
    public void Configure(EnemySpawnSpec spec)
    {
        Actor=GetComponent<EnemyBase>();agent=GetComponent<NavMeshAgent>();health=GetComponent<Damageable>();
        Profile=EnemyBattleProfile.For(spec);int identity=EnemySquad.Join(this);
        if(Profile.role==EnemyBattleRole.Raider){Actor.attackRange=2.1f;Actor.moveSpeed=5.4f;}
        random=new System.Random(unchecked((CombatRuntime.Run?.Seed??1)*397^identity*7919));
        side=random.Next(2)==0?-1:1;angleBias=35+(float)random.NextDouble()*40;
        nextThink=Time.time+.12f+identity%4*.035f;lastActionGeneration=CombatRuntime.ActionGeneration;
        health.OnDamaged+=OnHit;health.OnDied+=OnDeath;Intent=EnemyIntent.Approach;
        // Initial routes have a stable seeded preference, independent of effect randomness.
    }
    void OnHit(Damageable owner,DamageInfo hit)
    {
        if(owner.IsDead||Actor.TrainingTarget)return;
        if(!pendingPressure){pressureAt=Time.time;pendingPressure=true;}
        if(Time.time>=nextReport&&Actor.TargetVisible)
        {nextReport=Time.time+.8f;EnemySquad.Pressure(this,hit.SourcePosition);}
    }
    void OnDeath(Damageable owner){CancelMovement();EnemySquad.Leave(this);}
    void OnDisable(){CancelMovement();EnemySquad.Leave(this);}
    void OnDestroy(){if(health!=null){health.OnDamaged-=OnHit;health.OnDied-=OnDeath;}}
    public void ObserveAllyPressure(Vector3 observedShooter)
    {if(Time.time<nextSupport)return;supportUntil=Time.time+1.5f;nextSupport=Time.time+2.7f;}
    public void CancelMovement()
    {HasGoal=holdingCover=false;nextThink=Time.time+.16f;}
    public void AttackStarted(){HasGoal=holdingCover=false;Intent=EnemyIntent.Fire;EnemySquad.Committed();}
    public void AttackCompleted()
    {
        if(Actor==null||health.IsDead)return;
        Volleys++;pendingPressure=false;side=Volleys%2==0?-side:side;
        float offset=Profile.role==EnemyBattleRole.Raider?2.5f:Profile.preferred;
        var purpose=Time.time<supportUntil?EnemyIntent.Flank:EnemyIntent.Relocate;
        if(purpose==EnemyIntent.Flank){AllyReactions++;supportUntil=0;}
        if(FindPosition(Actor.LastKnownTarget,purpose,offset,1.8f,out var destination))
            SetGoal(destination,purpose,Profile.role==EnemyBattleRole.Artillery?.6f:1.05f);
        else Intent=EnemyIntent.Recover;
        plantUntil=Time.time+.85f;
    }
    public void RecoveryStep()
    {
        if(HasGoal&&Time.time<goalUntil)Actor.TacticalMove(Destination,.85f);
        else Actor.TacticalStop();
    }
    public void Tick()
    {
        if(Actor.TrainingTarget){Actor.TacticalStop();return;}
        if(lastActionGeneration!=CombatRuntime.ActionGeneration)
        {CancelMovement();pendingPressure=false;supportUntil=0;lastActionGeneration=CombatRuntime.ActionGeneration;}
        if(player==null&&Actor.target!=null)player=Actor.target.GetComponent<PlayerController>();
        Vector3 contact=Actor.LastKnownTarget;float distance=Distance(transform.position,contact);
        if(HasGoal)
        {
            float remaining=Distance(transform.position,Destination);
            if(Time.time>=nextProgress)
            {
                if(remaining>1&&Time.time<goalUntil&&Vector3.Distance(lastProgress,transform.position)<.10f&&!agent.pathPending)
                {Repaths++;side=-side;goalUntil=Time.time;}
                lastProgress=transform.position;nextProgress=Time.time+.65f;
            }
            bool contactMoved=Vector3.Distance(plannedContact,contact)>4&&Intent!=EnemyIntent.Disengage&&Intent!=EnemyIntent.TakeCover;
            if(remaining<.75f||Time.time>=goalUntil||contactMoved)
            {
                HasGoal=false;Actor.TacticalStop();
                if(Intent==EnemyIntent.TakeCover&&!Actor.TargetVisible)
                {holdingCover=true;holdUntil=Time.time+.45f+(float)random.NextDouble()*.2f;}
                nextThink=Time.time;
            }
            else if(Intent!=EnemyIntent.Approach)
            {Actor.TacticalMove(Destination,Intent==EnemyIntent.Disengage?1.12f:Profile.movement);return;}
        }
        if(holdingCover)
        {
            Actor.TacticalStop();if(Time.time<holdUntil&&distance>4)return;
            holdingCover=false;
            if(FindPosition(contact,EnemyIntent.Peek,Profile.preferred,1,out var peek))
            {SetGoal(peek,EnemyIntent.Peek,2.4f);return;}
        }
        if(Time.time<nextThink){if(HasGoal)Actor.TacticalMove(Destination,Profile.movement);return;}
        nextThink=Time.time+.16f;
        if(!Actor.TargetVisible)
        {
            if(!HasGoal&&FindPosition(contact,EnemyIntent.Search,Mathf.Min(9,Profile.preferred),1,out var search))
                SetGoal(search,EnemyIntent.Search,3.2f);
            else if(!HasGoal)SetGoal(contact,EnemyIntent.Search,1.4f);
            if(HasGoal)Actor.TacticalMove(Destination,Profile.movement);
            return;
        }
        bool raider=Profile.role==EnemyBattleRole.Raider;
        bool closing=Vector3.Dot(Actor.ObservedTargetVelocity,transform.position-contact)>5
            ||player!=null&&player.Melee!=null&&player.Melee.IsAttacking;
        if(!raider&&Time.time>=nextDefense&&Time.time>=plantUntil
            &&(distance<Profile.minimum||closing&&distance<Profile.minimum+1.8f||pendingPressure&&Time.time-pressureAt>=.20f))
        {
            bool pressure=pendingPressure&&Time.time-pressureAt>=.20f;pendingPressure=false;
            var intent=distance>Profile.minimum+1&&pressure&&Profile.role!=EnemyBattleRole.Skirmisher?EnemyIntent.TakeCover:EnemyIntent.Disengage;
            if(FindPosition(contact,intent,Profile.preferred,2,out var escape)
                ||intent==EnemyIntent.TakeCover&&FindPosition(contact,EnemyIntent.Disengage,Profile.preferred,2,out escape))
            {
                PressureReactions+=pressure?1:0;Retreats++;nextDefense=Time.time+2.5f;
                SetGoal(escape,intent,1.15f);plantUntil=Time.time+2.15f;return;
            }
            nextDefense=Time.time+.7f;
        }
        if(Time.time<supportUntil&&!HasGoal)
        {
            supportUntil=0;
            if(FindPosition(contact,EnemyIntent.Flank,raider?3.8f:Profile.preferred,2,out var flank))
            {AllyReactions++;SetGoal(flank,EnemyIntent.Flank,1.1f);return;}
        }
        if(raider)
        {
            if(distance<=CombatRules.Current.MeleeLungeRange&&Actor.TryTacticalAttack(true))return;
            if(distance>8.5f&&distance<16&&Volleys%2==0&&Actor.TryTacticalAttack(false))return;
            var predicted=contact+Vector3.ClampMagnitude(Actor.ObservedTargetVelocity*.28f,2.8f);
            if(distance<5.5f)Actor.TacticalMove(predicted,1.05f);
            else if(!HasGoal&&FindPosition(predicted,EnemyIntent.Approach,3.2f,.8f,out var approach))
                SetGoal(approach,EnemyIntent.Approach,2.4f);
            else if(HasGoal)Actor.TacticalMove(Destination,1.05f);
            return;
        }
        if(distance<=Profile.maximum&&Actor.TryTacticalAttack(false))return;
        if(!HasGoal&&(distance>Profile.maximum||Time.time>=plantUntil))
        {
            if(FindPosition(contact,EnemyIntent.Approach,Profile.preferred,1.6f,out var approach))
                SetGoal(approach,EnemyIntent.Approach,2.5f);
        }
        if(HasGoal)Actor.TacticalMove(Destination,Profile.movement);else Actor.TacticalStop();
    }
    void SetGoal(Vector3 goal,EnemyIntent intent,float duration)
    {
        Destination=goal;Intent=intent;HasGoal=true;plannedContact=Actor.LastKnownTarget;Plans++;
        goalUntil=Time.time+duration;nextProgress=Time.time+.85f;lastProgress=transform.position;
        Actor.TacticalMove(Destination,intent==EnemyIntent.Disengage?1.12f:Profile.movement);
    }
    bool FindPosition(Vector3 contact,EnemyIntent purpose,float range,float minMove,out Vector3 bestPoint)
    {
        bestPoint=transform.position;if(agent==null||!agent.enabled||!agent.isOnNavMesh)return false;
        if(path==null)path=new NavMeshPath();float best=float.PositiveInfinity;
        Vector3 away=transform.position-contact;away.y=0;float bearing=Mathf.Atan2(away.x,away.z)*Mathf.Rad2Deg;
        float desired=bearing+side*(purpose==EnemyIntent.Approach?angleBias*.35f:angleBias);
        bool local=purpose==EnemyIntent.TakeCover||purpose==EnemyIntent.Disengage;
        int steps=local?12:16;
        for(int ring=0;ring<2;ring++)for(int i=0;i<steps;i++)
        {
            float angle=i*(360f/steps)+desired;
            Vector3 candidate=(local?transform.position:contact)+Quaternion.Euler(0,angle,0)*Vector3.forward*(local?3+ring*2.6f:range+ring*1.8f);
            if(!NavMesh.SamplePosition(candidate,out var nav,.65f,NavMesh.AllAreas)||Vector3.Distance(nav.position,transform.position)<minMove)continue;
            float targetDistance=Distance(nav.position,contact);
            if(purpose==EnemyIntent.Disengage&&targetDistance<away.magnitude+1)continue;
            bool visible=EnemyTactics.ClearSight(nav.position,contact);
            if(purpose==EnemyIntent.TakeCover?visible:!local&&!visible)continue;
            if(!NavMesh.CalculatePath(transform.position,nav.position,NavMesh.AllAreas,path)||path.status!=NavMeshPathStatus.PathComplete)continue;
            int n=path.GetCornersNonAlloc(corners);if(n==corners.Length)continue;
            float length=0;for(int j=1;j<n;j++)length+=Vector3.Distance(corners[j-1],corners[j]);
            if(length>24||local&&length>10)continue;
            float score=length*.65f+EnemySquad.PositionCost(this,nav.position,contact);
            if(!local)score+=Mathf.Abs(Mathf.DeltaAngle(angle,desired))*.045f+Mathf.Abs(targetDistance-range)*.35f;
            if(purpose==EnemyIntent.Disengage)score-=Mathf.Min(away.magnitude+4,targetDistance)*.7f;
            if(score<best){best=score;bestPoint=nav.position;}
        }
        return !float.IsPositiveInfinity(best);
    }
}
