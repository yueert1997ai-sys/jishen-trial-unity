using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

[DefaultExecutionOrder(250)]
public sealed class P0CombatCheck : MonoBehaviour
{
    IEnumerator scenario;string output;float deadline;int errors;bool done;
    GameManager gm;PlayerController p;RaikenBladePresentation blade;ValkyrMotionDriver motion;LoadoutVisual visual;
    readonly List<string> report=new List<string>();
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Bootstrap()
    {if(Array.IndexOf(Environment.GetCommandLineArgs(),"-p0Check")>=0){AudioListener.volume=0;DontDestroyOnLoad(new GameObject("P0Check").AddComponent<P0CombatCheck>());}}
    void Awake(){Application.logMessageReceived+=Log;}
    void Start()
    {
        var profile=Environment.GetEnvironmentVariable("MECH_EQUIPMENT_PROFILE");
        if(string.IsNullOrEmpty(profile)){Application.Quit(2);return;}
        output=Path.GetDirectoryName(profile);Directory.CreateDirectory(output);
        Application.runInBackground=true;AudioListener.volume=0;
        PlayerInputRouter.AllowUnfocusedReplay=true;Time.captureDeltaTime=1f/60;Application.targetFrameRate=240;
        deadline=Time.realtimeSinceStartup+((CombatRuntime.HasArgument("-sliceReplay")||CombatRuntime.HasArgument("-naturalCheck")||CombatRuntime.HasArgument("-fullPlayCheck"))?1500:300);scenario=Scenario();
    }
    void LateUpdate()
    {
        if(done||scenario==null)return;
        try{if(Time.realtimeSinceStartup>deadline)throw new Exception("P0 timeout");if(!scenario.MoveNext())Finish(null);}
        catch(Exception e){Finish(e.ToString());}
    }
    void Log(string message,string stack,LogType type)
    {if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert){errors++;report.Add(message+"\n"+stack);}}
    void Check(bool value,string message){if(!value)throw new Exception(message);report.Add("PASS "+message);}
    void Command(bool slash=false,bool fire=false,bool dash=false,Vector2 move=default)
    {p.Simulate(new PlayerCommand{Melee=slash,Fire=fire,Dash=dash,Move=move,HasAim=true,AimPoint=new Vector3(0,1.6f,18)},Time.deltaTime);}
    void Reset()
    {p.RestoreAt(new Vector3(0,.1f,0));p.AimAt(new Vector3(0,1.6f,18));p.stats.ResetStats();}
    IEnumerator Scenario()
    {
        for(int i=0;i<45;i++)yield return null;
        gm=GameManager.Instance;p=gm.playerController;
        Check(p.Loadout.Select(PrimaryWeapon.M7),"existing local M7 selected");gm.BeginP0Combat();
        for(int i=0;i<15;i++)yield return null;
        p.enabled=false;p.InputRouter.readKeyboard=false;
        blade=p.GetComponentInChildren<RaikenBladePresentation>();motion=p.GetComponentInChildren<ValkyrMotionDriver>();visual=p.GetComponentInChildren<LoadoutVisual>();
        Check(blade!=null&&motion!=null&&visual!=null,"current hero body and full-body driver loaded");
        Check(p.Loadout.CanUseRifle&&p.Loadout.CanUseSword,"M7 and saber available on one mech");
        if(CombatRuntime.HasArgument("-mobileTravelCheck"))
        {var mobile=MobileTravelChecks.Run(gm,p,output,Check,Capture);while(mobile.MoveNext())yield return mobile.Current;yield break;}
        if(CombatRuntime.HasArgument("-contactIntegrityCheck"))
        {var contact=ContactIntegrityChecks.Run(gm,p,Check,Capture);while(contact.MoveNext())yield return contact.Current;yield break;}
        if(CombatRuntime.HasArgument("-weaponHandlingCheck"))
        {var arms=WeaponHandlingChecks.Run(gm,p,output,Check,Capture);while(arms.MoveNext())yield return arms.Current;yield break;}
        if(CombatRuntime.HasArgument("-weaponHudCheck"))
        {var arms=WeaponHandlingChecks.Hud(gm,p,output,Check,Capture);while(arms.MoveNext())yield return arms.Current;yield break;}
        if(CombatRuntime.HasArgument("-enemyInteractionCheck"))
        {var interaction=EnemyInteractionChecks.Run(gm,p,output,Check,Capture);while(interaction.MoveNext())yield return interaction.Current;yield break;}
        if(CombatRuntime.HasArgument("-combatArsenalCheck"))
        {var arsenal=CombatArsenalChecks.Run(gm,p,Check,Capture);while(arsenal.MoveNext())yield return arsenal.Current;yield break;}
        if(CombatRuntime.HasArgument("-valkyrCannonCheck"))
        {var cannon=ValkyrCannonChecks.Run(gm,p,Check,Capture);while(cannon.MoveNext())yield return cannon.Current;yield break;}
        if(CombatRuntime.HasArgument("-nemesisRaikenCheck"))
        {var raiken=NemesisRaikenChecks.Run(gm,p,output,Check,Capture);while(raiken.MoveNext())yield return raiken.Current;yield break;}
        if(CombatRuntime.HasArgument("-combatVelocityCheck"))
        {var velocity=CombatVelocityChecks.Run(gm,p,output,Check,Capture);while(velocity.MoveNext())yield return velocity.Current;yield break;}
        if(CombatRuntime.HasArgument("-importedMotionCheck"))
        {var imported=ImportedMotionChecks.Run(gm,p,output,Check,Capture);while(imported.MoveNext())yield return imported.Current;yield break;}
        if(CombatRuntime.HasArgument("-aiUiCheck"))
        {var aiui=AiUiChecks.Run(gm,p,output,Check,Capture);while(aiui.MoveNext())yield return aiui.Current;yield break;}
        if(CombatRuntime.HasArgument("-lunarBasinCheck"))
        {var lunar=LunarBasinChecks.Run(gm,p,output,Check,Capture);while(lunar.MoveNext())yield return lunar.Current;yield break;}
        if(CombatRuntime.HasArgument("-arenaTacticsCheck"))
        {var arena=ArenaTacticsChecks.Run(gm,p,output,Check,Capture);while(arena.MoveNext())yield return arena.Current;yield break;}
        if(CombatRuntime.HasArgument("-combatPunchCheck"))
        {var punch=CombatPunchChecks.Run(gm,p,Check,Capture);while(punch.MoveNext())yield return punch.Current;yield break;}
        if(CombatRuntime.HasArgument("-combatPunchPerformance"))
        {var perf=CombatPunchChecks.Performance(gm,p,output,Check);while(perf.MoveNext())yield return perf.Current;yield break;}
        if(CombatRuntime.HasArgument("-nemesisDroneVfxCheck"))
        {var vfx=NemesisDroneVfxChecks.Run(gm,p,Check,Capture);while(vfx.MoveNext())yield return vfx.Current;yield break;}
        if(CombatRuntime.HasArgument("-nemesisDronesCheck"))
        {var drones=NemesisDronesChecks.Run(gm,p,Check,Capture);while(drones.MoveNext())yield return drones.Current;yield break;}
        if(CombatRuntime.HasArgument("-heroSelectionCheck"))
        {var heroes=HeroSelectionChecks.Run(gm,p,Check,Capture);while(heroes.MoveNext())yield return heroes.Current;yield break;}
        if(CombatRuntime.HasArgument("-nemesisPerformance"))
        {var performance=NemesisPerformanceChecks.Run(gm,p,output,Check,Capture);while(performance.MoveNext())yield return performance.Current;yield break;}
        if(CombatRuntime.HasArgument("-nemesisCheck"))
        {var nemesis=NemesisMotionChecks.Run(gm,p,output,Check,Capture);while(nemesis.MoveNext())yield return nemesis.Current;yield break;}
        if(CombatRuntime.HasArgument("-recoveryCheck"))
        {var recovery=ImpactPolishChecks.Recovery(gm,p,Check,Capture);while(recovery.MoveNext())yield return recovery.Current;yield break;}
        if(CombatRuntime.HasArgument("-impactPolishCheck"))
        {var impact=ImpactPolishChecks.Presentation(gm,p,Check,Capture);while(impact.MoveNext())yield return impact.Current;yield break;}
        if(CombatRuntime.HasArgument("-meleeFeelCheck"))
        {var feel=MeleeFeelChecks.Run(gm,p,output,Check,Capture);while(feel.MoveNext())yield return feel.Current;yield break;}
        if(CombatRuntime.HasArgument("-naturalCheck")||CombatRuntime.HasArgument("-fullPlayCheck"))
        {
            var play=RebuildPlayChecks.Run(gm,p,output,Check,Capture);while(play.MoveNext())yield return play.Current;yield break;
        }
        if(Array.IndexOf(Environment.GetCommandLineArgs(),"-fullRebuildCheck")>=0)
        {
            var flow=RebuildFlowChecks.Run(gm,p,output,Check,Capture);while(flow.MoveNext())yield return flow.Current;yield break;
        }
        if(Array.IndexOf(Environment.GetCommandLineArgs(),"-rebuildCheck")>=0)
        {
            var rebuild=HadesRebuildChecks.Run(gm,p,output,Check,Capture);
            while(rebuild.MoveNext())yield return rebuild.Current;
            yield break;
        }
        if(Array.IndexOf(Environment.GetCommandLineArgs(),"-foundationCheck")>=0)
        {
            var foundation=CombatFoundationChecks.Run(gm,p,output,Check,Capture);
            while(foundation.MoveNext())yield return foundation.Current;
            yield break;
        }
        if(Array.IndexOf(Environment.GetCommandLineArgs(),"-combatLabCheck")>=0)
        {
            var lab=CombatLabChecks.Run(gm,p,output,Check,Capture);
            while(lab.MoveNext())yield return lab.Current;
            yield break;
        }
        if(Array.IndexOf(Environment.GetCommandLineArgs(),"-tacticsCheck")>=0)
        {
            var tactics=CombatTacticsChecks.Run(gm,p,output,Check,Capture);
            while(tactics.MoveNext())yield return tactics.Current;
            yield break;
        }
        if(Array.IndexOf(Environment.GetCommandLineArgs(),"-loopV2Playtest")>=0)
        {
            var play=CombatLoopV2Playtest.Run(gm,p,output,Check,Capture);
            while(play.MoveNext())yield return play.Current;
            yield break;
        }
        if(Array.IndexOf(Environment.GetCommandLineArgs(),"-loopV2Check")>=0)
        {
            var loop=CombatLoopV2Checks.Run(gm,p,output,Check,Capture);
            while(loop.MoveNext())yield return loop.Current;
            yield break;
        }
        if(Array.IndexOf(Environment.GetCommandLineArgs(),"-soundMixCheck")>=0)
        {
            var sound=R9AudioChecks.Run(gm,p,output,Check);while(sound.MoveNext())yield return sound.Current;
            var bank=R9AudioChecks.CaptureBank(output,Check);while(bank.MoveNext())yield return bank.Current;
            yield break;
        }
        if(Array.IndexOf(Environment.GetCommandLineArgs(),"-contactPolishReplay")>=0)
        {
            var detail=ContactPolishReplay.Run(gm,p,output,Check);
            while(detail.MoveNext())yield return detail.Current;
            yield break;
        }
        if(Array.IndexOf(Environment.GetCommandLineArgs(),"-contactPolishCheck")>=0)
        {
            var contact=ContactPolishChecks.Run(gm,p,Check,Capture);
            while(contact.MoveNext())yield return contact.Current;
            yield break;
        }
        if(Array.IndexOf(Environment.GetCommandLineArgs(),"-armorBreakCheck")>=0)
        {
            var armorBreak=ArmorBreakChecks.Run(gm,p,Check,Capture);
            while(armorBreak.MoveNext())yield return armorBreak.Current;
            yield break;
        }
        if(Array.IndexOf(Environment.GetCommandLineArgs(),"-absorptionCheck")>=0)
        {
            var absorption=SliceAbsorptionChecks.Run(gm,p,output,Check,Capture);
            while(absorption.MoveNext())yield return absorption.Current;
            yield break;
        }
        if(Array.IndexOf(Environment.GetCommandLineArgs(),"-combatAudioCheck")>=0)
        {
            var audio=CombatAudioChecks.Run(gm,p,Check);
            while(audio.MoveNext())yield return audio.Current;
            yield break;
        }
        if(Array.IndexOf(Environment.GetCommandLineArgs(),"-enemyImpactCheck")>=0)
        {
            var impact=EnemyImpactChecks.Run(gm,p,output,Check,Capture);
            while(impact.MoveNext())yield return impact.Current;
            yield break;
        }
        if(Array.IndexOf(Environment.GetCommandLineArgs(),"-rhythmCheck")>=0)
        {
            var rhythm=CombatRhythmChecks.Run(gm,p,output,Check,Capture);
            while(rhythm.MoveNext())yield return rhythm.Current;
            yield break;
        }
        if(Array.IndexOf(Environment.GetCommandLineArgs(),"-sliceReplay")>=0)
        {
            var replay=CombatSliceReplay.Run(gm,p,output,Check);
            while(replay.MoveNext())yield return replay.Current;
            yield break;
        }
        if(Array.IndexOf(Environment.GetCommandLineArgs(),"-sliceCheck")>=0)
        {
            var slice=CombatSliceChecks.Run(gm,p,output,Check,Capture);
            while(slice.MoveNext())yield return slice.Current;
            yield break;
        }
        if(Array.IndexOf(Environment.GetCommandLineArgs(),"-p0TerrainAudit")>=0)
        {
            var traversal=P0LunarTraversalAudit.Run(p,output,Check);
            while(traversal.MoveNext())yield return traversal.Current;
            yield break;
        }
        if(Array.IndexOf(Environment.GetCommandLineArgs(),"-m7Check")>=0)
        {
            var m7=M7RefinementChecks.Run(gm,p,visual,Check,Capture);
            while(m7.MoveNext())yield return m7.Current;
            yield break;
        }
        var overdrive=OverdriveChecks.Run(gm,p,Check,Capture);
        while(overdrive.MoveNext())yield return overdrive.Current;
        if(Array.IndexOf(Environment.GetCommandLineArgs(),"-p0VisualCheck")>=0)yield break;
        var ballistics=P0BallisticsChecks.Run(gm,p,visual,Check,Capture);
        while(ballistics.MoveNext())yield return ballistics.Current;
        var cadence=P0AudioCadenceChecks.Run(p,Check);
        while(cadence.MoveNext())yield return cadence.Current;
        var profile=p.Melee.MotionProfile;
        var effects=blade.GetComponent<RaikenCombatVfx>();
        foreach(int stage in new[]{0,2})
        {
            float time=profile.Get(stage).keys[stage==0?3:4].time;
            float speed=Vector3.Angle(profile.Evaluate(stage,time-.002f).blade,profile.Evaluate(stage,time+.002f).blade)/.004f;
            Check(speed>100,$"stage {stage+1} blade carries angular speed through interior cut key: {speed:F0} deg/s");
        }
        for(int stage=0;stage<2;stage++)
        {
            var a=profile.Evaluate(stage,profile.Get(stage).linkTime);var b=profile.Evaluate(stage+1,0);
            Check(Vector3.Distance(a.wrist,b.wrist)<.001f&&Vector3.Angle(a.blade,b.blade)<.01f&&Vector3.Distance(a.chest,b.chest)<.001f,"shared combo key has no neutral-pose reset: "+stage);
        }
        // Exercise real frame ordering, actual mesh transforms and input arbitration at three frame rates.
        foreach(int fps in new[]{30,60,120})
        {
            Time.captureDeltaTime=1f/fps;Reset();
            int releases=effects.HeavyReleaseBursts;
            Command(slash:true);yield return null;
            int[] frames=new int[3];bool[] queued=new bool[3];int last=-1;float maxGrip=0;float start=Time.time;
            Vector3 lastTip=blade.tip.position;float maxLinkTip=0;Quaternion initialChest=motion.Chest.rotation;float chestTurn=0,upperTurn=0;
            Quaternion initialUpper=visual.upperR.rotation;
            for(int i=0;i<fps*4;i++)
            {
                if(p.Melee.IsAttacking)
                {
                    int stage=p.Melee.ComboStage;frames[stage]++;
                    if(last>=0&&stage!=last)
                    {
                        maxLinkTip=Mathf.Max(maxLinkTip,Vector3.Distance(lastTip,blade.tip.position));
                        // Label changes after the previous pose is rendered. Compare that pose to the new stage at zero,
                        // rather than treating the preceding 33 ms of moving blade as a discontinuity.
                        Vector3 boundaryTip=blade.tip.position;
                        typeof(ValkyrMotionDriver).GetMethod("ApplyPose",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(motion,new object[]{0f,0f});
                        float jump=Vector3.Distance(boundaryTip,blade.tip.position);
                        Check(jump<.03f,$"{fps} FPS stage {last+1}->{stage+1} actual boundary pose delta={jump:F5} m");
                    }
                    if(stage<2&&!queued[stage]&&p.Melee.AttackElapsed>=.07f){Command(slash:true);queued[stage]=true;}else Command();
                    CheckFrame(!visual.WeaponObject.activeSelf,"rifle pose cannot overwrite melee arms");
                    maxGrip=Mathf.Max(maxGrip,motion.RightGripError);
                    chestTurn=Mathf.Max(chestTurn,Quaternion.Angle(initialChest,motion.Chest.rotation));
                    upperTurn=Mathf.Max(upperTurn,Quaternion.Angle(initialUpper,visual.upperR.rotation));
                    if(fps==60 && frames[stage]%3==0 && frames[stage]<=42)Capture($"combo_{stage+1}_{frames[stage]:00}.png");
                    last=stage;lastTip=blade.tip.position;
                }
                else if(last>=0)break;
                else Command();
                yield return null;
            }
            Check(frames.All(f=>f>0)&&!p.Melee.IsAttacking,$"{fps} FPS buffered fast-fast-heavy completes [{string.Join(",",frames)}]");
            Check(effects.HeavyReleaseBursts==releases+1,$"{fps} FPS exactly one heavy pressure release per combo");
            for(int fade=0;fade<fps/2;fade++){Command();yield return null;}
            Check(blade.TrailSamples==0,$"{fps} FPS slash afterimage clears after recovery");
            report.Add($"{fps} FPS maximum last moving-frame tip displacement at link={maxLinkTip:F3} m");
            Check(maxGrip<.03f&&chestTurn>35&&upperTurn>35,$"{fps} FPS full body power and hand contact: grip={maxGrip:F5}, chest={chestTurn:F1}, arm={upperTurn:F1}");
            report.Add($"COMBO {fps}FPS duration={Time.time-start:F3}s");
        }
        Time.captureDeltaTime=1f/60;Reset();
        foreach(int fps in new[]{30,60,120})
        {
            Time.captureDeltaTime=1f/fps;yield return null;Reset();
            float origin=p.transform.position.z;
            int frames=Mathf.RoundToInt(p.dashDuration*fps);
            for(int i=0;i<frames;i++){Command(dash:i==0,move:Vector2.up);yield return null;}
            float exitSpeed=p.Velocity.z;
            Check(Mathf.Abs(p.transform.position.z-origin-p.stats.DashDistance)<.08f,$"{fps} FPS running dash preserves configured distance: {p.transform.position.z-origin:F3}/{p.stats.DashDistance:F3}");
            Command(move:Vector2.up);yield return null;
            Check(exitSpeed>=p.stats.MoveSpeed*.9f&&Mathf.Abs(exitSpeed-p.Velocity.z)<8,$"{fps} FPS dash exits into movement without a stop: {exitSpeed:F2} -> {p.Velocity.z:F2} m/s");
        }
        Time.captureDeltaTime=1f/60;Reset();
        int shots=0;p.weaponController.BeamFired+=()=>shots++;
        for(int i=0;i<15;i++){Command(fire:true,move:Vector2.right);yield return null;}
        Check(shots>0&&p.transform.position.x>.25f,"moving rifle fire");
        Command(dash:true,fire:true,move:Vector2.up);yield return null;
        Check(p.IsDashing,"rifle to approach dash");
        Capture("grounded_dash.png");
        Command(slash:true);yield return null;
        for(int i=0;i<14;i++){Command();yield return null;}
        Check(p.Melee.IsAttacking,"slash buffered during dash draws and attacks");
        Vector3 before=p.transform.position;
        for(int i=0;i<6;i++){Command(move:Vector2.right);yield return null;}
        Check(p.transform.position.x-before.x>.10f,"melee retains directional movement");
        // Cancel separately from first and second stroke, then verify held fire resumes without release.
        foreach(int cancelStage in new[]{0,1})
        {
            Reset();Command(slash:true);yield return null;
            for(int i=0;i<60;i++)
            {
                if(p.Melee.IsAttacking&&p.Melee.ComboStage==cancelStage&&p.Melee.AttackElapsed>=.10f)break;
                Command(slash:p.Melee.IsAttacking&&p.Melee.ComboStage<cancelStage&&p.Melee.AttackElapsed>.06f);yield return null;
            }
            Check(p.Melee.ComboStage==cancelStage&&p.Melee.IsAttacking,"reached dash cancel stage "+cancelStage);
            Command(dash:true,fire:true,move:Vector2.right);yield return null;
            Check(!p.Melee.IsAttacking&&p.IsDashing&&!p.Melee.NextQueued,"dash cancels and clears combo "+cancelStage);
            int prior=shots;for(int i=0;i<24;i++){Command(fire:true);yield return null;}
            Check(shots>prior&&p.Stance.CanFire&&visual.WeaponObject.activeSelf,"dash back to held rifle fire "+cancelStage);
        }
        Reset();Command(slash:true);yield return null;
        for(int i=0;i<60;i++)
        {
            if(p.Melee.IsAttacking&&p.Melee.ComboStage==2)break;
            Command(slash:p.Melee.IsAttacking&&p.Melee.AttackElapsed>.07f);yield return null;
        }
        Check(p.Melee.ComboStage==2,"heavy entry");
        float energy=p.stats.CurrentEnergy;
        Check(p.TryDash(Vector2.right)&&!p.Melee.IsAttacking&&Mathf.Abs(energy-p.stats.CurrentEnergy-25)<.01f,"heavy windup accepts the authored dash cancel and spends energy once");
        Reset();Command(slash:true);yield return null;
        for(int i=0;i<60;i++)
        {
            if(p.Melee.IsAttacking&&p.Melee.ComboStage==2)break;
            Command(slash:p.Melee.IsAttacking&&p.Melee.AttackElapsed>.07f);yield return null;
        }
        for(int i=0;i<50;i++)
        {
            if(p.Melee.AttackElapsed>=p.Melee.CurrentStroke.contactEnd+.04f)break;
            Command();yield return null;
        }
        Command(dash:true);yield return null;
        for(int i=0;i<6;i++){Command();yield return null;}
        Check(p.IsDashing&&!p.Melee.IsAttacking,"heavy recovery accepts buffered dash");
        // Expired input must not produce a surprise attack after a long dash/cooldown.
        Reset();Command(slash:true);yield return null;
        for(int i=0;i<75;i++){Command();yield return null;}
        Check(!p.Melee.IsAttacking&&p.Melee.ComboStage==0,"single press does not auto-chain");
        Reset();float oldDash=p.dashDuration;p.dashDuration=.6f;
        Command(dash:true);yield return null;Command(slash:true);yield return null;
        for(int i=0;i<60;i++){Command();yield return null;}
        Check(!p.Melee.IsAttacking,"expired dash-to-slash input does not fire later");p.dashDuration=oldDash;
        // Actual swept collider hits, one hit per target per stroke, without an auto-aim lock.
        Reset();var dummies=new List<Damageable>();
        for(int i=0;i<12;i++)
        {
            float a=i*Mathf.PI/6;var go=GameObject.CreatePrimitive(PrimitiveType.Capsule);go.name="P0_DamageProbe";
            go.transform.position=new Vector3(Mathf.Sin(a)*3.2f,1,Mathf.Cos(a)*3.2f+1);
            var hp=go.AddComponent<Damageable>();hp.team=1;hp.destroyOnDeath=false;hp.RestoreLife(5000,5000);dummies.Add(hp);
        }
        yield return null;
        var hits=new Dictionary<string,int>();var amounts=new float[3];
        Action<Damageable,float> hit=(target,amount)=>{string key=p.Melee.ComboStage+"/"+target.GetInstanceID();hits[key]=hits.TryGetValue(key,out int n)?n+1:1;amounts[p.Melee.ComboStage]=amount;};
        p.Melee.StrikeHit+=hit;
        Command(slash:true);yield return null;
        int heavyContacts=effects.HeavyImpactBursts,priorStops=blade.ImpactStops;bool impactCaptured=false;
        for(int i=0;i<145;i++)
        {
            Command(slash:p.Melee.IsAttacking&&p.Melee.ComboStage<2&&p.Melee.AttackElapsed>.07f);yield return null;
            if(!impactCaptured&&effects.HeavyImpactBursts>heavyContacts){Capture("heavy_contact.png");impactCaptured=true;}
        }
        p.Melee.StrikeHit-=hit;
        Check(hits.Count>0&&hits.Values.All(n=>n==1),"actual blade sweep never double-hits one target within a stroke");
        Check(amounts.All(v=>v>0)&&amounts[2]>amounts[0]*1.5f,"three real contact windows and heavier finisher: "+string.Join(",",amounts));
        Check(impactCaptured,"heavy armor-contact effect triggered by actual damage");
        Check(blade.ImpactStops-priorStops<=3,"crowd sweep never restarts hitstop for each extra target");
        Capture("contact_result.png");foreach(var d in dummies)Destroy(d.gameObject);yield return null;
        Reset();var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.name="P0_Blocker";
        wall.transform.position=new Vector3(0,1.6f,1.5f);wall.transform.localScale=new Vector3(12,4,.3f);
        var blocked=GameObject.CreatePrimitive(PrimitiveType.Capsule);blocked.transform.position=new Vector3(0,1,3.2f);
        var blockedHp=blocked.AddComponent<Damageable>();blockedHp.team=1;blockedHp.RestoreLife(5000,5000);yield return null;
        Command(slash:true);yield return null;
        for(int i=0;i<40;i++){Command();yield return null;}
        Check(blockedHp.CurrentHealth==5000,"solid cover blocks actual blade damage");
        Command(dash:true,move:Vector2.up);yield return null;
        for(int i=0;i<20;i++){Command(move:Vector2.up);yield return null;}
        Check(p.transform.position.z<1.0f,"dash and melee advance respect CharacterController collision");
        Destroy(wall);Destroy(blocked);yield return null;
        // Production ordinary-input runs cover the new bounded short encounter and six-room loop.
        // Keep this suite focused on motion, collision, projectile and cancellation contracts.
        gm.EnterResult(false);gm.EnterHangar();p.Loadout.Select(PrimaryWeapon.M7);gm.BeginP0Combat();yield return null;
        Check(gm.IsCombatActive&&gm.upgradeSystem.Count==0,"restart restores clean shared combat rules");
    }
    void CheckFrame(bool ok,string message){if(!ok)throw new Exception(message);}
    void Capture(string name)
    {
        var cam=Camera.main;var rt=new RenderTexture(1600,900,24){antiAliasing=4};rt.Create();var old=cam.targetTexture;var active=RenderTexture.active;
        var canvases=new List<(Canvas canvas,Camera camera,float distance)>();
        bool ui=name.StartsWith("natural-")||name.StartsWith("v4-hangar")||name.StartsWith("v4-full-hud")||name.StartsWith("v4-result")||name.Contains("production");
        var tex=new Texture2D(1600,900,TextureFormat.RGB24,false);
        try
        {
            cam.targetTexture=rt;
            // Hidden batch Players have no presented backbuffer. Render production canvases offscreen
            // with the same camera/viewport, then restore their overlay settings in this frame.
            if(ui)foreach(var canvas in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
                if(canvas.isActiveAndEnabled&&canvas.renderMode==RenderMode.ScreenSpaceOverlay)
                {canvases.Add((canvas,canvas.worldCamera,canvas.planeDistance));canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=cam;canvas.planeDistance=1f;}
            Canvas.ForceUpdateCanvases();cam.Render();RenderTexture.active=rt;
            tex.ReadPixels(new Rect(0,0,1600,900),0,0);tex.Apply();File.WriteAllBytes(Path.Combine(output,name),tex.EncodeToPNG());
        }
        finally
        {
            foreach(var item in canvases){item.canvas.renderMode=RenderMode.ScreenSpaceOverlay;item.canvas.worldCamera=item.camera;item.canvas.planeDistance=item.distance;}
            cam.targetTexture=old;RenderTexture.active=active;Canvas.ForceUpdateCanvases();rt.Release();Destroy(rt);Destroy(tex);
        }
    }
    void Finish(string failure)
    {
        done=true;if(failure!=null)report.Add("FAIL "+failure);int code=failure==null&&errors==0?0:1;
        report.Add("P0_CHECK code="+code+" errors="+errors);File.WriteAllLines(Path.Combine(output,"quick-check.txt"),report);
        Application.logMessageReceived-=Log;Time.captureDeltaTime=0;Application.Quit(code);
    }
}
