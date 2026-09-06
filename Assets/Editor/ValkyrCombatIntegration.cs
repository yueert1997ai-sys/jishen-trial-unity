using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

public static class ValkyrCombatIntegration
{
    public const string Art = "Assets/Art/ValkyrRaiken_V3";
    public const string Visual = "Assets/Prefabs/Player/ValkyrRaikenVisual.prefab";
    public const string Rival = "Assets/Prefabs/Enemies/RivalVeteran.prefab";
    [Serializable] public class MaterialList { public MaterialInfo[] materials; }
    [Serializable] public class MaterialInfo
    {
        public string name, albedo, normal;
        public float[] baseColor, emission;
        public float metallic, roughness, emissionStrength;
    }
    static Transform Part(GameObject root, string name) => root.GetComponentsInChildren<Transform>(true).Single(t => t.name == name);
    private static bool updateHeroOnly;
    public static void UpdateHero()
    {
        updateHeroOnly = true;
        try { Build(); }
        finally { updateHeroOnly = false; }
    }
    public static void Build()
    {
        ImportMaterials();
        Directory.CreateDirectory("AuditEvidence/valkyr");
        var model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player/JishenTrialVisual.prefab"));
        PrefabUtility.UnpackPrefabInstance(model, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        model.name = "ValkyrRaikenVisual";
        var rig = model.GetComponent<RiggedMechAnimator>();
        var oldPose = model.GetComponent<RigidMechPoseDriver>();
        Object.DestroyImmediate(oldPose.assemblyRoot.gameObject);
        Object.DestroyImmediate(oldPose);
        var assembly = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Art + "/VALKYR_RAIKEN_GAME.fbx"));
        PrefabUtility.UnpackPrefabInstance(assembly, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        assembly.name = "Frozen_Valkyr_Raiken";
        assembly.transform.SetParent(model.transform, false);
        var pose = model.AddComponent<RigidMechPoseDriver>();
        rig.rigidPose = pose;
        pose.assemblyRoot = assembly.transform;
        pose.assemblyRestPosition = assembly.transform.localPosition;
        pose.sourceHips = rig.hips;
        pose.sourceHipsRest = model.transform.InverseTransformPoint(rig.hips.position);
        pose.pelvis = Part(assembly, "Pelvis");
        pose.cannon = Part(assembly, "Cannon_Pitch_Trunnion");
        pose.cannonRest = pose.cannon.localRotation;
        pose.beam = Part(assembly, "LOD0_AntiShipBlade_Beam").gameObject;
        var mappings = new List<RigidMechPoseDriver.Segment>();
        void Map(string sourceName, string targetName, string sourceChild = null, string targetChild = null)
        {
            Transform source = Part(model, sourceName), target = Part(assembly, targetName);
            Quaternion calibration = sourceChild == null ? Quaternion.identity
                : Quaternion.FromToRotation(Part(assembly, targetChild).position - target.position, Part(model, sourceChild).position - source.position);
            mappings.Add(new RigidMechPoseDriver.Segment { source = source, target = target,
                sourceRest = Quaternion.Inverse(model.transform.rotation) * source.rotation,
                targetRest = Quaternion.Inverse(model.transform.rotation) * target.rotation,
                calibration = calibration, localPosition = target.localPosition });
        }
        Map("spine.002", "Pelvis"); Map("spine.003", "Thorax"); Map("head", "Head");
        foreach (string side in new[] { "L", "R" })
        {
            Map("thigh." + side, "Thigh." + side, "shin." + side, "Shin." + side);
            Map("shin." + side, "Shin." + side, "foot." + side, "Foot." + side);
            Map("foot." + side, "Foot." + side);
            Map("upper_arm." + side, "UpperArm." + side, "forearm." + side, "Forearm." + side);
            Map("forearm." + side, "Forearm." + side, "hand." + side, "Hand." + side);
            Map("hand." + side, "Hand." + side);
            mappings.Last().calibration = mappings[mappings.Count - 2].calibration;
        }
        pose.segments = mappings.ToArray();
        pose.followers = new[] { "L", "R" }.Select(side => {
            var shoulder = Part(assembly, "Shoulder_Armor_Floating_Pivot." + side);
            var chest = Part(assembly, "Thorax");
            return new RigidMechPoseDriver.Follower { target = shoulder, from = chest, to = Part(assembly, "UpperArm." + side),
                rest = Quaternion.Inverse(chest.rotation) * shoulder.rotation, weight = .3f };
        }).ToArray();
        var contacts = new List<RigidMechPoseDriver.Contact>();
        foreach (var side in new[] { "L", "R" })
        foreach (var mesh in Part(assembly, "Foot." + side).GetComponentsInChildren<MeshFilter>())
        {
            foreach (var vertex in mesh.sharedMesh.vertices.OrderBy(v => mesh.transform.TransformPoint(v).y).Take(12))
                contacts.Add(new RigidMechPoseDriver.Contact { part = mesh.transform, localPoint = vertex });
        }
        pose.soleContacts = contacts.ToArray();
        pose.thrusters = new[] { "L", "R" }.Select(side => {
            var hinge = Part(assembly, "Backpack_Main_Deploy_Hinge." + side);
            var bounds = BoundsOf(hinge.gameObject);
            var nozzle = new GameObject("Valkyr_Exhaust_" + side).transform;
            nozzle.SetParent(hinge, false);
            nozzle.SetPositionAndRotation(new Vector3(bounds.center.x, bounds.min.y + .05f, bounds.center.z - .05f), Quaternion.LookRotation(new Vector3(0, -1, -.2f)));
            return nozzle;
        }).ToArray();
        var cannonBounds = BoundsOf(pose.cannon.gameObject);
        rig.muzzle = new GameObject("Valkyr_Shoulder_Muzzle").transform;
        rig.muzzle.SetParent(pose.cannon, false);
        rig.muzzle.SetPositionAndRotation(new Vector3(cannonBounds.center.x, cannonBounds.center.y, cannonBounds.max.z + .035f), Quaternion.identity);
        rig.bladeTip = Part(assembly, "RAIKEN_BLADE_TIP");
        rig.bladeTrail = null;
        var blade = model.AddComponent<RaikenBladePresentation>();
        blade.rig = rig;
        blade.tip = rig.bladeTip;
        blade.grip = Part(assembly, "RAIKEN_GRIP_SOCKET");
        blade.bladeRoot = Part(assembly, "AntiShip_Blade_Display_Root");
        blade.hand = Part(assembly, "Hand.R");
        blade.beam = pose.beam;
        model.AddComponent<ValkyrMotionDriver>();
        float bladeLength = Vector3.Distance(blade.grip.position, blade.tip.position);
        if (bladeLength < 3 || bladeLength > 4) throw new Exception("Unexpected blade scale: " + bladeLength);
        var saved = PrefabUtility.SaveAsPrefabAsset(model, Visual);
        File.WriteAllText("AuditEvidence/valkyr/import-result.txt", "Valkyr mesh parts=" + assembly.GetComponentsInChildren<MeshFilter>().Length
            + " triangles=" + assembly.GetComponentsInChildren<MeshFilter>().Sum(m => m.sharedMesh.triangles.Length / 3)
            + "\nGrip-to-tip=" + bladeLength + "\nBlade beam meshes=" + blade.beam.GetComponentsInChildren<MeshRenderer>().Length
            + "\nBones=" + mappings.Count + "\nMaterials=" + string.Join(",", assembly.GetComponentsInChildren<MeshRenderer>().SelectMany(r => r.sharedMaterials).Select(m => m.name).Distinct()));
        Object.DestroyImmediate(model);
        if (updateHeroOnly)
        {
            AssetDatabase.SaveAssets();
            Directory.CreateDirectory("AuditEvidence/v3-model-update");
            File.Copy("AuditEvidence/valkyr/import-result.txt", "AuditEvidence/v3-model-update/import-result.txt", true);
            Debug.Log("VALKYR_V3_HERO_UPDATE_PASS " + Visual);
            return;
        }
        BuildRival();
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/Demo_Main.unity");
        var gm = Object.FindFirstObjectByType<GameManager>();
        var loader = gm.playerController.GetComponent<PlayerMechLoader>();
        loader.defaultMechPrefab = saved;
        PrefabUtility.RecordPrefabInstancePropertyModifications(loader);
        gm.stageManager.enemySpawner.bossPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(Rival);
        EditorUtility.SetDirty(gm.stageManager.enemySpawner);
        var follow = Object.FindFirstObjectByType<CameraFollow>();
        follow.normalSize = 9f;
        EditorUtility.SetDirty(follow);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        RenderComparison();
        Debug.Log("VALKYR_INTEGRATION_PASS " + Visual + " / " + Rival);
    }
    static Bounds BoundsOf(GameObject root)
    {
        var renderers = root.GetComponentsInChildren<MeshRenderer>();
        var bounds = renderers[0].bounds;
        foreach (var renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds);
        return bounds;
    }
    static void BuildRival()
    {
        var root = new GameObject("RivalVeteran");
        var health = root.AddComponent<Damageable>(); health.team = 1; health.maxHealth = 1600;
        root.AddComponent<CombatFeedback>();
        var collider = root.AddComponent<CapsuleCollider>(); collider.center = Vector3.up * 1.65f; collider.height = 3.3f; collider.radius = .7f;
        var boss = root.AddComponent<BossController>(); boss.mechDuel = true; boss.encounterHealth = 1600; boss.armoredDamageScale = .65f; boss.moveSpeed = 4f;
        var model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player/JishenTrialVisual.prefab"));
        PrefabUtility.UnpackPrefabInstance(model, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        model.name = "Yesterday_Mech_Rival_Visual";
        model.transform.SetParent(root.transform, false);
        model.transform.localScale = Vector3.one * 1.08f;
        var adapter = model.GetComponent<RiggedMechAnimator>(); adapter.enabled = false;
        root.AddComponent<MechRivalPresentation>().visual = adapter;
        string signalPath = Art + "/Materials/Rival_Red_Signal.mat";
        var signal = AssetDatabase.LoadAssetAtPath<Material>(signalPath);
        if (signal == null) { signal = new Material(Shader.Find("Standard")); AssetDatabase.CreateAsset(signal, signalPath); }
        signal.color = new Color(.65f, .025f, .01f);
        signal.EnableKeyword("_EMISSION"); signal.SetColor("_EmissionColor", new Color(2, .06f, .015f));
        foreach (var renderer in model.GetComponentsInChildren<MeshRenderer>())
            renderer.sharedMaterials = renderer.sharedMaterials.Select(m => m != null && (m.name.Contains("Sensor") || m.name == "Beam") ? signal : m).ToArray();
        if (adapter.bladeTrail != null) adapter.bladeTrail.sharedMaterial = signal;
        PrefabUtility.SaveAsPrefabAsset(root, Rival);
        Object.DestroyImmediate(root);
    }
    static void RenderComparison()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var player = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Visual));
        var rival = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Rival));
        player.transform.position = new Vector3(-2.2f, 0, 0);
        rival.transform.position = new Vector3(2.2f, 0, 0);
        var clips = AssetDatabase.LoadAllAssetsAtPath("Assets/Art/Hero/RiggedSentinel.fbx").OfType<AnimationClip>();
        var idle = clips.First(c => c.name == "Idle");
        foreach (var model in new[] { player, rival.transform.GetChild(0).gameObject })
        {
            model.GetComponent<Animator>().enabled = false;
            idle.SampleAnimation(model, .1f);
            model.GetComponent<RigidMechPoseDriver>().ApplyPose();
        }
        var blade = player.GetComponent<RaikenBladePresentation>();
        blade.hand.rotation = Quaternion.FromToRotation(blade.tip.position - blade.grip.position, new Vector3(.4f, -.14f, 1).normalized) * blade.hand.rotation;
        var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.GetComponent<Renderer>().sharedMaterial = new Material(Shader.Find("Standard")) { color = new Color(.42f, .45f, .49f) };
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat; RenderSettings.ambientLight = Color.gray;
        var probe = new UnityEngine.Rendering.SphericalHarmonicsL2(); probe.AddAmbientLight(new Color(.5f, .52f, .57f)); RenderSettings.ambientProbe = probe;
        for (int i = 0; i < 2; i++)
        {
            var light = new GameObject("PreviewLight").AddComponent<Light>(); light.type = LightType.Directional;
            light.transform.rotation = Quaternion.Euler(i == 0 ? new Vector3(40, 140, 0) : new Vector3(30, -35, 0)); light.intensity = i == 0 ? 1.2f : .6f;
        }
        var camera = new GameObject("PreviewCamera").AddComponent<Camera>(); camera.orthographic = true; camera.orthographicSize = 3.8f;
        camera.transform.position = new Vector3(6, 5, 12); camera.transform.LookAt(new Vector3(0, 1.7f, .5f));
        camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.13f, .15f, .19f);
        JishenHeroIntegration.Capture(camera, "AuditEvidence/valkyr/hero_and_rival.png", 1600, 1000);
    }
    public static void BuildRelease()
    {
        string output = Path.GetFullPath("Builds/ValkyrV3"); Directory.CreateDirectory(output);
        PlayerSettings.companyName = "Yue"; PlayerSettings.productName = "MECH TRIAL Equipment Preview";
        PlayerSettings.bundleVersion = "valkyr-v3-" + DateTime.UtcNow.ToString("yyyyMMdd.HHmmss");
        PlayerSettings.defaultScreenWidth = 1600; PlayerSettings.defaultScreenHeight = 900; PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes = new[] { "Assets/Scenes/Demo_Main.unity" },
            locationPathName = Path.Combine(output, "MECH_TRIAL_Valkyr.exe"), target = BuildTarget.StandaloneWindows64, options = BuildOptions.StrictMode });
        string summary = report.summary.result + " errors=" + report.summary.totalErrors + " warnings=" + report.summary.totalWarnings;
        File.WriteAllText("AuditEvidence/valkyr/build-result.txt", summary);
        Directory.CreateDirectory("AuditEvidence/v3-model-update");
        File.WriteAllText("AuditEvidence/v3-model-update/build-result.txt", summary);
        if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded) throw new Exception(summary);
        Directory.CreateDirectory(Path.Combine(output, "Licenses"));
        foreach (var file in Directory.GetFiles("docs/licenses")) File.Copy(file, Path.Combine(output, "Licenses", Path.GetFileName(file)), true);
        File.Copy("docs/ASSET_PROVENANCE.md", Path.Combine(output, "Licenses", "ASSET_PROVENANCE.md"), true);
    }
    public static void Inspect()
    {
        Directory.CreateDirectory("AuditEvidence/valkyr");
        ImportMaterials();
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Art + "/VALKYR_RAIKEN_GAME.fbx"));
        File.WriteAllLines("AuditEvidence/valkyr/model-hierarchy.txt", instance.GetComponentsInChildren<Transform>(true).Select(t => t.name + " parent=" + t.parent?.name + " position=" + t.position.ToString("F4") + " rotation=" + t.eulerAngles + " scale=" + t.lossyScale));
        Object.DestroyImmediate(instance);
    }
    static void ImportMaterials()
    {
        AssetDatabase.Refresh();
        Directory.CreateDirectory(Art + "/Materials");
        var definitions = JsonUtility.FromJson<MaterialList>(File.ReadAllText(Art + "/materials.json"));
        var lookup = new Dictionary<string, Material>();
        foreach (var data in definitions.materials)
        {
            string path = Art + "/Materials/" + data.name + ".mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null) { mat = new Material(Shader.Find("Standard")); AssetDatabase.CreateAsset(mat, path); }
            mat.color = new Color(data.baseColor[0], data.baseColor[1], data.baseColor[2], data.baseColor[3]).gamma;
            mat.SetFloat("_Metallic", data.metallic);
            mat.SetFloat("_Glossiness", 1 - data.roughness);
            if (!string.IsNullOrEmpty(data.albedo)) mat.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(data.albedo);
            if (!string.IsNullOrEmpty(data.normal))
            {
                var texture = (TextureImporter)AssetImporter.GetAtPath(data.normal);
                if (texture.textureType != TextureImporterType.NormalMap) { texture.textureType = TextureImporterType.NormalMap; texture.SaveAndReimport(); }
                mat.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(data.normal)); mat.EnableKeyword("_NORMALMAP");
            }
            Color emission = new Color(data.emission[0], data.emission[1], data.emission[2]);
            if (emission.maxColorComponent > 0)
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", emission * Mathf.Min(data.emissionStrength, 3.5f));
            }
            EditorUtility.SetDirty(mat);
            lookup[data.name] = mat;
        }
        foreach (string file in new[] { "VALKYR_RAIKEN_GAME.fbx", "RAIKEN_MkII_GAME.fbx" })
        {
            var importer = (ModelImporter)AssetImporter.GetAtPath(Art + "/" + file);
            importer.isReadable = true;
            importer.importAnimation = false;
            importer.importCameras = false; importer.importLights = false;
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
            foreach (var data in definitions.materials)
                importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), data.name), lookup[data.name]);
            importer.SaveAndReimport();
        }
        AssetDatabase.SaveAssets();
    }
}
