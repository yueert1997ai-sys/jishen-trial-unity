using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class RemoveValkyrShoulderCannon
{
    public static void Build()
    {
        const string evidence="AuditEvidence/no-shoulder-cannon";Directory.CreateDirectory(evidence);
        const string path="Assets/Resources/Hangar/VALKYR_V9.prefab";
        var root=PrefabUtility.LoadPrefabContents(path);
        try{
            var targets=root.GetComponentsInChildren<Transform>(true).Where(t=>t.name=="Cannon_Pitch_Trunnion"||t.name=="Cannon_Right_Cradle_Mount").ToArray();
            if(targets.Length!=2)throw new Exception("Unexpected hero cannon hierarchy");
            var renderers=targets.SelectMany(t=>t.GetComponentsInChildren<Renderer>(true)).Distinct().ToArray();
            File.WriteAllLines(evidence+"/removed-meshes.txt",renderers.Select(r=>r.name));
            foreach(var renderer in renderers){var filter=renderer.GetComponent<MeshFilter>();if(filter!=null)UnityEngine.Object.DestroyImmediate(filter);UnityEngine.Object.DestroyImmediate(renderer);}
            foreach(var group in root.GetComponentsInChildren<LODGroup>(true)){
                var lods=group.GetLODs();for(int i=0;i<lods.Length;i++)lods[i].renderers=lods[i].renderers.Where(r=>r!=null).ToArray();group.SetLODs(lods);
            }
            var pose=root.GetComponent<RigidMechPoseDriver>();var rig=root.GetComponent<RiggedMechAnimator>();
            if(pose.cannon==null||rig.muzzle==null)throw new Exception("Animation and fallback muzzle anchors must remain valid");
            if(targets.Any(t=>t.GetComponentsInChildren<Renderer>(true).Length!=0))throw new Exception("Cannon geometry remains");
            PrefabUtility.SaveAsPrefabAsset(root,path);AssetDatabase.SaveAssets();
        }finally{PrefabUtility.UnloadPrefabContents(root);}
        const string output="Builds/P0_10_1_NoCannon/MECH_TRIAL_P0.exe";Directory.CreateDirectory(Path.GetDirectoryName(output));string previous=PlayerSettings.bundleVersion;
        try{
            PlayerSettings.bundleVersion="p0.10.1-no-shoulder-cannon-20260911";
            var result=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/Scenes/Demo_Main.unity"},locationPathName=output,target=BuildTarget.StandaloneWindows64,options=BuildOptions.StrictMode});
            File.WriteAllText(evidence+"/build-result.txt",result.summary.result+" errors="+result.summary.totalErrors+" warnings="+result.summary.totalWarnings);
            if(result.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded)throw new Exception("No cannon build failed");
        }finally{PlayerSettings.bundleVersion=previous;}
    }
}
