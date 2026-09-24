using UnityEngine;

// Identifies the independently recolored, uniformly fitted VALKYR anti-ship blade.
public sealed class NemesisRaikenBlade : MonoBehaviour
{
    public string sourceBlade;
    public float originalReach, fittedReach;
    public float fullLength, bodyHeight;
    public Transform grip, tip;

    // Preserve the sampled swing azimuth and roll; flatten only downward poses
    // that a longer blade cannot physically reach without entering the floor.
    public Quaternion FitRotation(Quaternion rotation, Vector3 palm, float floor, Vector3 heading)
    {
        Vector3 axis=rotation*Vector3.forward;
        float lowest=Mathf.Clamp((floor+.14f-palm.y)/Vector3.Distance(grip.position,tip.position),-.999f,.999f);
        if(axis.y>=lowest)return rotation;
        var flat=Vector3.ProjectOnPlane(axis,Vector3.up);
        if(flat.sqrMagnitude<.0001f)flat=Vector3.ProjectOnPlane(heading,Vector3.up);
        Vector3 fitted=flat.normalized*Mathf.Sqrt(1-lowest*lowest)+Vector3.up*lowest;
        return Quaternion.FromToRotation(axis,fitted)*rotation;
    }
}
