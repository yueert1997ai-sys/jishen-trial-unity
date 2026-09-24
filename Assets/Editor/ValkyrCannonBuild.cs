using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;

public static class ValkyrCannonBuild
{
    public static void Build()
    {
        const string folder="Assets/Art/ValkyrCannonR1";
        var data=JsonUtility.FromJson<ExtractedHeroesIntegration.Manifest>(File.ReadAllText(folder+"/manifest.json"));
        var path="Assets/Resources/Hangar/EXTRACTED_VALKYR.prefab";
        var root=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(path));
        root.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);
        var pose=root.GetComponent<RigidMechPoseDriver>();
        var bones=new Transform[data.bones.Length];var bind=new Matrix4x4[bones.Length];
        for(int i=0;i<bones.Length;i++)
        {
            var b=data.bones[i];bones[i]=root.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name==b.name);
            var p=new Vector3(b.position[0],b.position[1],b.position[2]);var q=new Quaternion(b.rotation[0],b.rotation[1],b.rotation[2],b.rotation[3]);
            if(bones[i]==null){bones[i]=new GameObject(b.name).transform;bones[i].SetParent(bones[b.parent],false);bones[i].SetPositionAndRotation(p,q);}
            if(b.name.StartsWith("BackCannon")){bones[i].SetParent(bones[b.parent],false);bones[i].SetPositionAndRotation(p,q);}
            bind[i]=Matrix4x4.TRS(p,q,Vector3.one).inverse;
        }
        foreach(var s in ExtractedHeroesIntegration.ReadSurfaces(folder+"/model.ehm"))
        {
            if(s.kind!="Body"){Object.DestroyImmediate(s.mesh);continue;}
            s.mesh.bindposes=bind;string asset=folder+"/Body_LOD"+s.lod+".asset";
            s.mesh=ExtractedHeroesIntegration.Store(s.mesh,asset);
            var skin=root.GetComponentsInChildren<SkinnedMeshRenderer>(true).Single(t=>t.name=="VALKYR_Body_LOD"+s.lod);
            skin.sharedMesh=s.mesh;skin.bones=bones;
        }
        var cannon=root.GetComponent<ValkyrBackCannon>()??root.AddComponent<ValkyrBackCannon>();
        cannon.mounts=new[]{bones[24],bones[25]};cannon.barrels=new[]{bones[26],bones[27]};cannon.muzzles=new Transform[2];
        for(int i=0;i<2;i++)
        {
            var t=cannon.barrels[i].Find("BackCannonMuzzle");if(t==null){t=new GameObject("BackCannonMuzzle").transform;t.SetParent(cannon.barrels[i],false);}
            // Source HG_2316 terminal faces, converted with the same axis/scale as the body.
            t.position=new Vector3(i==0?.260912f:-.260912f,.2175f,-1.350f);
            t.rotation=Quaternion.LookRotation(Vector3.down,Vector3.forward);cannon.muzzles[i]=t;
        }
        PrefabUtility.SaveAsPrefabAsset(root,path);Object.DestroyImmediate(root);AssetDatabase.SaveAssets();
        CombatLoopV2Build.Build();
    }
}
