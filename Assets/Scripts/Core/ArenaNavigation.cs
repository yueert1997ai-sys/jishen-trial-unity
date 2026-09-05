using UnityEngine;
using UnityEngine.AI;

public class ArenaNavigation : MonoBehaviour
{
    public NavMeshData data;
    private NavMeshDataInstance instance;
    private void OnEnable() { if (data != null) instance = NavMesh.AddNavMeshData(data); }
    private void OnDisable() { if (instance.valid) instance.Remove(); }
    public void SetData(NavMeshData value)
    {
        if (data == value && instance.valid) return;
        if (instance.valid) instance.Remove();
        data = value;
        if (isActiveAndEnabled && data != null) instance = NavMesh.AddNavMeshData(data);
    }
}
