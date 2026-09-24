using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Object=UnityEngine.Object;

public static class CombatArsenalChecks
{
    public static IEnumerator Run(GameManager gm,PlayerController p,Action<bool,string> check,Action<string> capture)
    {
        var beats=CombatRules.Current.Encounters;
        for(int seed=1;seed<=100;seed++)for(int room=-1;room<3;room++)
        {
            var a=new EncounterRoster(seed,room,beats);for(int i=0;i<50;i++)_ = UnityEngine.Random.value;
            var b=new EncounterRoster(seed,room,beats);
            var rows=Enumerable.Range(0,beats.Count).SelectMany(g=>a.Group(g)).ToArray();
            check(rows.Select(s=>s.body).Distinct().Count()>=3&&rows.Select(s=>s.weapon).Distinct().Count()>=4,"roster diversity seed="+seed+" room="+room);
            check(Enumerable.Range(0,beats.Count).All(g=>a.Entry(g)==b.Entry(g)&&a.Group(g).Select(s=>s.ToString()).SequenceEqual(b.Group(g).Select(s=>s.ToString()))),"roster repeatable independently of VFX random seed="+seed+" room="+room);
        }
        foreach(var hero in new[]{HeroMech.Valkyr,HeroMech.Nemesis})
        {
            gm.ExitPractice();for(int i=0;i<5;i++)yield return null;
            p.GetComponent<PlayerMechLoader>().SelectHero(hero);for(int i=0;i<20;i++)yield return null;
            gm.BeginWeaponTrial();for(int i=0;i<15;i++)yield return null;
            var trial=Object.FindFirstObjectByType<WeaponTrial>();trial.ClearTargets();yield return null;
            p.enabled=false;p.InputRouter.readKeyboard=false;
            var driver=p.GetComponentInChildren<ValkyrMotionDriver>();
            void Command(Vector2 move=default,bool dash=false,bool boost=false,bool skill=false)
            {p.Simulate(new PlayerCommand{Move=move,Dash=dash,BoostHeld=boost,Skill=skill,HasAim=true,AimPoint=p.transform.position+Vector3.forward*18},Time.deltaTime);}
            foreach(int fps in new[]{30,60,120})
            {
                Time.captureDeltaTime=1f/fps;yield return null;p.RestoreAt(new Vector3(0,.1f,-4));p.stats.ResetStats();
                float start=p.transform.position.z;
                Command(Vector2.up,true);yield return null;
                check(p.GetComponent<MechDashPresentation>().Pulse>.9f,"first dash frame has nozzle burst "+hero+" "+fps);
                if(hero==HeroMech.Valkyr)check(p.GetComponentsInChildren<IonThrusterVfx>().Count(t=>t.Visible)==2,"tap space lights both ion nozzles "+fps);
                for(int i=1;i<Mathf.CeilToInt(p.dashDuration*fps);i++){Command(Vector2.up);yield return null;}
                float distance=p.transform.position.z-start,expected=hero==HeroMech.Nemesis?6.5f:5;
                check(Mathf.Abs(distance-expected)<.45f,"physical dash distance "+hero+" "+fps+" = "+distance);
                check(Mathf.Abs(p.stats.MoveSpeed-(hero==HeroMech.Nemesis?13.5f:10.4f))<.01f,"independent unmodified base movement "+hero);
                p.RestoreAt(new Vector3(0,.1f,-5));p.stats.ResetStats();
                for(int i=0;i<fps/3;i++){Command(Vector2.up,boost:true);yield return null;}
                check(Mathf.Abs(p.Velocity.magnitude-p.stats.MoveSpeed*1.6f)<.1f,"held boost speed "+hero+" "+fps);
                Command();yield return null;
                for(int i=0;i<fps;i++){Command();yield return null;}
                check(p.GetComponent<MechDashPresentation>().Pulse<.05f,"jet release settles "+hero+" "+fps);
                p.RestoreAt(new Vector3(0,.1f,0));p.AimAt(p.transform.position+Vector3.forward*10);
                var before=p.transform.rotation;p.AimAt(p.transform.position+Vector3.right*10,1f/fps,720);
                check(Quaternion.Angle(before,p.transform.rotation)<=720f/fps+.01f,"bounded idle sword turn "+hero+" "+fps);
                p.Simulate(new PlayerCommand{Melee=true,HasAim=true,AimPoint=p.transform.position+Vector3.forward*10},Time.deltaTime);yield return null;
                for(int i=0;i<fps&&(!p.Melee.IsAttacking||p.Melee.AttackElapsed<p.Melee.CurrentStroke.contactStart);i++){Command();yield return null;}
                check(p.Melee.IsAttacking,"sword reaches contact phase "+hero+" "+fps);
                before=p.transform.rotation;p.AimAt(p.transform.position-Vector3.forward*10,1f/fps,720);
                check(Quaternion.Angle(before,p.transform.rotation)<.001f,"active cut locks its committed direction "+hero+" "+fps);
                p.RestoreAt(new Vector3(0,.1f,0));
            }
            Time.captureDeltaTime=1f/60;
            if(hero==HeroMech.Valkyr)
            {
                int contacts=0;Action<GameAudioCue> step=c=>{if(c==GameAudioCue.Footstep)contacts++;};GameAudio.CuePlayed+=step;
                try{for(int i=0;i<120;i++){Command(Vector2.up*.25f);yield return null;}}
                finally{GameAudio.CuePlayed-=step;}
                check(contacts>0&&contacts<12,"slow VALKYR steps have sparse physical contact sounds: "+contacts);
                p.RestoreAt(new Vector3(0,.1f,0));p.stats.ResetStats();
                var cannon=p.GetComponentInChildren<ValkyrBackCannon>();
                var target=trial.SpawnTarget(new Vector3(0,.1f,12),"",1000).GetComponent<Damageable>();
                Command(Vector2.right,skill:true);yield return null;Object.Destroy(target.gameObject);
                for(int i=0;i<33;i++){Command(Vector2.right,boost:true);yield return null;}
                check(cannon.ShotsFired==2,"moving E fires both committed barrels after target disappears");
                check(Mathf.Abs(Camera.main.orthographicSize-17)<.01f,"actual production combat view is 17");
                capture("arsenal-valkyr-moving-cannon-camera17.png");
            }
            else
            {
                p.RestoreAt(new Vector3(0,.1f,-3));p.stats.ResetStats();
                var target=trial.SpawnTarget(new Vector3(0,.1f,10),"",1000).GetComponent<Damageable>();
                Command(skill:true);yield return null;
                for(int i=0;i<100;i++){Command();yield return null;}
                var independent=p.GetComponentInChildren<IndependentDroneMeshes>();
                check(independent!=null&&independent.Units==6,"six separately bounded drone renderers");
                var lod=p.GetComponentInChildren<NemesisMotionRig>().GetComponentInChildren<LODGroup>();
                if(lod!=null)lod.ForceLOD(2);
                check(p.GetComponentsInChildren<MeshRenderer>().Count(r=>r.name.StartsWith("Autonomous drone shell")&&r.enabled)==6,"body far LOD keeps all external units visible");
                for(int i=0;i<20;i++){Command(Vector2.right);yield return null;}
                var ghosts=p.GetComponentInChildren<NemesisAfterimage>();check(ghosts.ActiveCount>0&&ghosts.ActiveCount<=4,"ordinary movement has bounded faint ghosts");
                capture("arsenal-nemesis-independent-camera17.png");if(lod!=null)lod.ForceLOD(-1);
            }
        }
        gm.ExitPractice();yield return null;gm.BeginWeaponTrial();for(int i=0;i<15;i++)yield return null;
        Object.FindFirstObjectByType<WeaponTrial>().ClearTargets();p.RestoreAt(new Vector3(0,.1f,-9));yield return null;
        var specimens=new List<GameObject>();
        var definitions=new[]{new EnemySpawnSpec("E01",EnemyWeapon.M7,EnemyKind.Ranged),new EnemySpawnSpec("GM",EnemyWeapon.M14,EnemyKind.Ranged),new EnemySpawnSpec("ZAKU",EnemyWeapon.Rocket,EnemyKind.Melee),new EnemySpawnSpec("DOM",EnemyWeapon.Ax01,EnemyKind.Ranged),new EnemySpawnSpec("GUNCANNON",EnemyWeapon.BackCannon,EnemyKind.Ranged)};
        foreach(var spec in definitions)
        {
            var go=Object.Instantiate(gm.stageManager.enemySpawner.meleePrefab,new Vector3((specimens.Count-2)*5,.1f,4),Quaternion.Euler(0,180,0));
            var arsenal=go.AddComponent<EnemyArsenal>();arsenal.Configure(spec);var enemy=go.GetComponent<EnemyBase>();enemy.Init(p.transform,null);enemy.ConfigureP0Role(spec.role);enemy.TrainingTarget=true;
            specimens.Add(go);
        }
        for(int i=0;i<35;i++)yield return null;
        foreach(var go in specimens)
        {
            var arsenal=go.GetComponent<EnemyArsenal>();go.GetComponent<EnemyBase>().enabled=false;
            check(arsenal.Muzzle!=null&&go.GetComponentsInChildren<Renderer>().Any(r=>r.enabled),"actual body and muzzle: "+arsenal.Spec);
            arsenal.Fire(PlanarCombat.Direction(p.transform.position-go.transform.position,go.transform.forward));
        }
        yield return null;capture("arsenal-five-chassis-camera17.png");
        foreach(var go in specimens)Object.Destroy(go);yield return null;
        foreach(var shot in Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None))shot.Despawn();
        var boss=gm.stageManager.enemySpawner.SpawnFazz(0);boss.GetComponent<UnityEngine.AI.NavMeshAgent>().Warp(new Vector3(0,.1f,8));yield return null;
        var fazz=boss.GetComponent<FazzBossController>();boss.enabled=false;
        check(boss.GetComponent<Damageable>().maxHealth==1800,"first-tier FAZZ actual health is exactly 1800");
        check(fazz.model.modelId=="FAZZ"&&fazz.model.bones.Length==28&&ImportedBodyMotion.LoadBank("fazz").Count==38,"rebuilt HG2250 skin and native motion ancestors loaded");
        for(int pattern=0;pattern<3;pattern++)
        {
            p.stats.ResetStats();fazz.Cancel();check(fazz.Begin(pattern),"start original FAZZ attack "+pattern);
            int frames=0;
            while(fazz.Active&&frames++<300)
            {
                fazz.Tick();yield return null;
                if(frames==44||pattern==1&&frames==23||pattern==2&&frames==55)
                {
                    capture("arsenal-fazz-pattern"+pattern+"-camera17.png");
                    var cam=Camera.main;var oldPos=cam.transform.position;var oldRot=cam.transform.rotation;float oldSize=cam.orthographicSize;
                    cam.orthographicSize=5;cam.transform.position=boss.transform.position+new Vector3(9,7,-13);cam.transform.LookAt(boss.transform.position+Vector3.up*2.5f);
                    capture("arsenal-fazz-pattern"+pattern+"-detail.png");cam.orthographicSize=oldSize;cam.transform.SetPositionAndRotation(oldPos,oldRot);
                }
            }
            check(!fazz.Active,"native montage ends "+pattern);
        }
        check(fazz.MegaShots==1&&fazz.TwinShots==2&&fazz.Missiles==18,"FAZZ emits one mega, two back beams, eighteen missiles per native attack");
        Object.Destroy(boss.gameObject);yield return null;
        for(int room=0;room<3;room++)
        {var profile=new FazzBossProfile(room);check(profile.health==1800+room*400&&Mathf.Abs(profile.gap-new[]{1.2f,.85f,.55f}[room])<.001f,"room FAZZ tier "+room);}
        gm.ExitPractice();yield return null;gm.BeginShortCombat();for(int i=0;i<5;i++)yield return null;
        var demo=Object.FindFirstObjectByType<P0CombatDemo>();int seedBefore=CombatRuntime.Run.Seed;demo.Restart(true);yield return null;
        check(CombatRuntime.Run.Seed==seedBefore,"short retry preserves encounter seed");
        gm.ExitPractice();yield return null;
        foreach(var hero in new[]{HeroMech.Valkyr,HeroMech.Nemesis})foreach(var id in new[]{"m7","m14","type08","ax01","rocket","missile_rack","back_cannon"})
        {
            p.GetComponent<PlayerMechLoader>().SelectHero(hero);for(int i=0;i<12;i++)yield return null;
            check(gm.equipmentLoop.Warehouse.Acquire(id)&&gm.equipmentLoop.TryEquip(id),"real recovered model equips in hangar "+hero+" "+id);
            for(int i=0;i<5;i++)yield return null;gm.BeginWeaponTrial();for(int i=0;i<12;i++)yield return null;
            var trial=Object.FindFirstObjectByType<WeaponTrial>();trial.ClearTargets();yield return null;
            p.RestoreAt(new Vector3(0,.1f,-4));p.stats.ResetStats();p.enabled=false;
            var target=trial.SpawnTarget(new Vector3(0,.1f,9),"",1000).GetComponent<Damageable>();
            var held=p.GetComponentInChildren<LoadoutVisual>();
            check(p.Loadout.Selected==PrimaryWeapon.Collection&&gm.equipmentLoop.Weapon.id==id&&held.WeaponObject!=null,"collection identity and visible prefab agree "+hero+" "+id);
            for(int i=0;i<65;i++){p.Simulate(new PlayerCommand{Fire=true,HasAim=true,AimPoint=target.AimCenter},Time.deltaTime);yield return null;}
            check(target.CurrentHealth<1000,"recovered weapon deals real ground-target damage "+hero+" "+id+" hp="+target.CurrentHealth);
            var before=held.WeaponObject.transform.position;
            for(int i=0;i<18;i++){p.Simulate(new PlayerCommand{Move=Vector2.right,HasAim=true,AimPoint=target.AimCenter},Time.deltaTime);yield return null;}
            check(Vector3.Distance(before,held.WeaponObject.transform.position)>.8f,"weapon mount travels with chassis "+hero+" "+id);
            check(held.RightGripError<.13f&&held.LeftGripError<.13f&&held.ShoulderContactError<.03f,"moving heavy grip and saddle remain seated "+hero+" "+id+" errors="+held.RightGripError+","+held.LeftGripError);
            if(id=="type08"||id=="ax01"||id=="back_cannon")capture("arsenal-grip-"+hero+"-"+id+".png");
            gm.ExitPractice();for(int i=0;i<5;i++)yield return null;
        }
        gm.equipmentLoop.OpenWarehouse();yield return null;
        var scroll=Object.FindObjectsByType<UnityEngine.UI.ScrollRect>(FindObjectsSortMode.None).First(s=>s.name=="WeaponScroll");
        check(scroll.content.rect.height>scroll.viewport.rect.height,"expanded real weapon collection is scrollable");scroll.verticalNormalizedPosition=0;yield return null;capture("arsenal-warehouse-production.png");gm.equipmentLoop.CloseWarehouse();
    }
}
