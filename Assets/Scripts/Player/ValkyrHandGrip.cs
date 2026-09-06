using System;
using UnityEngine;

// The palm and each phalanx are rigid meshes from the frozen character, with separate pivots.
public sealed class ValkyrHandGrip : MonoBehaviour
{
    public Vector3 grip, fingerAxis;
    public Transform[] fingers=Array.Empty<Transform>();
    private Quaternion[] closed;
    public float Openness {get;private set;}
    private void Awake(){closed=new Quaternion[fingers.Length];for(int i=0;i<fingers.Length;i++)closed[i]=fingers[i].localRotation;}
    public void Pose(float openness)
    {
        Openness=Mathf.Clamp01(openness);
        for(int i=0;i<fingers.Length;i++)
        {
            var t=fingers[i];float angle=t.name.StartsWith("Thumb")?-27:(t.name.EndsWith("_0")?26:42);
            t.localRotation=Quaternion.AngleAxis(angle*Openness,fingerAxis)*closed[i];
        }
    }
}
