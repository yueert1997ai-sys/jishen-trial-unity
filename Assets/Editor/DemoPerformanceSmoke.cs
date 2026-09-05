using System.Collections.Generic;
using System.Globalization;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Profiling;

[InitializeOnLoad]
public static class DemoPerformanceSmoke
{
    private const string Prefix = "MECH_ROUGE_PERF_";
    private const string ActiveKey = Prefix + "ACTIVE";
    private const string BeganKey = Prefix + "BEGAN";
    private const string StressKey = Prefix + "STRESS";
    private const string StartTimeKey = Prefix + "START_TIME";
    private const string WarmupTimeKey = Prefix + "WARMUP_TIME";
    private const string ScenePath = "Assets/Scenes/Demo_Main.unity";
    private const int TargetSamples = 120;
    private const int RenderWidth = 1920;
    private const int RenderHeight = 1080;

    private static readonly List<float> FrameTimes = new List<float>();
    private static int lastFrame = -1;
    private static int peakTriangles;
    private static int peakBatches;
    private static RenderTexture performanceTarget;
    private static Texture2D syncPixel;

    static DemoPerformanceSmoke()
    {
        if (EditorPrefs.GetBool(ActiveKey, false))
        {
            AttachUpdate();
        }
    }

    [MenuItem("MECH ROUGE/Run Performance Smoke")]
    public static void Run()
    {
        FrameTimes.Clear();
        lastFrame = -1;
        peakTriangles = 0;
        peakBatches = 0;
        EditorPrefs.SetBool(ActiveKey, true);
        EditorPrefs.SetBool(BeganKey, false);
        EditorPrefs.SetBool(StressKey, false);
        EditorPrefs.SetString(StartTimeKey, EditorApplication.timeSinceStartup.ToString(CultureInfo.InvariantCulture));
        EditorPrefs.DeleteKey(WarmupTimeKey);
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        AttachUpdate();
        EditorApplication.EnterPlaymode();
    }

    private static void AttachUpdate()
    {
        EditorApplication.update -= UpdatePerformance;
        EditorApplication.update += UpdatePerformance;
    }

    private static void UpdatePerformance()
    {
        if (!EditorPrefs.GetBool(ActiveKey, false))
        {
            Cleanup(0);
            return;
        }

        if (GetElapsed(StartTimeKey) > 45d)
        {
            Fail("Timed out before collecting enough rendered frames.");
            return;
        }

        if (!EditorApplication.isPlaying)
        {
            return;
        }

        GameManager gameManager = Object.FindFirstObjectByType<GameManager>();
        if (gameManager == null)
        {
            return;
        }

        if (!EditorPrefs.GetBool(BeganKey, false))
        {
            gameManager.BeginRun();
            EditorPrefs.SetBool(BeganKey, true);
            return;
        }

        if (!EditorPrefs.GetBool(StressKey, false))
        {
            Damageable player = gameManager.playerStats != null ? gameManager.playerStats.GetComponent<Damageable>() : null;
            if (player != null)
            {
                player.SetInvulnerable(30f);
            }

            SpawnStressActors();
            EditorPrefs.SetBool(StressKey, true);
            EditorPrefs.SetString(WarmupTimeKey, EditorApplication.timeSinceStartup.ToString(CultureInfo.InvariantCulture));
            return;
        }

        if (GetElapsed(WarmupTimeKey) < 1.2d || Time.frameCount == lastFrame)
        {
            return;
        }

        lastFrame = Time.frameCount;
        float renderTime = RenderSynchronizedFrame();
        if (renderTime <= 0f || renderTime > 1f)
        {
            return;
        }

        FrameTimes.Add(renderTime);
        peakTriangles = Mathf.Max(peakTriangles, UnityStats.triangles);
        peakBatches = Mathf.Max(peakBatches, UnityStats.batches);
        if (FrameTimes.Count >= TargetSamples)
        {
            Finish();
        }
    }

