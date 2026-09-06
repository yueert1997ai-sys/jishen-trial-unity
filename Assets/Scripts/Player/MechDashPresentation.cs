using UnityEngine;
using UnityEngine.Rendering;

// Additive visual feedback; never moves the motor or changes dash distance/invulnerability.
[DefaultExecutionOrder(100)]
[DisallowMultipleComponent]
public sealed class MechDashPresentation : MonoBehaviour
{
    private PlayerController player;
    private Damageable health;
    private RiggedMechAnimator rig;
    private Transform[] jets;
    private LineRenderer[] flames;
    private TrailRenderer[] trails;
    private Material glow, housing;
    private Vector3 direction;
    private float started = -10, pulse;
    private bool integratedNozzles;
    private ValkyrMotionDriver authored;
    public float Pulse => pulse;
    public int ActiveTrailCount => trails == null ? 0 : (trails[0].emitting ? 2 : 0);

    private void Awake() { player = GetComponent<PlayerController>(); health = GetComponent<Damageable>(); }
    private void OnEnable() { if (player != null) player.Dashed += OnDash; }
    private void OnDisable()
    {
        if (player != null) player.Dashed -= OnDash;
        started = -10;
        pulse = 0;
        if (trails != null) foreach (var trail in trails) if (trail != null) { trail.emitting = false; trail.Clear(); }
        if (flames != null) foreach (var flame in flames) if (flame != null) flame.enabled = false;
    }

    private void Start()
    {
        rig = GetComponentInChildren<RiggedMechAnimator>();
        authored = GetComponentInChildren<ValkyrMotionDriver>();
        Transform anchor = rig != null ? (rig.rigidPose != null ? rig.rigidPose.Resolve(rig.chest) : rig.chest) : transform;
        integratedNozzles = rig != null && rig.rigidPose != null && rig.rigidPose.thrusters.Length == 2;
        glow = new Material(Shader.Find("Sprites/Default"));
        housing = new Material(Shader.Find("Standard"));
        housing.color = new Color(.13f, .17f, .19f);
        housing.SetFloat("_Metallic", .65f);
        housing.SetFloat("_Glossiness", .45f);
        jets = new Transform[2]; flames = new LineRenderer[4]; trails = new TrailRenderer[2];
        for (int i = 0; i < 2; i++)
        {
            var jet = new GameObject("DashGimbal_" + i).transform;
            if (integratedNozzles) jet.SetParent(rig.rigidPose.thrusters[i], false);
            else
            {
                jet.position = transform.position + Vector3.up * 2.05f - transform.forward * .48f + transform.right * (i == 0 ? -.48f : .48f);
                jet.SetParent(anchor, true);
            }
            jets[i] = jet;
            if (!integratedNozzles)
            {
                var nozzle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                Destroy(nozzle.GetComponent<Collider>());
                nozzle.name = "ThrusterNozzle";
                nozzle.transform.SetParent(jet, false);
                nozzle.transform.localRotation = Quaternion.Euler(90, 0, 0);
                nozzle.transform.localScale = new Vector3(.29f, .2f, .29f);
                nozzle.GetComponent<Renderer>().sharedMaterial = housing;
            }
            for (int layer = 0; layer < 2; layer++)
            {
                var line = new GameObject("JetFlame_" + layer).AddComponent<LineRenderer>();
                line.transform.SetParent(jet, false);
                line.sharedMaterial = glow;
                line.useWorldSpace = false;
                line.positionCount = 3;
                line.numCapVertices = 3;
                line.shadowCastingMode = ShadowCastingMode.Off;
                line.receiveShadows = false;
                line.startColor = layer == 0 ? new Color(.04f,.68f,1,.75f) : new Color(.75f,1,1,1);
                line.endColor = new Color(.03f,.6f,1,0);
                flames[i * 2 + layer] = line;
            }
            var tail = new GameObject("BoostWake").AddComponent<TrailRenderer>();
            tail.transform.SetParent(jet, false);
            tail.sharedMaterial = glow;
            tail.time = .16f;
            tail.minVertexDistance = .13f;
            tail.startWidth = .36f;
            tail.endWidth = .025f;
            tail.startColor = new Color(.22f,.9f,1,.7f);
            tail.endColor = new Color(.05f,.6f,1,0);
            tail.shadowCastingMode = ShadowCastingMode.Off;
            tail.receiveShadows = false;
            tail.emitting = false;
            trails[i] = tail;
        }
    }

