using UnityEngine;

public partial class GameAudio
{
    void LoadVelocityBank()
    {
        void Variants(GameAudioCue cue,string stem)
        {Load(cue,"VelocityR1/"+stem+"_1","VelocityR1/"+stem+"_2","VelocityR1/"+stem+"_3");}
        Variants(GameAudioCue.RifleShot,"rifle");Variants(GameAudioCue.EnemyShot,"enemy");
        Variants(GameAudioCue.Beam,"beam");
        Variants(GameAudioCue.SwordCut1,"swing1");Variants(GameAudioCue.SwordCut2,"swing2");Variants(GameAudioCue.SwordCut3,"swing3");
        Variants(GameAudioCue.Slash,"swing1");Variants(GameAudioCue.Hit,"hit");
        Variants(GameAudioCue.HullHit,"metal");Variants(GameAudioCue.MetalHitLight,"metal");
        Variants(GameAudioCue.MetalHitHeavy,"heavy");Variants(GameAudioCue.ArmorClash,"heavy");
        Variants(GameAudioCue.SwordHit,"bladehit");Variants(GameAudioCue.SwordHitHeavy,"bladeheavy");
        Load(GameAudioCue.Footstep,"ArsenalR2/foot_1","ArsenalR2/foot_2","ArsenalR2/foot_3");
        Variants(GameAudioCue.Dash,"dash");Variants(GameAudioCue.BoostStart,"dash");
    }
}
