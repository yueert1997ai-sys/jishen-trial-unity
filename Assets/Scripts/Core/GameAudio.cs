using System.Collections.Generic;
using UnityEngine;

public enum GameAudioCue { Beam, Missile, Hit, Death, Dash, Warning, Wave, Reward, Victory, Defeat, Slash, SwordWindup, SwordRegrip, SwordCut1, SwordCut2, SwordCut3, SwordHit, SwordHitHeavy }

[DisallowMultipleComponent]
public class GameAudio : MonoBehaviour
{
    public static GameAudio Instance { get; private set; }
    private readonly Dictionary<GameAudioCue, AudioClip[]> clips = new Dictionary<GameAudioCue, AudioClip[]>();
    private readonly float[] lastCueTime = new float[System.Enum.GetValues(typeof(GameAudioCue)).Length];
    private AudioSource[] voices;
    private readonly float[] voiceGains = new float[22];
    private readonly GameAudioCue[] voiceCues = new GameAudioCue[22];
    private AudioSource ambience;
    private readonly AudioSource[] music = new AudioSource[2];
    private int activeMusic;
    private float crossfadeRemaining;
    private float musicLevel;
    public bool MusicLoaded => music[0] != null && music[0].clip != null;
    private float duckUntil;
    private int weaponVoice, impactVoice, priorityVoice,swordVoice;
    public static event System.Action<GameAudioCue> CuePlayed;
    public int LoadedCueCount => clips.Count;

    private void Awake()
    {
        Instance = this;
        voices = new AudioSource[22];
        for (int i = 0; i < voices.Length; i++)
        {
            voices[i] = gameObject.AddComponent<AudioSource>();
            voices[i].playOnAwake = false;
            voices[i].spatialBlend = 0;
            voices[i].dopplerLevel = 0;
            lastCueTime[i % lastCueTime.Length] = -100;
        }
        Load(GameAudioCue.Beam, "laserLarge_001", "laserLarge_002");
        Load(GameAudioCue.Missile, "thrusterFire_002");
        Load(GameAudioCue.Hit, "impactMetal_heavy_000", "impactMetal_heavy_001", "impactMetal_medium_000");
        Load(GameAudioCue.Death, "explosionCrunch_000", "explosionCrunch_002");
        Load(GameAudioCue.Dash, "thrusterFire_000");
        Load(GameAudioCue.Slash, "forceField_001");
        Load(GameAudioCue.SwordWindup,"RaikenV7/servo_load");
        Load(GameAudioCue.SwordRegrip,"RaikenV7/grip_lock");
        Load(GameAudioCue.SwordCut1,"RaikenV7/cut_reverse");
        Load(GameAudioCue.SwordCut2,"RaikenV7/cut_return");
        Load(GameAudioCue.SwordCut3,"RaikenV7/cut_heavy");
        Load(GameAudioCue.SwordHit,"RaikenV7/armor_cut_01","RaikenV7/armor_cut_02");
        Load(GameAudioCue.SwordHitHeavy,"RaikenV7/armor_break_heavy");
        Load(GameAudioCue.Warning, "computerNoise_000");
        Load(GameAudioCue.Wave, "jingles_HIT00");
        Load(GameAudioCue.Reward, "jingles_HIT04");
        Load(GameAudioCue.Victory, "jingles_HIT08");
        Load(GameAudioCue.Defeat, "jingles_HIT13");
        ambience = gameObject.AddComponent<AudioSource>();
        ambience.clip = Resources.Load<AudioClip>("Audio/Combat/spaceEngineLow_000");
        ambience.loop = true;
        ambience.volume = 0.018f * GamePreferences.Effects;
        ambience.Play();
        var score = Resources.Load<AudioClip>("Audio/Music/Subspace_Loop");
        for (int i = 0; i < music.Length; i++)
        {
            music[i] = gameObject.AddComponent<AudioSource>();
            music[i].clip = score;
            music[i].playOnAwake = false;
            music[i].volume = 0;
        }
        if (score != null) music[0].Play();
        else Debug.LogError("Missing Subspace music.");
    }

    private void Load(GameAudioCue cue, params string[] names)
    {
        var loaded = new List<AudioClip>();
        foreach (string name in names)
        {
            var clip = Resources.Load<AudioClip>("Audio/Combat/" + name);
            if (clip != null) loaded.Add(clip);
            else Debug.LogError("Missing combat audio: " + name);
        }
        clips[cue] = loaded.ToArray();
    }

