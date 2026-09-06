using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class ValkyrMotionInspection
{
    public static void BuildMotionRelease()
        => BuildRelease("ValkyrMotionV3","motion-v3");
    public static void BuildActionEffectsRelease()
        => BuildRelease("ValkyrActionV4","action-vfx");
    private static void BuildRelease(string folder,string evidence)
    {
        string output=Path.GetFullPath("Builds/"+folder);Directory.CreateDirectory(output);
        PlayerSettings.bundleVersion=evidence+"-"+System.DateTime.UtcNow.ToString("yyyyMMdd.HHmmss");
        var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/Scenes/Demo_Main.unity"},locationPathName=Path.Combine(output,"MECH_TRIAL_Valkyr.exe"),target=BuildTarget.StandaloneWindows64,options=BuildOptions.StrictMode});
        Directory.CreateDirectory("AuditEvidence/"+evidence);
        File.WriteAllText("AuditEvidence/"+evidence+"/build-result.txt",report.summary.result+" errors="+report.summary.totalErrors+" warnings="+report.summary.totalWarnings);
        if(report.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded)throw new System.Exception("Motion build failed");
        Directory.CreateDirectory(Path.Combine(output,"Licenses"));
        foreach(var file in Directory.GetFiles("docs/licenses"))File.Copy(file,Path.Combine(output,"Licenses",Path.GetFileName(file)),true);
        File.Copy("docs/ASSET_PROVENANCE.md",Path.Combine(output,"Licenses","ASSET_PROVENANCE.md"),true);
    }
    public static void CreateProfile()
    {
        Directory.CreateDirectory("Assets/Resources/ValkyrMotion");
        string path="Assets/Resources/ValkyrMotion/CombatMotion.asset";
        var profile=AssetDatabase.LoadAssetAtPath<ValkyrMotionProfile>(path);
        if(profile==null){profile=ScriptableObject.CreateInstance<ValkyrMotionProfile>();profile.slash=ValkyrMotionProfile.DefaultSlash();AssetDatabase.CreateAsset(profile,path);}
        AssetDatabase.SaveAssets();
    }
    public static void Inspect()
    {
        var model=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(ValkyrCombatIntegration.Visual));
        var sb=new StringBuilder();
        foreach(var t in model.GetComponentsInChildren<Transform>(true))
            if(t.name.Contains("V3B") || new[]{"Pelvis","Waist","Thorax","Head","UpperArm.R","Forearm.R","Hand.R","UpperArm.L","Forearm.L","Hand.L","Thigh.R","Shin.R","Foot.R","Thigh.L","Shin.L","Foot.L","RAIKEN_GRIP_SOCKET","RAIKEN_BLADE_TIP"}.Contains(t.name))
                sb.AppendLine(t.name+" parent="+t.parent.name+" position="+t.position.ToString("F4")+" rotation="+t.eulerAngles.ToString("F2")+" local="+t.localPosition.ToString("F4")+" right="+t.right.ToString("F3")+" up="+t.up.ToString("F3")+" forward="+t.forward.ToString("F3"));
        Directory.CreateDirectory("AuditEvidence/motion-v3");
        File.WriteAllText("AuditEvidence/motion-v3/rig-before.txt",sb.ToString());
        Object.DestroyImmediate(model);
    }
}
