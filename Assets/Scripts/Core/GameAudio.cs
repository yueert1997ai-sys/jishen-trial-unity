using System.Collections.Generic;
using UnityEngine;

public enum GameAudioCue { Beam, Missile, Hit, Death, Dash, Warning, Wave, Reward, Victory, Defeat, Slash, SwordWindup, SwordRegrip, SwordCut1, SwordCut2, SwordCut3, SwordHit, SwordHitHeavy, RifleShot, PlayerHit, MetalHitLight, MetalHitHeavy, SalvagePull, SalvageLock, SalvageReady, ArmorBreak, ArmorFinish, EnemyShot, BoostStart, BoostStop, WeaponDraw, WeaponStow, SalvageCatch, SalvageCancel, Footstep, WallHit, HullHit, ArmorClash }

[DisallowMultipleComponent]
public partial class GameAudio : MonoBehaviour
{
    public static GameAudio Instance { get; private set; }
    private readonly Dictionary<GameAudioCue, AudioClip[]> clips = new Dictionary<GameAudioCue, AudioClip[]>();
    private readonly float[] lastCueTime = new float[System.Enum.GetValues(typeof(GameAudioCue)).Length];
    private AudioSource[] voices;
    private readonly float[] voiceGains = new float[32];
    private readonly GameAudioCue[] voiceCues = new GameAudioCue[32];
    private readonly float[] swingFade = new float[32];
    private readonly AudioSource[] rifleBodies=new AudioSource[2],rifleTails=new AudioSource[2];
    private readonly float[] rifleTailLevel=new float[2],rifleGain=new float[2];
    private readonly bool[] rifleTailRetiring=new bool[2];
    private AudioClip rifleTailClip;
    private int rifleVoice;
    public int RifleShotsPlayed { get; private set; }
    private AudioSource ambience,boostLoop;
    private float boostLevel;
    public float BoostLevel => boostLevel;
    public bool BoostPlaying => boostLoop!=null&&boostLoop.isPlaying;
    private void UpdateBoost()
    {
        if(boostLoop==null)return;
        var gm=GameManager.Instance;var player=gm!=null?gm.playerController:null;
        bool active=gm!=null&&gm.IsCombatActive&&!gm.IsPaused&&player!=null;
        if(!active){boostLevel=0;boostLoop.Stop();boostLoop.volume=0;wasBoosting=false;return;}
        UpdateBoostEdges(player);
        float target=player.IsDashing?.14f:player.IsBoosting?.09f:0;
        boostLevel=Mathf.MoveTowards(boostLevel,target,Time.deltaTime*(target>boostLevel?2f:.9f));
        boostLoop.pitch=Mathf.Lerp(boostLoop.pitch,player.IsDashing?1.35f:1.05f,1-Mathf.Exp(-20*Time.deltaTime));
        boostLoop.volume=boostLevel*GamePreferences.Effects*SliceBackgroundGain;
        if(boostLevel>.001f&&!boostLoop.isPlaying)boostLoop.Play();
        else if(boostLevel<=.001f&&boostLoop.isPlaying)boostLoop.Stop();
    }
    private readonly AudioSource[] music = new AudioSource[2];
    private int activeMusic;
    private float crossfadeRemaining;
    private float musicLevel;
    public bool MusicLoaded => music[0] != null && music[0].clip != null;
    private float duckUntil;
    private int weaponVoice, impactVoice, priorityVoice,swordVoice;
    public static event System.Action<GameAudioCue> CuePlayed;
    public int LoadedCueCount => clips.Count;

