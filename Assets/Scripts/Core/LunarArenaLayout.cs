using UnityEngine;

// Authored room information shared by entry staging, camera and traversal checks.
// Scene geometry owns collision; this metadata never changes combat statistics.
public sealed class LunarArenaLayout : MonoBehaviour
{
    public string roomName;
    public Vector3 playerEntry = new Vector3(0,.1f,-9);
    public Vector3[] entries;
    public Vector3[] landmarks;
    public Vector3 coverNear, coverFar;
    public float cameraPitch = 62f;

    public Vector3 SelectEntry(Vector3 intended, Vector3 player)
    {
        if(entries==null||entries.Length==0)return intended;
        float best=float.NegativeInfinity;Vector3 result=entries[0];
        foreach(var point in entries)
        {
            float separation=Vector3.Distance(point,player);
            float score=Vector3.Dot(point.normalized,intended.normalized)*6f
                +Mathf.Min(separation,12)*.2f-(separation<7?20:0);
            if(score>best){best=score;result=point;}
        }
        return result;
    }
}
