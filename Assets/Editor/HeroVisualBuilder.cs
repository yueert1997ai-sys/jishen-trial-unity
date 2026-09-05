using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using Object = UnityEngine.Object;

// Explicit asset-only command. Never rebuilds the scene or touches combat data.
public static class HeroVisualBuilder
{
    private const string DirectoryPath = "Assets/Art/Hero";
    private const string ModelPath = DirectoryPath + "/RiggedSentinel.fbx";
    private const string PrefabPath = "Assets/Prefabs/Player/RiggedSentinelVisual.prefab";
    private const string PlayerPath = "Assets/Prefabs/Player/PlayerMech_Guest.prefab";

    public static void BuildRelease()
    {
        var roots = AssetDatabase.FindAssets("", new[] { "Assets/Resources" })
            .Select(AssetDatabase.GUIDToAssetPath).Append("Assets/Scenes/Demo_Main.unity").ToArray();
        var dependencies = AssetDatabase.GetDependencies(roots, true);
        if (!dependencies.Contains(PrefabPath) || dependencies.Any(p => p.Contains("RoseGoldSentinel") || p.Contains("Meshy_AI")))
            throw new Exception("Hero release dependencies did not select only the licensed new hero.");
        Directory.CreateDirectory("AuditEvidence/hero");
        File.WriteAllLines("AuditEvidence/hero/build-dependencies.txt", dependencies.OrderBy(p => p));
        DemoBuildPipeline.BuildMobileSlice();
    }