    private void Awake()
    {
        Instance = this;
        voices = new AudioSource[32];
        for(int i=0;i<lastCueTime.Length;i++)lastCueTime[i]=-100;
        Load(GameAudioCue.ArmorBreak,"R7/armor_break");
        Load(GameAudioCue.ArmorFinish,"R7/armor_finish");
        Load(GameAudioCue.SalvagePull,"thrusterFire_002");
        Load(GameAudioCue.SalvageLock,"RaikenV7/grip_lock");
        Load(GameAudioCue.SalvageReady,"computerNoise_000");
        for (int i = 0; i < voices.Length; i++)
        {
            voices[i] = gameObject.AddComponent<AudioSource>();
            voices[i].playOnAwake = false;
            voices[i].spatialBlend = 0;
            voices[i].dopplerLevel = 0;
            lastCueTime[i % lastCueTime.Length] = -100;
        }
        Load(GameAudioCue.Beam, "laserLarge_001", "laserLarge_002");
        Load(GameAudioCue.Missile, "thrusterFire_002");
        Load(GameAudioCue.Hit, "impactMetal_heavy_000", "impactMetal_heavy_001", "impactMetal_medium_000");
        Load(GameAudioCue.PlayerHit,"impactMetal_heavy_000","impactMetal_heavy_001");
        Load(GameAudioCue.Death, "explosionCrunch_000", "explosionCrunch_002");
        Load(GameAudioCue.Dash, "thrusterFire_000");
        Load(GameAudioCue.Slash, "forceField_001");
        Load(GameAudioCue.SwordWindup,"RaikenV7/servo_load");
        Load(GameAudioCue.SwordRegrip,"RaikenV7/grip_lock");
        Load(GameAudioCue.SwordCut1,"RaikenV7/cut_reverse");
        Load(GameAudioCue.SwordCut2,"RaikenV7/cut_return");
        Load(GameAudioCue.SwordCut3,"RaikenV7/cut_heavy");
        Load(GameAudioCue.SwordHit,"RaikenV7/armor_cut_01","RaikenV7/armor_cut_02");
        Load(GameAudioCue.SwordHitHeavy,"RaikenV7/armor_break_heavy");
        Load(GameAudioCue.RifleShot,"P0Cadence/m7_attack");
        rifleTailClip=Resources.Load<AudioClip>("Audio/Combat/P0Cadence/m7_tail");
        for(int i=0;i<2;i++)
        {
            rifleBodies[i]=NewWeaponVoice();rifleTails[i]=NewWeaponVoice();
        }
        Load(GameAudioCue.Warning, "computerNoise_000");
        Load(GameAudioCue.Wave, "jingles_HIT00");
        Load(GameAudioCue.Reward, "jingles_HIT04");
        Load(GameAudioCue.Victory, "jingles_HIT08");
        Load(GameAudioCue.Defeat, "jingles_HIT13");
        ambience = gameObject.AddComponent<AudioSource>();
        ambience.clip = Resources.Load<AudioClip>("Audio/Combat/spaceEngineLow_000");
        ambience.loop = true;
        ambience.volume = 0.018f * GamePreferences.Effects;
        ambience.Play();
        {
            var engine=new GameObject("BoostMotor");engine.transform.SetParent(transform,false);
            boostLoop=engine.AddComponent<AudioSource>();boostLoop.playOnAwake=false;boostLoop.loop=true;
            boostLoop.clip=ambience.clip;boostLoop.spatialBlend=0;boostLoop.dopplerLevel=0;boostLoop.volume=0;
        }
        var score = Resources.Load<AudioClip>("Audio/Music/P0_HeavyBattle");
        for (int i = 0; i < music.Length; i++)
        {
            music[i] = gameObject.AddComponent<AudioSource>();
            music[i].clip = score;
            music[i].playOnAwake = false;
            music[i].loop = true;
            music[i].volume = 0;
        }
        if (score != null) music[0].Play();
        else Debug.LogError("Missing combat music.");
        LoadSliceBank();
    }

    private void Load(GameAudioCue cue, params string[] names)
    {
        var loaded = new List<AudioClip>();
        foreach (string name in names)
        {
            var clip = Resources.Load<AudioClip>("Audio/Combat/" + name);
            if (clip != null) loaded.Add(clip);
            else Debug.LogError("Missing combat audio: " + name);
        }
        clips[cue] = loaded.ToArray();
    }

    private void Update()
    {
        UpdateSliceMix();
        UpdateWeaponVoices();
        UpdateBoost();
        float level = (AudioClock < duckUntil ? 0.006f : 0.018f) * GamePreferences.Effects;
        ambience.volume = Mathf.MoveTowards(ambience.volume, level, AudioDelta * 0.08f);
        var gm = GameManager.Instance;
        float targetLevel = gm != null && gm.Phase == GamePhase.Combat ? 0.17f : 0.07f;
        if (AudioClock < duckUntil) targetLevel *= 0.4f;
        targetLevel*=Mathf.Lerp(1,.48f,1-SliceBackgroundGain);
        float mixSpeed=targetLevel<musicLevel?3f:.3f;
        musicLevel = Mathf.MoveTowards(musicLevel, targetLevel, AudioDelta * mixSpeed);
        if (!MusicLoaded || (gm != null && gm.IsPaused)) return;
        // The new track is a complete musical loop. Crossfading at an arbitrary time offsets its beat.
        ApplyMusicGain(0);RefreshMix();
    }

    private void ApplyMusicGain(float fade)
    {
        music[activeMusic].volume = musicLevel * (1 - fade) * GamePreferences.Music;
        music[1 - activeMusic].volume = musicLevel * fade * GamePreferences.Music;
    }

