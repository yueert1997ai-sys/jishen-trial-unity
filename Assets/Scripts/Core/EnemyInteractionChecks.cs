using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using Object=UnityEngine.Object;

public static class EnemyInteractionChecks
{
    static EnemyBase Spawn(GameManager gm,EnemySpawnSpec spec,Vector3 at)
    {
        var actor=gm.stageManager.enemySpawner.SpawnEnemy(spec.role,at);
        actor.gameObject.AddComponent<EnemyArsenal>().Configure(spec);
        actor.GetComponent<Damageable>().RestoreLife(5000,5000);
        return actor;
    }
    static void Clear()
    {
        foreach(var actor in Object.FindObjectsByType<EnemyBase>(FindObjectsSortMode.None))Object.Destroy(actor.gameObject);
        foreach(var shot in Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None))shot.Despawn();
    }
    static DamageInfo Hit(PlayerController p)=>new DamageInfo(p.gameObject,p.transform.position,p.GetComponent<Damageable>(),10);
    public static IEnumerator Run(GameManager gm,PlayerController p,string output,Action<bool,string> check,Action<string> capture)
    {
        var log=new List<string>();
        gm.ExitPractice();yield return null;gm.BeginWeaponTrial();for(int i=0;i<20;i++)yield return null;
        Object.FindFirstObjectByType<WeaponTrial>().ClearTargets();yield return null;
        p.enabled=false;p.InputRouter.readKeyboard=false;
        foreach(int fps in new[]{30,60,120})
        {
            Time.captureDeltaTime=1f/fps;yield return null;p.RestoreAt(new Vector3(0,.1f,0));p.stats.ResetStats();p.GetComponent<Damageable>().SetInvulnerable(120);
            var a=Spawn(gm,new EnemySpawnSpec("E01",EnemyWeapon.M7,EnemyKind.Ranged),new Vector3(-2,0,7));
            var b=Spawn(gm,new EnemySpawnSpec("GM",EnemyWeapon.M14,EnemyKind.Ranged),new Vector3(2,0,8));
            var c=Spawn(gm,new EnemySpawnSpec("DOM",EnemyWeapon.Ax01,EnemyKind.Ranged),new Vector3(-5,0,10));
            check(a.CombatBrain.Profile.role==EnemyBattleRole.Assault&&b.CombatBrain.Profile.role==EnemyBattleRole.Marksman&&c.CombatBrain.Profile.role==EnemyBattleRole.Skirmisher,"actual equipped enemies select distinct engagement roles "+fps);
            int peak=0;bool separated=false;Vector3 aStart=a.transform.position,bStart=b.transform.position;
            for(int frame=0;frame<fps*6;frame++)
            {
                peak=Mathf.Max(peak,EnemyTactics.AttackersActive);
                if(a.CombatBrain.HasGoal&&b.CombatBrain.HasGoal&&Vector3.Distance(a.TacticalGoal,b.TacticalGoal)>3)separated=true;
                if(frame==fps/2)a.GetComponent<Damageable>().ApplyDamage(10,Hit(p));
                if(frame==fps*2&&fps==60)capture("interaction-crossfire-production.png");
                yield return null;
            }
            check(peak<=3&&a.ShotsEmitted+b.ShotsEmitted+c.ShotsEmitted>=3,"coordinated live volleys respect attack budget "+fps);
            check(separated&&Vector3.Distance(aStart,a.transform.position)+Vector3.Distance(bStart,b.transform.position)>2,"reserved firing routes diverge and enemies actually reposition "+fps);
            check(a.CombatBrain.PressureReactions>0,"real incoming hit changes the victim's movement plan "+fps);
            check(b.CombatBrain.AllyReactions+c.CombatBrain.AllyReactions>0,"visible ally under fire triggers a different firing angle "+fps);
            check(a.GetComponent<NavMeshAgent>().isOnNavMesh&&b.GetComponent<NavMeshAgent>().isOnNavMesh&&c.GetComponent<NavMeshAgent>().isOnNavMesh,"different routes remain on navigation "+fps);
            log.Add($"fps={fps} pressure={a.CombatBrain.PressureReactions} ally={b.CombatBrain.AllyReactions+c.CombatBrain.AllyReactions} shots={a.ShotsEmitted+b.ShotsEmitted+c.ShotsEmitted} plans={a.CombatBrain.Plans+b.CombatBrain.Plans+c.CombatBrain.Plans}");
            Clear();yield return null;

            p.RestoreAt(new Vector3(0,.1f,0));p.stats.ResetStats();p.GetComponent<Damageable>().SetInvulnerable(120);
            a=Spawn(gm,new EnemySpawnSpec("GM",EnemyWeapon.M14,EnemyKind.Ranged),new Vector3(0,0,3.8f));
            var before=a.transform.position;int firstRetreat=-1;
            for(int frame=0;frame<fps*5;frame++)
            {if(a.CombatBrain.Retreats>0&&firstRetreat<0)firstRetreat=frame;yield return null;}
            check(firstRetreat>=Mathf.FloorToInt(fps*.10f)&&firstRetreat<fps,"close player produces a delayed physical disengage "+fps);
            check(Vector3.Distance(before,a.transform.position)>1&&a.CombatBrain.Retreats<=2,"disengage has actual travel and a cooldown "+fps);
            check(a.ShotsEmitted>0,"pressured marksman plants and fires instead of fleeing indefinitely "+fps);
            gm.SetPaused(true);before=a.transform.position;int plans=a.CombatBrain.Plans;
            for(int i=0;i<12;i++)yield return null;
            check(Vector3.Distance(before,a.transform.position)<.01f&&plans==a.CombatBrain.Plans,"pause freezes decisions and navigation "+fps);gm.SetPaused(false);
            Clear();yield return null;

            p.RestoreAt(new Vector3(0,.1f,0));p.stats.ResetStats();p.GetComponent<Damageable>().SetInvulnerable(120);
            a=Spawn(gm,new EnemySpawnSpec("GUNCANNON",EnemyWeapon.BackCannon,EnemyKind.Ranged),new Vector3(0,0,10));
            for(int i=0;i<fps*3&&a.AttackPhase!=EnemyAttackPhase.Windup;i++)yield return null;
            check(a.AttackPhase==EnemyAttackPhase.Windup,"heavy cannon visibly announces its attack "+fps);
            var direction=a.CommittedShotDirection;p.RestoreAt(new Vector3(-6,.1f,0));
            for(int i=0;i<fps*2&&a.ShotsEmitted==0;i++)yield return null;
            check(a.ShotsEmitted>0&&Vector3.Angle(direction,a.CommittedShotDirection)<.01f,"dodging a committed cannon leaves the warned lane unchanged "+fps);
            Clear();yield return null;

            p.RestoreAt(new Vector3(0,.1f,0));p.stats.ResetStats();
            a=Spawn(gm,new EnemySpawnSpec("ZAKU",EnemyWeapon.Rocket,EnemyKind.Melee),new Vector3(0,0,3.7f));
            for(int i=0;i<fps*3&&a.AttackPhase!=EnemyAttackPhase.Windup;i++)yield return null;
            check(a.AttackPhase==EnemyAttackPhase.Windup,"close attacker announces a gap closer "+fps);
            Vector3 launch=a.transform.position;float hp=p.stats.CurrentHp;
            p.Simulate(new PlayerCommand{Move=Vector2.right,Dash=true},Time.deltaTime);yield return null;
            for(int i=0;i<fps&&p.IsDashing;i++){p.Simulate(new PlayerCommand{Move=Vector2.right},Time.deltaTime);yield return null;}
            for(int i=0;i<fps*2&&a.LungesCompleted==0;i++)yield return null;
            check(a.LungesCompleted==1&&a.IsInRecovery,"evaded lunge ends in a punishable recovery "+fps);
            check(Mathf.Abs(a.transform.position.x-launch.x)<.1f&&p.stats.CurrentHp==hp,"dash escapes the locked approach instead of being chased during the strike "+fps);
            a.GetComponent<Damageable>().ApplyDamage(10,new DamageInfo(p.gameObject,p.transform.position,p.GetComponent<Damageable>(),10){MeleeStrike=true,HeavyImpact=true,ContactTangent=Vector3.right});yield return null;
            check(a.HitStaggerRemaining>.2f&&!a.HasAttackWarning,"counterhit visibly interrupts a recovering attacker "+fps);
            Clear();yield return null;

            var layout=gm.arenaSector.ActiveLunarLayout;
            p.RestoreAt(layout.coverNear+Vector3.up*.15f);p.GetComponent<Damageable>().SetInvulnerable(120);
            a=Spawn(gm,new EnemySpawnSpec("E01",EnemyWeapon.M7,EnemyKind.Ranged),layout.coverFar);
            check(!EnemyTactics.ClearSight(a.transform.position,p.transform.position),"real map cover blocks new AI sight "+fps);
            before=a.transform.position;var lastKnown=a.LastKnownTarget;
            var hidden=p.transform.position+Vector3.right*.25f;
            check(!EnemyTactics.ClearSight(before,hidden),"hidden target fixture remains behind the same cover "+fps);
            p.RestoreAt(hidden);
            for(int i=0;i<Mathf.Max(3,fps/8);i++)yield return null;
            check(!a.TargetVisible&&Vector3.Distance(a.LastKnownTarget,lastKnown)<.01f&&a.ShotsEmitted==0,"no through-cover tracking or gunfire "+fps);
            for(int i=0;i<fps*16&&a.ShotsEmitted==0;i++)yield return null;
            check(a.ShotsEmitted>0&&Vector3.Distance(before,a.transform.position)>2,"new tactical search finds a reachable lane around actual cover "+fps);
            if(fps==60)capture("interaction-cover-route-production.png");
            Clear();yield return null;check(EnemySquad.Count==0&&EnemyTactics.AttackersActive==0,"destroyed actors release group plans and attack slots "+fps);
        }
        Time.captureDeltaTime=1f/60;p.RestoreAt(new Vector3(0,.1f,-9));p.stats.ResetStats();p.GetComponent<Damageable>().SetInvulnerable(120);
        string[] bodies={"E01","GM","ZAKU","DOM","GUNCANNON"};EnemyWeapon[] guns={EnemyWeapon.M7,EnemyWeapon.M14,EnemyWeapon.Rocket,EnemyWeapon.Ax01,EnemyWeapon.BackCannon};
        for(int i=0;i<5;i++)
        {
            var actor=Spawn(gm,new EnemySpawnSpec(bodies[i],guns[i],EnemyKind.Ranged),new Vector3((i-2)*4.8f,0,4));
            actor.TrainingTarget=true;actor.transform.rotation=Quaternion.Euler(0,180,0);
            check(actor.GetComponentsInChildren<Renderer>().Where(r=>r.enabled&&!(r is ParticleSystemRenderer)&&!(r is LineRenderer)&&!(r is TrailRenderer)).SelectMany(r=>r.sharedMaterials).Where(m=>m!=null).All(m=>m.shader.name!="Standard"),"white shared armor reaches body and equipped weapon "+bodies[i]);
        }
        for(int i=0;i<45;i++)yield return null;
        check(Mathf.Abs(Camera.main.orthographicSize-17)<.01f,"normal gameplay uses camera 17");capture("interaction-five-white-enemies-camera17.png");
        int shared=EnemyArmorPalette.MaterialCount;
        var duplicate=Spawn(gm,new EnemySpawnSpec("GM",EnemyWeapon.M14,EnemyKind.Ranged),new Vector3(0,0,14));duplicate.TrainingTarget=true;
        check(EnemyArmorPalette.MaterialCount==shared,"additional same-model enemy reuses white materials");
        Clear();yield return null;var boss=gm.stageManager.enemySpawner.SpawnFazz(0);boss.enabled=false;
        for(int i=0;i<45;i++)yield return null;check(Mathf.Abs(Camera.main.orthographicSize-17)<.01f,"Boss gameplay also uses camera 17");
        capture("interaction-white-fazz-camera17.png");Object.Destroy(boss.gameObject);yield return null;
        gm.ExitPractice();yield return null;check(EnemySquad.Count==0,"return to hangar clears tactical observations");
        File.WriteAllLines(Path.Combine(output,"interaction-metrics.txt"),log);
    }
}
