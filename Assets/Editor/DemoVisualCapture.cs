using System.Globalization;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class DemoVisualCapture
{
    private const string Prefix = "MECH_ROUGE_CAPTURE_";
    private const string ActiveKey = Prefix + "ACTIVE";
    private const string StartTimeKey = Prefix + "START_TIME";
    private const string BeganKey = Prefix + "BEGAN";
    private const string CombatStartTimeKey = Prefix + "COMBAT_START_TIME";
    private const string BossSetupKey = Prefix + "BOSS_SETUP";
    private const string BossStartTimeKey = Prefix + "BOSS_START_TIME";
    private const string HangarCapturedKey = Prefix + "HANGAR_CAPTURED";
    private const string ScenePath = "Assets/Scenes/Demo_Main.unity";
    private const string OutputName = "VisualValidation.png";
    private const string PauseOutputName = "VisualValidation_Pause.png";
    private const string BossOutputName = "VisualValidation_Boss.png";
    private const string HangarOutputName = "VisualValidation_Hangar.png";
    private const int CaptureWidth = 1920;
    private const int CaptureHeight = 1080;

    static DemoVisualCapture()
    {
        if (EditorPrefs.GetBool(ActiveKey, false))
        {
            AttachUpdate();
        }
    }

    [MenuItem("MECH ROUGE/Capture Visual Validation")]
    public static void Run()
    {
        EditorPrefs.SetBool(ActiveKey, true);
        EditorPrefs.SetBool(BeganKey, false);
        EditorPrefs.SetBool(BossSetupKey, false);
        EditorPrefs.SetBool(HangarCapturedKey, false);
        EditorPrefs.SetString(StartTimeKey, EditorApplication.timeSinceStartup.ToString(CultureInfo.InvariantCulture));
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        AttachUpdate();
        EditorApplication.EnterPlaymode();
    }

    private static void AttachUpdate()
    {
        EditorApplication.update -= UpdateCapture;
        EditorApplication.update += UpdateCapture;
    }

    private static void UpdateCapture()
    {
        if (!EditorPrefs.GetBool(ActiveKey, false))
        {
            Cleanup(0);
            return;
        }

        if (GetElapsed(StartTimeKey) > 30d)
        {
            Debug.LogError("MECH_ROUGE_CAPTURE_FAIL: Timed out waiting for the visual validation screenshot.");
            Cleanup(1);
            return;
        }

        if (!EditorApplication.isPlaying)
        {
            return;
        }

        if (!EditorPrefs.GetBool(BeganKey, false))
        {
            GameManager gameManager = Object.FindFirstObjectByType<GameManager>();
            if (gameManager == null)
            {
                return;
            }

            if (GetElapsed(StartTimeKey) < 0.7d)
            {
                return;
            }

            string rootPath = Path.GetDirectoryName(Application.dataPath);
            string hangarOutputPath = Path.Combine(rootPath, HangarOutputName);
            if (!CaptureFrame(hangarOutputPath))
            {
                return;
            }

            EditorPrefs.SetBool(HangarCapturedKey, true);

            gameManager.BeginRun();
            EditorPrefs.SetBool(BeganKey, true);
            EditorPrefs.SetString(CombatStartTimeKey, EditorApplication.timeSinceStartup.ToString(CultureInfo.InvariantCulture));
            return;
        }

        if (EditorPrefs.GetBool(BossSetupKey, false))
        {
            if (GetElapsed(BossStartTimeKey) < 0.45d)
            {
                return;
            }

            string rootPath = Path.GetDirectoryName(Application.dataPath);
            string bossOutputPath = Path.Combine(rootPath, BossOutputName);
            GameObject damageOverlay = GameObject.Find("DamageOverlay");
            if (damageOverlay != null)
            {
                damageOverlay.SetActive(false);
            }

            GameObject announcement = GameObject.Find("WaveAnnouncement");
            if (announcement != null)
            {
                announcement.SetActive(false);
            }

            BossController capturedBoss = Object.FindFirstObjectByType<BossController>();
            PlayerController capturedPlayer = Object.FindFirstObjectByType<PlayerController>();
            Camera capturedCamera = Camera.main;
            Debug.Log("MECH_ROUGE_CAPTURE_BOSS_STATE: boss=" + (capturedBoss != null ? capturedBoss.transform.position.ToString("F2") : "missing") + " player=" + (capturedPlayer != null ? capturedPlayer.transform.position.ToString("F2") : "missing") + " camera=" + (capturedCamera != null ? capturedCamera.transform.position.ToString("F2") : "missing"));

            if (!CaptureFrame(bossOutputPath))
            {
                return;
            }

            Debug.Log("MECH_ROUGE_CAPTURE_PASS: " + Path.Combine(rootPath, HangarOutputName) + " | " + Path.Combine(rootPath, OutputName) + " | " + Path.Combine(rootPath, PauseOutputName) + " | " + bossOutputPath);
            Cleanup(0);
            return;
        }

        string outputPath = Path.Combine(Path.GetDirectoryName(Application.dataPath), OutputName);
        if (GetElapsed(CombatStartTimeKey) < 2.1d)
        {
            return;
        }

        if (!CaptureFrame(outputPath))
        {
            return;
        }

        GameManager pauseOwner = Object.FindFirstObjectByType<GameManager>();
        if (pauseOwner == null)
        {
            return;
        }

        pauseOwner.SetPaused(true);
        GameObject crosshair = GameObject.Find("CombatCrosshair");
        if (crosshair != null)
        {
            crosshair.SetActive(false);
        }

        string pauseOutputPath = Path.Combine(Path.GetDirectoryName(Application.dataPath), PauseOutputName);
        if (!CaptureFrame(pauseOutputPath))
        {
            pauseOwner.SetPaused(false);
            return;
        }

        pauseOwner.SetPaused(false);
        EnemyBase[] enemies = Object.FindObjectsByType<EnemyBase>(FindObjectsSortMode.None);
        for (int i = 0; i < enemies.Length; i++)
        {
            if (enemies[i] != null)
            {
                Object.Destroy(enemies[i].gameObject);
            }
        }

        EnemySpawner spawner = Object.FindFirstObjectByType<EnemySpawner>();
        BossController boss = spawner != null ? spawner.SpawnBoss() : null;
        if (boss == null)
        {
            Debug.LogError("MECH_ROUGE_CAPTURE_FAIL: Boss could not be spawned for visual validation.");
            Cleanup(1);
            return;
        }

        Transform player = pauseOwner.playerController != null ? pauseOwner.playerController.transform : null;
        boss.transform.position = (player != null ? player.position : Vector3.zero) + new Vector3(4.5f, 0f, 2.5f);
        boss.enabled = false;
        pauseOwner.SetProgress("Boss - Phase 1");
        EditorPrefs.SetBool(BossSetupKey, true);
        EditorPrefs.SetString(BossStartTimeKey, EditorApplication.timeSinceStartup.ToString(CultureInfo.InvariantCulture));
    }

    private static bool CaptureFrame(string outputPath)
    {
        Camera camera = Camera.main;
        if (camera == null)
        {
            return false;
        }

        Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        for (int i = 0; i < canvases.Length; i++)
        {
            if (canvases[i].renderMode == RenderMode.WorldSpace)
            {
                continue;
            }

            canvases[i].renderMode = RenderMode.ScreenSpaceCamera;
            canvases[i].worldCamera = camera;
            canvases[i].planeDistance = 1f;
        }

        GameObject crosshair = GameObject.Find("CombatCrosshair");
        if (crosshair != null)
        {
            RectTransform crosshairRect = crosshair.GetComponent<RectTransform>();
            if (crosshairRect != null)
            {
                crosshairRect.anchoredPosition = Vector2.zero;
            }
        }

        Canvas.ForceUpdateCanvases();
        RenderTexture renderTexture = RenderTexture.GetTemporary(CaptureWidth, CaptureHeight, 24, RenderTextureFormat.ARGB32);
        RenderTexture previousActive = RenderTexture.active;
        RenderTexture previousTarget = camera.targetTexture;
        Texture2D frame = new Texture2D(CaptureWidth, CaptureHeight, TextureFormat.RGB24, false);

        camera.targetTexture = renderTexture;
        RenderTexture.active = renderTexture;
        camera.Render();
        frame.ReadPixels(new Rect(0f, 0f, CaptureWidth, CaptureHeight), 0, 0);
        frame.Apply();
        File.WriteAllBytes(outputPath, frame.EncodeToPNG());

        camera.targetTexture = previousTarget;
        RenderTexture.active = previousActive;
        RenderTexture.ReleaseTemporary(renderTexture);
        Object.Destroy(frame);
        return File.Exists(outputPath) && new FileInfo(outputPath).Length > 0;
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

    private static void Cleanup(int exitCode)
    {
        EditorApplication.update -= UpdateCapture;
        EditorPrefs.DeleteKey(ActiveKey);
        EditorPrefs.DeleteKey(StartTimeKey);
        EditorPrefs.DeleteKey(BeganKey);
        EditorPrefs.DeleteKey(CombatStartTimeKey);
        EditorPrefs.DeleteKey(BossSetupKey);
        EditorPrefs.DeleteKey(BossStartTimeKey);
        EditorPrefs.DeleteKey(HangarCapturedKey);

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
