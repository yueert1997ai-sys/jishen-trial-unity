using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;

public static class P0CombatBuild
{
    public static void Build()
    {
        const string output="Builds/P0_9_Clear";
        const string evidence="AuditEvidence/p0-9-clear";
        Directory.CreateDirectory(output);Directory.CreateDirectory(evidence);
        var previous=PlayerSettings.bundleVersion;
        try
        {
            PlayerSettings.bundleVersion="p0.9.1-clear-20260910";
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/Scenes/Demo_Main.unity"},locationPathName=output+"/MECH_TRIAL_P0.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.StrictMode});
            File.WriteAllText(evidence+"/build-result.txt",report.summary.result+" errors="+report.summary.totalErrors+" warnings="+report.summary.totalWarnings);
            if(report.summary.result!=BuildResult.Succeeded)throw new Exception("P0 build failed");
        }
        finally {PlayerSettings.bundleVersion=previous;}
    }
}
