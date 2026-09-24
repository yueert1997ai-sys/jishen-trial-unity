using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using Object=UnityEngine.Object;

public static class ValkyrCannonChecks
{
    public static IEnumerator Run(GameManager gm,PlayerController p,Action<bool,string> check,Action<string> capture)
    {
        gm.ExitPractice();yield return null;
        check(p.GetComponent<PlayerMechLoader>().SelectHero(HeroMech.Valkyr),"select VALKYR from the actual hangar");
        for(int i=0;i<15;i++)yield return null;
        gm.BeginWeaponTrial();for(int i=0;i<15;i++)yield return null;
        var trial=Object.FindFirstObjectByType<WeaponTrial>();trial.ClearTargets();yield return null;
        p.enabled=false;p.InputRouter.readKeyboard=false;
        var cannon=p.GetComponentInChildren<ValkyrBackCannon>();
        check(cannon!=null&&cannon.barrels.Length==2,"both original backpack barrels have independent joints");
        var skins=p.GetComponentsInChildren<SkinnedMeshRenderer>().Where(s=>s.name.StartsWith("VALKYR_Body")).ToArray();
        check(skins.Length==3&&skins.All(s=>s.bones.Contains(cannon.barrels[0])&&s.bones.Contains(cannon.barrels[1])&&s.sharedMesh.boneWeights.Count(w=>w.boneIndex0>=24)>100),"all three body LODs bind real geometry to both cannon joints");
        var cam=Camera.main;cam.GetComponent<CameraFollow>().enabled=false;cam.orthographic=true;cam.orthographicSize=4.4f;
        void View(){cam.transform.position=p.transform.position+new Vector3(8,6,10);cam.transform.LookAt(p.transform.position+new Vector3(0,2.5f,1));}
        foreach(int fps in new[]{30,60,120})
        {
            Time.captureDeltaTime=1f/fps;p.RestoreAt(new Vector3(0,.1f,0));p.stats.ResetStats();yield return null;
            var target=trial.SpawnTarget(new Vector3(0,.1f,12),"Cannon target",1000).GetComponent<Damageable>();yield return null;
            p.AimAt(target.AimCenter);
            p.Simulate(new PlayerCommand{Skill=true,HasAim=true,AimPoint=target.AimCenter},Time.deltaTime);yield return null;
            check(cannon.Active&&p.weaponController.SkillCooldownRemaining>9,"real E input starts back cannon at "+fps);
            for(int i=0;i<fps/4;i++)yield return null;
            check(target.CurrentHealth==1000&&cannon.ShotsFired==0,"deployment and charge cannot deal early damage at "+fps);
            gm.SetPaused(true);float age=cannon.Age;var positions=cannon.barrels.Select(t=>t.position).ToArray();
            for(int i=0;i<8;i++)yield return null;
            check(cannon.Age==age&&positions.Select((v,i)=>Vector3.Distance(v,cannon.barrels[i].position)).All(d=>d<.001f),"pause freezes charge and physical cannon pose at "+fps);gm.SetPaused(false);
            for(int i=0;i<fps&&cannon.ShotsFired==0;i++)yield return null;
            check(cannon.ShotsFired==2&&cannon.Hits==2&&Mathf.Abs(target.CurrentHealth-928)<.1f,"two real bores deal exactly 72 damage at "+fps+" hp="+target.CurrentHealth);
            check(cannon.muzzles.All(m=>Vector3.Dot(m.forward,Vector3.forward)>.97f),"muzzle axes point forward after deployment at "+fps);
            check(Object.FindObjectsByType<MissileProjectile>(FindObjectsSortMode.None).Length==0,"E spawns no legacy missiles at "+fps);
            View();if(fps==60)capture("valkyr-back-cannon-fire.png");
            float hp=target.CurrentHealth;p.weaponController.ResetCooldowns();check(!p.weaponController.TryFireSkill(target)&&p.weaponController.SkillCooldownRemaining>0,"active cannon cannot redeploy after cooldown reset");
            for(int i=0;i<fps*2;i++)yield return null;
            check(!cannon.Active&&target.CurrentHealth==hp,"afterglow does not repeat damage and barrels return at "+fps);
            trial.ClearTargets();yield return null;
        }
        Time.captureDeltaTime=1f/60;p.RestoreAt(new Vector3(0,.1f,0));yield return null;
        var behind=trial.SpawnTarget(new Vector3(0,.1f,12),"Covered target",1000).GetComponent<Damageable>();
        var cover=GameObject.CreatePrimitive(PrimitiveType.Cube);cover.transform.position=new Vector3(0,2,7);cover.transform.localScale=new Vector3(8,4,.5f);Physics.SyncTransforms();yield return null;
        p.AimAt(behind.AimCenter);check(p.weaponController.TryFireSkill(behind),"cannon accepts a covered aim point");
        for(int i=0;i<95;i++)yield return null;
        check(behind.CurrentHealth==1000&&cannon.Hits==0,"solid cover blocks both beams");Object.Destroy(cover);trial.ClearTargets();yield return null;
        p.RestoreAt(new Vector3(0,.1f,0));p.AimAt(new Vector3(0,1,15));check(p.weaponController.TryFireSkill(null),"manual cannon fires without an autoaim target");
        for(int i=0;i<8;i++)yield return null;p.RestoreAt(new Vector3(0,.1f,0));
        for(int i=0;i<45;i++)yield return null;check(!cannon.Active&&cannon.ShotsFired==0,"restart before discharge cancels pending damage");
        p.stats.ResetStats();for(int i=0;i<25;i++){p.Simulate(new PlayerCommand{Move=Vector2.up,BoostHeld=true,HasAim=true,AimPoint=p.transform.position+Vector3.forward*15},Time.deltaTime);yield return null;}
        check(p.IsBoosting&&p.GetComponentsInChildren<IonThrusterVfx>().Count(f=>f.Visible)==2,"sustained flight lights both actual ion nozzles");
        cam.transform.position=p.transform.position+new Vector3(7,5,-9);cam.transform.LookAt(p.transform.position+new Vector3(0,2,0));capture("valkyr-ion-flight.png");
        p.CancelMovement();p.weaponController.ResetCooldowns();check(p.weaponController.TryFireSkill(null),"prepare return cleanup");gm.ExitPractice();for(int i=0;i<8;i++)yield return null;
        check(!cannon.Active&&p.GetComponentsInChildren<IonThrusterVfx>().All(f=>!f.Visible),"return to hangar clears cannon and ion effects");
        check(p.GetComponent<PlayerMechLoader>().SelectHero(HeroMech.Nemesis),"switch back to NEMESIS");for(int i=0;i<15;i++)yield return null;
        check(p.GetComponentInChildren<ValkyrBackCannon>()==null&&p.GetComponentInChildren<NemesisDroneController>()!=null,"hero swap removes cannons and retains NEMESIS drones");
    }
}
