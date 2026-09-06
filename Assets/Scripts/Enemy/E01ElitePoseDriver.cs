using System;
using System.Collections.Generic;
using UnityEngine;

// Generic, rigid armor joints plus weighted living-metal chains. Driven by actual BossController state.
[DefaultExecutionOrder(80)]
public class E01ElitePoseDriver : MonoBehaviour
{
    public Transform rigRoot;
    public Transform coreSocket;
    public Renderer[] surfaces;
    public float PhaseBlend { get; private set; }
    public float CoreOpening { get; private set; }
    public float MotionSpeed { get; private set; }
    public float TendrilMotionDegrees { get; private set; }
    public int BoundBones => bones.Count;
    public const string DisplayName = "TYPE E-01 ELITE";

    sealed class Joint
    {
        public Transform t;
        public Quaternion rotation, modelRotation;
        public Vector3 position, modelPosition;
    }
    readonly Dictionary<string, Joint> bones = new Dictionary<string, Joint>();
    MaterialPropertyBlock block;
    BossController boss;
    Damageable health;
    Vector3 lastPosition, rigPosition;
    float stride, actionTime, opening, phase, hitRecoil;
    bool previousAction;
    BossPattern previousPattern;
    static readonly int Pulse = Shader.PropertyToID("_LifePulse");
    static readonly int Flow = Shader.PropertyToID("_FlowTime");

    void Awake()
    {
        block = new MaterialPropertyBlock();
        boss = GetComponent<BossController>();
        health = GetComponent<Damageable>();
        if (rigRoot == null) return;
        rigPosition = rigRoot.localPosition;
        foreach (var t in rigRoot.GetComponentsInChildren<Transform>(true))
            if (!bones.ContainsKey(t.name)) bones.Add(t.name, new Joint { t = t, rotation = t.localRotation,
                position = t.localPosition, modelPosition = rigRoot.InverseTransformPoint(t.position), modelRotation = Quaternion.Inverse(rigRoot.rotation) * t.rotation });
        lastPosition = transform.position;
    }
    void OnEnable() { if (health != null) health.OnDamaged += Hit; }
    void OnDisable() { if (health != null) health.OnDamaged -= Hit; }
    void Hit(Damageable actor, DamageInfo info) { hitRecoil = Mathf.Min(1, hitRecoil + .35f); }