    private void OnDash(Vector3 value)
    {
        direction = value;
        started = Time.time;
        if (trails != null) foreach (var trail in trails) trail.Clear();
        CombatEffects.Impact(transform.position + Vector3.up * .15f, new Color(.15f,.85f,1), .85f);
    }

    private void LateUpdate()
    {
        if (jets == null || Time.deltaTime <= 0) return;
        bool alive = health == null || !health.IsDead;
        bool active = alive && GameManager.Instance != null && GameManager.Instance.IsCombatActive;
        float age = Time.time - started;
        float attack = Mathf.SmoothStep(0, 1, age / .035f);
        float release = Mathf.Clamp01(1 - (age - player.dashDuration) / .14f);
        pulse = active && age >= 0 ? attack * release : 0;
        if (!player.IsDashing) pulse = Mathf.Min(pulse, .6f * release);
        if (player.IsBoosting) pulse = Mathf.Max(pulse, .72f);
        if(active && !player.Melee.IsAttacking && player.Velocity.sqrMagnitude>1)
            pulse=Mathf.Max(pulse,.16f+.055f*(1+Mathf.Sin(Time.time*24)));
        if (rig != null && rig.enabled && alive && pulse > 0 && authored == null)
        {
            // Tilt the displayed body and retain the solved muzzle direction.
            Transform hips = rig.rigidPose != null ? rig.rigidPose.Resolve(rig.hips) : rig.hips;
            Transform chest = rig.rigidPose != null ? rig.rigidPose.Resolve(rig.chest) : rig.chest;
            Transform cannon = rig.rigidPose != null ? rig.rigidPose.cannon : rig.leftForearm;
            Quaternion cannonRotation = cannon.rotation;
            Vector3 axis = Vector3.Cross(Vector3.up, direction);
            hips.rotation = Quaternion.AngleAxis(24 * pulse, axis) * hips.rotation;
            chest.rotation = Quaternion.AngleAxis(10 * pulse, axis) * chest.rotation;
            cannon.rotation = cannonRotation;
            if (rig.rigidPose != null) rig.rigidPose.GroundFeet();
            rig.RefreshSockets();
        }
        Vector3 exhaust = -(player.IsDashing ? direction : player.Velocity.sqrMagnitude > .1f ? player.Velocity.normalized : transform.forward) + Vector3.down * .24f;
        for (int i = 0; i < 2; i++)
        {
            jets[i].rotation = Quaternion.Slerp(jets[i].rotation, Quaternion.LookRotation(exhaust), 1 - Mathf.Exp(-40 * Time.deltaTime));
            bool fast=player.IsDashing||player.IsBoosting;
            trails[i].emitting = active && !player.Melee.IsAttacking && player.Velocity.sqrMagnitude > 4;
            trails[i].time=fast?.16f:.085f;
            trails[i].startWidth=fast?.36f:.12f;
            for (int layer = 0; layer < 2; layer++)
            {
                var flame = flames[i * 2 + layer];
                flame.enabled = active;
                float length = (.18f + 2.15f * pulse) * (layer == 0 ? 1 : .66f);
                length *= 1 + Mathf.Sin(Time.time * 90 + i) * .06f;
                flame.startWidth = (.07f + .3f * pulse) * (layer == 0 ? 1 : .45f);
                flame.endWidth = .008f;
                float start = integratedNozzles ? 0 : .17f;
                flame.SetPosition(0, new Vector3(0, 0, start));
                flame.SetPosition(1, new Vector3(0, 0, start + length * .35f));
                flame.SetPosition(2, new Vector3(0, 0, start + length));
            }
        }
    }

    private void OnDestroy()
    {
        if (jets != null) foreach (var jet in jets) if (jet != null) Destroy(jet.gameObject);
        if (glow != null) Destroy(glow);
        if (housing != null) Destroy(housing);
    }
}
