using UnityEngine;

// Shared combat mix: events, voices and clocks are independent of encounter route.
public partial class GameAudio
{
    readonly int[] sliceVariants=new int[System.Enum.GetValues(typeof(GameAudioCue)).Length];
    float focusRemaining,backgroundGain=1;
    bool wasBoosting;
    int hostileVoice;
    float AudioClock=>CombatRuntime.SimulationTime;
    float AudioDelta=>Time.deltaTime;
    public float SliceBackgroundGain=>backgroundGain;
    public AudioClip[] SliceClips(GameAudioCue cue)=>clips.TryGetValue(cue,out var result)?result:System.Array.Empty<AudioClip>();

    void LoadSliceBank()
    {
        void Bank(GameAudioCue c,params string[] names)
        {for(int i=0;i<names.Length;i++)names[i]="R9/"+names[i];Load(c,names);}
        Bank(GameAudioCue.RifleShot,"m7_attack","m7_attack_2","m7_attack_3");
        rifleTailClip=Resources.Load<AudioClip>("Audio/Combat/R9/m7_tail");
        Bank(GameAudioCue.Beam,"beam");Bank(GameAudioCue.Missile,"missile");
        Bank(GameAudioCue.Hit,"armor_tick_1","armor_tick_2","armor_tick_3");
        Bank(GameAudioCue.Death,"death_1","death_2");Bank(GameAudioCue.PlayerHit,"player_hit_1","player_hit_2");
        Bank(GameAudioCue.SwordWindup,"saber_load");Bank(GameAudioCue.SwordRegrip,"saber_regrip");
        Bank(GameAudioCue.SwordCut1,"saber_swing_1");Bank(GameAudioCue.SwordCut2,"saber_swing_2");Bank(GameAudioCue.SwordCut3,"saber_heavy");
        Bank(GameAudioCue.Slash,"saber_swing_1");
        Bank(GameAudioCue.SwordHit,"saber_hit","saber_hit_2","saber_hit_3");Bank(GameAudioCue.SwordHitHeavy,"saber_hit_heavy");
        Bank(GameAudioCue.ArmorBreak,"armor_break");Bank(GameAudioCue.ArmorFinish,"armor_finish");
        Bank(GameAudioCue.Dash,"dash");Bank(GameAudioCue.BoostStart,"boost_start");Bank(GameAudioCue.BoostStop,"boost_stop");
        Bank(GameAudioCue.Warning,"warning");Bank(GameAudioCue.EnemyShot,"enemy_shot_1","enemy_shot_2");
        Bank(GameAudioCue.WeaponDraw,"weapon_draw");Bank(GameAudioCue.WeaponStow,"weapon_stow");
        Bank(GameAudioCue.Footstep,"foot_1","foot_2","foot_3");Bank(GameAudioCue.WallHit,"wall_1","wall_2");
        Bank(GameAudioCue.SalvagePull,"salvage_pull");Bank(GameAudioCue.SalvageCatch,"salvage_catch");
        Bank(GameAudioCue.SalvageLock,"salvage_lock");Bank(GameAudioCue.SalvageReady,"salvage_ready");Bank(GameAudioCue.SalvageCancel,"salvage_cancel");
        Bank(GameAudioCue.Wave,"wave");Bank(GameAudioCue.Reward,"reward");Bank(GameAudioCue.Victory,"victory");Bank(GameAudioCue.Defeat,"defeat");
        void ImpactBank(GameAudioCue c,params string[] names)
        {for(int i=0;i<names.Length;i++)names[i]="ImpactR2/"+names[i];Load(c,names);}
        ImpactBank(GameAudioCue.RifleShot,"m7_attack","m7_attack_2","m7_attack_3");
        ImpactBank(GameAudioCue.HullHit,"hull_hit_1","hull_hit_2","hull_hit_3");
        ImpactBank(GameAudioCue.ArmorClash,"armor_clash_1","armor_clash_2","armor_clash_3");
        ImpactBank(GameAudioCue.MetalHitLight,"metal_hit_light_1","metal_hit_light_2","metal_hit_light_3");
        ImpactBank(GameAudioCue.MetalHitHeavy,"metal_hit_heavy_1","metal_hit_heavy_2");
        ImpactBank(GameAudioCue.SwordHit,"saber_hit","saber_hit_2","saber_hit_3");
        ImpactBank(GameAudioCue.SwordHitHeavy,"saber_hit_heavy");
        ImpactBank(GameAudioCue.SwordCut1,"saber_swing_1");ImpactBank(GameAudioCue.SwordCut2,"saber_swing_2");ImpactBank(GameAudioCue.SwordCut3,"saber_heavy");
        ImpactBank(GameAudioCue.ArmorBreak,"armor_break");ImpactBank(GameAudioCue.Death,"death_1","death_2");
        boostLoop.clip=Resources.Load<AudioClip>("Audio/Combat/R9/boost_loop");
        if(rifleTailClip==null||boostLoop.clip==null)Debug.LogError("R9 propulsion or rifle tail missing");
        // Critical information remains physical, but cannot be virtualized behind enemy chatter.
        for(int i=0;i<voices.Length;i++)voices[i].priority=(i==9||i==22||i==23||i==29)?24:i<6?160:80;
        foreach(var s in rifleBodies)s.priority=40;
        foreach(var s in rifleTails)s.priority=140;
        boostLoop.priority=150;ambience.priority=220;
        foreach(var s in music)s.priority=180;
    }

