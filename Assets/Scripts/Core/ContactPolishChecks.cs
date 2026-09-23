using System;
using System.Collections;
using System.Linq;
using UnityEngine;

public static class ContactPolishChecks
{
    static PlayerCommand Aim(GameObject target,bool fire=false,bool cut=false)=>new PlayerCommand{Fire=fire,Melee=cut,HasAim=true,AimPoint=target.transform.position};
    public static IEnumerator Run(GameManager gm,PlayerController p,Action<bool,string> check,Action<string> capture)
    {
        var fx=ArmorContactVfx.Get();GameObject go=null;
        try
        {
            foreach(int fps in new[]{30,60,120})
            {
                Time.captureDeltaTime=1f/fps;p.RestoreAt(new Vector3(0,.1f,0));fx.Clear();yield return null;
                go=UnityEngine.Object.Instantiate(gm.stageManager.enemySpawner.meleePrefab,new Vector3(0,.1f,4),Quaternion.identity);
                var enemy=go.GetComponent<EnemyBase>();enemy.ConfigureP0Role(EnemyKind.Melee);enemy.TrainingTarget=true;enemy.Init(p.transform,null);enemy.enabled=false;
                var hp=go.GetComponent<Damageable>();hp.destroyOnDeath=false;hp.RestoreLife(1000,1000);var motion=go.GetComponent<E01SoldierMotion>();motion.TrainingTarget=true;
                yield return null;int contacts=fx.BulletContacts,oldBursts=OverdriveVfx.HitBursts;
                p.Simulate(Aim(go,true),Time.deltaTime);
                var round=UnityEngine.Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None).Single(b=>b.Kinetic);
                check(Vector3.Dot(round.transform.forward,round.direction)>.999f,"physical round is aligned from launch frame at "+fps);
                check(round.GetComponent<TrailRenderer>().time<.01f&&round.explosionRadius==0,"kinetic tracer is short and carries no blast damage");
                var rifle=p.GetComponent<KineticRifleFeedback>();
                check(rifle.MuzzleFlash.GetComponent<ParticleSystemRenderer>().renderMode==ParticleSystemRenderMode.Mesh,"muzzle uses bore-aligned flame geometry");
                if(fps==60)capture("r8_muzzle_far.png");
                for(int i=0;i<fps/3;i++)yield return null;
                check(hp.CurrentHealth==982&&fx.BulletContacts==contacts+1,"one real round produces one armor contact at "+fps);
                check(OverdriveVfx.HitBursts==oldBursts,"ordinary ballistic hit no longer layers a generic overdrive burst");
                check(hp.LastHit.HasContact&&hp.LastHit.KineticRound&&Vector3.Dot(hp.LastHit.ContactNormal,round.direction)<-.99f,"hit carries ballistic contact point and incoming surface normal");
                check(Vector3.Distance(hp.LastHit.ContactPoint,go.GetComponent<Collider>().ClosestPoint(hp.LastHit.ContactPoint))<.25f,"contact is on the struck actor footprint");
                p.RestoreAt(new Vector3(0,.1f,1.3f));hp.RestoreLife(1000,1000);yield return null;
                int blades=fx.BladeContacts;p.Simulate(Aim(go,false,true),Time.deltaTime);
                for(int i=0;i<fps/2&&fx.BladeContacts==blades;i++){p.Simulate(Aim(go),Time.deltaTime);yield return null;}
                check(fx.BladeContacts==blades+1&&hp.LastHit.MeleeStrike,"one swept blade contact owns one visual event at "+fps);
                check(hp.LastHit.ContactTangent.magnitude>.99f&&Vector3.Dot(motion.HitDirection,hp.LastHit.ContactTangent)>.99f,"enemy reaction follows actual cut travel");
                yield return null;check(fx.ActiveMarks>0,"contact creates a short armor-attached scar");
                if(fps==60)
                {
                    capture("r8_blade_contact_far.png");
                    var cam=Camera.main;float size=cam.orthographicSize;cam.orthographicSize=4.8f;capture("r8_blade_contact_detail.png");cam.orthographicSize=size;
                }
                gm.SetPaused(true);int marks=fx.ActiveMarks;
                for(int i=0;i<5;i++)yield return null;
                check(fx.ActiveMarks==marks,"pause freezes contact marks");gm.SetPaused(false);yield return null;
                p.Melee.CancelAttack();p.RestoreAt(new Vector3(0,.1f,1.3f));hp.RestoreLife(70,70);yield return null;
                int released=fx.DetachedPieces;p.Simulate(Aim(go,false,true),Time.deltaTime);
                for(int i=0;i<fps&&!hp.IsDead;i++){p.Simulate(Aim(go),Time.deltaTime);yield return null;}
                check(hp.IsDead&&fx.DetachedPieces==released+2,"real lethal slash releases source shoulder and backpack geometry");
                for(int i=0;i<4;i++)yield return null;
                check(fx.ActivePieces==2&&fx.ActivePieces<=ArmorContactVfx.PieceCapacity,"armor fragments are simulated in a bounded pool");
                if(fps==60)capture("r8_armor_release.png");
                UnityEngine.Object.Destroy(go);go=null;
                for(int i=0;i<fps*2;i++)yield return null;
                check(fx.ActivePieces==0&&fx.ActiveMarks==0,"all contact remnants expire after actor disposal");
            }
            Time.captureDeltaTime=1f/60;p.RestoreAt(new Vector3(0,.1f,0));yield return null;
            var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.transform.position=new Vector3(0,1.5f,4);wall.transform.localScale=new Vector3(5,3,.3f);go=wall;yield return null;
            int hits=fx.WallContacts;p.Simulate(Aim(go,true),Time.deltaTime);for(int i=0;i<20;i++)yield return null;
            check(fx.WallContacts==hits+1,"wall contact receives its own physical dust and spark event");
            int created=fx.GetComponentsInChildren<LineRenderer>(true).Length;for(int i=0;i<100;i++)fx.Wall(Vector3.zero,Vector3.forward);
            check(fx.GetComponentsInChildren<LineRenderer>(true).Length==created&&created==ArmorContactVfx.MarkCapacity,"effects retain fixed mark capacity under a burst");
            gm.EnterHangar();yield return null;
            check(fx.ActiveMarks==0&&fx.ActivePieces==0,"return to hangar clears contact presentation");
        }
        finally{if(go!=null)UnityEngine.Object.Destroy(go);Time.captureDeltaTime=1f/60;gm.SetPaused(false);}
    }
}
