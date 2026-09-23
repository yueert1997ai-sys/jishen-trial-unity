using System;
using System.Collections.Generic;
using UnityEngine;

// Authored room composition; runtime progress stays in EncounterFlow.
public static class EncounterCatalog
{
    [Serializable] sealed class Document { public int version;public RoomData[] rooms; }
    [Serializable] sealed class RoomData {public string title;public Wave[] waves;}
    [Serializable] sealed class Wave {public int entry;public EnemyKind[] roles;}
    public sealed class Room
    {
        public readonly string Title;
        public readonly IReadOnlyList<CombatRules.Beat> Beats;
        public Room(string title,CombatRules.Beat[] beats){Title=title;Beats=Array.AsReadOnly(beats);}
    }
    static IReadOnlyList<Room> rooms;
    public static IReadOnlyList<Room> Rooms=>rooms??(rooms=Load());
    static IReadOnlyList<Room> Load()
    {
        var asset=Resources.Load<TextAsset>("Foundation/MissionEncounters");
        var data=asset!=null?JsonUtility.FromJson<Document>(asset.text):null;
        if(data==null||data.version!=1||data.rooms==null||data.rooms.Length!=6)throw new InvalidOperationException("Six authored mission encounters required");
        var result=new Room[6];Vector3[] entries={new Vector3(-11,0,5),new Vector3(0,0,12),new Vector3(11,0,5),new Vector3(0,0,-11)};
        for(int r=0;r<6;r++)
        {
            var room=data.rooms[r];if(room.waves==null||room.waves.Length==0)throw new InvalidOperationException("Empty mission encounter");
            var beats=new CombatRules.Beat[room.waves.Length];
            for(int i=0;i<beats.Length;i++)
            {
                var wave=room.waves[i];
                if(wave.entry<0||wave.entry>3||wave.roles==null||wave.roles.Length==0||wave.roles.Length>CombatRules.Current.MaxHostiles)throw new InvalidOperationException("Invalid mission wave");
                foreach(var role in wave.roles)if(!Enum.IsDefined(typeof(EnemyKind),role))throw new InvalidOperationException("Invalid mission role");
                beats[i]=new CombatRules.Beat("room"+r+"-"+i,entries[wave.entry],wave.roles,null);
            }
            result[r]=new Room(room.title,beats);
        }
        return Array.AsReadOnly(result);
    }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]static void Reset(){rooms=null;}
}
