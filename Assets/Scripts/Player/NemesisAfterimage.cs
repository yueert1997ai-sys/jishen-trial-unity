using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

// Four reusable, single-material rigid-skin snapshots. No per-frame mesh baking,
// physics actors, or cloned controllers. A snapshot retains its world pose.
[DefaultExecutionOrder(180)]
public sealed class NemesisAfterimage : MonoBehaviour
{
    public Mesh bodyProxy;
    const int Capacity=4;
    const float Life=.24f,Interval=.06f;
    sealed class Ghost
    {
        public Transform root;public Transform[] joints;
        public SkinnedMeshRenderer body;public MeshRenderer weapon;
        public MeshFilter weaponMesh;public float remaining,opacity;
    }
    Ghost[] pool;
    SkinnedMeshRenderer source;
    Transform[] sourceBones;
    PlayerController player;Damageable health;LoadoutVisual loadout;RaikenBladePresentation blade;
    Material material;
    MaterialPropertyBlock block;
    int next;float nextCapture;Vector3 lastPosition;
    public int ActiveCount {get;private set;}
    public int Captures {get;private set;}
    public bool Supported=>material!=null&&material.shader.isSupported;
    void Start()
    {
        block=new MaterialPropertyBlock();
        player=GetComponentInParent<PlayerController>();health=player.GetComponent<Damageable>();
        loadout=GetComponent<LoadoutVisual>();blade=GetComponent<RaikenBladePresentation>();
        source=GetComponentsInChildren<SkinnedMeshRenderer>(true).Single(s=>s.name=="J01_Body_LOD2");
        sourceBones=source.bones;
        // Ghosts capture the chassis only; deployed units have their own world-space shells.
        bodyProxy=Instantiate(bodyProxy);
        var droneBones=GetComponent<NemesisMotionRig>().Drones.Select(t=>Array.IndexOf(sourceBones,t)).ToArray();
        var weights=bodyProxy.boneWeights;
        for(int s=0;s<bodyProxy.subMeshCount;s++)
        {
            var tris=bodyProxy.GetTriangles(s);var keep=new System.Collections.Generic.List<int>();
            for(int t=0;t<tris.Length;t+=3)
                if(!droneBones.Contains(weights[tris[t]].boneIndex0))for(int k=0;k<3;k++)keep.Add(tris[t+k]);
            bodyProxy.SetTriangles(keep,s);
        }
        material=new Material(Resources.Load<Shader>("NemesisGhost"));
        pool=new Ghost[Capacity];int rootIndex=Array.IndexOf(sourceBones,source.rootBone);
        for(int i=0;i<Capacity;i++)
        {
            var ghost=new Ghost();ghost.root=new GameObject("J01 afterimage "+i).transform;
            ghost.joints=new Transform[sourceBones.Length];
            for(int b=0;b<ghost.joints.Length;b++)
            {var t=new GameObject("Pose "+b).transform;t.SetParent(ghost.root,false);ghost.joints[b]=t;}
            ghost.body=ghost.root.gameObject.AddComponent<SkinnedMeshRenderer>();ghost.body.sharedMesh=bodyProxy;
            ghost.body.bones=ghost.joints;ghost.body.rootBone=ghost.joints[Mathf.Max(0,rootIndex)];
            ghost.body.sharedMaterial=material;ghost.body.quality=SkinQuality.Bone1;
            ghost.body.localBounds=source.localBounds;ghost.body.shadowCastingMode=ShadowCastingMode.Off;
            ghost.body.receiveShadows=false;ghost.body.lightProbeUsage=LightProbeUsage.Off;ghost.body.reflectionProbeUsage=ReflectionProbeUsage.Off;
            var w=new GameObject("Held weapon");w.transform.SetParent(ghost.root,false);
            ghost.weaponMesh=w.AddComponent<MeshFilter>();ghost.weapon=w.AddComponent<MeshRenderer>();ghost.weapon.sharedMaterial=material;
            ghost.weapon.shadowCastingMode=ShadowCastingMode.Off;ghost.weapon.receiveShadows=false;
            ghost.weapon.lightProbeUsage=LightProbeUsage.Off;ghost.weapon.reflectionProbeUsage=ReflectionProbeUsage.Off;
            ghost.root.gameObject.SetActive(false);pool[i]=ghost;
        }
        lastPosition=player.transform.position;
    }
    void LateUpdate()
    {
        if(pool==null)return;
        var gm=GameManager.Instance;
        if(gm==null||health.IsDead||gm.Phase!=GamePhase.Combat){Clear();return;}
        if(gm.IsPaused)return;
        ActiveCount=0;
        foreach(var ghost in pool)
        {
            ghost.remaining=Mathf.Max(0,ghost.remaining-Time.deltaTime);
            ghost.root.gameObject.SetActive(ghost.remaining>0);
            if(ghost.remaining<=0)continue;
            ActiveCount++;
            var color=NemesisMotionRig.Amethyst;color.a=ghost.opacity*Mathf.Pow(ghost.remaining/Life,1.25f);
            block.SetColor("_Color",color);ghost.body.SetPropertyBlock(block);ghost.weapon.SetPropertyBlock(block);
        }
        if((player.IsDashing||player.IsBoosting||player.Velocity.magnitude>4)&&Time.time>=nextCapture&&Vector3.Distance(lastPosition,player.transform.position)>.24f)
        {Capture();nextCapture=Time.time+(player.IsDashing||player.IsBoosting?Interval:.11f);lastPosition=player.transform.position;}
    }
    void Capture()
    {
        var ghost=pool[next++%Capacity];
        ghost.root.SetPositionAndRotation(source.transform.position,source.transform.rotation);ghost.root.localScale=source.transform.lossyScale;
        for(int i=0;i<ghost.joints.Length;i++)
        {
            ghost.joints[i].SetPositionAndRotation(sourceBones[i].position,sourceBones[i].rotation);
            var scale=sourceBones[i].lossyScale;var rootScale=ghost.root.lossyScale;
            ghost.joints[i].localScale=new Vector3(scale.x/rootScale.x,scale.y/rootScale.y,scale.z/rootScale.z);
        }
        NemesisGhostMesh held=null;
        if(loadout.WeaponObject!=null&&loadout.WeaponObject.activeInHierarchy)held=loadout.WeaponObject.GetComponent<NemesisGhostMesh>();
        else if(blade.bladeRoot.gameObject.activeInHierarchy)held=blade.bladeRoot.GetComponent<NemesisGhostMesh>();
        ghost.weapon.enabled=held!=null;
        if(held!=null)
        {
            ghost.weaponMesh.sharedMesh=held.proxy;
            ghost.weapon.transform.SetPositionAndRotation(held.transform.position,held.transform.rotation);
            ghost.weapon.transform.localScale=held.transform.lossyScale/ghost.root.lossyScale.x;
        }
        ghost.remaining=Life;ghost.opacity=player.IsDashing||player.IsBoosting?.42f:.16f;ghost.root.gameObject.SetActive(true);Captures++;
        var color=NemesisMotionRig.Amethyst;color.a=ghost.opacity;block.SetColor("_Color",color);
        ghost.body.SetPropertyBlock(block);ghost.weapon.SetPropertyBlock(block);
        ActiveCount=pool.Count(g=>g.remaining>0);
    }
    public void Clear()
    {
        if(pool!=null)foreach(var ghost in pool){ghost.remaining=0;if(ghost.root!=null)ghost.root.gameObject.SetActive(false);}
        ActiveCount=0;nextCapture=0;if(player!=null)lastPosition=player.transform.position;
    }
    void OnDisable(){Clear();}
    void OnDestroy()
    {
        if(pool!=null)foreach(var ghost in pool)if(ghost.root!=null)Destroy(ghost.root.gameObject);
        if(material!=null)Destroy(material);if(bodyProxy!=null)Destroy(bodyProxy);
    }
}
