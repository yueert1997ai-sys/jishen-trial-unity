using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Object=UnityEngine.Object;

public static class HeroSelectionChecks
{
    static Button Button(string name)=>Object.FindObjectsByType<Button>(FindObjectsInactive.Include,FindObjectsSortMode.None).Single(b=>b.name==name);
    public static IEnumerator Run(GameManager gm,PlayerController p,Action<bool,string> check,Action<string> capture)
    {
        var loader=p.GetComponent<PlayerMechLoader>();
        check(loader.SelectedHero==HeroMech.Nemesis&&p.GetComponentInChildren<NemesisMotionRig>()!=null,"fresh launch defaults to J-01 NEMESIS");
        check(!loader.SelectHero(HeroMech.Valkyr),"hero replacement is rejected during combat");
        gm.ExitPractice();for(int i=0;i<12;i++)yield return null;
        Button("ChooseHeroButton").onClick.Invoke();yield return null;capture("hero-chooser-production.png");
        for(int pass=0;pass<4;pass++)
        {
            bool valkyr=pass%2==0;
            Button(valkyr?"SelectValkyr":"SelectNemesis").onClick.Invoke();
            for(int i=0;i<15;i++)yield return null;
            check(!loader.SelectionBusy&&loader.SelectedHero==(valkyr?HeroMech.Valkyr:HeroMech.Nemesis),"real hangar button selects "+loader.SelectedHero+" pass="+pass);
            check(p.GetComponentsInChildren<ValkyrMotionDriver>().Length==1&&p.GetComponentsInChildren<LoadoutVisual>().Length==1,"one body and one weapon/pose owner after selection");
            check((p.GetComponentInChildren<NemesisMotionRig>()==null)==valkyr,"selected body identity matches the actual mounted visual");
            var identity=p.GetComponentInChildren<ExtractedHeroIdentity>();
            check(identity!=null&&identity.hero==(valkyr?"VALKYR":"NEMESIS")&&identity.sourceSha256==(valkyr?"def0a2eeeddf72780cbbf1ba55b6a6c0450869de978e89e24bc4e08af2884c74":"d0a86ccc942a6edb18f0a2c24b3083db88e35daa701058667549675e5d33f8c9"),"selected body comes from the selected Blender revision");
            check(identity.sourceWeapon==(valkyr?"EqHG_Lrw038":"EqHG_Lrw607")&&!identity.standaloneShoulderCannon,"real extracted rifle replaces the placeholder and standalone shoulder cannon is absent");
            if(valkyr)
            {
                var skins=p.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                check(skins.Single(s=>s.name=="VALKYR_Body_LOD0").sharedMesh.vertexCount==149228&&skins.Single(s=>s.name=="VALKYR_Body_LOD1").sharedMesh.vertexCount==123252&&skins.Single(s=>s.name=="VALKYR_Body_LOD2").sharedMesh.vertexCount==65736,"every rendered VALKYR LOD uses the cannon-removed geometry buffers");
            }
            var parts=p.GetComponentsInChildren<Transform>(true);
            var ports=p.GetComponentInChildren<RigidMechPoseDriver>().thrusters;
            var jets=parts.Where(t=>t.name.StartsWith("DashGimbal_")).ToArray();
            check(!parts.Any(t=>t.name=="ThrusterNozzle")&&jets.Length==(valkyr?2:6)&&jets.All(t=>ports.Contains(t.parent)),"boost effects attach to every real exhaust port without floating fallback housings");
            var entry=p.Loadout.Equipped;
            check(entry.prefab==(valkyr?p.Loadout.Armory.valkyrRifle.prefab:p.Loadout.Armory.Find(PrimaryWeapon.M7).prefab),"selected hero uses its own retained rifle asset");
            check(p.Melee.MotionProfile==(valkyr?P0ComboProfile.Active:NemesisComboProfile.Active),"sword contact/motion profile rebinds to the selected machine");
            var ghostRoots=Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None).Count(t=>t.name.StartsWith("J01 afterimage "));
            check(ghostRoots==(valkyr?0:4),"switching removes the previous afterimage pool without leaked scene roots");
            var droneEffects=Object.FindObjectsByType<NemesisDroneVfx>(FindObjectsInactive.Include,FindObjectsSortMode.None);
            check(droneEffects.Length==(valkyr?0:1)&&droneEffects.All(v=>v.ActiveParticles==0&&v.VisibleRenderers==0),
                "hangar switching destroys the previous drone effects and keeps the mounted pool clear");
            if(pass<2)
            {
                for(int i=0;i<80;i++)yield return null;
                capture(valkyr?"valkyr-hangar-production.png":"nemesis-hangar-production.png");
                if(valkyr)
                    using(var carry=new ExtractedArmorClearanceProbe(p.GetComponentInChildren<LoadoutVisual>()))
                    {float depth=carry.MaxDepth();check(depth<.025f,"VALKYR hangar carry clears the chest: "+depth+" pair="+carry.WorstPair);}
                gm.BeginWeaponTrial();for(int i=0;i<12;i++)yield return null;p.enabled=false;p.InputRouter.readKeyboard=false;
                var trial=Object.FindFirstObjectByType<WeaponTrial>();trial.ClearTargets();yield return null;
                p.RestoreAt(new Vector3(0,.1f,0));p.stats.ResetStats();
                var target=trial.SpawnTarget(new Vector3(0,.1f,12),"",1000).GetComponent<Damageable>();
                void Command(bool fire=false,bool slash=false,bool dash=false)
                {p.Simulate(new PlayerCommand{Fire=fire,Melee=slash,Dash=dash,Move=dash?Vector2.right:Vector2.zero,HasAim=true,AimPoint=target.AimCenter},Time.deltaTime);}
                for(int i=0;i<20;i++){Command();yield return null;}
                var held=p.GetComponentInChildren<LoadoutVisual>();
                check(held.RightGripError<.04f&&(!valkyr||held.LeftGripError<.04f),"retained VALKYR two-hand hold / NEMESIS one-hand hold stays seated");
                check(held.authoredGripFrames&&Vector3.Dot(held.handR.right,held.WeaponObject.transform.up)>.995f,"closed right fist channel follows the actual pistol grip axis");
                if(valkyr)check(Mathf.Abs(Vector3.Dot(held.handL.right,held.WeaponObject.transform.up))>.995f,"left support fist follows the rifle's actual vertical foregrip");
                var cam=Camera.main;var follow=cam.GetComponent<CameraFollow>();
                var oldPosition=cam.transform.position;var oldRotation=cam.transform.rotation;float oldSize=cam.orthographicSize;bool oldFollow=follow.enabled;
                follow.enabled=false;cam.orthographicSize=2.8f;cam.transform.position=p.transform.TransformPoint(new Vector3(6,4.2f,10));cam.transform.LookAt(p.transform.TransformPoint(new Vector3(0,2.2f,.2f)));
                capture(valkyr?"valkyr-aim-body.png":"nemesis-aim-body.png");
                cam.orthographicSize=.72f;cam.transform.position=held.handR.position+p.transform.TransformDirection(new Vector3(3,1.6f,4));cam.transform.LookAt(held.handR.position+held.WeaponObject.transform.forward*.15f);
                capture(valkyr?"valkyr-aim-grip.png":"nemesis-aim-grip.png");
                if(!valkyr)NemesisEyeChecks.Run(held,cam,check,capture);
                if(valkyr)
                {
                    cam.orthographicSize=1.35f;
                    foreach(float angle in new[]{0f,90f,-90f})
                    {
                        Vector3 focus=p.transform.TransformPoint(new Vector3(0,3.35f,.35f));
                        cam.transform.position=focus+p.transform.rotation*Quaternion.Euler(0,angle,0)*new Vector3(0,.4f,8);
                        cam.transform.LookAt(focus);capture("valkyr-clearance-"+angle+".png");
                    }
                }
                cam.transform.SetPositionAndRotation(oldPosition,oldRotation);cam.orthographicSize=oldSize;follow.enabled=oldFollow;
                float movingGrip=0,armorDepth=0;string worstPair="none";
                var probe=valkyr?new ExtractedArmorClearanceProbe(held):null;
                if(probe!=null)
                {
                    float overlap=probe.ValidateOverlap();
                    check(overlap>.05f,"geometry probe detects a rifle deliberately placed inside the chest: "+overlap);
                    armorDepth=probe.MaxDepth();worstPair=probe.WorstPair;
                }
                for(int direction=0;direction<8;direction++)
                {
                    float a=direction*Mathf.PI/4;
                    for(int frame=0;frame<12;frame++)
                    {
                        p.Simulate(new PlayerCommand{Move=new Vector2(Mathf.Sin(a),Mathf.Cos(a)),HasAim=true,AimPoint=target.AimCenter},Time.deltaTime);yield return null;
                        movingGrip=Mathf.Max(movingGrip,held.RightGripError,valkyr?held.LeftGripError:0);
                        if(probe!=null){float depth=probe.MaxDepth();if(depth>armorDepth){armorDepth=depth;worstPair=probe.WorstPair;}}
                        if(valkyr&&direction==2&&frame==8)
                        {
                            follow.enabled=false;cam.orthographicSize=2.8f;cam.transform.position=p.transform.TransformPoint(new Vector3(6,4.2f,10));cam.transform.LookAt(p.transform.TransformPoint(new Vector3(0,2.2f,.2f)));capture("valkyr-running-clearance.png");
                            cam.transform.SetPositionAndRotation(oldPosition,oldRotation);cam.orthographicSize=oldSize;follow.enabled=oldFollow;
                        }
                    }
                }
                check(movingGrip<.04f,"both extracted bodies keep their rifle contacts in eight movement directions: "+movingGrip);
                if(probe!=null)
                {
                    for(int frame=0;frame<20;frame++)
                    {
                        p.Simulate(new PlayerCommand{Move=Vector2.up,Dash=frame==0,BoostHeld=true,HasAim=true,AimPoint=target.AimCenter},Time.deltaTime);yield return null;
                        float depth=probe.MaxDepth();if(depth>armorDepth){armorDepth=depth;worstPair=probe.WorstPair;}
                        movingGrip=Mathf.Max(movingGrip,held.RightGripError,held.LeftGripError);
                        if(frame==8)
                        {
                            follow.enabled=false;cam.orthographicSize=2.8f;cam.transform.position=p.transform.TransformPoint(new Vector3(6,4.2f,10));cam.transform.LookAt(p.transform.TransformPoint(new Vector3(0,2.2f,.2f)));capture("valkyr-boost-clearance.png");
                            cam.transform.SetPositionAndRotation(oldPosition,oldRotation);cam.orthographicSize=oldSize;follow.enabled=oldFollow;
                        }
                    }
                    check(movingGrip<.04f,"VALKYR boost keeps both fists seated: "+movingGrip);
                    check(armorDepth<.025f,"VALKYR rendered forearms, fists and rifle clear the chest while moving and boosting: "+armorDepth+" pair="+worstPair);
                }
                p.RestoreAt(new Vector3(0,.1f,0));for(int i=0;i<15;i++){Command();yield return null;}
                int fired=0;Action hear=()=>fired++;p.weaponController.BeamFired+=hear;
                Command(fire:true);yield return null;
                for(int i=0;i<35;i++){Command();yield return null;if(probe!=null)armorDepth=Mathf.Max(armorDepth,probe.MaxDepth());}
                p.weaponController.BeamFired-=hear;
                check(fired==1&&target.CurrentHealth<1000,"selected rifle fires once and hits after switching: "+loader.SelectedHero);
                if(probe!=null){check(armorDepth<.025f,"VALKYR firing recoil also clears the chest: "+armorDepth);probe.Dispose();}
                Command(slash:true);yield return null;
                for(int i=0;i<14;i++){Command();yield return null;}
                check(p.Melee.IsAttacking,"selected sword begins its real attack: "+loader.SelectedHero);
                for(int i=0;i<75;i++){Command();yield return null;}
                Command(dash:true);yield return null;
                for(int i=0;i<4;i++){Command();yield return null;}
                check(p.IsDashing&&p.GetComponent<MechDashPresentation>().Pulse>.4f,"selected body's rebound thrusters respond to dash: "+loader.SelectedHero);
                for(int i=0;i<25;i++){Command();yield return null;}
                p.weaponController.ResetCooldowns();check(p.weaponController.TryFireSkill(target),"selected machine activates its own support");yield return null;
                check(valkyr?p.GetComponentInChildren<ValkyrBackCannon>().Active&&Object.FindObjectsByType<MissileProjectile>(FindObjectsSortMode.None).Length==0:p.GetComponentInChildren<NemesisDroneController>().Active,
                    valkyr?"VALKYR deploys its real backpack cannons without missiles":"NEMESIS deploys its six autonomous beam drones");
                gm.ExitPractice();for(int i=0;i<12;i++)yield return null;
                check(loader.SelectedHero==(valkyr?HeroMech.Valkyr:HeroMech.Nemesis),"returning to the hangar preserves the session's hero choice");
            }
        }
        check(loader.SelectedHero==HeroMech.Nemesis&&p.Loadout.CanDeploy,"switching back leaves default hero ready for deployment");
    }
}
