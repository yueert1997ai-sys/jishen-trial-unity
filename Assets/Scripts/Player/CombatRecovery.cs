using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

// Recover positions, never complete an encounter, kill an actor or reset its equipment/health.
[DisallowMultipleComponent]
public sealed class CombatRecovery : MonoBehaviour
{
    public int PlayerRecoveries {get;private set;}
    public int EnemyRecoveries {get;private set;}
    public string Notice {get;private set;}
    public float NoticeUntil {get;private set;}
    public float BlockedSeconds {get;private set;}
    PlayerController player;
    readonly List<Vector3> safeTrail=new List<Vector3>(32);
    readonly Collider[] overlaps=new Collider[64];
    readonly Dictionary<EnemyBase,Watch> watches=new Dictionary<EnemyBase,Watch>();
    readonly NavMeshPath path=new NavMeshPath();
    class Watch {public Vector3 position;public float quiet;}
    int generation=-1;
    float retryAt,nextEnemies;
    void Awake(){player=GetComponent<PlayerController>();}
    void Update()
    {
        var gm=GameManager.Instance;
        if(gm==null||gm.Phase!=GamePhase.Combat||player.GetComponent<Damageable>().IsDead)return;
        if(player.InputRouter.readKeyboard&&Application.isFocused&&Input.GetKeyDown(KeyCode.F4))TryRecover(true);
        if(!gm.IsCombatActive||gm.IsPaused)return;
        if(Time.time>=nextEnemies){nextEnemies=Time.time+.5f;CheckEnemies(.5f);}
    }
    public bool IsSafe(Vector3 point,Transform owner=null)
    {
        if(float.IsNaN(point.x)||float.IsInfinity(point.x)||Mathf.Abs(point.x)>26.2f||Mathf.Abs(point.z)>26.2f)return false;
        if(!NavMesh.SamplePosition(point,out var nav,.35f,NavMesh.AllAreas)||Mathf.Abs(point.y-nav.position.y)>.3f)return false;
        float radius=owner==null||owner==transform?.79f:.86f;
        int count=Physics.OverlapCapsuleNonAlloc(point+Vector3.up*(radius+.16f),point+Vector3.up*2.55f,radius,overlaps,~0,QueryTriggerInteraction.Ignore);
        if(count==overlaps.Length)return false;
        for(int i=0;i<count;i++)
        {
            var c=overlaps[i];if(c.transform==owner||owner!=null&&c.transform.IsChildOf(owner))continue;
            if(c.bounds.max.y<=point.y+.16f)continue;
            return false;
        }
        return true;
    }
    void NewRun()
    {
        if(generation==CombatRuntime.Generation)return;
        generation=CombatRuntime.Generation;safeTrail.Clear();watches.Clear();BlockedSeconds=retryAt=0;
        Notice=null;PlayerRecoveries=EnemyRecoveries=0;
    }
    public void Observe(Vector2 requested,Vector3 before,float dt)
    {
        NewRun();var gm=GameManager.Instance;
        if(gm==null||!gm.IsCombatActive||gm.IsPaused||dt<=0)return;
        Vector3 now=transform.position;
        if(now.y< -2.5f||now.y>7||Mathf.Abs(now.x)>28||Mathf.Abs(now.z)>28){TryRecover(false);return;}
        bool safe=IsSafe(now,transform);
        if(safe&&(safeTrail.Count==0||Vector3.Distance(safeTrail[safeTrail.Count-1],now)>.8f))
        {if(safeTrail.Count==32)safeTrail.RemoveAt(0);safeTrail.Add(now);}
        if(player.Melee.IsAttacking||player.IsDashing||gm.equipmentLoop.Absorption.Busy){BlockedSeconds=0;return;}
        float moved=Vector3.ProjectOnPlane(now-before,Vector3.up).magnitude;
        BlockedSeconds=requested.sqrMagnitude>.36f&&moved<.08f*dt?BlockedSeconds+dt:0;
        if(BlockedSeconds>=1.8f)TryRecover(false);
    }
    public bool FindSafe(Vector3 origin,Transform owner,out Vector3 point)
    {
        for(int ring=0;ring<7;ring++)for(int i=0;i<(ring==0?1:16);i++)
        {
            float a=i*Mathf.PI/8;Vector3 candidate=origin+new Vector3(Mathf.Cos(a),0,Mathf.Sin(a))*ring*1.2f;
            if(!NavMesh.SamplePosition(candidate,out var hit,.8f,NavMesh.AllAreas))continue;
            candidate=hit.position+Vector3.up*.08f;
            if(IsSafe(candidate,owner)){point=candidate;return true;}
        }
        point=default;return false;
    }
    public bool TryRecover(bool manual)
    {
        NewRun();var gm=GameManager.Instance;
        if(gm==null||gm.Phase!=GamePhase.Combat||player.GetComponent<Damageable>().IsDead||Time.time<retryAt)return false;
        bool found=false;Vector3 point=default;
        for(int i=safeTrail.Count-1;i>=0;i--)
            if(Vector3.Distance(transform.position,safeTrail[i])>1&&IsSafe(safeTrail[i],transform))
            {point=safeTrail[i];found=true;break;}
        if(!found)found=FindSafe(transform.position,transform,out point);
        if(!found)found=FindSafe(new Vector3(0,.1f,-4),transform,out point);
        if(!found){Notice="暂时没有安全落点，请稍后再试";NoticeUntil=Time.unscaledTime+3;retryAt=Time.time+.5f;return false;}
        gm.equipmentLoop.Absorption.Cancel();player.CancelMovement();GameAudio.StopSwordSwings();GameAudio.StopSwordPreparation();
        player.Motor.enabled=false;transform.position=point;player.Motor.enabled=true;
        Physics.SyncTransforms();BlockedSeconds=0;retryAt=Time.time+3;PlayerRecoveries++;
        safeTrail.Clear();safeTrail.Add(point);
        Notice=manual?"已返回安全位置 · 战斗继续":"检测到位置卡住 · 已自动脱困";NoticeUntil=Time.unscaledTime+3;
        if(gm.IsPaused)gm.SetPaused(false);
        CombatEffects.Disc(point,1.2f,.4f,new Color(.18f,.9f,1,.65f));
        return true;
    }
    public void CheckEnemies(float dt)
    {
        NewRun();var gm=GameManager.Instance;
        if(gm==null||!gm.IsCombatActive||gm.IsPaused)return;
        foreach(var hp in Damageable.Active)
        {
            if(hp==null||hp.IsDead||hp.team==0)continue;
            var enemy=hp.GetComponent<EnemyBase>();if(enemy==null||!enemy.enabled||enemy.TrainingTarget)continue;
            if(!watches.TryGetValue(enemy,out var w)){w=new Watch{position=enemy.transform.position};watches[enemy]=w;}
            var agent=enemy.GetComponent<NavMeshAgent>();if(agent==null||!agent.enabled)continue;
            bool outside=!agent.isOnNavMesh;
            bool unreachable=!NavMesh.CalculatePath(enemy.transform.position,transform.position,NavMesh.AllAreas,path)||path.status!=NavMeshPathStatus.PathComplete;
            bool wantsMove=outside||unreachable||agent.hasPath&&!agent.isStopped&&agent.remainingDistance>2;
            bool idle=enemy.AttackPhase==EnemyAttackPhase.Ready&&!enemy.IsInRecovery&&enemy.HitStaggerRemaining<=0;
            w.quiet=wantsMove&&idle&&Vector3.Distance(w.position,enemy.transform.position)<.12f?w.quiet+dt:0;
            w.position=enemy.transform.position;
            if(w.quiet<5)continue;
            // Bring a stranded actor back into the reachable arena, with a fresh warning and grace period.
            Vector3 toward=Vector3.ProjectOnPlane(w.position-transform.position,Vector3.up).normalized;
            if(toward.sqrMagnitude<.1f)toward=Vector3.forward;
            if(FindSafe(transform.position+toward*9,enemy.transform,out var point)
                &&Vector3.Distance(point,transform.position)>5
                &&NavMesh.CalculatePath(transform.position,point,NavMesh.AllAreas,path)&&path.status==NavMeshPathStatus.PathComplete
                &&enemy.RecoverNavigation(point))
            {EnemyRecoveries++;CombatEffects.Disc(point,1.3f,1,new Color(1,.58f,.12f,.7f));}
            w.quiet=0;
        }
        if(watches.Count>32){var dead=new List<EnemyBase>();foreach(var pair in watches)if(pair.Key==null||pair.Key.GetComponent<Damageable>().IsDead)dead.Add(pair.Key);foreach(var key in dead)watches.Remove(key);}
    }
    void OnGUI()
    {
        if(Time.unscaledTime>=NoticeUntil||string.IsNullOrEmpty(Notice))return;
        GUI.Box(new Rect(Screen.width*.5f-200,Screen.height-90,400,34),Notice);
    }
}
