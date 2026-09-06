using System.Collections.Generic;
using UnityEngine;

// Rigid armor stays editable per joint. No deformation of the source armor is required.
[DefaultExecutionOrder(95)]
public sealed class E01SoldierMotion : MonoBehaviour
{
    public Transform visual, rifle, muzzle, support;
    public float sourceScale = .84f;
    public int ShotsFired { get; private set; }
    public float RunBlend { get; private set; }
    public float GripError { get; private set; }
    public bool TrainingTarget { get; set; }
    private EnemyBase enemy;
    private Damageable health;
    private Transform waist, chest, head;
    private Vector3 lastPosition;
    private float cycle, recoil, hit, death;
    private readonly Dictionary<Transform,Vector3> positions=new Dictionary<Transform,Vector3>();
    private readonly Dictionary<Transform,Quaternion> rotations=new Dictionary<Transform,Quaternion>();
    private readonly Transform[] thighs=new Transform[2],shins=new Transform[2],feet=new Transform[2],arms=new Transform[2],forearms=new Transform[2],hands=new Transform[2];
    private Renderer[] armor;
    private MaterialPropertyBlock flash;
    private Transform Find(string name)
    {foreach(var t in visual.GetComponentsInChildren<Transform>(true))if(t.name==name)return t;return null;}
    private void Awake()
    {
        enemy=GetComponent<EnemyBase>();health=GetComponent<Damageable>();
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
        health.OnDamaged+=Damaged;
    }
    private void Damaged(Damageable target,DamageInfo info){hit=.18f;}
    public void Recoil(){recoil=1;ShotsFired++;}
    private void LateUpdate()
    {
        if(GameManager.Instance!=null&&GameManager.Instance.IsPaused)return;
        float dt=Time.deltaTime;
        Vector3 velocity=(transform.position-lastPosition)/Mathf.Max(.001f,dt);lastPosition=transform.position;
        float speed=velocity.magnitude;
        RunBlend=Mathf.MoveTowards(RunBlend,Mathf.Clamp01(speed/2.4f),dt*9);
        if(speed<12)cycle+=speed*dt/1.9f;
        recoil=Mathf.MoveTowards(recoil,0,dt*6);hit=Mathf.Max(0,hit-dt);
        foreach(var pair in positions)
            if(pair.Key!=null && (pair.Key!=rifle || rifle.IsChildOf(visual)))pair.Key.SetLocalPositionAndRotation(pair.Value,rotations[pair.Key]);
        if(health.IsDead)
        {
            death+=dt;
            visual.localPosition=Vector3.down*Mathf.Min(.85f,death*1.9f);
            visual.localRotation=Quaternion.Euler(Mathf.Min(83,death*155),0,-Mathf.Min(18,death*40));
            if(!TrainingTarget && death>.62f)Destroy(gameObject);
            return;
        }
        death=0;visual.SetLocalPositionAndRotation(Vector3.zero,Quaternion.identity);
        float wave=Mathf.Sin(cycle*Mathf.PI*2),run=RunBlend;
        waist.localPosition+=new Vector3(-wave*.055f,-.07f*run+Mathf.Abs(wave)*.035f*run,0);
        waist.localRotation=Quaternion.Euler(run*9,-wave*8*run,wave*2*run);
        chest.localRotation=Quaternion.Euler(recoil*-6+hit*40, wave*5*run, hit*25);
        head.rotation=transform.rotation;
        for(int i=0;i<2;i++)
        {
            float phase=Mathf.Repeat(cycle+i*.5f,1),s=Mathf.Sin(phase*Mathf.PI*2);
            float side=i==0?-1:1;
            float direction=Vector3.Dot(velocity,transform.forward)>=0?1:-1;
            thighs[i].localRotation=Quaternion.Euler(s*36*run*direction,side*3,-side*3);
            shins[i].localRotation=Quaternion.Euler(-Mathf.Max(0,-s)*68*run-5*run,0,0);
            feet[i].rotation=transform.rotation*Quaternion.Euler(-Mathf.Max(0,s)*16*run,side*5,0);
        }
        if(rifle==null)return; // The detached mesh may already be travelling to the warehouse.
        Vector3 grip=visual.TransformPoint(new Vector3(.34f,1.89f-run*.06f,.28f-recoil*.055f));
        Vector3 aim=enemy!=null&&enemy.target!=null?enemy.target.GetComponent<Damageable>().AimCenter-grip:transform.forward;
        Quaternion orientation=Quaternion.LookRotation(aim.normalized,transform.up);
        rifle.SetPositionAndRotation(grip,orientation);
        for(int i=0;i<2;i++)
        {
            float side=i==0?-1:1;
            Vector3 palm=new Vector3(side*.006f,-.110f,.019f)*sourceScale;
            Vector3 target=i==1?grip:support.position;
            Quaternion hand=transform.rotation*Quaternion.Euler(-80,i==1?0:-15,0);
            Solve(arms[i],forearms[i],hands[i],target-hand*palm,visual.TransformPoint(new Vector3(side*.83f,1.61f,.05f)),hand);
            if(i==1)GripError=Vector3.Distance(hands[i].TransformPoint(palm),grip);
        }
        // Keep the real muzzle attached to the carried receiver after arm posing.
        rifle.SetPositionAndRotation(grip,orientation);
        foreach(var r in armor)
        {
            if(r==null)continue;
            flash.Clear();
            if(hit>.095f)flash.SetColor("_EmissionColor",new Color(.40f,.58f,.70f));
            r.SetPropertyBlock(flash);
        }
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
    private void OnDestroy(){if(health!=null)health.OnDamaged-=Damaged;}
}
