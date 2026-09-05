using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;

public static class SliceEnvironmentBuilder
{
    private const string Folder = "Assets/Art/SliceEnvironment";
    private static Transform root;
    private static readonly List<NavMeshBuildSource> sources = new List<NavMeshBuildSource>();
    private static Material hull, panel, deck, deckAlt, inset, teal, gold, white, glass;

    [MenuItem("MECH ROUGE/Update Maintenance Platform")]
    public static void Build()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/Demo_Main.unity");
        var previous = GameObject.Find("SliceEnvironment");
        if (previous != null) Object.DestroyImmediate(previous);
        // Keep the old blockout in the scene, disabled, for comparison and rollback.
        foreach (var go in scene.GetRootGameObjects())
        {
            string n = go.name;
            if (n.StartsWith("ArenaLine") || n.StartsWith("Boundary") || n.StartsWith("HangarPad") || n.StartsWith("BossGate")
                || n == "HangarStartPad" || n == "StageDivider" || n == "ColonyOuterWallArena") go.SetActive(false);
        }
        Directory.CreateDirectory(Folder);
        AssetDatabase.Refresh();
        hull = Mat("Hull", new Color(0.59f, 0.69f, 0.72f), 0.12f, 0.22f);
        panel = Mat("Ceramic", new Color(0.81f, 0.86f, 0.86f), 0.05f, 0.22f);
        deck = Mat("DeckSteel", new Color(0.39f, 0.45f, 0.46f), 0.08f, 0.18f);
        deckAlt = Mat("DeckSteelAlternate", new Color(0.415f, 0.475f, 0.48f), 0.08f, 0.18f);
        inset = Mat("Graphite", new Color(0.16f, 0.23f, 0.25f), 0.2f, 0.26f);
        teal = Mat("SignalTeal", new Color(0.015f, 0.55f, 0.59f), 0.1f, 0.3f);
        gold = Mat("SafetyYellow", new Color(0.94f, 0.69f, 0.07f), 0.08f, 0.24f);
        white = Mat("LaneWhite", new Color(0.97f, 0.98f, 0.96f), 0, 0.15f);
        glass = Mat("Telemetry", new Color(0.025f, 0.35f, 0.39f), 0.25f, 0.5f);
        glass.EnableKeyword("_EMISSION");
        glass.SetColor("_EmissionColor", new Color(0.025f, 0.5f, 0.5f));
        root = new GameObject("SliceEnvironment").transform;
        sources.Clear();
        Box("PlatformFoundation", new Vector3(0, -0.7f, 0), new Vector3(58, 1.3f, 58), inset, true);
        for (int x = -4; x <= 4; x++)
        for (int z = -4; z <= 4; z++)
        {
            var surface = (x + z) % 3 == 0 ? deckAlt : deck;
            Box("DeckTile", new Vector3(x * 6.2f, -0.015f, z * 6.2f), new Vector3(6.12f, 0.03f, 6.12f), surface);
            if (x % 2 == 0 && z % 2 == 0)
                Box("TileServiceHatch", new Vector3(x * 6.2f + 2.1f, 0.012f, z * 6.2f + 1.9f), new Vector3(0.75f, 0.012f, 1.1f), inset);
            for (int side = -1; side <= 1; side += 2)
                Box("PanelLatch", new Vector3(x * 6.2f + side * 2.7f, 0.02f, z * 6.2f - 2.7f), new Vector3(0.16f, 0.025f, 0.3f), hull);
        }
        for (int side = -1; side <= 1; side += 2)
        {
            Box("OuterRail", new Vector3(side * 28, 0.6f, 0), new Vector3(0.55f, 1.2f, 56), hull, true);
            Box("OuterRail", new Vector3(0, 0.6f, side * 28), new Vector3(56, 1.2f, 0.55f), hull, true);
            Box("RailInlay", new Vector3(side * 27.7f, 0.9f, 0), new Vector3(0.09f, 0.18f, 55), teal);
            Box("RailInlay", new Vector3(0, 0.9f, side * 27.7f), new Vector3(55, 0.18f, 0.09f), teal);
            for (int z = -2; z <= 2; z++)
            {
                Box("RailSupport", new Vector3(side * 28.2f, 1, z * 11), new Vector3(1, 2, 1.3f), inset);
                Box("RailLight", new Vector3(side * 27.65f, 1.4f, z * 11), new Vector3(0.1f, 0.6f, 0.55f), white);
            }
            for (int z = -1; z <= 1; z += 2) RepairBank(new Vector3(side * 9.4f, 0, z * 8.2f));
        }
        // Broad centre route, with readable dashed lanes and a clear deployment apron.
        for (int z = -12; z <= 24; z += 4)
        {
            Box("LaneDash", new Vector3(-3.4f, 0.028f, z), new Vector3(0.12f, 0.025f, 1.5f), white);
            Box("LaneDash", new Vector3(3.4f, 0.028f, z), new Vector3(0.12f, 0.025f, 1.5f), white);
        }
        Box("DeploymentApron", new Vector3(0, 0.025f, 0), new Vector3(5, 0.025f, 4.2f), teal);
        for (int x = -2; x <= 2; x++)
            Box("ApronStripe", new Vector3(x * 0.95f, 0.045f, -2.4f), new Vector3(0.58f, 0.02f, 0.35f), gold);
        DeckText("01", new Vector3(0, 0.065f, -4.5f), 0.16f, inset);
        DeckText("MAINTENANCE", new Vector3(0, 0.06f, 5), 0.038f, inset);
        for (int x = -1; x <= 1; x += 2)
        {
            Box("FarGateColumn", new Vector3(x * 7.5f, 2, 25), new Vector3(2.2f, 4, 2.2f), inset, true);
            Box("GateArmor", new Vector3(x * 7.5f, 2.6f, 24), new Vector3(1.5f, 2.2f, 0.3f), panel);
            Box("GateLight", new Vector3(x * 7.5f, 2.5f, 23.8f), new Vector3(0.22f, 1.4f, 0.1f), teal);
        }
        var settings = NavMesh.GetSettingsByID(0);
        settings.agentRadius = 0.65f;
        settings.agentHeight = 2.5f;
        settings.agentClimb = 0.25f;
        settings.overrideVoxelSize = true;
        settings.voxelSize = 0.16f;
        var baked = NavMeshBuilder.BuildNavMeshData(settings, sources, new Bounds(Vector3.zero, new Vector3(64, 12, 64)), Vector3.zero, Quaternion.identity);
        if (baked == null) throw new System.Exception("Maintenance NavMesh bake failed.");
        string navPath = Folder + "/MaintenanceNavMesh.asset";
        var saved = AssetDatabase.LoadAssetAtPath<NavMeshData>(navPath);
        if (saved == null) { AssetDatabase.CreateAsset(baked, navPath); saved = baked; }
        else { EditorUtility.CopySerialized(baked, saved); Object.DestroyImmediate(baked); }
        root.gameObject.AddComponent<ArenaNavigation>().data = saved;
        BuildEncounter();
        var light = Object.FindFirstObjectByType<Light>();
        light.transform.rotation = Quaternion.Euler(48, -35, 0);
        light.color = new Color(1, 0.96f, 0.89f);
        light.intensity = 1.05f;
        light.shadows = LightShadows.Soft;
        light.shadowStrength = 0.7f;
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.53f, 0.62f, 0.65f);
        RenderSettings.ambientEquatorColor = new Color(0.4f, 0.46f, 0.48f);
        RenderSettings.ambientGroundColor = new Color(0.27f, 0.31f, 0.33f);
        RenderSettings.fog = false;
        QualitySettings.shadowDistance = 65;
        QualitySettings.antiAliasing = 4;
        Camera.main.clearFlags = CameraClearFlags.SolidColor;
        Camera.main.backgroundColor = new Color(0.56f, 0.72f, 0.79f);
        AssetDatabase.SaveAssets();
        EditorSceneManager.SaveScene(scene);
        Debug.Log("SLICE_ENVIRONMENT_BUILT sources=" + sources.Count + " objects=" + root.childCount);
    }

    private static void RepairBank(Vector3 position)
    {
        Box("RepairBankCollider", position + new Vector3(0, 0.75f, 0), new Vector3(2.8f, 1.5f, 4.8f), inset, true);
        Box("RepairBankArmor", position + new Vector3(0, 1.55f, 0), new Vector3(2.95f, 0.5f, 4.6f), panel);
        Box("ServiceStripe", position + new Vector3(0, 1.81f, 0), new Vector3(0.55f, 0.03f, 4.55f), teal);
        for (int z = -2; z <= 2; z++)
            Box("CoolingFin", position + new Vector3(0, 1.85f, z * 0.65f), new Vector3(1.65f, 0.16f, 0.12f), inset);
        Box("ServiceScreen", position + new Vector3(1.43f, 1.05f, -0.7f), new Vector3(0.06f, 0.7f, 1.2f), glass);
        for (int i = -1; i <= 1; i++)
            Box("WarningStripe", position + new Vector3(i * 0.75f, 0.045f, -2.95f), new Vector3(0.48f, 0.03f, 0.4f), gold);
    }

    private static void BuildEncounter()
    {
        const string path = "Assets/Art/SliceEnvironment/MaintenanceEncounter.asset";
        var encounter = AssetDatabase.LoadAssetAtPath<EncounterDefinition>(path);
        if (encounter == null) { encounter = ScriptableObject.CreateInstance<EncounterDefinition>(); AssetDatabase.CreateAsset(encounter, path); }
        encounter.title = "SECURE MAINTENANCE DECK";
        encounter.maxAlive = 10;
        var beats = new List<EncounterBeat>();
        for (int i = 0; i < 23; i++)
        {
            EnemyKind kind = i < 5 ? EnemyKind.Melee : i % 7 == 6 ? EnemyKind.Elite : i % 4 == 2 ? EnemyKind.Ranged : i % 4 == 3 ? EnemyKind.Drone : EnemyKind.Melee;
            beats.Add(new EncounterBeat { at = i * 3.2f, kind = kind, count = kind == EnemyKind.Elite ? 1 : i < 6 ? 2 : 3, entry = i % 4 });
        }
        encounter.beats = beats.ToArray();
        EditorUtility.SetDirty(encounter);
        Object.FindFirstObjectByType<StageManager>().maintenanceEncounter = encounter;
    }

    private static void Box(string name, Vector3 position, Vector3 scale, Material material, bool solid = false)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(root, false);
        go.transform.position = position;
        go.transform.localScale = scale;
        go.GetComponent<Renderer>().sharedMaterial = material;
        GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic);
        if (!solid) Object.DestroyImmediate(go.GetComponent<Collider>());
        else sources.Add(new NavMeshBuildSource { shape = NavMeshBuildSourceShape.Box, transform = Matrix4x4.TRS(position, Quaternion.identity, Vector3.one), size = scale, area = 0 });
    }

    private static void DeckText(string text, Vector3 position, float size, Material material)
    {
        var go = new GameObject("DeckLabel_" + text);
        go.transform.SetParent(root, false);
        go.transform.position = position;
        go.transform.rotation = Quaternion.Euler(90, 0, 0);
        var mesh = go.AddComponent<TextMesh>();
        mesh.text = text;
        mesh.fontSize = 100;
        mesh.characterSize = size;
        mesh.anchor = TextAnchor.MiddleCenter;
        mesh.color = material.color;
    }

    private static Material Mat(string name, Color color, float metallic, float smoothness)
    {
        string path = Folder + "/" + name + ".mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null) { mat = new Material(Shader.Find("Standard")); AssetDatabase.CreateAsset(mat, path); }
        mat.color = color;
        mat.SetFloat("_Metallic", metallic);
        mat.SetFloat("_Glossiness", smoothness);
        return mat;
    }
}
