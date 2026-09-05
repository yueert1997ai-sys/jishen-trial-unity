using UnityEngine;

[System.Serializable]
public struct EncounterBeat
{
    public float at;
    public EnemyKind kind;
    public int count;
    public int entry;
}

[CreateAssetMenu(menuName = "Mech Trial/Encounter")]
public class EncounterDefinition : ScriptableObject
{
    public string title = "Maintenance deck";
    public int maxAlive = 10;
    public EncounterBeat[] beats;
}
