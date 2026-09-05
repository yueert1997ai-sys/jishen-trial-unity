using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public Vector3 offset = new Vector3(0f, 16f, -12.5f);
    public float followSpeed = 9f;

    private float shakeStrength;
    private float shakeTimeRemaining;
    private float shakeDuration;

    public void AddShake(float strength, float duration)
    {
        shakeStrength = Mathf.Max(shakeStrength, Mathf.Max(0f, strength));
        shakeTimeRemaining = Mathf.Max(shakeTimeRemaining, Mathf.Max(0f, duration));
        shakeDuration = Mathf.Max(shakeDuration, Mathf.Max(0.01f, duration));
    }

    private void LateUpdate()
    {
        if (target == null)
        {
            return;
        }

        Vector3 shakeOffset = Vector3.zero;
        if (shakeTimeRemaining > 0f)
        {
            float fade = Mathf.Clamp01(shakeTimeRemaining / shakeDuration);
            shakeOffset = Random.insideUnitSphere * shakeStrength * fade;
            shakeOffset.y *= 0.35f;
            shakeTimeRemaining -= Time.deltaTime;
            if (shakeTimeRemaining <= 0f)
            {
                shakeStrength = 0f;
                shakeDuration = 0f;
            }
        }

        Vector3 desired = target.position + offset + shakeOffset;
        transform.position = Vector3.Lerp(transform.position, desired, followSpeed * Time.deltaTime);
        transform.rotation = Quaternion.Euler(57f, 0f, 0f);
    }
}
