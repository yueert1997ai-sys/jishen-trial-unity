using UnityEngine;

public static class MobilePlatform
{
    // The desktop override is available only to an explicitly requested headless regression run.
    static readonly bool touchCheck = Application.isBatchMode && CombatRuntime.HasArgument("-mobileTravelCheck");
    public static bool UsesTouch => Application.isMobilePlatform || touchCheck;
}
