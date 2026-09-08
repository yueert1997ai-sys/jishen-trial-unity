using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

public static class HangarIntegration
{
    const string Art = "Assets/Art/HangarLoadout";
    const string Output = "Assets/Resources/Hangar";
    [Serializable] private class Palette { public MaterialRecord[] materials; }
    [Serializable] private class MaterialRecord { public string name; public float[] color, emission; public float metallic, roughness; }
    static Dictionary<string, Material> palette;
    [MenuItem("MECH ROUGE/Hangar/Build Mac Preview")]
    public static void BuildMac()
    {
        var args=Environment.GetCommandLineArgs();
        int index=Array.IndexOf(args,"-hangarBuildPath");
        string path=index>=0 && index+1<args.Length ? args[index+1] : "Builds/Mac/MECH_TRIAL_Hangar_20260908.app";
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions {
            scenes=new[]{"Assets/Scenes/Demo_Main.unity"}, locationPathName=path,
            target=BuildTarget.StandaloneOSX, options=BuildOptions.None
        });
        Directory.CreateDirectory("AuditEvidence/hangar");
        File.WriteAllText("AuditEvidence/hangar/mac-build.txt",$"{report.summary.result}\nEditor: {Application.unityVersion}\nOutput: {path}\nErrors: {report.summary.totalErrors}\nWarnings: {report.summary.totalWarnings}\nBytes: {report.summary.totalSize}\n");
        if(report.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded) throw new Exception("Mac hangar build failed");
        Debug.Log("HANGAR_MAC_BUILD_PASS");
    }
    static Transform Part(GameObject root, string name) => root.GetComponentsInChildren<Transform>(true).Single(t => t.name == name);
    static void Materials(GameObject root)
    {
        foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            renderer.sharedMaterials = renderer.sharedMaterials.Select(m => m != null && palette.TryGetValue(m.name, out var replacement) ? replacement : m).ToArray();
    }
    static GameObject Model(string file)
    {
        var root = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Art+"/"+file+".fbx"));
        root.name = file; Materials(root); return root;
    }
    [MenuItem("MECH ROUGE/Hangar/Build Loadout Assets")]
    public static void Build()
    {
        Directory.CreateDirectory(Output+"/Materials"); AssetDatabase.Refresh();
        foreach (string path in Directory.GetFiles(Art,"*.fbx"))
        {
            var importer = (ModelImporter)AssetImporter.GetAtPath(path);
            importer.isReadable = true; importer.importAnimation = false; importer.globalScale = 1;
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
            importer.SaveAndReimport();
        }
        palette = new Dictionary<string, Material>();
        foreach (var row in JsonUtility.FromJson<Palette>(File.ReadAllText(Art+"/materials.json")).materials)
        {
            string path = Output+"/Materials/"+row.name.Replace('/','_')+".mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null) { mat = new Material(Shader.Find("Standard")); AssetDatabase.CreateAsset(mat,path); }
            mat.name = row.name;
            mat.color = new Color(row.color[0],row.color[1],row.color[2],1);
            mat.SetFloat("_Metallic",row.metallic); mat.SetFloat("_Glossiness",1-row.roughness);
            var emission = new Color(row.emission[0],row.emission[1],row.emission[2]);
            mat.SetColor("_EmissionColor",emission);
            if (emission.maxColorComponent > .001f) mat.EnableKeyword("_EMISSION");
            palette[row.name] = mat; EditorUtility.SetDirty(mat);
        }
        var armory = AssetDatabase.LoadAssetAtPath<HangarArmory>(Output+"/Armory.asset");
        if (armory == null) { armory = ScriptableObject.CreateInstance<HangarArmory>(); AssetDatabase.CreateAsset(armory,Output+"/Armory.asset"); }
        var entries = new List<HangarArmory.Entry>();
        foreach (var row in new[]{ ("M7",PrimaryWeapon.M7,18f,.18f,38f,0),("RAIKEN",PrimaryWeapon.Greatsword,42f,.68f,0f,0),("M14",PrimaryWeapon.M14,46f,.48f,48f,1) })
        {
            var weapon = Model(row.Item1);
            // Authored export origin is the primary grip; Unity forward is +Z.
            var muzzle = Part(weapon,"Muzzle");
            var primary = Part(weapon,"Grip");
            Vector3 axis = muzzle.position-primary.position;
            Quaternion turn = Quaternion.FromToRotation(new Vector3(axis.x,0,axis.z).normalized,Vector3.forward);
            weapon.transform.rotation = turn * weapon.transform.rotation;
            var wrapper = new GameObject(row.Item1+"_Equippable"); weapon.transform.SetParent(wrapper.transform,true);
            weapon.transform.position -= primary.position;
            var prefab = PrefabUtility.SaveAsPrefabAsset(wrapper,Output+"/"+row.Item1+".prefab");
            entries.Add(new HangarArmory.Entry { weapon=row.Item2,prefab=prefab,damage=row.Item3,interval=row.Item4,speed=row.Item5,pierce=row.Item6 });
            Object.DestroyImmediate(wrapper);
        }
        armory.weapons = entries.ToArray();
        armory.heroPrefab = BuildHero();
        var room = Model("HangarRoom");
        void Light(string name, Vector3 position, Vector3 target, float intensity, Color color)
        {
            var light = new GameObject(name).AddComponent<Light>(); light.transform.SetParent(room.transform,false);
            light.transform.position=position; light.transform.LookAt(target); light.type=LightType.Spot;
            light.spotAngle=100; light.range=18; light.intensity=intensity; light.color=color; light.shadows=LightShadows.Soft;
            light.shadowBias=.025f; light.shadowNormalBias=.1f;
        }
        Light("Armor key",new Vector3(2.7f,5,4),new Vector3(0,2,0),1.7f,new Color(.83f,.91f,1));
        Light("Armor fill",new Vector3(-3,3.8f,3),new Vector3(0,2,0),1.4f,new Color(1,.9f,.78f));
        Light("Back rim",new Vector3(1,4,-3),new Vector3(0,2,0),2f,new Color(.4f,.65f,1));
        Light("Room wash",new Vector3(0,5.5f,2),new Vector3(0,1,-3),1.5f,new Color(.7f,.83f,1));
        armory.roomPrefab=PrefabUtility.SaveAsPrefabAsset(room,Output+"/HangarRoom.prefab");Object.DestroyImmediate(room);
        EditorUtility.SetDirty(armory);AssetDatabase.SaveAssets();
        Directory.CreateDirectory("AuditEvidence/hangar");
        File.WriteAllText("AuditEvidence/hangar/build.txt","V3 hero, unarmed default, M7/RAIKEN/M14, hangar environment and materials built.\n");
        Debug.Log("HANGAR_BUILD_PASS");
    }
    static GameObject BuildHero()
    {
        var model=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player/JishenTrialVisual.prefab"));
        model.name="VALKYR_V3_Loadout";
        var adapter=model.GetComponent<RiggedMechAnimator>();var pose=adapter.rigidPose;
        var oldSegments=pose.segments.Select(s=>(s.source,s.target.name,s.calibration)).ToArray();
        Object.DestroyImmediate(pose.assemblyRoot.gameObject);
        var assembly=Model("VALKYR_V3");assembly.transform.SetParent(model.transform,false);
        var blade=Part(assembly,"AntiShip_Blade_Display_Root");Object.DestroyImmediate(blade.gameObject);
        pose.assemblyRoot=assembly.transform;pose.assemblyRestPosition=assembly.transform.localPosition;
        pose.pelvis=Part(assembly,"Pelvis");pose.cannon=Part(assembly,"Cannon_Pitch_Trunnion");pose.cannonRest=pose.cannon.localRotation;
        pose.beam=null;adapter.bladeTrail=null;adapter.bladeTip=null;
        pose.segments=oldSegments.Select(s=>{
            var target=Part(assembly,s.name);
            return new RigidMechPoseDriver.Segment{source=s.source,target=target,calibration=s.calibration,sourceRest=Quaternion.Inverse(model.transform.rotation)*s.source.rotation,targetRest=Quaternion.Inverse(model.transform.rotation)*target.rotation,localPosition=target.localPosition};
        }).ToArray();
        pose.followers=new[]{"L","R"}.Select(side=>{
            var target=Part(assembly,"Shoulder_Armor_Floating_Pivot."+side);var chest=Part(assembly,"Thorax");
            return new RigidMechPoseDriver.Follower{target=target,from=chest,to=Part(assembly,"UpperArm."+side),rest=Quaternion.Inverse(chest.rotation)*target.rotation,weight=.15f};
        }).ToArray();
        pose.soleContacts=new[]{"L","R"}.SelectMany(side=>Part(assembly,"Foot."+side).GetComponentsInChildren<MeshFilter>()).SelectMany(f=>{
            var verts=f.sharedMesh.vertices;
            float min=verts.Min(v=>f.transform.TransformPoint(v).y);
            return verts.Where(v=>f.transform.TransformPoint(v).y<min+.002f).Select(v=>new RigidMechPoseDriver.Contact{part=f.transform,localPoint=v});
        }).ToArray();
        pose.thrusters=new[]{"L","R"}.Select(side=>{
            var t=new GameObject("Exhaust_"+side).transform;t.SetParent(Part(assembly,"Backpack_Main_Deploy_Hinge."+side),false);return t;
        }).ToArray();
        adapter.muzzle=new GameObject("ShoulderMuzzle").transform;adapter.muzzle.SetParent(pose.cannon,false);
        adapter.muzzle.SetPositionAndRotation(new Vector3(.53f,3.2f,.45f),Quaternion.identity);
        var visual=model.AddComponent<LoadoutVisual>();visual.adapter=adapter;visual.assembly=assembly.transform;
        visual.upperR=Part(assembly,"UpperArm.R");visual.lowerR=Part(assembly,"Forearm.R");visual.handR=Part(assembly,"Hand.R");
        visual.upperL=Part(assembly,"UpperArm.L");visual.lowerL=Part(assembly,"Forearm.L");visual.handL=Part(assembly,"Hand.L");
        visual.gripOffsetR=visual.handR.InverseTransformPoint(Part(assembly,"V3B_Sword_Grip_Socket").position);
        visual.gripOffsetL=visual.handL.InverseTransformPoint(Part(assembly,"V3B_Left_Support_Grip_Socket").position);
        visual.handRestR=visual.handR.rotation;visual.handRestL=visual.handL.rotation;
        visual.neutral=assembly.GetComponentsInChildren<Transform>(true).Select(t=>new LoadoutVisual.Rest{part=t,position=t.localPosition,rotation=t.localRotation}).ToArray();
        var prefab=PrefabUtility.SaveAsPrefabAsset(model,Output+"/VALKYR_V3.prefab");Object.DestroyImmediate(model);return prefab;
    }
}
