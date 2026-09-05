using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

public static class PackageInstallUtility
{
    private static AddRequest request;

    [MenuItem("MECH ROUGE/Install glTFast")]
    public static void InstallGltfFast()
    {
        if (request != null && !request.IsCompleted)
        {
            Debug.Log("MECH ROUGE: glTFast install is already running.");
            return;
        }

        Debug.Log("MECH ROUGE: Installing com.unity.cloud.gltfast...");
        request = Client.Add("com.unity.cloud.gltfast");
        EditorApplication.update += WaitForInstall;
    }

    private static void WaitForInstall()
    {
        if (request == null || !request.IsCompleted)
        {
            return;
        }

        EditorApplication.update -= WaitForInstall;
        if (request.Status == StatusCode.Success)
        {
            Debug.Log("MECH ROUGE: Installed " + request.Result.packageId);
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(0);
            }
        }
        else
        {
            Debug.LogError("MECH ROUGE: glTFast install failed: " + request.Error.message);
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(1);
            }
        }
    }
}
