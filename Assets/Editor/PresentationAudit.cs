using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class PresentationAudit
{
    public static IEnumerator Scenarios()
    {
        yield return null;
        yield return null;
        bool language = GamePreferences.Chinese, shake = GamePreferences.Shake;
        float master = GamePreferences.Master, music = GamePreferences.Music, effects = GamePreferences.Effects;
        int quality = GamePreferences.Quality;
        try
        {
            var gm = GameManager.Instance;
            GamePreferences.SetLanguage(false);
            GamePreferences.SetMaster(1);
            GamePreferences.SetMusic(1);
            GamePreferences.SetEffects(1);
            var font = RuntimeUIFactory.DefaultFont;
            ProjectAudit.Check(font.name.Contains("Noto"), "bundled_font_loaded");
            foreach (char c in string.Concat(GameText.Translations).Distinct())
                if (!char.IsWhiteSpace(c) && !font.HasCharacter(c)) throw new Exception("Missing localized glyph U+" + ((int)c).ToString("X4"));
            ProjectAudit.Check(font.HasCharacter('\u2699') && font.HasCharacter('\u2713'), "settings_icon_glyphs_available");
            float settle = Time.time + 0.8f;
            while (Time.time < settle) yield return null;
            ProjectAudit.Capture("presentation_01_hangar_en", 1280, 720, validateUI: true);
            ProjectAudit.Click("HangarSettingsButton");
            yield return null;
            ProjectAudit.Check(gm.settingsUI.IsVisible && gm.IsPaused && !gm.CanPlayerControl, "settings_pause_input");
            ProjectAudit.Capture("presentation_02_settings_en", 1280, 720, validateUI: true);
            ProjectAudit.Click("ChineseButton");
            yield return null;
            ProjectAudit.Check(GamePreferences.Chinese && FindText("GameTitle").text == "\u673a\u795e\u8bd5\u70bc", "language_updates_existing_labels");
            ProjectAudit.Capture("presentation_03_settings_zh", 2400, 1080, new Rect(90, 35, 2220, 1045), true);
            ProjectAudit.Capture("presentation_04_tablet_zh", 1024, 768, validateUI: true);
            Slider("MasterSlider").value = 0.37f;
            Slider("MusicSlider").value = 0;
            Slider("EffectsSlider").value = 0;
            GameAudio.Play(GameAudioCue.Beam, 1);
            ProjectAudit.Check(Mathf.Approximately(AudioListener.volume, 0.37f), "master_volume_applied");
            ProjectAudit.Check(GameAudio.Instance.GetComponents<AudioSource>().All(s => s.volume <= 0.001f), "music_and_effects_mute_live_sources");
            var toggle = Object.FindObjectsByType<Toggle>(FindObjectsSortMode.None).Single(t => t.name == "ShakeToggle");
            toggle.isOn = false;
            ProjectAudit.Check(!GamePreferences.Shake, "shake_option_applied");
            ProjectAudit.Click("BalancedButton");
            ProjectAudit.Check(QualitySettings.antiAliasing == 2 && QualitySettings.shadowDistance == 35, "balanced_quality_applied");
            ProjectAudit.Click("HighButton");
            ProjectAudit.Check(QualitySettings.antiAliasing == 4 && Application.targetFrameRate == 60, "high_quality_and_frame_cap_applied");
            ProjectAudit.Click("CreditsButton");
            yield return null;
            ProjectAudit.Capture("presentation_05_credits_zh", 1280, 720, validateUI: true);
            ProjectAudit.Click("CloseSettingsButton");
            ProjectAudit.Check(!gm.IsPaused && !gm.settingsUI.IsVisible, "settings_close_restores_hangar");
            ProjectAudit.Check(Mathf.Approximately(PlayerPrefs.GetFloat("MechTrial.Slice.Master"), 0.37f), "settings_saved_to_playerprefs");
            ProjectAudit.Capture("presentation_06_hangar_zh", 1280, 720, validateUI: true);
            ProjectAudit.Click("DeployButton");
            yield return null;
            gm.stageManager.StopStage();
            yield return null;
            ProjectAudit.Capture("presentation_09_combat_zh", 1280, 720, validateUI: true);
            ProjectAudit.Click("PauseButton");
            yield return null;
            ProjectAudit.Click("PauseSettingsButton");
            yield return null;
            ProjectAudit.Click("CloseSettingsButton");
            ProjectAudit.Check(gm.IsPaused && gm.pauseUI.IsVisible, "settings_close_preserves_existing_pause");
            ProjectAudit.Click("ResumeButton");
            gm.OnEncounterCleared(0);
            yield return null;
            ProjectAudit.Capture("presentation_07_reward_zh", 1280, 720, validateUI: true);
            ProjectAudit.Click("ChooseButton");
            gm.stageManager.StopStage();
            gm.EnterResult(false);
            yield return null;
            ProjectAudit.Capture("presentation_08_result_zh", 1280, 720, validateUI: true);
            ProjectAudit.Click("ReturnHangarButton");
            yield return null;
            yield return null;
            ProjectAudit.Check(FindText("GameTitle").text == "\u673a\u795e\u8bd5\u70bc", "language_survives_scene_restart");
            ProjectAudit.Record("PRESENTATION_PASS UI raycast buttons; synthetic slider/toggle values; no physical phone or listening claim");
        }
        finally
        {
            GamePreferences.SetLanguage(language);
            GamePreferences.SetMaster(master);
            GamePreferences.SetMusic(music);
            GamePreferences.SetEffects(effects);
            GamePreferences.SetShake(shake);
            GamePreferences.SetQuality(quality);
            GamePreferences.Save();
        }
    }

    private static Slider Slider(string name) => Object.FindObjectsByType<Slider>(FindObjectsSortMode.None).Single(s => s.name == name);
    private static Text FindText(string name) => Object.FindObjectsByType<Text>(FindObjectsSortMode.None).First(t => t.name == name);

    public static void CheckTextFits()
    {
        var failures = new System.Collections.Generic.List<string>();
        foreach (var text in Object.FindObjectsByType<Text>(FindObjectsSortMode.None))
        {
            if (!text.isActiveAndEnabled || string.IsNullOrWhiteSpace(text.text)) continue;
            var rect = text.rectTransform.rect;
            if (text.preferredHeight > rect.height + 2)
                failures.Add(text.name + " height=" + text.preferredHeight + " available=" + rect.height + " text=" + text.text);
        }
        if (failures.Count > 0) throw new Exception("UI text overflow: " + string.Join("; ", failures));
    }
}
