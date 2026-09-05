using System.IO;
using UnityEditor;

[InitializeOnLoad]
public static class DemoAutomationBridge
{
    private const string SmokeTriggerPath = "Temp/MECH_ROUGE_RUN_SMOKE";

    static DemoAutomationBridge()
    {
        EditorApplication.update -= Poll;
        EditorApplication.update += Poll;
    }

    private static void Poll()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode)
        {
            return;
        }

        if (!File.Exists(SmokeTriggerPath))
        {
            return;
        }

        File.Delete(SmokeTriggerPath);
        DemoPlayModeSmoke.RunFullFlow();
    }
}