    void LateUpdate()
    {
        if (rigRoot == null || boss == null || health == null) return;
        float dt = Time.deltaTime;
        if (dt <= 0 || (GameManager.Instance != null && GameManager.Instance.IsPaused)) return;
        MotionSpeed = Mathf.Lerp(MotionSpeed, (transform.position - lastPosition).magnitude / dt, 1 - Mathf.Exp(-dt * 9));
        lastPosition = transform.position;
        bool active = GameManager.Instance == null || GameManager.Instance.IsCombatActive;
        bool action = boss.ActionRunning && !boss.CoreExposed;
        if (action && (!previousAction || previousPattern != boss.CurrentPattern)) actionTime = 0;
        if (action) actionTime += dt;
        previousAction = action; previousPattern = boss.CurrentPattern;
        phase = Mathf.MoveTowards(phase, boss.IsPhaseTwo ? 1 : 0, dt * .8f);
        opening = Mathf.MoveTowards(opening, boss.CoreExposed ? 1 : phase * .32f, dt * (boss.CoreExposed ? 2.4f : 1.6f));
        PhaseBlend = phase; CoreOpening = opening;
        hitRecoil = Mathf.MoveTowards(hitRecoil, 0, dt * 3.5f);
        float moving = Mathf.Clamp01(MotionSpeed / 2.2f) * (action ? .05f : 1);
        stride += dt * Mathf.Lerp(2.0f, 5.2f, moving);
        float breathe = Mathf.Sin(Time.time * 1.55f);
        float wind = boss.AttackWindup;
        float charge = action && boss.CurrentPattern == BossPattern.Charge ? 1 : 0;
        float mortar = action && boss.CurrentPattern == BossPattern.Mortar ? 1 : 0;
        float scatter = action && boss.CurrentPattern == BossPattern.Scatter ? 1 : 0;
        float summon = action && boss.CurrentPattern == BossPattern.Reinforcements ? 1 : 0;
        float recoil = scatter * Mathf.Pow(Mathf.Max(0, Mathf.Cos((actionTime - .85f) * 12.08f)), 10) * (actionTime > .85f ? 1 : 0);

        foreach (var joint in bones.Values) { joint.t.localRotation = joint.rotation; joint.t.localPosition = joint.position; }
        rigRoot.localPosition = rigPosition;
        Rotate("Waist", new Vector3(1.5f + moving * 3 + charge * (6 + wind * 5), breathe * .6f, Mathf.Sin(stride) * moving * 1.4f));
        Rotate("Chest", new Vector3(1 + breathe * .8f - recoil * 3 - hitRecoil * 4 - opening * 2, -4 - wind * charge * 9, -1.5f));
        Rotate("Head", new Vector3(7 + breathe + wind * 3, 6 + Mathf.Sin(Time.time * .7f) * 2, -5));
        for (int side = 0; side < 2; side++)
        {
            string suffix = side == 0 ? ".R" : ".L";
            float sign = side == 0 ? 1 : -1;
            float cycle = stride + side * Mathf.PI;
            float swing = Mathf.Sin(cycle) * moving;
            Rotate("UpperArm" + suffix, new Vector3(-swing * 7 - (side == 0 ? charge * (13 + wind * 13) : mortar * 14 + summon * 20), sign * (charge * 5 + summon * 12), sign * (3 + phase * 4 + opening * 5)));
            Rotate("Shoulder" + suffix, new Vector3(-swing * 2, 0, sign * (phase * 2 + opening * 2)));
            Rotate("Forearm" + suffix, new Vector3(-4 - scatter * (7 + wind * 7) - mortar * 15 - summon * 10, 0, sign * phase * 3));
            // Short heavy steps; feet are solved back to real ground targets after torso sway.
            SolveLeg(suffix, cycle, moving, charge * .10f);
        }
        TendrilMotionDegrees = 0;
        for (int i = 1; i <= 6; i++)
        for (int j = 0; j < 6; j++)
        {
            float wave = Mathf.Sin(Time.time * (1.3f + phase * .7f) - j * .72f + i * 1.31f);
            float amount = (j == 0 ? 1.3f : 3.4f) + phase * 2 + mortar * 2.5f;
            float outward = (i <= 3 ? 1 : -1) * (phase * 4 + mortar * wind * 5 + summon * 6);
            var angles = new Vector3(wave * amount + mortar * wind * 3, Mathf.Cos(Time.time * 1.1f - j + i) * amount * .6f, wave * amount * .7f + outward * (j == 0 ? 1 : .25f));
            Rotate($"Tendril.{i:00}.{j:00}", angles);
            TendrilMotionDegrees = Mathf.Max(TendrilMotionDegrees, angles.magnitude);
        }
        for (int i = 1; i <= 6; i++)
        for (int j = 0; j < 4; j++)
            Rotate($"Talon.{i:00}.{j:00}", new Vector3(Mathf.Sin(Time.time * 2 - j * .5f + i) * .8f + phase * .9f,
                (i - 3.5f) * (phase * 1.3f + wind * 1.2f), (i - 3.5f) * (phase * 1.5f + opening * .8f)));
        for (int i = 1; i <= 9; i++)
        {
            float angle = (i - 1) * Mathf.PI * 2 / 9;
            Shift($"Iris.{i:00}", new Vector3(Mathf.Cos(angle) * .070f, Mathf.Sin(angle) * .080f, .025f) * opening);
            Rotate($"Iris.{i:00}", new Vector3(0, 0, opening * 8));
        }
        Shift("Iris.Lower", new Vector3(0, -.095f, .015f) * opening);
        float intensity = active ? .88f + phase * .65f + opening * .55f + wind * .45f + recoil * .6f + Mathf.Sin(Time.time * (2.4f + phase)) * .10f : .75f;
        foreach (var surface in surfaces)
        {
            if (surface == null) continue;
            surface.GetPropertyBlock(block); // Keep CombatFeedback's hit flash parameters intact.
            block.SetFloat(Pulse, intensity);
            block.SetFloat(Flow, Time.time * (1 + phase * .7f));
            surface.SetPropertyBlock(block);
        }
    }

    void Rotate(string name, Vector3 modelEuler)
    {
        if (!bones.TryGetValue(name, out var b)) return;
        b.t.localRotation = b.rotation * Quaternion.Inverse(b.modelRotation) * Quaternion.Euler(modelEuler) * b.modelRotation;
    }
    void Shift(string name, Vector3 modelOffset)
    {
        if (!bones.TryGetValue(name, out var b)) return;
        b.t.localPosition = b.position + b.t.parent.InverseTransformVector(rigRoot.TransformVector(modelOffset));
    }
    void SolveLeg(string suffix, float cycle, float amount, float crouch)
    {
        if (!bones.TryGetValue("Thigh" + suffix, out var hip) || !bones.TryGetValue("Calf" + suffix, out var knee) || !bones.TryGetValue("Foot" + suffix, out var foot)) return;
        Vector3 h = hip.t.position, k = knee.t.position, f = foot.t.position;
        float upper = Vector3.Distance(h,k), lower = Vector3.Distance(k,f);
        Vector3 target = rigRoot.TransformPoint(foot.modelPosition + new Vector3(0, Mathf.Max(0, Mathf.Cos(cycle)) * .14f * amount, Mathf.Sin(cycle) * .33f * amount));
        target += transform.forward * crouch;
        Vector3 axis = target-h; float distance = Mathf.Clamp(axis.magnitude,.05f,upper+lower-.005f); axis.Normalize();
        Vector3 pole = Vector3.ProjectOnPlane(transform.forward,axis).normalized;
        float along = (upper*upper-lower*lower+distance*distance)/(2*distance);
        Vector3 desiredKnee = h+axis*along+pole*Mathf.Sqrt(Mathf.Max(0,upper*upper-along*along));
        hip.t.rotation = Quaternion.FromToRotation(k-h,desiredKnee-h)*hip.t.rotation;
        knee.t.rotation = Quaternion.FromToRotation(foot.t.position-knee.t.position,target-knee.t.position)*knee.t.rotation;
        foot.t.rotation = rigRoot.rotation * foot.modelRotation;
    }
}
