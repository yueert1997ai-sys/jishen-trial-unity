using UnityEngine;

// J01 has a broader shoulder-led gesture. Timing, damage windows, motor advance
// and cancellation stay shared with the existing fast / fast / heavy chain.
public static class NemesisComboProfile
{
    static ValkyrComboProfile active;
    public static ValkyrComboProfile Active
    {
        get
        {
            if(active!=null)return active;
            active=Object.Instantiate(P0ComboProfile.Active);active.name="NEMESIS broad three-cut chain";
            var ready=ValkyrComboProfile.Ready;
            foreach(var stroke in active.strokes)for(int i=0;i<stroke.keys.Length;i++)
            {
                var k=stroke.keys[i];
                k.wrist=ready.wrist+Vector3.Scale(k.wrist-ready.wrist,new Vector3(1.12f,1.15f,1.15f));
                k.elbow=ready.elbow+(k.elbow-ready.elbow)*1.10f;
                k.hips.y*=1.12f;k.waist.y*=1.13f;k.chest.y*=1.14f;k.chest.z*=1.12f;
                k.pelvis=ready.pelvis+Vector3.Scale(k.pelvis-ready.pelvis,new Vector3(1.12f,1.10f,1.12f));
                k.leftFoot=ready.leftFoot+(k.leftFoot-ready.leftFoot)*1.05f;
                k.rightFoot=ready.rightFoot+(k.rightFoot-ready.rightFoot)*1.05f;
                stroke.keys[i]=k;
            }
            return active;
        }
    }
}
