using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public static class NemesisMotionChecks
{
    public static IEnumerator Run(GameManager gm,PlayerController p,string output,Action<bool,string> check,Action<string> capture)
    {
        var rig=p.GetComponentInChildren<NemesisMotionRig>();var driver=p.GetComponentInChildren<ValkyrMotionDriver>();var visual=p.GetComponentInChildren<LoadoutVisual>();var blade=p.GetComponentInChildren<RaikenBladePresentation>();
        check(rig!=null&&rig.sourceSha256=="d0a86ccc942a6edb18f0a2c24b3083db88e35daa701058667549675e5d33f8c9","extracted R07 NEMESIS source with the corrected original eye surfaces is the actual player");
        var joints=rig.GetComponentsInChildren<Transform>(true).GroupBy(t=>t.name).ToDictionary(g=>g.Key,g=>g.First());
        var cam=Camera.main;var follow=cam.GetComponent<CameraFollow>();
        void View(Vector3 from,Vector3 at,float size=2.5f)
        {follow.enabled=false;cam.orthographic=true;cam.orthographicSize=size;cam.transform.position=p.transform.TransformPoint(from);cam.transform.LookAt(p.transform.TransformPoint(at));}
        void Command(Vector2 move=default,bool slash=false,bool fire=false,bool dash=false,bool boost=false)
        {p.Simulate(new PlayerCommand{Move=move,Melee=slash,Fire=fire,Dash=dash,BoostHeld=boost,HasAim=true,AimPoint=p.transform.position+Vector3.forward*16},Time.deltaTime);}
        gm.EnterResult(false);gm.EnterHangar();for(int i=0;i<12;i++)yield return null;
        p.enabled=false;p.Loadout.Select(PrimaryWeapon.None);
        for(int i=0;i<8;i++)yield return null;
        foreach(var side in new[]{"L","R"})
        {
            float sign=side=="R"?1:-1;
            var hand=joints["Hand."+side];var foot=joints["Foot."+side];
            float palm=Vector3.Dot(hand.forward,-sign*p.transform.right);
            var toe=p.transform.InverseTransformDirection(foot.forward);
            float yaw=Mathf.Atan2(toe.x,toe.z)*Mathf.Rad2Deg;
            check(palm>.90f,$"{side} neutral palm faces inward, back outward: dot={palm:F3}");
            check(sign*yaw>8&&sign*yaw<18,$"{side} neutral toe-out angle={yaw:F2} deg");
        }
        View(new Vector3(0,2.7f,9),new Vector3(0,1.9f,0),2.3f);capture("01-neutral-front.png");
        View(new Vector3(6,3.4f,9),new Vector3(0,1.9f,0),2.3f);capture("02-neutral-quarter.png");
        View(new Vector3(3,1.4f,7),new Vector3(.50f,1.1f,.12f),1.25f);capture("03-hands-feet.png");
        p.Loadout.Select(PrimaryWeapon.M7);for(int i=0;i<10;i++)yield return null;
        View(new Vector3(6,3.4f,9),new Vector3(0,1.9f,0),2.5f);capture("04-rifle-carry.png");
        check(visual.RightGripError<.025f,"rifle safe-carry grip remains seated: "+visual.RightGripError);
        gm.BeginWeaponTrial();for(int i=0;i<15;i++)yield return null;
        var trial=UnityEngine.Object.FindFirstObjectByType<WeaponTrial>();trial.ClearTargets();
        p.enabled=false;p.InputRouter.readKeyboard=false;p.RestoreAt(new Vector3(0,.1f,0));
        for(int i=0;i<15;i++){Command();yield return null;}
        View(new Vector3(6,3.4f,9),new Vector3(0,1.9f,.2f),2.5f);capture("05-rifle-aim.png");
        View(new Vector3(-6,3.4f,9),new Vector3(0,2.1f,.5f),2.5f);capture("05-rifle-open-shoulder.png");
        View(new Vector3(0,3.1f,10),new Vector3(0,2.1f,.5f),2.5f);capture("05-rifle-front.png");
        check(visual.RightGripError<.025f,"rifle combat grip remains seated: "+visual.RightGripError);
        float armExtension=Vector3.Angle(visual.upperR.position-visual.lowerR.position,visual.handR.position-visual.lowerR.position);
        check(armExtension>140,"reference rifle gesture extends the arm with a relaxed elbow: "+armExtension);
        float maxGrip=0,minFeet=1,maxFeet=-1;
        for(int direction=0;direction<8;direction++)
        {
            p.RestoreAt(new Vector3(0,.1f,0));float a=direction*Mathf.PI/4;var move=new Vector2(Mathf.Sin(a),Mathf.Cos(a));
            for(int i=0;i<30;i++)
            {
                Command(move,fire:i%8==0);yield return null;
                maxGrip=Mathf.Max(maxGrip,visual.RightGripError);minFeet=Mathf.Min(minFeet,driver.FeetAboveGround);maxFeet=Mathf.Max(maxFeet,driver.FeetAboveGround);
                if(i==12||i==22){View(new Vector3(6,3.4f,9),new Vector3(0,1.9f,0),2.55f);capture($"run-{direction}-{i}.png");}
            }
        }
        check(maxGrip<.035f,"eight-direction running rifle stays in hand: "+maxGrip);
        check(minFeet>-.06f,"running soles do not sink into floor: "+minFeet);
        p.RestoreAt(new Vector3(0,.1f,0));for(int i=0;i<20;i++){Command();yield return null;}
        var afterimage=rig.GetComponent<NemesisAfterimage>();float maxWing=0,maxFlight=0;int maxGhost=0;
        for(int i=0;i<22;i++)
        {
            Command(Vector2.up,dash:i==0);yield return null;
            maxWing=Mathf.Max(maxWing,rig.WingOpen);maxFlight=Mathf.Max(maxFlight,driver.FlightBlend);maxGhost=Mathf.Max(maxGhost,afterimage.ActiveCount);
            if(i==4||i==10||i==18){View(new Vector3(6,3.4f,9),new Vector3(0,1.9f,0),2.6f);capture($"dash-{i}.png");}
        }
        check(maxWing>.9f&&maxGhost>=2&&afterimage.Supported&&maxFlight>.9f,$"Space dash opens both wings and leaves supported purple pose afterimages: wing={maxWing:F2} flight={maxFlight:F2} ghosts={maxGhost}");
        for(int i=0;i<24;i++){Command(Vector2.up,boost:true);yield return null;}
        View(new Vector3(5,4,-10),new Vector3(0,2,-.2f),3.8f);capture("dash-wings-rear.png");
        check(rig.WingOpen>.95f&&afterimage.ActiveCount>=2,"held Space sustains wing spread and bounded afterimages");
        gm.SetPaused(true);int frozenGhosts=afterimage.ActiveCount;float frozenWing=rig.WingOpen;
        for(int i=0;i<6;i++)yield return null;
        check(afterimage.ActiveCount==frozenGhosts&&rig.WingOpen==frozenWing,"pause freezes wing motion and afterimage lifetimes");gm.SetPaused(false);
        for(int i=0;i<55;i++){Command();yield return null;}
        check(rig.WingOpen==0&&afterimage.ActiveCount==0,"released boost folds wings and clears afterimages");
        for(int fpsIndex=0;fpsIndex<3;fpsIndex++)
        {
            int fps=new[]{30,60,120}[fpsIndex];Time.captureDeltaTime=1f/fps;p.RestoreAt(new Vector3(0,.1f,0));
            for(int i=0;i<fps/3;i++){Command();yield return null;}
            var counts=new int[3];var queued=new bool[3];float gripError=0;int previous=-1;
            Command(slash:true);yield return null;
            for(int i=0;i<fps*4;i++)
            {
                if(p.Melee.IsAttacking)
                {
                    int stage=p.Melee.ComboStage;
                    bool next=stage<2&&!queued[stage]&&p.Melee.AttackElapsed>.07f;
                    if(next)queued[stage]=true;Command(slash:next);yield return null;
                    counts[stage]++;gripError=Mathf.Max(gripError,driver.RightGripError);
                    if(fps==60&&counts[stage]%4==0)
                    {View(new Vector3(6,3.4f,9),new Vector3(0,1.9f,.2f),2.75f);capture($"blade-{stage+1}-{counts[stage]:00}.png");}
                    previous=stage;
                }
                else if(previous>=0)break;
                else {Command();yield return null;}
            }
            check(counts.All(c=>c>0)&&!p.Melee.IsAttacking,$"{fps} FPS full three-hit action and recovery [{string.Join(",",counts)}]");
            check(gripError<.015f,$"{fps} FPS actual J01 hilt remains seated throughout all cuts: {gripError:F5}");
        }
        Time.captureDeltaTime=1f/60;
        gm.EnterResult(false);gm.EnterHangar();for(int i=0;i<10;i++)yield return null;
        var launcherButton=UnityEngine.Object.FindObjectsByType<UnityEngine.UI.Button>(FindObjectsInactive.Include,FindObjectsSortMode.None)
            .Single(b=>b.name=="EquipNemesisLauncher");
        launcherButton.onClick.Invoke();yield return null;
        check(p.Loadout.Selected==PrimaryWeapon.NemesisLauncher,"actual hangar loadout button equips J01 launcher");
        gm.BeginWeaponTrial();for(int i=0;i<15;i++)yield return null;
        trial=UnityEngine.Object.FindFirstObjectByType<WeaponTrial>();trial.ClearTargets();p.enabled=false;p.RestoreAt(new Vector3(0,.1f,0));
        var target=trial.SpawnTarget(new Vector3(0,.1f,12),"Rocket",1000).GetComponent<Damageable>();
        for(int i=0;i<20;i++){Command();yield return null;}
        View(new Vector3(6,3.4f,9),new Vector3(0,1.9f,.2f),2.6f);capture("06-launcher-aim.png");
        check(visual.RightGripError<.025f&&visual.LeftGripError<.04f,$"launcher two-hand contacts R={visual.RightGripError:F4}, L={visual.LeftGripError:F4}");
        Command(fire:true);yield return null;
        for(int i=0;i<40;i++){Command();yield return null;}
        check(target.CurrentHealth<1000,"J01 launcher projectile hits an ordinary floor-level target");
        p.weaponController.ResetCooldowns();p.weaponController.TryFireSkill(target);
        for(int i=0;i<20;i++){Command();yield return null;}
        View(new Vector3(6,3.4f,-9),new Vector3(0,1.9f,0),2.6f);capture("07-support-deploy.png");
        check(rig.Deployment>.5f&&rig.Drones.Length==6,"six drone assemblies deploy during support");
        gm.SetPaused(true);var parked=rig.Drones.Select(t=>t.localPosition).ToArray();
        for(int i=0;i<6;i++)yield return null;
        check(rig.Drones.Select((t,i)=>Vector3.Distance(t.localPosition,parked[i])).All(d=>d<.00001f),"pause freezes all deployed drones");
        gm.SetPaused(false);
        for(int i=0;i<Mathf.CeilToInt(NemesisDroneController.Duration*60)+2;i++){Command();yield return null;}
        check(rig.Deployment==0,"support drones return to their docking transforms");
        float movingSupport=0;
        for(int i=0;i<30;i++){Command(Vector2.right,fire:i==0);yield return null;movingSupport=Mathf.Max(movingSupport,visual.LeftGripError);}
        check(movingSupport<.08f,"moving launcher keeps its support hand seated: "+movingSupport);
        var health=p.GetComponent<Damageable>();
        for(int i=0;i<18;i++){Command();yield return null;}
        health.ApplyDamage(5,new DamageInfo(target.gameObject,target.transform.position,target,5){HeavyImpact=true});
        yield return null;yield return null;
        check(driver.HitReactionWeight>0,"J01 chassis responds to a real resolved hit");
        View(new Vector3(6,3.4f,9),new Vector3(0,1.9f,.2f),2.6f);capture("08-hit-reaction.png");
        follow.enabled=true;for(int i=0;i<90;i++){Command();yield return null;}
        capture("09-production-camera.png");
        float standingHip=joints["Pelvis"].position.y;
        health.ApplyDamage(99999,new DamageInfo(target.gameObject,target.transform.position,target,99999){HeavyImpact=true});
        for(int i=0;i<70;i++)yield return null;
        check(health.IsDead&&gm.Phase==GamePhase.Result&&joints["Pelvis"].position.y<standingHip-.5f,"J01 enters collapsed defeat pose and results");
        View(new Vector3(6,3.4f,9),new Vector3(0,1.4f,.2f),2.6f);capture("10-defeat.png");
        gm.ExitPractice();for(int i=0;i<25;i++)yield return null;
        check(GameManager.Instance==gm&&UnityEngine.Object.FindFirstObjectByType<WeaponTrial>()==null
            &&GameObject.Find("WeaponTrialCanvas")==null,"leaving the range removes its controls and preserves GameManager");
        check(!health.IsDead&&rig.Deployment==0&&afterimage.ActiveCount==0&&joints["Pelvis"].position.y>standingHip-.1f,"return to hangar restores the standing rig and docking");
        follow.enabled=true;for(int i=0;i<90;i++)yield return null;
        capture("11-hangar-production.png");
        File.WriteAllText(Path.Combine(output,"motion-metrics.json"),JsonUtility.ToJson(new Metrics{maxRifleGrip=maxGrip,minSoleHeight=minFeet,maxSoleHeight=maxFeet,bladeReach=blade.Reach,rigidBones=rig.GetComponent<RigidMechPoseDriver>().assemblyRoot.GetComponentsInChildren<Transform>().Length},true));
    }
    [Serializable] class Metrics {public float maxRifleGrip,minSoleHeight,maxSoleHeight,bladeReach;public int rigidBones;}
}
