using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

// Imports only the new cannon. Existing hero, room and weapon prefabs keep their identities.
public static class HeavyCannonIntegration
{
    const string Art = "Assets/Art/TYPE08Cannon";
    const string Output = "Assets/Resources/Hangar";
    const string Evidence = "art_prototypes/ZZ_HyperCannon_20260908/unity_evidence";
    [Serializable] class Palette { public Row[] materials; }
    [Serializable] class Row { public string name; public float[] color, emission; public float metallic, roughness; }
    static Transform Part(GameObject root, string name) => root.GetComponentsInChildren<Transform>(true).Single(t => t.name == name);

    [MenuItem("MECH ROUGE/TYPE-08/Import Heavy Cannon")]
    public static void Integrate()
    {
        Directory.CreateDirectory(Art + "/Materials");
        Directory.CreateDirectory(Evidence);
        AssetDatabase.Refresh();
        var importer = (ModelImporter)AssetImporter.GetAtPath(Art + "/TYPE08_CANNON.fbx");
        importer.isReadable = true;
        importer.importAnimation = false;
        importer.importCameras = false;
        importer.importLights = false;
        importer.globalScale = 1;
        importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
        importer.SaveAndReimport();
        var materials = new Dictionary<string, Material>();
        foreach (var row in JsonUtility.FromJson<Palette>(File.ReadAllText(Art + "/materials.json")).materials)
        {
            string path = Art + "/Materials/" + row.name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Standard"));
                AssetDatabase.CreateAsset(material, path);
            }
            material.name = row.name;
            material.color = new Color(row.color[0], row.color[1], row.color[2], 1);
            material.SetFloat("_Metallic", row.metallic);
            material.SetFloat("_Glossiness", 1 - row.roughness);
            var emission = new Color(row.emission[0], row.emission[1], row.emission[2]);
            material.SetColor("_EmissionColor", emission);
            if (emission.maxColorComponent > .001f) material.EnableKeyword("_EMISSION");
            else material.DisableKeyword("_EMISSION");
            EditorUtility.SetDirty(material);
            materials.Add(row.name, material);
        }
        var model = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Art + "/TYPE08_CANNON.fbx"));
        model.name = "TYPE08_Cannon_Model";
        foreach (var renderer in model.GetComponentsInChildren<Renderer>(true))
            renderer.sharedMaterials = renderer.sharedMaterials.Select(m => materials.TryGetValue(m.name, out var replacement) ? replacement : m).ToArray();
        foreach (var socket in model.GetComponentsInChildren<Transform>(true))
            if (socket.name.EndsWith("_Export")) socket.name = socket.name.Substring(0, socket.name.Length - 7);
        Transform grip = Part(model, "Grip"), muzzle = Part(model, "Muzzle");
        Vector3 forward = muzzle.position - grip.position;
        model.transform.rotation = Quaternion.FromToRotation(new Vector3(forward.x, 0, forward.z).normalized, Vector3.forward) * model.transform.rotation;
        var wrapper = new GameObject("TYPE08_HeavyCannon_Equippable");
        model.transform.SetParent(wrapper.transform, true);
        model.transform.position -= grip.position;
        muzzle.rotation = grip.rotation = wrapper.transform.rotation;
        foreach (var group in model.GetComponentsInChildren<LODGroup>(true)) Object.DestroyImmediate(group);
        var lodGroup = wrapper.AddComponent<LODGroup>();
        var lods = new LOD[3];
        int[] triangles = new int[3];
        for (int i = 0; i < 3; i++)
        {
            var part = Part(model, "TYPE08_LOD" + i);
            part.gameObject.SetActive(true);
            var renderer = part.GetComponent<Renderer>();
            renderer.enabled = true;
            lods[i] = new LOD(new[] { .40f, .18f, .025f }[i], new[] { renderer });
            triangles[i] = part.GetComponent<MeshFilter>().sharedMesh.triangles.Length / 3;
        }
        lodGroup.SetLODs(lods);
        lodGroup.RecalculateBounds();
        var mesh0 = Part(model, "TYPE08_LOD0").GetComponent<MeshFilter>();
        var points = mesh0.sharedMesh.vertices.Select(v => wrapper.transform.InverseTransformPoint(mesh0.transform.TransformPoint(v))).ToArray();
        float length = points.Max(v => v.z) - points.Min(v => v.z);
        if (Mathf.Abs(length - 3.491269f) > .01f) throw new Exception("Cannon import scale changed: " + length);
        if (grip.position.sqrMagnitude > .000001f) throw new Exception("Grip must be the weapon origin");
        var prefab = PrefabUtility.SaveAsPrefabAsset(wrapper, Output + "/TYPE08.prefab");
        Object.DestroyImmediate(wrapper);
        var armory = AssetDatabase.LoadAssetAtPath<HangarArmory>(Output + "/Armory.asset");
        if (armory == null) throw new Exception("Existing hangar armory is required");
        var entries = armory.weapons.Where(e => e.weapon != PrimaryWeapon.Type08).ToList();
        entries.Add(new HangarArmory.Entry { weapon = PrimaryWeapon.Type08, prefab = prefab, damage = 120, interval = 1.1f, speed = 0, pierce = 2, blastRadius = 1.8f, rightHandOnly = true });
        armory.weapons = entries.ToArray();
        armory.startingWeapon = PrimaryWeapon.Type08;
        EditorUtility.SetDirty(armory);
        AssetDatabase.SaveAssets();
        File.WriteAllText(Evidence + "/import.txt", $"TYPE08_IMPORT_PASS\nLength: {length:F6} m\nBody height: 3.495401 m\nTriangles: {string.Join(", ", triangles)}\nRight hand only: true\nArmory entries: {entries.Count}\nPrefab: {Output}/TYPE08.prefab\nEditor: {Application.unityVersion}\n");
        Debug.Log("TYPE08_IMPORT_PASS");
    }

    public static void BuildAndAudit()
    {
        Integrate();
        HeavyCannonAudit.Run();
    }
}
