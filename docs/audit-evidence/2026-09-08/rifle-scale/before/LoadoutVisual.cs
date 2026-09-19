using System;
using UnityEngine;

// Rifle contacts follow V7 locomotion. The sword driver continues to own all sword poses.
[DefaultExecutionOrder(120)]
public sealed class LoadoutVisual : MonoBehaviour
{
    [Serializable] public struct Rest { public Transform part; public Vector3 position; public Quaternion rotation; }
    public Rest[] neutral;
    public Transform upperR, lowerR, handR, upperL, lowerL, handL, assembly;
    public Vector3 gripOffsetR, gripOffsetL;
    public Quaternion handRestR, handRestL;
    public RiggedMechAnimator adapter;
    public GameObject WeaponObject { get; private set; }
    public Transform WeaponMuzzle { get; private set; }
    public float RightGripError { get; private set; }
    public float LeftGripError { get; private set; }
    private PlayerLoadout loadout;
    private PlayerController player;
    private RaikenBladePresentation blade;
    private Transform support;
    private float recoil;
    private void Start()
    {
        player = GetComponentInParent<PlayerController>(); loadout = player.Loadout;
        blade = GetComponent<RaikenBladePresentation>();
        loadout.Changed += Equip; player.weaponController.BeamFired += OnShot; Equip();
    }
    private void OnShot() { recoil = .075f; }
    private void OnDestroy()
    {
        if (loadout != null) loadout.Changed -= Equip;
        if (player != null) player.weaponController.BeamFired -= OnShot;
    }
    private void Equip()
    {
        if (WeaponObject != null) { WeaponObject.SetActive(false); Destroy(WeaponObject); }
        WeaponObject = null; WeaponMuzzle = support = null;
        player.weaponController.muzzle = adapter.muzzle;
        if (loadout.IsRifle && loadout.Equipped?.prefab != null)
        {
            WeaponObject = Instantiate(loadout.Equipped.prefab, transform);
            WeaponObject.name = "Equipped_" + loadout.Selected;
            WeaponMuzzle = Find(WeaponObject.transform, "Muzzle");
            support = Find(WeaponObject.transform, "Support");
        }
        blade.bladeRoot.gameObject.SetActive(loadout.CanUseSword);
        blade.SetBeamEnabled(loadout.CanUseSword);
    }
    private static Transform Find(Transform root, string name)
    { foreach (var child in root.GetComponentsInChildren<Transform>(true)) if (child.name == name) return child; return null; }
    private void LateUpdate()
    {
        if (player == null || loadout == null || GameManager.Instance.IsPaused) return;
        bool hangar = GameManager.Instance.Phase == GamePhase.Hangar;
        recoil = Mathf.MoveTowards(recoil, 0, Time.deltaTime * .7f);
        if (hangar && !loadout.CanUseSword)
        {
            foreach (var rest in neutral) if (rest.part != null) rest.part.SetLocalPositionAndRotation(rest.position, rest.rotation);
            FreeArm(upperR, lowerR, handR, 1); FreeArm(upperL, lowerL, handL, -1);
        }
        if (WeaponObject != null)
        {
            float lift = GetComponent<ValkyrMotionDriver>().FlightBlend * .85f;
            Vector3 grip = hangar ? new Vector3(.37f, 2.13f, .34f) : new Vector3(.08f, 2.45f + lift, .06f);
            Quaternion angle = hangar ? player.transform.rotation * Quaternion.Euler(16, -58, 0) :
                Quaternion.LookRotation(player.HasAimPoint ? (player.AimPoint-player.transform.TransformPoint(grip)).normalized : player.transform.forward);
            WeaponObject.transform.SetPositionAndRotation(player.transform.TransformPoint(grip) - angle * Vector3.forward * recoil, angle);
            Quaternion rightRotation = player.transform.rotation * handRestR;
            Quaternion leftRotation = player.transform.rotation * handRestL;
            Vector3 leftGrip = support != null ? support.position : WeaponObject.transform.position;
            SolveArm(upperR, lowerR, handR, WeaponObject.transform.position - rightRotation * gripOffsetR,
                player.transform.TransformPoint(new Vector3(1.1f, 2.0f + lift, -.1f)));
            SolveArm(upperL, lowerL, handL, leftGrip - leftRotation * gripOffsetL,
                player.transform.TransformPoint(new Vector3(-1.1f, 2.05f + lift, .3f)));
            handR.rotation = rightRotation; handL.rotation = leftRotation;
            RightGripError = Vector3.Distance(handR.TransformPoint(gripOffsetR), WeaponObject.transform.position);
            LeftGripError = Vector3.Distance(handL.TransformPoint(gripOffsetL), leftGrip);
            player.weaponController.muzzle = WeaponMuzzle;
        }
        adapter.RefreshSockets();
    }
    private void FreeArm(Transform upper, Transform lower, Transform hand, int side)
    {
        SolveArm(upper, lower, hand, player.transform.TransformPoint(new Vector3(side*.76f,1.95f,.07f)),
            player.transform.TransformPoint(new Vector3(side*1.08f,2.46f,-.15f)));
        hand.rotation = player.transform.rotation * (side > 0 ? handRestR : handRestL);
    }
    private static void SolveArm(Transform shoulder, Transform elbow, Transform hand, Vector3 target, Vector3 hint)
    {
        Vector3 start = shoulder.position;
        float a = Vector3.Distance(start, elbow.position), b = Vector3.Distance(elbow.position, hand.position);
        Vector3 delta = target-start; Vector3 axis = delta.normalized;
        float distance = Mathf.Clamp(delta.magnitude, Mathf.Abs(a-b)+.001f, a+b-.001f);
        Vector3 bend = Vector3.ProjectOnPlane(hint-start, axis).normalized;
        float along = (a*a-b*b+distance*distance)/(2*distance);
        Vector3 elbowTarget = start + axis*along + bend*Mathf.Sqrt(Mathf.Max(0,a*a-along*along));
        shoulder.rotation = Quaternion.FromToRotation(elbow.position-start,elbowTarget-start)*shoulder.rotation;
        elbow.rotation = Quaternion.FromToRotation(hand.position-elbow.position,target-elbow.position)*elbow.rotation;
    }
}
