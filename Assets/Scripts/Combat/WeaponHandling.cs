using System;
using System.Collections.Generic;
using UnityEngine;

// Player weapon distances are metres along the combat-plane flight path, including the barrel.
// Definitions are copied into immutable shot snapshots; equipment changes cannot alter flying rounds.
public readonly struct WeaponHandlingProfile
{
    public readonly int Capacity;
    public readonly float Recovery, OptimalRange, FalloffRange, MaximumRange, MinimumDamage;
    public bool HasRange => MaximumRange > 0;
    public WeaponHandlingProfile(int capacity,float recovery,float optimal,float falloff,float maximum,float minimum)
    {Capacity=capacity;Recovery=recovery;OptimalRange=optimal;FalloffRange=falloff;MaximumRange=maximum;MinimumDamage=minimum;}
    public float DamageScale(float distance)
    {
        if(!HasRange)return 1;
        if(distance>MaximumRange+.001f)return 0;
        return Mathf.Lerp(1,MinimumDamage,Mathf.SmoothStep(0,1,Mathf.InverseLerp(OptimalRange,FalloffRange,distance)));
    }
}

public static class WeaponHandling
{
    [Serializable] sealed class Entry
    {
        public string weapon;
        public int capacity;
        public float recovery,optimalRange,falloffRange,maximumRange,minimumDamage;
    }
    [Serializable] sealed class Definition { public int version;public Entry[] weapons; }
    static Dictionary<PrimaryWeapon,WeaponHandlingProfile> profiles;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Reset(){profiles=null;}
    public static WeaponHandlingProfile For(PrimaryWeapon weapon)
    {
        if(profiles==null)
        {
            var asset=Resources.Load<TextAsset>("Foundation/WeaponHandling");
            if(asset==null)throw new InvalidOperationException("Missing Foundation/WeaponHandling");
            var definition=JsonUtility.FromJson<Definition>(asset.text);
            if(definition==null||definition.version!=1||definition.weapons==null)
                throw new InvalidOperationException("Invalid weapon handling definition");
            profiles=new Dictionary<PrimaryWeapon,WeaponHandlingProfile>();
            foreach(var row in definition.weapons)
            {
                if(!Enum.TryParse(row.weapon,out PrimaryWeapon id)||profiles.ContainsKey(id)||row.capacity<1||
                    !FinitePositive(row.recovery)||!FinitePositive(row.optimalRange)||!FinitePositive(row.falloffRange)||
                    !FinitePositive(row.maximumRange)||!FinitePositive(row.minimumDamage)||row.minimumDamage>1||
                    row.falloffRange<=row.optimalRange||row.maximumRange<row.falloffRange)
                    throw new InvalidOperationException("Invalid weapon handling: "+row.weapon);
                profiles.Add(id,new WeaponHandlingProfile(row.capacity,row.recovery,row.optimalRange,row.falloffRange,row.maximumRange,row.minimumDamage));
            }
        }
        return profiles.TryGetValue(weapon,out var profile)?profile:default;
    }
    static bool FinitePositive(float value)=>value>0&&!float.IsInfinity(value)&&!float.IsNaN(value);
}
