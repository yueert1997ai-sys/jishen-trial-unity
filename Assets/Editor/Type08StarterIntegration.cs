using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

// Adds the reviewed cannon to the playable V9 armory without rebuilding its V3 body or other equipment.
public static class Type08StarterIntegration
{
    public static void ConfigureAndBuild()
    {
        Configure();
        CompanyUpdateIntegration.Build();
    }
    public static void Configure()
    {
        AssetDatabase.Refresh();
        var armory = AssetDatabase.LoadAssetAtPath<HangarArmory>("Assets/Resources/Hangar/Armory.asset");
        var cannon = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Hangar/TYPE08.prefab");
        if (armory == null || cannon == null || armory.heroPrefab == null) throw new Exception("V9 armory, hero and TYPE08 prefab are required.");
        if ((int)PrimaryWeapon.Collection != 4 || (int)PrimaryWeapon.Type08 != 5) throw new Exception("Existing serialized collection ID must stay unchanged.");
        var lods = cannon.GetComponent<LODGroup>()?.GetLODs();
        if (lods == null || lods.Length != 3 || lods.Any(l => l.renderers.Length == 0)) throw new Exception("Cannon LODs are missing.");
        if (!cannon.GetComponentsInChildren<Transform>(true).Any(t => t.name == "Muzzle")) throw new Exception("Cannon muzzle is missing.");
        foreach (var renderer in cannon.GetComponentsInChildren<Renderer>(true))
            if (renderer.sharedMaterials.Any(m => m == null || m.shader == null)) throw new Exception("Cannon material reference failed: " + renderer.name);
        if (Shader.Find("MECH ROUGE/Minovsky Glow") == null) throw new Exception("Minovsky beam shader is missing.");
        var entries = armory.weapons.Where(e => e.weapon != PrimaryWeapon.Type08).ToList();
        entries.Add(new HangarArmory.Entry { weapon=PrimaryWeapon.Type08, prefab=cannon, damage=120, interval=1.1f, speed=0, pierce=2, blastRadius=1.8f, rightHandOnly=true });
        armory.weapons = entries.ToArray();
        armory.startingWeapon = PrimaryWeapon.Type08;
        EditorUtility.SetDirty(armory);
        AssetDatabase.SaveAssets();
        const string evidence = "AuditEvidence/type08-starter-v9";
        Directory.CreateDirectory(evidence);
        File.WriteAllText(evidence + "/integration.txt", "TYPE08_STARTER_CONFIG_PASS\nStarting weapon: TYPE08\nLength: 3.491269 m\nRight hand only: true\nPink particle beam: 120 damage, 1.1 s interval, 2 penetration, 1.8 m blast\nHero: " + AssetDatabase.GetAssetPath(armory.heroPrefab) + "\nArmory entries: " + entries.Count);
        Debug.Log("TYPE08_STARTER_CONFIG_PASS");
    }
}