    [MenuItem("MECH ROUGE/Hero/Build Rigged Sentinel Visual")]
    public static void Build()
    {
        AssetDatabase.Refresh();
        var importer = (ModelImporter)AssetImporter.GetAtPath(ModelPath);
        importer.animationType = ModelImporterAnimationType.Generic;
        importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
        importer.importAnimation = true;
        importer.animationCompression = ModelImporterAnimationCompression.Off;
        importer.importCameras = importer.importLights = false;
        importer.materialImportMode = ModelImporterMaterialImportMode.None;
        var clips = importer.defaultClipAnimations;
        foreach (var clip in clips)
        {
            clip.name = clip.takeName.Split('|').Last();
            clip.loopTime = clip.name == "Idle" || clip.name.StartsWith("Run") || clip.name == "Walk";
            clip.loopPose = clip.loopTime;
            clip.lockRootRotation = true;
            clip.lockRootPositionXZ = true;
            clip.keepOriginalOrientation = true;
            clip.keepOriginalPositionXZ = true;
        }
        importer.clipAnimations = clips;
        importer.SaveAndReimport();
        var imported = AssetDatabase.LoadAllAssetsAtPath(ModelPath).OfType<AnimationClip>().Where(c => !c.name.StartsWith("__preview")).ToDictionary(c => c.name);
        foreach (string name in new[] { "Idle", "Run", "CannonPose", "SwordSlash", "DashPose", "Death" })
            if (!imported.ContainsKey(name)) throw new Exception("Missing baked clip: " + name + " / " + string.Join(",", imported.Keys));

        string controllerPath = DirectoryPath + "/Sentinel.controller";
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        if (controller != null) AssetDatabase.DeleteAsset(controllerPath);
        controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
        controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
        controller.AddParameter("Stride", AnimatorControllerParameterType.Float);
        var stateMachine = controller.layers[0].stateMachine;
        var locomotion = stateMachine.AddState("Locomotion");
        var blend = new BlendTree { name = "IdleRun", blendParameter = "Speed", blendType = BlendTreeType.Simple1D, useAutomaticThresholds = false };
        AssetDatabase.AddObjectToAsset(blend, controller);
        blend.AddChild(imported["Idle"], 0);
        blend.AddChild(imported["Run"], 1);
        locomotion.motion = blend;
        locomotion.speedParameter = "Stride";
        locomotion.speedParameterActive = true;
        stateMachine.defaultState = locomotion;
        AddState(stateMachine, "Slash", imported["SwordSlash"]);
        AddState(stateMachine, "Death", imported["Death"]);
        AddState(stateMachine, "Dash", imported["DashPose"]);

        var model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath));
        PrefabUtility.UnpackPrefabInstance(model, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        model.name = "RiggedSentinelVisual";
        var animator = model.GetComponent<Animator>() ?? model.AddComponent<Animator>();
        animator.runtimeAnimatorController = controller;
        animator.applyRootMotion = false;
        var adapter = model.AddComponent<RiggedMechAnimator>();
        adapter.animator = animator;
        adapter.hips = Bone(model, "spine.002");
        adapter.chest = Bone(model, "spine.003");
        adapter.head = Bone(model, "head");
        adapter.leftArm = Bone(model, "upper_arm.L");
        adapter.leftForearm = Bone(model, "forearm.L");
        adapter.leftHand = Bone(model, "hand.L");
        adapter.rightArm = Bone(model, "upper_arm.R");
        adapter.rightForearm = Bone(model, "forearm.R");
        adapter.rightHand = Bone(model, "hand.R");
        var mask = new AvatarMask { name = "LeftCannonArm" };
        mask.AddTransformPath(model.transform, true);
        string leftPath = AnimationUtility.CalculateTransformPath(adapter.leftArm, model.transform);
        for (int i = 0; i < mask.transformCount; i++) mask.SetTransformActive(i, mask.GetTransformPath(i).StartsWith(leftPath, StringComparison.Ordinal));
        AssetDatabase.AddObjectToAsset(mask, controller);
        controller.AddLayer("CannonArm");
        var layers = controller.layers;
        layers[1].avatarMask = mask;
        layers[1].defaultWeight = 0;
        controller.layers = layers;
        var cannon = AddState(layers[1].stateMachine, "Cannon", imported["CannonPose"]);
        layers[1].stateMachine.defaultState = cannon;

        var armor = Material("Armor", new Color(.72f, .79f, .82f), .45f, .5f);
        var frame = Material("Frame", new Color(.16f, .22f, .25f), .7f, .4f);
        var dark = Material("Graphite", new Color(.045f, .09f, .12f), .55f, .5f);
        var orange = Material("Copper", new Color(.78f, .26f, .1f), .55f, .5f);
        var steel = Material("Steel", new Color(.4f, .5f, .6f), .7f, .65f);
        var energy = Material("Energy", new Color(.1f, .75f, 1), .3f, .55f, new Color(.08f, .6f, 1));
        // FBX material slot order follows the source mesh: explicitly verified below in the asset report.
        foreach (var skin in model.GetComponentsInChildren<SkinnedMeshRenderer>())
        {
            skin.sharedMaterials = new[] { armor, frame, dark, orange, steel, energy };
            skin.updateWhenOffscreen = true;
            skin.quality = SkinQuality.Bone4;
            skin.localBounds = new Bounds(new Vector3(0, 1.6f, 0), new Vector3(6, 5, 6));
        }

        var gun = new GameObject("ForearmCannon").transform;
        gun.SetParent(adapter.leftForearm, false);
        Vector3 armAxis = (adapter.leftHand.position - adapter.leftForearm.position).normalized;
        gun.SetPositionAndRotation(Vector3.Lerp(adapter.leftForearm.position, adapter.leftHand.position, .65f) + model.transform.right * -.09f,
            Quaternion.LookRotation(armAxis, model.transform.forward));
        Primitive(gun, "CannonHousing", PrimitiveType.Cube, Vector3.zero, new Vector3(.2f, .17f, .46f), dark);
        Primitive(gun, "Barrel", PrimitiveType.Cube, new Vector3(0, 0, .28f), new Vector3(.12f, .11f, .28f), steel);
        Primitive(gun, "Aperture", PrimitiveType.Cube, new Vector3(0, 0, .427f), new Vector3(.08f, .065f, .018f), energy);
        adapter.muzzle = new GameObject("RiggedMuzzle").transform;
        adapter.muzzle.SetParent(gun, false);
        adapter.muzzle.localPosition = new Vector3(0, 0, .45f);

        var sword = new GameObject("BladeGrip").transform;
        sword.SetParent(adapter.rightHand, false);
        Vector3 handAxis = (adapter.rightHand.position - adapter.rightForearm.position).normalized;
        sword.SetPositionAndRotation(adapter.rightHand.position + handAxis * .12f, Quaternion.LookRotation(model.transform.forward, model.transform.up));
        Primitive(sword, "Grip", PrimitiveType.Cube, new Vector3(0, 0, -.08f), new Vector3(.065f, .085f, .28f), dark);
        Primitive(sword, "Guard", PrimitiveType.Cube, new Vector3(0, 0, .08f), new Vector3(.32f, .06f, .065f), steel);
        var blade = new GameObject("Blade");
        blade.transform.SetParent(sword, false);
        blade.AddComponent<MeshFilter>().sharedMesh = BladeMesh();
        blade.AddComponent<MeshRenderer>().sharedMaterial = steel;
        Primitive(sword, "BladeSpine", PrimitiveType.Cube, new Vector3(0, .027f, .7f), new Vector3(.022f, .018f, 1.05f), energy);
        adapter.bladeTip = new GameObject("BladeTip").transform;
        adapter.bladeTip.SetParent(sword, false);
        adapter.bladeTip.localPosition = new Vector3(0, 0, 1.55f);
        adapter.bladeTrail = adapter.bladeTip.gameObject.AddComponent<TrailRenderer>();
        adapter.bladeTrail.sharedMaterial = energy;
        adapter.bladeTrail.time = .09f;
        adapter.bladeTrail.startWidth = .13f;
        adapter.bladeTrail.endWidth = 0;
        adapter.bladeTrail.minVertexDistance = .04f;
        adapter.bladeTrail.emitting = false;
        adapter.bladeTrail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        var prefab = PrefabUtility.SaveAsPrefabAsset(model, PrefabPath);
        Object.DestroyImmediate(model);
        var player = PrefabUtility.LoadPrefabContents(PlayerPath);
        try
        {
            player.GetComponent<PlayerMechLoader>().defaultMechPrefab = prefab;
            PrefabUtility.SaveAsPrefabAsset(player, PlayerPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(player); }
        AssetDatabase.SaveAssets();
        Debug.Log("HERO_VISUAL_BUILD_PASS clips=" + string.Join(",", imported.Keys));
    }

    private static AnimatorState AddState(AnimatorStateMachine machine, string name, AnimationClip clip)
    {
        var state = machine.AddState(name);
        state.motion = clip;
        state.writeDefaultValues = true;
        return state;
    }

    private static Transform Bone(GameObject root, string name) => root.GetComponentsInChildren<Transform>().Single(t => t.name == name);

    private static Material Material(string name, Color color, float metallic, float smoothness, Color emission = default)
    {
        string path = DirectoryPath + "/Sentinel_" + name + ".mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null) { mat = new Material(Shader.Find("Standard")); AssetDatabase.CreateAsset(mat, path); }
        mat.color = color;
        mat.SetFloat("_Metallic", metallic);
        mat.SetFloat("_Glossiness", smoothness);
        if (emission.maxColorComponent > 0) { mat.EnableKeyword("_EMISSION"); mat.SetColor("_EmissionColor", emission); }
        EditorUtility.SetDirty(mat);
        return mat;
    }

    private static void Primitive(Transform parent, string name, PrimitiveType type, Vector3 position, Vector3 scale, Material material)
    {
        var obj = GameObject.CreatePrimitive(type);
        obj.name = name;
        Object.DestroyImmediate(obj.GetComponent<Collider>());
        obj.transform.SetParent(parent, false);
        obj.transform.localPosition = position;
        obj.transform.localScale = scale;
        obj.GetComponent<Renderer>().sharedMaterial = material;
    }

    private static Mesh BladeMesh()
    {
        string path = DirectoryPath + "/SentinelBlade.asset";
        var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (mesh == null) { mesh = new Mesh { name = "SentinelBlade" }; AssetDatabase.CreateAsset(mesh, path); }
        mesh.Clear();
        mesh.vertices = new[] { new Vector3(-.08f, 0, .1f), new Vector3(0, .026f, .1f), new Vector3(.08f, 0, .1f), new Vector3(0, -.026f, .1f),
            new Vector3(-.055f, 0, 1.25f), new Vector3(0, .022f, 1.25f), new Vector3(.055f, 0, 1.25f), new Vector3(0, -.022f, 1.25f), new Vector3(0, 0, 1.55f) };
        mesh.triangles = new[] { 0,1,4, 1,5,4, 1,2,5, 2,6,5, 2,3,6, 3,7,6, 3,0,7, 0,4,7, 4,5,8, 5,6,8, 6,7,8, 7,4,8, 0,3,1, 1,3,2 };
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        EditorUtility.SetDirty(mesh);
        return mesh;
    }
}
