using System;
using System.Collections.Generic;
using UnityEngine;

public enum GameAudioCue
{
    Beam,
    Missile,
    Hit,
    Death,
    Dash,
    Warning,
    Wave,
    Reward,
    Victory,
    Defeat
}

[DisallowMultipleComponent]
public class GameAudio : MonoBehaviour
{
    public static GameAudio Instance { get; private set; }

    private const int SampleRate = 22050;
    private readonly Dictionary<GameAudioCue, AudioClip> clips = new Dictionary<GameAudioCue, AudioClip>();
    private AudioSource[] sources;
    private int nextSource;

    private void Awake()
    {
        Instance = this;
        sources = new AudioSource[10];
        for (int i = 0; i < sources.Length; i++)
        {
            AudioSource source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            source.dopplerLevel = 0f;
            sources[i] = source;
        }

        clips[GameAudioCue.Beam] = CreateSweep("Beam", 0.085f, 980f, 360f, 0.42f, 0.02f, 11);
        clips[GameAudioCue.Missile] = CreateSweep("Missile", 0.18f, 180f, 520f, 0.5f, 0.08f, 17);
        clips[GameAudioCue.Hit] = CreateSweep("Hit", 0.07f, 210f, 90f, 0.38f, 0.48f, 23);
        clips[GameAudioCue.Death] = CreateSweep("Death", 0.22f, 160f, 48f, 0.5f, 0.42f, 31);
        clips[GameAudioCue.Dash] = CreateSweep("Dash", 0.16f, 170f, 740f, 0.42f, 0.12f, 43);
        clips[GameAudioCue.Warning] = CreatePulse("Warning", 0.34f, 220f, 0.42f);
        clips[GameAudioCue.Wave] = CreateSweep("Wave", 0.28f, 330f, 660f, 0.34f, 0f, 47);
        clips[GameAudioCue.Reward] = CreateSweep("Reward", 0.4f, 440f, 920f, 0.34f, 0f, 53);
        clips[GameAudioCue.Victory] = CreateSweep("Victory", 0.65f, 420f, 1050f, 0.38f, 0f, 59);
        clips[GameAudioCue.Defeat] = CreateSweep("Defeat", 0.55f, 260f, 65f, 0.4f, 0.16f, 61);
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public static void Play(GameAudioCue cue, float volume = 1f, float pitch = 1f)
    {
        if (Instance == null)
        {
            return;
        }

        Instance.PlayInternal(cue, volume, pitch);
    }

    private void PlayInternal(GameAudioCue cue, float volume, float pitch)
    {
        AudioClip clip;
        if (!clips.TryGetValue(cue, out clip) || clip == null || sources == null || sources.Length == 0)
        {
            return;
        }

        AudioSource source = sources[nextSource];
        nextSource = (nextSource + 1) % sources.Length;
        source.Stop();
        source.clip = clip;
        source.volume = Mathf.Clamp01(volume);
        source.pitch = Mathf.Clamp(pitch, 0.65f, 1.5f);
        source.Play();
    }

    private static AudioClip CreateSweep(string clipName, float duration, float startFrequency, float endFrequency, float volume, float noiseMix, int seed)
    {
        int sampleCount = Mathf.CeilToInt(duration * SampleRate);
        float[] samples = new float[sampleCount];
        System.Random random = new System.Random(seed);
        float phase = 0f;
        for (int i = 0; i < sampleCount; i++)
        {
            float progress = i / (float)Mathf.Max(1, sampleCount - 1);
            float frequency = Mathf.Lerp(startFrequency, endFrequency, progress);
            phase += frequency / SampleRate;
            float tone = Mathf.Sin(phase * Mathf.PI * 2f);
            float noise = (float)(random.NextDouble() * 2d - 1d);
            float envelope = Mathf.Pow(1f - progress, 1.7f) * Mathf.Min(1f, progress * 18f);
            samples[i] = (tone * (1f - noiseMix) + noise * noiseMix) * volume * envelope;
        }

        AudioClip clip = AudioClip.Create(clipName, sampleCount, 1, SampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private static AudioClip CreatePulse(string clipName, float duration, float frequency, float volume)
    {
        int sampleCount = Mathf.CeilToInt(duration * SampleRate);
        float[] samples = new float[sampleCount];
        for (int i = 0; i < sampleCount; i++)
        {
            float time = i / (float)SampleRate;
            float progress = i / (float)Mathf.Max(1, sampleCount - 1);
            float gate = Mathf.Sin(time * Mathf.PI * 8f) > 0f ? 1f : 0.18f;
            float envelope = Mathf.Pow(1f - progress, 0.65f) * Mathf.Min(1f, progress * 20f);
            samples[i] = Mathf.Sin(time * frequency * Mathf.PI * 2f) * volume * gate * envelope;
        }

        AudioClip clip = AudioClip.Create(clipName, sampleCount, 1, SampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }
}
