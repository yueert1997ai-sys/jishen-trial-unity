using System;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;

// Run only on a materialized staging copy made by tools/apple/prepare_testflight.py.
// No scene regeneration, desktop promotion, signing secrets or upload occurs here.
public static class TestFlightBuild
{
    public static void Export()
    {
        string root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        if (!Application.isBatchMode || !File.Exists(Path.Combine(root, ".testflight-staging.json")))
            throw new BuildFailedException("Use a staging copy and Unity -batchmode -buildTarget iOS.");
        if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.iOS, BuildTarget.iOS))
            throw new BuildFailedException("Install iOS Build Support for this exact Unity version.");
        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.iOS)
            throw new BuildFailedException("Start Unity with -buildTarget iOS.");

        string bundle = Required("MECH_APPLE_BUNDLE_ID");
        string version = Required("MECH_APPLE_VERSION");
        string build = Required("MECH_APPLE_BUILD");
        if (!Regex.IsMatch(bundle, @"^[A-Za-z][A-Za-z0-9-]*(\.[A-Za-z0-9-]+)+$") || bundle.Contains("example"))
            throw new BuildFailedException("Supply the actual registered Bundle ID, not an example.");
        if (!Regex.IsMatch(version, @"^[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}$"))
            throw new BuildFailedException("Version must be numeric major.minor.patch, e.g. 0.1.0.");
        if (!Regex.IsMatch(build, @"^[1-9][0-9]{0,3}$"))
            throw new BuildFailedException("Build must be a positive integer from 1 to 9999; increment after upload.");
        string output = Path.Combine(root, "Builds", "iPhone", version + "-" + build);
        if (Directory.Exists(output)) throw new BuildFailedException("Refusing to overwrite " + output);
        string team = Environment.GetEnvironmentVariable("MECH_APPLE_TEAM_ID") ?? "";
        if (team.Length != 0 && !Regex.IsMatch(team, "^[A-Z0-9]{10}$"))
            throw new BuildFailedException("Team ID must be 10 uppercase letters/digits, or omitted for signing in Xcode.");

        PlayerSettings.productName = "机神试炼";
        PlayerSettings.bundleVersion = version;
        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, bundle);
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.iOS, ScriptingImplementation.IL2CPP);
        PlayerSettings.iOS.buildNumber = build;
        PlayerSettings.iOS.targetDevice = iOSTargetDevice.iPhoneOnly;
        PlayerSettings.iOS.sdkVersion = iOSSdkVersion.DeviceSDK;
        PlayerSettings.iOS.targetOSVersionString = "15.0";
        PlayerSettings.iOS.appleDeveloperTeamID = team;
        PlayerSettings.iOS.appleEnableAutomaticSigning = true;
        PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.iOS, false);
        PlayerSettings.SetGraphicsAPIs(BuildTarget.iOS, new[] { GraphicsDeviceType.Metal });
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
        PlayerSettings.allowedAutorotateToPortrait = false;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
        PlayerSettings.allowedAutorotateToLandscapeLeft = true;
        PlayerSettings.allowedAutorotateToLandscapeRight = true;
        AssetDatabase.SaveAssets();
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
            scenes = new[] { "Assets/Scenes/Demo_Main.unity" },
            locationPathName = output, target = BuildTarget.iOS, options = BuildOptions.StrictMode
        });
        if (report.summary.result != BuildResult.Succeeded)
            throw new BuildFailedException("iPhone export failed: " + report.summary.result);
        Debug.Log("TESTFLIGHT_XCODE_EXPORT_OK " + output + " — archive/sign/device-test in Xcode before upload.");
    }

    private static string Required(string key)
    {
        string value = Environment.GetEnvironmentVariable(key);
        if (string.IsNullOrWhiteSpace(value)) throw new BuildFailedException("Missing " + key);
        return value.Trim();
    }
}
