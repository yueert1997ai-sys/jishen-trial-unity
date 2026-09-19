using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

public static class E01EliteIntegration
{
    public const string Art = "Assets/Art/Enemies/TypeE01Elite";
    public const string BossPath = "Assets/Prefabs/Enemies/Boss_HeavyMech.prefab";
    const string ModelPath = Art + "/TypeE01Elite_R02.fbx";
    const string Evidence = "AuditEvidence/e01-elite";
    [Serializable] class MaterialEntry { public string name; public float[] color, emission; public float metallic, smoothness; public bool living; public int wear; }
    [Serializable] class Manifest { public string source_sha256; public int lod0_triangles, lod1_triangles, bones; public MaterialEntry[] material_list; }
    static Transform Part(GameObject model, string name) => model.GetComponentsInChildren<Transform>(true).Single(t => t.name == name);

    public static void InspectImported()
    {
        Directory.CreateDirectory(Evidence);
        var model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
        File.WriteAllLines(Evidence+"/imported-materials.txt",model.GetComponentsInChildren<Renderer>(true).SelectMany(r=>r.sharedMaterials).Select(m=>m == null ? "NULL" : m.name+" | shader="+m.shader.name+" | path="+AssetDatabase.GetAssetPath(m)).Distinct());
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(model);
        string report="";
        foreach (var t in instance.GetComponentsInChildren<Transform>().Take(14)) report+=t.name+" local="+t.localPosition+" world="+t.position+" scale="+t.lossyScale+" rotation="+t.eulerAngles+"\n";
        foreach (var s in instance.GetComponentsInChildren<SkinnedMeshRenderer>()) {
            var mesh=new Mesh(); s.BakeMesh(mesh,false);
            report+=s.name+" mesh="+s.sharedMesh.bounds+" renderer="+s.bounds+" baked="+mesh.bounds+"\n";
            s.BakeMesh(mesh,true); report+="bakedScale="+mesh.bounds+"\n";
            Object.DestroyImmediate(mesh);
        }
        File.WriteAllText(Evidence+"/imported-transforms.txt",report);
        Object.DestroyImmediate(instance);
    }

