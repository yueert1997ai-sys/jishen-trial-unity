using UnityEngine;

// A finite health layer, never posture. The breaking contact is fully absorbed.
[DisallowMultipleComponent]
public sealed class ArmorHealth : MonoBehaviour
{
    public float Maximum {get;private set;}
    public float Current {get;private set;}
    public bool Intact=>Current>0;
    public float Ratio=>Maximum>0?Current/Maximum:0;
    public void Configure(float capacity){Maximum=Mathf.Max(0,capacity);ResetState();}
    public void ResetState(){Current=Maximum;}
    public float Absorb(float amount,DamageInfo info)
    {
        if(!Intact)return amount;
        info.ArmorDamage=Mathf.Min(Current,amount);
        Current=Mathf.Max(0,Current-amount);
        info.BrokeArmor=Current==0;
        return 0;
    }
}