    int NextSliceVariant(GameAudioCue c,int count)=>sliceVariants[(int)c]++%count;
    float SliceSpacing(GameAudioCue c,float fallback)
    {
        if(c==GameAudioCue.Warning)return .32f;
        if(c==GameAudioCue.Footstep)return .085f;
        if(c==GameAudioCue.Hit||c==GameAudioCue.WallHit)return .045f;
        if(c==GameAudioCue.HullHit)return .045f;
        if(c==GameAudioCue.MetalHitLight)return .05f;
        if(c==GameAudioCue.MetalHitHeavy)return .075f;
        if(c==GameAudioCue.ArmorClash)return .07f;
        if(c==GameAudioCue.ArmorBreak||c==GameAudioCue.ArmorFinish)return .045f;
        if(c>GameAudioCue.ArmorFinish)return .035f;
        return fallback;
    }
    int SliceVoice(GameAudioCue c,int original)
    {
        switch(c)
        {
            case GameAudioCue.HullHit:case GameAudioCue.ArmorClash:return 6+impactVoice++%3;
            case GameAudioCue.EnemyShot:return hostileVoice++%4;
            case GameAudioCue.Beam:case GameAudioCue.Missile:return 4+(weaponVoice-1)%2;
            case GameAudioCue.BoostStart:return 24;
            case GameAudioCue.BoostStop:return 25;
            case GameAudioCue.WeaponDraw:case GameAudioCue.WeaponStow:return 26;
            case GameAudioCue.Footstep:return 27;
            case GameAudioCue.WallHit:return 28;
            case GameAudioCue.Warning:return 29;
            case GameAudioCue.SalvageCatch:return 30;
            case GameAudioCue.SalvageCancel:return 31;
            default:return original;
        }
    }
    float SliceCueGain(GameAudioCue c)
    {
        switch(c)
        {
            case GameAudioCue.ArmorBreak:case GameAudioCue.ArmorFinish:case GameAudioCue.PlayerHit:case GameAudioCue.Warning:return 1;
            case GameAudioCue.RifleShot:return Mathf.Lerp(.67f,1,backgroundGain);
            case GameAudioCue.SwordHit:case GameAudioCue.SwordHitHeavy:return Mathf.Lerp(.72f,1,backgroundGain);
            case GameAudioCue.SwordCut1:case GameAudioCue.SwordCut2:case GameAudioCue.SwordCut3:return Mathf.Lerp(.62f,1,backgroundGain);
            default:return backgroundGain;
        }
    }
    void FocusSliceCue(GameAudioCue c)
    {
        float hold=c==GameAudioCue.ArmorFinish?.25f:c==GameAudioCue.ArmorBreak?.19f:c==GameAudioCue.PlayerHit?.15f:c==GameAudioCue.Warning?.10f:c==GameAudioCue.Death?.08f:c==GameAudioCue.SwordHitHeavy?.10f:0;
        if(hold<=0)return;
        focusRemaining=Mathf.Max(focusRemaining,hold);backgroundGain=Mathf.Min(backgroundGain,c==GameAudioCue.Death?.64f:c==GameAudioCue.SwordHitHeavy?.55f:.32f);
    }
    void UpdateSliceMix()
    {
        if(GameManager.Instance!=null&&GameManager.Instance.IsPaused)return;
        focusRemaining=Mathf.Max(0,focusRemaining-AudioDelta);
        if(focusRemaining<=0)backgroundGain=Mathf.MoveTowards(backgroundGain,1,AudioDelta/ .19f);
    }
    void UpdateBoostEdges(PlayerController p)
    {
        bool boosting=p.IsBoosting;
        if(boosting&&!wasBoosting&&!p.IsDashing)Play(GameAudioCue.BoostStart,.48f);
        if(!boosting&&wasBoosting&&!p.IsDashing)Play(GameAudioCue.BoostStop,.35f);
        wasBoosting=boosting;
    }
    public static void ResetCombatSound()
    {
        var a=Instance;if(a==null)return;
        for(int i=0;i<a.voices.Length;i++){a.voices[i].Stop();a.voices[i].volume=0;a.voiceGains[i]=a.swingFade[i]=0;}
        for(int i=0;i<2;i++){a.rifleBodies[i].Stop();a.rifleTails[i].Stop();a.rifleGain[i]=a.rifleTailLevel[i]=0;a.rifleTailRetiring[i]=false;}
        for(int i=0;i<a.lastCueTime.Length;i++)a.lastCueTime[i]=-100;
        a.boostLoop.Stop();a.boostLoop.volume=0;a.boostLevel=0;a.wasBoosting=false;
        a.focusRemaining=0;a.backgroundGain=1;a.duckUntil=0;
    }
}
