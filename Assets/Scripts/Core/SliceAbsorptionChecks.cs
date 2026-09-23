using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.AI;

public static class SliceAbsorptionChecks
{
    public static IEnumerator Run(GameManager gm,PlayerController p,string output,Action<bool,string> check,Action<string> capture)
    {
        var loop=gm.equipmentLoop;var intake=loop.Absorption;
        check(intake!=null,"slice absorption is active");
        foreach(int fps in new[]{30,60,120})
        {
            Time.captureDeltaTime=1f/fps;loop.ClearPickups();
            gm.EnterHangar();p.Loadout.Select(PrimaryWeapon.M7);gm.BeginP0Combat();
            p.RestoreAt(new Vector3(0,.1f,0));p.enabled=false;
            yield return null;
            check(NavMesh.SamplePosition(new Vector3(0,0,4),out var spot,2,NavMesh.AllAreas),"carrier test location exists");
            var go=UnityEngine.Object.Instantiate(gm.stageManager.enemySpawner.meleePrefab,spot.position,Quaternion.identity);
            var enemy=go.GetComponent<EnemyBase>();enemy.ConfigureP0Role(EnemyKind.Ranged);enemy.Init(p.transform,null);
            var carrier=go.GetComponent<SalvageCarrier>();
            check(carrier!=null,"first production ranged soldier is the designated carrier");
            var original=carrier.visual;int identity=original.GetInstanceID();
            go.GetComponent<Damageable>().Kill(new DamageInfo(p.gameObject,p.transform.position,p.GetComponent<Damageable>(),9999));
            for(int i=0;i<fps;i++)yield return null;
            check(intake.Module!=null&&intake.Module.GetInstanceID()==identity,"original weapon survives corpse disposal");
            p.RestoreAt(new Vector3(0,.1f,-12));yield return null;
            check(loop.AbsorbNearby()==0,"F rejects out of range pickup");
            p.RestoreAt(new Vector3(0,.1f,0));yield return null;
            var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.transform.position=new Vector3(0,1.5f,2);wall.transform.localScale=new Vector3(8,3,.3f);Physics.SyncTransforms();
            check(loop.AbsorbNearby()==0,"F cannot pull through a solid wall");UnityEngine.Object.Destroy(wall);yield return null;
            check(intake.CanAbsorb,"clear nearby target is offered");
            if(fps==30)
            {
                var warehouse=loop.Warehouse;string bad=Path.Combine(output,"blocked-save");Directory.CreateDirectory(bad);
                typeof(EquipmentLoop).GetProperty("Warehouse").SetValue(loop,new SalvageWarehouse(bad));
                check(loop.AbsorbNearby()==0&&!intake.Busy&&intake.Module==original,"save failure keeps physical pickup and old weapon");
                typeof(EquipmentLoop).GetProperty("Warehouse").SetValue(loop,warehouse);
            }
            check(loop.AbsorbNearby()==1,"F begins one absorption at "+fps+" FPS");
            check(loop.AbsorbNearby()==0&&loop.AbsorbNearby(true)==0,"repeated F and batch collection cannot duplicate transaction");
            check(!p.Stance.CanFire&&!p.Stance.CanMelee,"intake hand owns attack pose during ceremony");
            for(int i=0;i<4;i++)yield return null;
            gm.SetPaused(true);float paused=intake.Progress;for(int i=0;i<5;i++)yield return null;
            check(intake.Progress==paused,"pause freezes ceremony");gm.SetPaused(false);
            check(p.TryDash(Vector2.right),"real dash can interrupt intake");
            check(!intake.Busy&&!intake.Installed&&p.Loadout.Selected==PrimaryWeapon.M7,"dash immediately retains original gun");
            check(loop.Warehouse.Owns("e01_rifle")&&intake.Module==original,"cancel preserves acquired ownership and retryable original mesh");
            for(int i=0;i<fps;i++){p.Simulate(new PlayerCommand(),Time.deltaTime);yield return null;}
            p.RestoreAt(new Vector3(0,.1f,0));yield return null;
            check(loop.AbsorbNearby()==1,"cancelled absorption can restart");
            float health=p.stats.CurrentHp;p.GetComponent<Damageable>().TakeDamage(2,new DamageInfo(null,p.transform.position+Vector3.forward,null,2));
            check(p.stats.CurrentHp<health&&!intake.Busy,"actual damage interrupts without ceremony invulnerability");
            var sounds=new List<GameAudioCue>();Action<GameAudioCue> listen=c=>sounds.Add(c);GameAudio.CuePlayed+=listen;
            check(loop.AbsorbNearby()==1,"damaged absorption can restart");
            var phases=new HashSet<SliceAbsorption.Step>();float began=Time.time;
            while(intake.Busy&&Time.time-began<2)
            {
                if(phases.Add(intake.Phase)&&fps==60)capture("r6_"+intake.Phase+".png");
                yield return null;
            }
            check(phases.Count==4&&intake.Installed,"all four ceremony phases complete at "+fps+" FPS");
            GameAudio.CuePlayed-=listen;
            foreach(var cue in new[]{GameAudioCue.SalvagePull,GameAudioCue.SalvageCatch,GameAudioCue.SalvageLock,GameAudioCue.SalvageReady})
                check(sounds.FindAll(c=>c==cue).Count==1,"one actual ceremony sound for "+cue+" at "+fps+" FPS");
            yield return null;
            check(p.GetComponent<E01PlayerRifle>().Model.GetInstanceID()==identity&&intake.Pickup==null,"same recovered weapon becomes held model");
            check(loop.Weapon.id=="e01_rifle"&&p.Loadout.CanUseSword&&p.Loadout.CanUseRifle,"recovered rifle is functional while sword remains available");
            int shots=0;Action shot=()=>shots++;p.weaponController.BeamFired+=shot;
            p.AimAt(new Vector3(0,1.6f,18));
            p.Simulate(new PlayerCommand{Fire=true,HasAim=true,AimPoint=new Vector3(0,1.6f,18)},Time.deltaTime);yield return null;
            p.weaponController.BeamFired-=shot;
            check(shots==1&&p.weaponController.muzzle.IsChildOf(original.transform),"actual recovered-rifle fire uses the recovered muzzle");
            p.Simulate(new PlayerCommand{Melee=true,HasAim=true,AimPoint=new Vector3(0,1.6f,18)},Time.deltaTime);
            for(int i=0;i<fps/5;i++){p.Simulate(new PlayerCommand(),Time.deltaTime);yield return null;}
            check(p.Melee.IsAttacking,"sword attack still works after installation");
            gm.EnterHangar();p.Loadout.Select(PrimaryWeapon.M7);gm.BeginP0Combat();yield return null;
            check(!intake.Installed&&intake.Pickup==null&&p.Loadout.Selected==PrimaryWeapon.M7,"restart returns to baseline gun without a stale pickup");
            check(loop.Warehouse.Profile.owned.FindAll(x=>x=="e01_rifle").Count==1,"repeated recoveries persist one ownership entry");
        }
        Time.captureDeltaTime=1f/60;
    }
}
