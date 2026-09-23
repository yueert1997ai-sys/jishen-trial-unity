using UnityEngine;

public enum CombatLabVariant { BasicFeedback, FullFeedback, NoCameraShake }

// A local, temporary comparison. Never serialized into the equipment profile.
public static class CombatLabSettings
{
    public static float Duration => CombatRules.Current.LabLimit;
    public static bool Active { get; private set; }
    public static CombatLabVariant Variant { get; private set; } = CombatLabVariant.FullFeedback;
    public static bool MinimalFeedback => Active && Variant == CombatLabVariant.BasicFeedback;
    public static bool BreakEnabled => false; // Retired pressure mechanic.
    public static bool NoCameraShake => Active && (MinimalFeedback || Variant==CombatLabVariant.NoCameraShake);
    public static int DisplayNumber => (int)Variant + 1;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Reset() { Active = false; Variant = CombatLabVariant.FullFeedback; }

    public static void Select(CombatLabVariant variant)
    {
        Active = true;
        Variant = (CombatLabVariant)Mathf.Clamp((int)variant, 0, 2);
    }

    public static void Exit() { Active = false; }

    public static bool SuppressImpactCue(GameAudioCue cue)
    {
        if (!MinimalFeedback) return false;
        return cue == GameAudioCue.Hit || cue == GameAudioCue.Death ||
            cue == GameAudioCue.SwordHit || cue == GameAudioCue.SwordHitHeavy ||
            cue == GameAudioCue.MetalHitLight || cue == GameAudioCue.MetalHitHeavy ||
            cue == GameAudioCue.ArmorBreak || cue == GameAudioCue.ArmorFinish || cue==GameAudioCue.HullHit || cue==GameAudioCue.ArmorClash;
    }
}
