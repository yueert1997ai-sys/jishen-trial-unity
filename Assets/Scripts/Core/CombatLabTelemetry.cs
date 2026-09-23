using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

// Main-thread scheduling observations, NOT physical input/display/audio latency.
// Bounded in-memory rows; disk IO happens only when a round ends.
public sealed class CombatLabTelemetry
{
    const int Limit = 16384;
    static readonly CultureInfo Inv = CultureInfo.InvariantCulture;
    static CombatLabTelemetry current;
    public static bool Recording => current != null;
    public static string LastOutput { get; private set; }
    public static string LastError { get; private set; }
    public static LabSummary LastSummary { get; private set; }

    struct EventRow
    {
        public int id, frame, actor, source;
        public double game, wall;
        public string name, detail;
        public float value, extra, x, z;
    }
    struct FrameRow
    {
        public int frame;
        public double game, wall;
        public float wallMs, simulationMs, unscaledMs;
        public bool paused, eligible;
    }
    [Serializable]
    public sealed class LabSummary
    {
        public string version, variant, weapon, reason, startedUtc, timingScope;
        public int seed, eventRows, frameRows, droppedEvents, droppedFrames;
        public int shots, salvos, slashes, dashes, hits, breaks, directHits, kills;
        public float activeSeconds, endingHp, damageDealt, damageTaken;
        public float frameMedianMs, frameP90Ms, frameP95Ms, frameP99Ms;
        public int frameSamples, framesOver33Ms;
        public bool scriptedInputs, fixedSimulationStep, batchMode, inputToPhotonMeasured, audioOnsetMeasured;
    }

    readonly List<EventRow> events = new List<EventRow>(4096);
    readonly List<FrameRow> frames = new List<FrameRow>(4096);
    readonly PlayerController player;
    readonly double gameStart, wallStart;
    readonly int firstFrame;
    readonly LabSummary summary;
    double previousWall;
    bool previousPaused = true;

    CombatLabTelemetry(PlayerController target)
    {
        player = target;
        gameStart = Time.timeAsDouble; wallStart = previousWall = Time.realtimeSinceStartupAsDouble;
        firstFrame = Time.frameCount;
        summary = new LabSummary {
            version = Application.version, variant = CombatLabSettings.Variant.ToString(),
            weapon = player.Loadout.Selected.ToString(), seed = CombatSliceSettings.Seed,
            startedUtc = DateTime.UtcNow.ToString("O", Inv),
            timingScope = "Engine command/action/projectile/contact/feedback/audio scheduling; wall-clock main-thread frame intervals. No physical input-to-photon or audio onset measurement. Fixed-step/batch runs are NOT performance benchmarks.",
            scriptedInputs = PlayerInputRouter.AllowUnfocusedReplay,
            fixedSimulationStep = Time.captureDeltaTime > 0, batchMode = Application.isBatchMode
        };
        player.weaponController.BeamFired += Shot;
        player.weaponController.SkillFired += Salvo;
        player.Melee.AttackStarted += Slash;
        player.Dashed += Dash;
        GameAudio.CuePlayed += AudioScheduled;
    }

    public static void Begin(PlayerController target)
    {
        End("restart", 0);
        LastError = null;
        current = new CombatLabTelemetry(target);
        current.Add("round_start", target, detail: current.summary.variant);
    }

    int Add(string name, UnityEngine.Object actor = null, float value = 0, float extra = 0,
        string detail = "", int source = 0, float x = 0, float z = 0)
    {
        if (events.Count >= Limit) { summary.droppedEvents++; return 0; }
        int id = events.Count + 1;
        events.Add(new EventRow { id = id, frame = Time.frameCount - firstFrame,
            game = Time.timeAsDouble - gameStart, wall = Time.realtimeSinceStartupAsDouble - wallStart,
            name = name, actor = actor != null ? actor.GetInstanceID() : 0,
            value = value, extra = extra, detail = detail, source = source, x = x, z = z });
        return id;
    }

    public static void Command(PlayerCommand command)
    {
        if (current == null) return;
        int buttons = (command.Fire ? 1 : 0) | (command.Melee ? 2 : 0) | (command.Dash ? 4 : 0) |
            (command.Skill ? 8 : 0) | (command.BoostHeld ? 16 : 0);
        current.Add("command_consumed", current.player, buttons, command.HasAim ? 1 : 0,
            "buttons:fire=1,melee=2,dash=4,skill=8,boost=16", x: command.Move.x, z: command.Move.y);
    }

    public static int ProjectileReady(Projectile projectile)
    {
        if (current == null) return 0;
        return current.Add("projectile_ready", projectile, projectile.damage, projectile.Impact,
            projectile.HitKind.ToString(), x: projectile.transform.position.x, z: projectile.transform.position.z);
    }

    public static void Hit(Damageable target, DamageInfo hit, float healthLost)
    {
        if (current == null) return;
        var s = current.summary;
        if (target.team == 0) s.damageTaken += healthLost;
        else { s.hits++; s.damageDealt += healthLost; if (hit.BrokeArmor) s.breaks++; if (hit.DirectHit) s.directHits++; }
        var shot = hit.SourceObject != null ? hit.SourceObject.GetComponent<Projectile>() : null;
        string detail = hit.Kind + (hit.FrontGuarded ? ":guard" : hit.FlankHit ? ":flank" : "");
        var armor = target.GetComponent<ArmorHealth>();
        current.Add("damage_committed", target, healthLost, hit.ArmorDamage,
            detail, shot != null ? shot.LabEventId : 0, target.transform.position.x, target.transform.position.z);
        if (hit.BrokeArmor) current.Add("armor_depleted", target, extra: armor!=null?armor.Maximum:0);
        if (hit.DirectHit) current.Add("direct_hit", target, healthLost, detail: detail);
    }

