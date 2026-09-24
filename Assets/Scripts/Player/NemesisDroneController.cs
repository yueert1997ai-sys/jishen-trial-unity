using UnityEngine;

// Six independent world-space units; physical flight and shots both respect cover.
[DefaultExecutionOrder(140)]
public sealed class NemesisDroneController : MonoBehaviour
{
    public const float Duration=7.5f,Range=22f;
    const int Pulses=6;
    public Vector3[] muzzleLocal;
    public bool Active {get;private set;}
    public float Age {get;private set;}
    public float Deployment=>Active?Mathf.Min(1,Mathf.Min(Age/.35f,(Duration-Age)/.65f)):0;
    public int ShotsFired {get;private set;}
    public int Hits {get;private set;}
    public int TargetChanges {get;private set;}
    public int VisibleBeams=>Visuals!=null?Visuals.VisibleBeams:0;
    public NemesisDroneVfx Visuals {get;private set;}
    sealed class Unit
    {
        public Transform joint;public Vector3 position,end,returnPosition;public Quaternion rotation,returnRotation;public bool recalling;
        public Damageable target;public float scan,shot,routeAge;public int fired;public Vector3 destination;
    }
    Unit[] units;
    NemesisMotionRig rig;PlayerController player;Damageable owner;
    float damage,impact;
    readonly RaycastHit[] obstacles=new RaycastHit[64];
    readonly Collider[] volumes=new Collider[64];
    public Damageable Target(int index)=>units[index].target;
    public Vector3 Muzzle(int index)=>units[index].joint.TransformPoint(muzzleLocal[index]);
    void Start()
    {
        rig=GetComponent<NemesisMotionRig>();player=rig.Player;owner=player.GetComponent<Damageable>();
        Visuals=gameObject.AddComponent<NemesisDroneVfx>();Visuals.Initialize();units=new Unit[6];
        for(int i=0;i<6;i++)
        {
            units[i]=new Unit{joint=rig.Drones[i]};
        }
        gameObject.AddComponent<IndependentDroneMeshes>().Initialize(rig.Drones);
    }
    public bool Deploy(float totalDamage,float totalImpact)
    {
        if(units==null||Active)return false;
        Active=true;Age=0;damage=totalDamage/(6*Pulses);impact=totalImpact/(6*Pulses);
        for(int i=0;i<6;i++)
        {
            var u=units[i];u.position=u.joint.position;u.rotation=u.joint.rotation;
            u.target=null;u.scan=0;u.shot=.48f+i*.075f;u.fired=0;u.recalling=false;u.routeAge=0;u.destination=u.position;
        }
        return true;
    }
    bool Valid(Damageable candidate)=>candidate!=null&&candidate.isActiveAndEnabled&&!candidate.IsDead&&candidate.team!=owner.team
        &&(PlanarCombat.Point(candidate.transform.position)-PlanarCombat.Point(player.transform.position)).sqrMagnitude<Range*Range;
    float CoverDistance(Vector3 from,Vector3 direction,float distance)
    {
        int n=Physics.RaycastNonAlloc(PlanarCombat.Point(from),direction,obstacles,distance,~0,QueryTriggerInteraction.Ignore);
        float nearest=distance;
        for(int i=0;i<n;i++)
            if(obstacles[i].collider.GetComponentInParent<Damageable>()==null)nearest=Mathf.Min(nearest,obstacles[i].distance);
        return nearest;
    }
    bool Visible(Vector3 from,Damageable candidate)
    {
        return !Blocked(from,candidate.AimCenter,0,out _);
    }
    bool Blocked(Vector3 from,Vector3 to,float radius,out Collider obstacle)
    {
        var delta=to-from;float distance=delta.magnitude;obstacle=null;
        if(distance<.001f)return false;
        int count=radius>0?Physics.SphereCastNonAlloc(from,radius,delta/distance,obstacles,distance,~0,QueryTriggerInteraction.Ignore)
            :Physics.RaycastNonAlloc(from,delta/distance,obstacles,distance,~0,QueryTriggerInteraction.Ignore);
        float nearest=distance;
        for(int i=0;i<count;i++)if(obstacles[i].collider.GetComponentInParent<Damageable>()==null&&obstacles[i].distance<=nearest)
        {nearest=obstacles[i].distance;obstacle=obstacles[i].collider;}
        return obstacle!=null;
    }
    bool Occupied(Vector3 point)
    {
        int count=Physics.OverlapSphereNonAlloc(point,.30f,volumes,~0,QueryTriggerInteraction.Ignore);
        for(int i=0;i<count;i++)if(volumes[i].GetComponentInParent<Damageable>()==null)return true;
        return false;
    }
    Vector3 Fly(Vector3 from,Vector3 goal,float distance)
    {
        if(Blocked(from,goal,.32f,out var obstacle))
        {
            var bounds=obstacle.bounds;bounds.Expand(1.4f);
            Vector3 best=from;float cost=float.MaxValue;
            for(int x=0;x<2;x++)for(int z=0;z<2;z++)
            {
                var corner=new Vector3(x==0?bounds.min.x:bounds.max.x,Mathf.Max(3.3f,from.y),z==0?bounds.min.z:bounds.max.z);
                if(Mathf.Abs(corner.x)>27||Mathf.Abs(corner.z)>27||Occupied(corner)||Blocked(from,corner,.32f,out _))continue;
                float score=Vector3.Distance(from,corner)+Vector3.Distance(corner,goal);
                if(score<cost){cost=score;best=corner;}
            }
            goal=best;
        }
        return Vector3.MoveTowards(from,goal,distance);
    }
    void Acquire(Unit u,Vector3 muzzle)
    {
        if(Valid(u.target))return;
        Damageable best=null;float score=float.MaxValue;
        for(int i=0;i<Damageable.Active.Count;i++)
        {
            var candidate=Damageable.Active[i];if(!Valid(candidate))continue;
            float d=(candidate.AimCenter-muzzle).sqrMagnitude;
            // Spread the six units across available threats, converge when only one remains.
            for(int j=0;j<6;j++)if(units[j]!=u&&units[j].target==candidate)d+=35;
            if(d>=score)continue;
            best=candidate;score=d;
        }
        if(best!=u.target)TargetChanges++;u.target=best;
    }
    void LateUpdate()
    {
        if(units==null)return;
        var gm=GameManager.Instance;
        if(owner.IsDead||gm==null||gm.Phase!=GamePhase.Combat){Cancel();return;}
        if(gm.IsPaused)return;
        if(!Active)return;
        Age+=Time.deltaTime;
        if(Age>=Duration){Cancel();return;}
        for(int i=0;i<6;i++)
        {
            var u=units[i];int rank=i%3;float side=i<3?1:-1;
            // The rig has just supplied this frame's moving dock pose.
            rig.DroneDock(i,out var dockPosition,out var dockRotation);
            float follow=1-Mathf.Exp(-16*Time.deltaTime);
            u.scan-=Time.deltaTime;
            if(u.scan<=0){Acquire(u,u.position);u.scan=.18f+i*.007f;}
            u.routeAge-=Time.deltaTime;
            if(u.routeAge<=0)
            {
                u.destination=FiringPosition(i,u);u.routeAge=.32f+i*.017f;
            }
            var direction=Valid(u.target)?(u.target.AimCenter-u.position).normalized:(u.destination-u.position).normalized;
            if(direction.sqrMagnitude<.01f)direction=player.transform.forward;
            // Long blades fire through their pointed -Y ends, with the broad face on top.
            var top=Vector3.ProjectOnPlane(Vector3.up,direction);
            if(top.sqrMagnitude<.001f)top=Vector3.ProjectOnPlane(player.transform.forward,direction);
            var aim=Quaternion.LookRotation(top.normalized,-direction);
            bool returning=Age>Duration-.65f;
            float returnBlend=returning?Mathf.SmoothStep(0,1,1-Deployment):0;
            if(returning&&!u.recalling){u.recalling=true;u.returnPosition=u.position;u.returnRotation=u.rotation;}
            u.position=Fly(u.position,returning?dockPosition:u.destination,(returning?48f:Age<.35f?24f:18f)*Time.deltaTime);
            u.rotation=returning?Quaternion.Slerp(u.returnRotation,dockRotation,returnBlend):Quaternion.Slerp(u.rotation,aim,follow);
            u.joint.SetPositionAndRotation(u.position,u.rotation);
            if(!returning&&Age>=u.shot&&u.fired<Pulses&&Valid(u.target)&&Visible(Muzzle(i),u.target)
                &&Vector3.Angle(-u.joint.up,u.target.AimCenter-Muzzle(i))<14)
            {Fire(i);u.shot=Age+1f;u.fired++;u.routeAge=0;}
            float charge=!returning&&u.fired<Pulses&&Valid(u.target)
                ?Mathf.Clamp01(1-(u.shot-Age)/NemesisDroneVfx.ChargeTime):0;
            Visuals.Tick(i,Muzzle(i),-u.joint.up,charge,Time.deltaTime);
        }
    }
    Vector3 FiringPosition(int index,Unit u)
    {
        bool enemy=Valid(u.target);Vector3 center=enemy?u.target.transform.position:player.transform.position;
        float angle=index*Mathf.PI/3+Age*(enemy?.48f:.72f)+u.fired*.55f;
        float radius=enemy?4.8f+(index%3)*.6f:3.8f+index%2;
        Vector3 best=u.position;float score=float.MaxValue;
        for(int step=0;step<12;step++)
        {
            float a=angle+step*Mathf.PI/6;
            Vector3 p=center+new Vector3(Mathf.Cos(a)*radius,3.3f+(index%3)*.48f,Mathf.Sin(a)*radius);
            p.x=Mathf.Clamp(p.x,-26,26);p.z=Mathf.Clamp(p.z,-26,26);
            // A candidate must have an actual airborne shot and clearance from solid geometry.
            if(enemy&&!Visible(p,u.target))continue;
            if(Occupied(p))continue;
            float cost=Vector3.Distance(u.position,p)+step*.30f;
            for(int j=0;j<6;j++)if(j!=index)cost+=Mathf.Max(0,1.5f-Vector3.Distance(p,units[j].position))*5;
            if(cost<score){score=cost;best=p;}
        }
        if(float.IsPositiveInfinity(score)||score==float.MaxValue)
            best=player.transform.position+new Vector3(Mathf.Cos(angle)*5,4+index*.12f,Mathf.Sin(angle)*5);
        return best;
    }
    void Fire(int index)
    {
        var u=units[index];var from=Muzzle(index);var heading=(u.target.AimCenter-from).normalized;float distance=Range;
        Damageable struck=null;
        int count=Physics.RaycastNonAlloc(from,heading,obstacles,Range,~0,QueryTriggerInteraction.Ignore);
        for(int i=0;i<count;i++)
        {
            var hit=obstacles[i];var candidate=hit.collider.GetComponentInParent<Damageable>();
            if(candidate!=null&&(candidate.team==owner.team||candidate.IsDead))continue;
            if(hit.distance<distance){distance=hit.distance;struck=candidate;}
        }
        var visualDirection=heading;u.end=from+heading*distance;ShotsFired++;
        if(struck!=null)
        {
            var info=new DamageInfo(gameObject,PlanarCombat.Point(from),owner,damage){Kind=CombatHitKind.Rifle,Impact=impact,
                HasContact=true,ContactPoint=u.end,ContactNormal=-visualDirection,ContactTangent=Vector3.Cross(Vector3.up,heading)};
            var result=struck.ApplyDamage(damage,info);if(result.AppliedDamage>0)Hits++;
        }
        Visuals.Fire(index,from,u.end,struck!=null||distance<Range-.01f);
        if(index==0)GameAudio.PlayAt(GameAudioCue.Beam,from,.16f,1.22f);
    }
    public void Cancel()
    {
        Active=false;Age=0;Visuals?.Clear();
        if(units==null)return;
        for(int i=0;i<units.Length;i++)
        {var u=units[i];u.target=null;if(rig!=null&&u.joint!=null){rig.DroneDock(i,out var p,out var q);u.joint.SetPositionAndRotation(p,q);u.position=p;u.rotation=q;}}

    }
    void OnDisable(){Cancel();}
}
