using System;
using System.Collections;
using System.IO;
using UnityEngine;

// Live Player checks for the approved free gun/sword rebuild, independent of legacy Break expectations.
public static class HadesRebuildChecks
{
    public static IEnumerator Run(GameManager gm, PlayerController p, string output, Action<bool,string> check, Action<string> capture)
    {
        var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-rebuildModule");
        int module=at>=0&&at+1<args.Length?int.Parse(args[at+1]):13;
        check(CombatRuntime.Run!=null,"run state created by the production game entry");
        var original=CombatRuntime.Run;int token=original.Generation;
        CombatRuntime.BeginRun(9172026);
        check(!CombatRuntime.Owns(token)&&CombatRuntime.Run.Seed==9172026,"new run invalidates prior run ownership and pins its own seed");
        check(CombatRuntime.Run.Encounter==0&&!CombatRuntime.Run.Finished,"new run cannot inherit previous progress");
        var rules=CombatRules.Current;CombatRuntime.BeginRun(9172026);
        check(ReferenceEquals(rules,CombatRules.Current),"run restart reuses immutable definitions, not mutable run state");
        int generation=CombatRuntime.ActionGeneration;gm.SetPaused(true);
        float simulation=CombatRuntime.SimulationTime, presentation=CombatRuntime.PresentationTime;
        for(int i=0;i<8;i++)yield return null;
        check(CombatRuntime.SimulationTime==simulation&&CombatRuntime.PresentationTime>=presentation,"pause freezes simulation while presentation clock remains independent");
        check(!p.Actions.Pending(BufferedCombatAction.Dash,Time.time),"pause clears pending combat input");
        gm.SetPaused(false);
        p.Actions.Enqueue(BufferedCombatAction.Slash,Time.time,.25f);
        PlayerInputRouter.AllowUnfocusedReplay=false;p.InputRouter.SendMessage("OnApplicationFocus",false);
        check(gm.IsPaused&&!p.Actions.Pending(BufferedCombatAction.Slash,Time.time),"loss of focus pauses and discards outstanding input");
        PlayerInputRouter.AllowUnfocusedReplay=true;gm.SetPaused(false);CombatRuntime.EndRun();
        check(!CombatRuntime.Owns(CombatRuntime.Run.Generation)&&CombatRuntime.ActionGeneration>generation,"end run invalidates action work");
        CombatRuntime.BeginRun(9172026);
        if(module>=2)
        {
            var follow=Camera.main.GetComponent<CameraFollow>();
            check(Mathf.Approximately(follow.CombatSize,CameraFollow.StandardCombatSize),"default battle view uses the shared C profile");
            follow.AddShake(20,5);
            check(follow.ShakeAmplitude<=.24f,"camera bounds extreme feedback amplitude");
            for(int i=0;i<45;i++)yield return null;
            check(Camera.main.orthographic&&Mathf.Abs(Camera.main.orthographicSize-CameraFollow.StandardCombatSize)<.05f,"hit feedback cannot zoom the normal battle camera");
            p.RestoreAt(new Vector3(25,.1f,22));for(int i=0;i<60;i++)yield return null;
            check(Mathf.Abs(follow.Focus.x)+Camera.main.orthographicSize*(16f/9f)<30f&&Mathf.Abs(follow.Focus.z)<17,"camera clamps the viewport footprint, including at the arena edge");
            capture("v4-edge-camera.png");p.RestoreAt(new Vector3(0,.1f,-4));
        }
        if(module>=3)
        {
            var queue=new CombatActionQueue();queue.Enqueue(BufferedCombatAction.Dash,Time.time,.18f,Vector3.right,true);
            check(queue.TryRead(BufferedCombatAction.Dash,Time.time,out var request)&&request.Identity>0&&request.Direction==Vector3.right,"buffer captures identity, time, cancellation and original direction");
            check(!queue.Pending(BufferedCombatAction.Dash,Time.time+.181f),"expired input cannot execute late");
            queue.Enqueue(BufferedCombatAction.Slash,Time.time,.25f);CombatRuntime.InvalidateActions();
            check(!queue.Pending(BufferedCombatAction.Slash,Time.time),"global cancellation rejects requests from an older action generation");
            foreach(int fps in new[]{30,60,120})
            {
                Time.captureDeltaTime=1f/fps;p.stats.ResetStats();p.RestoreAt(new Vector3(0,.1f,-8));yield return null;
                float start=p.transform.position.z;
                for(int i=0;i<Mathf.RoundToInt(p.dashDuration*fps);i++)
                {p.Simulate(new PlayerCommand{Move=Vector2.up,Dash=i==0},Time.deltaTime);yield return null;}
                check(Mathf.Abs(p.transform.position.z-start-p.stats.DashDistance)<.08f,"live dash integrates configured distance at "+fps+" FPS");
            }
            Time.captureDeltaTime=1f/60;p.RestoreAt(new Vector3(0,.1f,-4));
        }
        if(module>=4)
        {
            int shots=0;Action onShot=()=>shots++;p.weaponController.BeamFired+=onShot;
            p.stats.ResetStats();p.RestoreAt(new Vector3(0,.1f,-4));
            p.Simulate(new PlayerCommand{HasAim=true,AimPoint=new Vector3(0,1,15),Fire=true},Time.deltaTime);yield return null;
            check(shots==1,"base gun fires from ranged stance");
            p.Simulate(new PlayerCommand{HasAim=true,AimPoint=new Vector3(0,1,15),Melee=true,Fire=true},Time.deltaTime);yield return null;
            for(int i=0;i<10&&!p.Melee.IsAttacking;i++){p.Simulate(default,Time.deltaTime);yield return null;}
            check(p.Melee.IsAttacking&&shots==1,"melee wins over same-frame primary input and starts from the gun stance");
            int action=p.Melee.ActionIdentity;
            p.Simulate(new PlayerCommand{Dash=true,Move=Vector2.right,Fire=true,HasAim=true,AimPoint=new Vector3(0,1,15)},Time.deltaTime);yield return null;
            check(!p.Melee.IsAttacking&&p.IsDashing&&shots==1,"dash immediately cancels blade contact and blocks firing");
            for(int i=0;i<35;i++){p.Simulate(default,Time.deltaTime);yield return null;}
            check(!p.Melee.IsAttacking&&p.Melee.ActionIdentity==action&&shots==1,"cancelled attack produces no delayed combo or bullet");
            for(int i=0;i<10;i++){p.Simulate(new PlayerCommand{Fire=true,HasAim=true,AimPoint=new Vector3(0,1,15)},Time.deltaTime);yield return null;}
            check(shots>1&&p.Stance.CanFire,"gun returns after sword and dash without a mode-specific permission");
            p.weaponController.BeamFired-=onShot;p.CancelMovement();
        }
        if(module>=5)
        {
            var go=new GameObject("V4 damage contract target");var hp=go.AddComponent<Damageable>();hp.team=1;hp.destroyOnDeath=false;hp.RestoreLife(110,110);
            var source=p.GetComponent<Damageable>();int deaths=0,resolved=0;
            hp.OnDied+=_=>deaths++;hp.OnResolved+=(_,r)=>resolved++;
            var first=hp.ApplyDamage(78,new DamageInfo(p.gameObject,p.transform.position,source,78){MeleeStrike=true,Kind=CombatHitKind.Melee});
            check(first.HealthDamage==78&&!first.Lethal&&!first.BreakFinisher,"ordinary first cut damages health with no posture or execution gate");
            var second=hp.ApplyDamage(78,new DamageInfo(p.gameObject,p.transform.position,source,78){MeleeStrike=true});
            hp.ApplyDamage(78,new DamageInfo(p.gameObject,p.transform.position,source,78));
            check(second.Lethal&&deaths==1&&resolved==2,"ordinary soldier dies in two cuts and duplicate contact cannot award another kill");
            hp.RestoreLife(110,110);var armor=go.AddComponent<ArmorHealth>();armor.Configure(100);
            var hit=hp.ApplyDamage(60,new DamageInfo(p.gameObject,p.transform.position,source,60){Kind=CombatHitKind.Rifle});
            check(hit.Applied&&hit.ArmorDamage==60&&hit.HealthDamage==0&&!hit.BrokeArmor&&hp.CurrentHealth==110,"armor-only rifle contact is a valid committed hit");
            hit=hp.ApplyDamage(78,new DamageInfo(p.gameObject,p.transform.position,source,78){MeleeStrike=true});
            check(hit.BrokeArmor&&hit.ArmorDamage==40&&hit.HealthDamage==0&&hp.CurrentHealth==110,"breaking blade contact consumes remaining armor without spilling to health");
            for(int i=0;i<120;i++)yield return null;
            check(armor.Current==0,"depleted armor cannot regenerate or create another posture layer");
            hit=hp.ApplyDamage(18,new DamageInfo(p.gameObject,p.transform.position,source,18){Kind=CombatHitKind.Rifle});
            check(hit.HealthDamage==18&&hit.ArmorDamage==0,"rifle independently damages health after armor depletion");
            hp.RestoreLife(110,110);check(armor.Current==100,"pooled enemy reset restores its own armor definition");
            UnityEngine.Object.Destroy(go);yield return null;
        }
        if(module>=6)
        {
            gm.stageManager.StopStage();p.RestoreAt(new Vector3(0,.1f,-4));p.GetComponent<Damageable>().SetInvulnerable(30);
            var enemy=gm.stageManager.enemySpawner.SpawnEnemy(EnemyKind.Ranged,new Vector3(0,0,5));
            var motion=enemy.GetComponent<E01SoldierMotion>();if(motion!=null)motion.TrainingTarget=true;
            var hp=enemy.GetComponent<Damageable>();hp.destroyOnDeath=false;hp.RestoreLife(10000,10000);
            for(int i=0;i<120&&enemy.AttackPhase!=EnemyAttackPhase.Windup;i++)yield return null;
            check(enemy.AttackPhase==EnemyAttackPhase.Windup&&enemy.HasAttackWarning,"live rifle attack announces a windup before emission");
            var direction=enemy.CommittedShotDirection;p.RestoreAt(new Vector3(6,.1f,-4));
            for(int i=0;i<50&&enemy.ShotsCommitted==0;i++)yield return null;
            check(enemy.ShotsCommitted==1&&enemy.CommittedShotDirection==direction,"moving during warning does not retarget the committed projectile");
            int before=enemy.ShotsCommitted,staggered=0,resisted=0;
            for(int i=0;i<240;i++)
            {
                if(i%11==0)
                {
                    var r=hp.ApplyDamage(1,new DamageInfo(p.gameObject,p.transform.position,p.GetComponent<Damageable>(),1){Kind=CombatHitKind.Rifle});
                    if(r.Staggered)staggered++;else resisted++;
                }
                yield return null;
            }
            check(staggered>0&&resisted>0&&enemy.ShotsCommitted>before,"continuous rifle contacts cause reactions without infinitely denying all enemy attacks");
            for(int i=0;i<180&&enemy.AttackPhase!=EnemyAttackPhase.Windup;i++)yield return null;
            before=enemy.ShotsCommitted;hp.Kill(new DamageInfo(p.gameObject,p.transform.position,p.GetComponent<Damageable>(),10000));
            for(int i=0;i<60;i++)yield return null;
            check(enemy.ShotsCommitted==before&&!enemy.HasAttackWarning,"death cancels the unfinished attack and its warning without a late shot");
            UnityEngine.Object.Destroy(enemy.gameObject);gm.stageManager.StopStage();yield return null;
        }
        if(module>=7)
        {
            p.RestoreAt(new Vector3(0,.1f,0));var blade=p.GetComponentInChildren<RaikenBladePresentation>();
            int impacts=blade.ImpactCount,stops=blade.ImpactStops;
            p.Simulate(new PlayerCommand{Melee=true,HasAim=true,AimPoint=new Vector3(0,1,10)},Time.deltaTime);
            for(int i=0;i<40;i++){p.Simulate(default,Time.deltaTime);yield return null;}
            check(blade.ImpactCount==impacts&&blade.ImpactStops==stops,"empty swing has no hit report or impact pause");
            p.RestoreAt(new Vector3(0,.1f,0));
            var targets=new GameObject[2];
            for(int i=0;i<2;i++)
            {
                targets[i]=UnityEngine.Object.Instantiate(gm.stageManager.enemySpawner.meleePrefab,new Vector3(i==0?-.35f:.35f,.1f,2.8f),Quaternion.identity);
                var enemy=targets[i].GetComponent<EnemyBase>();enemy.ConfigureP0Role(EnemyKind.Melee);enemy.TrainingTarget=true;enemy.Init(p.transform,null);enemy.enabled=false;
                targets[i].GetComponent<E01SoldierMotion>().TrainingTarget=true;targets[i].GetComponent<Damageable>().RestoreLife(1000,1000);
            }
            yield return null;p.Simulate(new PlayerCommand{Melee=true,HasAim=true,AimPoint=new Vector3(0,1,10)},Time.deltaTime);
            for(int i=0;i<40;i++){p.Simulate(default,Time.deltaTime);if(i==12)capture("v4-blade-contact.png");yield return null;}
            check(blade.ImpactCount==impacts+2&&blade.ImpactStops==stops+1,"one real blade sweep hits two targets once and pauses only once");
            foreach(var go in targets){check(go.GetComponent<Damageable>().CurrentHealth==922,"actual blade contact deduplicates damage per target");UnityEngine.Object.Destroy(go);}
            check(blade.TrailSamples<=96,"blade ribbon has a fixed sample budget");
            yield return null;
            foreach(float hitch in new[]{.10f,.18f,.32f})foreach(bool covered in new[]{false,true})
            {
                p.RestoreAt(new Vector3(0,.1f,0));yield return null;
                var go=UnityEngine.Object.Instantiate(gm.stageManager.enemySpawner.meleePrefab,new Vector3(0,.1f,2.8f),Quaternion.identity);
                var e=go.GetComponent<EnemyBase>();e.ConfigureP0Role(EnemyKind.Melee);e.TrainingTarget=true;e.Init(p.transform,null);e.enabled=false;
                go.GetComponent<E01SoldierMotion>().TrainingTarget=true;var health=go.GetComponent<Damageable>();health.RestoreLife(1000,1000);
                GameObject wall=null;
                if(covered){wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.transform.position=new Vector3(0,1.5f,1.4f);wall.transform.localScale=new Vector3(12,4,.25f);}
                Physics.SyncTransforms();yield return null;
                Time.captureDeltaTime=hitch;p.Simulate(new PlayerCommand{Melee=true,HasAim=true,AimPoint=new Vector3(0,1,15)},hitch);
                for(int i=0;i<12;i++){p.Simulate(default,Time.deltaTime);yield return null;}
                check(health.CurrentHealth==(covered?1000:922),"long frame retains swept blade contact, per-cut deduplication and solid cover: "+hitch+" covered="+covered);
                UnityEngine.Object.Destroy(go);if(wall!=null)UnityEngine.Object.Destroy(wall);Time.captureDeltaTime=1f/60;yield return null;
            }
        }
        if(module>=8)
        {
            var audio=GameAudio.Instance;check(audio!=null&&audio.LoadedCueCount>=30,"shared mix contains gun, blade, propulsion, enemies and salvage cues");
            foreach(int fps in new[]{30,60,120})
            {
                Time.captureDeltaTime=1f/fps;p.RestoreAt(new Vector3(0,.1f,-4));yield return null;
                int shots=0,sounds=0;Action fired=()=>shots++;Action<GameAudioCue> cue=c=>{if(c==GameAudioCue.RifleShot)sounds++;};
                p.weaponController.BeamFired+=fired;GameAudio.CuePlayed+=cue;
                for(int i=0;i<fps;i++){p.Simulate(new PlayerCommand{Fire=true,HasAim=true,AimPoint=new Vector3(0,1,18)},Time.deltaTime);yield return null;}
                check(shots>0&&sounds==shots,"one rifle cue per actual discharge at "+fps+" FPS");
                int before=shots;for(int i=0;i<fps/2;i++){p.Simulate(default,Time.deltaTime);yield return null;}
                check(shots==before&&sounds==before,"release has no late gun or sound event at "+fps+" FPS");
                p.weaponController.BeamFired-=fired;GameAudio.CuePlayed-=cue;
            }
            Time.captureDeltaTime=1f/60;p.stats.ResetStats();
            for(int i=0;i<30;i++){p.Simulate(new PlayerCommand{BoostHeld=true,Move=Vector2.right},Time.deltaTime);yield return null;}
            check(audio.BoostLevel>0,"movement drives shared propulsion mix");
            p.CancelMovement();GameAudio.ResetCombatSound();
            check(audio.BoostLevel==0&&!audio.BoostPlaying,"combat cancellation stops the propulsion loop");
        }
        if(module>=9)
        {
            var intakeChecks=SliceAbsorptionChecks.Run(gm,p,output,check,capture);
            while(intakeChecks.MoveNext())yield return intakeChecks.Current;
            var intake=gm.equipmentLoop.Absorption;
            var enemy=gm.stageManager.enemySpawner.SpawnEnemy(EnemyKind.Ranged,new Vector3(0,.1f,3));
            enemy.GetComponent<Damageable>().Kill(new DamageInfo(p.gameObject,p.transform.position,p.GetComponent<Damageable>(),9999));
            for(int i=0;i<240;i++)yield return null;
            check(intake.Offering&&intake.Module!=null,"released weapon remains offered beyond the former three-second timeout");
            var oldGun=p.Loadout.Selected;
            check(intake.CollectWithoutInstalling()&&gm.equipmentLoop.Warehouse.Owns("e01_rifle"),"explicit continue can collect an untried weapon");
            check(p.Loadout.Selected==oldGun&&!intake.Installed&&intake.Pickup==null,"collection without installation preserves the current gun");
        }
        if(module>=11)
        {
            var upgrades=gm.upgradeSystem;upgrades.ResetUpgrades(917);
            for(int room=0;room<6;room++)
            {
                var choices=upgrades.GenerateOptions();var distinct=new System.Collections.Generic.HashSet<RunUpgradeKind>();
                foreach(var choice in choices){distinct.Add(choice.kind);check(upgrades.IsEffective(choice.kind)&&choice.rank<=3,"reward candidate affects carried equipment and respects rank cap");}
                check(choices.Count==3&&distinct.Count==3,"six-run reward offers remain useful and unique at choice "+room);
                check(upgrades.ApplyOption(choices[0])&&!upgrades.ApplyOption(choices[0])&&!upgrades.ApplyOption(choices[1]),"only one card from an offer can commit");
            }
            check(upgrades.Count==6,"one run records exactly six choices");upgrades.ResetUpgrades(917);
            var blade=p.GetComponentInChildren<RaikenBladePresentation>();yield return null;float reach=blade.Reach;
            upgrades.Restore(new[]{RunUpgradeKind.BladeReach,RunUpgradeKind.BladeReach,RunUpgradeKind.BladeReach},917);
            for(int i=0;i<3;i++)yield return null;
            check(Mathf.Abs(blade.Reach/reach-1.3f)<.01f,"three reach ranks extend the actual visible and swept blade by 30 percent");
            for(int i=0;i<4;i++)gm.equipmentLoop.ApplyLoadout();
            check(Mathf.Approximately(upgrades.BladeReachMultiplier,1.3f)&&upgrades.Count==3,"reapplying equipment cannot compound run modifiers");
            upgrades.Restore(new[]{RunUpgradeKind.ThrusterEfficiency,RunUpgradeKind.ThrusterEfficiency,RunUpgradeKind.ThrusterEfficiency},917);
            p.RestoreAt(new Vector3(0,.1f,-4));p.stats.ResetStats();float energy=p.stats.CurrentEnergy;
            check(p.TryDash(Vector2.up)&&Mathf.Abs(energy-p.stats.CurrentEnergy-17.5f)<.01f,"three propulsion ranks spend 70 percent of base dash cost");
            p.CancelMovement();upgrades.Restore(new[]{RunUpgradeKind.BladeTempo,RunUpgradeKind.BladeTempo,RunUpgradeKind.BladeTempo},917);p.RestoreAt(new Vector3(0,.1f,-4));
            check(Mathf.Approximately(p.Melee.AttackSpeed,1.24f),"blade timing reads the additive action speed modifier");
            p.Simulate(new PlayerCommand{Melee=true,HasAim=true,AimPoint=new Vector3(0,1,15)},Time.deltaTime);
            while(!p.Melee.IsAttacking){p.Simulate(default,Time.deltaTime);yield return null;}
            float started=Time.time;while(p.Melee.IsAttacking){p.Simulate(default,Time.deltaTime);yield return null;}
            check(Time.time-started<.39f,"blade speed upgrade shortens the real animation and collision timeline");
            upgrades.ResetUpgrades(917);yield return null;
            check(upgrades.Count==0&&upgrades.BladeReachMultiplier==1&&upgrades.FireRateMultiplier==1&&upgrades.BonusPierce==0,"new run clears every modifier back to base values");
        }
        if(module>=12)
        {
            var bossChecks=RebuildBossChecks.Run(gm,p,check,capture);while(bossChecks.MoveNext())yield return bossChecks.Current;
        }
        if(module>=13){var ui=RebuildUiChecks.Run(gm,p,output,check,capture);while(ui.MoveNext())yield return ui.Current;}
        File.WriteAllText(Path.Combine(output,"module.txt"),"module="+module+"; runtime checks use live Player; subjective feel remains unconfirmed");
        capture("module-"+module.ToString("00")+".png");
    }
}
