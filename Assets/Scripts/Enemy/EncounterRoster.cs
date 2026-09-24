using System;
using System.Collections.Generic;
using UnityEngine;

public enum EnemyWeapon { M7, M14, Type08, Ax01, Rocket, Missiles, BackCannon }
public readonly struct EnemySpawnSpec
{
    public readonly string body;public readonly EnemyWeapon weapon;public readonly EnemyKind role;public readonly bool closeAssault;
    public bool Heavy=>weapon==EnemyWeapon.Type08||weapon==EnemyWeapon.Ax01||weapon==EnemyWeapon.BackCannon;
    public EnemySpawnSpec(string body,EnemyWeapon weapon,EnemyKind role,bool closeAssault=false){this.body=body;this.weapon=weapon;this.role=role;this.closeAssault=closeAssault;}
    public override string ToString()=>body+"/"+weapon+"/"+role+(closeAssault?"/CloseAssault":"");
}
// Independent seeded deck: particle count and frame rate never advance encounter randomness.
public sealed class EncounterRoster
{
    readonly List<EnemySpawnSpec[]> groups=new List<EnemySpawnSpec[]>();readonly Vector3[] entries;
    public EnemySpawnSpec[] Group(int index)=>groups[index];
    public Vector3 Entry(int index)=>entries[index];
    public EncounterRoster(int seed,int room,IReadOnlyList<CombatRules.Beat> beats)
    {
        var random=new System.Random(unchecked(seed*397^(room+2)*7919));
        var deck=new[]{EnemyWeapon.M7,EnemyWeapon.M14,EnemyWeapon.Rocket,EnemyWeapon.Type08,EnemyWeapon.Missiles,EnemyWeapon.Ax01,EnemyWeapon.BackCannon};
        for(int i=deck.Length-1;i>0;i--){int j=random.Next(i+1);var v=deck[i];deck[i]=deck[j];deck[j]=v;}
        entries=new Vector3[beats.Count];int slot=0;
        for(int g=0;g<beats.Count;g++)
        {
            var rows=new EnemySpawnSpec[beats[g].Roles.Count];
            for(int i=0;i<rows.Length;i++)
            {
                var role=beats[g].Roles[i];if(role==EnemyKind.Drone){rows[i]=new EnemySpawnSpec("DRONE",EnemyWeapon.M7,role);continue;}
                var weapon=deck[slot++%deck.Length];
                string body=weapon==EnemyWeapon.BackCannon?"GUNCANNON":weapon==EnemyWeapon.Rocket||weapon==EnemyWeapon.Missiles?"ZAKU":weapon==EnemyWeapon.Ax01?"DOM":slot%3==0?"E01":"GM";
                if(weapon==EnemyWeapon.Type08)body=slot%2==0?"E01":"DOM";
                if(weapon==EnemyWeapon.M7)body=(slot/7)%2==0?"E01":"GM";
                if(weapon==EnemyWeapon.M14)body=(slot/7)%2==0?"GM":"E01";
                // Keep a close-combat front; its rocket opens the approach before the lunge.
                bool closeAssault=weapon==EnemyWeapon.Rocket||role==EnemyKind.Melee&&weapon==EnemyWeapon.M7;
                // Preserve the published health/armor class while varying approach and attack decisions.
                if(role==EnemyKind.Melee&&weapon!=EnemyWeapon.Rocket)role=EnemyKind.Ranged;
                rows[i]=new EnemySpawnSpec(body,weapon,role,closeAssault);
            }
            groups.Add(rows);float angle=(random.Next(8)*45+22.5f)*Mathf.Deg2Rad;
            entries[g]=new Vector3(Mathf.Cos(angle)*21,0,Mathf.Sin(angle)*21);
        }
    }
}
