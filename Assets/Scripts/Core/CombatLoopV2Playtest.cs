using System;
using System.Collections;
// Existing automation flag routes to the V4 gun/blade/mixed ordinary-input replay.
public static class CombatLoopV2Playtest
{
    public static IEnumerator Run(GameManager gm,PlayerController p,string output,Action<bool,string> check,Action<string> capture)
    {return RebuildPlayChecks.Run(gm,p,output,check,capture);}
}
