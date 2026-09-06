using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.Build.Reporting;

public static class PowerComboIntegration
{
    public static void PrepareAndBuild()
    {
        AssetDatabase.Refresh();
        const string path="Assets/Resources/ValkyrMotion/PowerComboV7.asset";
        var profile=AssetDatabase.LoadAssetAtPath<ValkyrComboProfile>(path);
        if(profile==null){profile=ScriptableObject.CreateInstance<ValkyrComboProfile>();AssetDatabase.CreateAsset(profile,path);}
        profile.strokes=ValkyrComboProfile.PowerDefaults();EditorUtility.SetDirty(profile);AssetDatabase.SaveAssets();
        foreach(var file in Directory.GetFiles("Assets/Resources/Audio/Combat/RaikenV7","*.wav"))
        {
            var importer=AssetImporter.GetAtPath(file.Replace('\\','/')) as AudioImporter;
            var sample=importer.defaultSampleSettings;sample.loadType=AudioClipLoadType.DecompressOnLoad;
            sample.compressionFormat=AudioCompressionFormat.PCM;sample.sampleRateSetting=AudioSampleRateSetting.PreserveSampleRate;
            importer.defaultSampleSettings=sample;importer.forceToMono=false;importer.loadInBackground=false;importer.SaveAndReimport();
        }
        Build();
    }
    public static void Build()
    {
        PlayerSettings.bundleVersion="power-combo-v7-"+DateTime.UtcNow.ToString("yyyyMMdd.HHmmss");
        const string folder="Builds/ValkyrPowerComboV7";Directory.CreateDirectory(folder);
        var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/Scenes/Demo_Main.unity"},locationPathName=folder+"/MECH_TRIAL_Valkyr.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.StrictMode});
        Directory.CreateDirectory("AuditEvidence/power-combo-v7");
        File.WriteAllText("AuditEvidence/power-combo-v7/build-result.txt",report.summary.result+" errors="+report.summary.totalErrors+" warnings="+report.summary.totalWarnings);
        if(report.summary.result!=BuildResult.Succeeded)throw new Exception("V7 build failed");
        Directory.CreateDirectory(folder+"/Licenses");foreach(var f in Directory.GetFiles("docs/licenses"))File.Copy(f,folder+"/Licenses/"+Path.GetFileName(f),true);
        File.Copy("docs/ASSET_PROVENANCE.md",folder+"/Licenses/ASSET_PROVENANCE.md",true);
    }
}
