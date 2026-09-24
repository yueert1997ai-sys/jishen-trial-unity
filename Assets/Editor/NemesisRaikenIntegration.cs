using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Object = UnityEngine.Object;

// Reuse the original VALKYR anti-ship blade, retaining its mesh and authored details.
public static class NemesisRaikenIntegration
{
    const string Art = "Assets/Art/NemesisRaikenR1";
    const string SwordPath = "Assets/Resources/Hangar/NEMESIS_RAIKEN.prefab";
    const string HeroPath = "Assets/Resources/Hangar/EXTRACTED_NEMESIS.prefab";
    static Transform Find(GameObject root, string name) => root.GetComponentsInChildren<Transform>(true).Single(t => t.name == name);
    static Color Hex(string value) { ColorUtility.TryParseHtmlString(value, out var color); return color; }

    static Material Gray(Material source, Dictionary<Material, Material> copies)
    {
        if (copies.TryGetValue(source, out var saved)) return saved;
        var name = source.name; string path = Art + "/" + name + "_Nemesis.mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null) { m = new Material(source); AssetDatabase.CreateAsset(m, path); }
        m.name = "NEMESIS graphite / " + name;
        var dark = Hex("#383C42");
        if (name.Contains("Navy") || name.Contains("Recess") || name.Contains("Grip")) dark = Hex("#202329");
        else if (name.Contains("Titanium") || name.Contains("Fastener")) dark = Hex("#646A72");
        else if (name.Contains("Inner") || name.Contains("Gunmetal")) dark = Hex("#454B53");
        else if (name.Contains("Beam") || name.Contains("Cyan") || name.Contains("Emitter")) dark = Hex("#717983");
        else if (name.Contains("Identification")) dark = Hex("#8C9197");
        m.color = dark; m.SetColor("_EmissionColor", Color.black); m.DisableKeyword("_EMISSION");
        m.SetFloat("_Metallic", .55f); m.SetFloat("_Glossiness", .38f);
        EditorUtility.SetDirty(m); copies[source] = m; return m;
    }

    public static void Build() { Integrate(); CombatLoopV2Build.Build(); }

