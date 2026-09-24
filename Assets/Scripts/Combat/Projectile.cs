using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    public int team;
    public float damage = 10f, speed = 24f, lifetime = 2f, explosionRadius;
    public int pierceCount;
    public Vector3 direction = Vector3.forward;
    protected Damageable source;
    public float Impact { get; private set; } = -1f;
    public float PunishMultiplier { get; private set; } = -1f;
    public CombatHitKind HitKind { get; private set; }
    public int LabEventId { get; private set; }
    public WeaponHandlingProfile Handling {get;private set;}
    public float DistanceTravelled {get;private set;}
    public void SetHandling(WeaponHandlingProfile profile)
    {
        Handling=profile;
        DistanceTravelled=source!=null?Vector3.Distance(PlanarCombat.Point(source.transform.position),PlanarCombat.Point(transform.position)):0;
    }
    public void SetImpact(float impact, CombatHitKind kind, float punish=1.35f)
    {
        Impact=Mathf.Max(0,impact); HitKind=kind; PunishMultiplier=punish;
        if(team==0&&LabEventId==0)LabEventId=CombatLabTelemetry.ProjectileReady(this);
    }
    DamageInfo ContactInfo(float amount, Vector3 point, float falloff=1f)
    {
        // Contact direction is captured at impact; moving the shooter cannot change which plate was hit.
        return new DamageInfo(gameObject,point-direction,source,amount)
        {Impact=Impact<0?-1:Impact*falloff,Kind=HitKind,DirectHitMultiplier=PunishMultiplier,
         HeavyImpact=HitKind==CombatHitKind.HeavyRifle,HasContact=true,KineticRound=Kinetic,
         ContactPoint=point,ContactNormal=-direction,ContactTangent=Vector3.Cross(Vector3.up,direction)};
    }
    [System.NonSerialized] public ProjectilePool pool;
    private MaterialPropertyBlock visualBlock;
    public MaterialPropertyBlock VisualBlock => visualBlock ?? (visualBlock = new MaterialPropertyBlock());
    private readonly RaycastHit[] sweepHits = new RaycastHit[24];
    private readonly HashSet<Damageable> struck = new HashSet<Damageable>();
    private readonly List<Damageable> splashCandidates = new List<Damageable>(32);
    private static readonly IComparer<RaycastHit> HitOrder = Comparer<RaycastHit>.Create((a, b) => a.distance.CompareTo(b.distance));
    private bool spent;
    public bool Planar {get;private set;}
    public bool Kinetic {get;set;}
    private bool checkBarrel;
    private readonly Collider[] overlaps=new Collider[24];

    private void Awake()
    {
        var collider = GetComponent<Collider>();
        if (collider == null) collider = gameObject.AddComponent<SphereCollider>();
        collider.isTrigger = true;
        var body = GetComponent<Rigidbody>();
        if (body == null) body = gameObject.AddComponent<Rigidbody>();
        body.isKinematic = true;
        body.useGravity = false;
    }

    public void Init(int sourceTeam, Damageable sourceDamageable, Vector3 shotDirection, float shotDamage, float shotSpeed, float shotLifetime, float shotExplosionRadius, int shotPierceCount)
    {
        team = sourceTeam;
        source = sourceDamageable;
        direction = shotDirection.sqrMagnitude > 0.01f ? shotDirection.normalized : Vector3.forward;
        Planar=!(this is MissileProjectile);
        if(Planar)direction=PlanarCombat.Direction(direction,sourceDamageable!=null?sourceDamageable.transform.forward:Vector3.forward);
        checkBarrel=Planar && sourceDamageable!=null;
        transform.rotation=Quaternion.LookRotation(direction,Vector3.up);
        damage = shotDamage;
        speed = shotSpeed;
        lifetime = shotLifetime;
        explosionRadius = shotExplosionRadius;
        pierceCount = shotPierceCount;
        Impact=-1f; PunishMultiplier=-1f; HitKind=CombatHitKind.Generic;
        LabEventId=0;
        Handling=default;DistanceTravelled=0;
        spent = false;
        struck.Clear();
        var energy=GetComponent<EnergyBoltVisual>();
        if(energy==null)energy=gameObject.AddComponent<EnergyBoltVisual>();
        if(energy!=null)energy.Configure(this,true);
    }

    protected virtual void Update()
    {
        if (spent || (GameManager.Instance != null && !GameManager.Instance.IsCombatActive)) return;
        float distance = speed * Time.deltaTime;
        if(Planar)
        {
            if(Handling.HasRange)distance=Mathf.Min(distance,Mathf.Max(0,Handling.MaximumRange-DistanceTravelled));
            if(checkBarrel && source!=null)
            {
                checkBarrel=false;
                Vector3 barrel=PlanarCombat.Point(transform.position)-PlanarCombat.Point(source.transform.position);
                if(barrel.sqrMagnitude>.0001f)SweepPlanar(source.transform.position,barrel.normalized,Handling.HasRange?Mathf.Min(barrel.magnitude,Handling.MaximumRange):barrel.magnitude,0);
            }
            if(!spent&&distance>0)SweepPlanar(transform.position,direction,distance,DistanceTravelled);
            if(!spent)
            {
                DistanceTravelled+=distance;
                transform.position+=direction*distance;transform.rotation=Quaternion.LookRotation(direction,Vector3.up);
                lifetime-=Time.deltaTime;if(lifetime<=0||Handling.HasRange&&DistanceTravelled>=Handling.MaximumRange-.0001f)Despawn();
            }
            return;
        }
        // Sweep the entire travelled segment: fast shots must not tunnel through cover or targets.
        int count = Physics.SphereCastNonAlloc(transform.position, 0.09f, direction, sweepHits, distance, ~0, QueryTriggerInteraction.Ignore);
        System.Array.Sort(sweepHits, 0, count, HitOrder);
        for (int i = 0; i < count && !spent; i++) Hit(sweepHits[i].collider, sweepHits[i].point, sweepHits[i].normal);
        if (spent) return;
        transform.position += direction * distance;
        transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
        lifetime -= Time.deltaTime;
        if (lifetime <= 0f) Despawn();
    }

    private void OnTriggerEnter(Collider other) { if (!spent && !Planar) Hit(other, transform.position); }

    private void SweepPlanar(Vector3 from,Vector3 heading,float distance,float travelled)
    {
        from=PlanarCombat.Point(from);
        int overlapCount=Physics.OverlapSphereNonAlloc(from,.09f,overlaps,~0,QueryTriggerInteraction.Ignore);
        for(int i=0;i<overlapCount;i++)
            if(overlaps[i].GetComponentInParent<Damageable>()==null){Hit(overlaps[i],from);return;}
        if(overlapCount==overlaps.Length){Despawn();return;}
        int count=Physics.SphereCastNonAlloc(from,.09f,heading,sweepHits,distance,~0,QueryTriggerInteraction.Ignore);
        if(count==sweepHits.Length){Despawn();return;} // Saturation must never shoot through unreported cover.
        float wallDistance=distance;Collider wall=null;Vector3 wallNormal=-heading;
        for(int i=0;i<count;i++)
            if(sweepHits[i].collider.GetComponentInParent<Damageable>()==null && sweepHits[i].distance<=wallDistance)
            {wallDistance=sweepHits[i].distance;wall=sweepHits[i].collider;wallNormal=sweepHits[i].normal;}
        // Actor footprints ignore cosmetic altitude; choose nearest contacts before the nearest solid cover.
        int budget=Damageable.Active.Count;
        while(!spent && budget-->0)
        {
            Collider nearest=null;float nearestAt=wallDistance;
            var actors=Damageable.Active;
            for(int i=0;i<actors.Count;i++)
            {
                var actor=actors[i];if(actor==null||actor.IsDead||actor.team==team||struck.Contains(actor))continue;
                var shape=actor.GetComponent<Collider>();if(shape==null||!shape.enabled||shape.isTrigger)continue;
                if(PlanarCombat.FootprintHit(shape,from,heading,nearestAt,.09f,out float at))
                {nearest=shape;nearestAt=at;}
            }
            if(nearest==null)break;
            var contact=from+heading*nearestAt;contact.y=transform.position.y;Hit(nearest,contact,default,travelled+nearestAt);
        }
        if(!spent && wall!=null){var point=from+heading*wallDistance;point.y=transform.position.y;Hit(wall,point,wallNormal);}
    }

    private void Hit(Collider other, Vector3 point, Vector3 normal=default,float travelled=0)
    {
        if (other == null || other.isTrigger || (GameManager.Instance != null && !GameManager.Instance.IsCombatActive)) return;
        var target = other.GetComponentInParent<Damageable>();
        if (target != null && (target.team == team || target.IsDead || struck.Contains(target))) return;
        if (target == null)
        {
            if(normal.sqrMagnitude<.01f)normal=-direction;
            if(Kinetic){ArmorContactVfx.Get().Wall(point,normal);GameAudio.PlayAt(GameAudioCue.WallHit,point,.22f);}
            else if(team==1){EnemyVfx.ImpactBurst(point,EnergyBoltVisual.EnemyRed);GameAudio.PlayAt(GameAudioCue.WallHit,point,.16f);}
            else {CombatFeedback.SpawnImpactPulse(point, new Color(1f, 0.72f, 0.22f), 0.2f);BeamFxKit.StarGlare(point,new Color(1f,.72f,.22f),.7f,.1f);}
            Despawn();
            return;
        }
        // Damageable publishes the committed result to CombatFeedback. A collider contact
        // alone cannot generate a successful hit flash (invulnerability, no damage, etc.).
        float rangeScale=Handling.DamageScale(travelled);
        float amount=damage*rangeScale;
        if (explosionRadius > 0.05f)
        {
            struck.Add(target);
            target.TakeDamage(amount, ContactInfo(amount,point,rangeScale));
            ApplyExplosionDamage(point,rangeScale);
            CombatFeedback.SpawnImpactPulse(point, new Color(1f, 0.65f, 0.15f), explosionRadius);
            BeamFxKit.ImpactBurst(point,direction,new Color(1f,.6f,.18f),.55f+explosionRadius*.15f);
            if (spent || pierceCount-- > 0) return;
            Despawn();
            return;
        }
        struck.Add(target);
        target.TakeDamage(amount, ContactInfo(amount,point,rangeScale));
        if (spent) return;
        if (pierceCount-- > 0) return;
        Despawn();
    }

    protected void ApplyExplosionDamage(Vector3 center,float rangeScale=1)
    {
        // Damageable registry avoids duplicate damage from multi-collider actors.
        splashCandidates.Clear();foreach(var actor in Damageable.Active)splashCandidates.Add(actor);
        for (int i = splashCandidates.Count - 1; i >= 0; i--)
        {
            var target = splashCandidates[i];
            if (target == null || target.team == team || target.IsDead || struck.Contains(target)) continue;
            float distance = Vector3.Distance(center, target.AimCenter);
            if (distance > explosionRadius || !CombatContactQuery.BlastVisible(center,target)) continue;
            struck.Add(target);
            float amount = damage * rangeScale * (0.45f + Mathf.Clamp01(1f - distance / explosionRadius) * 0.55f);
            var splash = ContactInfo(amount,center,damage>0?amount/damage:0);
            splash.SourcePosition=center;
            splash.ContactNormal=Vector3.ProjectOnPlane(center-target.AimCenter,Vector3.up).normalized;
            target.TakeDamage(amount, splash);
        }
    }

    public void Despawn()
    {
        if (spent) return;
        spent = true;
        if (pool != null) pool.Release(this);
        else { gameObject.SetActive(false); Destroy(gameObject); }
    }
}
