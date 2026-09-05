using System;
using UnityEngine;

public static class GamePreferences
{
    private const string Prefix = "MechTrial.Slice.";
    public static bool Chinese { get; private set; }
    public static float Master { get; private set; } = 1f;
    public static float Music { get; private set; } = 1f;
    public static float Effects { get; private set; } = 1f;
    public static bool Shake { get; private set; } = true;
    public static int Quality { get; private set; } = 1;
    public static event Action LanguageChanged;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetEvents() { LanguageChanged = null; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Load()
    {
        Chinese = PlayerPrefs.GetInt(Prefix + "Chinese", 0) == 1;
        Master = Mathf.Clamp01(PlayerPrefs.GetFloat(Prefix + "Master", 1));
        Music = Mathf.Clamp01(PlayerPrefs.GetFloat(Prefix + "Music", 1));
        Effects = Mathf.Clamp01(PlayerPrefs.GetFloat(Prefix + "Effects", 1));
        Shake = PlayerPrefs.GetInt(Prefix + "Shake", 1) == 1;
        Quality = Mathf.Clamp(PlayerPrefs.GetInt(Prefix + "Quality", 1), 0, 1);
        AudioListener.volume = Master;
        ApplyQuality();
    }

    public static void SetLanguage(bool chinese)
    {
        if (Chinese == chinese) return;
        Chinese = chinese;
        LanguageChanged?.Invoke();
    }

    public static void SetMaster(float value) { Master = Mathf.Clamp01(value); AudioListener.volume = Master; }
    public static void SetMusic(float value) { Music = Mathf.Clamp01(value); if (GameAudio.Instance != null) GameAudio.Instance.RefreshMix(); }
    public static void SetEffects(float value) { Effects = Mathf.Clamp01(value); if (GameAudio.Instance != null) GameAudio.Instance.RefreshMix(); }
    public static void SetShake(bool value) { Shake = value; }
    public static void SetQuality(int value) { Quality = Mathf.Clamp(value, 0, 1); ApplyQuality(); }

    private static void ApplyQuality()
    {
        QualitySettings.antiAliasing = Quality == 1 ? 4 : 2;
        QualitySettings.shadows = Quality == 1 ? ShadowQuality.All : ShadowQuality.HardOnly;
        QualitySettings.shadowResolution = Quality == 1 ? ShadowResolution.Medium : ShadowResolution.Low;
        QualitySettings.shadowDistance = Quality == 1 ? 65f : 35f;
        QualitySettings.lodBias = Quality == 1 ? 1.35f : 0.85f;
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 60;
    }

    public static void Save()
    {
        PlayerPrefs.SetInt(Prefix + "Chinese", Chinese ? 1 : 0);
        PlayerPrefs.SetFloat(Prefix + "Master", Master);
        PlayerPrefs.SetFloat(Prefix + "Music", Music);
        PlayerPrefs.SetFloat(Prefix + "Effects", Effects);
        PlayerPrefs.SetInt(Prefix + "Shake", Shake ? 1 : 0);
        PlayerPrefs.SetInt(Prefix + "Quality", Quality);
        PlayerPrefs.Save();
    }
}
