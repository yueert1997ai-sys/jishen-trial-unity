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
    public Transform EjectionPort {get;private set;}
    public float RightGripError { get; private set; }
    public float LeftGripError { get; private set; }
    private PlayerLoadout loadout;
    private PlayerController player;
    private RaikenBladePresentation blade;
    private Transform support;
    private Transform thorax;
    private HalbreakerMount shoulderMount;
    public float ShoulderContactError { get; private set; }
    private float recoil;
    private NemesisMotionRig nemesis;
    private void Start()
    {
        player = GetComponentInParent<PlayerController>(); loadout = player.Loadout;
        nemesis=GetComponent<NemesisMotionRig>();
        blade = GetComponent<RaikenBladePresentation>();
        loadout.Changed += Equip; player.weaponController.BeamFired += OnShot; Equip();
    }
    private void OnShot() { recoil = nemesis!=null?.13f:.075f; }
    private void OnDestroy()
    {
        if (loadout != null) loadout.Changed -= Equip;
        if (player != null) player.weaponController.BeamFired -= OnShot;
    }
    private void Equip()
    {
        if (WeaponObject != null) { WeaponObject.SetActive(false); Destroy(WeaponObject); }
        WeaponObject = null; WeaponMuzzle = support = EjectionPort = null;
        shoulderMount = null;
        player.weaponController.muzzle = adapter.muzzle;
        if (loadout.IsRifle && loadout.Equipped?.prefab != null)
        {
            WeaponObject = Instantiate(loadout.Equipped.prefab, transform);
            WeaponObject.name = "Equipped_" + loadout.Selected;
            WeaponMuzzle = Find(WeaponObject.transform, "Muzzle");
            EjectionPort=Find(WeaponObject.transform,"EjectionPort");
            // Imported M7 sockets inherited the mesh's -90 degree conversion; +Z must denote the barrel.
            if(loadout.Selected==PrimaryWeapon.M7 && WeaponMuzzle!=null)WeaponMuzzle.rotation=WeaponObject.transform.rotation;
            support = Find(WeaponObject.transform, "Support");
            shoulderMount = WeaponObject.GetComponent<HalbreakerMount>();
            thorax = Find(assembly, "Thorax");
            if (shoulderMount != null) WeaponObject.transform.SetParent(thorax, true);
        }
        blade.bladeRoot.gameObject.SetActive(loadout.CanUseSword);
        blade.SetBeamEnabled(loadout.CanUseSword);
    }
    private static Transform Find(Transform root, string name)
    { foreach (var child in root.GetComponentsInChildren<Transform>(true)) if (child.name == name) return child; return null; }
    private void LateUpdate() => UpdatePose(true);
    public void SynchronizeForFire() => UpdatePose(false);
    private void UpdatePose(bool advanceRecoil)
    {
        if (player == null || loadout == null || GameManager.Instance.IsPaused) return;
        bool hangar = GameManager.Instance.Phase == GamePhase.Hangar;
        if(advanceRecoil) recoil = Mathf.MoveTowards(recoil, 0, Time.deltaTime * .7f);
        if (hangar && !loadout.CanUseSword)
        {
            foreach (var rest in neutral) if (rest.part != null) rest.part.SetLocalPositionAndRotation(rest.position, rest.rotation);
            FreeArm(upperR, lowerR, handR, 1); FreeArm(upperL, lowerL, handL, -1);
        }
        if (WeaponObject != null)
        {
            if(loadout.CanUseSword && player.Stance.State!=WeaponStance.Ranged)
            {
                // This later pose writer must relinquish the arms to the complete melee pose.
                WeaponObject.SetActive(false);adapter.RefreshSockets();return;
            }
            WeaponObject.SetActive(true);
            if(nemesis!=null&&(loadout.Selected==PrimaryWeapon.M7||loadout.Selected==PrimaryWeapon.NemesisLauncher))
            {PoseNemesis(hangar);adapter.RefreshSockets();return;}
            if (shoulderMount != null)
            {
                PoseShoulderCannon(hangar);
                adapter.RefreshSockets();
                return;
            }
            float lift = GetComponent<ValkyrMotionDriver>().FlightBlend * .85f;
            // Larger rifles sit ahead of the waist armor; the support marker remains on the rear handguard.
            Vector3 grip = hangar ? new Vector3(.46f, 2.22f, .34f) : new Vector3(.08f, 2.45f + lift, .06f);
            Quaternion angle = hangar ? player.transform.rotation * Quaternion.Euler(16, -68, 0) :
                Quaternion.LookRotation(player.HasAimPoint ? (player.AimPoint-player.transform.TransformPoint(grip)).normalized : player.transform.forward);
            if(!hangar)
                angle=PlanarCombat.Rotation(player.transform.TransformPoint(grip),player.AimPoint,player.AimDirection);
            bool rightHandOnly = loadout.Equipped != null && loadout.Equipped.rightHandOnly;
            float rightSide = Mathf.Sign(player.transform.InverseTransformPoint(upperR.position).x);
            if (rightHandOnly)
            {
                grip = new Vector3(rightSide * 1.06f, 2.25f + lift, .48f);
                angle = player.transform.rotation * Quaternion.Euler(8, -rightSide * 12, 0);
            }
            WeaponObject.transform.SetPositionAndRotation(player.transform.TransformPoint(grip) - angle * Vector3.forward * recoil, angle);
            if (rightHandOnly && !hangar && player.HasAimPoint && WeaponMuzzle != null)
            {
                Vector3 localMuzzle = WeaponObject.transform.InverseTransformPoint(WeaponMuzzle.position);
                for (int i = 0; i < 3; i++)
                {
                    Vector3 aim = player.AimPoint - WeaponObject.transform.TransformPoint(localMuzzle);
                    aim=PlanarCombat.Direction(aim,player.AimDirection);
                    if (aim.sqrMagnitude > .25f) WeaponObject.transform.rotation = Quaternion.LookRotation(aim.normalized, player.transform.up);
                }
            }
            if(!hangar && !rightHandOnly && WeaponMuzzle!=null)
            {
                // Converge the offset barrel on the cursor in the same plane as the projectile.
                for(int i=0;i<3;i++)WeaponObject.transform.rotation=PlanarCombat.Rotation(WeaponMuzzle.position,player.AimPoint,player.AimDirection);
            }
            Quaternion rightRotation = player.transform.rotation * handRestR;
            Quaternion leftRotation = player.transform.rotation * handRestL;
            Vector3 leftGrip = support != null ? support.position : WeaponObject.transform.position;
            SolveArm(upperR, lowerR, handR, WeaponObject.transform.position - rightRotation * gripOffsetR,
                player.transform.TransformPoint(rightHandOnly ? new Vector3(rightSide * 1.38f, 2.01f + lift, -.02f) : new Vector3(1.1f, 2.0f + lift, -.1f)));
            handR.rotation = rightRotation;
            if (!rightHandOnly)
            {
                SolveArm(upperL, lowerL, handL, leftGrip - leftRotation * gripOffsetL,
                    player.transform.TransformPoint(new Vector3(-1.1f, 2.05f + lift, .3f)));
                handL.rotation = leftRotation;
            }
            RightGripError = Vector3.Distance(handR.TransformPoint(gripOffsetR), WeaponObject.transform.position);
            LeftGripError = rightHandOnly ? 0f : Vector3.Distance(handL.TransformPoint(gripOffsetL), leftGrip);
            player.weaponController.muzzle = WeaponMuzzle;
        }
        adapter.RefreshSockets();
    }
    private void PoseShoulderCannon(bool hangar)
    {
        float side = Mathf.Sign(player.transform.InverseTransformPoint(upperR.position).x);
        // The saddle follows the chest, including running lean and boost lift. The hand never carries the root.
        Vector3 anchor = thorax.TransformPoint(new Vector3(side * .84f, .576385f, .008546f));
        Quaternion angle = player.transform.rotation;
        Vector3 shoulderLocal = shoulderMount.shoulder.localPosition;
        Vector3 muzzleLocal = shoulderMount.muzzle.localPosition;
        var root = WeaponObject.transform;
        for (int i = 0; i < 5; i++)
        {
            root.SetPositionAndRotation(anchor - angle * shoulderLocal, angle);
            if (!hangar && player.HasAimPoint)
            {
                Vector3 direction = PlanarCombat.AimDirection(root.TransformPoint(muzzleLocal),player.AimPoint,player.AimDirection);
                if (direction.sqrMagnitude > .04f) angle = Quaternion.LookRotation(direction.normalized, player.transform.up);
            }
        }
        root.SetPositionAndRotation(anchor - angle * shoulderLocal, angle);
        // Horizontal crossbar uses the authored closed right fist; left locomotion pose is left free.
        Quaternion handAngle = angle * handRestR;
        Vector3 target = shoulderMount.grip.position;
        SolveArm(upperR, lowerR, handR, target - handAngle * gripOffsetR,
            anchor + player.transform.rotation * new Vector3(side * .42f, -.45f, .15f));
        handR.rotation = handAngle;
        handR.GetComponentInChildren<ValkyrHandGrip>()?.Pose(0);
        RightGripError = Vector3.Distance(handR.TransformPoint(gripOffsetR), target);
        LeftGripError = 0;
        ShoulderContactError = Vector3.Distance(shoulderMount.shoulder.position, anchor);
        player.weaponController.muzzle = WeaponMuzzle;
    }
    private void PoseNemesis(bool hangar)
    {
        bool launcher=loadout.Selected==PrimaryWeapon.NemesisLauncher;
        bool down=player.GetComponent<Damageable>().IsDead;
        float lift=GetComponent<ValkyrMotionDriver>().FlightBlend*.28f;
        Vector3 grip=hangar?new Vector3(.84f,2.00f,.28f):launcher?new Vector3(.40f,2.76f+lift,.20f):new Vector3(.58f,2.85f+lift,1.42f);
        Quaternion angle=hangar?player.transform.rotation*Quaternion.Euler(64,8,0):player.transform.rotation;
        if(!launcher&&!hangar&&!down)
        {
            // The reference gesture extends the arm from its moving shoulder,
            // with a relaxed elbow, instead of anchoring a bent fist to the chest.
            float reach=Vector3.Distance(upperR.position,lowerR.position)+Vector3.Distance(lowerR.position,handR.position);
            angle=player.transform.rotation*Quaternion.AngleAxis(-16,Vector3.forward);
            var handAngle=angle*Quaternion.LookRotation(Vector3.left,new Vector3(0,-.242f,-.970f));
            var wrist=upperR.position+player.transform.forward*(reach*.96f)-player.transform.up*.085f;
            grip=player.transform.InverseTransformPoint(wrist+handAngle*gripOffsetR);
        }
        if(down)
        {
            // Carry the weapon down with the collapsed shoulder; never keep an
            // absolute standing-height aim target after the chassis falls.
            grip=player.transform.InverseTransformPoint(upperR.position)+new Vector3(.12f,-.56f,.40f);
            angle=player.transform.rotation*Quaternion.Euler(46,18,-12);
        }
        WeaponObject.transform.SetPositionAndRotation(player.transform.TransformPoint(grip)-angle*Vector3.forward*recoil,angle);
        // Aim from the grip, including the rifle's casual cant. Iterating from
        // the long muzzle would oscillate on targets inside its barrel reach.
        if(!hangar&&!down&&player.HasAimPoint)
            WeaponObject.transform.rotation=NemesisAim(launcher?0:-16);
        angle=WeaponObject.transform.rotation;
        // The authored aim can lie outside the moving shoulder's reach. Resolve the
        // arm, seat the weapon in its actual palm, then reconverge the barrel.
        Quaternion right=Quaternion.identity;
        for(int pass=0;pass<4;pass++)
        {
            angle=WeaponObject.transform.rotation;
            right=angle*Quaternion.LookRotation(Vector3.left,new Vector3(0,-.242f,-.970f));
            SolveArm(upperR,lowerR,handR,WeaponObject.transform.position-right*gripOffsetR,
                player.transform.TransformPoint(launcher||hangar?new Vector3(1.04f,2.18f+lift,-.02f):new Vector3(.92f,2.56f+lift,.68f)));
            handR.rotation=right;
            WeaponObject.transform.position=handR.TransformPoint(gripOffsetR);
            if(pass<3&&!hangar&&!down&&player.HasAimPoint)WeaponObject.transform.rotation=NemesisAim(launcher?0:-16);
        }
        handR.GetComponent<ValkyrHandGrip>()?.Pose(0);
        RightGripError=Vector3.Distance(handR.TransformPoint(gripOffsetR),WeaponObject.transform.position);
        LeftGripError=0;
        if(launcher&&!hangar&&!down)
        {
            Quaternion left=angle*Quaternion.LookRotation(Vector3.up,Vector3.left);
            SolveArm(upperL,lowerL,handL,support.position-left*gripOffsetL,
                player.transform.TransformPoint(new Vector3(-.89f,2.26f+lift,.26f)));
            handL.rotation=left;handL.GetComponent<ValkyrHandGrip>()?.Pose(.10f);
            LeftGripError=Vector3.Distance(handL.TransformPoint(gripOffsetL),support.position);
        }
        player.weaponController.muzzle=WeaponMuzzle;
    }
    private Quaternion NemesisAim(float cant)
    {
        var root=WeaponObject.transform;
        var planar=player.AimPoint-root.position;planar.y=0;
        var direction=PlanarCombat.AimDirection(root.position,player.AimPoint,player.AimDirection);
        float offset=(Quaternion.AngleAxis(cant,Vector3.forward)*WeaponMuzzle.localPosition).x;
        float correction=Vector3.Dot(planar.normalized,direction)>.99f
            ?Mathf.Asin(Mathf.Clamp(offset/Mathf.Max(.30f,planar.magnitude),-.5f,.5f))*Mathf.Rad2Deg:0;
        return Quaternion.LookRotation(direction,Vector3.up)*Quaternion.Euler(0,-correction,0)*Quaternion.AngleAxis(cant,Vector3.forward);
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
