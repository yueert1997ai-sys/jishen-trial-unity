using System;
using UnityEngine;

// Opt-in R1 comparison. The local P0 rules remain the baseline for every view.
public static class CombatSliceSettings
{
    public static bool Enabled => CombatRuntime.Run!=null?CombatRuntime.Run.Mode==CombatMode.ShortCombat:Array.IndexOf(Environment.GetCommandLineArgs(), "-combatSlice") >= 0;
    public const int Seed = 9142026;
    public const float TimeLimit = 90f;
    public static int ViewIndex { get; private set; } = InitialView();
    public static float Size => ViewIndex == 0 ? 9f : ViewIndex == 1 ? 10.35f : 11.25f;
    public static string ViewLabel => ViewIndex == 0 ? "A · 当前视野" : ViewIndex == 1 ? "B · 视野 +15%" : "C · 视野 +25%";
    static int InitialView()
    {
        var args = Environment.GetCommandLineArgs();
        int i = Array.IndexOf(args, "-sliceView");
        return i >= 0 && i + 1 < args.Length ? (args[i + 1] == "C" ? 2 : args[i + 1] == "B" ? 1 : 0) : 2;
    }
    public static void SelectView(int index) { ViewIndex = Mathf.Clamp(index, 0, 2); }
}
