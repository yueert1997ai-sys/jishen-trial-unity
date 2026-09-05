using UnityEngine;

[DisallowMultipleComponent]
public class MechMotionAnimator : MonoBehaviour
{
    public PlayerController controller;
    public WeaponController weaponController;
    public Transform visualRoot;
    public float moveBobHeight = 0.045f;
    public float moveBobSpeed = 8f;
    public float leanAngle = 5f;

    private Vector3 baseLocalPosition;
    private Quaternion baseLocalRotation;
    private float recoil;
    private float dashImpulse;
    private float nextThrust;
    private Quaternion smoothedRotation;
    private Vector3 smoothedLean;

    private void Awake()
    {
        if (controller == null)
        {
            controller = GetComponent<PlayerController>();
        }

        if (weaponController == null)
        {
            weaponController = GetComponent<WeaponController>();
        }

        if (visualRoot == null)
        {
            visualRoot = transform.Find("VisualRoot");
        }
    }

    private void Start()
    {
        if (visualRoot != null)
        {
            baseLocalPosition = visualRoot.localPosition;
            baseLocalRotation = visualRoot.localRotation;
            smoothedRotation = visualRoot.rotation;
        }
    }

    private void OnEnable()
    {
        if (weaponController != null)
        {
            weaponController.BeamFired += HandleBeamFired;
        }

        if (controller != null)
        {
            controller.Dashed += HandleDashed;
        }
    }

    private void OnDisable()
    {
        if (weaponController != null)
        {
            weaponController.BeamFired -= HandleBeamFired;
        }

        if (controller != null)
        {
            controller.Dashed -= HandleDashed;
        }
    }

    private void LateUpdate()
    {
        if (visualRoot == null || controller == null)
        {
            return;
        }

        float moveAmount = Mathf.Clamp01(controller.Velocity.magnitude / 7.4f);
        Vector3 localMove = transform.InverseTransformDirection(controller.Velocity / 7.4f);
        localMove = Vector3.ClampMagnitude(localMove, 1.8f);
        float bobSpeed = Mathf.Lerp(2.2f, moveBobSpeed, moveAmount);
        float bobHeight = Mathf.Lerp(0.014f, 0.024f, moveAmount);
        float bob = Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        float idleSway = Mathf.Sin(Time.time * 1.35f) * 0.65f;

        recoil = Mathf.MoveTowards(recoil, 0f, Time.deltaTime * 0.8f);
        dashImpulse = Mathf.MoveTowards(dashImpulse, 0f, Time.deltaTime * 4.2f);

        visualRoot.localPosition = baseLocalPosition + new Vector3(0f, bob + moveAmount * 0.08f, -recoil * 0.7f);
        float pitch = -localMove.z * leanAngle - dashImpulse * 4f + recoil * 18f;
        float roll = -localMove.x * leanAngle + idleSway * (1f - moveAmount);
        smoothedLean = Vector3.Lerp(smoothedLean, new Vector3(pitch, 0, roll), 1 - Mathf.Exp(-12f * Time.deltaTime));
        smoothedRotation = Quaternion.Slerp(smoothedRotation, transform.rotation * baseLocalRotation * Quaternion.Euler(smoothedLean), 1 - Mathf.Exp(-18f * Time.deltaTime));
        visualRoot.rotation = smoothedRotation;
        if (moveAmount > 0.05f && Time.time >= nextThrust && GameManager.Instance != null && GameManager.Instance.IsCombatActive)
        {
            nextThrust = Time.time + 0.035f;
            Vector3 exhaust = -controller.Velocity.normalized - Vector3.up;
            CombatEffects.Thrust(transform.position + Vector3.up * 0.6f + transform.right * 0.35f, exhaust, controller.Velocity.magnitude > 12);
            CombatEffects.Thrust(transform.position + Vector3.up * 0.6f - transform.right * 0.35f, exhaust, controller.Velocity.magnitude > 12);
        }
    }

    private void HandleBeamFired()
    {
        recoil = Mathf.Max(recoil, 0.085f);
    }

    private void HandleDashed(Vector3 direction)
    {
        dashImpulse = 1f;
    }
}
