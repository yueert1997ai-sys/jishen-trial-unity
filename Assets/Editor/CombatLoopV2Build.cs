using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;

public static class CombatLoopV2Build
{
    public static void Build()
    {
        string output=Environment.GetEnvironmentVariable("MECH_LOOP_V2_BUILD")??"Builds/CombatLoop_V2_20260916";
        string evidence=Environment.GetEnvironmentVariable("MECH_LOOP_V2_EVIDENCE")??"AuditEvidence/combat-loop-v2";
        Directory.CreateDirectory(output);
        Directory.CreateDirectory(evidence);
        var previous=PlayerSettings.bundleVersion;
        try
        {
            PlayerSettings.bundleVersion=Environment.GetEnvironmentVariable("MECH_LOOP_V2_VERSION")??"combat-loop-v2-20260916";
            foreach(var clip in Directory.GetFiles("Assets/Resources/Audio/Combat/R9","*.wav"))
                AssetDatabase.ImportAsset(clip.Replace('\\','/'),ImportAssetOptions.ForceUpdate);
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/Scenes/Demo_Main.unity"},locationPathName=output+"/MECH_TRIAL_P0.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.StrictMode});
            File.WriteAllText(Path.Combine(evidence,"build-result.txt"),report.summary.result+" errors="+report.summary.totalErrors+" warnings="+report.summary.totalWarnings);
            if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Combat slice build failed");
        }
        finally{PlayerSettings.bundleVersion=previous;}
    }
}
