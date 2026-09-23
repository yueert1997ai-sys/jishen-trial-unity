using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.AI;

// Ordinary command replay, without injected damage, healing, invulnerability, or spawn suppression.
// It measures a repeatable control policy, not human combat feel or presented frame rate.
public static class RebuildPlayChecks
{
    enum Style { Rifle, Blade, Mixed }
    static readonly NavMeshPath route=new NavMeshPath();
    static Vector3 Route(Vector3 from,Vector3 destination,Vector3 fallback)
    {
        if(!NavMesh.SamplePosition(from,out var a,2,NavMesh.AllAreas)||!NavMesh.SamplePosition(destination,out var b,3,NavMesh.AllAreas)
            || !NavMesh.CalculatePath(a.position,b.position,NavMesh.AllAreas,route)||route.status!=NavMeshPathStatus.PathComplete)return fallback;
        foreach(var corner in route.corners)
        {var delta=corner-from;delta.y=0;if(delta.sqrMagnitude>.8f*.8f)return delta.normalized;}
        return fallback;
    }
    static bool Covered(Vector3 from,Damageable target)
    {
        var delta=target.AimCenter-from;
        foreach(var hit in Physics.RaycastAll(from,delta.normalized,delta.magnitude,~0,QueryTriggerInteraction.Ignore))
            if(hit.collider.GetComponentInParent<Damageable>()==null)return true;
        return false;
    }
    // Respond to the same visible warning fills the player sees. The previous
    // replay never dodged ordinary enemies, even during a stationary combo.
    static Vector3 WarningDodge(PlayerController p,bool closeIn)
    {
        Vector3 position=p.transform.position;Damageable threat=null;float closest=6;
        // Dodge a visible incoming packet at close range, rather than spending
        // the dash a second before a distant burst actually reaches the player.
        foreach(var shot in UnityEngine.Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None))
        {
            if(shot.team==0||shot.speed<=0)continue;
            Vector3 delta=Vector3.ProjectOnPlane(position-shot.transform.position,Vector3.up);
            float along=Vector3.Dot(delta,shot.direction);
            if(along<=0||along/shot.speed>.30f||(delta-shot.direction*along).sqrMagnitude>1.1f)continue;
            return DodgeSide(p,shot.direction,closeIn);
        }
        foreach(var actor in Damageable.Active)
        {
            if(actor==null||actor.team==0||actor.IsDead)continue;
            var enemy=actor.GetComponent<EnemyBase>();
            if(enemy==null||!enemy.HasAttackWarning||enemy.AttackWindup<.62f)continue;
            float distance=Vector3.Distance(position,actor.transform.position);
            if(distance>=closest||enemy.kind==EnemyKind.Melee&&distance>6
                ||Covered(position+Vector3.up*1.2f,actor))continue;
            closest=distance;threat=actor;
        }
        if(threat==null)return Vector3.zero;
        Vector3 direction=Vector3.ProjectOnPlane(position-threat.transform.position,Vector3.up).normalized;
        return DodgeSide(p,direction,closeIn);
    }
    static Vector3 DodgeSide(PlayerController p,Vector3 direction,bool closeIn)
    {
        Vector3 position=p.transform.position;
        Vector3 side=Vector3.Cross(Vector3.up,direction),best=side;float score=float.NegativeInfinity;
        foreach(float sign in new[]{1f,-1f})
        {
            Vector3 candidate=(side*sign+(closeIn?-direction*.7f:direction*.2f)).normalized;
            Vector3 destination=position+candidate*p.stats.DashDistance;
            if(!NavMesh.SamplePosition(destination,out var point,.5f,NavMesh.AllAreas)
                ||NavMesh.Raycast(position,point.position,out _,NavMesh.AllAreas))continue;
            Vector3 goal=closeIn&&p.HasAimPoint?p.AimPoint:Vector3.zero;
            float s=-Vector3.ProjectOnPlane(point.position-goal,Vector3.up).sqrMagnitude;
            if(s>score){score=s;best=candidate;}
        }
        return best;
    }
    public static IEnumerator Run(GameManager gm,PlayerController p,string output,Action<bool,string> check,Action<string> capture)
    {
        bool full=CombatRuntime.HasArgument("-fullPlayCheck");
        bool mix=CombatRuntime.HasArgument("-playMixCapture");
        var styles=full?new[]{Style.Mixed}:new[]{Style.Rifle,Style.Blade,Style.Mixed};
        var summaries=new List<string>();
        foreach(var style in styles)
        {
            Time.captureDeltaTime=1f/60;gm.ExitPractice();yield return null;
            p.Loadout.Select(PrimaryWeapon.M7);P0CombatDemo demo=null;
            if(full)gm.BeginFullDemo();else
            {
                gm.BeginShortCombat();for(int i=0;i<5;i++)yield return null;
                demo=UnityEngine.Object.FindFirstObjectByType<P0CombatDemo>();demo.Restart();
            }
            p.enabled=false;p.InputRouter.readKeyboard=false;
            // Approach according to the equipped physical blade. The old policy
            // started four metres away even for J01's shorter design-sheet sword.
            float physicalReach=p.GetComponentInChildren<RaikenBladePresentation>()?.Reach??3.3f;
            float slashDistance=Mathf.Clamp(physicalReach+.9f,2.3f,4.1f);
            float bladeStandOff=Mathf.Min(2.5f,slashDistance-.8f);
            float approachMargin=Mathf.Min(1,slashDistance*.22f);
            float start=Time.time,nextDash=0,nextPicture=12,lowestHp=p.stats.CurrentHp,bossStart=-1,clearAt=-1,gun=0,blade=0;
            int shots=0,blades=0,armorBreaks=0,maxOccupied=0,rewardCount=0,dashes=0;
            bool installed=false,sawCore=false;float soundPeak=0;
            var seen=new HashSet<Damageable>();
            var trace=new List<string>{"seconds,encounter,hp,kills,occupied,x,z,distance,stance,bladeStage,bossHealth"};
            Action fired=()=>shots++;p.weaponController.BeamFired+=fired;
            Action<Vector3> dashed=_=>dashes++;p.Dashed+=dashed;
            PlayerAudioCapture audio=null;
            if(mix&&style==Style.Mixed){audio=new PlayerAudioCapture();AudioListener.volume=1;}
            try
            {
                for(int frame=0;frame<(full?1200:240)*60&&gm.Phase!=GamePhase.Result;frame++)
                {
                    float elapsed=Time.time-start;
                    foreach(var actor in Damageable.Active)
                    {
                        if(actor==null||actor.team==0||!seen.Add(actor))continue;
                        actor.OnResolved+=(target,result)=>
                        {
                            if(!result.Applied)return;
                            float amount=result.HealthDamage+result.ArmorDamage;
                            if(result.ToDamageInfo()?.MeleeStrike==true){blade+=amount;blades++;}else gun+=amount;
                            if(result.BrokeArmor)armorBreaks++;
                        };
                    }
                    if(gm.Phase==GamePhase.Reward)
                    {
                        var choices=gm.upgradeSystem.GenerateOptions();
                        // A stable mix of blade/rifle/mobility upgrades, using the same offered cards as the UI.
                        var pick=choices[0];
                        foreach(var option in choices)if((rewardCount%2==0&&option.kind==RunUpgradeKind.BladeTempo)||(rewardCount%2==1&&option.kind==RunUpgradeKind.FireRate)){pick=option;break;}
                        check(gm.upgradeSystem.ApplyOption(pick),"ordinary run selects one eligible reward "+(rewardCount+1));rewardCount++;gm.FinishReward();clearAt=-1;
                    }
                    Damageable nearest=null;float distance=float.MaxValue;
                    BossController boss=full?gm.stageManager.Director?.Boss:demo?.SliceBoss;
                    if(boss!=null){if(bossStart<0)bossStart=elapsed;sawCore|=boss.CoreExposed;}
                    foreach(var actor in Damageable.Active)
                    {
                        if(actor==null||actor.team==0||actor.IsDead)continue;
                        float d=Vector3.Distance(p.transform.position,actor.transform.position);
                        float score=d+(actor.GetComponent<BossController>()!=null&&!boss.CoreExposed?3:0);
                        if(score<distance){distance=score;nearest=actor;}
                    }
                    Vector3 toward=nearest!=null?nearest.transform.position-p.transform.position:Vector3.zero;toward.y=0;
                    distance=toward.magnitude;toward.Normalize();
                    Vector3 lateral=Vector3.Cross(Vector3.up,toward);
                    bool covered=nearest!=null&&Covered(p.transform.position+Vector3.up*1.2f,nearest);
                    bool useBlade=style==Style.Blade || style==Style.Mixed&&(boss==null?distance<slashDistance+.4f:boss.CoreExposed);
                    float desired=useBlade||style==Style.Mixed&&boss==null?bladeStandOff:7.5f;
                    float margin=useBlade||style==Style.Mixed&&boss==null?approachMargin:1;
                    Vector3 movement=nearest==null?Vector3.zero:distance>desired+margin?toward:distance<desired-.7f?-toward:lateral*.6f;
                    if(p.Melee.IsAttacking&&useBlade&&distance<slashDistance-.3f)movement=lateral*.55f;
                    Vector3 position=p.transform.position;
                    if(nearest!=null&&(distance>desired+margin||covered))movement=Route(position,nearest.transform.position,toward);
                    else if(movement.sqrMagnitude>.1f)movement=Route(position,position+movement*3,movement);
                    Vector3 horizontal=new Vector3(position.x,0,position.z);
                    if(horizontal.magnitude>17 && Vector3.Dot(movement,horizontal)>0)movement=(movement-horizontal.normalized*1.7f).normalized;
                    bool slash=nearest!=null&&useBlade&&distance<slashDistance&&!p.Melee.IsAttacking;
                    if(nearest!=null&&distance<slashDistance+.8f&&useBlade&&p.Melee.IsAttacking&&p.Melee.ComboStage<2&&p.Melee.AttackElapsed>.10f&&!p.Melee.NextQueued)slash=true;
                    bool finishCut=useBlade&&distance<slashDistance+.3f&&p.Melee.IsAttacking&&!p.Melee.CanSheathe;
                    Vector3 evade=!finishCut&&Time.time>=nextDash&&p.IsDashReady&&p.stats.CurrentEnergy>=25?WarningDodge(p,useBlade):Vector3.zero;
                    bool dash=nearest!=null&&Time.time>=nextDash&&p.IsDashReady&&p.stats.CurrentEnergy>=25
                        && (evade.sqrMagnitude>.1f || style==Style.Blade&&distance>7&&distance<11 || boss!=null&&boss.AttackWindup>.60f);
                    if(dash)
                    {
                        if(evade.sqrMagnitude>.1f)movement=evade;
                        else if(boss!=null&&boss.AttackWindup>.60f)movement=lateral;
                        slash=false;nextDash=Time.time+.65f;
                    }
                    var intake=gm.equipmentLoop.Absorption;
                    if(gm.AwaitingContinue || !full&&gm.stageManager.EnemiesAlive==0&&intake.Offering)
                    {
                        if(clearAt<0)clearAt=Time.time;
                        if(full&&intake.Pickup!=null&&!intake.Installed)
                        {
                            Vector3 approach=intake.Pickup.transform.position-position;approach.y=0;
                            if(intake.CanAbsorb)intake.TryBegin();else if(!intake.Busy)movement=Route(position,intake.Pickup.transform.position,approach.normalized);
                            dash=false;slash=false;
                        }
                        else if(!intake.Busy&&Time.time-clearAt>1.2f)
                        {
                            if(full)gm.ContinueAfterSalvage();else intake.CollectWithoutInstalling();clearAt=-1;
                        }
                    }
                    installed|=intake.Installed;
                    bool fire=nearest!=null&&style!=Style.Blade&&!useBlade;
                    p.Simulate(new PlayerCommand{HasAim=nearest!=null,AimPoint=nearest!=null?nearest.AimCenter:position+Vector3.forward*10,
                        Move=new Vector2(movement.x,movement.z),Melee=slash,Fire=fire,Dash=dash,
                        Skill=full&&nearest!=null&&p.weaponController.SkillCooldownRemaining<=0},Time.deltaTime);
                    yield return null;
                    if(audio!=null)audio.Advance(frame%2==1);
                    lowestHp=Mathf.Min(lowestHp,p.stats.CurrentHp);maxOccupied=Mathf.Max(maxOccupied,gm.stageManager.EnemiesAlive);
                    if(frame%30==0)
                    {
                        trace.Add(FormattableString.Invariant($"{elapsed:F2},{gm.stageManager.CurrentEncounter},{p.stats.CurrentHp:F2},{gm.Kills},{gm.stageManager.EnemiesAlive},{p.transform.position.x:F2},{p.transform.position.z:F2},{distance:F2},{p.Stance.State},{p.Melee.ComboStage},{(boss!=null?boss.GetComponent<Damageable>().CurrentHealth:0):F1}"));
                        File.WriteAllLines(Path.Combine(output,"natural-"+style+".csv"),trace);
                    }
                    if(elapsed>=nextPicture){capture("natural-"+style+"-"+(int)elapsed+"s.png");nextPicture+=full?60:20;}
                }
                if(audio!=null){soundPeak=audio.Peak;audio.Save(Path.Combine(output,"natural-mix.wav"));audio=null;}
                if(full)File.WriteAllText(Path.Combine(output,"encounter-seconds.json"),"["+string.Join(",",Array.ConvertAll(gm.stageManager.EncounterSeconds,v=>v.ToString("F3",System.Globalization.CultureInfo.InvariantCulture)))+"]");
                var rescue=p.GetComponent<CombatRecovery>();
                string summary=FormattableString.Invariant($"{style}: seed={CombatRuntime.Run.Seed} full={full} victory={gm.LastResultVictory&&gm.Phase==GamePhase.Result} seconds={Time.time-start:F2} hp={p.stats.CurrentHp:F2} lowestHP={lowestHp:F2} kills={gm.Kills} shots={shots} bladeContacts={blades} gunDamage={gun:F1} bladeDamage={blade:F1} armorBreaks={armorBreaks} maxOccupied={maxOccupied} rewards={rewardCount} installed={installed} bossSeconds={(bossStart<0?0:Time.time-start-bossStart):F2} peak={soundPeak:F4} playerRescues={rescue.PlayerRecoveries} enemyRescues={rescue.EnemyRecoveries} dashes={dashes}");
                summaries.Add(summary);File.WriteAllLines(Path.Combine(output,"natural-results.txt"),summaries);
                File.AppendAllText(Path.Combine(output,"natural-results.txt"),"\nOrdinary PlayerCommand inputs only; no direct health edits, invulnerability, forced kills, timed waits to pad combat, or spawn suppression. Not human feel approval.\n");
                check(gm.Phase==GamePhase.Result&&gm.LastResultVictory&&p.stats.CurrentHp>0,"natural control policy completes "+summary);
                check(maxOccupied<=CombatRules.Current.MaxHostiles,"ordinary run respects live and reserved enemy budget");
                if(!full)check(gm.upgradeSystem.Count==0,"base-gear trial has zero upgrades");
                if(style==Style.Rifle)check(gun>0&&blade==0,"rifle-only run needs no blade execution");
                if(style==Style.Blade)check(blade>0&&gun==0,"blade-only run needs no gun pressure");
                if(style==Style.Mixed)check(blade>0&&gun>0,"mixed run lands actual gun and blade contacts");
                if(full)check(rewardCount==6&&installed&&sawCore,"natural full run installs E-01, chooses six upgrades and uses Boss openings");
                if(mix&&style==Style.Mixed)check(soundPeak>.02f&&soundPeak<.95f,"continuous native combat mixer has signal and unclipped headroom");
            }
            finally
            {
                p.weaponController.BeamFired-=fired;
                p.Dashed-=dashed;
                if(audio!=null)audio.Save(Path.Combine(output,"natural-mix-incomplete.wav"));
                gm.ExitPractice();
            }
            yield return null;
        }
    }
}