    public void RefreshMix()
    {
        if (voices == null) return;
        for (int i = 0; i < voices.Length; i++) voices[i].volume = voiceGains[i] * GamePreferences.Effects * (swingFade[i]>0?swingFade[i]/.035f:1) * SliceCueGain(voiceCues[i]);
        for(int i=0;i<2;i++)
        {
            rifleBodies[i].volume=rifleGain[i]*GamePreferences.Effects*SliceCueGain(GameAudioCue.RifleShot);
            rifleTails[i].volume=rifleGain[i]*rifleTailLevel[i]*GamePreferences.Effects*SliceBackgroundGain;
        }
        if(boostLoop!=null)boostLoop.volume=boostLevel*GamePreferences.Effects*SliceBackgroundGain;
        if (MusicLoaded) ApplyMusicGain(crossfadeRemaining / 2.5f);
        ambience.volume = (AudioClock < duckUntil ? 0.006f : 0.018f) * GamePreferences.Effects * (SliceBackgroundGain*.45f);
    }

    private void OnDestroy() { if (Instance == this){Instance = null;CuePlayed=null;} }
    public static void StopSalvage()
    {
        if(Instance==null)return;
        for(int i=0;i<Instance.voices.Length;i++)if((Instance.voiceCues[i]>=GameAudioCue.SalvagePull&&Instance.voiceCues[i]<=GameAudioCue.SalvageReady)||Instance.voiceCues[i]==GameAudioCue.SalvageCatch)Instance.voices[i].Stop();
        for(int i=(int)GameAudioCue.SalvagePull;i<=(int)GameAudioCue.SalvageReady;i++)Instance.lastCueTime[i]=-100;
        Instance.lastCueTime[(int)GameAudioCue.SalvageCatch]=-100;
    }
    public static void StopSwordPreparation()
    {
        if(Instance==null)return;
        for(int i=14;i<Instance.voices.Length;i++)if(Instance.voiceCues[i]==GameAudioCue.SwordWindup||Instance.voiceCues[i]==GameAudioCue.SwordRegrip)Instance.voices[i].Stop();
    }

    private AudioSource NewWeaponVoice()
    {
        var source=gameObject.AddComponent<AudioSource>();source.playOnAwake=false;
        source.loop=false;source.spatialBlend=0;source.dopplerLevel=0;source.pitch=1;
        return source;
    }

    public static void StopSwordSwings()
    {
        if(Instance==null)return;
        for(int i=14;i<Instance.voices.Length;i++)
            if(Instance.voiceCues[i]>=GameAudioCue.SwordCut1&&Instance.voiceCues[i]<=GameAudioCue.SwordCut3)
                Instance.swingFade[i]=.035f;
        StopSwordPreparation();
    }

    private void UpdateWeaponVoices()
    {
        if(GameManager.Instance!=null && GameManager.Instance.IsPaused)return;
        for(int i=14;i<voices.Length;i++)if(swingFade[i]>0)
        {
            swingFade[i]=Mathf.Max(0,swingFade[i]-AudioDelta);
            voices[i].volume=voiceGains[i]*GamePreferences.Effects*(swingFade[i]/.035f);
            if(swingFade[i]==0){voices[i].Stop();voiceGains[i]=0;}
        }
        for(int i=0;i<2;i++)if(rifleTailRetiring[i])
        {
            rifleTailLevel[i]=Mathf.Max(0,rifleTailLevel[i]-AudioDelta/.025f);
            rifleTails[i].volume=rifleGain[i]*rifleTailLevel[i]*GamePreferences.Effects;
            if(rifleTailLevel[i]==0){rifleTails[i].Stop();rifleTailRetiring[i]=false;}
        }
    }

    private void PlayRifleShot(float volume,AudioClip attack)
    {
        // Called once per successful fire event, never by a timer or an independent audio loop.
        // Reserve voices so enemy beams and sword impacts cannot steal the player's gun voice.
        int index=rifleVoice++%2;int previous=1-index;
        rifleTailRetiring[previous]=true;
        rifleGain[index]=Mathf.Clamp01(volume)*.72f;
        var body=rifleBodies[index];body.Stop();body.clip=attack;body.pitch=1;
        body.volume=rifleGain[index]*GamePreferences.Effects*SliceCueGain(GameAudioCue.RifleShot);body.Play();
        var tail=rifleTails[index];tail.Stop();tail.clip=rifleTailClip;tail.pitch=1;
        rifleTailLevel[index]=1;rifleTailRetiring[index]=false;
        tail.volume=rifleGain[index]*GamePreferences.Effects*SliceBackgroundGain;tail.Play();
        RifleShotsPlayed++;CuePlayed?.Invoke(GameAudioCue.RifleShot);
        duckUntil=Mathf.Max(duckUntil,AudioClock+.11f);
    }

