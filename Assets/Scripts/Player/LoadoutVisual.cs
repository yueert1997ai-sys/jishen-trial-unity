using System;
using UnityEngine;

// Runs after animation: neutral maintenance pose in the hangar, shared weapon grip in combat.
[DefaultExecutionOrder(1000)]
public sealed class LoadoutVisual : MonoBehaviour
{
    [Serializable] public struct Rest
    {
        public Transform part;
        public Vector3 position;
        public Quaternion rotation;
    }
    public Rest[] neutral;
    public Transform upperR, lowerR, handR, upperL, lowerL, handL;
    public Vector3 gripOffsetR, gripOffsetL;
    public Quaternion handRestR, handRestL;
    public Transform assembly;
    public RiggedMechAnimator adapter;
    public GameObject WeaponObject { get; private set; }
    public Transform WeaponMuzzle { get; private set; }
    public float RightGripError { get; private set; }
    public float LeftGripError { get; private set; }
    private PlayerLoadout loadout;
    private PlayerController player;
    private Transform support;
    private void Start()
    {
        player = GetComponentInParent<PlayerController>();
        loadout = player.GetComponent<PlayerLoadout>();
        loadout.Changed += Equip;
        Equip();
    }
    private void OnDestroy() { if (loadout != null) loadout.Changed -= Equip; }
    private void Equip()
    {
        if (WeaponObject != null) { WeaponObject.SetActive(false); Destroy(WeaponObject); }
        WeaponObject = null; WeaponMuzzle = support = null;
        if (loadout.Equipped?.prefab != null)
        {
            WeaponObject = Instantiate(loadout.Equipped.prefab, transform);
            WeaponObject.name = "Equipped_" + loadout.Selected;
            WeaponMuzzle = Find(WeaponObject.transform, "Muzzle");
            support = Find(WeaponObject.transform, "Support");
        }
    }
    private static Transform Find(Transform root, string name)
    {
        foreach (var child in root.GetComponentsInChildren<Transform>(true)) if (child.name == name) return child;
        return null;
    }
    private void LateUpdate()
    {
        if (player == null || loadout == null) return;
        bool hangar = GameManager.Instance != null && GameManager.Instance.Phase == GamePhase.Hangar;
        if (hangar)
            foreach (var rest in neutral) if (rest.part != null) rest.part.SetLocalPositionAndRotation(rest.position, rest.rotation);
        if (WeaponObject == null) { player.weaponController.muzzle = adapter.muzzle; return; }
        bool sword = loadout.Selected == PrimaryWeapon.Greatsword;
        if (!hangar && player.Melee.IsAttacking && sword)
        {
            // Follow the animated hand during the actual slash, preserving the grip attachment.
            WeaponObject.transform.SetPositionAndRotation(handR.TransformPoint(gripOffsetR), handR.rotation * Quaternion.Inverse(handRestR) * Quaternion.Euler(-12, -35, 0));
        }
        else
        {
            Vector3 grip = sword ? new Vector3(.06f, 2.18f, .28f)
                : hangar ? new Vector3(.28f, 2.15f, .27f) : new Vector3(.1f, 2.45f, .05f);
            Quaternion angle = sword ? Quaternion.Euler(24, 62, 0)
                : hangar ? Quaternion.Euler(12, -65, 0) : Quaternion.identity;
            WeaponObject.transform.SetPositionAndRotation(player.transform.TransformPoint(grip), player.transform.rotation * angle);
            Quaternion rightRotation = player.transform.rotation * handRestR;
            Quaternion leftRotation = player.transform.rotation * handRestL;
            Vector3 rightTarget = WeaponObject.transform.position - rightRotation * gripOffsetR;
            Vector3 leftGrip = support != null ? support.position : WeaponObject.transform.position;
            Vector3 leftTarget = leftGrip - leftRotation * gripOffsetL;
            SolveArm(upperR, lowerR, handR, rightTarget, player.transform.TransformPoint(new Vector3(1.15f, 1.85f, .15f)));
            SolveArm(upperL, lowerL, handL, leftTarget, player.transform.TransformPoint(new Vector3(-1.15f, 1.85f, .25f)));
            handR.rotation = rightRotation; handL.rotation = leftRotation;
            RightGripError = Vector3.Distance(handR.TransformPoint(gripOffsetR), WeaponObject.transform.position);
            LeftGripError = Vector3.Distance(handL.TransformPoint(gripOffsetL), leftGrip);
        }
        player.weaponController.muzzle = loadout.IsRifle ? WeaponMuzzle : adapter.muzzle;
        adapter.RefreshSockets();
    }
    private static void SolveArm(Transform shoulder, Transform elbow, Transform hand, Vector3 target, Vector3 hint)
    {
        Vector3 start = shoulder.position;
        float a = Vector3.Distance(start, elbow.position), b = Vector3.Distance(elbow.position, hand.position);
        Vector3 delta = target - start;
        float distance = Mathf.Clamp(delta.magnitude, Mathf.Abs(a - b) + .001f, a + b - .001f);
        Vector3 axis = delta.normalized;
        Vector3 bend = Vector3.ProjectOnPlane(hint - start, axis).normalized;
        float along = (a * a - b * b + distance * distance) / (2 * distance);
        Vector3 elbowTarget = start + axis * along + bend * Mathf.Sqrt(Mathf.Max(0, a * a - along * along));
        shoulder.rotation = Quaternion.FromToRotation(elbow.position - start, elbowTarget - start) * shoulder.rotation;
        elbow.rotation = Quaternion.FromToRotation(hand.position - elbow.position, target - elbow.position) * elbow.rotation;
    }
}
