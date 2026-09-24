using System;
using UnityEngine;
using UnityEngine.Rendering;

// Animates the original HG_2316 backpack barrels. Damage is committed once per
// bore; the lingering beam is presentation only. No alternate missile executor.
[DefaultExecutionOrder(110)]
public sealed class ValkyrBackCannon : MonoBehaviour
{
    public const float FireAt=.48f,Duration=1.35f,Range=40f;
    public Transform[] mounts,barrels,muzzles;
    public bool Active {get;private set;}
    public float Age {get;private set;}
    public int ShotsFired {get;private set;}
    public int Hits {get;private set;}
    public float Deployment=>Active?Mathf.SmoothStep(0,1,Mathf.Min(Age/.3f,(Duration-Age)/.4f)):0;
    PlayerController player;Damageable owner;Transform chest;
    Vector3[] restPosition;Quaternion[] restRotation;Vector3 aim;
    Vector3[] starts=new Vector3[2];Vector3[] ends=new Vector3[2];LineRenderer[] beams=new LineRenderer[6];
    Material material;float damage,impact;bool fired;
    void Start()
    {
        player=GetComponentInParent<PlayerController>();owner=player.GetComponent<Damageable>();
        chest=mounts[0].parent.parent;restPosition=new Vector3[2];restRotation=new Quaternion[2];
        material=OverdriveVfx.CreateJetMaterial();
        material.mainTexture=ArsenalBeam.BeamStrip; // Soft edges across the beam; full emission along its entire length.
        for(int i=0;i<2;i++)
        {
            restPosition[i]=barrels[i].localPosition;restRotation[i]=barrels[i].localRotation;
            for(int j=0;j<3;j++)
            {
                var line=new GameObject("Back cannon beam "+i+" layer "+j).AddComponent<LineRenderer>();line.transform.SetParent(transform,false);
                line.sharedMaterial=material;line.useWorldSpace=true;line.positionCount=2;line.shadowCastingMode=ShadowCastingMode.Off;line.receiveShadows=false;
                line.enabled=false;beams[i*3+j]=line;
            }
        }
    }
    public bool Deploy(Damageable target,float totalDamage,float totalImpact)
    {
        if(player==null||Active||owner.IsDead)return false;
        aim=player.HasAimPoint?player.AimPoint:target!=null&&!target.IsDead?target.AimCenter:player.transform.position+player.transform.forward*Range;
        damage=totalDamage*.5f;impact=totalImpact*.5f;Active=true;Age=0;fired=false;ShotsFired=Hits=0;return true;
    }
    public void Cancel()
    {
        Active=false;Age=0;fired=false;
        if(restPosition!=null)for(int i=0;i<2;i++)barrels[i].SetLocalPositionAndRotation(restPosition[i],restRotation[i]);
        if(mounts!=null)foreach(var mount in mounts)if(mount!=null)mount.localRotation=Quaternion.identity;
        foreach(var line in beams)if(line!=null)line.enabled=false;
    }
    void OnDisable(){Cancel();}
    void OnDestroy(){if(material!=null)Destroy(material);}
    void LateUpdate()
    {
        if(player==null)return;var gm=GameManager.Instance;
        if(owner.IsDead||gm==null||gm.Phase!=GamePhase.Combat){Cancel();return;}
        if(gm.IsPaused||Time.deltaTime<=0)return;
        if(!Active)return;
        Age+=Time.deltaTime;if(Age>=Duration){Cancel();return;}
        // Keep the submitted aim when the cursor crosses a barrel or the target disappears.
        float deploy=Deployment,recoil=fired?Mathf.Exp(-(Age-FireAt)*15)*.16f:0;
        // Brace the torso before the rifle's hand IK, without changing a live blade sweep.
        if(!player.Melee.IsAttacking)chest.rotation=Quaternion.AngleAxis(-8*deploy+recoil*35,player.transform.right)*chest.rotation;
        for(int i=0;i<2;i++)
        {
            mounts[i].localRotation=Quaternion.Slerp(Quaternion.identity,Quaternion.Euler(40,i==0?-65:65,0),deploy);
            var b=barrels[i];b.SetLocalPositionAndRotation(restPosition[i],restRotation[i]);
            var dock=b.position;var rest=b.rotation;
            // Both bores converge at the committed cursor distance on the combat plane.
            var direction=PlanarCombat.AimDirection(b.position,aim,player.transform.forward);
            var rotation=Quaternion.LookRotation(Vector3.up,-direction); // local -Y is the source bore
            b.rotation=Quaternion.Slerp(rest,rotation,deploy);b.position-=direction*recoil;
            if(!fired&&Age>.25f)BeamFxKit.ChargeConverge(muzzles[i].position,muzzles[i].forward,Color.white,Color.cyan,2,.4f);
        }
        if(!fired&&Age>=FireAt)
        {
            fired=true;for(int i=0;i<2;i++)Discharge(i);
            GameAudio.Play(GameAudioCue.Beam,.65f,.76f);Camera.main?.GetComponent<CameraFollow>()?.AddShake(.10f,.10f);
        }
        float fade=fired?Mathf.Clamp01(1-(Age-FireAt)/.38f):0;
        for(int i=0;i<2;i++)
        {
            for(int j=0;j<3;j++)
            {
                var line=beams[i*3+j];line.enabled=fade>0;line.SetPosition(0,starts[i]);line.SetPosition(1,ends[i]);
                line.startWidth=line.endWidth=(j==0?1.4f:j==1?.55f:.19f)*Mathf.Min(1,fade*4);
                var color=j==0?new Color(.02f,.35f,1,.28f*fade):j==1?new Color(.08f,.9f,1,.8f*fade):new Color(.85f,1,1,fade);
                line.startColor=line.endColor=color;
            }
            if(fade>.1f)BeamFxKit.BeamStream(starts[i],ends[i],Color.white,Color.cyan,Color.cyan,.16f,2,2);
        }
    }
    void Discharge(int index)
    {
        var muzzle=muzzles[index];starts[index]=muzzle.position;var direction=PlanarCombat.Direction(muzzle.forward,player.transform.forward);
        var hits=Physics.SphereCastAll(PlanarCombat.Point(muzzle.position),.25f,direction,Range,~0,QueryTriggerInteraction.Ignore);
        Array.Sort(hits,(a,b)=>a.distance.CompareTo(b.distance));ends[index]=muzzle.position+direction*Range;
        foreach(var hit in hits)
        {
            var target=hit.collider.GetComponentInParent<Damageable>();
            if(target!=null&&(target==owner||target.team==owner.team||target.IsDead))continue;
            ends[index]=muzzle.position+direction*hit.distance;
            if(target!=null)
            {
                target.ApplyDamage(damage,new DamageInfo(gameObject,muzzle.position,owner,damage){Kind=CombatHitKind.HeavyRifle,Impact=impact,HeavyImpact=true});Hits++;
            }
            BeamFxKit.ImpactBurst(ends[index],direction,Color.cyan,1.2f);break;
        }
        ShotsFired++;BeamFxKit.MuzzleBlast(muzzle.position,direction,Color.cyan,1.1f);
        BeamFxKit.StarGlare(muzzle.position,Color.white,1.4f,.13f);
    }
}
