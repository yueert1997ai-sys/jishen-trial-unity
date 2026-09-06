using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[DefaultExecutionOrder(150)]
public sealed class RaikenBladePresentation : MonoBehaviour
{
    public Transform grip, tip, hand, bladeRoot;
    public GameObject beam;
    public RiggedMechAnimator rig;
    public float Reach => grip != null && tip != null ? Vector3.Distance(grip.position,tip.position) : 0;
    public int ImpactCount { get; private set; }
    public bool BeamEnabled => beam != null && beam.activeSelf;
    public int TrailSamples => samples.Count;
    public event System.Action<Vector3,Vector3> CutSampled;
    private PlayerController player;
    private PlayerMeleeController melee;
    private Damageable health;
    private Mesh ribbon;
    private GameObject ribbonObject;
    private Material trailMaterial;
    private Vector3 previousGrip, previousTip;
    private bool previousValid;
    private bool requestedBeam = true;
    private readonly List<Sample> samples = new List<Sample>();
    private readonly List<Vector3> vertices = new List<Vector3>();
    private readonly List<Color> colors = new List<Color>();
    private readonly List<int> indices = new List<int>();
    private static readonly float[] bands = { 0, .40f, .72f, .90f, 1 };
    private static readonly float[] opacity = { 0, .022f, .11f, .40f, .95f };
    private struct Sample { public Vector3 near, far; public float time; }
    private void Start()
    {
        player = GetComponentInParent<PlayerController>();
        if (player == null) { enabled = false; return; }
        melee = player.Melee;
        health = player.GetComponent<Damageable>();
        melee.AttackStarted += Started;
        melee.AttackCancelled += Clear;
        melee.StrikeHit += Hit;
        ribbonObject = new GameObject("RAIKEN_SweptLightRibbon", typeof(MeshFilter), typeof(MeshRenderer));
        ribbon = new Mesh { name = "RaikenActualBladeSweep" }; ribbon.MarkDynamic();
        ribbonObject.GetComponent<MeshFilter>().sharedMesh = ribbon;
        trailMaterial = new Material(Resources.Load<Shader>("RaikenEnergy"));
        var renderer = ribbonObject.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = trailMaterial;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        if (rig.bladeTrail != null) rig.bladeTrail.enabled = false;
        if (GetComponent<RaikenCombatVfx>() == null) gameObject.AddComponent<RaikenCombatVfx>();
    }
    private void Started()
    {
        previousValid = false;
        samples.Clear();
    }
    private void Clear()
    {
        samples.Clear();
        if (ribbon != null) ribbon.Clear();
        if (rig != null && rig.animator != null) rig.animator.speed = 1;
    }
    private void Hit(Damageable target, float damage)
    {
        ImpactCount++;
        bool heavy=melee.ComboStage==2;
        if (Camera.main != null) Camera.main.GetComponent<CameraFollow>()?.AddShake(heavy?.16f:.11f,heavy?.15f:.10f);
        melee.HoldImpact(melee.CurrentStroke.hitHold);
        GameAudio.Play(heavy?GameAudioCue.SwordHitHeavy:GameAudioCue.SwordHit,heavy?.90f:.77f);
    }
    public void SetBeamEnabled(bool active) { requestedBeam=active; if (beam != null) beam.SetActive(active); if (!active) Clear(); }
    public void RecordCutSample(Vector3 near, Vector3 far)
    {
        if(player == null || !player.Stance.CanMelee)return;
        CutSampled?.Invoke(near,far);
        melee.SampleBladeSweep(previousValid?previousGrip:near,previousValid?previousTip:far,near,far);
        previousGrip=near;previousTip=far;previousValid=true;
        if(BeamEnabled)samples.Add(new Sample{near=Vector3.Lerp(near,far,.12f),far=far,time=Time.time});
    }
    private void LateUpdate()
    {
        if (player == null || hand == null || health.IsDead) { Clear(); return; }
        if (GameManager.Instance != null && GameManager.Instance.IsPaused) return;
        bool visible=requestedBeam && player.Stance.CanMelee;
        if(beam!=null && beam.activeSelf!=visible){beam.SetActive(visible);if(!visible)Clear();}
        melee.CompletePoseFrame();
        samples.RemoveAll(sample => Time.time - sample.time > .13f);
        while (samples.Count > 32) samples.RemoveAt(0);
        DrawRibbon();
    }
    private void DrawRibbon()
    {
        vertices.Clear(); colors.Clear(); indices.Clear();
        for (int i = 0; i < samples.Count; i++)
        {
            var s = samples[i];
            float fade = Mathf.Pow(Mathf.Clamp01(1 - (Time.time - s.time) / .13f), 1.5f);
            for (int band = 0; band < bands.Length; band++)
            {
                vertices.Add(Vector3.Lerp(s.near, s.far, bands[band]));
                Color color = Color.Lerp(new Color(.015f, .38f, 1), new Color(.72f, 1, 1), Mathf.Pow(bands[band], 14));
                color.a = opacity[band] * fade;
                colors.Add(color);
            }
            if (i == 0) continue;
            for (int band = 0; band < bands.Length - 1; band++)
            {
                int a = (i - 1) * bands.Length + band, b = i * bands.Length + band;
                indices.Add(a); indices.Add(b); indices.Add(a + 1);
                indices.Add(a + 1); indices.Add(b); indices.Add(b + 1);
            }
        }
        ribbon.Clear();
        ribbon.SetVertices(vertices); ribbon.SetColors(colors); ribbon.SetTriangles(indices, 0); ribbon.RecalculateBounds();
    }
    private void OnDisable()
    {
        if (melee != null) { melee.AttackStarted -= Started; melee.AttackCancelled -= Clear; melee.StrikeHit -= Hit; }
        Clear();
    }
    private void OnDestroy()
    {
        if (ribbonObject != null) Destroy(ribbonObject);
        if (ribbon != null) Destroy(ribbon);
        if (trailMaterial != null) Destroy(trailMaterial);
    }
}
