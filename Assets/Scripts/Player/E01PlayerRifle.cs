using UnityEngine;

// The recovered rifle is a held weapon. Its muzzle replaces the shoulder origin only in ranged stance.
[DefaultExecutionOrder(110)]
public sealed class E01PlayerRifle : MonoBehaviour
{
    public GameObject Model { get; private set; }
    public bool Equipped { get; private set; }
    private PlayerController player;
    private RaikenBladePresentation blade;
    private Transform muzzle;
    private Vector3 primaryGrip;
    private void Awake(){player=GetComponent<PlayerController>();}
    public void SetEquipped(bool equipped)
    {
        Equipped=equipped;
        if(!equipped){if(Model!=null)Model.SetActive(false);RestoreMuzzle();return;}
        blade=GetComponentInChildren<RaikenBladePresentation>();
        if(blade==null)return;
        foreach(var t in blade.hand.GetComponentsInChildren<Transform>(true))
            if(t.name=="V3B_Sword_Grip_Socket")primaryGrip=blade.hand.InverseTransformPoint(t.position);
        if(Model==null)
        {
            Model=Instantiate(Resources.Load<GameObject>("E01/Rifle"),transform,false);Model.name="Recovered_E01_Held_Rifle";
            Model.transform.localScale=Vector3.one*1.25f;
            muzzle=Model.transform.Find("Rifle_Muzzle");
        }
        Model.SetActive(false);
    }
    public void Adopt(GameObject original)
    {
        if(Model!=null && Model!=original)Destroy(Model);
        Model=original;Model.transform.SetParent(transform,true);
        Model.transform.localScale=Vector3.one*1.25f;
        muzzle=Model.transform.Find("Rifle_Muzzle");
        Model.SetActive(true);
    }
    private void LateUpdate()
    {
        if(!Equipped||Model==null||blade==null)return;
        bool visible=player.Stance.State==WeaponStance.Ranged;
        Model.SetActive(visible);
        if(!visible){RestoreMuzzle();return;}
        Vector3 grip=blade.hand.TransformPoint(primaryGrip);
        Vector3 aim=player.HasAimPoint?player.AimPoint-grip:transform.forward;
        Model.transform.SetPositionAndRotation(grip,Quaternion.LookRotation(aim.normalized,Vector3.up));
        player.weaponController.muzzle=muzzle;
    }
    private void RestoreMuzzle(){if(blade!=null)player.weaponController.muzzle=blade.rig.muzzle;}
    private void OnDisable(){RestoreMuzzle();}
}
