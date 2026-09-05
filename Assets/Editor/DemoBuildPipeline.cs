using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class DemoBuildPipeline
{
    private const string ScenePath = "Assets/Scenes/Demo_Main.unity";

    [MenuItem("MECH ROUGE/Build Versioned Mobile Slice (Windows)")]
    public static void BuildMobileSlice()
    {
        string root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        string version = System.DateTime.UtcNow.ToString("yyyyMMdd.HHmmss");
        string directory = Path.Combine(root, "Builds", "Windows", "MECH_TRIAL_" + version);
        if (Directory.Exists(directory)) throw new IOException("Refusing to overwrite " + directory);
        Directory.CreateDirectory(directory);
        PlayerSettings.companyName = "Yue";
        PlayerSettings.productName = "MECH TRIAL";
        PlayerSettings.bundleVersion = version;
        PlayerSettings.defaultScreenWidth = 1600;
        PlayerSettings.defaultScreenHeight = 900;
        PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
        PlayerSettings.resizableWindow = true;
        PlayerSettings.runInBackground = true;
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
        PlayerSettings.allowedAutorotateToLandscapeLeft = true;
        PlayerSettings.allowedAutorotateToLandscapeRight = true;
        PlayerSettings.allowedAutorotateToPortrait = false;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
        PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
        AssetDatabase.SaveAssets();
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
            scenes = new[] { ScenePath }, locationPathName = Path.Combine(directory, "MECH_TRIAL.exe"),
            target = BuildTarget.StandaloneWindows64, options = BuildOptions.StrictMode });
        if (report.summary.result != BuildResult.Succeeded)
            throw new UnityEditor.Build.BuildFailedException("Mobile slice build failed: " + report.summary.result);
        string licenses = Path.Combine(directory, "Licenses");
        Directory.CreateDirectory(licenses);
        foreach (string file in Directory.GetFiles(Path.Combine(root, "docs", "licenses")))
            File.Copy(file, Path.Combine(licenses, Path.GetFileName(file)));
        File.Copy(Path.Combine(root, "docs", "ASSET_PROVENANCE.md"), Path.Combine(licenses, "ASSET_PROVENANCE.md"));
        File.Copy(Path.Combine(root, "docs", "PLAYTEST_README.md"), Path.Combine(directory, "START_HERE.md"));
        File.WriteAllText(Path.Combine(directory, "BUILD.txt"), "MECH TRIAL " + version + "\nUnity " + Application.unityVersion
            + "\nWindows x64 / Mono / release / strict build\nSize bytes: " + report.summary.totalSize
            + "\nBuild errors: " + report.summary.totalErrors + "\nBuild warnings: " + report.summary.totalWarnings
            + "\nBuild duration: " + report.summary.totalTime + "\n");
        Directory.CreateDirectory(Path.Combine(root, "AuditEvidence"));
        File.WriteAllText(Path.Combine(root, "AuditEvidence", "latest-build.txt"), directory);
        Debug.Log("MECH_TRIAL_BUILD_PASS " + directory);
    }

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
