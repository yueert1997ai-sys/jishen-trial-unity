using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
using Object=UnityEngine.Object;

public static class WeaponHandlingChecks
{
    static readonly Vector3 anchor=new Vector3(180,0,180);
    static Damageable Body(float distance,int team=1)
    {
        var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name="Arms range probe";
        go.transform.position=anchor+new Vector3(0,team==0?1:7,distance);go.transform.localScale=new Vector3(1,2,1);
        var hp=go.AddComponent<Damageable>();hp.team=team;hp.destroyOnDeath=false;hp.RestoreLife(100,100);return hp;
    }
    static void ClearShots(){foreach(var shot in Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None))shot.Despawn();}
    static PlayerCommand Command(PlayerController p,bool fire=false,bool melee=false,bool dash=false)=>new PlayerCommand
    {Fire=fire,Melee=melee,Dash=dash,Move=dash?Vector2.right:Vector2.zero,HasAim=true,AimPoint=p.transform.position+Vector3.forward*10};
    public static IEnumerator Run(GameManager gm,PlayerController p,string output,Action<bool,string> check,Action<string> capture)
    {
        var observations=new List<string>();
        foreach(var hero in new[]{HeroMech.Nemesis,HeroMech.Valkyr})foreach(var weapon in new[]{PrimaryWeapon.M7,PrimaryWeapon.M14})
        {
            gm.ExitPractice();for(int i=0;i<5;i++)yield return null;
            p.GetComponent<PlayerMechLoader>().SelectHero(hero);for(int i=0;i<20;i++)yield return null;
            check(p.Loadout.Select(weapon),"select actual weapon "+hero+" "+weapon);
            gm.BeginWeaponTrial();for(int i=0;i<20;i++)yield return null;
            Object.FindFirstObjectByType<WeaponTrial>().ClearTargets();yield return null;
            p.enabled=false;p.InputRouter.readKeyboard=false;
            foreach(int fps in new[]{30,60,120})
            {
                Time.captureDeltaTime=1f/fps;yield return null;p.RestoreAt(new Vector3(0,.1f,0));p.stats.ResetStats();
                var times=new List<float>();int sounds=0;bool actualProfiles=true;
                Action onShot=()=>{times.Add(Time.time);actualProfiles&=Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None).Any(s=>s.team==0&&s.Handling.Capacity==(weapon==PrimaryWeapon.M7?18:4));};
                Action<GameAudioCue> onSound=c=>{if(c==(weapon==PrimaryWeapon.M7?GameAudioCue.RifleShot:GameAudioCue.Beam))sounds++;};
                p.weaponController.BeamFired+=onShot;GameAudio.CuePlayed+=onSound;
                int capacity=weapon==PrimaryWeapon.M7?18:4;float interval=weapon==PrimaryWeapon.M7?.18f:.75f,recovery=weapon==PrimaryWeapon.M7?.65f:1.1f;
                try
                {
                    int budget=fps*8;
                    while(times.Count<capacity&&budget-->0){p.Simulate(Command(p,true),Time.deltaTime);yield return null;}
                    check(times.Count==capacity&&p.weaponController.PrimaryRoundsRemaining==0&&p.weaponController.PrimaryRecoveryRemaining>0,"actual held fire enters automatic service "+hero+" "+weapon+" "+fps);
                    check(times.Count==sounds,"one discharge sound per trigger, no reload shot "+hero+" "+weapon+" "+fps);
                    check(actualProfiles,"real muzzle projectiles receive immutable weapon profiles "+hero+" "+weapon+" "+fps);
                    check(Mathf.Abs(times[capacity-1]-times[0]-(capacity-1)*interval)<1f/fps+.008f,"within-cycle cadence retained "+weapon+" "+fps);
                    float remaining=p.weaponController.PrimaryRecoveryRemaining;gm.SetPaused(true);
                    for(int i=0;i<8;i++){p.weaponController.TryFireBeam(true);yield return null;}
                    check(times.Count==capacity&&Mathf.Abs(p.weaponController.PrimaryRecoveryRemaining-remaining)<.001f,"pause freezes service and blocks fire "+fps);gm.SetPaused(false);
                    budget=fps*2;while(times.Count<=capacity&&budget-->0){p.Simulate(Command(p,true),Time.deltaTime);yield return null;}
                    check(times.Count==capacity+1&&Mathf.Abs(times[capacity]-times[capacity-1]-recovery)<1f/fps+.008f,"held fire resumes once after finite service "+hero+" "+weapon+" "+fps);
                    int stopped=times.Count;
                    for(int i=0;i<fps/2;i++){p.Simulate(Command(p),Time.deltaTime);yield return null;}
                    check(times.Count==stopped&&sounds==stopped,"release has no autonomous fire or delayed sounds "+fps);
                    observations.Add($"hero={hero} weapon={weapon} fps={fps} shots={times.Count} service={times[capacity]-times[capacity-1]:F4}");

                    p.weaponController.ResetCooldowns();p.Stance.ResetStance();times.Clear();
                    budget=fps*8;while(times.Count<capacity&&budget-->0){p.Simulate(Command(p,true),Time.deltaTime);yield return null;}
                    p.Simulate(Command(p,dash:true),Time.deltaTime);yield return null;
                    check(p.IsDashing&&p.weaponController.PrimaryRecoveryRemaining>0,"auto service does not lock dash "+weapon+" "+fps);
                    while(p.IsDashing){p.Simulate(Command(p),Time.deltaTime);yield return null;}
                    p.Simulate(Command(p,melee:true),Time.deltaTime);yield return null;
                    for(int i=0;i<Mathf.CeilToInt(.2f*fps)&&!p.Melee.IsAttacking;i++){p.Simulate(Command(p),Time.deltaTime);yield return null;}
                    check(p.Melee.IsAttacking,"blade remains independently usable during gun service "+weapon+" "+fps);
                    p.RestoreAt(new Vector3(0,.1f,0));
                    check(p.weaponController.PrimaryRoundsRemaining==capacity&&p.weaponController.PrimaryRecoveryRemaining==0,"run restore clears cycle state "+weapon+" "+fps);
                }
                finally{p.weaponController.BeamFired-=onShot;GameAudio.CuePlayed-=onSound;ClearShots();}
            }
        }
        // Actual swept projectile paths, including piercing across different ranges and cosmetic altitude.
        foreach(int fps in new[]{30,60,120})foreach(var weapon in new[]{PrimaryWeapon.M7,PrimaryWeapon.M14})
        {
            Time.captureDeltaTime=1f/fps;yield return null;
            var owner=Body(0,0);var near=Body(8.59f);var mid=Body(21.59f);var far=Body(32.59f);var beyond=Body(38.59f);
            yield return null;Physics.SyncTransforms();
            var shot=ProjectilePool.Spawn(false,"Arms range shot",anchor+new Vector3(0,3,2),Color.cyan);
            shot.Init(0,owner,Vector3.forward,20,72,3,0,5);shot.SetHandling(WeaponHandling.For(weapon));shot.SetImpact(30,CombatHitKind.Rifle);
            yield return null;yield return null;owner.transform.position+=Vector3.right*6;
            for(int i=0;i<fps;i++)yield return null;
            check(Mathf.Abs(near.CurrentHealth-80)<.03f,"full damage in optimal range "+weapon+" "+fps);
            check(Mathf.Abs(mid.CurrentHealth-(weapon==PrimaryWeapon.M7?84.5f:80f))<.05f,"distance curves separate rifle roles, unchanged by shooter movement "+weapon+" "+fps);
            check(Mathf.Abs(far.CurrentHealth-(weapon==PrimaryWeapon.M7?89f:82.5f))<.05f,"piercing preserves absolute flight distance "+weapon+" "+fps);
            check(weapon==PrimaryWeapon.M7?beyond.CurrentHealth==100:beyond.CurrentHealth<100,"hard range does not overshoot on a long frame "+weapon+" "+fps);
            check(!shot.gameObject.activeSelf,"range terminates pooled shot "+weapon+" "+fps);
            foreach(var hp in new[]{owner,near,mid,far,beyond}){hp.gameObject.SetActive(false);Object.Destroy(hp.gameObject);}yield return null;
            owner=Body(0,0);far=Body(42);yield return null;Physics.SyncTransforms();
            var reuse=ProjectilePool.Spawn(false,"Unprofiled reuse",anchor+new Vector3(0,3,2),Color.red);
            reuse.Init(0,owner,Vector3.forward,20,72,2,0,0);
            for(int i=0;i<fps;i++)yield return null;
            check(far.CurrentHealth==80,"pool Init removes previous range restrictions "+fps);
            Object.Destroy(owner.gameObject);Object.Destroy(far.gameObject);ClearShots();yield return null;
        }
        foreach(int fps in new[]{30,60,120})
        {
            Time.captureDeltaTime=1f/fps;yield return null;
            check(NavMesh.SamplePosition(new Vector3(0,0,5),out var spot,3,NavMesh.AllAreas),"reaction probe on current navigation "+fps);
            var actor=Object.Instantiate(gm.stageManager.enemySpawner.meleePrefab,spot.position,Quaternion.identity);
            var enemy=actor.GetComponent<EnemyBase>();enemy.TrainingTarget=true;enemy.Init(p.transform,null);enemy.ConfigureP0Role(EnemyKind.Ranged);
            var hp=actor.GetComponent<Damageable>();hp.RestoreLife(5000,5000);hp.destroyOnDeath=false;yield return null;
            DamageResult Apply(bool heavy=false,bool melee=false)=>hp.ApplyDamage(1,new DamageInfo(p.gameObject,p.transform.position,p.GetComponent<Damageable>(),1)
                {Kind=melee?CombatHitKind.Melee:heavy?CombatHitKind.HeavyRifle:CombatHitKind.Rifle,HeavyImpact=heavy,MeleeStrike=melee});
            Apply();check(hp.LastHit.Staggered,"light rifle can start short reaction "+fps);
            Apply(true);check(hp.LastHit.Staggered&&enemy.HitStaggerRemaining>.16f,"heavy shot overrides preceding light reaction "+fps);
            Apply(true);check(!hp.LastHit.Staggered,"repeated heavy round cannot restart hard stagger "+fps);
            Apply();check(!hp.LastHit.Staggered,"light shot cannot replace heavy response "+fps);
            Apply(false,true);check(hp.LastHit.Staggered,"gun immunity never consumes blade stagger "+fps);
            for(int i=0;i<fps*3;i++){if(i%Mathf.Max(1,fps/10)==0)Apply();yield return null;}
            check(enemy.ShotsEmitted>0,"continuous rifle hits still leave enemy an attack opportunity "+fps);
            enemy.ConfigureP0Role(EnemyKind.Elite);var armor=actor.GetComponent<ArmorHealth>();Apply(true);
            check(!hp.LastHit.Staggered&&armor.Intact,"intact finite armor still resists hard stagger "+fps);
            Object.Destroy(actor);ClearShots();yield return null;
        }
        File.WriteAllLines(Path.Combine(output,"weapon-observations.txt"),observations);
        var hud=Hud(gm,p,output,check,capture);while(hud.MoveNext())yield return hud.Current;
    }
    public static IEnumerator Hud(GameManager gm,PlayerController p,string output,Action<bool,string> check,Action<string> capture)
    {
        Time.captureDeltaTime=1f/60;gm.ExitPractice();for(int i=0;i<15;i++)yield return null;
        check(p.Loadout.Select(PrimaryWeapon.M14),"HUD case selects actual M14 in hangar");gm.BeginP0Combat();
        for(int i=0;i<20;i++)yield return null;p.enabled=false;p.InputRouter.readKeyboard=false;
        p.RestoreAt(new Vector3(0,.1f,0));p.GetComponent<Damageable>().SetInvulnerable(120);p.AimAt(new Vector3(0,1.1f,35));
        for(int i=0;i<12;i++)yield return null;
        var state=Object.FindObjectsByType<Text>(FindObjectsSortMode.None).FirstOrDefault(t=>t.name=="WeaponStateText");
        check(state!=null&&state.text.Contains("/ 4")&&state.cachedTextGenerator.vertexCount>4,"live HUD reports the equipped weapon cycle with visible glyphs");
        capture("arms-range-production.png");
        int serviceBudget=300;while(p.weaponController.PrimaryRecoveryRemaining<=0&&serviceBudget-->0){p.Simulate(Command(p,true),Time.deltaTime);yield return null;}
        string began=$"budget={serviceBudget} cycle={p.weaponController.PrimaryRoundsRemaining} recovery={p.weaponController.PrimaryRecoveryRemaining} phase={gm.Phase} stance={p.Stance.State} time={Time.time} real={Time.unscaledTime} text={state.text}";
        for(int i=0;i<8;i++)yield return null;
        File.WriteAllText(Path.Combine(output,"hud-diagnostic.txt"),began+"\n"+$"cycle={p.weaponController.PrimaryRoundsRemaining} recovery={p.weaponController.PrimaryRecoveryRemaining} phase={gm.Phase} time={Time.time} real={Time.unscaledTime} text={state.text}");
        capture("arms-service-production.png");
        check(state.text.Contains("整备")||state.text.Contains("RELOAD"),"HUD explains actual automatic service");capture("arms-service-production.png");
        gm.ExitPractice();yield return null;
        check(p.weaponController.PrimaryRecoveryRemaining==0,"hangar exit clears service and queued fire");
    }
}
