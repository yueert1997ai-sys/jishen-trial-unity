using System;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public PlayerStats stats;
    public WeaponController weaponController;
    public Vector3 AimDirection { get; private set; }
    public Vector3 MoveDirection { get; private set; }
    public float DashCooldownRemaining { get { return Mathf.Max(0f, nextDashTime - Time.time); } }
    public bool IsDashReady { get { return DashCooldownRemaining <= 0f; } }
    public event Action<Vector3> Dashed;

    private float nextDashTime;
    private Damageable damageable;

    private void Awake()
    {
        if (stats == null)
        {
            stats = GetComponent<PlayerStats>();
        }

        if (weaponController == null)
        {
            weaponController = GetComponent<WeaponController>();
        }

        damageable = GetComponent<Damageable>();
        AimDirection = Vector3.forward;
        MoveDirection = Vector3.zero;
    }

    private void Update()
    {
        bool canControl = GameManager.Instance == null || GameManager.Instance.CanPlayerControl;
        UpdateAim();
        if (!canControl)
        {
            MoveDirection = Vector3.zero;
            return;
        }

        Move();
        if (Input.GetKeyDown(KeyCode.Space))
        {
            TryDash();
        }
    }

    private void Move()
    {
        Vector3 movement = Vector3.zero;
        if (Input.GetKey(KeyCode.W))
        {
            movement.z += 1f;
        }

        if (Input.GetKey(KeyCode.S))
        {
            movement.z -= 1f;
        }

        if (Input.GetKey(KeyCode.A))
        {
            movement.x -= 1f;
        }

        if (Input.GetKey(KeyCode.D))
        {
            movement.x += 1f;
        }

        if (movement.sqrMagnitude > 1f)
        {
            movement.Normalize();
        }

        MoveDirection = movement;
        float speed = stats != null ? stats.MoveSpeed : 7f;
        transform.position += movement * speed * Time.deltaTime;
        ClampToArena();
    }

    private void UpdateAim()
    {
        Camera camera = Camera.main;
        if (camera == null)
        {
            return;
        }

        Ray ray = camera.ScreenPointToRay(Input.mousePosition);
        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
        float distance;
        if (!groundPlane.Raycast(ray, out distance))
        {
            return;
        }

        Vector3 target = ray.GetPoint(distance);
        Vector3 direction = target - transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.01f)
        {
            return;
        }

        AimDirection = direction.normalized;
        transform.rotation = Quaternion.LookRotation(AimDirection, Vector3.up);
    }

    private void TryDash()
    {
        if (Time.time < nextDashTime)
        {
            return;
        }

        if (stats != null && !stats.TrySpendEnergy(25f))
        {
            return;
        }

        Vector3 dashDirection = MoveDirection.sqrMagnitude > 0.01f ? MoveDirection : AimDirection;
        if (dashDirection.sqrMagnitude < 0.01f)
        {
            dashDirection = transform.forward;
        }

        float dashDistance = stats != null ? stats.DashDistance : 5f;
        Vector3 dashStart = transform.position;
        transform.position += dashDirection.normalized * dashDistance;
        ClampToArena();

        if (damageable != null)
        {
            damageable.SetInvulnerable(0.22f);
        }

        CombatFeedback.SpawnGroundLine(dashStart, dashDirection, dashDistance, 0.5f, 0.18f, new Color(0.08f, 0.78f, 1f, 1f));
        if (Dashed != null)
        {
            Dashed.Invoke(dashDirection.normalized);
        }
        GameAudio.Play(GameAudioCue.Dash, 0.42f, UnityEngine.Random.Range(0.95f, 1.05f));
        CameraFollow cameraFollow = Camera.main != null ? Camera.main.GetComponent<CameraFollow>() : null;
        if (cameraFollow != null)
        {
            cameraFollow.AddShake(0.08f, 0.12f);
        }

        nextDashTime = Time.time + (stats != null ? stats.DashCooldown : 1.2f);

        EquipmentManager equipmentManager = GetComponent<EquipmentManager>();
        int boosterLevel = equipmentManager != null ? equipmentManager.GetLevel(EquipmentType.Backpack) : 0;
        if (boosterLevel >= 3 && weaponController != null)
        {
            weaponController.SetTemporaryFireRateBonus(1.45f, 1.25f);
        }
    }

    private void ClampToArena()
    {
        Vector3 position = transform.position;
        position.x = Mathf.Clamp(position.x, -27f, 27f);
        position.z = Mathf.Clamp(position.z, -27f, 27f);
        transform.position = position;
    }
}
