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
                gm.BeginWeaponTrial();for(int i=0;i<12;i++)yield return null;p.enabled=false;p.InputRouter.readKeyboard=false;
                var trial=Object.FindFirstObjectByType<WeaponTrial>();trial.ClearTargets();yield return null;
                p.RestoreAt(new Vector3(0,.1f,0));p.stats.ResetStats();
                var target=trial.SpawnTarget(new Vector3(0,.1f,12),"",1000).GetComponent<Damageable>();
                void Command(bool fire=false,bool slash=false,bool dash=false)
                {p.Simulate(new PlayerCommand{Fire=fire,Melee=slash,Dash=dash,Move=dash?Vector2.right:Vector2.zero,HasAim=true,AimPoint=target.AimCenter},Time.deltaTime);}
                for(int i=0;i<20;i++){Command();yield return null;}
                var held=p.GetComponentInChildren<LoadoutVisual>();
                check(held.RightGripError<.04f&&(!valkyr||held.LeftGripError<.04f),"retained VALKYR two-hand hold / NEMESIS one-hand hold stays seated");
                int fired=0;Action hear=()=>fired++;p.weaponController.BeamFired+=hear;
                Command(fire:true);yield return null;
                for(int i=0;i<35;i++){Command();yield return null;}
                p.weaponController.BeamFired-=hear;
                check(fired==1&&target.CurrentHealth<1000,"selected rifle fires once and hits after switching: "+loader.SelectedHero);
                Command(slash:true);yield return null;
                for(int i=0;i<14;i++){Command();yield return null;}
                check(p.Melee.IsAttacking,"selected sword begins its real attack: "+loader.SelectedHero);
                for(int i=0;i<75;i++){Command();yield return null;}
                Command(dash:true);yield return null;
                for(int i=0;i<4;i++){Command();yield return null;}
                check(p.IsDashing&&p.GetComponent<MechDashPresentation>().Pulse>.4f,"selected body's rebound thrusters respond to dash: "+loader.SelectedHero);
                for(int i=0;i<25;i++){Command();yield return null;}
                p.weaponController.ResetCooldowns();check(p.weaponController.TryFireSkill(target),"selected machine activates its own support");yield return null;
                check(valkyr?Object.FindObjectsByType<MissileProjectile>(FindObjectsSortMode.None).Length==4:p.GetComponentInChildren<NemesisDroneController>().Active,
                    valkyr?"VALKYR retains four physical support missiles":"NEMESIS deploys its six autonomous beam drones");
                gm.ExitPractice();for(int i=0;i<12;i++)yield return null;
                check(loader.SelectedHero==(valkyr?HeroMech.Valkyr:HeroMech.Nemesis),"returning to the hangar preserves the session's hero choice");
            }
        }
        check(loader.SelectedHero==HeroMech.Nemesis&&p.Loadout.CanDeploy,"switching back leaves default hero ready for deployment");
    }
}
