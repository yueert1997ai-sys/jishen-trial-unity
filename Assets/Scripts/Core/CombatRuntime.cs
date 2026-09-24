using System;
using UnityEngine;

public enum CombatMode { FullDemo, ShortCombat }

// Run-only state. Definitions and permanent equipment are owned elsewhere.
public sealed class CombatRunState
{
    public readonly int Generation, Seed;
    public readonly CombatMode Mode;
    public int Encounter { get; internal set; }
    public bool Finished { get; internal set; }
    internal CombatRunState(int generation, int seed, CombatMode mode)
    { Generation=generation;Seed=seed;Mode=mode; }
}

public static class CombatRuntime
{
    public static CombatRunState Run { get; private set; }
    public static int Generation { get; private set; }
    public static int ActionGeneration { get; private set; }
    static int? retrySeed;
    public static void RetryCurrentSeed(){if(Run!=null)retrySeed=Run.Seed;}
    public static float SimulationTime => Time.time;
    public static float PresentationTime => Time.unscaledTime;
    public static CombatMode RequestedMode => HasArgument("-combatSlice") || HasArgument("-combatLab")
        ? CombatMode.ShortCombat : CombatMode.FullDemo;
    public static bool HasArgument(string value) => Array.IndexOf(Environment.GetCommandLineArgs(),value)>=0;
    public static void BeginRun(int seed=0,CombatMode? mode=null)
    {
        InvalidateActions();
        if(seed==0&&retrySeed.HasValue)seed=retrySeed.Value;retrySeed=null;
        if(seed==0)
        {
            var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-combatSeed");
            if(at<0||at+1>=args.Length||!int.TryParse(args[at+1],out seed))seed=Environment.TickCount;
        }
        Run=new CombatRunState(++Generation,seed,mode??RequestedMode);
    }
    public static void InvalidateActions() { ActionGeneration++; }
    public static void EndRun()
    { if(Run!=null)Run.Finished=true;InvalidateActions(); }
    public static bool Owns(int generation) => Run!=null&&!Run.Finished&&Run.Generation==generation;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Reset() { Run=null;Generation=ActionGeneration=0;retrySeed=null; }
}
