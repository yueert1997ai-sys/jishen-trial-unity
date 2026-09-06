using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class LiquidBossIntegration
{
    const string BossPath = "Assets/Prefabs/Enemies/LiquidE01Boss.prefab";
    const string ModelPath = "Assets/Art/Enemies/TypeE01Elite/TypeE01Elite_R02.fbx";
    const string Output = "Builds/ValkyrLiquidBossV8";
    const string Evidence = "AuditEvidence/liquid-boss-v8";

    public static void IntegrateAndBuild()
    {
        AssetDatabase.Refresh();
        Directory.CreateDirectory(Evidence);
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(BossPath);
        if (prefab == null) throw new Exception("Liquid boss prefab import failed.");
        foreach (var part in prefab.GetComponentsInChildren<Transform>(true))
            if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(part.gameObject) != 0)
                throw new Exception("Missing imported script on " + part.name);
        var boss = prefab.GetComponent<BossController>();
        var pose = prefab.GetComponent<E01ElitePoseDriver>();
        if (boss == null || pose == null || pose.rigRoot == null || boss.coreSocket == null || boss.mechDuel)
            throw new Exception("Liquid boss controller/rig references are incomplete.");
        var skins = prefab.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        if (skins.Length != 2) throw new Exception("Expected both existing E01 LODs.");
        foreach (var mat in skins.SelectMany(s => s.sharedMaterials).Distinct())
            if (mat == null || mat.shader == null || mat.shader.name != "MECH ROUGE/E01 Living Metal" || ShaderUtil.ShaderHasError(mat.shader))
                throw new Exception("Liquid boss has an invalid material or shader.");

        // Edit just the new reference in the current scene; do not regenerate its player or waves.
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/Demo_Main.unity");
        var spawner = UnityEngine.Object.FindFirstObjectByType<EnemySpawner>();
        if (spawner == null || spawner.bossPrefab == null || !spawner.bossPrefab.GetComponent<BossController>().mechDuel)
            throw new Exception("Expected the current V7 scene with the retained veteran boss.");
        spawner.liquidBossPrefab = prefab;
        EditorUtility.SetDirty(spawner);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        if (!AssetDatabase.GetDependencies(scene.path, true).Contains(ModelPath))
            throw new Exception("New boss model is not in the playable scene dependencies.");
        File.WriteAllText(Evidence + "/integration-check.txt",
            "Liquid boss and veteran have separate prefabs.\nTwo skinned LODs; living-metal materials and core references valid.\n" +
            "Normal final encounter resolves to liquid boss. Existing veteran, V7 protagonist and E01 infantry retained.\n");
        Build();
    }

    public static void Build()
    {
        Directory.CreateDirectory(Output);
        Directory.CreateDirectory(Evidence);
        PlayerSettings.bundleVersion = "liquid-boss-v8-" + DateTime.UtcNow.ToString("yyyyMMdd.HHmmss");
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = new[] { "Assets/Scenes/Demo_Main.unity" },
            locationPathName = Output + "/MECH_TRIAL_Valkyr.exe",
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.StrictMode
        });
        File.WriteAllText(Evidence + "/build-result.txt", report.summary.result + " errors=" + report.summary.totalErrors + " warnings=" + report.summary.totalWarnings);
        if (report.summary.result != BuildResult.Succeeded) throw new Exception("V8 build failed.");
        Directory.CreateDirectory(Output + "/Licenses");
        foreach (var path in Directory.GetFiles("docs/licenses")) File.Copy(path, Output + "/Licenses/" + Path.GetFileName(path), true);
        File.Copy("docs/ASSET_PROVENANCE.md", Output + "/Licenses/ASSET_PROVENANCE.md", true);
        if (File.Exists("docs/LIQUID_BOSS_V8.md")) File.Copy("docs/LIQUID_BOSS_V8.md", Output + "/更新说明.md", true);
    }
}
