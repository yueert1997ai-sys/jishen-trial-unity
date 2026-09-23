using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;

public static class P0LunarNavBuild
{
    public static void Rebuild()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/Demo_Main.unity");
        var sector=UnityEngine.Object.FindFirstObjectByType<ArenaSector>();
        if(sector==null||sector.lunar==null)throw new Exception("Missing lunar sector");
        var sources=sector.lunar.GetComponentsInChildren<MeshFilter>(true).Select(m=>new NavMeshBuildSource{
            shape=NavMeshBuildSourceShape.Mesh,sourceObject=m.sharedMesh,transform=m.transform.localToWorldMatrix,area=0}).ToList();
        var settings=NavMesh.GetSettingsByIndex(0);
        // Player capsule: radius .72, height 3.25, step .45. Leave clearance at props/overhangs.
        settings.agentRadius=.8f;settings.agentHeight=3.4f;settings.agentClimb=.45f;settings.agentSlope=45;
        settings.overrideVoxelSize=true;settings.voxelSize=.1f;
        var data=NavMeshBuilder.BuildNavMeshData(settings,sources,new Bounds(Vector3.zero,new Vector3(56,24,56)),Vector3.zero,Quaternion.identity);
        if(data==null)throw new Exception("Lunar navigation bake failed");
        const string path="Assets/Art/LunarBaseV9/MARE07_Navigation.asset";
        var existing=AssetDatabase.LoadAssetAtPath<NavMeshData>(path);
        if(existing==null)throw new Exception("Missing existing lunar navigation asset");
        EditorUtility.CopySerialized(data,existing);UnityEngine.Object.DestroyImmediate(data);EditorUtility.SetDirty(existing);
        AssetDatabase.SaveAssets();
        File.WriteAllText("AuditEvidence/p0-lunar-fix/nav-build.txt","Updated existing lunar nav asset: radius=.8 height=3.4 climb=.45 voxel=.1; render meshes and scene preserved.");
    }
}
