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

        float moveAmount = Mathf.Clamp01(controller.MoveDirection.magnitude);
        Vector3 localMove = transform.InverseTransformDirection(controller.MoveDirection);
        float bobSpeed = Mathf.Lerp(2.2f, moveBobSpeed, moveAmount);
        float bobHeight = Mathf.Lerp(0.012f, moveBobHeight, moveAmount);
        float bob = Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        float idleSway = Mathf.Sin(Time.time * 1.35f) * 0.65f;

        recoil = Mathf.MoveTowards(recoil, 0f, Time.deltaTime * 0.8f);
        dashImpulse = Mathf.MoveTowards(dashImpulse, 0f, Time.deltaTime * 4.2f);

        visualRoot.localPosition = baseLocalPosition + new Vector3(0f, bob, -recoil * 0.7f);
        float pitch = -localMove.z * leanAngle - dashImpulse * 4f + recoil * 18f;
        float roll = -localMove.x * leanAngle + idleSway * (1f - moveAmount);
        visualRoot.localRotation = baseLocalRotation * Quaternion.Euler(pitch, 0f, roll);
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
