using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public Vector3 offset = new Vector3(0f, 18f, -10.4f);
    public float followSpeed = 9f;
    public float normalSize = 11.5f;
    public bool screenShake = true;

    private Camera view;
    private PlayerController controller;
    private BossController boss;
    private Vector3 focus;
    private float nextBossLookup;
    private float shakeStrength;
    private float shakeTimeRemaining;
    private float shakeDuration;
    private bool initialized;

    private void Awake()
    {
        view = GetComponent<Camera>();
        view.orthographic = true;
        view.orthographicSize = normalSize;
    }

    public void AddShake(float strength, float duration)
    {
        if (!screenShake || !GamePreferences.Shake) return;
        shakeStrength = Mathf.Max(shakeStrength, Mathf.Min(0.2f, strength));
        shakeTimeRemaining = Mathf.Max(shakeTimeRemaining, duration);
        shakeDuration = Mathf.Max(shakeDuration, Mathf.Max(0.01f, duration));
    }

    private void LateUpdate()
    {
        if (target == null) return;
        if (controller == null) controller = target.GetComponent<PlayerController>();
        if (Time.unscaledTime >= nextBossLookup)
        {
            nextBossLookup = Time.unscaledTime + 0.25f;
            boss = FindFirstObjectByType<BossController>();
        }
        Vector3 desiredFocus = target.position + (controller != null ? controller.MoveDirection * 1.25f : Vector3.zero);
        float desiredSize = normalSize;
        if (GameManager.Instance != null && GameManager.Instance.Phase == GamePhase.Hangar)
        {
            desiredFocus = target.position + new Vector3(2.2f, 1f, 0);
            desiredSize = 4.2f;
        }
        if (boss != null && GameManager.Instance != null && GameManager.Instance.IsCombatActive)
        {
            desiredFocus = Vector3.Lerp(target.position, boss.transform.position, 0.5f);
            desiredSize = Mathf.Clamp(Vector3.Distance(target.position, boss.transform.position) * 0.5f + 5f, normalSize, 17f);
        }
        desiredFocus.x = Mathf.Clamp(desiredFocus.x, -19f, 19f);
        desiredFocus.z = Mathf.Clamp(desiredFocus.z, -19f, 19f);
        float blend = 1f - Mathf.Exp(-followSpeed * Time.deltaTime);
        focus = initialized ? Vector3.Lerp(focus, desiredFocus, blend) : desiredFocus;
        initialized = true;
        view.orthographicSize = Mathf.Lerp(view.orthographicSize, desiredSize, blend);
        Vector3 shake = Vector3.zero;
        if (!screenShake || !GamePreferences.Shake) shakeTimeRemaining = shakeStrength = shakeDuration = 0f;
        if (screenShake && GamePreferences.Shake && shakeTimeRemaining > 0f)
        {
            float strength = shakeStrength * Mathf.Clamp01(shakeTimeRemaining / shakeDuration);
            shake = new Vector3(Mathf.PerlinNoise(Time.time * 37f, 0f) - 0.5f, 0f, Mathf.PerlinNoise(0f, Time.time * 43f) - 0.5f) * strength;
            shakeTimeRemaining -= Time.deltaTime;
            if (shakeTimeRemaining <= 0f) shakeStrength = shakeDuration = 0f;
        }
        // Use a fixed pitch so touch movement and the battlefield stay predictable.
        float pitch = GameManager.Instance != null && GameManager.Instance.Phase == GamePhase.Hangar ? 38f : 60f;
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(pitch, 0f, 0f), initialized ? blend : 1f);
        transform.position = focus - transform.forward * 24f + shake;
        float aspect = Screen.width / (float)Mathf.Max(1, Screen.height);
        float height = Mathf.Min(1f, aspect / (16f / 9f));
        view.rect = new Rect(0f, (1f - height) * 0.5f, 1f, height);
    }
}