    [MenuItem("MECH ROUGE/E01 Elite/Integrate R02 Boss")]
    public static void Integrate()
    {
        Directory.CreateDirectory(Evidence);
        Directory.CreateDirectory(Art + "/Materials");
        AssetDatabase.Refresh();
        var manifest = JsonUtility.FromJson<Manifest>(File.ReadAllText(Art + "/export_manifest.json"));
        if (manifest.material_list == null) throw new Exception("Missing frozen material manifest.");
        var shader = Shader.Find("MECH ROUGE/E01 Living Metal");
        if (shader == null || ShaderUtil.ShaderHasError(shader)) throw new Exception("Boss surface shader failed to compile.");
        var importer = (ModelImporter)AssetImporter.GetAtPath(ModelPath);
        importer.animationType = ModelImporterAnimationType.Generic;
        importer.importAnimation = false;
        importer.importCameras = false;
        importer.importLights = false;
        importer.globalScale = 1;
        importer.useFileScale = true;
        importer.isReadable = true;
        importer.optimizeGameObjects = false;
        importer.meshCompression = ModelImporterMeshCompression.Off;
        importer.importNormals = ModelImporterNormals.Import;
        importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
        foreach (var entry in manifest.material_list)
        {
            string filename = Regex.Replace(entry.name, "[^a-zA-Z0-9_-]", "_");
            string path = Art + "/Materials/" + filename + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null) { material = new Material(shader); AssetDatabase.CreateAsset(material,path); }
            material.shader = shader;
            material.SetColor("_Color", new Color(entry.color[0], entry.color[1], entry.color[2],1).gamma);
            material.SetFloat("_Metallic",entry.metallic);
            material.SetFloat("_Glossiness",entry.smoothness);
            material.SetColor("_EmissionColor",new Color(entry.emission[0],entry.emission[1],entry.emission[2],1));
            material.SetFloat("_Living",entry.living ? 1 : 0);
            material.SetFloat("_Wear",entry.wear);
            material.SetFloat("_LifePulse",1);
            material.enableInstancing = true;
            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            EditorUtility.SetDirty(material);
            importer.RemoveRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material),entry.name));
            importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material),entry.name.Replace('|','_')),material);
        }
        importer.SaveAndReimport();
        var boss = PrefabUtility.LoadPrefabContents(BossPath);
        try
        {
            foreach (Transform child in boss.transform.Cast<Transform>().ToArray()) Object.DestroyImmediate(child.gameObject);
            var oldMotion = boss.GetComponent<EnemyMotionAnimator>();
            if (oldMotion != null) Object.DestroyImmediate(oldMotion);
            var oldPose = boss.GetComponent<E01ElitePoseDriver>();
            if (oldPose != null) Object.DestroyImmediate(oldPose);
            var model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath),boss.transform);
            model.name = "E01Elite_R02_Visual";
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.identity;
            model.transform.localScale = Vector3.one;
            var animator = model.GetComponent<Animator>();
            if (animator != null) animator.enabled = false;
            var skins = model.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            if (skins.Length != 2) throw new Exception("Expected two frozen skinned LOD meshes, got " + skins.Length);
            var lod0 = skins.Single(s => s.name == "E01Elite_LOD0");
            var lod1 = skins.Single(s => s.name == "E01Elite_LOD1");
            foreach (var skin in skins)
            {
                skin.gameObject.SetActive(true); skin.enabled = true;
                skin.updateWhenOffscreen = true;
                skin.quality = SkinQuality.Bone2;
                skin.shadowCastingMode = ShadowCastingMode.On;
                var skinBounds = skin.sharedMesh.bounds; skinBounds.Expand(.035f); skin.localBounds = skinBounds;
                if (skin.sharedMaterials.Any(m => m == null || m.shader != shader)) throw new Exception("Unmapped boss material on " + skin.name);
            }
            // FBX can contain an automatically inferred LODGroup; replace it with explicit game thresholds.
            foreach (var existing in model.GetComponentsInChildren<LODGroup>()) Object.DestroyImmediate(existing);
            var lod = model.AddComponent<LODGroup>();
            lod.SetLODs(new[] {new LOD(.18f,new Renderer[]{lod0}),new LOD(.001f,new Renderer[]{lod1})});
            lod.localReferencePoint = new Vector3(0,2.7f,0); lod.size = 6.5f;
            var pose = boss.AddComponent<E01ElitePoseDriver>();
            pose.rigRoot = model.transform; pose.surfaces = skins;
            var core = new GameObject("E01_Core_Emission").transform;
            core.SetParent(Part(model,"Chest"),false);
            core.position = Part(model,"Iris.01").position + model.transform.forward * .07f;
            pose.coreSocket = core;
            var controller = boss.GetComponent<BossController>();
            controller.displayName = E01ElitePoseDriver.DisplayName;
            controller.coreSocket = core;
            controller.defeatDelay = 1.1f;
            var damageable = boss.GetComponent<Damageable>();
            damageable.destroyOnDeath = false;
            var capsule = boss.GetComponent<CapsuleCollider>();
            capsule.center = new Vector3(0,2.18f,0); capsule.height = 4.4f; capsule.radius = 1.1f;
            var bar = boss.GetComponent<WorldHealthBar>();
            bar.height = 5.6f; bar.width = 4.1f;
            PrefabUtility.SaveAsPrefabAsset(boss,BossPath);
            var mesh = new Mesh(); lod0.BakeMesh(mesh,true);
            var vertices = mesh.vertices;
            var bounds = new Bounds(model.transform.InverseTransformPoint(lod0.transform.TransformPoint(vertices[0])),Vector3.zero);
            foreach(var vertex in vertices) bounds.Encapsulate(model.transform.InverseTransformPoint(lod0.transform.TransformPoint(vertex)));
            if (bounds.size.y < 4.5f || bounds.size.y > 6.6f) throw new Exception("Unexpected imported Boss height " + bounds.size.y);
            string report = "source_sha256="+manifest.source_sha256+"\nlod0_triangles="+manifest.lod0_triangles+"\nlod1_triangles="+manifest.lod1_triangles+"\nbones="+manifest.bones+"\nmaterials="+lod0.sharedMaterials.Length+"\nbounds="+bounds+"\n";
            foreach (string name in new[]{"Head","Chest","Foot.L","Foot.R","Tendril.02.04","Iris.01"}) report += name+"="+model.transform.InverseTransformPoint(Part(model,name).position)+"\n";
            File.WriteAllText(Evidence+"/import-check.txt",report);
            Object.DestroyImmediate(mesh);
        }
        finally { PrefabUtility.UnloadPrefabContents(boss); }
        AssetDatabase.SaveAssets();
        var dependencies=AssetDatabase.GetDependencies("Assets/Scenes/Demo_Main.unity",true);
        if (!dependencies.Contains(ModelPath)) throw new Exception("Demo_Main does not reference the new boss FBX.");
        File.WriteAllLines(Evidence+"/scene-dependencies.txt",dependencies.OrderBy(p=>p));
        RenderReview();
        Debug.Log("E01_ELITE_INTEGRATION_PASS");
    }

    public static void RenderReview()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(.40f,.44f,.49f);
        RenderSettings.ambientEquatorColor = new Color(.18f,.19f,.22f);
        RenderSettings.ambientGroundColor = new Color(.07f,.08f,.10f);
        var model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(BossPath));
        var pose = model.GetComponent<E01ElitePoseDriver>();
        foreach (var group in model.GetComponentsInChildren<LODGroup>()) group.ForceLOD(0);
        var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
        var mat = new Material(Shader.Find("Standard")); mat.color = new Color(.14f,.15f,.17f); mat.SetFloat("_Glossiness",.3f);
        floor.GetComponent<Renderer>().sharedMaterial=mat;
        var light=new GameObject("Key").AddComponent<Light>(); light.type=LightType.Directional; light.intensity=1.5f; light.transform.rotation=Quaternion.Euler(38,-32,0);
        var rim=new GameObject("Rim").AddComponent<Light>(); rim.type=LightType.Directional; rim.intensity=1;rim.color=new Color(.52f,.67f,1);rim.transform.rotation=Quaternion.Euler(14,155,0);
        var camera=new GameObject("Review Camera").AddComponent<Camera>();camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.052f,.061f,.078f);camera.fieldOfView=32;
        camera.transform.position=new Vector3(8.8f,6.1f,12);camera.transform.LookAt(new Vector3(0,2.8f,0));
        Capture(camera,Evidence+"/unity_model.png",1300,1300);
        camera.transform.position=new Vector3(1.4f,4.95f,3.2f);camera.transform.LookAt(new Vector3(0,4.48f,.07f));camera.fieldOfView=29;
        Capture(camera,Evidence+"/unity_head.png",1100,900);
        Object.DestroyImmediate(mat);
    }

    public static void Capture(Camera camera,string path,int width,int height)
    {
        var rt=new RenderTexture(width,height,24){antiAliasing=4}; rt.Create();
        var old=camera.targetTexture;var active=RenderTexture.active;
        camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;
        var image=new Texture2D(width,height,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,width,height),0,0);image.Apply();File.WriteAllBytes(path,image.EncodeToPNG());
        camera.targetTexture=old;RenderTexture.active=active;rt.Release();Object.DestroyImmediate(rt);Object.DestroyImmediate(image);
    }

    [MenuItem("MECH ROUGE/E01 Elite/Build PC Boss Demo")]
    public static void BuildRelease()
    {
        if (!AssetDatabase.GetDependencies("Assets/Scenes/Demo_Main.unity",true).Contains(ModelPath)) throw new Exception("New boss missing from build dependencies.");
        DemoBuildPipeline.BuildMobileSlice();
        string build=File.ReadAllText("AuditEvidence/latest-build.txt").Trim();
        File.WriteAllText(Path.Combine(build,"PLAY_BOSS.cmd"),"@echo off\r\ncd /d \"%~dp0\"\r\nstart \"\" \"%~dp0MECH_TRIAL.exe\" -e01BossPreview -logFile \"%~dp0boss-player.log\"\r\n");
        File.WriteAllText("AuditEvidence/e01-elite/build-path.txt",build);
        File.Copy("docs/E01_ELITE_GAME_HANDOFF.md",Path.Combine(build,"BOSS_README.md"),true);
    }
}
