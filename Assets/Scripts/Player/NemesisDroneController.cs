using UnityEngine;

// Six actual backpack joints, six reusable beam flashes. No cloned mechs or
// homing missiles. Simulation uses the same ground plane as the other weapons.
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
        public Damageable target;public float scan,shot;public int fired;
    }
    Unit[] units;
    NemesisMotionRig rig;PlayerController player;Damageable owner;
    float damage,impact;
    readonly RaycastHit[] obstacles=new RaycastHit[64];
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
    }
    public bool Deploy(float totalDamage,float totalImpact)
    {
        if(units==null||Active)return false;
        Active=true;Age=0;damage=totalDamage/(6*Pulses);impact=totalImpact/(6*Pulses);
        for(int i=0;i<6;i++)
        {
            var u=units[i];u.position=u.joint.position;u.rotation=u.joint.rotation;
            u.target=null;u.scan=0;u.shot=.48f+i*.075f;u.fired=0;u.recalling=false;
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
        var delta=PlanarCombat.Point(candidate.AimCenter)-PlanarCombat.Point(from);
        return CoverDistance(from,delta.normalized,delta.magnitude)>=delta.magnitude-.15f;
    }
    void Acquire(Unit u,Vector3 muzzle)
    {
        if(Valid(u.target)&&Visible(muzzle,u.target))return;
        Damageable best=null;float score=float.MaxValue;
        for(int i=0;i<Damageable.Active.Count;i++)
        {
            var candidate=Damageable.Active[i];if(!Valid(candidate))continue;
            float d=(candidate.AimCenter-muzzle).sqrMagnitude;
            // Spread the six units across available threats, converge when only one remains.
            for(int j=0;j<6;j++)if(units[j]!=u&&units[j].target==candidate)d+=35;
            if(d>=score||!Visible(muzzle,candidate))continue;
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
            var dockPosition=u.joint.position;var dockRotation=u.joint.rotation;
            float follow=1-Mathf.Exp(-10*Time.deltaTime);
            var formation=player.transform.position+player.transform.TransformDirection(new Vector3(side*(2.05f+rank*.72f),3.3f+rank*.28f,-.4f-rank*.56f));
            formation+=Vector3.up*(.08f*Mathf.Sin(Age*2.7f+i*1.4f));
            u.scan-=Time.deltaTime;
            if(u.scan<=0){Acquire(u,u.position);u.scan=.18f+i*.007f;}
            var direction=Valid(u.target)?(u.target.AimCenter-u.position).normalized:player.transform.forward;
            // Long blades fire through their pointed -Y ends, with the broad face on top.
            var top=Vector3.ProjectOnPlane(Vector3.up,direction);
            if(top.sqrMagnitude<.001f)top=Vector3.ProjectOnPlane(player.transform.forward,direction);
            var aim=Quaternion.LookRotation(top.normalized,-direction);
            bool returning=Age>Duration-.65f;
            float returnBlend=returning?Mathf.SmoothStep(0,1,1-Deployment):0;
            if(returning&&!u.recalling){u.recalling=true;u.returnPosition=u.position;u.returnRotation=u.rotation;}
            u.position=returning?Vector3.Lerp(u.returnPosition,dockPosition,returnBlend):Vector3.Lerp(u.position,formation,follow);
            u.rotation=returning?Quaternion.Slerp(u.returnRotation,dockRotation,returnBlend):Quaternion.Slerp(u.rotation,aim,follow);
            u.joint.SetPositionAndRotation(u.position,u.rotation);
            if(!returning&&Age>=u.shot&&u.fired<Pulses&&Valid(u.target)&&Visible(Muzzle(i),u.target)
                &&Vector3.Angle(-u.joint.up,u.target.AimCenter-Muzzle(i))<14)
            {Fire(i);u.shot=Age+1f;u.fired++;}
            float charge=!returning&&u.fired<Pulses&&Valid(u.target)
                ?Mathf.Clamp01(1-(u.shot-Age)/NemesisDroneVfx.ChargeTime):0;
            Visuals.Tick(i,Muzzle(i),-u.joint.up,charge,Time.deltaTime);
        }
    }
    void Fire(int index)
    {
        var u=units[index];var from=Muzzle(index);var delta=PlanarCombat.Point(u.target.AimCenter)-PlanarCombat.Point(from);
        var heading=delta.normalized;float distance=CoverDistance(from,heading,Range);
        Damageable struck=null;
        // Registry excludes duplicate colliders. Match projectile footprint collision for low targets.
        for(int i=0;i<Damageable.Active.Count;i++)
        {
            var candidate=Damageable.Active[i];if(candidate==null||candidate==owner||candidate.team==owner.team||candidate.IsDead)continue;
            var shape=candidate.GetComponent<Collider>();
            if(shape==null||!shape.enabled||!PlanarCombat.FootprintHit(shape,from,heading,distance,.045f,out float at))continue;
            if(at<distance){distance=at;struck=candidate;}
        }
        var visualDirection=(u.target.AimCenter-from).normalized;
        float planarScale=new Vector2(visualDirection.x,visualDirection.z).magnitude;
        u.end=from+visualDirection*(distance/Mathf.Max(.01f,planarScale));ShotsFired++;
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
        foreach(var u in units)u.target=null;
    }
    void OnDisable(){Cancel();}
}