    public static void Feedback(Damageable target)
    { current?.Add("hit_feedback_scheduled", target); }

    public static void Frame(bool paused)
    {
        if (current == null) return;
        var c = current;
        double now = Time.realtimeSinceStartupAsDouble;
        if (c.frames.Count < Limit)
            c.frames.Add(new FrameRow { frame = Time.frameCount - c.firstFrame,
                game = Time.timeAsDouble - c.gameStart, wall = now - c.wallStart,
                wallMs = (float)((now - c.previousWall) * 1000), simulationMs = Time.deltaTime * 1000, unscaledMs = Time.unscaledDeltaTime * 1000,
                paused = paused, eligible = c.frames.Count > 0 && !paused && !c.previousPaused });
        else c.summary.droppedFrames++;
        c.previousWall = now; c.previousPaused = paused;
        c.summary.fixedSimulationStep |= Time.captureDeltaTime > 0;
    }

    void Shot() { summary.shots++; Add("rifle_action", player); }
    void Salvo() { summary.salvos++; Add("salvo_action", player); }
    void Slash() { summary.slashes++; Add("melee_action", player, player.Melee.ComboStage); }
    void Dash(Vector3 direction) { summary.dashes++; Add("dash_action", player, x: direction.x, z: direction.z); }
    void AudioScheduled(GameAudioCue cue) { Add("audio_scheduled", detail: cue.ToString()); }

    public static void End(string reason, float activeSeconds)
    {
        if (current == null) return;
        var c = current; current = null;
        c.player.weaponController.BeamFired -= c.Shot;
        c.player.weaponController.SkillFired -= c.Salvo;
        c.player.Melee.AttackStarted -= c.Slash;
        c.player.Dashed -= c.Dash;
        GameAudio.CuePlayed -= c.AudioScheduled;
        c.summary.reason = reason; c.summary.activeSeconds = activeSeconds;
        c.summary.endingHp = c.player.stats.CurrentHp;
        c.summary.kills = GameManager.Instance != null ? GameManager.Instance.Kills : 0;
        c.Add("round_end", value: activeSeconds, detail: reason);
        c.summary.eventRows = c.events.Count; c.summary.frameRows = c.frames.Count;
        var timings = new List<float>();
        foreach (var f in c.frames) if (f.eligible) { timings.Add(f.wallMs); if (f.wallMs > 33) c.summary.framesOver33Ms++; }
        timings.Sort(); c.summary.frameSamples = timings.Count;
        c.summary.frameMedianMs = Percentile(timings, .5f); c.summary.frameP90Ms = Percentile(timings, .9f);
        c.summary.frameP95Ms = Percentile(timings, .95f); c.summary.frameP99Ms = Percentile(timings, .99f);
        LastSummary = c.summary;
        try { c.Write(); }
        catch (Exception e) { LastError = e.Message; Debug.LogWarning("Combat Lab recording could not be saved: " + e.Message); }
    }

    static float Percentile(List<float> sorted, float fraction)
    { return sorted.Count == 0 ? 0 : sorted[Mathf.Clamp(Mathf.CeilToInt(sorted.Count * fraction) - 1, 0, sorted.Count - 1)]; }
    static string Number(double value) { return value.ToString("F6", Inv); }
    static string Cell(string value) { return "\"" + (value ?? "").Replace("\"", "\"\"") + "\""; }

    void Write()
    {
        string profile = Environment.GetEnvironmentVariable("MECH_EQUIPMENT_PROFILE");
        string root = string.IsNullOrEmpty(profile) ? Path.GetDirectoryName(Application.dataPath) : Path.GetDirectoryName(Path.GetFullPath(profile));
        string directory = Path.Combine(root, "CombatLabRuns", DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff", Inv) + "-" + Guid.NewGuid().ToString("N").Substring(0, 8));
        Directory.CreateDirectory(directory);
        var csv = new StringBuilder("id,frame,game_s,wall_s,event,actor_id,source_event,value,extra,detail,x,z\n");
        foreach (var e in events)
            csv.Append(e.id).Append(',').Append(e.frame).Append(',').Append(Number(e.game)).Append(',').Append(Number(e.wall)).Append(',')
                .Append(e.name).Append(',').Append(e.actor).Append(',').Append(e.source).Append(',').Append(Number(e.value)).Append(',')
                .Append(Number(e.extra)).Append(',').Append(Cell(e.detail)).Append(',').Append(Number(e.x)).Append(',').Append(Number(e.z)).Append('\n');
        File.WriteAllText(Path.Combine(directory, "events.csv"), csv.ToString(), Encoding.UTF8);
        csv.Clear().Append("frame,game_s,wall_s,wall_interval_ms,simulation_step_ms,unscaled_step_ms,paused,percentile_eligible\n");
        foreach (var f in frames)
            csv.Append(f.frame).Append(',').Append(Number(f.game)).Append(',').Append(Number(f.wall)).Append(',')
                .Append(Number(f.wallMs)).Append(',').Append(Number(f.simulationMs)).Append(',').Append(Number(f.unscaledMs)).Append(',').Append(f.paused ? 1 : 0).Append(',').Append(f.eligible ? 1 : 0).Append('\n');
        File.WriteAllText(Path.Combine(directory, "frames.csv"), csv.ToString(), Encoding.UTF8);
        File.WriteAllText(Path.Combine(directory, "summary.json"), JsonUtility.ToJson(summary, true), Encoding.UTF8);
        LastOutput = directory;
    }
}
