using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.AI;

public static class CombatLabChecks
{
    public static IEnumerator Run(GameManager gm, PlayerController p, string output,
        Action<bool,string> check, Action<string> capture)
    {
        string profile = Environment.GetEnvironmentVariable("MECH_EQUIPMENT_PROFILE");
        string beforeProfile = File.Exists(profile) ? File.ReadAllText(profile) : null;
        var demo = new GameObject("CombatLabCheck").AddComponent<P0CombatDemo>();
        for(int i=0;i<5;i++)yield return null;
        p.enabled=false;p.InputRouter.readKeyboard=false;
        var outcomes=new List<CombatLabTelemetry.LabSummary>();
        float baselineHealth=0;
        try
        {
            foreach(CombatLabVariant variant in Enum.GetValues(typeof(CombatLabVariant)))
            {
                demo.StartLab(variant);
                // Production Elite in an isolated state-machine fixture. Full ordinary-input rounds follow below.
                demo.enabled=false;
                var elite=gm.stageManager.enemySpawner.SpawnEnemy(EnemyKind.Elite,new Vector3(0,0,6));
                elite.TrainingTarget=true;elite.enabled=false;elite.GetComponent<NavMeshAgent>().enabled=false;
                elite.transform.rotation=Quaternion.Euler(0,180,0);
                var hp=elite.GetComponent<Damageable>();hp.destroyOnDeath=false;hp.RestoreLife(5000,5000);
                yield return null;
                int fractures=ArmorContactVfx.Get().Fractures;
                for(int i=0;i<3;i++)
                    hp.TakeDamage(60,new DamageInfo(p.gameObject,p.transform.position,p.GetComponent<Damageable>(),60)
                        { Impact=105, Kind=CombatHitKind.HeavyRifle, DirectHitMultiplier=1.35f });
                hp.TakeDamage(20,new DamageInfo(p.gameObject,p.transform.position,p.GetComponent<Damageable>(),20)
                    { Impact=42, Kind=CombatHitKind.Melee, MeleeStrike=true });
                check(hp.CurrentHealth<5000,"real Elite receives damage in "+variant);
                if(variant==CombatLabVariant.BasicFeedback)
                {
                    baselineHealth=hp.CurrentHealth;
                    check(!elite.Armor.Intact&&!elite.GuardActive,"minimal feedback retains finite armor damage");
                    check(ArmorContactVfx.Get().Fractures==fractures,"minimal feedback suppresses decorative fracture");
                }
                else
                {
                    check(Mathf.Abs(hp.CurrentHealth-baselineHealth)<.001f&&!elite.Armor.Intact,"all feedback variants have identical armor and health rules");
                    check(ArmorContactVfx.Get().Fractures>fractures,"full local feedback emits armor fracture");
                }
                check(gm.equipmentLoop.AbsorbNearby(true)==0,"lab cannot acquire permanent equipment in "+variant);
                {
                    foreach(int fps in new[]{30,60,120})
                    {
                        Time.captureDeltaTime=1f/fps;p.RestoreAt(new Vector3(0,.1f,-4));yield return null;
                        for(int i=0;i<12&&!p.Melee.IsAttacking;i++)
                        {
                            p.Simulate(new PlayerCommand{Melee=i==0,HasAim=true,AimPoint=new Vector3(0,1,18)},Time.deltaTime);
                            yield return null;
                        }
                        check(p.Melee.IsAttacking,"live blade available for hit-stop clock probe at "+fps);
                        p.Melee.HoldImpact(.095f);int heldFrames=0;float largestUnscaled=0;
                        while(p.Melee.ImpactHeld&&heldFrames<30)
                        {
                            // Deliberate render workload in this isolated fixture, never in ordinary-input rounds.
                            System.Threading.Thread.Sleep(25);yield return null;heldFrames++;
                            largestUnscaled=Mathf.Max(largestUnscaled,Time.unscaledDeltaTime);
                        }
                        File.AppendAllText(Path.Combine(output,"lab-timing.txt"),FormattableString.Invariant($"variant={variant} fps={fps} holdFrames={heldFrames} maxUnscaled={largestUnscaled:F6} fixedStep={Time.captureDeltaTime:F6}\n"));
                        check(heldFrames==Mathf.CeilToInt(.095f*fps),"hit-stop follows combat simulation despite capture workload at "+fps+" in "+variant);
                    }
                    Time.captureDeltaTime=1f/60;p.CancelMovement();yield return null;
                }
                demo.enabled=true;demo.StartLab(variant);
                for(int i=0;i<100;i++)yield return null;
                var roles=new HashSet<EnemyKind>();
                foreach(var enemy in UnityEngine.Object.FindObjectsByType<EnemyBase>(FindObjectsSortMode.None))
                {
                    roles.Add(enemy.kind);
                    check(enemy.GetComponent<SalvageCarrier>()==null,"lab actor has no collectible carrier in "+variant);
                }
                check(roles.Count==3&&roles.Contains(EnemyKind.Melee)&&roles.Contains(EnemyKind.Ranged)&&roles.Contains(EnemyKind.Elite),
                    "same three production enemy roles spawn in "+variant);
                check(demo.LabGroups==1&&gm.upgradeSystem.Count==0&&CombatSliceSettings.ViewIndex==2,"same zero-upgrade start and C camera in "+variant);
                float pausedAt=demo.Elapsed;gm.SetPaused(true);
                for(int i=0;i<8;i++)yield return null;
                check(demo.Elapsed==pausedAt&&CombatLabTelemetry.Recording,"pause preserves round time and recording in "+variant);
                gm.SetPaused(false);
                int peak=0;
                var native=Array.IndexOf(Environment.GetCommandLineArgs(),"-combatLabReplay")>=0
                    ?new NativeCapture(Path.Combine(output,"capture-"+variant)):null;
                try
                {
                for(int frame=0;frame<1900&&!demo.Finished;frame++)
                {
                    // Public commands only: no immunity, healing, forced damage, buffs or teleporting.
                    Damageable target=null;float distance=float.MaxValue;
                    foreach(var actor in Damageable.Active)
                    {
                        if(actor==null||actor.team==0||actor.IsDead)continue;
                        float d=Vector3.Distance(p.transform.position,actor.transform.position);
                        if(d<distance){distance=d;target=actor;}
                    }
                    Vector3 waypoint=new Vector3(Mathf.Sin(demo.Elapsed*.31f)*4,0,Mathf.Cos(demo.Elapsed*.31f)*3);
                    Vector3 move=waypoint-p.transform.position;move.y=0;move.Normalize();
                    bool slash=target!=null&&distance<4.3f&&frame%22==0;
                    if(p.Melee.IsAttacking&&p.Melee.ComboStage<2&&p.Melee.AttackElapsed>.07f&&!p.Melee.NextQueued)slash=true;
                    p.Simulate(new PlayerCommand { Move=new Vector2(move.x,move.z), HasAim=true,
                        AimPoint=target!=null?target.AimCenter:new Vector3(0,1,10), Fire=target!=null,
                        Melee=slash, Dash=frame%110==0, Skill=target!=null&&distance<12&&p.weaponController.SkillCooldownRemaining<=0 },Time.deltaTime);
                    yield return null;
                    native?.Frame(frame);
                    peak=Mathf.Max(peak,gm.stageManager.EnemiesAlive);
                    if(frame==300)capture("lab-"+variant+".png");
                }
                }
                finally { native?.Dispose(); }
                var s=CombatLabTelemetry.LastSummary;
                check(demo.Finished&&!CombatLabTelemetry.Recording&&s!=null&&s.variant==variant.ToString(),"round finishes and flushes its own recording in "+variant);
                check(s.reason=="time_limit"&&Mathf.Abs(s.activeSeconds-30)<.001f,"ordinary inputs survive complete 30-second round in "+variant);
                check(s.shots>0&&s.hits>0&&s.dashes>0&&s.salvos>0,"actual weapon/movement/contact events recorded in "+variant);
                check(s.directHits==0,"feedback variants never enable the retired pressure/direct-hit mechanic in "+variant);
                check(peak<=3&&gm.upgradeSystem.Count==0&&gm.equipmentLoop.Pickups.Count==0,"round stays within three enemies and zero progression in "+variant);
                check(s.fixedSimulationStep&&s.scriptedInputs&&s.batchMode&&!s.inputToPhotonMeasured&&!s.audioOnsetMeasured,"synthetic run cannot be mistaken for physical latency evidence in "+variant);
                check(s.droppedEvents==0&&s.droppedFrames==0&&s.frameRows>900,"bounded telemetry retains the complete short run in "+variant);
                check(CombatLabTelemetry.LastError==null&&File.Exists(Path.Combine(CombatLabTelemetry.LastOutput,"summary.json"))&&
                    File.ReadAllLines(Path.Combine(CombatLabTelemetry.LastOutput,"events.csv")).Length==s.eventRows+1,
                    "CSV and summary saved without truncated events in "+variant);
                outcomes.Add(s);
            }
            File.WriteAllText(Path.Combine(output,"lab-rounds.json"),JsonUtility.ToJson(new RoundResults{rounds=outcomes.ToArray()},true));
            // A policy reacts to moving targets; separate Unity runs are not lockstep replays.
            // Compare A/B rules with the controlled HP/window/clock fixtures above, and retain
            // all ordinary-run outcomes for analysis instead of asserting identical kill totals.
            demo.StartLab(CombatLabVariant.FullFeedback);
            // Isolated entry fixture: camping the previous Elite position cannot stall the next wave.
            p.RestoreAt(new Vector3(0,.1f,10));
            for(int i=0;i<100;i++)yield return null;
            var opposite=UnityEngine.Object.FindObjectsByType<EnemyBase>(FindObjectsSortMode.None);
            check(opposite.Length==3&&Array.TrueForAll(opposite,e=>e.transform.position.z<0),
                "occupied entry moves the triad to the opposite side without stalling or spawning on the player");
            demo.Restart();yield return null;
            p.weaponController.TryFireBeam();
            demo.Restart();yield return null;
            check(UnityEngine.Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None).Length==0,"restart clears shots from previous round");
            p.GetComponent<Damageable>().TakeDamage(99999,new DamageInfo(null,Vector3.zero,null,99999));
            yield return null;yield return null;
            check(demo.Finished&&CombatLabTelemetry.LastSummary.reason=="down"&&!CombatLabTelemetry.Recording,"death ends and flushes a lab round");
            demo.StartLab(CombatLabVariant.NoCameraShake);yield return null;
            check(CombatLabSettings.NoCameraShake&&CombatLabTelemetry.Recording,"restart after death opens the camera-steady variant");
            demo.LeaveLab();yield return null;
            check(!CombatLabSettings.Active&&!CombatLabTelemetry.Recording,"exit restores full feedback and detaches recorder");
            demo.enabled=false;
            var restored=gm.stageManager.enemySpawner.SpawnEnemy(EnemyKind.Elite,new Vector3(0,0,6));
            restored.enabled=false;
            restored.GetComponent<Damageable>().ApplyDamage(restored.Armor.Current,new DamageInfo(p.gameObject,p.transform.position,p.GetComponent<Damageable>(),999));
            check(!restored.Armor.Intact,"normal combat depletes armor after leaving camera-steady variant");
            gm.stageManager.StopStage();
            check(beforeProfile==(File.Exists(profile)?File.ReadAllText(profile):null),"A/B/C, death, restart and exit leave the persistent profile byte-for-byte unchanged");
            File.WriteAllText(Path.Combine(output,"lab-rounds.json"),JsonUtility.ToJson(new RoundResults{rounds=outcomes.ToArray()},true));
        }
        finally
        {
            gm.SetPaused(false);
            CombatLabTelemetry.End("test_cleanup",demo!=null?demo.Elapsed:0);
            CombatLabSettings.Exit();
            if(demo!=null)UnityEngine.Object.Destroy(demo.gameObject);
        }
    }
    [Serializable] sealed class RoundResults { public CombatLabTelemetry.LabSummary[] rounds; }

    // Offline Unity renderer/mixer capture only. No visible Player window or speaker output.
    sealed class NativeCapture : IDisposable
    {
        readonly string directory;
        readonly PlayerAudioCapture audio;
        readonly Camera camera;
        readonly RenderTexture render;
        readonly Texture2D pixels;
        int frames;
        public NativeCapture(string path)
        {
            directory=path;Directory.CreateDirectory(directory);
            camera=Camera.main;render=new RenderTexture(960,540,24){antiAliasing=2};render.Create();
            pixels=new Texture2D(960,540,TextureFormat.RGB24,false);
            audio=new PlayerAudioCapture();AudioListener.volume=1;AudioListener.pause=false;
        }
        public void Frame(int simulationFrame)
        {
            audio.Advance(simulationFrame%2==1);
            if(simulationFrame%2==0)return;
            var previous=camera.targetTexture;var active=RenderTexture.active;
            try
            {
                camera.targetTexture=render;camera.Render();RenderTexture.active=render;
                pixels.ReadPixels(new Rect(0,0,960,540),0,0);pixels.Apply();
                File.WriteAllBytes(Path.Combine(directory,"frame_"+frames.ToString("D4")+".png"),pixels.EncodeToPNG());frames++;
            }
            finally { camera.targetTexture=previous;RenderTexture.active=active; }
        }
        public void Dispose()
        {
            audio.Save(Path.Combine(directory,"game-mix.wav"));AudioListener.volume=0;
            render.Release();UnityEngine.Object.Destroy(render);UnityEngine.Object.Destroy(pixels);
            File.WriteAllText(Path.Combine(directory,"capture.txt"),"Frames="+frames+"\nSeconds="+(frames/30f)+"\nAudioPeak="+audio.Peak+
                "\nNative 30fps render of 60Hz public-command replay from round time 1.67s, after initial spawn and pause check. No HUD, physical latency claim or human feel approval.\n");
        }
    }
}
