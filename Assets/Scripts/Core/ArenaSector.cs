using UnityEngine;
using UnityEngine.AI;

public class ArenaSector : MonoBehaviour
{
    public GameObject maintenance;
    public GameObject reactor;
    public NavMeshData maintenanceNavigation;
    public NavMeshData reactorNavigation;
    public int CurrentSector { get; private set; } = 1;

    public void ShowSector(int sector)
    {
        CurrentSector = sector <= 1 ? 1 : 2;
        maintenance.SetActive(CurrentSector == 1);
        reactor.SetActive(CurrentSector == 2);
        GetComponent<ArenaNavigation>().SetData(CurrentSector == 1 ? maintenanceNavigation : reactorNavigation);
        Physics.SyncTransforms();
    }
}
