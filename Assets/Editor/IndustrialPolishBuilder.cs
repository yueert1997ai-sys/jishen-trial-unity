using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

public static class IndustrialPolishBuilder
{
    private const string Folder = "Assets/Art/IndustrialModules";
    private static readonly Dictionary<string, Material> materials = new Dictionary<string, Material>();

    [MenuItem("MECH ROUGE/Polish/Apply Industrial Modules")]
    public static void Build()
    {
        AssetDatabase.Refresh();
        string[] names = { "Structure", "Armor", "Edge", "Recess", "Safety", "Signal" };
        Color[] colors = { new Color(.16f,.19f,.20f), new Color(.44f,.49f,.49f), new Color(.66f,.70f,.69f),
            new Color(.045f,.065f,.068f), new Color(.88f,.55f,.08f), new Color(.06f,.65f,.73f) };
        for (int i = 0; i < names.Length; i++)
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(Folder + "/" + names[i] + ".mat");
            if (mat == null) { mat = new Material(Shader.Find("Standard")); AssetDatabase.CreateAsset(mat, Folder + "/" + names[i] + ".mat"); }
            mat.color = colors[i];
            mat.SetFloat("_Metallic", .45f);
            mat.SetFloat("_Glossiness", .32f);
            mat.enableInstancing = true;
            if (names[i] == "Signal") { mat.EnableKeyword("_EMISSION"); mat.SetColor("_EmissionColor", colors[i] * 1.4f); }
            materials[names[i]] = mat;
            EditorUtility.SetDirty(mat);
        }
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/Demo_Main.unity");
        var sector = Object.FindFirstObjectByType<ArenaSector>();
        if (sector == null) throw new Exception("Existing arena sector required; no scene rebuild permitted.");
        string[] replaced = { "RepairBankCollider", "RepairBankArmor", "ServiceStripe", "CoolingFin", "ServiceScreen",
            "ReactorPylon", "PylonCap", "PylonBus", "PylonRadiator" };
        foreach (var t in sector.GetComponentsInChildren<Transform>(true))
            if (replaced.Contains(t.name)) t.gameObject.SetActive(false);
        foreach (var layout in new[] { sector.maintenance.transform, sector.reactor.transform })
        {
            var old = layout.Find("IndustrialDetail");
            if (old != null) Object.DestroyImmediate(old.gameObject);
            var root = new GameObject("IndustrialDetail").transform;
            root.SetParent(layout, false);
            bool reactor = layout == sector.reactor.transform;
            for (int side = -1; side <= 1; side += 2)
            {
                for (int end = -1; end <= 1; end += 2)
                {
                    Place(root, "ServiceBuilding", new Vector3(side * 20, 0, end * 12), side * 90,
                        new Vector3(7.2f, 4.4f, 5.4f));
                    Place(root, "Reservoir", new Vector3(side * 23, 0, end * 23), 0, new Vector3(4.3f, 5, 4.3f));
                    Place(root, "CoolantPump", new Vector3(side * (reactor ? 12 : 9.4f), 0, end * (reactor ? 10 : 8.2f)), 0,
                        new Vector3(3, 1.85f, 4.7f));
                    Place(root, "CargoCrate", new Vector3(side * 7, 0, end * 17), end * 13, new Vector3(3.3f, 2, 2));
                }
                Place(root, "CargoCrate", new Vector3(side * 16, 0, 2), 90, new Vector3(3.3f, 2, 2));
                for (int z = -18; z <= 18; z += 6)
                    Place(root, "DrainGrate", new Vector3(side * 4.4f, .025f, z), 90, Vector3.zero);
            }
        }
        var ground = new Material(Shader.Find("Standard"));
        string groundPath = Folder + "/WornDeck.mat";
        var existing = AssetDatabase.LoadAssetAtPath<Material>(groundPath);
        if (existing != null) { Object.DestroyImmediate(ground); ground = existing; }
        else AssetDatabase.CreateAsset(ground, groundPath);
        ground.color = new Color(.48f, .51f, .50f);
        ground.SetFloat("_Metallic", .3f);
        ground.SetFloat("_Glossiness", .22f);
        ground.mainTexture = MakeDeckTexture();
        ground.enableInstancing = true;
        foreach (var renderer in sector.GetComponentsInChildren<MeshRenderer>(true))
            if (renderer.name == "DeckTile") renderer.sharedMaterial = ground;
        EditorUtility.SetDirty(ground);
        bool wasMaintenance = sector.maintenance.activeSelf, wasReactor = sector.reactor.activeSelf;
        sector.maintenance.SetActive(true);
        sector.reactor.SetActive(false);
        sector.maintenanceNavigation = Bake(sector.transform, "IndustrialMaintenanceNav");
        sector.maintenance.SetActive(false);
        sector.reactor.SetActive(true);
        sector.reactorNavigation = Bake(sector.transform, "IndustrialReactorNav");
        sector.maintenance.SetActive(wasMaintenance);
        sector.reactor.SetActive(wasReactor);
        sector.GetComponent<ArenaNavigation>().data = wasMaintenance ? sector.maintenanceNavigation : sector.reactorNavigation;
        var sun = Object.FindObjectsByType<Light>(FindObjectsSortMode.None).First(l => l.type == LightType.Directional);
        sun.transform.rotation = Quaternion.Euler(48, -32, 0);
        sun.intensity = 1.2f;
        sun.color = new Color(1, .96f, .9f);
        sun.shadowStrength = .82f;
        RenderSettings.ambientSkyColor = new Color(.39f, .47f, .53f);
        RenderSettings.ambientEquatorColor = new Color(.24f, .29f, .31f);
        RenderSettings.ambientGroundColor = new Color(.16f, .18f, .19f);
        AssetDatabase.SaveAssets();
        EditorSceneManager.SaveScene(scene);
        Debug.Log("INDUSTRIAL_POLISH_BUILT: detailed modules, solid cover and both navigation sectors; encounter data untouched.");
    }

    private static void Place(Transform parent, string asset, Vector3 position, float yaw, Vector3 colliderSize)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Folder + "/" + asset + ".fbx");
        if (prefab == null) throw new Exception("Missing generated module: " + asset);
        var placement = new GameObject(asset).transform;
        placement.SetParent(parent, false);
        placement.SetLocalPositionAndRotation(position, Quaternion.Euler(0, yaw, 0));
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        go.name = asset;
        // Preserve the FBX importer's axis correction; yaw and collision belong to the wrapper.
        go.transform.SetParent(placement, false);
        foreach (var renderer in go.GetComponentsInChildren<MeshRenderer>())
        {
            renderer.sharedMaterials = renderer.sharedMaterials.Select(m => materials.TryGetValue(m.name.Split(' ')[0], out var mapped) ? mapped : materials["Armor"]).ToArray();
            GameObjectUtility.SetStaticEditorFlags(renderer.gameObject, StaticEditorFlags.BatchingStatic);
        }
        if (colliderSize != Vector3.zero)
        {
            var collider = placement.gameObject.AddComponent<BoxCollider>();
            collider.center = Vector3.up * colliderSize.y * .5f;
            collider.size = colliderSize;
        }
    }

    private static NavMeshData Bake(Transform root, string name)
    {
        var sources = new List<NavMeshBuildSource>();
        foreach (var box in root.GetComponentsInChildren<BoxCollider>())
            if (box.enabled && !box.isTrigger)
                sources.Add(new NavMeshBuildSource { shape = NavMeshBuildSourceShape.Box, area = 0,
                    transform = box.transform.localToWorldMatrix * Matrix4x4.Translate(box.center), size = box.size });
        var settings = NavMesh.GetSettingsByID(0);
        settings.agentRadius = .65f;
        settings.agentHeight = 2.5f;
        settings.agentClimb = .25f;
        settings.overrideVoxelSize = true;
        settings.voxelSize = .16f;
        var data = NavMeshBuilder.BuildNavMeshData(settings, sources, new Bounds(Vector3.zero, new Vector3(64, 20, 64)), Vector3.zero, Quaternion.identity);
        if (data == null) throw new Exception("Industrial nav bake failed");
        string path = Folder + "/" + name + ".asset";
        var saved = AssetDatabase.LoadAssetAtPath<NavMeshData>(path);
        if (saved == null) { AssetDatabase.CreateAsset(data, path); saved = data; }
        else { EditorUtility.CopySerialized(data, saved); Object.DestroyImmediate(data); }
        return saved;
    }

    private static Texture2D MakeDeckTexture()
    {
        const int size = 512;
        var texture = new Texture2D(size, size, TextureFormat.RGB24, true);
        var pixels = new Color32[size * size];
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            uint hash = (uint)(x * 374761393 + y * 668265263);
            hash = (hash ^ (hash >> 13)) * 1274126177;
            int edge = Math.Min(Math.Min(x, size - 1 - x), Math.Min(y, size - 1 - y));
            float value = .86f + (hash % 100) * .0007f;
            if (edge < 3) value *= .67f;
            else if (edge < 6) value += .065f;
            if ((y % 113 == 17 || y % 191 == 83) && x % 173 > 25 && x % 173 < 80) value += .035f;
            byte v = (byte)(Mathf.Clamp01(value) * 255);
            pixels[y * size + x] = new Color32(v, v, v, 255);
        }
        texture.SetPixels32(pixels);
        texture.Apply();
        string path = Folder + "/DeckSurface.png";
        File.WriteAllBytes(path, texture.EncodeToPNG());
        Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(path);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.mipmapEnabled = true;
        importer.anisoLevel = 4;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }
}
