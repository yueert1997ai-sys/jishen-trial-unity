using System.Collections.Generic;
using UnityEngine;

// Shared cover contract: opaque world colliders block; actors never hide a wall behind them.
// Queries stay on the combat plane, independently of decorative muzzle/armor altitude.
public static class CombatContactQuery
{
    static readonly RaycastHit[] hits=new RaycastHit[128];
    static readonly Collider[] overlaps=new Collider[128];
    static bool Solid(Collider c)=>c!=null&&c.GetComponentInParent<Damageable>()==null;
    public static bool Wall(Vector3 start,Vector3 direction,float distance,float radius,
        out float at,out Vector3 normal)
    {
        start=PlanarCombat.Point(start);direction=PlanarCombat.Direction(direction,Vector3.forward);
        at=0;normal=-direction;
        int count=Physics.OverlapSphereNonAlloc(start,Mathf.Max(.001f,radius),overlaps,~0,QueryTriggerInteraction.Ignore);
        if(count==overlaps.Length)return true; // Incomplete query cannot grant permission to shoot through cover.
        for(int i=0;i<count;i++)if(Solid(overlaps[i]))return true;
        if(distance<=.0001f)return false;
        count=radius>0?Physics.SphereCastNonAlloc(start,radius,direction,hits,distance,~0,QueryTriggerInteraction.Ignore)
            :Physics.RaycastNonAlloc(start,direction,hits,distance,~0,QueryTriggerInteraction.Ignore);
        if(count==hits.Length)return true;
        bool blocked=false;at=distance;
        for(int i=0;i<count;i++)if(Solid(hits[i].collider)&&hits[i].distance<=at)
        {blocked=true;at=hits[i].distance;normal=hits[i].normal;}
        return blocked;
    }
    public static bool BarrelBlocked(Damageable owner,Vector3 muzzle,float radius,out Vector3 point,out Vector3 normal)
    {
        point=muzzle;normal=Vector3.back;if(owner==null)return false;
        Vector3 from=PlanarCombat.Point(owner.transform.position),delta=PlanarCombat.Point(muzzle)-from;
        var direction=PlanarCombat.Direction(delta,owner.transform.forward);
        if(!Wall(from,direction,delta.magnitude,radius,out float distance,out normal))return false;
        point=from+direction*distance;point.y=muzzle.y;return true;
    }
    public static bool BlastVisible(Vector3 center,Damageable target)
    {
        if(target==null)return false;
        var delta=PlanarCombat.Point(target.AimCenter)-PlanarCombat.Point(center);
        // No forward epsilon: it could skip a thin wall immediately next to the blast.
        return !Wall(center,delta,delta.magnitude,0,out _,out _);
    }
}

// One resolver per beam instance. Gather first, then publish damage; callbacks cannot overwrite queries.
public sealed class BeamContactResolver
{
    public bool BarrelObstructed {get;private set;}
    struct Contact
    {
        public Damageable target;public Vector3 point,normal;public float distance;
    }
    readonly List<Contact> contacts=new List<Contact>(32);
    readonly HashSet<Damageable> reserved=new HashSet<Damageable>();
    readonly List<Damageable> splashCandidates=new List<Damageable>(32);
    public int Resolve(GameObject emitter,Damageable owner,int team,Vector3 origin,Vector3 heading,
        float range,float radius,int penetration,float damage,float blastRadius,CombatHitKind kind,bool heavy,
        List<Vector3> impacts,out Vector3 end)
    {
        contacts.Clear();reserved.Clear();impacts.Clear();
        var direction=PlanarCombat.Direction(heading,owner!=null?owner.transform.forward:Vector3.forward);
        end=origin+direction*range;
        bool barrel=CombatContactQuery.BarrelBlocked(owner,origin,radius,out var barrelPoint,out var barrelNormal);
        BarrelObstructed=barrel;
        if(barrel)
        {
            end=barrelPoint;
            contacts.Add(new Contact{point=barrelPoint,normal=barrelNormal});
        }
        else
        {
            bool wall=CombatContactQuery.Wall(origin,direction,range,radius,out float wallAt,out var wallNormal);
            float limit=wall?wallAt:range;
            foreach(var actor in Damageable.Active)
            {
                if(actor==null||actor==owner||actor.IsDead||actor.team==team)continue;
                var shape=actor.GetComponent<Collider>();if(shape==null||!shape.enabled||shape.isTrigger)continue;
                if(PlanarCombat.FootprintHit(shape,PlanarCombat.Point(origin),direction,limit,radius,out float distance)
                    &&(!wall||distance<wallAt-.0001f))
                    contacts.Add(new Contact{target=actor,point=origin+direction*distance,normal=-direction,distance=distance});
            }
            contacts.Sort((a,b)=>a.distance.CompareTo(b.distance));
            int count=Mathf.Min(contacts.Count,Mathf.Max(0,penetration)+1);
            if(count<contacts.Count)contacts.RemoveRange(count,contacts.Count-count);
            if(count==Mathf.Max(0,penetration)+1)end=contacts[count-1].point;
            else if(wall){end=origin+direction*wallAt;contacts.Add(new Contact{point=end,normal=wallNormal,distance=wallAt});}
        }
        // Direct contacts are reserved even when rejected: splash must not retry the same contact.
        foreach(var contact in contacts)if(contact.target!=null)reserved.Add(contact.target);
        int applied=0;
        foreach(var contact in contacts)
        {
            if(contact.target==null){impacts.Add(contact.point);continue;}
            var result=contact.target.ApplyDamage(damage,new DamageInfo(emitter,origin,owner,damage)
            {Kind=kind,HeavyImpact=heavy,HasContact=true,ContactPoint=contact.point,ContactNormal=contact.normal,
             ContactTangent=Vector3.Cross(Vector3.up,direction)});
            if(result.Applied){applied++;impacts.Add(contact.point);}
        }
        if(blastRadius>.05f)
        {
            // Snapshot actor candidates so OnResolved/OnDied registry changes cannot skip or repeat them.
            splashCandidates.Clear();foreach(var actor in Damageable.Active)splashCandidates.Add(actor);
            foreach(var contact in contacts)
            {
                // Explosions are physical contacts, including rejected direct hits; only success visuals are gated.
                Vector3 center=contact.point;
                foreach(var target in splashCandidates)
                {
                    if(target==null||target.IsDead||target.team==team||reserved.Contains(target))continue;
                    float distance=Vector3.Distance(center,target.AimCenter);
                    if(distance>blastRadius||!CombatContactQuery.BlastVisible(center,target))continue;
                    reserved.Add(target);float amount=damage*(.45f+Mathf.Clamp01(1-distance/blastRadius)*.55f);
                    target.ApplyDamage(amount,new DamageInfo(emitter,center,owner,amount)
                    {Kind=kind,HeavyImpact=heavy,HasContact=true,ContactPoint=target.AimCenter,
                     ContactNormal=PlanarCombat.Direction(center-target.AimCenter,-direction)});
                }
            }
        }
        return applied;
    }
}
