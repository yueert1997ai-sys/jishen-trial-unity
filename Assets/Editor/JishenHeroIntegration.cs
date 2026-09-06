using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

// Uses a frozen FBX snapshot. No Blender file, source geometry, scene builder or combat rule is touched.
public static class JishenHeroIntegration
{
    public const string Art = "Assets/Art/JishenTrial_Assembly_V3";
    public const string Prefab = "Assets/Prefabs/Player/JishenTrialVisual.prefab";
    const string Player = "Assets/Prefabs/Player/PlayerMech_Guest.prefab";
    static Transform Part(GameObject root, string name) => root.GetComponentsInChildren<Transform>(true).Single(t => t.name == name);

    [MenuItem("MECH ROUGE/Jishen/Build Frozen Hero Integration")]
    public static void Build()
    {
        Directory.CreateDirectory("AuditEvidence/jishen");
        AssetDatabase.Refresh();
        var model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player/RiggedSentinelVisual.prefab"));
        PrefabUtility.UnpackPrefabInstance(model, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        model.name = "JishenTrialVisual";
        var adapter = model.GetComponent<RiggedMechAnimator>();
        foreach (var oldSkin in model.GetComponentsInChildren<SkinnedMeshRenderer>()) Object.DestroyImmediate(oldSkin.gameObject);
        Object.DestroyImmediate(Part(model, "ForearmCannon").gameObject);
        Object.DestroyImmediate(Part(model, "BladeGrip").gameObject);
        var assembly = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Art + "/JishenTrial_Assembly_V3_STATIC.prefab"));
        PrefabUtility.UnpackPrefabInstance(assembly, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        assembly.name = "Frozen_Assembly_V3";
        assembly.transform.SetParent(model.transform, false);
        var pose = model.AddComponent<RigidMechPoseDriver>();
        adapter.rigidPose = pose;
        pose.assemblyRoot = assembly.transform;
        pose.assemblyRestPosition = assembly.transform.localPosition;
        pose.sourceHips = adapter.hips;
        pose.sourceHipsRest = model.transform.InverseTransformPoint(adapter.hips.position);
        pose.pelvis = Part(assembly, "Pelvis");
        pose.cannon = Part(assembly, "Cannon_Pitch_Trunnion");
        pose.cannonRest = pose.cannon.localRotation;
        pose.beam = Part(assembly, "LOD0_AntiShipBlade_Beam").gameObject;
        pose.thrusters = new Transform[2];
        for (int i = 0; i < 2; i++)
        {
            string side = i == 0 ? "L" : "R";
            var nozzle = new GameObject("Jishen_Exhaust_" + side).transform;
            nozzle.SetParent(Part(assembly, "Backpack_Main_Deploy_Hinge." + side), false);
            nozzle.SetPositionAndRotation(new Vector3(i == 0 ? -.2405f : .2405f, 2.0605f, -.48425f), Quaternion.LookRotation(new Vector3(0, -1, -.12f)));
            pose.thrusters[i] = nozzle;
        }
        var mappings = new List<RigidMechPoseDriver.Segment>();
        void Map(string sourceName, string targetName, string sourceChild = null, string targetChild = null)
        {
            var source = Part(model, sourceName);
            var target = Part(assembly, targetName);
            var calibration = Quaternion.identity;
            if (sourceChild != null)
                calibration = Quaternion.FromToRotation(Part(assembly, targetChild).position - target.position, Part(model, sourceChild).position - source.position);
            mappings.Add(new RigidMechPoseDriver.Segment {
                source = source, target = target, sourceRest = Quaternion.Inverse(model.transform.rotation) * source.rotation,
                targetRest = Quaternion.Inverse(model.transform.rotation) * target.rotation, calibration = calibration, localPosition = target.localPosition });
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
        var followers = new List<RigidMechPoseDriver.Follower>();
        foreach (string side in new[] { "L", "R" })
        {
            var shoulder = Part(assembly, "Shoulder_Armor_Floating_Pivot." + side);
            var chest = Part(assembly, "Thorax");
            followers.Add(new RigidMechPoseDriver.Follower { target = shoulder, from = chest, to = Part(assembly, "UpperArm." + side),
                rest = Quaternion.Inverse(chest.rotation) * shoulder.rotation, weight = .3f });
        }
        pose.followers = followers.ToArray();
        var contacts = new List<RigidMechPoseDriver.Contact>();
        foreach (string side in new[] { "L", "R" })
        foreach (var filter in Part(assembly, "Foot." + side).GetComponentsInChildren<MeshFilter>())
        {
            var vertices = filter.sharedMesh.vertices;
            // Surface vertices from the convex extremes: contact samples are taken from actual geometry.
            var selected = new HashSet<int>();
            for (int x = -1; x <= 1; x++) for (int y = -1; y <= 0; y++) for (int z = -1; z <= 1; z++)
            {
                var dir = new Vector3(x, y, z); if (dir == Vector3.zero) continue;
                int index = 0; float score = float.NegativeInfinity;
                for (int i = 0; i < vertices.Length; i++)
                {
                    float next = Vector3.Dot(filter.transform.TransformPoint(vertices[i]), dir);
                    if (next > score) { score = next; index = i; }
                }
                selected.Add(index);
            }
            contacts.AddRange(selected.Select(i => new RigidMechPoseDriver.Contact { part = filter.transform, localPoint = vertices[i] }));
        }
        pose.soleContacts = contacts.ToArray();

        // The short cannon's aperture, measured from the frozen mesh construction (meters).
        adapter.muzzle = new GameObject("Jishen_RightShoulder_Muzzle").transform;
        adapter.muzzle.SetParent(pose.cannon, false);
        adapter.muzzle.SetPositionAndRotation(new Vector3(.52975f, 3.1444f, .509f), Quaternion.LookRotation(new Vector3(0, .025f, 1)));
        var hand = Part(assembly, "Hand.R");
        var sword = Part(assembly, "AntiShip_Blade_Display_Root");
        // FBX sword axis is world -Y, pommel at root, center of grip 0.18m along the axis.
        Vector3 oldGrip = sword.position + Vector3.down * .18f;
        Vector3 desiredAxis = new Vector3(.12f, -.22f, 1).normalized;
        Quaternion turn = Quaternion.FromToRotation(Vector3.down, desiredAxis);
        Vector3 gripPosition = hand.position + new Vector3(.018f, -.085f, .025f);
        sword.SetPositionAndRotation(gripPosition + turn * (sword.position - oldGrip), turn * sword.rotation);
        sword.SetParent(hand, true);
        var grip = new GameObject("Jishen_Blade_Grip").transform;
        grip.SetParent(sword, false); grip.position = gripPosition;
        adapter.bladeTip = new GameObject("Jishen_Blade_Tip").transform;
        adapter.bladeTip.SetParent(sword, false); adapter.bladeTip.position = sword.position + desiredAxis * 2.275f;
        adapter.bladeTrail = adapter.bladeTip.gameObject.AddComponent<TrailRenderer>();
        adapter.bladeTrail.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(Art + "/Materials/Beam.mat");
        adapter.bladeTrail.time = .09f; adapter.bladeTrail.startWidth = .09f; adapter.bladeTrail.endWidth = 0;
        adapter.bladeTrail.minVertexDistance = .04f; adapter.bladeTrail.emitting = false;
        adapter.bladeTrail.shadowCastingMode = ShadowCastingMode.Off;
        var filters = assembly.GetComponentsInChildren<MeshFilter>();
        if (filters.Length != 110 || filters.Sum(f => f.sharedMesh.triangles.Length / 3) != 129824)
            throw new Exception("Frozen model topology changed unexpectedly.");
        var prefab = PrefabUtility.SaveAsPrefabAsset(model, Prefab);
        var player = PrefabUtility.LoadPrefabContents(Player);
        try { player.GetComponent<PlayerMechLoader>().defaultMechPrefab = prefab; PrefabUtility.SaveAsPrefabAsset(player, Player); }
        finally { PrefabUtility.UnloadPrefabContents(player); }
        AssetDatabase.SaveAssets();
        File.WriteAllLines("AuditEvidence/jishen/bindings.txt", mappings.Select(s => s.source.name + " -> " + s.target.name + " pivot=" + s.target.position + " calibration=" + s.calibration.eulerAngles)
            .Concat(new[] { "meshes=" + filters.Length, "triangles=129824", "soleContacts=" + contacts.Count, "source=Frozen Assembly V3; vertices unchanged" }));
        Object.DestroyImmediate(model);
        RenderPoses();
        Debug.Log("JISHEN_INTEGRATION_BUILD_PASS " + Prefab);
    }

    public static void RenderPoses()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Prefab));
        var pose = model.GetComponent<RigidMechPoseDriver>();
        var animator = model.GetComponent<Animator>(); animator.enabled = false;
        var clips = AssetDatabase.LoadAllAssetsAtPath("Assets/Art/Hero/RiggedSentinel.fbx").OfType<AnimationClip>()
            .Where(c => !c.name.StartsWith("__preview")).ToDictionary(c => c.name);
        var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        var mat = new Material(Shader.Find("Standard")); mat.color = new Color(.62f, .64f, .66f); mat.SetFloat("_Glossiness", .15f);
        ground.GetComponent<Renderer>().sharedMaterial = mat;
        RenderSettings.ambientMode = AmbientMode.Flat; RenderSettings.ambientLight = new Color(.6f, .63f, .66f);
        var ambient = new SphericalHarmonicsL2(); ambient.AddAmbientLight(new Color(.6f, .63f, .66f)); RenderSettings.ambientProbe = ambient;
        for (int i = 0; i < 2; i++)
        {
            var light = new GameObject("Review Light").AddComponent<Light>(); light.type = LightType.Directional;
            light.transform.rotation = Quaternion.Euler(i == 0 ? new Vector3(40, 145, 0) : new Vector3(30, -30, 0));
            light.intensity = i == 0 ? 1.1f : .65f; light.shadows = i == 0 ? LightShadows.Soft : LightShadows.None;
        }
        var camera = new GameObject("Review Camera").AddComponent<Camera>(); camera.orthographic = true;
        camera.orthographicSize = 2.25f; camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.68f, .7f, .72f);
        var shots = new[] { ("idle_front", "Idle", .1f, new Vector3(0, 2.0f, 10)), ("idle_side", "Idle", .1f, new Vector3(10, 2.1f, 0)),
            ("idle_3q", "Idle", .1f, new Vector3(6, 4.3f, 10)), ("run", "Run", .2f, new Vector3(6, 4.3f, 10)),
            ("dash", "DashPose", .02f, new Vector3(6, 4.3f, 10)), ("slash", "SwordSlash", .27f, new Vector3(6, 4.3f, 10)) };
        foreach (var shot in shots)
        {
            clips[shot.Item2].SampleAnimation(model, shot.Item3); pose.ApplyPose();
            camera.transform.position = shot.Item4; camera.transform.LookAt(new Vector3(0, 1.65f, .25f));
            Capture(camera, "AuditEvidence/jishen/pose_" + shot.Item1 + ".png", 1400, 1400);
        }
        Object.DestroyImmediate(model); Object.DestroyImmediate(mat);
    }

    public static void Capture(Camera camera, string file, int width, int height)
    {
        var rt = new RenderTexture(width, height, 24) { antiAliasing = 4 }; rt.Create();
        var oldTarget = camera.targetTexture; var old = RenderTexture.active;
        camera.targetTexture = rt; camera.Render(); RenderTexture.active = rt;
        var image = new Texture2D(width, height, TextureFormat.RGB24, false);
        image.ReadPixels(new Rect(0, 0, width, height), 0, 0); image.Apply(); File.WriteAllBytes(file, image.EncodeToPNG());
        camera.targetTexture = oldTarget; RenderTexture.active = old; rt.Release(); Object.DestroyImmediate(rt); Object.DestroyImmediate(image);
    }

    public static void BuildRelease()
    {
        var roots = AssetDatabase.FindAssets("", new[] { "Assets/Resources" }).Select(AssetDatabase.GUIDToAssetPath).Append("Assets/Scenes/Demo_Main.unity").ToArray();
        var dependencies = AssetDatabase.GetDependencies(roots, true);
        if (!dependencies.Contains(Prefab) || !dependencies.Contains(Art + "/JishenTrial_Assembly_V3.fbx")) throw new Exception("Saved game did not select the Jishen hero.");
        File.WriteAllLines("AuditEvidence/jishen/build-dependencies.txt", dependencies.OrderBy(p => p));
        DemoBuildPipeline.BuildMobileSlice();
    }
}
