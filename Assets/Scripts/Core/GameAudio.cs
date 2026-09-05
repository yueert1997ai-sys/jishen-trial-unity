using System.Collections.Generic;
using UnityEngine;

public enum GameAudioCue { Beam, Missile, Hit, Death, Dash, Warning, Wave, Reward, Victory, Defeat }

[DisallowMultipleComponent]
public class GameAudio : MonoBehaviour
{
    public static GameAudio Instance { get; private set; }
    private readonly Dictionary<GameAudioCue, AudioClip[]> clips = new Dictionary<GameAudioCue, AudioClip[]>();
    private readonly float[] lastCueTime = new float[10];
    private AudioSource[] voices;
    private AudioSource ambience;
    private readonly AudioSource[] music = new AudioSource[2];
    private int activeMusic;
    private float crossfadeRemaining;
    private float musicLevel;
    public bool MusicLoaded => music[0] != null && music[0].clip != null;
    private float duckUntil;
    private int weaponVoice, impactVoice, priorityVoice;
    public int LoadedCueCount => clips.Count;

    private void Awake()
    {
        Instance = this;
        voices = new AudioSource[14];
        for (int i = 0; i < voices.Length; i++)
        {
            voices[i] = gameObject.AddComponent<AudioSource>();
            voices[i].playOnAwake = false;
            voices[i].spatialBlend = 0;
            voices[i].dopplerLevel = 0;
            lastCueTime[i % 10] = -100;
        }
        Load(GameAudioCue.Beam, "laserLarge_001", "laserLarge_002");
        Load(GameAudioCue.Missile, "thrusterFire_002");
        Load(GameAudioCue.Hit, "impactMetal_heavy_000", "impactMetal_heavy_001", "impactMetal_medium_000");
        Load(GameAudioCue.Death, "explosionCrunch_000", "explosionCrunch_002");
        Load(GameAudioCue.Dash, "thrusterFire_000");
        Load(GameAudioCue.Warning, "computerNoise_000");
        Load(GameAudioCue.Wave, "jingles_HIT00");
        Load(GameAudioCue.Reward, "jingles_HIT04");
        Load(GameAudioCue.Victory, "jingles_HIT08");
        Load(GameAudioCue.Defeat, "jingles_HIT13");
        ambience = gameObject.AddComponent<AudioSource>();
        ambience.clip = Resources.Load<AudioClip>("Audio/Combat/spaceEngineLow_000");
        ambience.loop = true;
        ambience.volume = 0.018f;
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
        float level = Time.unscaledTime < duckUntil ? 0.006f : 0.018f;
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
        music[activeMusic].volume = musicLevel * (1 - fade);
        music[1 - activeMusic].volume = musicLevel * fade;
    }

    private void OnDestroy() { if (Instance == this) Instance = null; }

    public static void Play(GameAudioCue cue, float volume = 1f, float pitch = 1f)
    {
        if (Instance != null) Instance.PlayInternal(cue, volume, pitch);
    }

    private void PlayInternal(GameAudioCue cue, float volume, float pitch)
    {
        if (!clips.TryGetValue(cue, out var options) || options.Length == 0) return;
        bool priority = cue >= GameAudioCue.Warning;
        float spacing = priority ? 0.18f : cue == GameAudioCue.Hit ? 0.055f : 0.035f;
        if (Time.unscaledTime - lastCueTime[(int)cue] < spacing) return;
        lastCueTime[(int)cue] = Time.unscaledTime;
        // Reserve voices for telegraphs and results so rapid fire cannot cut them off.
        int index = priority ? 10 + priorityVoice++ % 4
            : cue == GameAudioCue.Beam || cue == GameAudioCue.Missile ? weaponVoice++ % 6 : 6 + impactVoice++ % 4;
        var voice = voices[index];
        voice.Stop();
        voice.clip = options[Random.Range(0, options.Length)];
        voice.volume = Mathf.Clamp01(volume) * (cue == GameAudioCue.Beam ? 0.42f : 0.72f);
        voice.pitch = Mathf.Clamp(pitch, 0.8f, 1.2f);
        voice.Play();
        if (priority) duckUntil = Time.unscaledTime + 1.2f;
    }
}
