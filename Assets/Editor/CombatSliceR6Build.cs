using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;

public static class CombatSliceR6Build
{
    public static void Build()
    {
        const string output="Builds/CombatSlice_R6_20260915";
        Directory.CreateDirectory(output);
        var previous=PlayerSettings.bundleVersion;
        try
        {
            PlayerSettings.bundleVersion="slice-r6-absorption-20260915";
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/Scenes/Demo_Main.unity"},locationPathName=output+"/MECH_TRIAL_P0.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.StrictMode});
            File.WriteAllText("AuditEvidence/combat-slice-r6/build-result.txt",report.summary.result+" errors="+report.summary.totalErrors+" warnings="+report.summary.totalWarnings);
            if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Combat slice build failed");
        }
        finally{PlayerSettings.bundleVersion=previous;}
    }
}
