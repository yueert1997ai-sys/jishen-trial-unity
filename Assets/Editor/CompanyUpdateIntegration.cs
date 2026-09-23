using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using Object = UnityEngine.Object;

public static class CompanyUpdateIntegration
{
    public const string Output = "Builds/ValkyrHangarV9";
    const string Evidence = "AuditEvidence/company-update-v9";
    [Serializable] class Palette { public Row[] materials; }
    [Serializable] class Row { public string name; public float[] color, emission; public float metallic, roughness; }
    static Transform Part(GameObject root,string name) => root.GetComponentsInChildren<Transform>(true).Single(t=>t.name==name);
    public static void IntegrateAndBuild()
    {
        AssetDatabase.Refresh(); Directory.CreateDirectory(Evidence);
        BuildHero();
        var scene=EditorSceneManager.OpenScene("Assets/Scenes/Demo_Main.unity");
        var gm=Object.FindFirstObjectByType<GameManager>();
        if(gm.stageManager.enemySpawner.liquidBossPrefab==null)throw new Exception("Expected current V8 scene.");
        BuildLunar(gm.arenaSector);
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
        Build();
    }
    static void BuildHero()
    {
        var model=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(ValkyrCombatIntegration.Visual));
        model.name="VALKYR_V3_V9_Combo";
        var pose=model.GetComponent<RigidMechPoseDriver>();var adapter=model.GetComponent<RiggedMechAnimator>();var blade=model.GetComponent<RaikenBladePresentation>();
        var old=pose.assemblyRoot;
        var assembly=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/HangarLoadout/VALKYR_V3.fbx"));
        assembly.name="Company_VALKYR_V3_5251340"; assembly.transform.SetParent(model.transform,false);
        var palette=AssetDatabase.FindAssets("t:Material",new[]{"Assets/Resources/Hangar/Materials"})
            .Select(g=>AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(g))).GroupBy(m=>m.name).ToDictionary(g=>g.Key,g=>g.First());
        foreach(var r in assembly.GetComponentsInChildren<Renderer>(true))r.sharedMaterials=r.sharedMaterials.Select(m=>m!=null&&palette.TryGetValue(m.name,out var n)?n:m).ToArray();
        var display=assembly.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name=="AntiShip_Blade_Display_Root");
        if(display!=null)Object.DestroyImmediate(display.gameObject);
        // Keep the tested Mk-II blade, its real contact markers, and articulated hand from V8.
        blade.bladeRoot.SetParent(assembly.transform,true);
        var newHand=Part(assembly,"Hand.R");
        foreach(var renderer in newHand.GetComponentsInChildren<Renderer>(true))renderer.gameObject.SetActive(false);
        var articulated=old.GetComponentInChildren<ValkyrHandGrip>(true);
        if(articulated==null)throw new Exception("V8 articulated hand is missing.");
        articulated.transform.SetParent(newHand,false);
        foreach(var s in pose.segments)
        {
            s.target=Part(assembly,s.target.name); s.targetRest=Quaternion.Inverse(model.transform.rotation)*s.target.rotation;
            s.localPosition=s.target.localPosition;
        }
        pose.pelvis=Part(assembly,"Pelvis");pose.cannon=Part(assembly,"Cannon_Pitch_Trunnion");pose.cannonRest=pose.cannon.localRotation;
        adapter.muzzle.SetParent(pose.cannon,true);
        foreach(var t in pose.thrusters)t.SetParent(Part(assembly,t.parent.name),true);
        foreach(var f in pose.followers)
        {
            f.target=Part(assembly,f.target.name);f.from=Part(assembly,f.from.name);f.to=Part(assembly,f.to.name);
            f.rest=Quaternion.Inverse(f.from.rotation)*f.target.rotation;
        }
        pose.soleContacts=new[]{"L","R"}.SelectMany(side=>Part(assembly,"Foot."+side).GetComponentsInChildren<MeshFilter>())
            .SelectMany(m=>m.sharedMesh.vertices.OrderBy(v=>m.transform.TransformPoint(v).y).Take(12)
                .Select(v=>new RigidMechPoseDriver.Contact{part=m.transform,localPoint=v})).ToArray();
        blade.hand=newHand;
        pose.assemblyRoot=assembly.transform;pose.assemblyRestPosition=assembly.transform.localPosition;
        Object.DestroyImmediate(old.gameObject);
        var visual=model.AddComponent<LoadoutVisual>();visual.adapter=adapter;visual.assembly=assembly.transform;
        visual.upperR=Part(assembly,"UpperArm.R");visual.lowerR=Part(assembly,"Forearm.R");visual.handR=newHand;
        visual.upperL=Part(assembly,"UpperArm.L");visual.lowerL=Part(assembly,"Forearm.L");visual.handL=Part(assembly,"Hand.L");
        visual.gripOffsetR=newHand.InverseTransformPoint(Part(assembly,"V3B_Sword_Grip_Socket").position);
        visual.gripOffsetL=visual.handL.InverseTransformPoint(Part(assembly,"V3B_Left_Support_Grip_Socket").position);
        visual.handRestR=newHand.rotation;visual.handRestL=visual.handL.rotation;
        visual.neutral=assembly.GetComponentsInChildren<Transform>(true).Select(t=>new LoadoutVisual.Rest{part=t,position=t.localPosition,rotation=t.localRotation}).ToArray();
        var saved=PrefabUtility.SaveAsPrefabAsset(model,"Assets/Resources/Hangar/VALKYR_V9.prefab");
        var armory=AssetDatabase.LoadAssetAtPath<HangarArmory>("Assets/Resources/Hangar/Armory.asset");
        armory.heroPrefab=saved;EditorUtility.SetDirty(armory);
        File.WriteAllText(Evidence+"/hero.txt","Company V3 body + materials; retained V7 motion, Mk-II sword and articulated hand.\nBlade reach="+blade.Reach+"\nMeshes="+assembly.GetComponentsInChildren<MeshFilter>(true).Length);
        Object.DestroyImmediate(model);AssetDatabase.SaveAssets();
    }
    static void BuildLunar(ArenaSector sector)
    {
        const string art="Assets/Art/LunarBaseV9";
        var importer=(ModelImporter)AssetImporter.GetAtPath(art+"/MARE07_Game.fbx");
        importer.isReadable=true;importer.importAnimation=false;importer.SaveAndReimport();
        Directory.CreateDirectory(art+"/Materials");
        var palette=new Dictionary<string,Material>();
        foreach(var row in JsonUtility.FromJson<Palette>(File.ReadAllText(art+"/materials.json")).materials)
        {
            string path=art+"/Materials/"+row.name.Replace('/','_')+".mat";
            var m=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(m==null){m=new Material(Shader.Find("Standard"));AssetDatabase.CreateAsset(m,path);}
            m.name=row.name;m.color=new Color(row.color[0],row.color[1],row.color[2]);
            m.SetFloat("_Metallic",row.metallic);m.SetFloat("_Glossiness",1-row.roughness);
            var e=new Color(row.emission[0],row.emission[1],row.emission[2]);m.SetColor("_EmissionColor",e);
            if(e.maxColorComponent>.001f)m.EnableKeyword("_EMISSION");palette[row.name]=m;EditorUtility.SetDirty(m);
        }
        if(sector.lunar!=null)Object.DestroyImmediate(sector.lunar);
        var root=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(art+"/MARE07_Game.fbx"));
        root.name="MARE07_Lunar_Combat_Sector";root.transform.SetParent(sector.transform,false);
        foreach(var r in root.GetComponentsInChildren<MeshRenderer>())r.sharedMaterials=r.sharedMaterials.Select(m=>m!=null&&palette.TryGetValue(m.name,out var n)?n:m).ToArray();
        foreach(var m in root.GetComponentsInChildren<MeshFilter>())m.gameObject.AddComponent<MeshCollider>().sharedMesh=m.sharedMesh;
        var sources=root.GetComponentsInChildren<MeshFilter>().Select(m=>new NavMeshBuildSource{shape=NavMeshBuildSourceShape.Mesh,sourceObject=m.sharedMesh,transform=m.transform.localToWorldMatrix,area=0}).ToList();
        // Match P0LunarNavBuild and leave clearance for the player's capsule.
        var settings=NavMesh.GetSettingsByIndex(0);settings.agentRadius=.8f;settings.agentHeight=3.4f;settings.agentClimb=.45f;settings.agentSlope=45;
        settings.overrideVoxelSize=true;settings.voxelSize=.1f;
        var data=NavMeshBuilder.BuildNavMeshData(settings,sources,new Bounds(Vector3.zero,new Vector3(56,24,56)),Vector3.zero,Quaternion.identity);
        if(data==null)throw new Exception("Lunar navigation bake failed.");
        const string navPath=art+"/MARE07_Navigation.asset";
        var existing=AssetDatabase.LoadAssetAtPath<NavMeshData>(navPath);
        if(existing!=null){EditorUtility.CopySerialized(data,existing);Object.DestroyImmediate(data);data=existing;EditorUtility.SetDirty(data);}
        else AssetDatabase.CreateAsset(data,navPath);
        sector.lunar=root;sector.lunarNavigation=data;root.SetActive(false);EditorUtility.SetDirty(sector);
        if(sector.commonDeck==null)
        {
            var common=new GameObject("Legacy_Common_Deck");common.transform.SetParent(sector.transform,false);
            foreach(var child in sector.transform.Cast<Transform>().ToArray())
                if(child.gameObject!=root&&child.gameObject!=common&&child.gameObject!=sector.maintenance&&child.gameObject!=sector.reactor)
                    child.SetParent(common.transform,true);
            sector.commonDeck=common;
        }
        File.WriteAllText(Evidence+"/lunar.txt","MARE07 static batches="+sources.Count+"; mesh collision and baked navigation.\nFirst three encounters use lunar sector. Reactor and bosses preserved.\n");
    }
    public static void Build()
    {
        var args = Environment.GetCommandLineArgs();
        int pathIndex = Array.IndexOf(args, "-companyBuildPath");
        string output = pathIndex >= 0 && pathIndex + 1 < args.Length ? args[pathIndex + 1] : Output;
        Directory.CreateDirectory(output);Directory.CreateDirectory(Evidence);
        PlayerSettings.bundleVersion=(output.Contains("HALBREAKER")?"halbreaker-v10-":"hangar-lunar-v9-")+DateTime.UtcNow.ToString("yyyyMMdd.HHmmss");
        var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/Scenes/Demo_Main.unity"},locationPathName=output+"/MECH_TRIAL_Valkyr.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.StrictMode});
        File.WriteAllText(Evidence+"/build-result.txt",report.summary.result+" errors="+report.summary.totalErrors+" warnings="+report.summary.totalWarnings+"\nVersion="+PlayerSettings.bundleVersion);
        if(report.summary.result!=BuildResult.Succeeded)throw new Exception("V9 build failed.");
        Directory.CreateDirectory(output+"/Licenses");foreach(var f in Directory.GetFiles("docs/licenses"))File.Copy(f,output+"/Licenses/"+Path.GetFileName(f),true);
        File.Copy("docs/ASSET_PROVENANCE.md",output+"/Licenses/ASSET_PROVENANCE.md",true);
        Debug.Log("COMPANY_V9_BUILD_PASS");
    }
}
