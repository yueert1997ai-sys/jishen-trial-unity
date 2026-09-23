using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;

public static class CombatSliceR9Build
{
    public static void Build()
    {
        const string output="Builds/CombatSlice_R9_20260916";
        Directory.CreateDirectory(output);
        var previous=PlayerSettings.bundleVersion;
        try
        {
            PlayerSettings.bundleVersion="slice-r9-audio-overhaul-20260916";
            foreach(var clip in Directory.GetFiles("Assets/Resources/Audio/Combat/R9","*.wav"))
                AssetDatabase.ImportAsset(clip.Replace('\\','/'),ImportAssetOptions.ForceUpdate);
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/Scenes/Demo_Main.unity"},locationPathName=output+"/MECH_TRIAL_P0.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.StrictMode});
            File.WriteAllText("AuditEvidence/combat-slice-r9/build-result.txt",report.summary.result+" errors="+report.summary.totalErrors+" warnings="+report.summary.totalWarnings);
            if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Combat slice build failed");
        }
        finally{PlayerSettings.bundleVersion=previous;}
    }
}