    private static float RenderSynchronizedFrame()
    {
        Camera camera = Camera.main;
        if (camera == null)
        {
            return 0f;
        }

        if (performanceTarget == null)
        {
            performanceTarget = new RenderTexture(RenderWidth, RenderHeight, 24, RenderTextureFormat.ARGB32);
            performanceTarget.name = "PerformanceSmoke1080p";
            performanceTarget.Create();
        }

        if (syncPixel == null)
        {
            syncPixel = new Texture2D(1, 1, TextureFormat.RGB24, false);
        }

        RenderTexture previousActive = RenderTexture.active;
        RenderTexture previousTarget = camera.targetTexture;
        long started = System.Diagnostics.Stopwatch.GetTimestamp();
        camera.targetTexture = performanceTarget;
        RenderTexture.active = performanceTarget;
        camera.Render();
        syncPixel.ReadPixels(new Rect(RenderWidth / 2f, RenderHeight / 2f, 1f, 1f), 0, 0);
        syncPixel.Apply(false, false);
        long finished = System.Diagnostics.Stopwatch.GetTimestamp();
        camera.targetTexture = previousTarget;
        RenderTexture.active = previousActive;
        return (float)((finished - started) / (double)System.Diagnostics.Stopwatch.Frequency);
    }

    private static void SpawnStressActors()
    {
        EnemySpawner spawner = Object.FindFirstObjectByType<EnemySpawner>();
        if (spawner == null)
        {
            Fail("EnemySpawner was not found for the performance scenario.");
            return;
        }

        EnemyKind[] kinds = { EnemyKind.Melee, EnemyKind.Ranged, EnemyKind.Drone, EnemyKind.Elite };
        for (int i = 0; i < 16; i++)
        {
            float angle = i * Mathf.PI * 2f / 16f;
            float radius = 8f + (i % 3) * 1.6f;
            Vector3 position = new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
            spawner.SpawnEnemy(kinds[i % kinds.Length], position);
        }
    }

    private static void Finish()
    {
        FrameTimes.Sort();
        double total = 0d;
        for (int i = 0; i < FrameTimes.Count; i++)
        {
            total += FrameTimes[i];
        }

        double averageSeconds = total / FrameTimes.Count;
        double averageFps = averageSeconds > 0d ? 1d / averageSeconds : 0d;
        int p95Index = Mathf.Clamp(Mathf.CeilToInt(FrameTimes.Count * 0.95f) - 1, 0, FrameTimes.Count - 1);
        double p95Ms = FrameTimes[p95Index] * 1000d;
        double maxMs = FrameTimes[FrameTimes.Count - 1] * 1000d;
        double memoryMb = Profiler.GetTotalAllocatedMemoryLong() / (1024d * 1024d);

        if (averageFps < 30d || p95Ms > 80d || maxMs > 250d)
        {
            Fail("Performance gate missed: avg_fps=" + averageFps.ToString("0.0") + " p95_ms=" + p95Ms.ToString("0.0") + " max_ms=" + maxMs.ToString("0.0"));
            return;
        }

        Debug.Log("MECH_ROUGE_PERF_PASS: mode=forced_1080p_gpu_sync samples=" + FrameTimes.Count +
            " avg_fps=" + averageFps.ToString("0.0") +
            " p95_ms=" + p95Ms.ToString("0.0") +
            " max_ms=" + maxMs.ToString("0.0") +
            " peak_triangles=" + peakTriangles +
            " peak_batches=" + peakBatches +
            " allocated_mb=" + memoryMb.ToString("0.0"));
        Cleanup(0);
    }

    private static double GetElapsed(string key)
    {
        double start;
        if (!double.TryParse(EditorPrefs.GetString(key, "0"), NumberStyles.Float, CultureInfo.InvariantCulture, out start))
        {
            start = EditorApplication.timeSinceStartup;
        }

        return EditorApplication.timeSinceStartup - start;
    }

    private static void Fail(string message)
    {
        Debug.LogError("MECH_ROUGE_PERF_FAIL: " + message);
        Cleanup(1);
    }

    private static void Cleanup(int exitCode)
    {
        EditorApplication.update -= UpdatePerformance;
        EditorPrefs.DeleteKey(ActiveKey);
        EditorPrefs.DeleteKey(BeganKey);
        EditorPrefs.DeleteKey(StressKey);
        EditorPrefs.DeleteKey(StartTimeKey);
        EditorPrefs.DeleteKey(WarmupTimeKey);
        Time.timeScale = 1f;
        if (performanceTarget != null)
        {
            performanceTarget.Release();
            Object.DestroyImmediate(performanceTarget);
            performanceTarget = null;
        }

        if (syncPixel != null)
        {
            Object.DestroyImmediate(syncPixel);
            syncPixel = null;
        }

        if (Application.isBatchMode)
        {
            EditorApplication.Exit(exitCode);
        }
        else if (EditorApplication.isPlaying)
        {
            EditorApplication.ExitPlaymode();
        }
    }
}
