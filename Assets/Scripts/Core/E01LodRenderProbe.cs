using UnityEngine;

// Attached only by the opt-in standalone audit. Counts real camera submissions, including hidden-window rendering.
public class E01LodRenderProbe : MonoBehaviour
{
    public int draws;
    void OnWillRenderObject()
    {
        if (Camera.current != null && Camera.current.name == "Audit Close Camera") draws++;
    }
}
