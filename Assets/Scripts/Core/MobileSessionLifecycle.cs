using UnityEngine;

public sealed class MobileSessionLifecycle : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Install()
    {
        if (!MobilePlatform.UsesTouch) return;
        var host = new GameObject("MobileSessionLifecycle");
        DontDestroyOnLoad(host);
        host.AddComponent<MobileSessionLifecycle>();
    }

    void Update()
    {
        int desired = GameManager.Instance != null && GameManager.Instance.IsCombatActive
            ? SleepTimeout.NeverSleep : SleepTimeout.SystemSetting;
        if (Screen.sleepTimeout != desired) Screen.sleepTimeout = desired;
    }

    void OnApplicationPause(bool paused) { if (paused) Suspend(); }
    void OnApplicationFocus(bool focused) { if (!focused) Suspend(); }
    void OnApplicationQuit() { GamePreferences.Save(); }
    void OnDestroy() { Screen.sleepTimeout = SleepTimeout.SystemSetting; }

    void Suspend()
    {
        GamePreferences.Save();
        var gm = GameManager.Instance;
        if (gm != null)
        {
            gm.playerController?.InputRouter.Clear();
            gm.SetPaused(true);
        }
        Screen.sleepTimeout = SleepTimeout.SystemSetting;
    }
}
