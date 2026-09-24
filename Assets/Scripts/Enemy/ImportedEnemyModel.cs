using System;
using UnityEngine;

[DefaultExecutionOrder(80)]
public sealed class ImportedEnemyModel : MonoBehaviour
{
    public string modelId;
    public Transform[] bones;
    public string[] sourceNames;
    public Transform[] muzzles;
    public float height=4.5f;
    public Transform Chest=>Find("Thorax");
    public Transform Find(string name){foreach(var bone in bones)if(bone.name==name)return bone;return null;}
    ImportedBodyMotion locomotion,special;ImportedBodyMotion.Joint[] pose;
    Quaternion[] rest,world;Vector3[] local;
    Damageable health;EnemyBase enemy;Vector3 last,hitDirection;float cycle,hitAge=1,hitDuration=.18f,hitPower,death;
    string specialClip;float specialTime;
    public void Play(string clip,float time){specialClip=clip;specialTime=time;}
    void Awake()
    {
        health=GetComponentInParent<Damageable>();enemy=GetComponentInParent<EnemyBase>();
        locomotion=ImportedBodyMotion.Load();if(modelId=="FAZZ")special=ImportedBodyMotion.LoadBank("fazz");
        rest=new Quaternion[bones.Length];world=new Quaternion[bones.Length];local=new Vector3[bones.Length];
        for(int i=0;i<bones.Length;i++){rest[i]=bones[i].localRotation;world[i]=Quaternion.Inverse(transform.rotation)*bones[i].rotation;local[i]=bones[i].localPosition;}
        last=transform.parent.position;if(health!=null)health.OnDamaged+=Hit;
    }
    void Hit(Damageable victim,DamageInfo info)
    {
        hitDirection=Vector3.ProjectOnPlane(info.HasContact&&info.MeleeStrike?info.ContactTangent:transform.position-info.SourcePosition,Vector3.up).normalized;
        hitAge=0;hitPower=info.HeavyImpact?2.2f:info.MeleeStrike?1.6f:.55f;hitDuration=info.HeavyImpact?.30f:info.MeleeStrike?.18f:.10f;
    }
    void LateUpdate()
    {
        if(GameManager.Instance?.IsPaused==true)return;
        float dt=Time.deltaTime;hitAge+=dt;
        transform.localPosition=Vector3.zero;transform.localRotation=Quaternion.identity;
        var root=transform.parent;Vector3 delta=root.position-last;last=root.position;float speed=delta.magnitude/Mathf.Max(.001f,dt);
        cycle+=Mathf.Min(delta.magnitude,1)/2.6f;
        for(int i=0;i<bones.Length;i++)bones[i].SetLocalPositionAndRotation(local[i],rest[i]);
        if(health!=null&&health.IsDead)
        {
            death+=dt;float weight=Mathf.SmoothStep(0,1,death/.60f);
            transform.localRotation=Quaternion.AngleAxis(78*weight,root.InverseTransformDirection(Vector3.Cross(Vector3.up,hitDirection.sqrMagnitude>.01f?hitDirection:root.forward)));
            transform.localPosition=Vector3.down*(height*.12f*weight);
            if(enemy!=null&&death>.72f)Destroy(root.gameObject);return;
        }
        var bank=specialClip!=null&&special!=null?special:locomotion;if(bank==null)return;
        string name=specialClip??(modelId=="DOM"?"rfl_boost":speed>.3f?"rfl_run":"rfl_idle");
        var clip=bank.clips[name];if(pose==null||pose.Length!=bank.Count)pose=new ImportedBodyMotion.Joint[bank.Count];
        clip.Sample(specialClip!=null?Mathf.Clamp01(specialTime)*clip.Duration:Mathf.Repeat(speed>.3f?cycle*clip.Duration:Time.time,clip.Duration),pose);
        for(int i=0;i<bones.Length;i++)
        {
            int at=Array.IndexOf(bank.names,sourceNames[i]);if(at<0)continue;
            var rotation=pose[at].q*Quaternion.Inverse(bank.bind[at].q);
            bones[i].rotation=transform.rotation*rotation*world[i];
        }
        float weightHit=(1-Mathf.Exp(-hitAge*180))*Mathf.Pow(Mathf.Clamp01(1-hitAge/hitDuration),.8f)*hitPower;
        Chest.rotation=Quaternion.AngleAxis(weightHit*19,Vector3.Cross(Vector3.up,hitDirection))*Chest.rotation;
        if(modelId=="DOM")transform.localPosition=Vector3.up*(.14f+.025f*Mathf.Sin(Time.time*7));
        // Ground both imported feet with the same root correction, preserving the original leg proportions.
        float low=Mathf.Min(Find("Foot.L").position.y,Find("Foot.R").position.y)-root.position.y;
        if(low<.12f)transform.position+=Vector3.up*(.12f-low);
    }
    void OnDestroy(){if(health!=null)health.OnDamaged-=Hit;}
}
