using UnityEditor;

public static class MobileTravelBuild
{
    // Separate PlayerPrefs namespace as well as MECH_EQUIPMENT_PROFILE for regression Players.
    public static void Build()
    {
        string product = PlayerSettings.productName;
        try
        {
            PlayerSettings.productName = "MECH TRIAL Mobile QA";
            CombatLoopV2Build.Build();
        }
        finally
        {
            PlayerSettings.productName = product;
            AssetDatabase.SaveAssets();
        }
    }
}