    private void Update()
    {
        float level = (Time.unscaledTime < duckUntil ? 0.006f : 0.018f) * GamePreferences.Effects;
        ambience.volume = Mathf.MoveTowards(ambience.volume, level, Time.unscaledDeltaTime * 0.08f);
        var gm = GameManager.Instance;
        float targetLevel = gm != null && gm.Phase == GamePhase.Combat ? 0.17f : 0.07f;
        if (Time.unscaledTime < duckUntil) targetLevel *= 0.4f;
        musicLevel = Mathf.MoveTowards(musicLevel, targetLevel, Time.unscaledDeltaTime * 0.2f);
        if (!MusicLoaded || (gm != null && gm.IsPaused)) return;
        var active = music[activeMusic];
        if (crossfadeRemaining <= 0 && active.clip.length - active.time < 2.5f)
        {
            activeMusic = 1 - activeMusic;
            music[activeMusic].time = 0;
            music[activeMusic].Play();
            crossfadeRemaining = 2.5f;
        }
        crossfadeRemaining = Mathf.Max(0, crossfadeRemaining - Time.unscaledDeltaTime);
        float fade = crossfadeRemaining / 2.5f;
        ApplyMusicGain(fade);
    }

    private void ApplyMusicGain(float fade)
    {
        music[activeMusic].volume = musicLevel * (1 - fade) * GamePreferences.Music;
        music[1 - activeMusic].volume = musicLevel * fade * GamePreferences.Music;
    }

    public void RefreshMix()
    {
        if (voices == null) return;
        for (int i = 0; i < voices.Length; i++) voices[i].volume = voiceGains[i] * GamePreferences.Effects;
        if (MusicLoaded) ApplyMusicGain(crossfadeRemaining / 2.5f);
        ambience.volume = (Time.unscaledTime < duckUntil ? 0.006f : 0.018f) * GamePreferences.Effects;
    }

    private void OnDestroy() { if (Instance == this){Instance = null;CuePlayed=null;} }
    public static void StopSwordPreparation()
    {
        if(Instance==null)return;
        for(int i=14;i<Instance.voices.Length;i++)if(Instance.voiceCues[i]==GameAudioCue.SwordWindup||Instance.voiceCues[i]==GameAudioCue.SwordRegrip)Instance.voices[i].Stop();
    }

    public static void Play(GameAudioCue cue, float volume = 1f, float pitch = 1f)
    {
        if (Instance != null) Instance.PlayInternal(cue, volume, pitch);
    }

    private void PlayInternal(GameAudioCue cue, float volume, float pitch)
    {
        if (!clips.TryGetValue(cue, out var options) || options.Length == 0) return;
        bool sword=cue>=GameAudioCue.SwordWindup;
        bool priority = cue >= GameAudioCue.Warning && cue <= GameAudioCue.Defeat;
        float spacing = priority ? 0.18f : cue == GameAudioCue.Hit ? 0.055f : cue>=GameAudioCue.SwordHit?.075f:.035f;
        if (Time.unscaledTime - lastCueTime[(int)cue] < spacing) return;
        lastCueTime[(int)cue] = Time.unscaledTime;
        // Reserve voices for telegraphs and results so rapid fire cannot cut them off.
        int index = sword ? 14+swordVoice++%8 : priority ? 10 + priorityVoice++ % 4
            : cue == GameAudioCue.Beam || cue == GameAudioCue.Missile ? weaponVoice++ % 6 : 6 + impactVoice++ % 4;
        var voice = voices[index];
        voice.Stop();
        voice.clip = options[Random.Range(0, options.Length)];
        voiceCues[index]=cue;
        voiceGains[index] = Mathf.Clamp01(volume) * (sword?.72f:cue == GameAudioCue.Beam ? 0.42f : 0.72f);
        voice.volume = voiceGains[index] * GamePreferences.Effects;
        voice.pitch = Mathf.Clamp(pitch, 0.8f, 1.2f);
        voice.Play();
        CuePlayed?.Invoke(cue);
        if (priority) duckUntil = Time.unscaledTime + 1.2f;
        else if(cue>=GameAudioCue.SwordHit)duckUntil=Mathf.Max(duckUntil,Time.unscaledTime+(cue==GameAudioCue.SwordHitHeavy?.24f:.13f));
    }
}
