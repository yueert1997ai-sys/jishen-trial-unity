using System.Collections;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public enum HeroMech { Nemesis, Valkyr }

public class PlayerMechLoader : MonoBehaviour
{
    public GameObject defaultMechPrefab;
    public string editorCustomPrefabPath = "Assets/UserContent/PlayerMech/PlayerMech.prefab";

    private MechHardpointManager hardpoints;
    public HeroMech SelectedHero {get;private set;}=HeroMech.Nemesis;
    public bool SelectionBusy {get;private set;}
    public GameObject CurrentVisual {get;private set;}

    // Selection belongs to this play session. A fresh launch still starts with J-01.
    public bool SelectHero(HeroMech hero)
    {
        var gm=GameManager.Instance;
        if(gm==null||gm.Phase!=GamePhase.Hangar||gm.IsPaused||SelectionBusy)return false;
        if(hero==SelectedHero)return true;
        var armory=Resources.Load<HangarArmory>("Hangar/Armory");
        if(armory==null||(hero==HeroMech.Valkyr?armory.valkyrPrefab:armory.heroPrefab)==null)return false;
        SelectionBusy=true;SelectedHero=hero;
        var player=GetComponent<PlayerController>();player.CancelMovement();player.Melee.ResetCooldown();player.Stance.ResetStance();
        player.weaponController.ResetCooldowns();LoadVisualModel();StartCoroutine(RebindPresentation());return true;
    }
    IEnumerator RebindPresentation()
    {
        // Old subscriptions finish OnDestroy and the replacement completes Start first.
        yield return null;
        GetComponent<MechDashPresentation>()?.RebindVisuals();
        GetComponent<CombatFeedback>()?.RefreshVisuals();
        SelectionBusy=false;
        GetComponent<PlayerLoadout>().EnterHangar();
    }

    private void Awake()
    {
        var groundMarker=transform.Find("PlayerGroundMarker");
        if(groundMarker!=null)groundMarker.gameObject.SetActive(false);
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
        GetComponent<PlayerStats>()?.ConfigureHero(SelectedHero);
        var motor=GetComponent<PlayerController>();var profile=MechMovementProfile.For(SelectedHero);
        if(motor!=null){motor.acceleration=profile.acceleration;motor.braking=profile.braking;motor.dashDuration=profile.dashSeconds;}

        Transform visualRoot = hardpoints.visualRoot;
        for (int i = visualRoot.childCount - 1; i >= 0; i--)
        {
            var old=visualRoot.GetChild(i).gameObject;old.SetActive(false);Destroy(old);
        }

        GameObject selectedPrefab = defaultMechPrefab;
        var armory = Resources.Load<HangarArmory>("Hangar/Armory");
        if (armory != null && armory.heroPrefab != null) selectedPrefab = SelectedHero==HeroMech.Valkyr?armory.valkyrPrefab:armory.heroPrefab;
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
        CurrentVisual=visual;
        visual.name = usingCustomPrefab ? "CustomMechModel" : "DefaultMechModel";
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = Quaternion.identity;
        if(SelectedHero==HeroMech.Nemesis)MechEnergyPalette.Apply(visual.transform);
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
