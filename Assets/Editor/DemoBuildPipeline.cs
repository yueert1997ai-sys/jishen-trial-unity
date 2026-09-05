using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class DemoBuildPipeline
{
    private const string ScenePath = "Assets/Scenes/Demo_Main.unity";

    [MenuItem("MECH ROUGE/Build Windows Demo")]
    public static void BuildWindowsDemo()
    {
        string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        string outputDirectory = Path.Combine(projectRoot, "Builds", "Windows", "MECH_ROUGE_Demo");
        string executablePath = Path.Combine(outputDirectory, "MECH_ROUGE.exe");
        Directory.CreateDirectory(outputDirectory);

        PlayerSettings.companyName = "Yue";
        PlayerSettings.productName = "MECH ROUGE";
        PlayerSettings.defaultScreenWidth = 1600;
        PlayerSettings.defaultScreenHeight = 900;
        PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
        PlayerSettings.resizableWindow = true;
        PlayerSettings.runInBackground = true;

        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = new[] { ScenePath },
            locationPathName = executablePath,
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.StrictMode
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
        {
            throw new UnityEditor.Build.BuildFailedException("Windows build failed: " + report.summary.result + " with " + report.summary.totalErrors + " errors.");
        }

        Debug.Log("MECH_ROUGE_BUILD_PASS: " + executablePath + " size_bytes=" + report.summary.totalSize + " duration=" + report.summary.totalTime);
    }
}