    public static void Play(GameAudioCue cue, float volume = 1f, float pitch = 1f)
    {
        if (Instance != null) Instance.PlayInternal(cue, volume, pitch);
    }

    public static void PlayAt(GameAudioCue cue,Vector3 position,float volume=1f,float pitch=1f)
    {
        if(Instance!=null)Instance.PlayInternal(cue,volume,pitch,position);
    }

    private void PlayInternal(GameAudioCue cue, float volume, float pitch, Vector3? position=null)
    {
        if(CombatLabSettings.SuppressImpactCue(cue))return;
        if (!clips.TryGetValue(cue, out var options) || options.Length == 0) return;
        if(GameManager.Instance!=null&&GameManager.Instance.IsPaused)return;
        if(cue==GameAudioCue.RifleShot){PlayRifleShot(volume,options[NextSliceVariant(cue,options.Length)]);return;}
        if(cue>=GameAudioCue.SwordCut1&&cue<=GameAudioCue.SwordCut3)StopSwordSwings();
        bool sword=cue>=GameAudioCue.SwordWindup && cue<=GameAudioCue.SwordHitHeavy;
        bool priority = (cue >= GameAudioCue.Warning && cue <= GameAudioCue.Defeat) || cue>=GameAudioCue.SalvagePull;
        float spacing = priority ? 0.18f : cue == GameAudioCue.Hit ? 0.055f : cue>=GameAudioCue.SwordHit?.075f:.035f;
        spacing=SliceSpacing(cue,spacing);
        if (AudioClock - lastCueTime[(int)cue] < spacing) return;
        lastCueTime[(int)cue] = AudioClock;
        // Reserve voices for telegraphs and results so rapid fire cannot cut them off.
        int index = sword ? 14+swordVoice++%8 : priority ? 10 + priorityVoice++ % 4
            : cue == GameAudioCue.Beam || cue == GameAudioCue.Missile || cue == GameAudioCue.RifleShot ? weaponVoice++ % 6 : 6 + impactVoice++ % 4;
        {
            if(cue==GameAudioCue.ArmorBreak)index=22;
            else if(cue==GameAudioCue.ArmorFinish)index=23;
            else if(cue==GameAudioCue.PlayerHit)index=9;
            else if(cue==GameAudioCue.Dash)index=13;
            else if(priority)index=10+(priorityVoice-1)%3;
            else if(!sword&&cue!=GameAudioCue.Beam&&cue!=GameAudioCue.Missile)index=6+(impactVoice-1)%3;
            index=SliceVoice(cue,index);
        }
        float pan=0;
        if(position.HasValue&&GameManager.Instance!=null)
        {
            Vector3 delta=position.Value-GameManager.Instance.playerController.transform.position;delta.y=0;
            volume*=Mathf.Lerp(1,.28f,Mathf.InverseLerp(3,20,delta.magnitude));
            if(cue==GameAudioCue.ArmorBreak||cue==GameAudioCue.ArmorFinish)volume=Mathf.Max(volume,.52f);
            Vector3 right=Camera.main!=null?Camera.main.transform.right:Vector3.right;
            pan=Mathf.Clamp(Vector3.Dot(delta,right)/14f,-.7f,.7f);
        }
        var voice = voices[index];
        voice.panStereo=pan;
        voice.Stop();
        voice.clip = options[NextSliceVariant(cue,options.Length)];
        voiceCues[index]=cue;
        swingFade[index]=0;
        voiceGains[index] = Mathf.Clamp01(volume) * (sword?.72f:cue == GameAudioCue.Beam ? 0.42f : 0.72f);
        voice.volume = voiceGains[index] * GamePreferences.Effects;
        voice.pitch = Mathf.Clamp(pitch, 0.8f, 1.2f);
        voice.Play();
        FocusSliceCue(cue);RefreshMix();
        CuePlayed?.Invoke(cue);
        if(cue==GameAudioCue.PlayerHit)duckUntil=Mathf.Max(duckUntil,AudioClock+.22f);
        if(cue==GameAudioCue.ArmorBreak||cue==GameAudioCue.ArmorFinish)duckUntil=Mathf.Max(duckUntil,AudioClock+.25f);
        else if(cue>GameAudioCue.ArmorFinish)return;
        else if (priority) duckUntil = AudioClock + 1.2f;
        else if(cue==GameAudioCue.SwordHit || cue==GameAudioCue.SwordHitHeavy)duckUntil=Mathf.Max(duckUntil,AudioClock+(cue==GameAudioCue.SwordHitHeavy?.24f:.13f));
        else if(cue==GameAudioCue.RifleShot)duckUntil=Mathf.Max(duckUntil,AudioClock+.11f);
    }
}
