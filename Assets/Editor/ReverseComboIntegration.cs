using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using UnityEditor.Build.Reporting;

public static class ReverseComboIntegration
{
    const string Art="Assets/Art/ValkyrHand_V6", Profile="Assets/Resources/ValkyrMotion/ReverseCombo.asset";
    [Serializable] public class Export{public Part[] parts;public string[] materials;public Vector3 grip,axis;}
    [Serializable] public class Part{public string name,parent;public Vector3 position;public Batch[] batches;}
    [Serializable] public class Batch{public int material;public Vector3[] vertices,normals;public int[] triangles;}
    public static void Prepare()
    {
        var data=JsonUtility.FromJson<Export>(File.ReadAllText(Art+"/hand_meshes.json"));
        Directory.CreateDirectory(Art+"/Meshes");
        var existing=AssetDatabase.FindAssets("t:Material",new[]{"Assets/Art/ValkyrRaiken_V3/Materials"})
            .Select(g=>AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(g))).ToDictionary(m=>m.name);
        var material=data.materials.Select(n=>existing[n]).ToArray();
        var root=new GameObject("V6_Articulated_Right_Hand");var map=new Dictionary<string,Transform>();
        foreach(var p in data.parts)
        {
            var o=new GameObject(p.name);o.transform.SetParent(string.IsNullOrEmpty(p.parent)?root.transform:map[p.parent],false);o.transform.localPosition=p.position;map[p.name]=o.transform;
            var vertices=new List<Vector3>();var normals=new List<Vector3>();var triangles=new List<int[]>();
            foreach(var b in p.batches){int start=vertices.Count;vertices.AddRange(b.vertices);normals.AddRange(b.normals);triangles.Add(b.triangles.Select(t=>t+start).ToArray());}
            string path=Art+"/Meshes/"+p.name+".asset";var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(mesh==null){mesh=new Mesh();AssetDatabase.CreateAsset(mesh,path);}else mesh.Clear();
            mesh.name="V6_"+p.name;mesh.indexFormat=IndexFormat.UInt32;mesh.SetVertices(vertices);mesh.SetNormals(normals);mesh.subMeshCount=triangles.Count;
            for(int i=0;i<triangles.Count;i++)mesh.SetTriangles(triangles[i],i);mesh.RecalculateBounds();EditorUtility.SetDirty(mesh);
            o.AddComponent<MeshFilter>().sharedMesh=mesh;o.AddComponent<MeshRenderer>().sharedMaterials=p.batches.Select(b=>material[b.material]).ToArray();
        }
        // Commit generated meshes before the prefab serializer traverses their references.
        AssetDatabase.SaveAssets();
        var grip=root.AddComponent<ValkyrHandGrip>();grip.grip=data.grip;grip.fingerAxis=data.axis;grip.fingers=map.Where(k=>k.Key!="Palm").Select(k=>k.Value).ToArray();
        var handPrefab=PrefabUtility.SaveAsPrefabAsset(root,"Assets/Resources/ValkyrMotion/RightHand_V6.prefab");UnityEngine.Object.DestroyImmediate(root);
        const string visualPath="Assets/Prefabs/Player/ValkyrRaikenVisual.prefab";
        var model=PrefabUtility.LoadPrefabContents(visualPath);
        try
        {
            var hand=model.GetComponentsInChildren<Transform>(true).Single(t=>t.name=="Hand.R");
            var old=hand.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name=="LOD0_Hand.R_V3");if(old!=null)old.gameObject.SetActive(false);
            var previous=hand.GetComponentInChildren<ValkyrHandGrip>(true);if(previous!=null)UnityEngine.Object.DestroyImmediate(previous.gameObject);
            var child=(GameObject)PrefabUtility.InstantiatePrefab(handPrefab);child.transform.SetParent(hand,false);
            PrefabUtility.SaveAsPrefabAsset(model,visualPath);
        }
        finally{PrefabUtility.UnloadPrefabContents(model);}
        var profile=AssetDatabase.LoadAssetAtPath<ValkyrComboProfile>(Profile);
        if(profile==null){profile=ScriptableObject.CreateInstance<ValkyrComboProfile>();AssetDatabase.CreateAsset(profile,Profile);}
        profile.strokes=ValkyrComboProfile.Defaults();EditorUtility.SetDirty(profile);AssetDatabase.SaveAssets();
        Debug.Log("V6_HAND_AND_COMBO_READY");
    }
    public static void PrepareAndBuild(){Prepare();Build();}
    public static void Build()
    {
        PlayerSettings.bundleVersion="reverse-combo-v6-"+DateTime.UtcNow.ToString("yyyyMMdd.HHmmss");
        string folder="Builds/ValkyrReverseComboV6";Directory.CreateDirectory(folder);
        var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/Scenes/Demo_Main.unity"},locationPathName=folder+"/MECH_TRIAL_Valkyr.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.StrictMode});
        Directory.CreateDirectory("AuditEvidence/reverse-combo-v6");
        File.WriteAllText("AuditEvidence/reverse-combo-v6/build-result.txt",report.summary.result+" errors="+report.summary.totalErrors+" warnings="+report.summary.totalWarnings);
        if(report.summary.result!=BuildResult.Succeeded)throw new Exception("V6 build failed");
        Directory.CreateDirectory(folder+"/Licenses");foreach(var f in Directory.GetFiles("docs/licenses"))File.Copy(f,folder+"/Licenses/"+Path.GetFileName(f),true);
        File.Copy("docs/ASSET_PROVENANCE.md",folder+"/Licenses/ASSET_PROVENANCE.md",true);
    }
}
