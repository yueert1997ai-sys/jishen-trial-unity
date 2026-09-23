using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public static class ImpactPolishChecks
{
    public static IEnumerator Recovery(GameManager gm,PlayerController p,Action<bool,string> check,Action<string> capture)
    {
        var safety=p.GetComponent<CombatRecovery>();GameObject block=null,actor=null;
        try
        {
            foreach(int fps in new[]{30,60,120})
            {
                gm.EnterHangar();gm.BeginP0Combat();gm.stageManager.StopStage();Time.captureDeltaTime=1f/fps;
                p.RestoreAt(new Vector3(-3,.1f,0));yield return null;
                safety.Observe(Vector2.zero,p.transform.position,1f/fps);
                check(safety.IsSafe(p.transform.position,p.transform),"records a navigable capsule-clear fallback at "+fps);
                float hp=p.stats.CurrentHp;var weapon=p.Loadout.Selected;int kills=gm.Kills;
                p.Motor.enabled=false;p.transform.position=new Vector3(0,.1f,0);p.Motor.enabled=true;
                block=GameObject.CreatePrimitive(PrimitiveType.Cube);block.name="Recovery_Test_Trap";block.transform.position=new Vector3(0,1.6f,0);block.transform.localScale=new Vector3(2,3.2f,2);Physics.SyncTransforms();
                for(int i=0;i<fps*2;i++){safety.Observe(Vector2.right,p.transform.position,1f/fps);yield return null;}
                check(safety.PlayerRecoveries==1&&safety.IsSafe(p.transform.position,p.transform),"blocked input automatically returns to clear ground at "+fps);
                check(p.stats.CurrentHp==hp&&p.Loadout.Selected==weapon&&gm.Kills==kills,"recovery preserves health equipment and encounter at "+fps);
                check(!p.Melee.IsAttacking&&!p.IsDashing&&!p.Melee.NextQueued,"recovery clears old action and root motion at "+fps);
                UnityEngine.Object.Destroy(block);block=null;yield return null;
                for(int i=0;i<fps*4;i++){safety.Observe(Vector2.zero,p.transform.position,1f/fps);yield return null;}
                check(safety.PlayerRecoveries==1,"standing still is not treated as stuck at "+fps);
                p.Motor.enabled=false;p.transform.position=new Vector3(0,-4,0);p.Motor.enabled=true;
                gm.SetPaused(true);safety.Observe(Vector2.right,p.transform.position,2);yield return null;
                check(safety.PlayerRecoveries==1,"pause does not advance stuck detection at "+fps);
                check(safety.TryRecover(true),"pause menu can recover fallen player at "+fps);
                check(!gm.IsPaused&&safety.IsSafe(p.transform.position,p.transform)&&safety.PlayerRecoveries==2,"manual recovery resumes on safe ground at "+fps);
                if(fps==60)capture("recovery-safe.png");
            }
            Time.captureDeltaTime=1f/60;gm.EnterHangar();gm.BeginP0Combat();gm.stageManager.StopStage();p.RestoreAt(new Vector3(0,.1f,0));yield return null;
            check(safety.FindSafe(new Vector3(17,.1f,8),null,out var spawn),"finds reachable enemy fixture");
            actor=UnityEngine.Object.Instantiate(gm.stageManager.enemySpawner.meleePrefab,spawn,Quaternion.identity);
            var enemy=actor.GetComponent<EnemyBase>();enemy.ConfigureP0Role(EnemyKind.Melee);enemy.Init(p.transform,null);
            var health=actor.GetComponent<Damageable>();float beforeHealth=health.CurrentHealth;
            var nav=actor.GetComponent<NavMeshAgent>();nav.updatePosition=false;
            for(int i=0;i<480&&safety.EnemyRecoveries==0;i++)
            {nav.nextPosition=spawn;yield return null;}
            nav.updatePosition=true;
            check(safety.EnemyRecoveries==1,"stranded moving enemy is relocated without a fake kill");
            check(health.CurrentHealth==beforeHealth&&!health.IsDead&&nav.isOnNavMesh,"enemy rescue preserves health and rejoins navigation");
            check(!enemy.HasAttackWarning&&enemy.AttackPhase==EnemyAttackPhase.Ready,"relocation cancels stale enemy attack commitment");
            UnityEngine.Object.Destroy(actor);actor=null;
        }
        finally{if(block!=null)UnityEngine.Object.Destroy(block);if(actor!=null)UnityEngine.Object.Destroy(actor);gm.SetPaused(false);Time.captureDeltaTime=1f/60;p.RestoreAt(new Vector3(0,.1f,0));}
    }
    public static IEnumerator Presentation(GameManager gm,PlayerController p,Action<bool,string> check,Action<string> capture)
    {
        gm.stageManager.StopStage();p.RestoreAt(new Vector3(0,.1f,0));yield return null;
        var fx=ImpactAccentVfx.Get();GameObject actor=null;
        try
        {
            actor=UnityEngine.Object.Instantiate(gm.stageManager.enemySpawner.meleePrefab,new Vector3(0,.1f,4),Quaternion.identity);
            var enemy=actor.GetComponent<EnemyBase>();enemy.ConfigureP0Role(EnemyKind.Melee);enemy.Init(p.transform,null);enemy.enabled=false;
            var hp=actor.GetComponent<Damageable>();hp.destroyOnDeath=false;hp.RestoreLife(10000,10000);yield return null;
            Vector3 contact=hp.AimCenter-Vector3.forward*.5f;
            DamageInfo Hit(bool sword=false,bool heavy=false)=>new DamageInfo(p.gameObject,p.transform.position,p.GetComponent<Damageable>(),1)
                {Kind=sword?CombatHitKind.Melee:CombatHitKind.Rifle,MeleeStrike=sword,HeavyImpact=heavy,HasContact=true,ContactPoint=contact,ContactNormal=Vector3.back,ContactTangent=Vector3.right};
            fx.Clear();int count=fx.Presented;
            hp.ApplyDamage(1,Hit());yield return null;
            check(fx.Presented==count+1&&fx.ActiveCount==1&&fx.LastTier==HitPresentation.Health,"one committed rifle hit produces one layered contact silhouette");capture("r2-rifle-contact.png");
            var armor=actor.AddComponent<ArmorHealth>();armor.Configure(20);fx.Clear();
            hp.ApplyDamage(1,Hit(true));yield return null;check(fx.LastTier==HitPresentation.Armor,"armor contact has its own visual tier");capture("r2-armor-contact.png");
            fx.Clear();hp.ApplyDamage(19,Hit(true,true));yield return null;check(fx.LastTier==HitPresentation.ArmorBreak,"only actual depletion triggers break silhouette");capture("r2-armor-break.png");
            fx.Clear();for(int i=0;i<100;i++)hp.ApplyDamage(1,Hit(true));yield return null;
            check(fx.ActiveCount<=ImpactAccentVfx.Capacity,"overlapping impacts are bounded by presentation capacity");
            int frozen=fx.ActiveCount;gm.SetPaused(true);for(int i=0;i<10;i++)yield return null;check(fx.ActiveCount==frozen,"pause freezes layered impact lifetime");gm.SetPaused(false);
            fx.Clear();hp.ApplyDamage(20000,Hit(true,true));yield return null;check(fx.LastTier==HitPresentation.Kill,"lethal contact has a stronger directional kill tier");capture("r2-blade-kill.png");
            count=fx.Presented;hp.ApplyDamage(1,Hit());check(fx.Presented==count,"duplicate corpse callback cannot replay kill accent");
            UnityEngine.Object.Destroy(actor);actor=null;fx.Clear();count=fx.Presented;
            p.RestoreAt(new Vector3(0,.1f,0));bool captured=false;
            for(int i=0;i<50;i++)
            {
                p.Simulate(new PlayerCommand{Melee=i==0,HasAim=true,AimPoint=new Vector3(0,1,15)},Time.deltaTime);yield return null;
                if(!captured&&p.Melee.IsCutting&&p.Melee.AttackElapsed>.14f){capture("r2-empty-sweep.png");captured=true;}
            }
            check(captured,"empty-swing reference is an actual player action");
            check(fx.Presented==count,"empty blade sweep never creates a contact accent");
            gm.EnterHangar();yield return null;check(fx.ActiveCount==0,"hangar clears all layered feedback");
        }
        finally{if(actor!=null)UnityEngine.Object.Destroy(actor);gm.SetPaused(false);}
    }
}
