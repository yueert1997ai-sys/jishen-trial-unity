using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class DemoSceneBuilder
{
    private const string ScenePath = "Assets/Scenes/Demo_Main.unity";
    private const string GuestGlbPath = "Assets/UserContent/PlayerMech/Meshy_AI_Rose_Gold_Sentinel_0618172915_texture.glb";
    private const string GuestObjPath = "Assets/UserContent/PlayerMech/Decimated/RoseGoldSentinel_LOD_High.obj";
    private const string GuestMediumObjPath = "Assets/UserContent/PlayerMech/Decimated/RoseGoldSentinel_LOD_Medium.obj";
    private const string GuestLowObjPath = "Assets/UserContent/PlayerMech/Decimated/RoseGoldSentinel_LOD.obj";
    private const string GuestBaseColorPath = "Assets/UserContent/PlayerMech/Decimated/RoseGoldSentinel_LOD_High_baseColor.jpg";
    private const string GuestNormalPath = "Assets/UserContent/PlayerMech/Decimated/RoseGoldSentinel_LOD_High_normal.jpg";
    private const string GuestMetallicSmoothnessPath = "Assets/UserContent/PlayerMech/Decimated/RoseGoldSentinel_LOD_High_metallicSmoothness.png";
    private const string GuestEmissivePath = "Assets/UserContent/PlayerMech/Decimated/RoseGoldSentinel_LOD_High_emissive.jpg";
    private const string GuestMaterialPath = "Assets/Materials/MR_RoseGoldSentinel_PBR.mat";
    private const string GuestVisualPrefabPath = "Assets/Prefabs/Player/RoseGoldSentinelVisual.prefab";
    private const string GuestPlayerPrefabPath = "Assets/Prefabs/Player/PlayerMech_Guest.prefab";

    [MenuItem("MECH ROUGE/Build Phase 1 Demo")]
    public static void BuildDemo()
    {
        EnsureDirectories();
        Dictionary<string, Material> materials = CreateMaterials();
        Dictionary<string, GameObject> equipmentPrefabs = CreateEquipmentPrefabs(materials);
        Dictionary<string, EquipmentData> equipmentData = CreateEquipmentData(equipmentPrefabs);
        GameObject defaultMech = CreateDefaultMechPrefab(materials);
        GameObject guestVisual = CreateGuestMechVisualPrefab(materials);
        GameObject playerRootPrefab = CreatePlayerRootPrefab(guestVisual != null ? guestVisual : defaultMech, equipmentData, materials, GuestPlayerPrefabPath);
        Dictionary<EnemyKind, GameObject> enemyPrefabs = CreateEnemyPrefabs(materials);
        GameObject bossPrefab = CreateBossPrefab(materials);
        CreateScene(playerRootPrefab, enemyPrefabs, bossPrefab, equipmentData, materials);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        if (!Application.isBatchMode)
        {
            EditorUtility.DisplayDialog("MECH ROUGE", "Phase 1 demo generated.\nOpen Assets/Scenes/Demo_Main.unity and press Play.", "OK");
        }
    }

    private static void EnsureDirectories()
    {
        string[] paths =
        {
            "Assets/Scenes",
            "Assets/Materials",
            "Assets/Prefabs",
            "Assets/Prefabs/Player",
            "Assets/Prefabs/Equipment",
            "Assets/Prefabs/Enemies",
            "Assets/Prefabs/Projectiles",
            "Assets/ScriptableObjects",
            "Assets/ScriptableObjects/Equipment",
            "Assets/UserContent",
            "Assets/UserContent/PlayerMech"
        };

        for (int i = 0; i < paths.Length; i++)
        {
            if (!AssetDatabase.IsValidFolder(paths[i]))
            {
                string parent = System.IO.Path.GetDirectoryName(paths[i]).Replace("\\", "/");
                string name = System.IO.Path.GetFileName(paths[i]);
                AssetDatabase.CreateFolder(parent, name);
            }
        }
    }

    private static Dictionary<string, Material> CreateMaterials()
    {
        Dictionary<string, Material> materials = new Dictionary<string, Material>();
        materials["White"] = CreateMaterial("MR_ArmorWhite", new Color(0.86f, 0.88f, 0.88f));
        materials["Blue"] = CreateMaterial("MR_ArmorBlue", new Color(0.03f, 0.25f, 0.58f));
        materials["Navy"] = CreateMaterial("MR_WingNavy", new Color(0.02f, 0.06f, 0.12f));
        materials["Red"] = CreateMaterial("MR_AccentRed", new Color(1f, 0.12f, 0.08f));
        materials["Yellow"] = CreateMaterial("MR_AntennaYellow", new Color(1f, 0.75f, 0.05f));
        materials["Grey"] = CreateMaterial("MR_FrameGrey", new Color(0.32f, 0.34f, 0.36f));
        materials["Dark"] = CreateMaterial("MR_DarkMetal", new Color(0.08f, 0.09f, 0.1f));
        materials["Cyan"] = CreateEmissiveMaterial("MR_EnergyCyan", new Color(0.06f, 0.72f, 1f), 1.8f);
        materials["Enemy"] = CreateMaterial("MR_EnemyRed", new Color(0.55f, 0.08f, 0.08f));
        materials["EnemyDark"] = CreateMaterial("MR_EnemyDark", new Color(0.12f, 0.09f, 0.1f));
        materials["EnemySteel"] = CreateMaterial("MR_EnemySteel", new Color(0.22f, 0.25f, 0.28f));
        materials["EnemyGlow"] = CreateEmissiveMaterial("MR_EnemyGlow", new Color(1f, 0.08f, 0.025f), 2.4f);
        materials["DroneGlow"] = CreateEmissiveMaterial("MR_DroneGlow", new Color(1f, 0.38f, 0.025f), 2.5f);
        materials["EliteGlow"] = CreateEmissiveMaterial("MR_EliteGlow", new Color(1f, 0.04f, 0.42f), 2.6f);
        materials["Ground"] = CreateMaterial("MR_Ground", new Color(0.045f, 0.06f, 0.075f));
        materials["Grid"] = CreateMaterial("MR_Grid", new Color(0.12f, 0.19f, 0.23f));
        materials["Pad"] = CreateMaterial("MR_HangarPad", new Color(0.025f, 0.13f, 0.2f));
        materials["PadLight"] = CreateEmissiveMaterial("MR_HangarPadLight", new Color(0.025f, 0.28f, 0.52f), 0.85f);
        materials["Boundary"] = CreateMaterial("MR_ArenaBoundary", new Color(0.035f, 0.045f, 0.06f));
        return materials;
    }

    private static Material CreateMaterial(string name, Color color)
    {
        string path = "Assets/Materials/" + name + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            Shader shader = Shader.Find("Standard");
            if (shader == null)
            {
                shader = Shader.Find("Universal Render Pipeline/Lit");
            }

            material = new Material(shader);
            AssetDatabase.CreateAsset(material, path);
        }

        material.color = color;
        EditorUtility.SetDirty(material);
        return material;
    }

    private static Material CreateEmissiveMaterial(string name, Color color, float intensity)
    {
        Material material = CreateMaterial(name, color);
        if (material.HasProperty("_EmissionColor"))
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", color * intensity);
            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            EditorUtility.SetDirty(material);
        }

        return material;
    }

    private static Dictionary<string, GameObject> CreateEquipmentPrefabs(Dictionary<string, Material> materials)
    {
        Dictionary<string, GameObject> prefabs = new Dictionary<string, GameObject>();
        for (int level = 1; level <= 3; level++)
        {
            prefabs["BeamRifle_Lv" + level] = SavePrefab(CreateBeamRifle(level, materials), "Assets/Prefabs/Equipment/BeamRifle_Lv" + level + ".prefab");
            prefabs["MissilePod_Lv" + level] = SavePrefab(CreateMissilePod(level, materials), "Assets/Prefabs/Equipment/MissilePod_Lv" + level + ".prefab");
            prefabs["BoosterPack_Lv" + level] = SavePrefab(CreateBoosterPack(level, materials), "Assets/Prefabs/Equipment/BoosterPack_Lv" + level + ".prefab");
            prefabs["Shield_Lv" + level] = SavePrefab(CreateShield(level, materials), "Assets/Prefabs/Equipment/Shield_Lv" + level + ".prefab");
        }

        return prefabs;
    }

    private static GameObject CreateBeamRifle(int level, Dictionary<string, Material> materials)
    {
        GameObject root = new GameObject("BeamRifle_Lv" + level);
        float length = 0.9f + level * 0.28f;
        AddBox(root.transform, "Grip", new Vector3(0f, -0.18f, 0.05f), new Vector3(0.16f, 0.34f, 0.12f), materials["Grey"], Vector3.zero);
        AddBox(root.transform, "Receiver", new Vector3(0f, 0f, 0.28f), new Vector3(0.24f, 0.2f, 0.55f), materials["White"], Vector3.zero);
        AddBox(root.transform, "Barrel", new Vector3(0f, 0.02f, 0.65f), new Vector3(0.12f, 0.12f, length), materials["Dark"], Vector3.zero);
        AddBox(root.transform, "BlueCasing", new Vector3(0f, 0.11f, 0.28f), new Vector3(0.28f, 0.08f, 0.42f), materials["Blue"], Vector3.zero);
        if (level >= 2)
        {
            AddBox(root.transform, "EnergyCell", new Vector3(0f, 0.15f, 0.62f), new Vector3(0.16f, 0.07f, 0.5f), materials["Cyan"], Vector3.zero);
        }

        if (level >= 3)
        {
            AddBox(root.transform, "RedSight", new Vector3(0f, 0.23f, 0.2f), new Vector3(0.08f, 0.08f, 0.22f), materials["Red"], Vector3.zero);
            AddBox(root.transform, "LongEmitter", new Vector3(0f, 0.02f, 1.25f), new Vector3(0.1f, 0.1f, 0.44f), materials["Cyan"], Vector3.zero);
        }

        return root;
    }

    private static GameObject CreateMissilePod(int level, Dictionary<string, Material> materials)
    {
        GameObject root = new GameObject("MissilePod_Lv" + level);
        AddBox(root.transform, "PodBody", Vector3.zero, new Vector3(0.62f + level * 0.12f, 0.28f + level * 0.06f, 0.58f), materials["Navy"], Vector3.zero);
        int tubes = level == 1 ? 2 : level == 2 ? 4 : 6;
        for (int i = 0; i < tubes; i++)
        {
            float x = (i % 3 - 1) * 0.2f;
            float y = i / 3 == 0 ? 0.07f : -0.09f;
            AddBox(root.transform, "Tube" + i, new Vector3(x, y, 0.34f), new Vector3(0.13f, 0.13f, 0.34f), materials["Grey"], Vector3.zero);
            AddBox(root.transform, "Warhead" + i, new Vector3(x, y, 0.54f), new Vector3(0.09f, 0.09f, 0.1f), materials["Red"], Vector3.zero);
        }

        return root;
    }

    private static GameObject CreateBoosterPack(int level, Dictionary<string, Material> materials)
    {
        GameObject root = new GameObject("BoosterPack_Lv" + level);
        AddBox(root.transform, "BackCore", new Vector3(0f, 0f, -0.05f), new Vector3(0.62f + level * 0.12f, 0.75f, 0.3f), materials["Blue"], Vector3.zero);
        AddBox(root.transform, "LeftNozzle", new Vector3(-0.23f, -0.35f, -0.22f), new Vector3(0.16f, 0.2f, 0.18f), materials["Dark"], Vector3.zero);
        AddBox(root.transform, "RightNozzle", new Vector3(0.23f, -0.35f, -0.22f), new Vector3(0.16f, 0.2f, 0.18f), materials["Dark"], Vector3.zero);
        AddBox(root.transform, "LeftWing", new Vector3(-0.52f - level * 0.08f, 0.12f, -0.22f), new Vector3(0.22f, 1.1f + level * 0.32f, 0.12f), materials["Navy"], new Vector3(0f, 0f, -18f));
        AddBox(root.transform, "RightWing", new Vector3(0.52f + level * 0.08f, 0.12f, -0.22f), new Vector3(0.22f, 1.1f + level * 0.32f, 0.12f), materials["Navy"], new Vector3(0f, 0f, 18f));
        if (level >= 2)
        {
            AddBox(root.transform, "BlueWingStripeL", new Vector3(-0.64f, 0.06f, -0.12f), new Vector3(0.06f, 0.88f, 0.14f), materials["Blue"], new Vector3(0f, 0f, -18f));
            AddBox(root.transform, "BlueWingStripeR", new Vector3(0.64f, 0.06f, -0.12f), new Vector3(0.06f, 0.88f, 0.14f), materials["Blue"], new Vector3(0f, 0f, 18f));
        }

        if (level >= 3)
        {
            AddBox(root.transform, "LeftEnergyTrail", new Vector3(-0.23f, -0.58f, -0.28f), new Vector3(0.1f, 0.46f, 0.1f), materials["Cyan"], Vector3.zero);
            AddBox(root.transform, "RightEnergyTrail", new Vector3(0.23f, -0.58f, -0.28f), new Vector3(0.1f, 0.46f, 0.1f), materials["Cyan"], Vector3.zero);
        }

        return root;
    }

    private static GameObject CreateShield(int level, Dictionary<string, Material> materials)
    {
        GameObject root = new GameObject("Shield_Lv" + level);
        float height = 0.95f + level * 0.28f;
        AddBox(root.transform, "ShieldMain", new Vector3(0f, 0f, 0f), new Vector3(0.38f + level * 0.08f, height, 0.14f), materials["White"], Vector3.zero);
        AddBox(root.transform, "ShieldDarkFace", new Vector3(0f, 0f, 0.08f), new Vector3(0.22f + level * 0.05f, height * 0.78f, 0.04f), materials["Dark"], Vector3.zero);
        AddBox(root.transform, "ShieldRedStripe", new Vector3(0f, 0f, 0.12f), new Vector3(0.06f, height * 0.62f, 0.04f), materials["Red"], Vector3.zero);
        if (level >= 3)
        {
            AddBox(root.transform, "ShieldEnergyLip", new Vector3(0f, height * 0.46f, 0.14f), new Vector3(0.34f, 0.05f, 0.05f), materials["Cyan"], Vector3.zero);
        }

        return root;
    }

    private static Dictionary<string, EquipmentData> CreateEquipmentData(Dictionary<string, GameObject> prefabs)
    {
        Dictionary<string, EquipmentData> data = new Dictionary<string, EquipmentData>();
        data["BeamRifle"] = SaveEquipmentData("BeamRifle", "Beam Rifle", EquipmentType.RightHandWeapon, "RightHandSocket", 60, 8f, 7f, 0f, 0f, 0f, prefabs, "Default beam rifle. Lv2 fires twin shots. Lv3 fires triple piercing shots.");
        data["MissilePod"] = SaveEquipmentData("MissilePod", "Shoulder Missile Pod", EquipmentType.RightShoulder, "RightShoulderSocket", 85, 5f, 0f, 0f, 0f, 0f, prefabs, "Homing missiles for clearing groups. Higher levels add tubes and blast radius.");
        data["BoosterPack"] = SaveEquipmentData("BoosterPack", "Wing Booster Pack", EquipmentType.Backpack, "BackSocket", 75, 0f, 6f, 0f, 20f, 1.4f, prefabs, "Back-mounted wing booster. Improves dash and energy. Lv3 grants fire-rate after dash.");
        data["Shield"] = SaveEquipmentData("Shield", "Aegis Shield", EquipmentType.Shield, "LeftArmSocket", 70, 0f, 0f, 25f, 0f, 0f, prefabs, "Arm shield. Reduces frontal damage and reflects some damage at Lv3.");
        return data;
    }

    private static EquipmentData SaveEquipmentData(string id, string displayName, EquipmentType type, string socket, int price, float damage, float fireRate, float hp, float energy, float dash, Dictionary<string, GameObject> prefabs, string description)
    {
        string path = "Assets/ScriptableObjects/Equipment/" + id + ".asset";
        EquipmentData data = AssetDatabase.LoadAssetAtPath<EquipmentData>(path);
        if (data == null)
        {
            data = ScriptableObject.CreateInstance<EquipmentData>();
            AssetDatabase.CreateAsset(data, path);
        }

        data.equipmentId = id;
        data.displayName = displayName;
        data.equipmentType = type;
        data.targetSocketName = socket;
        data.level = 1;
        data.maxLevel = 3;
        data.price = price;
        data.damageBonus = damage;
        data.fireRateBonus = fireRate;
        data.hpBonus = hp;
        data.energyBonus = energy;
        data.dashBonus = dash;
        string prefix = id == "BeamRifle" ? "BeamRifle" : id == "MissilePod" ? "MissilePod" : id == "BoosterPack" ? "BoosterPack" : "Shield";
        data.prefabLevel1 = prefabs[prefix + "_Lv1"];
        data.prefabLevel2 = prefabs[prefix + "_Lv2"];
        data.prefabLevel3 = prefabs[prefix + "_Lv3"];
        data.description = description;
        EditorUtility.SetDirty(data);
        return data;
    }

    private static GameObject CreateDefaultMechPrefab(Dictionary<string, Material> materials)
    {
        GameObject root = new GameObject("PlayerMech_Default");
        Transform artRoot = new GameObject("WingedArmorArt").transform;
        artRoot.SetParent(root.transform, false);
        artRoot.localPosition = new Vector3(0f, 0.85f, 0f);

        AddBox(artRoot, "PelvisFrame", new Vector3(0f, 0.92f, 0.02f), new Vector3(0.7f, 0.28f, 0.4f), materials["White"], Vector3.zero);
        AddBox(artRoot, "FrontSkirt", new Vector3(0f, 0.76f, 0.28f), new Vector3(0.46f, 0.24f, 0.14f), materials["White"], new Vector3(-12f, 0f, 0f));
        AddBox(artRoot, "WaistRedBlock", new Vector3(0f, 1.05f, 0.29f), new Vector3(0.2f, 0.14f, 0.08f), materials["Red"], Vector3.zero);

        AddLeg(artRoot, -1f, materials);
        AddLeg(artRoot, 1f, materials);

        AddBox(artRoot, "TorsoBlueCore", new Vector3(0f, 1.65f, 0f), new Vector3(0.7f, 0.72f, 0.46f), materials["Blue"], Vector3.zero);
        AddBox(artRoot, "LeftWhiteChestPlate", new Vector3(-0.22f, 1.72f, 0.27f), new Vector3(0.28f, 0.44f, 0.1f), materials["White"], new Vector3(0f, 0f, -8f));
        AddBox(artRoot, "RightWhiteChestPlate", new Vector3(0.22f, 1.72f, 0.27f), new Vector3(0.28f, 0.44f, 0.1f), materials["White"], new Vector3(0f, 0f, 8f));
        AddBox(artRoot, "ChestCyanSensor", new Vector3(0f, 1.88f, 0.34f), new Vector3(0.24f, 0.18f, 0.06f), materials["Cyan"], Vector3.zero);
        AddBox(artRoot, "ChestRedVent", new Vector3(0f, 1.48f, 0.34f), new Vector3(0.4f, 0.08f, 0.08f), materials["Red"], Vector3.zero);
        AddBox(artRoot, "BackpackSpine", new Vector3(0f, 1.75f, -0.36f), new Vector3(0.76f, 0.74f, 0.22f), materials["Dark"], Vector3.zero);
        AddBox(artRoot, "BackpackBlueCover", new Vector3(0f, 1.82f, -0.48f), new Vector3(0.52f, 0.5f, 0.12f), materials["Blue"], Vector3.zero);
        AddBox(artRoot, "LeftRearThruster", new Vector3(-0.16f, 1.45f, -0.58f), new Vector3(0.12f, 0.18f, 0.1f), materials["Red"], Vector3.zero);
        AddBox(artRoot, "RightRearThruster", new Vector3(0.16f, 1.45f, -0.58f), new Vector3(0.12f, 0.18f, 0.1f), materials["Red"], Vector3.zero);

        AddBox(artRoot, "Neck", new Vector3(0f, 2.12f, 0.02f), new Vector3(0.22f, 0.14f, 0.18f), materials["Dark"], Vector3.zero);
        AddBox(artRoot, "HeadWhiteBlock", new Vector3(0f, 2.32f, 0.08f), new Vector3(0.36f, 0.3f, 0.34f), materials["White"], Vector3.zero);
        AddBox(artRoot, "FaceGreyMask", new Vector3(0f, 2.27f, 0.29f), new Vector3(0.24f, 0.12f, 0.06f), materials["Grey"], Vector3.zero);
        AddBox(artRoot, "ForeheadCyanSensor", new Vector3(0f, 2.42f, 0.29f), new Vector3(0.15f, 0.07f, 0.05f), materials["Cyan"], Vector3.zero);
        AddBox(artRoot, "LeftVFin", new Vector3(-0.17f, 2.56f, 0.12f), new Vector3(0.05f, 0.5f, 0.04f), materials["Yellow"], new Vector3(0f, 0f, -34f));
        AddBox(artRoot, "RightVFin", new Vector3(0.17f, 2.56f, 0.12f), new Vector3(0.05f, 0.5f, 0.04f), materials["Yellow"], new Vector3(0f, 0f, 34f));
        AddBox(artRoot, "CenterAntenna", new Vector3(0f, 2.58f, 0.07f), new Vector3(0.04f, 0.36f, 0.04f), materials["White"], Vector3.zero);

        AddArm(artRoot, -1f, materials);
        AddArm(artRoot, 1f, materials);
        AddIntegratedRifle(artRoot, 1f, materials);
        AddIntegratedShield(artRoot, -1f, materials);
        AddWingSet(artRoot, -1f, materials);
        AddWingSet(artRoot, 1f, materials);

        return SavePrefab(root, "Assets/Prefabs/Player/PlayerMech_Default.prefab");
    }

    private static GameObject CreateGuestMechVisualPrefab(Dictionary<string, Material> materials)
    {
        GameObject root = new GameObject("RoseGoldSentinelVisual");
        bool importedGlb = false;

        if (System.IO.File.Exists(GuestObjPath))
        {
            Material guestMaterial = CreateGuestPbrMaterial();
            GameObject highModel = InstantiateGuestLod(root.transform, GuestObjPath, "RoseGoldSentinel_LOD_High", guestMaterial);
            if (highModel != null)
            {
                GameObject mediumModel = InstantiateGuestLod(root.transform, GuestMediumObjPath, "RoseGoldSentinel_LOD_Medium", guestMaterial);
                GameObject lowModel = InstantiateGuestLod(root.transform, GuestLowObjPath, "RoseGoldSentinel_LOD_Low", guestMaterial);
                ConfigureGuestLodGroup(root, highModel, mediumModel, lowModel);
                importedGlb = true;
            }
        }

        if (!importedGlb && System.IO.File.Exists(GuestGlbPath))
        {
            AssetDatabase.ImportAsset(GuestGlbPath, ImportAssetOptions.ForceSynchronousImport);
            GameObject glbAsset = AssetDatabase.LoadAssetAtPath<GameObject>(GuestGlbPath);
            if (glbAsset != null)
            {
                GameObject model = (GameObject)PrefabUtility.InstantiatePrefab(glbAsset);
                model.name = "RoseGoldSentinel";
                model.transform.SetParent(root.transform, false);
                model.transform.localPosition = Vector3.zero;
                model.transform.localRotation = Quaternion.identity;
                model.transform.localScale = Vector3.one;
                FitVisualToHeightAndGround(model.transform, 2.8f);
                importedGlb = true;
            }
        }

        if (!importedGlb)
        {
            Debug.Log("MECH ROUGE: Supplied GLB/decimated OBJ was not imported as a GameObject. Generated a rose-gold placeholder visual for the playable demo.");
            AddGuestPlaceholder(root.transform, materials);
        }

        return SavePrefab(root, GuestVisualPrefabPath);
    }

    private static GameObject InstantiateGuestLod(Transform parent, string assetPath, string objectName, Material material)
    {
        if (!System.IO.File.Exists(assetPath))
        {
            return null;
        }

        AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);
        GameObject objAsset = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
        if (objAsset == null)
        {
            return null;
        }

        GameObject model = (GameObject)PrefabUtility.InstantiatePrefab(objAsset);
        model.name = objectName;
        model.transform.SetParent(parent, false);
        model.transform.localPosition = Vector3.zero;
        model.transform.localRotation = Quaternion.identity;
        model.transform.localScale = Vector3.one;
        FitVisualToHeightAndGround(model.transform, 2.8f);
        ApplyMaterialToHierarchy(model, material);
        return model;
    }

    private static void ConfigureGuestLodGroup(GameObject root, GameObject highModel, GameObject mediumModel, GameObject lowModel)
    {
        if (root == null || highModel == null)
        {
            return;
        }

        List<LOD> lods = new List<LOD>();
        lods.Add(new LOD(0.2f, highModel.GetComponentsInChildren<Renderer>(true)) { fadeTransitionWidth = 0.12f });
        if (mediumModel != null)
        {
            lods.Add(new LOD(0.075f, mediumModel.GetComponentsInChildren<Renderer>(true)) { fadeTransitionWidth = 0.12f });
        }

        if (lowModel != null)
        {
            lods.Add(new LOD(0.018f, lowModel.GetComponentsInChildren<Renderer>(true)) { fadeTransitionWidth = 0.12f });
        }

        LODGroup group = root.AddComponent<LODGroup>();
        group.fadeMode = LODFadeMode.CrossFade;
        group.animateCrossFading = false;
        group.SetLODs(lods.ToArray());
        group.RecalculateBounds();
    }

    private static Material CreateGuestPbrMaterial()
    {
        ConfigureTextureImport(GuestBaseColorPath, TextureImporterType.Default, true);
        ConfigureTextureImport(GuestNormalPath, TextureImporterType.NormalMap, false);
        ConfigureTextureImport(GuestMetallicSmoothnessPath, TextureImporterType.Default, false);
        ConfigureTextureImport(GuestEmissivePath, TextureImporterType.Default, true);

        Material material = AssetDatabase.LoadAssetAtPath<Material>(GuestMaterialPath);
        if (material == null)
        {
            Shader shader = Shader.Find("Standard");
            if (shader == null)
            {
                shader = Shader.Find("Universal Render Pipeline/Lit");
            }

            material = new Material(shader);
            AssetDatabase.CreateAsset(material, GuestMaterialPath);
        }

        Texture2D baseColor = AssetDatabase.LoadAssetAtPath<Texture2D>(GuestBaseColorPath);
        Texture2D normal = AssetDatabase.LoadAssetAtPath<Texture2D>(GuestNormalPath);
        Texture2D metallicSmoothness = AssetDatabase.LoadAssetAtPath<Texture2D>(GuestMetallicSmoothnessPath);
        Texture2D emissive = AssetDatabase.LoadAssetAtPath<Texture2D>(GuestEmissivePath);

        if (material.HasProperty("_MainTex"))
        {
            material.SetTexture("_MainTex", baseColor);
        }

        if (material.HasProperty("_BaseMap"))
        {
            material.SetTexture("_BaseMap", baseColor);
        }

        if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", Color.white);
        }

        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", Color.white);
        }

        if (normal != null && material.HasProperty("_BumpMap"))
        {
            material.SetTexture("_BumpMap", normal);
            material.SetFloat("_BumpScale", 1f);
            material.EnableKeyword("_NORMALMAP");
        }

        if (metallicSmoothness != null && material.HasProperty("_MetallicGlossMap"))
        {
            material.SetTexture("_MetallicGlossMap", metallicSmoothness);
            material.SetFloat("_Metallic", 1f);
            material.SetFloat("_GlossMapScale", 0.72f);
            material.EnableKeyword("_METALLICGLOSSMAP");
        }

        if (emissive != null && material.HasProperty("_EmissionMap"))
        {
            material.SetTexture("_EmissionMap", emissive);
            material.SetColor("_EmissionColor", Color.white);
            material.EnableKeyword("_EMISSION");
            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        }

        if (material.HasProperty("_Cull"))
        {
            material.SetInt("_Cull", 0);
        }

        material.doubleSidedGI = true;
        EditorUtility.SetDirty(material);
        return material;
    }

    private static void ApplyMaterialToHierarchy(GameObject root, Material material)
    {
        if (root == null || material == null)
        {
            return;
        }

        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            Material[] assigned = renderers[i].sharedMaterials;
            for (int j = 0; j < assigned.Length; j++)
            {
                assigned[j] = material;
            }

            renderers[i].sharedMaterials = assigned;
        }
    }

    private static void ConfigureTextureImport(string texturePath, TextureImporterType textureType, bool srgb)
    {
        if (!System.IO.File.Exists(texturePath))
        {
            return;
        }

        AssetDatabase.ImportAsset(texturePath, ImportAssetOptions.ForceSynchronousImport);
        TextureImporter importer = AssetImporter.GetAtPath(texturePath) as TextureImporter;
        if (importer == null)
        {
            return;
        }

        importer.maxTextureSize = 4096;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.textureType = textureType;
        importer.sRGBTexture = srgb;
        importer.mipmapEnabled = true;
        importer.filterMode = FilterMode.Trilinear;
        TextureImporterPlatformSettings defaultSettings = importer.GetDefaultPlatformTextureSettings();
        defaultSettings.maxTextureSize = 4096;
        defaultSettings.format = TextureImporterFormat.RGBA32;
        defaultSettings.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SetPlatformTextureSettings(defaultSettings);
        importer.SaveAndReimport();
    }

    private static void AddGuestPlaceholder(Transform root, Dictionary<string, Material> materials)
    {
        Material rose = CreateMaterial("MR_RoseGoldPlaceholder", new Color(0.92f, 0.58f, 0.42f));
        Material gold = CreateMaterial("MR_GoldPlaceholder", new Color(1f, 0.77f, 0.28f));
        Material dark = materials["Dark"];
        AddBox(root, "FallbackTorso", new Vector3(0f, 1.35f, 0f), new Vector3(0.72f, 0.92f, 0.45f), rose, Vector3.zero);
        AddBox(root, "FallbackHead", new Vector3(0f, 2.02f, 0.08f), new Vector3(0.38f, 0.32f, 0.32f), gold, Vector3.zero);
        AddBox(root, "FallbackShoulderL", new Vector3(-0.62f, 1.62f, 0f), new Vector3(0.42f, 0.28f, 0.44f), rose, new Vector3(0f, 0f, -12f));
        AddBox(root, "FallbackShoulderR", new Vector3(0.62f, 1.62f, 0f), new Vector3(0.42f, 0.28f, 0.44f), rose, new Vector3(0f, 0f, 12f));
        AddBox(root, "FallbackArmL", new Vector3(-0.82f, 1.14f, 0f), new Vector3(0.24f, 0.72f, 0.24f), rose, Vector3.zero);
        AddBox(root, "FallbackArmR", new Vector3(0.82f, 1.14f, 0f), new Vector3(0.24f, 0.72f, 0.24f), rose, Vector3.zero);
        AddBox(root, "FallbackLegL", new Vector3(-0.28f, 0.38f, 0f), new Vector3(0.3f, 0.84f, 0.3f), rose, Vector3.zero);
        AddBox(root, "FallbackLegR", new Vector3(0.28f, 0.38f, 0f), new Vector3(0.3f, 0.84f, 0.3f), rose, Vector3.zero);
        AddBox(root, "FallbackWingL", new Vector3(-0.84f, 1.35f, -0.34f), new Vector3(0.22f, 1.5f, 0.12f), dark, new Vector3(0f, 0f, -18f));
        AddBox(root, "FallbackWingR", new Vector3(0.84f, 1.35f, -0.34f), new Vector3(0.22f, 1.5f, 0.12f), dark, new Vector3(0f, 0f, 18f));
        AddBox(root, "FallbackVFinL", new Vector3(-0.16f, 2.24f, 0.08f), new Vector3(0.04f, 0.4f, 0.04f), gold, new Vector3(0f, 0f, -32f));
        AddBox(root, "FallbackVFinR", new Vector3(0.16f, 2.24f, 0.08f), new Vector3(0.04f, 0.4f, 0.04f), gold, new Vector3(0f, 0f, 32f));
        FitVisualToHeightAndGround(root, 2.8f);
    }

    private static void FitVisualToHeightAndGround(Transform visual, float targetHeight)
    {
        Bounds bounds;
        if (!TryGetRendererBounds(visual, out bounds) || bounds.size.y <= 0.001f)
        {
            return;
        }

        float scale = targetHeight / bounds.size.y;
        visual.localScale *= scale;

        if (!TryGetRendererBounds(visual, out bounds))
        {
            return;
        }

        Vector3 position = visual.localPosition;
        position.y -= bounds.min.y;
        visual.localPosition = position;
    }

    private static bool TryGetRendererBounds(Transform root, out Bounds bounds)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>();
        bounds = new Bounds(root.position, Vector3.zero);
        bool hasRenderer = false;
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null)
            {
                continue;
            }

            if (!hasRenderer)
            {
                bounds = renderers[i].bounds;
                hasRenderer = true;
            }
            else
            {
                bounds.Encapsulate(renderers[i].bounds);
            }
        }

        return hasRenderer;
    }

    private static void AddLeg(Transform root, float side, Dictionary<string, Material> materials)
    {
        float x = side * 0.34f;
        AddBox(root, "Thigh_" + side, new Vector3(x, 0.46f, 0f), new Vector3(0.28f, 0.56f, 0.28f), materials["White"], Vector3.zero);
        AddBox(root, "HipJoint_" + side, new Vector3(x, 0.78f, 0f), new Vector3(0.34f, 0.18f, 0.3f), materials["Grey"], Vector3.zero);
        AddBox(root, "KneeBlue_" + side, new Vector3(x, 0.22f, 0.18f), new Vector3(0.3f, 0.18f, 0.12f), materials["Blue"], new Vector3(12f, 0f, 0f));
        AddBox(root, "ShinWhite_" + side, new Vector3(x, -0.2f, 0f), new Vector3(0.34f, 0.72f, 0.3f), materials["White"], Vector3.zero);
        AddBox(root, "ShinBlueSide_" + side, new Vector3(x + side * 0.17f, -0.18f, 0.03f), new Vector3(0.08f, 0.46f, 0.3f), materials["Blue"], Vector3.zero);
        AddBox(root, "ShinRedVent_" + side, new Vector3(x, -0.24f, 0.2f), new Vector3(0.08f, 0.34f, 0.05f), materials["Red"], Vector3.zero);
        AddBox(root, "FootBlue_" + side, new Vector3(x, -0.67f, 0.18f), new Vector3(0.46f, 0.2f, 0.64f), materials["Blue"], new Vector3(0f, side * 5f, 0f));
        AddBox(root, "ToeWhite_" + side, new Vector3(x, -0.6f, 0.44f), new Vector3(0.28f, 0.12f, 0.22f), materials["White"], Vector3.zero);
    }

    private static void AddArm(Transform root, float side, Dictionary<string, Material> materials)
    {
        AddBox(root, "ShoulderBlue_" + side, new Vector3(side * 0.62f, 1.93f, 0.02f), new Vector3(0.46f, 0.32f, 0.48f), materials["Blue"], new Vector3(0f, 0f, side * 8f));
        AddBox(root, "ShoulderWhiteCap_" + side, new Vector3(side * 0.79f, 2.03f, 0.03f), new Vector3(0.32f, 0.18f, 0.36f), materials["White"], new Vector3(0f, 0f, side * 14f));
        AddBox(root, "ShoulderRedEdge_" + side, new Vector3(side * 0.91f, 1.96f, 0.08f), new Vector3(0.08f, 0.12f, 0.26f), materials["Red"], new Vector3(0f, 0f, side * 14f));
        AddBox(root, "UpperArm_" + side, new Vector3(side * 0.86f, 1.48f, 0.02f), new Vector3(0.24f, 0.5f, 0.24f), materials["White"], new Vector3(0f, 0f, side * 5f));
        AddBox(root, "Elbow_" + side, new Vector3(side * 0.88f, 1.18f, 0.03f), new Vector3(0.22f, 0.18f, 0.24f), materials["Grey"], Vector3.zero);
        AddBox(root, "Forearm_" + side, new Vector3(side * 0.9f, 0.93f, 0.05f), new Vector3(0.26f, 0.42f, 0.26f), materials["White"], Vector3.zero);
        AddBox(root, "Hand_" + side, new Vector3(side * 0.9f, 0.67f, 0.05f), new Vector3(0.2f, 0.16f, 0.2f), materials["Dark"], Vector3.zero);
    }

    private static void AddIntegratedRifle(Transform root, float side, Dictionary<string, Material> materials)
    {
        AddBox(root, "DefaultRifleReceiver", new Vector3(side * 1.1f, 0.9f, 0.38f), new Vector3(0.18f, 0.44f, 0.18f), materials["Grey"], new Vector3(8f, 0f, 0f));
        AddBox(root, "DefaultRifleBlueCasing", new Vector3(side * 1.1f, 1.02f, 0.48f), new Vector3(0.13f, 0.18f, 0.08f), materials["Blue"], Vector3.zero);
        AddBox(root, "DefaultRifleBarrel", new Vector3(side * 1.1f, 0.52f, 0.67f), new Vector3(0.08f, 0.66f, 0.08f), materials["Dark"], new Vector3(18f, 0f, 0f));
        AddBox(root, "DefaultRifleMuzzle", new Vector3(side * 1.1f, 0.22f, 0.83f), new Vector3(0.1f, 0.12f, 0.1f), materials["Cyan"], new Vector3(18f, 0f, 0f));
    }

    private static void AddIntegratedShield(Transform root, float side, Dictionary<string, Material> materials)
    {
        AddBox(root, "DefaultShieldOuter", new Vector3(side * 1.18f, 1.22f, 0.28f), new Vector3(0.42f, 1.04f, 0.1f), materials["White"], new Vector3(0f, 0f, -side * 8f));
        AddBox(root, "DefaultShieldDarkFace", new Vector3(side * 1.19f, 1.22f, 0.35f), new Vector3(0.28f, 0.8f, 0.05f), materials["Dark"], new Vector3(0f, 0f, -side * 8f));
        AddBox(root, "DefaultShieldRedStripe", new Vector3(side * 1.2f, 1.38f, 0.4f), new Vector3(0.07f, 0.58f, 0.04f), materials["Red"], new Vector3(0f, 0f, -side * 8f));
    }

    private static void AddWingSet(Transform root, float side, Dictionary<string, Material> materials)
    {
        AddTriangularPrism(root, "OuterWing_" + side, new Vector2(side * 0.34f, 2.12f), new Vector2(side * 1.72f, 1.9f), new Vector2(side * 1.22f, 0.46f), -0.45f, 0.1f, materials["Navy"]);
        AddTriangularPrism(root, "InnerWingBlue_" + side, new Vector2(side * 0.5f, 1.92f), new Vector2(side * 1.32f, 1.65f), new Vector2(side * 1.02f, 0.72f), -0.36f, 0.08f, materials["Blue"]);
        AddBox(root, "WingBase_" + side, new Vector3(side * 0.46f, 1.9f, -0.28f), new Vector3(0.26f, 0.38f, 0.22f), materials["Grey"], new Vector3(0f, 0f, side * 18f));
        AddBox(root, "ShoulderCannon_" + side, new Vector3(side * 0.42f, 2.18f, -0.08f), new Vector3(0.09f, 0.5f, 0.09f), materials["Dark"], new Vector3(-18f, 0f, 0f));
        AddBox(root, "ShoulderCannonGlow_" + side, new Vector3(side * 0.42f, 2.36f, 0.04f), new Vector3(0.08f, 0.09f, 0.08f), materials["Cyan"], new Vector3(-18f, 0f, 0f));
    }

    private static GameObject CreatePlayerRootPrefab(GameObject defaultMech, Dictionary<string, EquipmentData> equipmentData, Dictionary<string, Material> materials, string prefabPath)
    {
        GameObject root = new GameObject("PlayerMechRoot");
        root.transform.position = Vector3.zero;
        Transform visualRoot = new GameObject("VisualRoot").transform;
        visualRoot.SetParent(root.transform, false);
        Transform hardpoints = new GameObject("Hardpoints").transform;
        hardpoints.SetParent(root.transform, false);
        CreateSocket(hardpoints, "HeadSocket", new Vector3(0f, 3.18f, 0.18f));
        CreateSocket(hardpoints, "ChestSocket", new Vector3(0f, 2.58f, 0.5f));
        CreateSocket(hardpoints, "BackSocket", new Vector3(0f, 2.58f, -0.72f));
        CreateSocket(hardpoints, "LeftShoulderSocket", new Vector3(-0.86f, 2.8f, 0.1f));
        CreateSocket(hardpoints, "RightShoulderSocket", new Vector3(0.86f, 2.8f, 0.1f));
        CreateSocket(hardpoints, "LeftHandSocket", new Vector3(-1.05f, 1.78f, 0.62f));
        CreateSocket(hardpoints, "RightHandSocket", new Vector3(1.05f, 1.78f, 0.62f));
        CreateSocket(hardpoints, "LeftArmSocket", new Vector3(-1.02f, 2.05f, 0.28f));
        CreateSocket(hardpoints, "RightArmSocket", new Vector3(1.02f, 2.05f, 0.28f));
        CreateSocket(hardpoints, "WaistSocket", new Vector3(0f, 1.72f, 0.02f));

        Transform colliderRoot = new GameObject("Collider").transform;
        colliderRoot.SetParent(root.transform, false);
        CapsuleCollider collider = colliderRoot.gameObject.AddComponent<CapsuleCollider>();
        collider.center = new Vector3(0f, 1.65f, 0f);
        collider.height = 3.25f;
        collider.radius = 0.86f;

        Transform weaponMuzzle = new GameObject("WeaponMuzzle").transform;
        weaponMuzzle.SetParent(root.transform, false);
        weaponMuzzle.localPosition = new Vector3(0.78f, 1.72f, 1.05f);
        Transform cameraTarget = new GameObject("CameraTarget").transform;
        cameraTarget.SetParent(root.transform, false);
        cameraTarget.localPosition = new Vector3(0f, 1.85f, 0f);
        Transform groundCheck = new GameObject("GroundCheck").transform;
        groundCheck.SetParent(root.transform, false);
        groundCheck.localPosition = Vector3.zero;

        Transform groundMarker = new GameObject("PlayerGroundMarker").transform;
        groundMarker.SetParent(root.transform, false);
        AddBox(groundMarker, "MarkerFront", new Vector3(0f, 0.085f, 1.02f), new Vector3(0.62f, 0.035f, 0.09f), materials["Cyan"], Vector3.zero);
        AddBox(groundMarker, "MarkerBack", new Vector3(0f, 0.085f, -1.02f), new Vector3(0.62f, 0.035f, 0.09f), materials["Cyan"], Vector3.zero);
        AddBox(groundMarker, "MarkerLeft", new Vector3(-1.02f, 0.085f, 0f), new Vector3(0.09f, 0.035f, 0.62f), materials["Cyan"], Vector3.zero);
        AddBox(groundMarker, "MarkerRight", new Vector3(1.02f, 0.085f, 0f), new Vector3(0.09f, 0.035f, 0.62f), materials["Cyan"], Vector3.zero);

        Damageable damageable = root.AddComponent<Damageable>();
        damageable.team = 0;
        damageable.maxHealth = 180f;
        damageable.destroyOnDeath = false;
        damageable.hitInvulnerabilityDuration = 0.13f;
        CombatFeedback playerFeedback = root.AddComponent<CombatFeedback>();
        playerFeedback.hitColor = new Color(1f, 0.32f, 0.18f, 1f);

        MechHardpointManager hardpointManager = root.AddComponent<MechHardpointManager>();
        hardpointManager.visualRoot = visualRoot;
        hardpointManager.hardpointsRoot = hardpoints;

        PlayerMechLoader loader = root.AddComponent<PlayerMechLoader>();
        loader.defaultMechPrefab = defaultMech;

        PlayerStats stats = root.AddComponent<PlayerStats>();
        PlayerController controller = root.AddComponent<PlayerController>();
        WeaponController weapon = root.AddComponent<WeaponController>();
        MechMotionAnimator motionAnimator = root.AddComponent<MechMotionAnimator>();
        EquipmentManager equipmentManager = root.AddComponent<EquipmentManager>();
        equipmentManager.playerStats = stats;
        equipmentManager.weaponController = weapon;
        equipmentManager.hardpointManager = hardpointManager;
        controller.stats = stats;
        controller.weaponController = weapon;
        motionAnimator.controller = controller;
        motionAnimator.weaponController = weapon;
        motionAnimator.visualRoot = visualRoot;
        weapon.playerStats = stats;
        weapon.playerController = controller;
        weapon.equipmentManager = equipmentManager;
        weapon.damageable = damageable;
        weapon.muzzle = weaponMuzzle;
        return SavePrefab(root, prefabPath);
    }

    private static Dictionary<EnemyKind, GameObject> CreateEnemyPrefabs(Dictionary<string, Material> materials)
    {
        Dictionary<EnemyKind, GameObject> prefabs = new Dictionary<EnemyKind, GameObject>();
        prefabs[EnemyKind.Melee] = SavePrefab(CreateEnemy(EnemyKind.Melee, materials), "Assets/Prefabs/Enemies/Enemy_Melee.prefab");
        prefabs[EnemyKind.Ranged] = SavePrefab(CreateEnemy(EnemyKind.Ranged, materials), "Assets/Prefabs/Enemies/Enemy_Ranged.prefab");
        prefabs[EnemyKind.Drone] = SavePrefab(CreateEnemy(EnemyKind.Drone, materials), "Assets/Prefabs/Enemies/Enemy_Drone.prefab");
        prefabs[EnemyKind.Elite] = SavePrefab(CreateEnemy(EnemyKind.Elite, materials), "Assets/Prefabs/Enemies/Enemy_Elite.prefab");
        return prefabs;
    }

    private static GameObject CreateEnemy(EnemyKind kind, Dictionary<string, Material> materials)
    {
        GameObject root = new GameObject("Enemy_" + kind);
        CapsuleCollider collider = root.AddComponent<CapsuleCollider>();
        collider.center = new Vector3(0f, 0.75f, 0f);
        collider.height = kind == EnemyKind.Drone ? 1f : 1.8f;
        collider.radius = kind == EnemyKind.Elite ? 0.8f : 0.55f;
        Damageable damageable = root.AddComponent<Damageable>();
        damageable.team = 1;
        CombatFeedback enemyFeedback = root.AddComponent<CombatFeedback>();
        enemyFeedback.hitColor = new Color(0.65f, 0.95f, 1f, 1f);
        WorldHealthBar healthBar = root.AddComponent<WorldHealthBar>();
        healthBar.height = kind == EnemyKind.Drone ? 1.35f : kind == EnemyKind.Elite ? 2.5f : 2.05f;
        healthBar.width = kind == EnemyKind.Elite ? 2f : 1.45f;
        EnemyBase enemy = root.AddComponent<EnemyBase>();
        enemy.kind = kind;
        Transform visualRoot = new GameObject("VisualRoot").transform;
        visualRoot.SetParent(root.transform, false);
        EnemyMotionAnimator motion = root.AddComponent<EnemyMotionAnimator>();
        motion.visualRoot = visualRoot;

        if (kind == EnemyKind.Melee)
        {
            damageable.maxHealth = 32f;
            enemy.moveSpeed = 3.7f;
            enemy.contactDamage = 7f;
            AddMeleeEnemyVisual(visualRoot, materials);
        }
        else if (kind == EnemyKind.Ranged)
        {
            damageable.maxHealth = 26f;
            enemy.moveSpeed = 2.9f;
            enemy.fireInterval = 1.65f;
            AddRangedEnemyVisual(visualRoot, materials);
        }
        else if (kind == EnemyKind.Drone)
        {
            damageable.maxHealth = 14f;
            enemy.moveSpeed = 4.7f;
            enemy.attackRange = 1.2f;
            motion.isDrone = true;
            AddDroneEnemyVisual(visualRoot, materials);
        }
        else
        {
            damageable.maxHealth = 68f;
            enemy.moveSpeed = 3.0f;
            enemy.fireInterval = 1.25f;
            enemy.killReward = 12;
            motion.isHeavy = true;
            AddEliteEnemyVisual(visualRoot, materials);
        }

        return root;
    }

    private static GameObject CreateBossPrefab(Dictionary<string, Material> materials)
    {
        var integrated = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemies/Boss_HeavyMech.prefab");
        if (integrated != null && integrated.GetComponent<E01ElitePoseDriver>() != null) return integrated;
        GameObject root = new GameObject("Boss_HeavyMech");
        CapsuleCollider collider = root.AddComponent<CapsuleCollider>();
        collider.center = new Vector3(0f, 1.4f, 0f);
        collider.height = 3.3f;
        collider.radius = 1.25f;
        Damageable damageable = root.AddComponent<Damageable>();
        damageable.team = 1;
        damageable.maxHealth = 480f;
        CombatFeedback bossFeedback = root.AddComponent<CombatFeedback>();
        bossFeedback.hitColor = new Color(1f, 0.75f, 0.25f, 1f);
        WorldHealthBar bossHealthBar = root.AddComponent<WorldHealthBar>();
        bossHealthBar.height = 3.6f;
        bossHealthBar.width = 3.8f;
        bossHealthBar.alwaysVisible = false;
        root.AddComponent<BossController>();
        Transform visualRoot = new GameObject("VisualRoot").transform;
        visualRoot.SetParent(root.transform, false);
        EnemyMotionAnimator motion = root.AddComponent<EnemyMotionAnimator>();
        motion.visualRoot = visualRoot;
        motion.isHeavy = true;
        AddBossEnemyVisual(visualRoot, materials);
        return SavePrefab(root, "Assets/Prefabs/Enemies/Boss_HeavyMech.prefab");
    }

    private static void CreateScene(GameObject playerRootPrefab, Dictionary<EnemyKind, GameObject> enemyPrefabs, GameObject bossPrefab, Dictionary<string, EquipmentData> equipmentData, Dictionary<string, Material> materials)
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        RenderSettings.ambientLight = new Color(0.3f, 0.33f, 0.38f);

        GameObject lightObject = new GameObject("Directional Light");
        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.15f;
        lightObject.transform.rotation = Quaternion.Euler(55f, -35f, 0f);

        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "ColonyOuterWallArena";
        ground.transform.localScale = new Vector3(6.4f, 1f, 6.4f);
        SetRendererMaterial(ground, materials["Ground"]);

        CreateArenaMarks(materials);

        GameObject player = (GameObject)PrefabUtility.InstantiatePrefab(playerRootPrefab);
        player.name = "PlayerMechRoot";
        player.transform.position = Vector3.zero;
        Transform cameraTarget = player.transform.Find("CameraTarget");

        GameObject cameraObject = new GameObject("Main Camera");
        Camera camera = cameraObject.AddComponent<Camera>();
        cameraObject.AddComponent<AudioListener>();
        camera.tag = "MainCamera";
        camera.fieldOfView = 41f;
        camera.nearClipPlane = 0.1f;
        camera.farClipPlane = 160f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.012f, 0.02f, 0.032f);
        cameraObject.transform.position = new Vector3(0f, 16f, -12.5f);
        cameraObject.transform.rotation = Quaternion.Euler(58f, 0f, 0f);
        CameraFollow follow = cameraObject.AddComponent<CameraFollow>();
        follow.target = cameraTarget != null ? cameraTarget : player.transform;
        follow.offset = new Vector3(0f, 16f, -12.5f);

        GameObject spawnerObject = new GameObject("EnemySpawner");
        EnemySpawner spawner = spawnerObject.AddComponent<EnemySpawner>();
        spawner.player = player.transform;
        spawner.meleePrefab = enemyPrefabs[EnemyKind.Melee];
        spawner.rangedPrefab = enemyPrefabs[EnemyKind.Ranged];
        spawner.dronePrefab = enemyPrefabs[EnemyKind.Drone];
        spawner.elitePrefab = enemyPrefabs[EnemyKind.Elite];
        spawner.bossPrefab = bossPrefab;

        GameObject managerObject = new GameObject("GameManager");
        managerObject.AddComponent<GameAudio>();
        managerObject.AddComponent<StandaloneRuntimeSmoke>();
        RunManager runManager = managerObject.AddComponent<RunManager>();
        StageManager stageManager = managerObject.AddComponent<StageManager>();
        RunUpgradeSystem upgradeSystem = managerObject.AddComponent<RunUpgradeSystem>();
        GameManager gameManager = managerObject.AddComponent<GameManager>();
        spawner.stageManager = stageManager;
        stageManager.enemySpawner = spawner;
        stageManager.player = player.transform;

        EquipmentManager equipmentManager = player.GetComponent<EquipmentManager>();
        PlayerStats playerStats = player.GetComponent<PlayerStats>();
        WeaponController weaponController = player.GetComponent<WeaponController>();
        upgradeSystem.playerStats = playerStats;
        upgradeSystem.weaponController = weaponController;
        if (weaponController != null)
        {
            weaponController.upgradeSystem = upgradeSystem;
        }

        GameObject hudObject = new GameObject("CombatHUD");
        CombatHUD combatHUD = hudObject.AddComponent<CombatHUD>();
        combatHUD.gameManager = gameManager;
        combatHUD.playerStats = playerStats;
        combatHUD.equipmentManager = equipmentManager;
        combatHUD.upgradeSystem = upgradeSystem;
        PauseUI pauseUI = new GameObject("PauseUI").AddComponent<PauseUI>();
        HangarDeploymentUI hangarUI = new GameObject("HangarDeploymentUI").AddComponent<HangarDeploymentUI>();
        RewardUI rewardUI = new GameObject("RewardUI").AddComponent<RewardUI>();
        ResultUI resultUI = new GameObject("ResultUI").AddComponent<ResultUI>();

        gameManager.runManager = runManager;
        gameManager.stageManager = stageManager;
        gameManager.playerController = player.GetComponent<PlayerController>();
        gameManager.playerStats = playerStats;
        gameManager.equipmentManager = equipmentManager;
        gameManager.upgradeSystem = upgradeSystem;
        gameManager.combatHUD = combatHUD;
        gameManager.hangarUI = hangarUI;
        gameManager.pauseUI = pauseUI;
        gameManager.rewardUI = rewardUI;
        gameManager.resultUI = resultUI;

        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
    }

    private static void CreateArenaMarks(Dictionary<string, Material> materials)
    {
        for (int i = -3; i <= 3; i++)
        {
            AddWorldBox("ArenaLineX" + i, new Vector3(i * 8f, 0.02f, 0f), new Vector3(0.07f, 0.035f, 56f), materials["Grid"]);
            AddWorldBox("ArenaLineZ" + i, new Vector3(0f, 0.021f, i * 8f), new Vector3(56f, 0.035f, 0.07f), materials["Grid"]);
        }

        AddWorldBox("HangarStartPad", new Vector3(0f, 0.035f, 0f), new Vector3(5.6f, 0.045f, 4.4f), materials["Pad"]);
        AddWorldBox("HangarPadFront", new Vector3(0f, 0.065f, 2.15f), new Vector3(5.7f, 0.035f, 0.09f), materials["PadLight"]);
        AddWorldBox("HangarPadBack", new Vector3(0f, 0.065f, -2.15f), new Vector3(5.7f, 0.035f, 0.09f), materials["PadLight"]);
        AddWorldBox("HangarPadLeft", new Vector3(-2.75f, 0.065f, 0f), new Vector3(0.09f, 0.035f, 4.4f), materials["PadLight"]);
        AddWorldBox("HangarPadRight", new Vector3(2.75f, 0.065f, 0f), new Vector3(0.09f, 0.035f, 4.4f), materials["PadLight"]);

        AddWorldBox("BoundaryNorth", new Vector3(0f, 0.6f, 30f), new Vector3(61f, 1.2f, 0.65f), materials["Boundary"]);
        AddWorldBox("BoundarySouth", new Vector3(0f, 0.6f, -30f), new Vector3(61f, 1.2f, 0.65f), materials["Boundary"]);
        AddWorldBox("BoundaryWest", new Vector3(-30f, 0.6f, 0f), new Vector3(0.65f, 1.2f, 61f), materials["Boundary"]);
        AddWorldBox("BoundaryEast", new Vector3(30f, 0.6f, 0f), new Vector3(0.65f, 1.2f, 61f), materials["Boundary"]);
        AddWorldBox("BoundaryNorthLight", new Vector3(0f, 1.22f, 29.62f), new Vector3(58f, 0.1f, 0.08f), materials["Cyan"]);
        AddWorldBox("BoundarySouthLight", new Vector3(0f, 1.22f, -29.62f), new Vector3(58f, 0.1f, 0.08f), materials["Cyan"]);

        AddWorldBox("StageDivider", new Vector3(0f, 0.05f, 12f), new Vector3(19f, 0.04f, 0.12f), materials["Blue"]);
        AddWorldBox("BossGateMark", new Vector3(0f, 0.06f, 23f), new Vector3(9f, 0.05f, 0.32f), materials["Red"]);
        AddWorldBox("BossGateLeft", new Vector3(-4.35f, 0.06f, 25f), new Vector3(0.32f, 0.05f, 4.2f), materials["Red"]);
        AddWorldBox("BossGateRight", new Vector3(4.35f, 0.06f, 25f), new Vector3(0.32f, 0.05f, 4.2f), materials["Red"]);
    }

    private static void AddMeleeEnemyVisual(Transform root, Dictionary<string, Material> materials)
    {
        AddBox(root, "RaiderWaist", new Vector3(0f, 0.72f, 0f), new Vector3(0.46f, 0.22f, 0.34f), materials["EnemySteel"], Vector3.zero);
        AddBox(root, "RaiderTorso", new Vector3(0f, 1.12f, 0f), new Vector3(0.72f, 0.72f, 0.5f), materials["EnemyDark"], Vector3.zero);
        AddTriangularPrism(root, "RaiderChestArmor", new Vector2(-0.5f, 0.95f), new Vector2(0f, 1.52f), new Vector2(0.5f, 0.95f), 0.3f, 0.16f, materials["Enemy"]);
        AddBox(root, "RaiderChestCore", new Vector3(0f, 1.18f, 0.43f), new Vector3(0.18f, 0.2f, 0.08f), materials["EnemyGlow"], Vector3.zero);
        AddBox(root, "RaiderHead", new Vector3(0f, 1.68f, 0.05f), new Vector3(0.38f, 0.3f, 0.38f), materials["EnemySteel"], Vector3.zero);
        AddBox(root, "RaiderVisor", new Vector3(0f, 1.68f, 0.26f), new Vector3(0.28f, 0.07f, 0.055f), materials["EnemyGlow"], Vector3.zero);
        AddBox(root, "RaiderShoulderL", new Vector3(-0.58f, 1.3f, 0.02f), new Vector3(0.44f, 0.28f, 0.58f), materials["Enemy"], new Vector3(0f, 0f, -14f));
        AddBox(root, "RaiderShoulderR", new Vector3(0.58f, 1.3f, 0.02f), new Vector3(0.44f, 0.28f, 0.58f), materials["Enemy"], new Vector3(0f, 0f, 14f));
        AddBox(root, "RaiderArmL", new Vector3(-0.62f, 0.88f, 0.08f), new Vector3(0.24f, 0.58f, 0.26f), materials["EnemySteel"], new Vector3(0f, 0f, -6f));
        AddBox(root, "RaiderArmR", new Vector3(0.62f, 0.88f, 0.08f), new Vector3(0.24f, 0.58f, 0.26f), materials["EnemySteel"], new Vector3(0f, 0f, 6f));
        AddTriangularPrism(root, "RaiderBladeL", new Vector2(-0.82f, 0.88f), new Vector2(-0.58f, 0.7f), new Vector2(-0.72f, 0.12f), 0.22f, 0.12f, materials["EnemyGlow"]);
        AddTriangularPrism(root, "RaiderBladeR", new Vector2(0.82f, 0.88f), new Vector2(0.58f, 0.7f), new Vector2(0.72f, 0.12f), 0.22f, 0.12f, materials["EnemyGlow"]);
        AddBox(root, "RaiderLegL", new Vector3(-0.23f, 0.34f, 0f), new Vector3(0.28f, 0.62f, 0.3f), materials["EnemyDark"], Vector3.zero);
        AddBox(root, "RaiderLegR", new Vector3(0.23f, 0.34f, 0f), new Vector3(0.28f, 0.62f, 0.3f), materials["EnemyDark"], Vector3.zero);
        AddBox(root, "RaiderKneeL", new Vector3(-0.23f, 0.44f, 0.2f), new Vector3(0.24f, 0.2f, 0.14f), materials["Enemy"], Vector3.zero);
        AddBox(root, "RaiderKneeR", new Vector3(0.23f, 0.44f, 0.2f), new Vector3(0.24f, 0.2f, 0.14f), materials["Enemy"], Vector3.zero);
        AddBox(root, "RaiderFootL", new Vector3(-0.23f, 0.08f, 0.16f), new Vector3(0.36f, 0.16f, 0.56f), materials["EnemySteel"], Vector3.zero);
        AddBox(root, "RaiderFootR", new Vector3(0.23f, 0.08f, 0.16f), new Vector3(0.36f, 0.16f, 0.56f), materials["EnemySteel"], Vector3.zero);
    }

    private static void AddRangedEnemyVisual(Transform root, Dictionary<string, Material> materials)
    {
        AddBox(root, "GunnerWaist", new Vector3(0f, 0.65f, 0f), new Vector3(0.4f, 0.18f, 0.32f), materials["EnemyDark"], Vector3.zero);
        AddBox(root, "GunnerTorso", new Vector3(0f, 1.02f, 0f), new Vector3(0.58f, 0.66f, 0.42f), materials["EnemySteel"], Vector3.zero);
        AddTriangularPrism(root, "GunnerChest", new Vector2(-0.38f, 0.88f), new Vector2(0f, 1.38f), new Vector2(0.38f, 0.88f), 0.25f, 0.12f, materials["EnemyDark"]);
        AddBox(root, "GunnerSensor", new Vector3(0f, 1.08f, 0.35f), new Vector3(0.13f, 0.16f, 0.08f), materials["EnemyGlow"], Vector3.zero);
        AddBox(root, "GunnerHead", new Vector3(0f, 1.52f, 0.03f), new Vector3(0.3f, 0.28f, 0.34f), materials["EnemyDark"], Vector3.zero);
        AddBox(root, "GunnerVisor", new Vector3(0f, 1.52f, 0.22f), new Vector3(0.22f, 0.055f, 0.05f), materials["EnemyGlow"], Vector3.zero);
        AddBox(root, "GunnerAntenna", new Vector3(-0.12f, 1.79f, 0f), new Vector3(0.035f, 0.38f, 0.035f), materials["EnemyGlow"], new Vector3(0f, 0f, -16f));
        AddBox(root, "GunnerBackpack", new Vector3(0f, 1.05f, -0.31f), new Vector3(0.5f, 0.54f, 0.2f), materials["EnemyDark"], Vector3.zero);
        AddBox(root, "GunnerArmL", new Vector3(-0.43f, 0.86f, 0.14f), new Vector3(0.2f, 0.52f, 0.22f), materials["EnemySteel"], new Vector3(-8f, 0f, -8f));
        AddBox(root, "GunnerArmR", new Vector3(0.43f, 0.86f, 0.18f), new Vector3(0.2f, 0.52f, 0.22f), materials["EnemySteel"], new Vector3(-8f, 0f, 8f));
        AddBox(root, "GunnerRifle", new Vector3(0.48f, 0.82f, 0.62f), new Vector3(0.22f, 0.2f, 1.15f), materials["Enemy"], Vector3.zero);
        AddBox(root, "GunnerBarrel", new Vector3(0.48f, 0.84f, 1.35f), new Vector3(0.1f, 0.1f, 0.55f), materials["EnemyDark"], Vector3.zero);
        AddBox(root, "GunnerMuzzle", new Vector3(0.48f, 0.84f, 1.67f), new Vector3(0.14f, 0.14f, 0.12f), materials["EnemyGlow"], Vector3.zero);
        AddBox(root, "GunnerLegL", new Vector3(-0.2f, 0.29f, 0f), new Vector3(0.25f, 0.58f, 0.27f), materials["EnemyDark"], Vector3.zero);
        AddBox(root, "GunnerLegR", new Vector3(0.2f, 0.29f, 0f), new Vector3(0.25f, 0.58f, 0.27f), materials["EnemyDark"], Vector3.zero);
        AddBox(root, "GunnerFootL", new Vector3(-0.2f, 0.07f, 0.14f), new Vector3(0.32f, 0.14f, 0.46f), materials["EnemySteel"], Vector3.zero);
        AddBox(root, "GunnerFootR", new Vector3(0.2f, 0.07f, 0.14f), new Vector3(0.32f, 0.14f, 0.46f), materials["EnemySteel"], Vector3.zero);
    }

    private static void AddDroneEnemyVisual(Transform root, Dictionary<string, Material> materials)
    {
        GameObject rotor = AddPrimitive(root, "Rotor", PrimitiveType.Cylinder, new Vector3(0f, 0.72f, 0f), new Vector3(0.52f, 0.055f, 0.52f), materials["EnemyDark"], Vector3.zero);
        AddBox(rotor.transform, "RotorBladeA", Vector3.zero, new Vector3(1.75f, 0.07f, 0.1f), materials["EnemySteel"], Vector3.zero);
        AddBox(rotor.transform, "RotorBladeB", Vector3.zero, new Vector3(0.1f, 0.07f, 1.75f), materials["EnemySteel"], Vector3.zero);
        AddPrimitive(root, "DroneHull", PrimitiveType.Sphere, new Vector3(0f, 0.68f, 0f), new Vector3(0.82f, 0.3f, 0.72f), materials["Enemy"], Vector3.zero);
        AddBox(root, "DroneNose", new Vector3(0f, 0.66f, 0.46f), new Vector3(0.34f, 0.2f, 0.28f), materials["EnemyDark"], new Vector3(-12f, 0f, 0f));
        AddBox(root, "DroneCore", new Vector3(0f, 0.68f, 0.62f), new Vector3(0.22f, 0.12f, 0.08f), materials["DroneGlow"], Vector3.zero);
        AddBox(root, "DroneWingL", new Vector3(-0.68f, 0.65f, -0.02f), new Vector3(0.72f, 0.09f, 0.34f), materials["EnemySteel"], new Vector3(0f, 18f, -6f));
        AddBox(root, "DroneWingR", new Vector3(0.68f, 0.65f, -0.02f), new Vector3(0.72f, 0.09f, 0.34f), materials["EnemySteel"], new Vector3(0f, -18f, 6f));
        AddTriangularPrism(root, "DroneTailL", new Vector2(-0.48f, 0.66f), new Vector2(-0.2f, 0.82f), new Vector2(-0.28f, 0.46f), -0.48f, 0.12f, materials["Enemy"]);
        AddTriangularPrism(root, "DroneTailR", new Vector2(0.48f, 0.66f), new Vector2(0.2f, 0.82f), new Vector2(0.28f, 0.46f), -0.48f, 0.12f, materials["Enemy"]);
        AddBox(root, "DroneThruster", new Vector3(0f, 0.66f, -0.48f), new Vector3(0.28f, 0.13f, 0.12f), materials["DroneGlow"], Vector3.zero);
    }

    private static void AddEliteEnemyVisual(Transform root, Dictionary<string, Material> materials)
    {
        AddBox(root, "ElitePelvis", new Vector3(0f, 0.82f, 0f), new Vector3(0.7f, 0.28f, 0.48f), materials["EnemySteel"], Vector3.zero);
        AddBox(root, "EliteTorso", new Vector3(0f, 1.4f, 0f), new Vector3(1.05f, 0.98f, 0.66f), materials["EnemyDark"], Vector3.zero);
        AddTriangularPrism(root, "EliteChestArmor", new Vector2(-0.68f, 1.14f), new Vector2(0f, 1.92f), new Vector2(0.68f, 1.14f), 0.4f, 0.18f, materials["Enemy"]);
        AddBox(root, "EliteCore", new Vector3(0f, 1.43f, 0.55f), new Vector3(0.3f, 0.3f, 0.09f), materials["EliteGlow"], Vector3.zero);
        AddBox(root, "EliteHead", new Vector3(0f, 2.08f, 0.04f), new Vector3(0.5f, 0.38f, 0.48f), materials["EnemySteel"], Vector3.zero);
        AddBox(root, "EliteVisor", new Vector3(0f, 2.08f, 0.31f), new Vector3(0.38f, 0.08f, 0.06f), materials["EliteGlow"], Vector3.zero);
        AddBox(root, "EliteShoulderL", new Vector3(-0.83f, 1.66f, 0f), new Vector3(0.65f, 0.4f, 0.78f), materials["Enemy"], new Vector3(0f, 0f, -10f));
        AddBox(root, "EliteShoulderR", new Vector3(0.83f, 1.66f, 0f), new Vector3(0.65f, 0.4f, 0.78f), materials["Enemy"], new Vector3(0f, 0f, 10f));
        AddBox(root, "EliteCannonL", new Vector3(-0.72f, 1.92f, 0.5f), new Vector3(0.26f, 0.28f, 1.05f), materials["EnemyDark"], Vector3.zero);
        AddBox(root, "EliteCannonR", new Vector3(0.72f, 1.92f, 0.5f), new Vector3(0.26f, 0.28f, 1.05f), materials["EnemyDark"], Vector3.zero);
        AddBox(root, "EliteMuzzleL", new Vector3(-0.72f, 1.92f, 1.08f), new Vector3(0.17f, 0.18f, 0.12f), materials["EliteGlow"], Vector3.zero);
        AddBox(root, "EliteMuzzleR", new Vector3(0.72f, 1.92f, 1.08f), new Vector3(0.17f, 0.18f, 0.12f), materials["EliteGlow"], Vector3.zero);
        AddBox(root, "EliteArmL", new Vector3(-0.86f, 1.06f, 0.05f), new Vector3(0.32f, 0.78f, 0.34f), materials["EnemySteel"], Vector3.zero);
        AddBox(root, "EliteArmR", new Vector3(0.86f, 1.06f, 0.05f), new Vector3(0.32f, 0.78f, 0.34f), materials["EnemySteel"], Vector3.zero);
        AddBox(root, "EliteLegL", new Vector3(-0.34f, 0.4f, 0f), new Vector3(0.4f, 0.8f, 0.42f), materials["EnemyDark"], Vector3.zero);
        AddBox(root, "EliteLegR", new Vector3(0.34f, 0.4f, 0f), new Vector3(0.4f, 0.8f, 0.42f), materials["EnemyDark"], Vector3.zero);
        AddBox(root, "EliteFootL", new Vector3(-0.34f, 0.09f, 0.22f), new Vector3(0.5f, 0.18f, 0.72f), materials["EnemySteel"], Vector3.zero);
        AddBox(root, "EliteFootR", new Vector3(0.34f, 0.09f, 0.22f), new Vector3(0.5f, 0.18f, 0.72f), materials["EnemySteel"], Vector3.zero);
    }

    private static void AddBossEnemyVisual(Transform root, Dictionary<string, Material> materials)
    {
        AddBox(root, "BossPelvis", new Vector3(0f, 1.02f, 0f), new Vector3(1.15f, 0.4f, 0.72f), materials["EnemySteel"], Vector3.zero);
        AddBox(root, "BossTorso", new Vector3(0f, 1.78f, 0f), new Vector3(1.65f, 1.35f, 0.92f), materials["EnemyDark"], Vector3.zero);
        AddTriangularPrism(root, "BossChestArmor", new Vector2(-1.02f, 1.42f), new Vector2(0f, 2.48f), new Vector2(1.02f, 1.42f), 0.55f, 0.22f, materials["Enemy"]);
        AddBox(root, "BossReactor", new Vector3(0f, 1.82f, 0.72f), new Vector3(0.5f, 0.52f, 0.11f), materials["EnemyGlow"], Vector3.zero);
        AddBox(root, "BossReactorFrame", new Vector3(0f, 1.82f, 0.66f), new Vector3(0.76f, 0.72f, 0.09f), materials["EnemySteel"], Vector3.zero);
        AddBox(root, "BossNeck", new Vector3(0f, 2.52f, 0f), new Vector3(0.4f, 0.22f, 0.38f), materials["EnemySteel"], Vector3.zero);
        AddBox(root, "BossHead", new Vector3(0f, 2.82f, 0.08f), new Vector3(0.62f, 0.46f, 0.58f), materials["EnemyDark"], Vector3.zero);
        AddBox(root, "BossVisor", new Vector3(0f, 2.83f, 0.41f), new Vector3(0.5f, 0.09f, 0.07f), materials["EnemyGlow"], Vector3.zero);
        AddBox(root, "BossHornL", new Vector3(-0.3f, 3.12f, 0.05f), new Vector3(0.08f, 0.62f, 0.08f), materials["Enemy"], new Vector3(0f, 0f, -28f));
        AddBox(root, "BossHornR", new Vector3(0.3f, 3.12f, 0.05f), new Vector3(0.08f, 0.62f, 0.08f), materials["Enemy"], new Vector3(0f, 0f, 28f));
        AddBox(root, "BossShoulderL", new Vector3(-1.25f, 2.05f, 0f), new Vector3(0.92f, 0.55f, 1.02f), materials["Enemy"], new Vector3(0f, 0f, -10f));
        AddBox(root, "BossShoulderR", new Vector3(1.25f, 2.05f, 0f), new Vector3(0.92f, 0.55f, 1.02f), materials["Enemy"], new Vector3(0f, 0f, 10f));
        AddBox(root, "BossLauncherL", new Vector3(-1.05f, 2.48f, 0.35f), new Vector3(0.52f, 0.5f, 1.35f), materials["EnemyDark"], Vector3.zero);
        AddBox(root, "BossLauncherR", new Vector3(1.05f, 2.48f, 0.35f), new Vector3(0.52f, 0.5f, 1.35f), materials["EnemyDark"], Vector3.zero);
        AddBox(root, "BossLauncherGlowL", new Vector3(-1.05f, 2.48f, 1.08f), new Vector3(0.3f, 0.28f, 0.12f), materials["EnemyGlow"], Vector3.zero);
        AddBox(root, "BossLauncherGlowR", new Vector3(1.05f, 2.48f, 1.08f), new Vector3(0.3f, 0.28f, 0.12f), materials["EnemyGlow"], Vector3.zero);
        AddBox(root, "BossUpperArmL", new Vector3(-1.3f, 1.38f, 0.06f), new Vector3(0.46f, 0.9f, 0.48f), materials["EnemySteel"], new Vector3(0f, 0f, -4f));
        AddBox(root, "BossUpperArmR", new Vector3(1.3f, 1.38f, 0.06f), new Vector3(0.46f, 0.9f, 0.48f), materials["EnemySteel"], new Vector3(0f, 0f, 4f));
        AddBox(root, "BossForearmL", new Vector3(-1.34f, 0.82f, 0.2f), new Vector3(0.56f, 0.66f, 0.62f), materials["Enemy"], Vector3.zero);
        AddBox(root, "BossForearmR", new Vector3(1.34f, 0.82f, 0.2f), new Vector3(0.56f, 0.66f, 0.62f), materials["Enemy"], Vector3.zero);
        AddBox(root, "BossLegL", new Vector3(-0.48f, 0.48f, 0f), new Vector3(0.58f, 0.95f, 0.55f), materials["EnemyDark"], Vector3.zero);
        AddBox(root, "BossLegR", new Vector3(0.48f, 0.48f, 0f), new Vector3(0.58f, 0.95f, 0.55f), materials["EnemyDark"], Vector3.zero);
        AddBox(root, "BossKneeL", new Vector3(-0.48f, 0.6f, 0.38f), new Vector3(0.5f, 0.35f, 0.24f), materials["Enemy"], Vector3.zero);
        AddBox(root, "BossKneeR", new Vector3(0.48f, 0.6f, 0.38f), new Vector3(0.5f, 0.35f, 0.24f), materials["Enemy"], Vector3.zero);
        AddBox(root, "BossFootL", new Vector3(-0.48f, 0.1f, 0.28f), new Vector3(0.72f, 0.2f, 0.9f), materials["EnemySteel"], Vector3.zero);
        AddBox(root, "BossFootR", new Vector3(0.48f, 0.1f, 0.28f), new Vector3(0.72f, 0.2f, 0.9f), materials["EnemySteel"], Vector3.zero);
        AddBox(root, "BossBackFinL", new Vector3(-0.8f, 1.88f, -0.72f), new Vector3(0.24f, 1.8f, 0.18f), materials["Enemy"], new Vector3(12f, 0f, -18f));
        AddBox(root, "BossBackFinR", new Vector3(0.8f, 1.88f, -0.72f), new Vector3(0.24f, 1.8f, 0.18f), materials["Enemy"], new Vector3(12f, 0f, 18f));
        AddBox(root, "BossThrusterL", new Vector3(-0.4f, 1.45f, -0.62f), new Vector3(0.26f, 0.42f, 0.2f), materials["EnemyGlow"], Vector3.zero);
        AddBox(root, "BossThrusterR", new Vector3(0.4f, 1.45f, -0.62f), new Vector3(0.26f, 0.42f, 0.2f), materials["EnemyGlow"], Vector3.zero);
    }

    private static void AddWorldBox(string name, Vector3 position, Vector3 scale, Material material)
    {
        GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
        box.name = name;
        box.transform.position = position;
        box.transform.localScale = scale;
        SetRendererMaterial(box, material);
        Collider collider = box.GetComponent<Collider>();
        if (collider != null)
        {
            Object.DestroyImmediate(collider);
        }
    }

    private static GameObject AddBox(Transform parent, string name, Vector3 localPosition, Vector3 localScale, Material material, Vector3 localEuler)
    {
        GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
        box.name = name;
        box.transform.SetParent(parent, false);
        box.transform.localPosition = localPosition;
        box.transform.localRotation = Quaternion.Euler(localEuler);
        box.transform.localScale = localScale;
        SetRendererMaterial(box, material);
        Collider collider = box.GetComponent<Collider>();
        if (collider != null)
        {
            Object.DestroyImmediate(collider);
        }

        return box;
    }

    private static GameObject AddPrimitive(Transform parent, string name, PrimitiveType type, Vector3 localPosition, Vector3 localScale, Material material, Vector3 localEuler)
    {
        GameObject primitive = GameObject.CreatePrimitive(type);
        primitive.name = name;
        primitive.transform.SetParent(parent, false);
        primitive.transform.localPosition = localPosition;
        primitive.transform.localRotation = Quaternion.Euler(localEuler);
        primitive.transform.localScale = localScale;
        SetRendererMaterial(primitive, material);
        Collider collider = primitive.GetComponent<Collider>();
        if (collider != null)
        {
            Object.DestroyImmediate(collider);
        }

        return primitive;
    }

    private static GameObject AddTriangularPrism(Transform parent, string name, Vector2 a, Vector2 b, Vector2 c, float zCenter, float thickness, Material material)
    {
        GameObject meshObject = new GameObject(name);
        meshObject.transform.SetParent(parent, false);
        float halfThickness = thickness * 0.5f;
        Vector3[] vertices =
        {
            new Vector3(a.x, a.y, zCenter - halfThickness),
            new Vector3(b.x, b.y, zCenter - halfThickness),
            new Vector3(c.x, c.y, zCenter - halfThickness),
            new Vector3(a.x, a.y, zCenter + halfThickness),
            new Vector3(b.x, b.y, zCenter + halfThickness),
            new Vector3(c.x, c.y, zCenter + halfThickness)
        };
        int[] triangles =
        {
            0, 2, 1,
            3, 4, 5,
            0, 1, 4,
            0, 4, 3,
            1, 2, 5,
            1, 5, 4,
            2, 0, 3,
            2, 3, 5
        };

        Mesh mesh = new Mesh();
        mesh.name = name + "_Mesh";
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        MeshFilter filter = meshObject.AddComponent<MeshFilter>();
        filter.sharedMesh = mesh;
        MeshRenderer renderer = meshObject.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        return meshObject;
    }

    private static void SetRendererMaterial(GameObject target, Material material)
    {
        Renderer renderer = target.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.sharedMaterial = material;
        }
    }

    private static void CreateSocket(Transform hardpoints, string name, Vector3 localPosition)
    {
        Transform socket = new GameObject(name).transform;
        socket.SetParent(hardpoints, false);
        socket.localPosition = localPosition;
        socket.localRotation = Quaternion.identity;
    }

    private static GameObject SavePrefab(GameObject root, string path)
    {
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        return prefab;
    }
}
