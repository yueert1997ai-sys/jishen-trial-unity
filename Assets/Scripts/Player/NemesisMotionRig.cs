using System;
using System.Linq;
using UnityEngine;

// J-01 proportions and rigid appendages. The existing combat driver still owns
// animation time, hit windows, planted feet, cancellation and the three-cut chain.
[DefaultExecutionOrder(100)]
public sealed class NemesisMotionRig : MonoBehaviour
{
    public string sourceSha256;
    public static readonly Color Amethyst=MechEnergyPalette.Nemesis;
    public PlayerController Player {get;private set;}
    public Transform[] Drones {get;private set;}
    public float Deployment {get;private set;}
    public float WingOpen {get;private set;}
    Transform[] skirts,wings,thighs,shins;
    Quaternion[] skirtRest,wingRest,droneRest;
    Vector3[] dronePosition;
    Transform waist,thorax;
    Vector3 waistRest;
    NemesisDroneController support;
    ValkyrMotionDriver motion;
    RaikenBladePresentation blade;
    NemesisRaikenBlade largeBlade;
    float wingHoldUntil,wingBank;
    Transform Part(string n)=>GetComponentsInChildren<Transform>(true).Single(t=>t.name==n);
    void Awake()
    {
        Player=GetComponentInParent<PlayerController>();motion=GetComponent<ValkyrMotionDriver>();blade=GetComponent<RaikenBladePresentation>();
        largeBlade=blade.bladeRoot.GetComponent<NemesisRaikenBlade>();
        waist=Part("Waist");thorax=Part("Thorax");waistRest=waist.localPosition;
        skirts=new[]{Part("SKIRT_FRONT_PIVOT.L"),Part("SKIRT_FRONT_PIVOT.R"),Part("SKIRT_SIDE_PIVOT.L"),Part("SKIRT_SIDE_PIVOT.R")};
        thighs=new[]{Part("Thigh.L"),Part("Thigh.R")};shins=new[]{Part("Shin.L"),Part("Shin.R")};
        wings=new[]{Part("WING_GIMBAL.L"),Part("WING_GIMBAL.R")};
        Drones=Enumerable.Range(1,6).Select(i=>Part("DRONE_"+i.ToString("00")+"_ROOT")).ToArray();
        skirtRest=skirts.Select(t=>t.localRotation).ToArray();wingRest=wings.Select(t=>t.localRotation).ToArray();
        droneRest=Drones.Select(t=>t.localRotation).ToArray();dronePosition=Drones.Select(t=>t.localPosition).ToArray();
    }
    void Start()
    {
        support=GetComponent<NemesisDroneController>();
    }
    public void ResetDeployment(){support?.Cancel();Deployment=0;WingOpen=0;wingHoldUntil=0;wingBank=0;GetComponent<NemesisAfterimage>()?.Clear();}
    public void ExtendLumbar(float pitch)
    {waist.localPosition=waistRest+Vector3.up*Mathf.Min(.083f,Mathf.Max(0,Mathf.Abs(pitch)-3)*.0031f);}
    public void ParkBlade()
    {
        var direction=Player.transform.TransformDirection(new Vector3(-.15f,-.985f,-.07f));
        blade.bladeRoot.rotation=Quaternion.LookRotation(direction,Player.transform.right);
        blade.bladeRoot.position=thorax.position+Player.transform.TransformDirection(new Vector3(-.65f,.20f,-.47f));
        if(largeBlade!=null)blade.bladeRoot.position+=Vector3.up*Mathf.Max(0,Player.transform.position.y+.18f-blade.tip.position.y);
    }
    public void DroneDock(int index,out Vector3 position,out Quaternion rotation)
    {
        var parent=Drones[index].parent;float side=index<3?1:-1;int rank=index%3;
        position=parent.TransformPoint(dronePosition[index]);
        rotation=parent.rotation*droneRest[index]*Quaternion.Euler(0,0,side*(rank-1)*WingOpen*14);
    }
    void LateUpdate()
    {
        if(GameManager.Instance==null||GameManager.Instance.IsPaused)return;
        float deploy=support!=null?support.Deployment:0;
        Deployment=deploy;
        float air=motion.FlightBlend;
        bool active=GameManager.Instance.IsCombatActive&&!Player.GetComponent<Damageable>().IsDead;
        float speed=Player.Velocity.magnitude;
        bool moving=active&&(speed>.8f||Player.IsDashing||Player.IsBoosting);
        if(moving)wingHoldUntil=Time.time+.35f;
        float targetWing=!active?0:Player.IsDashing||Player.IsBoosting?1:moving||Time.time<wingHoldUntil?.82f:0;
        WingOpen=Mathf.MoveTowards(WingOpen,targetWing,Time.deltaTime*(targetWing>WingOpen?9:2.8f));
        float bank=active?Mathf.Clamp(Vector3.Dot(Player.Velocity,Player.transform.right)/10.4f,-1,1)*5:0;
        wingBank=Mathf.Lerp(wingBank,bank,1-Mathf.Exp(-12*Time.deltaTime));
        for(int i=0;i<2;i++)
        {
            float side=i==0?-1:1;
            // Use the actual shin pivot, not a mesh child or the hip yaw, for skirt clearance.
            var axis=Player.transform.InverseTransformDirection(shins[i].position-thighs[i].position);
            float opening=Mathf.Clamp(Mathf.Atan2(axis.z,-axis.y)*Mathf.Rad2Deg,0,60);
            skirts[i].localRotation=skirtRest[i]*Quaternion.Euler(-opening*.78f,0,0);
            skirts[i+2].localRotation=skirtRest[i+2]*Quaternion.Euler(0,0,-side*(4+8*air));
            wings[i].localRotation=wingRest[i]*Quaternion.Euler(WingOpen*10,0,side*(WingOpen*52+deploy*11)+wingBank*WingOpen);
        }
        for(int i=0;i<6;i++)
        {
            float side=i<3?1:-1;int rank=i%3;
            if(support==null||!support.Active)
            {DroneDock(i,out var p,out var q);Drones[i].SetPositionAndRotation(p,q);}
        }
        if(GameManager.Instance.Phase==GamePhase.Hangar)ParkBlade();
    }
}
