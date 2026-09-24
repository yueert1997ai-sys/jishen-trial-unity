using System.Collections.Generic;
using UnityEngine;

// Rigid armor stays editable per joint. No deformation of the source armor is required.
[DefaultExecutionOrder(95)]
public sealed class E01SoldierMotion : MonoBehaviour
{
    public Transform visual, rifle, muzzle, support;
    public float sourceScale = .84f;
    public const float CombatHeight=4.5f;
    public const float CombatRadius=.82f;
    public const float VisualScale=CombatHeight/2.8f;
    public int ShotsFired { get; private set; }
    public float RunBlend { get; private set; }
    public float GripError { get; private set; }
    public bool TrainingTarget { get; set; }
    private EnemyBase enemy;
    private Damageable health;
    private Transform waist, chest, head;
    private Vector3 lastPosition;
    private float cycle, recoil, hit, death, armorBreakUntil;
    private readonly Dictionary<Transform,Vector3> positions=new Dictionary<Transform,Vector3>();
    private readonly Dictionary<Transform,Quaternion> rotations=new Dictionary<Transform,Quaternion>();
    private readonly Transform[] thighs=new Transform[2],shins=new Transform[2],feet=new Transform[2],arms=new Transform[2],forearms=new Transform[2],hands=new Transform[2];
    private Renderer[] armor;private Renderer contactArmor;
    private MaterialPropertyBlock flash;
    private MaterialPropertyBlock[] armorRest;
    private float hitAge=1,hitDuration=.2f,hitStrength,hitTwist;
    private Vector3 hitDirection=Vector3.back;
    private Color hitTint;
    private bool armorFlashed;
    public float ReactionWeight => hitStrength*(1-Mathf.Exp(-hitAge*150))*Mathf.Pow(Mathf.Clamp01(1-hitAge/hitDuration),.8f);
    public Vector3 HitDirection => hitDirection;
    public Renderer[] ArmorRenderers => armor;
    public float DeathProgress => death;
    public Vector3 ReactorCenter=>chest!=null?chest.position:visual.position+Vector3.up*1.6f;
    public float BreakPoseWeight { get; private set; }
    private void RestoreArmor()
    {
        if(!armorFlashed||armorRest==null)return;
        for(int i=0;i<armor.Length;i++)if(armor[i]!=null)armor[i].SetPropertyBlock(armorRest[i]);
        armorFlashed=false;
    }
    private void UpdateArmor()
    {
        bool broken=Time.time<armorBreakUntil;
        if(health.IsDead||(!broken&&hitAge>(hitStrength>1?.055f:.035f))){RestoreArmor();return;}
        for(int i=0;i<armor.Length;i++)if(armor[i]!=null)
        {
            if(!armorFlashed)armor[i].GetPropertyBlock(armorRest[i]);
            if(!broken&&contactArmor!=null&&armor[i]!=contactArmor)continue;
            armor[i].GetPropertyBlock(flash);flash.SetColor("_EmissionColor",broken?(hitAge<.09f?new Color(.5f,1.8f,2.2f):new Color(.08f,.32f,.42f)):hitTint);
            armor[i].SetPropertyBlock(flash);
        }
        armorFlashed=true;
    }
    private Transform Find(string name)
    {foreach(var t in visual.GetComponentsInChildren<Transform>(true))if(t.name==name)return t;return null;}
    private void Awake()
    {
        enemy=GetComponent<EnemyBase>();health=GetComponent<Damageable>();
        // Scale the complete hierarchy: armor, hands, carried gun and real muzzle.
        visual.localScale=Vector3.one*VisualScale;
        var capsule=GetComponent<CapsuleCollider>();
        if(capsule!=null){capsule.height=CombatHeight;capsule.radius=CombatRadius;capsule.center=Vector3.up*(CombatHeight*.5f);}
        var nav=GetComponent<UnityEngine.AI.NavMeshAgent>();
        if(nav!=null){nav.height=CombatHeight;nav.radius=CombatRadius;}
        waist=Find("E01_WAIST");chest=Find("E01_CHEST");head=Find("E01_HEAD");
        for(int i=0;i<2;i++)
        {
            string s=i==0?"L":"R";
            thighs[i]=Find("E01_"+s+"_THIGH");shins[i]=Find("E01_"+s+"_CALF");feet[i]=Find("E01_"+s+"_FOOT");
            arms[i]=Find("E01_"+s+"_UPPER_ARM");forearms[i]=Find("E01_"+s+"_FOREARM");hands[i]=Find("E01_"+s+"_HAND");
        }
        foreach(var t in visual.GetComponentsInChildren<Transform>())
            if(t.name.StartsWith("E01_")){positions[t]=t.localPosition;rotations[t]=t.localRotation;}
        lastPosition=transform.position;armor=visual.GetComponentsInChildren<Renderer>();flash=new MaterialPropertyBlock();
        armorRest=new MaterialPropertyBlock[armor.Length];
        for(int i=0;i<armor.Length;i++)armorRest[i]=new MaterialPropertyBlock();
        health.OnDamaged+=Damaged;
    }
    private void Damaged(Damageable target,DamageInfo info)
    {
        hit=.18f;if(info.BrokeArmor)armorBreakUntil=Time.time+.24f;
        RestoreArmor();contactArmor=null;
        if(info.HasContact){float nearest=float.MaxValue;foreach(var renderer in armor)if(renderer!=null){float d=(renderer.bounds.ClosestPoint(info.ContactPoint)-info.ContactPoint).sqrMagnitude;if(d<nearest){nearest=d;contactArmor=renderer;}}}
        hitDirection=Vector3.ProjectOnPlane(transform.position-info.SourcePosition,Vector3.up).normalized;
        if(info.MeleeStrike&&info.ContactTangent.sqrMagnitude>.01f)hitDirection=Vector3.ProjectOnPlane(info.ContactTangent,Vector3.up).normalized;
        if(hitDirection.sqrMagnitude<.01f)hitDirection=-transform.forward;
        bool slash=info.SourceObject!=null&&info.SourceObject.GetComponent<PlayerMeleeController>()!=null;
        hitStrength=info.HeavyImpact?2.05f:slash?1.35f:.72f;
        hitDuration=info.HeavyImpact?.30f:slash?.18f:.11f;hitAge=0;
        if((info.FrontGuarded||info.ArmorDamage>0) && !info.BrokeArmor){hitStrength=info.HeavyImpact?.55f:.30f;hitDuration=.11f;}
        if(info.BrokeArmor){hitStrength=3.1f;hitDuration=.44f;}
        hitTwist=slash?Mathf.Clamp(Vector3.Dot(hitDirection,transform.right)*14,-14,14):0;
        hitTint=info.HeavyImpact?new Color(.32f,.70f,.85f):slash?new Color(.24f,.5f,.65f):new Color(.8f,.42f,.12f);
    }
    public void Recoil(){recoil=1;ShotsFired++;}
    public void SynchronizeForFire(Vector3 direction)
    {
        if(rifle!=null && direction.sqrMagnitude>.01f)
            rifle.rotation=Quaternion.LookRotation(PlanarCombat.Direction(direction,transform.forward),Vector3.up);
    }
    private void LateUpdate()
    {
        if(GameManager.Instance!=null&&GameManager.Instance.IsPaused)return;
        float dt=Time.deltaTime;hitAge+=dt;
        UpdateArmor();
        Vector3 velocity=(transform.position-lastPosition)/Mathf.Max(.001f,dt);lastPosition=transform.position;
        float speed=velocity.magnitude;
        RunBlend=Mathf.MoveTowards(RunBlend,Mathf.Clamp01(speed/2.4f),dt*9);
        if(speed<12)cycle+=speed*dt/(1.9f*VisualScale);
        recoil=Mathf.MoveTowards(recoil,0,dt*6);hit=Mathf.Max(0,hit-dt);
        foreach(var pair in positions)
            if(pair.Key!=null && pair.Key.IsChildOf(visual))pair.Key.SetLocalPositionAndRotation(pair.Value,rotations[pair.Key]);
        if(health.IsDead)
        {
            death+=dt;
            {
                var localDirection=transform.InverseTransformDirection(hitDirection);
                float kick=Mathf.Sin(Mathf.Clamp01(death/.25f)*Mathf.PI),fall=Mathf.SmoothStep(0,1,death/.52f);
                visual.localPosition=localDirection*(.72f*fall)+Vector3.up*(.16f*kick-.42f*fall);
                visual.localRotation=Quaternion.AngleAxis(82*fall,Vector3.Cross(Vector3.up,localDirection));
                chest.localRotation*=Quaternion.Euler(-18*kick,8*kick,0);
                for(int i=0;i<2;i++)
                {
                    arms[i].localRotation*=Quaternion.Euler(-25*kick,0,(i==0?1:-1)*32*fall);
                    thighs[i].localRotation*=Quaternion.Euler(24*fall,0,0);shins[i].localRotation*=Quaternion.Euler(-48*fall,0,0);
                }
                if(death<.14f)
                {
                    for(int i=0;i<armor.Length;i++)if(armor[i]!=null&&armor[i].enabled)
                    {
                        if(!armorFlashed)armor[i].GetPropertyBlock(armorRest[i]);
                        armor[i].GetPropertyBlock(flash);flash.SetColor("_EmissionColor",new Color(1,.21f,.015f)*(1-death/.14f));armor[i].SetPropertyBlock(flash);
                    }
                    armorFlashed=true;
                }
                if(death>.46f)foreach(var r in armor)if(r!=null&&r.transform.IsChildOf(visual))r.enabled=false;
            }
            if(!TrainingTarget && death>.62f)Destroy(gameObject);
            return;
        }
        if(death>0)foreach(var renderer in armor)if(renderer!=null&&renderer.transform.IsChildOf(visual))renderer.enabled=true;
        death=0;visual.SetLocalPositionAndRotation(Vector3.zero,Quaternion.identity);
        bool broken=Time.time<armorBreakUntil;
        BreakPoseWeight=Mathf.MoveTowards(BreakPoseWeight,broken?1:0,dt*(broken?14:7));
        visual.localPosition=Vector3.down*(.32f*BreakPoseWeight);
        float wave=Mathf.Sin(cycle*Mathf.PI*2),run=RunBlend;
        bool melee=enemy!=null&&enemy.kind==EnemyKind.Melee;
        float anticipation=melee?enemy.AttackWindup:0;
        float strike=melee?enemy.StrikePose:0;
        float lunge=melee&&enemy.AttackPhase==EnemyAttackPhase.Commit?1:0;
        waist.localPosition+=new Vector3(-wave*.055f,-.07f*run+Mathf.Abs(wave)*.035f*run,0);
        Vector3 travelDirection=transform.InverseTransformDirection(Vector3.ClampMagnitude(velocity,6));
        waist.localRotation=Quaternion.Euler(run*9-anticipation*12+lunge*19,-wave*8*run,wave*2*run-travelDirection.x*1.4f);
        waist.localPosition+=Vector3.down*(anticipation*.13f);
        chest.localRotation=Quaternion.Euler(recoil*-6+hit*40, wave*5*run, hit*25);
        head.rotation=transform.rotation;
        {
            chest.localRotation=Quaternion.Euler(recoil*-6,wave*5*run,0);
            Vector3 axis=Vector3.Cross(Vector3.up,hitDirection);float weight=ReactionWeight;
            waist.rotation=Quaternion.AngleAxis(5*weight,axis)*waist.rotation;
            chest.rotation=Quaternion.AngleAxis(17*weight,axis)*chest.rotation;
            chest.rotation=Quaternion.AngleAxis(hitTwist*weight,Vector3.up)*chest.rotation;
            waist.position+=hitDirection*(.035f*weight)+Vector3.down*(.035f*weight);
            head.rotation=Quaternion.AngleAxis(7*weight,axis)*head.rotation;
            waist.localRotation*=Quaternion.Euler(18*BreakPoseWeight,0,-9*BreakPoseWeight);
            chest.localRotation*=Quaternion.Euler(24*BreakPoseWeight,0,12*BreakPoseWeight);
            head.localRotation*=Quaternion.Euler(18*BreakPoseWeight,0,0);
        }
        for(int i=0;i<2;i++)
        {
            float phase=Mathf.Repeat(cycle+i*.5f,1),s=Mathf.Sin(phase*Mathf.PI*2);
            float side=i==0?-1:1;
            float direction=Vector3.Dot(velocity,transform.forward)>=0?1:-1;
            thighs[i].localRotation=Quaternion.Euler(s*36*run*direction,side*3,-side*3);
            thighs[i].localRotation*=Quaternion.Euler((i==0?32:17)*BreakPoseWeight,0,0);
            shins[i].localRotation=Quaternion.Euler(-Mathf.Max(0,-s)*68*run-5*run-40*BreakPoseWeight,0,0);
            feet[i].rotation=transform.rotation*Quaternion.Euler(-Mathf.Max(0,s)*16*run,side*5,0);
        }
        if(rifle==null)return; // The detached mesh may already be travelling to the warehouse.
        Vector3 grip=visual.TransformPoint(new Vector3(.34f,1.89f-run*.06f,.28f-recoil*.055f));
        grip+=transform.forward*(strike*.38f-anticipation*.13f)+Vector3.up*(anticipation*.18f-lunge*.1f);
        grip+=Vector3.down*(.52f*BreakPoseWeight);
        float contactWeight=ReactionWeight;
        grip+=hitDirection*(.14f*contactWeight)+Vector3.down*(.035f*contactWeight);
        Vector3 aim=enemy!=null&&enemy.WeaponAimDirection.sqrMagnitude>.01f?enemy.WeaponAimDirection:
            enemy!=null&&enemy.target!=null&&enemy.TargetVisible?enemy.target.position-grip:transform.forward;
        Quaternion orientation=Quaternion.LookRotation(aim.normalized,transform.up);
        orientation=Quaternion.LookRotation(PlanarCombat.Direction(aim,transform.forward),Vector3.up);
        orientation*=Quaternion.Euler(28*BreakPoseWeight,0,-15*BreakPoseWeight);
        if(melee)orientation*=Quaternion.Euler(-anticipation*22+strike*14,0,anticipation*8);
        orientation=Quaternion.AngleAxis(9*contactWeight,Vector3.Cross(Vector3.up,hitDirection))*orientation;
        orientation=Quaternion.AngleAxis(hitTwist*contactWeight*.65f,Vector3.up)*orientation;
        rifle.SetPositionAndRotation(grip,orientation);
        for(int i=0;i<2;i++)
        {
            float side=i==0?-1:1;
            Vector3 palm=new Vector3(side*.006f,-.110f,.019f)*sourceScale;
            Vector3 target=i==1?grip:support.position;
            Quaternion hand=transform.rotation*Quaternion.Euler(-80,i==1?0:-15,0);
            Solve(arms[i],forearms[i],hands[i],target-hand*Vector3.Scale(hands[i].lossyScale,palm),visual.TransformPoint(new Vector3(side*.83f,1.61f,.05f)),hand);
            if(i==1)GripError=Vector3.Distance(hands[i].TransformPoint(palm),grip);
        }
        // Keep the real muzzle attached to the carried receiver after arm posing.
        rifle.SetPositionAndRotation(grip,orientation);

    }
    private static void Solve(Transform upper,Transform lower,Transform end,Vector3 target,Vector3 pole,Quaternion rotation)
    {
        Vector3 origin=upper.position,offset=target-origin;
        float a=Vector3.Distance(origin,lower.position),b=Vector3.Distance(lower.position,end.position);
        float d=Mathf.Clamp(offset.magnitude,Mathf.Abs(a-b)+.005f,a+b-.005f);
        Vector3 axis=offset.normalized,bend=Vector3.ProjectOnPlane(pole-origin,axis).normalized;
        float along=(a*a-b*b+d*d)/(2*d);
        Vector3 elbow=origin+axis*along+bend*Mathf.Sqrt(Mathf.Max(0,a*a-along*along));
        upper.rotation=Quaternion.FromToRotation(lower.position-origin,elbow-origin)*upper.rotation;
        lower.rotation=Quaternion.FromToRotation(end.position-lower.position,origin+axis*d-lower.position)*lower.rotation;
        end.rotation=rotation;
    }
    private void OnDisable(){RestoreArmor();hitAge=1;hitStrength=0;}
    private void OnDestroy(){if(health!=null)health.OnDamaged-=Damaged;}
}
