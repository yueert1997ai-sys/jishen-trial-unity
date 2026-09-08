using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class PlayerMechLoader : MonoBehaviour
{
    public GameObject defaultMechPrefab;
    public string editorCustomPrefabPath = "Assets/UserContent/PlayerMech/PlayerMech.prefab";

    private MechHardpointManager hardpoints;

    private void Awake()
    {
        hardpoints = GetComponent<MechHardpointManager>();
        if (hardpoints == null)
        {
            hardpoints = gameObject.AddComponent<MechHardpointManager>();
        }

        LoadVisualModel();
    }

    public void LoadVisualModel()
    {
        hardpoints.EnsureDefaultHardpoints();

        Transform visualRoot = hardpoints.visualRoot;
        for (int i = visualRoot.childCount - 1; i >= 0; i--)
        {
            Destroy(visualRoot.GetChild(i).gameObject);
        }

        GameObject selectedPrefab = defaultMechPrefab;
        var armory = Resources.Load<HangarArmory>("Hangar/Armory");
        if (armory != null && armory.heroPrefab != null) selectedPrefab = armory.heroPrefab;
        bool usingCustomPrefab = false;

#if UNITY_EDITOR
        GameObject customPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(editorCustomPrefabPath);
        if (customPrefab != null)
        {
            selectedPrefab = customPrefab;
            usingCustomPrefab = true;
        }
#endif

        if (selectedPrefab == null)
        {
            CreateEmergencyVisual(visualRoot);
            return;
        }

        GameObject visual = Instantiate(selectedPrefab, visualRoot);
        visual.name = usingCustomPrefab ? "CustomMechModel" : "DefaultMechModel";
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = Quaternion.identity;
    }

    private static void CreateEmergencyVisual(Transform parent)
    {
        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
        body.name = "EmergencyMechBody";
        body.transform.SetParent(parent, false);
        body.transform.localPosition = new Vector3(0f, 1f, 0f);
        body.transform.localScale = new Vector3(0.9f, 1.3f, 0.55f);
    }
}