    static void GhostProxy(GameObject weapon)
    {
        var points=new List<Vector3>(); var triangles=new List<int>();
        var cells=new Dictionary<Vector3Int,int>(); var faces=new HashSet<(int,int,int)>();
        foreach(var filter in weapon.GetComponentsInChildren<MeshFilter>(true))
        {
            var matrix=weapon.transform.worldToLocalMatrix*filter.transform.localToWorldMatrix;
            var vertices=filter.sharedMesh.vertices; var map=new int[vertices.Length];
            for(int i=0;i<vertices.Length;i++)
            {
                var point=matrix.MultiplyPoint3x4(vertices[i]); var cell=Vector3Int.RoundToInt(point/.085f);
                if(!cells.TryGetValue(cell,out int id)){id=points.Count;cells[cell]=id;points.Add(point);} map[i]=id;
            }
            var indices=filter.sharedMesh.triangles;
            for(int i=0;i<indices.Length;i+=3)
            {
                int a=map[indices[i]],b=map[indices[i+1]],c=map[indices[i+2]];
                if(a!=b&&a!=c&&b!=c&&faces.Add((a,b,c))){triangles.Add(a);triangles.Add(b);triangles.Add(c);}
            }
        }
        string path=Art+"/Raiken_Ghost.asset"; var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if(mesh==null){mesh=new Mesh();AssetDatabase.CreateAsset(mesh,path);}
        mesh.Clear();mesh.name="NEMESIS_RAIKEN_Ghost";mesh.SetVertices(points);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();EditorUtility.SetDirty(mesh);
        weapon.AddComponent<NemesisGhostMesh>().proxy=mesh;
    }
    public static void Integrate()
    {
        Directory.CreateDirectory(Art); AssetDatabase.Refresh();
        var source = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Hangar/VALKYR_V9.prefab"));
        var original = source.GetComponent<RaikenBladePresentation>();
        var copy = Object.Instantiate(original.bladeRoot.gameObject);
        var grip = Find(copy, "RAIKEN_GRIP_SOCKET"); var tip = Find(copy, "RAIKEN_BLADE_TIP");
        var beam = Find(copy, original.beam.name);
        var axis = (tip.position - grip.position).normalized;
        var edge = Find(copy, "V3B_Blade_Edge_Frame").position - grip.position;
        var weapon = new GameObject("NEMESIS_RAIKEN_DARK_GRAY");
        weapon.transform.SetPositionAndRotation(grip.position, ValkyrMotionProfile.Frame(axis, edge));
        copy.transform.SetParent(weapon.transform, true);
        weapon.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        float originalReach = Vector3.Distance(grip.position, tip.position);
        var body = AssetDatabase.LoadAssetAtPath<GameObject>(HeroPath).GetComponentsInChildren<SkinnedMeshRenderer>(true).Single(s => s.name == "J01_Body_LOD0");
        float bodyHeight = body.sharedMesh.bounds.size.y * body.transform.lossyScale.y;
        float minZ=float.PositiveInfinity,maxZ=float.NegativeInfinity;
        foreach(var filter in weapon.GetComponentsInChildren<MeshFilter>(true))
            foreach(var vertex in filter.sharedMesh.vertices)
            {
                var point=weapon.transform.InverseTransformPoint(filter.transform.TransformPoint(vertex));
                minZ=Mathf.Min(minZ,point.z);maxZ=Mathf.Max(maxZ,point.z);
            }
        float scale=bodyHeight/(maxZ-minZ);
        weapon.transform.localScale = Vector3.one * scale;
        var copies = new Dictionary<Material, Material>();
        foreach (var renderer in weapon.GetComponentsInChildren<MeshRenderer>(true))
            renderer.sharedMaterials = renderer.sharedMaterials.Select(m => Gray(m, copies)).ToArray();
        var fit = weapon.AddComponent<NemesisRaikenBlade>();
        fit.originalReach = originalReach; fit.fittedReach = originalReach*scale;
        fit.fullLength=bodyHeight;fit.bodyHeight=bodyHeight;
        fit.sourceBlade = "VALKYR_V9 / AntiShip_Blade_Display_Root";
        fit.grip = grip; fit.tip = tip;
        GhostProxy(weapon);
        var sword = PrefabUtility.SaveAsPrefabAsset(weapon, SwordPath);
        string beamName = beam.name;
        Object.DestroyImmediate(weapon); Object.DestroyImmediate(source);

        var model = PrefabUtility.LoadPrefabContents(HeroPath);
        try
        {
            var blade = model.GetComponent<RaikenBladePresentation>();
            var pose = model.GetComponent<RigidMechPoseDriver>();
            var old = blade.bladeRoot;
            var held = Object.Instantiate(sword, old.parent);
            held.name = "NEMESIS_RAIKEN_DARK_GRAY";
            blade.bladeRoot = held.transform;
            blade.grip = Find(held, "RAIKEN_GRIP_SOCKET"); blade.tip = Find(held, "RAIKEN_BLADE_TIP");
            blade.beam = Find(held, beamName).gameObject; pose.beam = blade.beam;
            model.GetComponent<RiggedMechAnimator>().bladeTip = blade.tip;
            held.transform.position += Find(model, "V3B_Sword_Grip_Socket").position - blade.grip.position;
            Object.DestroyImmediate(old.gameObject);
            model.GetComponent<LoadoutVisual>().neutral = pose.assemblyRoot.GetComponentsInChildren<Transform>(true)
                .Select(t => new LoadoutVisual.Rest { part = t, position = t.localPosition, rotation = t.localRotation }).ToArray();
            PrefabUtility.SaveAsPrefabAsset(model, HeroPath);
            var record = new {
                source = "VALKYR_V9 / AntiShip_Blade_Display_Root",
                originalReach, fittedReach = blade.Reach, scale, fullLength=bodyHeight,
                meshes = held.GetComponentsInChildren<MeshFilter>(true).Select(m => new { name = m.sharedMesh.name, vertices = m.sharedMesh.vertexCount, triangles = m.sharedMesh.triangles.Length / 3, asset = AssetDatabase.GetAssetPath(m.sharedMesh) }).ToArray(),
                materialCount = copies.Count
            };
            string output = Environment.GetEnvironmentVariable("MECH_LOOP_V2_EVIDENCE");
            if (!string.IsNullOrEmpty(output)) {
                Directory.CreateDirectory(output);
                File.WriteAllText(Path.Combine(output, "raiken-asset.txt"),
                    $"source={record.source}\noriginalReach={originalReach:F6}\nfittedReach={blade.Reach:F6}\nscale={record.scale:F6}\nfullLength={bodyHeight:F6}\nbodyHeight={bodyHeight:F6}\nmaterials={record.materialCount}\n" +
                    string.Join("\n", record.meshes.Select(m => $"{m.name} vertices={m.vertices} triangles={m.triangles} source={m.asset}")));
            }
        }
        finally { PrefabUtility.UnloadPrefabContents(model); }
        AssetDatabase.SaveAssets(); Debug.Log("NEMESIS_RAIKEN_INTEGRATED");
    }
}
