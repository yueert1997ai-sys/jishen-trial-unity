using UnityEngine;
using UnityEngine.AI;

public class ArenaNavigation : MonoBehaviour
{
    public NavMeshData data;
    private NavMeshDataInstance instance;
    private void OnEnable() { if (data != null) instance = NavMesh.AddNavMeshData(data); }
    private void OnDisable() { if (instance.valid) instance.Remove(); }
}
