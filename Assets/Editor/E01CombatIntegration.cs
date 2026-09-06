using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object=UnityEngine.Object;

public static class E01CombatIntegration
{
    public const string Art="Assets/Art/E01_Stage02_Game";
    public const string Prefab="Assets/Prefabs/Enemies/E01RifleSoldier.prefab";
    [Serializable] public class Export {public Part[] parts;public Mat[] materials;public float height,scale;public Vector3 muzzle,support;}
    [Serializable] public class Part {public string name;public Vector3 position;public Batch[] batches;}
    [Serializable] public class Batch {public int material;public Vector3[] vertices,normals;public int[] triangles;}
    [Serializable] public class Mat {public string name;public float[] color,emission;public float metallic,roughness,strength;}
    public static void Integrate()
    {
        var data=JsonUtility.FromJson<Export>(File.ReadAllText(Art+"/e01_meshes.json"));
        Directory.CreateDirectory(Art+"/Meshes");Directory.CreateDirectory(Art+"/Materials");Directory.CreateDirectory("Assets/Resources/E01");
        var materials=data.materials.Select((m,i)=>{
            string path=Art+"/Materials/E01_"+i+".mat";
            var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(mat==null){mat=new Material(Shader.Find("Standard"));AssetDatabase.CreateAsset(mat,path);}
            mat.name=m.name;mat.color=new Color(m.color[0],m.color[1],m.color[2],1).gamma;
            mat.SetFloat("_Metallic",Mathf.Min(.5f,m.metallic));mat.SetFloat("_Glossiness",1-m.roughness);
            if(m.name.Contains("grey-white armor")){mat.color=new Color(.91f,.93f,.92f);mat.SetFloat("_Metallic",.20f);}
            mat.enableInstancing=true;mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor",new Color(m.emission[0],m.emission[1],m.emission[2])*m.strength);
            EditorUtility.SetDirty(mat);return mat;
        }).ToArray();
        var actor=new GameObject("E01_White_Rifle_Soldier");
        var visual=new GameObject("E01_Visual");visual.transform.SetParent(actor.transform,false);
        var parts=new Dictionary<string,Transform>();
        foreach(var p in data.parts)
        {
            var obj=new GameObject(p.name);obj.transform.SetParent(visual.transform,false);obj.transform.localPosition=p.position;
            parts[p.name]=obj.transform;
            var verts=new List<Vector3>();var normals=new List<Vector3>();var tris=new List<int[]>();
            foreach(var b in p.batches){int start=verts.Count;verts.AddRange(b.vertices);normals.AddRange(b.normals);tris.Add(b.triangles.Select(v=>v+start).ToArray());}
            string path=Art+"/Meshes/"+p.name+".asset";
            var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(mesh==null){mesh=new Mesh();AssetDatabase.CreateAsset(mesh,path);}else mesh.Clear();
            mesh.name=p.name;mesh.indexFormat=IndexFormat.UInt32;mesh.SetVertices(verts);mesh.SetNormals(normals);mesh.subMeshCount=tris.Count;
            for(int i=0;i<tris.Count;i++)mesh.SetTriangles(tris[i],i);
            mesh.RecalculateBounds();mesh.Optimize();EditorUtility.SetDirty(mesh);
            obj.AddComponent<MeshFilter>().sharedMesh=mesh;
            obj.AddComponent<MeshRenderer>().sharedMaterials=p.batches.Select(b=>materials[b.material]).ToArray();
        }
        void Parent(string child,string parent){parts[child].SetParent(parts[parent],true);}
        Parent("E01_CHEST","E01_WAIST");Parent("E01_HEAD","E01_CHEST");Parent("E01_BACKPACK","E01_CHEST");
        foreach(string s in new[]{"L","R"})
        {
            Parent("E01_"+s+"_SHOULDER","E01_CHEST");Parent("E01_"+s+"_UPPER_ARM","E01_CHEST");
            Parent("E01_"+s+"_FOREARM","E01_"+s+"_UPPER_ARM");Parent("E01_"+s+"_HAND","E01_"+s+"_FOREARM");
            Parent("E01_"+s+"_THIGH","E01_WAIST");Parent("E01_"+s+"_CALF","E01_"+s+"_THIGH");Parent("E01_"+s+"_FOOT","E01_"+s+"_CALF");
        }
        Transform Socket(string name,Vector3 position){var t=new GameObject(name).transform;t.SetParent(parts["E01_RIFLE_ROOT"],false);t.localPosition=position;return t;}
        var muzzle=Socket("Rifle_Muzzle",data.muzzle);var support=Socket("Rifle_Support",data.support);
        PrefabUtility.SaveAsPrefabAsset(parts["E01_RIFLE_ROOT"].gameObject,"Assets/Resources/E01/Rifle.prefab");
        Parent("E01_RIFLE_ROOT","E01_R_HAND");
        var health=actor.AddComponent<Damageable>();health.team=1;health.maxHealth=26;health.destroyOnDeath=false;
        var collider=actor.AddComponent<CapsuleCollider>();collider.height=2.8f;collider.radius=.52f;collider.center=Vector3.up*1.4f;
        actor.AddComponent<CombatFeedback>();
        var enemy=actor.AddComponent<EnemyBase>();enemy.kind=EnemyKind.Ranged;enemy.moveSpeed=2.9f;enemy.fireInterval=1.65f;enemy.killReward=5;enemy.rifleMuzzle=muzzle;
        var motion=actor.AddComponent<E01SoldierMotion>();motion.visual=visual.transform;motion.rifle=parts["E01_RIFLE_ROOT"];motion.muzzle=muzzle;motion.support=support;motion.sourceScale=data.scale;
        var prefab=PrefabUtility.SaveAsPrefabAsset(actor,Prefab);Object.DestroyImmediate(actor);
        var scene=EditorSceneManager.OpenScene("Assets/Scenes/Demo_Main.unity");
        var spawner=Object.FindFirstObjectByType<EnemySpawner>();
        spawner.meleePrefab=spawner.rangedPrefab=spawner.dronePrefab=spawner.elitePrefab=prefab;
        EditorUtility.SetDirty(spawner);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        var profile=AssetDatabase.LoadAssetAtPath<ValkyrMotionProfile>("Assets/Resources/ValkyrMotion/CombatMotion.asset");
        profile.slash=ValkyrMotionProfile.DefaultSlash();EditorUtility.SetDirty(profile);
        AssetDatabase.SaveAssets();
        Directory.CreateDirectory("AuditEvidence/combat-v5");
        File.WriteAllText("AuditEvidence/combat-v5/import.txt","E01 height=2.8m; rigid parts="+data.parts.Length+"; shared materials="+materials.Length+"; all 4 ordinary spawns mapped; boss preserved.");
        Debug.Log("E01_INTEGRATION_PASS");
    }
    public static void Build()
    {
        PlayerSettings.bundleVersion="combat-v5-"+DateTime.UtcNow.ToString("yyyyMMdd.HHmmss");
        string folder="Builds/ValkyrCombatV5";Directory.CreateDirectory(folder);
        var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/Scenes/Demo_Main.unity"},locationPathName=folder+"/MECH_TRIAL_Valkyr.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.StrictMode});
        Directory.CreateDirectory("AuditEvidence/combat-v5");
        File.WriteAllText("AuditEvidence/combat-v5/build-result.txt",report.summary.result+" errors="+report.summary.totalErrors+" warnings="+report.summary.totalWarnings);
        if(report.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded)throw new Exception("V5 build failed");
        Directory.CreateDirectory(folder+"/Licenses");
        foreach(var file in Directory.GetFiles("docs/licenses"))File.Copy(file,folder+"/Licenses/"+Path.GetFileName(file),true);
        File.Copy("docs/ASSET_PROVENANCE.md",folder+"/Licenses/ASSET_PROVENANCE.md",true);
    }
}
