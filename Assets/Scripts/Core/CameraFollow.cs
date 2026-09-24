using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public Vector3 offset = new Vector3(0f, 18f, -10.4f);
    public float followSpeed = 9f;
    public const float StandardCombatSize = 17f;
    public float normalSize = StandardCombatSize;
    public bool screenShake = true;
    public float p0Size = 9f;
    public float p0Pitch = 68f;
    public float movementLookAhead = 1.25f;
    public float CombatSize => CombatLabSettings.Active ? CombatSliceSettings.Size : StandardCombatSize;
    public Vector3 Focus => focus;
    public float ShakeAmplitude => shakeStrength;

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
        // Crisp combat presentation: no full-screen glow diffusion pass.
    }

    public void AddShake(float strength, float duration)
    {
        if (!screenShake || !GamePreferences.Shake || CombatLabSettings.NoCameraShake) return;
        shakeStrength = Mathf.Max(shakeStrength, Mathf.Clamp(strength,0f,.24f));
        duration = Mathf.Clamp(duration, .01f, .18f);
        shakeTimeRemaining = Mathf.Max(shakeTimeRemaining, duration);
        shakeDuration = Mathf.Max(shakeDuration, duration);
    }

    private void LateUpdate()
    {
        if (target == null) return;
        if (GameManager.Instance != null && GameManager.Instance.Phase == GamePhase.Hangar)
        {
            GameManager.Instance.Hangar.UpdateCamera(view, GameManager.Instance.playerController.transform);
            initialized = false;
            return;
        }
        view.orthographic = true;
        if (controller == null) controller = target.GetComponent<PlayerController>();
        if (Time.unscaledTime >= nextBossLookup)
        {
            nextBossLookup = Time.unscaledTime + 0.25f;
            boss = FindFirstObjectByType<BossController>();
        }
        Vector3 desiredFocus = target.position + (controller != null ? controller.MoveDirection * Mathf.Clamp(movementLookAhead,0f,1.25f) : Vector3.zero);
        float desiredSize = CombatSize;
        if (GameManager.Instance != null && GameManager.Instance.Phase == GamePhase.Hangar)
        {
            desiredFocus = target.position + new Vector3(2.2f, 1f, 0);
            desiredSize = 4.2f;
        }
        if (boss != null && GameManager.Instance != null && GameManager.Instance.IsCombatActive)
        {
            Vector3 toBoss = boss.transform.position - target.position; toBoss.y = 0;
            desiredFocus = target.position;
            desiredSize = CombatSize;
        }
        var lunar=GameManager.Instance!=null&&GameManager.Instance.arenaSector!=null?GameManager.Instance.arenaSector.ActiveLunarLayout:null;
        float pitch=lunar!=null?lunar.cameraPitch:68f;
        // The lunar apron outside the play boundary fills the camera footprint.
        var ground=lunar!=null?lunar.CameraGroundBounds:new Bounds(Vector3.zero,new Vector3(116,1,116));
        float extentX=Mathf.Max(0,ground.extents.x-desiredSize*Mathf.Max(16f/9f,Screen.width/(float)Mathf.Max(1,Screen.height))-.5f);
        float extentZ=Mathf.Max(0,ground.extents.z-desiredSize/Mathf.Sin(pitch*Mathf.Deg2Rad)-2f);
        desiredFocus.x=Mathf.Clamp(desiredFocus.x,ground.center.x-extentX,ground.center.x+extentX);
        desiredFocus.z=Mathf.Clamp(desiredFocus.z,ground.center.z-extentZ,ground.center.z+extentZ);
        float blend = 1f - Mathf.Exp(-followSpeed * Time.deltaTime);
        focus = initialized ? Vector3.Lerp(focus, desiredFocus, blend) : desiredFocus;
        initialized = true;
        view.orthographicSize = Mathf.Lerp(view.orthographicSize, desiredSize, blend);
        Vector3 shake = Vector3.zero;
        if (!screenShake || !GamePreferences.Shake || CombatLabSettings.NoCameraShake) shakeTimeRemaining = shakeStrength = shakeDuration = 0f;
        if (screenShake && GamePreferences.Shake && shakeTimeRemaining > 0f)
        {
            float strength = shakeStrength * Mathf.Clamp01(shakeTimeRemaining / shakeDuration);
            shake = new Vector3(Mathf.PerlinNoise(Time.time * 37f, 0f) - 0.5f, 0f, Mathf.PerlinNoise(0f, Time.time * 43f) - 0.5f) * strength;
            shakeTimeRemaining -= Time.deltaTime;
            if (shakeTimeRemaining <= 0f) shakeStrength = shakeDuration = 0f;
        }
        // Use a fixed pitch so touch movement and the battlefield stay predictable.
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(pitch, 0f, 0f), initialized ? blend : 1f);
        transform.position = focus - transform.forward * 24f + shake;
        float aspect = Screen.width / (float)Mathf.Max(1, Screen.height);
        float height = Mathf.Min(1f, aspect / (16f / 9f));
        view.rect = new Rect(0f, (1f - height) * 0.5f, 1f, height);
    }
}
