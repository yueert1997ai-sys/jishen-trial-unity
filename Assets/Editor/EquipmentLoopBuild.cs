using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class EquipmentLoopBuild
{
    public static void Build()
    {
        string output = Path.GetFullPath("Builds/EquipmentLoop");
        Directory.CreateDirectory(output);
        PlayerSettings.companyName = "Yue";
        PlayerSettings.productName = "MECH TRIAL Equipment Preview";
        PlayerSettings.bundleVersion = "equipment-v1-" + System.DateTime.UtcNow.ToString("yyyyMMdd.HHmmss");
        PlayerSettings.defaultScreenWidth = 1600;
        PlayerSettings.defaultScreenHeight = 900;
        PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
        PlayerSettings.resizableWindow = true;
        PlayerSettings.runInBackground = true;
        PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
        var result = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
            scenes = new[] { "Assets/Scenes/Demo_Main.unity" },
            locationPathName = Path.Combine(output, "MECH_TRIAL_Equipment.exe"),
            target = BuildTarget.StandaloneWindows64, options = BuildOptions.StrictMode
        });
        string summary = result.summary.result + " errors=" + result.summary.totalErrors + " warnings=" + result.summary.totalWarnings;
        File.WriteAllText("AuditEvidence/equipment-loop/build-result.txt", summary + "\n" + output);
        if (result.summary.result != BuildResult.Succeeded) throw new System.Exception(summary);
        string licenses = Path.Combine(output, "Licenses");
        Directory.CreateDirectory(licenses);
        foreach (var file in Directory.GetFiles("docs/licenses")) File.Copy(file, Path.Combine(licenses, Path.GetFileName(file)), true);
        File.Copy("docs/ASSET_PROVENANCE.md", Path.Combine(licenses, "ASSET_PROVENANCE.md"), true);
        Debug.Log("EQUIPMENT_BUILD " + summary);
    }
}

[InitializeOnLoad]
public static class EquipmentLoopAudit
{
    static EquipmentLoopAudit()
    {
        EditorApplication.playModeStateChanged += state => {
            if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool("EquipmentLoopCheck", false)) return;
            SessionState.SetBool("EquipmentLoopCheck", false);
            EquipmentLoopQuickCheck.Create();
        };
    }
    public static void Run()
    {
        SessionState.SetBool("EquipmentLoopCheck", true);
        EditorSceneManager.OpenScene("Assets/Scenes/Demo_Main.unity");
        EditorApplication.EnterPlaymode();
    }
}
