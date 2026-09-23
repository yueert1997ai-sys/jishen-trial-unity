using UnityEngine;
using UnityEngine.AI;

public class ArenaSector : MonoBehaviour
{
    public GameObject maintenance;
    public GameObject reactor;
    public GameObject lunar;
    public NavMeshData lunarNavigation;
    public GameObject commonDeck;
    public NavMeshData maintenanceNavigation;
    public NavMeshData reactorNavigation;
    public GameObject tacticalMaintenance, tacticalReactor;
    public NavMeshData tacticalMaintenanceNavigation, tacticalReactorNavigation;
    public int CurrentSector { get; private set; } = 1;
    public LunarArenaLayout ActiveLunarLayout => (CurrentSector==1?tacticalMaintenance:tacticalReactor)?.GetComponent<LunarArenaLayout>();
    public Vector3 PlayerEntry => ActiveLunarLayout!=null?ActiveLunarLayout.playerEntry:new Vector3(0,.1f,-4);

    public void ShowSector(int sector)
    {
        CurrentSector = sector <= 1 ? 1 : 2;
        if (tacticalMaintenance != null && tacticalReactor != null)
        {
            maintenance.SetActive(false); reactor.SetActive(false);
            if (lunar != null) lunar.SetActive(false);
            if (commonDeck != null) commonDeck.SetActive(false);
            tacticalMaintenance.SetActive(CurrentSector == 1);
            tacticalReactor.SetActive(CurrentSector == 2);
            GetComponent<ArenaNavigation>().SetData(CurrentSector == 1 ? tacticalMaintenanceNavigation : tacticalReactorNavigation);
            Physics.SyncTransforms();
            return;
        }
        if (commonDeck != null) commonDeck.SetActive(CurrentSector != 1 || lunar == null);
        maintenance.SetActive(CurrentSector == 1 && lunar == null);
        if (lunar != null) lunar.SetActive(CurrentSector == 1);
        reactor.SetActive(CurrentSector == 2);
        GetComponent<ArenaNavigation>().SetData(CurrentSector == 1 ? (lunar != null ? lunarNavigation : maintenanceNavigation) : reactorNavigation);
        Physics.SyncTransforms();
    }
}
