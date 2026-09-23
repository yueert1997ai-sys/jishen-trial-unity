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
    public int ImpactStops { get; private set; }
    public bool BeamEnabled => beam != null && beam.activeSelf;
    public int TrailSamples => samples.Count;
    public event System.Action<Vector3,Vector3> CutSampled;
    private PlayerController player;
    private PlayerMeleeController melee;
    private Damageable health;
    private Mesh ribbon;
    private GameObject ribbonObject;
    private Material trailMaterial;
    private Material contrastMaterial;
    private GameObject contrastObject;
    private Vector3 previousGrip, previousTip;
    private bool previousValid;
    private bool requestedBeam = true;
    private readonly List<Sample> samples = new List<Sample>();
    private readonly List<Vector3> vertices = new List<Vector3>();
    private readonly List<Color> colors = new List<Color>();
    private readonly List<int> indices = new List<int>();
    private static readonly float[] bands = { 0, .40f, .76f, .95f, 1 };
    private static readonly float[] opacity = { 0, .12f, .30f, .76f, .95f };
    private struct Sample { public Vector3 near, far; public float time,life; public int stroke; }
    private int strokeSerial;
    private bool stoppedThisStroke;
    private Transform extensionPivot;
    private bool amethyst;
    private void Start()
    {
        player = GetComponentInParent<PlayerController>();
        if (player == null) { enabled = false; return; }
        melee = player.Melee;
        amethyst=GetComponent<NemesisMotionRig>()!=null;
        health = player.GetComponent<Damageable>();
        extensionPivot=new GameObject("RunBladeReach").transform;
        extensionPivot.SetParent(bladeRoot,false);
        extensionPivot.position=grip.position;
        extensionPivot.rotation=Quaternion.LookRotation((tip.position-grip.position).normalized,bladeRoot.up);
        var children=new List<Transform>();foreach(Transform child in bladeRoot)if(child!=extensionPivot)children.Add(child);
        foreach(var child in children)child.SetParent(extensionPivot,true);
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
        contrastObject=new GameObject("RAIKEN_SweepContrast",typeof(MeshFilter),typeof(MeshRenderer));
        contrastObject.GetComponent<MeshFilter>().sharedMesh=ribbon;
        contrastMaterial=new Material(Resources.Load<Shader>("CombatContrast"));
        var dark=contrastObject.GetComponent<MeshRenderer>();dark.sharedMaterial=contrastMaterial;dark.shadowCastingMode=ShadowCastingMode.Off;dark.receiveShadows=false;
        if (rig.bladeTrail != null) rig.bladeTrail.enabled = false;
        if (GetComponent<RaikenCombatVfx>() == null) gameObject.AddComponent<RaikenCombatVfx>();
    }
    private void Started()
    {
        stoppedThisStroke=false;
        previousValid = false;
        strokeSerial++;
    }
    private void Update()
    {
        if(extensionPivot!=null)extensionPivot.localScale=new Vector3(1,1,player.weaponController.upgradeSystem!=null?player.weaponController.upgradeSystem.BladeReachMultiplier:1);
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
        // One physical interruption per stroke, even when the sweep crosses a crowd.
        // Later targets still receive damage and armor effects without restarting the pause.
        if(!stoppedThisStroke)
        {
            stoppedThisStroke=true;ImpactStops++;
            if (Camera.main != null) Camera.main.GetComponent<CameraFollow>()?.AddShake(heavy?.16f:.11f,heavy?.15f:.10f);
            bool armored=target.LastResult.ArmorDamage>0&&!target.LastResult.BrokeArmor;
            melee.HoldImpact(armored?(heavy?.035f:.018f):melee.CurrentStroke.hitHold);
            GameAudio.StopSwordSwings();
        }
        if(!target.LastResult.BrokeArmor)
        {
            bool armored=target.LastResult.ArmorDamage>0;
            var cue=armored?GameAudioCue.ArmorClash:heavy?GameAudioCue.SwordHitHeavy:GameAudioCue.SwordHit;
            var contact=target.LastResult.ToDamageInfo();
            GameAudio.PlayAt(cue,contact.HasContact?contact.ContactPoint:target.AimCenter,heavy?.90f:.77f,armored?.85f:1);
        }
    }
    public void SetBeamEnabled(bool active) { requestedBeam=active; if (beam != null) beam.SetActive(active); if (!active) Clear(); }
    public void RecordCutSample(Vector3 near, Vector3 far,float sampleTime=-1)
    {
        if(player == null || !player.Stance.CanMelee)return;
        CutSampled?.Invoke(near,far);
        melee.SampleBladeSweep(previousValid?previousGrip:near,previousValid?previousTip:far,near,far);
        previousGrip=near;previousTip=far;previousValid=true;
        if(BeamEnabled)samples.Add(new Sample{near=Vector3.Lerp(near,far,melee.ComboStage==2?.27f:.36f),far=far,time=sampleTime<0?Time.time:sampleTime,
            life=melee.ComboStage==2?.16f:.105f,stroke=strokeSerial});
    }
    private void LateUpdate()
    {
        if (player == null || hand == null || health.IsDead) { Clear(); return; }
        if (GameManager.Instance != null && GameManager.Instance.IsPaused) return;
        bool visible=requestedBeam && player.Stance.CanMelee;
        if(beam!=null && beam.activeSelf!=visible){beam.SetActive(visible);if(!visible)Clear();}
        melee.CompletePoseFrame();
        samples.RemoveAll(sample => Time.time - sample.time > sample.life);
        while (samples.Count > 96) samples.RemoveAt(0);
        DrawRibbon();
    }
    private void DrawRibbon()
    {
        vertices.Clear(); colors.Clear(); indices.Clear();
        for (int i = 0; i < samples.Count; i++)
        {
            var s = samples[i];
            float fade = Mathf.Pow(Mathf.Clamp01(1 - (Time.time - s.time) / s.life), 1.5f);
            for (int band = 0; band < bands.Length; band++)
            {
                vertices.Add(Vector3.Lerp(s.near, s.far, bands[band]));
                Color color = Color.Lerp(amethyst?NemesisMotionRig.Amethyst:new Color(.015f, .38f, 1),amethyst?new Color(.76f,.52f,1):new Color(.72f, 1, 1), Mathf.Pow(bands[band], 14));
                color.a = opacity[band] * fade*(CombatLabSettings.MinimalFeedback?.15f:1f);
                colors.Add(color);
            }
            if (i == 0 || samples[i-1].stroke!=s.stroke) continue;
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
        if(contrastObject!=null)Destroy(contrastObject);
        if(contrastMaterial!=null)Destroy(contrastMaterial);
    }
}
