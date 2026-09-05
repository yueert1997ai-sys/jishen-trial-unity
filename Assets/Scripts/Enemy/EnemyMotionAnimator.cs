using UnityEngine;

public class EnemyMotionAnimator : MonoBehaviour
{
    public Transform visualRoot;
    public bool isDrone;
    public bool isHeavy;

    private Transform rotor;
    private Vector3 baseLocalPosition;
    private Quaternion baseLocalRotation;
    private Vector3 previousWorldPosition;
    private float phase;
    private EnemyBase enemy;
    private BossController boss;

    private void Awake()
    {
        enemy = GetComponent<EnemyBase>();
        boss = GetComponent<BossController>();
        if (visualRoot == null)
        {
            visualRoot = transform.Find("VisualRoot");
        }

        if (visualRoot != null)
        {
            baseLocalPosition = visualRoot.localPosition;
            baseLocalRotation = visualRoot.localRotation;
            rotor = visualRoot.Find("Rotor");
        }

        previousWorldPosition = transform.position;
        phase = Random.value * Mathf.PI * 2f;
    }

    private void LateUpdate()
    {
        if (visualRoot == null)
        {
            return;
        }

        float deltaTime = Mathf.Max(Time.deltaTime, 0.0001f);
        Vector3 worldVelocity = (transform.position - previousWorldPosition) / deltaTime;
        previousWorldPosition = transform.position;
        Vector3 localVelocity = transform.InverseTransformDirection(worldVelocity);
        float speed = new Vector2(worldVelocity.x, worldVelocity.z).magnitude;
        phase += Time.deltaTime * (isDrone ? 5.5f : Mathf.Lerp(2.8f, 8.5f, Mathf.Clamp01(speed / 4f)));

        float bob;
        float pitch;
        float roll;
        if (isDrone)
        {
            bob = Mathf.Sin(phase) * 0.09f;
            pitch = Mathf.Clamp(localVelocity.z * -0.9f, -7f, 7f);
            roll = Mathf.Clamp(localVelocity.x * -1.15f, -10f, 10f);
            if (rotor != null)
            {
                rotor.Rotate(0f, 240f * Time.deltaTime, 0f, Space.Self);
            }
        }
        else
        {
            float stride = Mathf.Clamp01(speed / 3.2f);
            bob = Mathf.Abs(Mathf.Sin(phase)) * (isHeavy ? 0.035f : 0.055f) * stride;
            pitch = Mathf.Clamp(localVelocity.z * (isHeavy ? -0.45f : -0.7f), -5f, 5f);
            roll = Mathf.Sin(phase) * (isHeavy ? 1.1f : 2.2f) * stride;
        }

        visualRoot.localPosition = baseLocalPosition + Vector3.up * bob;
        float windup = enemy != null ? enemy.AttackWindup : boss != null ? boss.AttackWindup : 0f;
        var desired = baseLocalRotation * Quaternion.Euler(pitch - windup * 9f, 0f, roll);
        visualRoot.localRotation = Quaternion.Slerp(visualRoot.localRotation, desired, 1f - Mathf.Exp(-18f * Time.deltaTime));
    }
}
