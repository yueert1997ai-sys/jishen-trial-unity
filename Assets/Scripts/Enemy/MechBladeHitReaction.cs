using UnityEngine;

// Brief armor flash and visual recoil. Does not alter enemy AI, colliders or damage.
[DefaultExecutionOrder(180)]
public sealed class MechBladeHitReaction : MonoBehaviour
{
    private Renderer[] renderers;
    private bool authoredSoldier;
    private MaterialPropertyBlock[] original;
    private readonly MaterialPropertyBlock block=new MaterialPropertyBlock();
    private Transform chest,hips;
    private Quaternion chestBefore,hipsBefore;
    private Vector3 axis;
    private float remaining;
    private float duration=.24f,weight=1;
    private bool posed,flashed;
    private void Awake()
    {
        authoredSoldier=GetComponent<E01SoldierMotion>()!=null;
        renderers=GetComponentsInChildren<MeshRenderer>();original=new MaterialPropertyBlock[renderers.Length];
        for(int i=0;i<renderers.Length;i++){original[i]=new MaterialPropertyBlock();renderers[i].GetPropertyBlock(original[i]);}
        var rig=GetComponentInChildren<RiggedMechAnimator>();
        if(rig!=null){chest=rig.rigidPose!=null?rig.rigidPose.Resolve(rig.chest):rig.chest;hips=rig.rigidPose!=null?rig.rigidPose.Resolve(rig.hips):rig.hips;}
    }
    public void Trigger(Vector3 direction,bool heavy=false)
    {
        if(authoredSoldier)return; // The production soldier owns its pose and armor response.
        duration=heavy?.30f:.16f;
        weight=heavy?1.65f:.8f;
        remaining=duration;axis=Vector3.Cross(Vector3.up,direction).normalized;
    }
    private void Update(){RestorePose();}
    private void LateUpdate()
    {
        if(GameManager.Instance!=null&&GameManager.Instance.IsPaused)return;
        if(remaining<=0){RestoreColor();return;}
        remaining=Mathf.Max(0,remaining-Time.deltaTime);
        float t=duration-remaining;
        if(t<(weight>1?.055f:.035f))
        {
            for(int i=0;i<renderers.Length;i++)if(renderers[i]!=null)
            {
                renderers[i].GetPropertyBlock(block);block.SetColor("_Color",new Color(.55f,.83f,1));
                block.SetColor("_EmissionColor",new Color(.25f,.7f,1)*1.6f);renderers[i].SetPropertyBlock(block);
            }
            flashed=true;
        }
        else RestoreColor();
        float recoil=weight*(1-Mathf.Exp(-t*160))*Mathf.Pow(Mathf.Clamp01(1-t/duration),2);
        if(hips!=null){hipsBefore=hips.rotation;hips.rotation=Quaternion.AngleAxis(6*recoil,axis)*hips.rotation;}
        if(chest!=null){chestBefore=chest.rotation;chest.rotation=Quaternion.AngleAxis(15*recoil,axis)*chest.rotation;}
        posed=true;
    }
    private void RestorePose()
    {
        if(!posed)return;
        if(hips!=null)hips.rotation=hipsBefore;if(chest!=null)chest.rotation=chestBefore;posed=false;
    }
    private void RestoreColor()
    {
        if(!flashed)return;
        for(int i=0;i<renderers.Length;i++)if(renderers[i]!=null)renderers[i].SetPropertyBlock(original[i]);flashed=false;
    }
    private void OnDisable(){RestorePose();RestoreColor();remaining=0;}
}
