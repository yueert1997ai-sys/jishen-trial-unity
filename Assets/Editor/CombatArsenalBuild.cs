using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using Object=UnityEngine.Object;

public static class CombatArsenalBuild
{
    const string Art="Assets/Art/CombatArsenalR2";
    [Serializable] class Bone {public string name,source;public int parent;public float[] position,rotation;}
    [Serializable] class Surface {public string name,albedo,emission;}
    [Serializable] class Marker {public string name;public int bone;public float[] position;}
    [Serializable] class Data {public string label;public float height;public Bone[] bones;public Surface[] materials;public Marker[] markers;}
    static Vector3 V(float[] a)=>new Vector3(a[0],a[1],a[2]);
    public static void Build(){Integrate();CombatLoopV2Build.Build();}
    public static void Integrate()
    {
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        Directory.CreateDirectory("Assets/Resources/Enemies/Arsenal");
        foreach(var label in new[]{"GM","ZAKU","DOM","GUNCANNON","FAZZ"})Import(label);
        foreach(var path in Directory.GetFiles("Assets/Resources/Audio/Combat/ArsenalR2","*.wav"))AssetDatabase.ImportAsset(path.Replace('\\','/'),ImportAssetOptions.ForceUpdate);
        AssetDatabase.SaveAssets();Debug.Log("ARSENAL_MODELS_INTEGRATED");
    }
    static void Import(string label)
    {
        var folder=Art+"/"+label;var data=JsonUtility.FromJson<Data>(File.ReadAllText(folder+"/manifest.json"));
        var root=new GameObject(label);var transforms=new Transform[data.bones.Length];
        for(int i=0;i<transforms.Length;i++)
        {
            var b=data.bones[i];var t=new GameObject(b.name).transform;t.SetParent(root.transform,false);
            t.SetPositionAndRotation(V(b.position),new Quaternion(b.rotation[0],b.rotation[1],b.rotation[2],b.rotation[3]));transforms[i]=t;
        }
        for(int i=0;i<transforms.Length;i++)if(data.bones[i].parent>=0)transforms[i].SetParent(transforms[data.bones[i].parent],true);
        var mesh=new Mesh{name=label+" original rigid armor"};
        using(var r=new BinaryReader(File.OpenRead(folder+"/model.bin")))
        {
            int count=r.ReadInt32(),subs=r.ReadInt32();var vertices=new Vector3[count];var normals=new Vector3[count];var uv=new Vector2[count];var weights=new BoneWeight[count];
            if(count>65535)mesh.indexFormat=UnityEngine.Rendering.IndexFormat.UInt32;
            for(int i=0;i<count;i++)
            {vertices[i]=new Vector3(r.ReadSingle(),r.ReadSingle(),r.ReadSingle());normals[i]=new Vector3(r.ReadSingle(),r.ReadSingle(),r.ReadSingle());uv[i]=new Vector2(r.ReadSingle(),r.ReadSingle());weights[i]=new BoneWeight{boneIndex0=r.ReadInt32(),weight0=1};}
            mesh.vertices=vertices;mesh.normals=normals;mesh.uv=uv;mesh.boneWeights=weights;mesh.subMeshCount=subs;
            for(int s=0;s<subs;s++){var tri=new int[r.ReadInt32()];for(int i=0;i<tri.Length;i++)tri[i]=r.ReadInt32();mesh.SetTriangles(tri,s);}
        }
        mesh.bindposes=transforms.Select(t=>t.worldToLocalMatrix).ToArray();mesh.RecalculateBounds();
        string meshPath=folder+"/Body.asset";var old=AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
        if(old!=null){EditorUtility.CopySerialized(mesh,old);Object.DestroyImmediate(mesh);mesh=old;}else AssetDatabase.CreateAsset(mesh,meshPath);
        var materials=new Material[data.materials.Length];
        for(int i=0;i<materials.Length;i++)
        {
            var row=data.materials[i];string path=folder+"/"+row.name+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(mat==null){mat=new Material(Shader.Find("Standard"));AssetDatabase.CreateAsset(mat,path);}
            mat.color=Color.white;mat.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(folder+"/"+row.albedo);
            if(mat.mainTexture==null)throw new Exception("Missing paint "+row.albedo);
            mat.SetFloat("_Metallic",.35f);mat.SetFloat("_Glossiness",.48f);mat.SetTexture("_EmissionMap",AssetDatabase.LoadAssetAtPath<Texture2D>(folder+"/"+row.emission));mat.SetColor("_EmissionColor",Color.white);mat.EnableKeyword("_EMISSION");EditorUtility.SetDirty(mat);materials[i]=mat;
        }
        var skin=root.AddComponent<SkinnedMeshRenderer>();skin.sharedMesh=mesh;skin.bones=transforms;skin.rootBone=transforms[0];skin.sharedMaterials=materials;skin.quality=SkinQuality.Bone1;skin.localBounds=new Bounds(Vector3.up*data.height*.5f,new Vector3(16,12,16));
        var model=root.AddComponent<ImportedEnemyModel>();model.modelId=label;model.bones=transforms;model.sourceNames=data.bones.Select(b=>b.source).ToArray();model.height=data.height;
        model.muzzles=data.markers.Select(m=>{var t=new GameObject(m.name).transform;t.position=V(m.position);t.SetParent(transforms[m.bone],true);return t;}).ToArray();
        if(label=="GUNCANNON")BuildRecoveredCannons(root,mesh,transforms,materials,model.muzzles,folder);
        PrefabUtility.SaveAsPrefabAsset(root,"Assets/Resources/Enemies/Arsenal/"+label+".prefab");Object.DestroyImmediate(root);
    }
    static void BuildRecoveredCannons(GameObject source,Mesh mesh,Transform[] bones,Material[] materials,Transform[] markers,string folder)
    {
        var root=new GameObject("GUNCANNON twin back cannons");var module=root.AddComponent<BackCannonMount>();module.muzzles=new Transform[2];
        var verts=mesh.vertices;var normals=mesh.normals;var uv=mesh.uv;var weights=mesh.boneWeights;
        for(int side=0;side<2;side++)
        {
            int bone=Array.FindIndex(bones,t=>t.name=="chr100109_J_bodyExtra_"+(180+side));
            var port=markers.First(t=>t.name.StartsWith("efLocator_body_10"+(side+1)));
            var rotation=Quaternion.FromToRotation((port.position-bones[bone].position).normalized,Vector3.forward);
            var pivot=new Vector3(side==0?-.48f:.48f,0,0);var v=new System.Collections.Generic.List<Vector3>();var n=new System.Collections.Generic.List<Vector3>();var tex=new System.Collections.Generic.List<Vector2>();
            var triangles=new System.Collections.Generic.List<int>[mesh.subMeshCount];
            for(int s=0;s<mesh.subMeshCount;s++)
            {
                triangles[s]=new System.Collections.Generic.List<int>();var original=mesh.GetTriangles(s);
                for(int at=0;at<original.Length;at+=3)
                    if(weights[original[at]].boneIndex0==bone)for(int j=0;j<3;j++)
                    {int i=original[at+j];triangles[s].Add(v.Count);v.Add(pivot+rotation*(verts[i]-bones[bone].position));n.Add(rotation*normals[i]);tex.Add(uv[i]);}
            }
            var piece=new Mesh{name="Recovered native barrel "+side};piece.SetVertices(v);piece.SetNormals(n);piece.SetUVs(0,tex);piece.subMeshCount=triangles.Length;
            for(int s=0;s<triangles.Length;s++)piece.SetTriangles(triangles[s],s);piece.RecalculateBounds();
            string path=folder+"/RecoveredCannon"+side+".asset";var saved=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(saved!=null){EditorUtility.CopySerialized(piece,saved);Object.DestroyImmediate(piece);piece=saved;}else AssetDatabase.CreateAsset(piece,path);
            var shell=new GameObject("Cannon "+side);shell.transform.SetParent(root.transform,false);shell.AddComponent<MeshFilter>().sharedMesh=piece;shell.AddComponent<MeshRenderer>().sharedMaterials=materials;
            var muzzle=new GameObject(side==0?"Muzzle":"MuzzleRight").transform;muzzle.SetParent(root.transform,false);muzzle.localPosition=pivot+rotation*(port.position-bones[bone].position);module.muzzles[side]=muzzle;
        }
        PrefabUtility.SaveAsPrefabAsset(root,"Assets/Resources/Hangar/GC_BACK_CANNON.prefab");Object.DestroyImmediate(root);
    }
}
