using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class ManualCombatPalette
{
    [MenuItem("MECH ROUGE/Polish/Apply Restrained Palette")]
    public static void Apply()
    {
        // Absolute assignments make this pass repeatable, without cumulative desaturation.
        Set("Assets/Art/IndustrialModules/Safety.mat", new Color(.55f,.49f,.35f));
        Set("Assets/Art/IndustrialModules/Signal.mat", new Color(.39f,.56f,.55f));
        Set("Assets/Art/SliceEnvironment/SignalTeal.mat", new Color(.29f,.36f,.35f));
        Set("Assets/Art/SliceEnvironment/SafetyYellow.mat", new Color(.58f,.52f,.38f));
        Set("Assets/Art/SliceEnvironment/ReactorCoral.mat", new Color(.43f,.32f,.29f));
        Set("Assets/Art/SliceEnvironment/Telemetry.mat", new Color(.43f,.57f,.56f));
        Set("Assets/Materials/MR_EnemyRed.mat", new Color(.47f,.29f,.27f));
        Set("Assets/Materials/MR_GoldPlaceholder.mat", new Color(.53f,.47f,.33f));
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/Demo_Main.unity");
        var sector = Object.FindFirstObjectByType<ArenaSector>();
        var deck = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/IndustrialModules/WornDeck.mat");
        if (sector == null || deck == null) throw new System.Exception("Existing industrial arena required");
        foreach (var renderer in sector.GetComponentsInChildren<MeshRenderer>(true))
            if (renderer.name == "DeploymentApron" || renderer.name == "CoreHatch" || renderer.name == "CoreVent")
                renderer.sharedMaterial = deck;
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("MANUAL_PALETTE_APPLIED");
    }
    private static void Set(string path, Color color)
    {
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null) throw new FileNotFoundException(path);
        material.color = color;
        if (material.IsKeywordEnabled("_EMISSION")) material.SetColor("_EmissionColor", color * .65f);
        EditorUtility.SetDirty(material);
    }
}
