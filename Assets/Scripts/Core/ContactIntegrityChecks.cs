using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Object=UnityEngine.Object;

// Real production projectile/beam entry points, isolated targets and opaque cover.
public static class ContactIntegrityChecks
{
    static readonly List<GameObject> fixtures=new List<GameObject>();
    static readonly Vector3 anchor=new Vector3(180,0,180);
    static Damageable Target(Vector3 offset,int team=1,float health=100)
    {
        var go=GameObject.CreatePrimitive(PrimitiveType.Cube);fixtures.Add(go);
        go.name="Contact fixture";go.transform.position=anchor+offset;go.transform.localScale=new Vector3(.8f,2,.8f);
        var d=go.AddComponent<Damageable>();d.team=team;d.destroyOnDeath=false;d.RestoreLife(health,health);
        go.AddComponent<CombatFeedback>();return d;
    }
    static GameObject Wall(Vector3 offset,Vector3 scale)
    {var go=GameObject.CreatePrimitive(PrimitiveType.Cube);fixtures.Add(go);go.name="Solid contact cover";go.transform.position=anchor+offset;go.transform.localScale=scale;return go;}
    static Transform Socket(Vector3 offset)
    {var go=new GameObject("Fixture muzzle");fixtures.Add(go);go.transform.position=anchor+offset;return go.transform;}
    static void Clear()
    {
        foreach(var shot in Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None))shot.Despawn();
        foreach(var b in Object.FindObjectsByType<MinovskyBeam>(FindObjectsSortMode.None))Object.Destroy(b.gameObject);
        foreach(var b in Object.FindObjectsByType<HalbreakerBeam>(FindObjectsSortMode.None))Object.Destroy(b.gameObject);
        foreach(var go in fixtures)if(go!=null){go.SetActive(false);Object.Destroy(go);}fixtures.Clear();
    }
    static Projectile Shot(Damageable owner,Vector3 offset,float blast=0,int pierce=0)
    {
        var shot=ProjectilePool.Spawn(false,"ContactCheck",anchor+offset,Color.cyan);
        shot.Init(owner.team,owner,Vector3.forward,20,72,1,blast,pierce);shot.SetImpact(10,CombatHitKind.Rifle);return shot;
    }
    public static IEnumerator Run(GameManager gm,PlayerController p,Action<bool,string> check,Action<string> capture)
    {
        gm.ExitPractice();yield return null;gm.BeginWeaponTrial();for(int i=0;i<20;i++)yield return null;
        Object.FindFirstObjectByType<WeaponTrial>().ClearTargets();yield return null;p.enabled=false;p.InputRouter.readKeyboard=false;
        // Keep all original level geometry and simulation active; fixtures are outside its bounds.
        foreach(int fps in new[]{30,60,120})
        {
            Time.captureDeltaTime=1f/fps;
            var owner=Target(new Vector3(0,1,0),0);var direct=Target(new Vector3(0,1,4));
            var covered=Target(new Vector3(2,1,4));var exposed=Target(new Vector3(-2,1,4));
            Wall(new Vector3(1,1,4),new Vector3(.15f,2,5));yield return null;Physics.SyncTransforms();
            int directEvents=0;direct.OnResolved+=(d,r)=>{if(r.Applied)directEvents++;};
            Shot(owner,new Vector3(0,1.1f,1),4);
            for(int i=0;i<fps/3;i++)yield return null;
            check(covered.CurrentHealth==100,"explosive projectile respects thin opaque cover "+fps);
            check(exposed.CurrentHealth<100,"explosive projectile still reaches exposed flank "+fps);
            check(direct.CurrentHealth==80&&directEvents==1,"direct explosion target is not damaged twice "+fps);
            Clear();yield return null;

            owner=Target(new Vector3(0,1,0),0);direct=Target(new Vector3(0,1,4));
            covered=Target(new Vector3(2,1,4));exposed=Target(new Vector3(-2,1,4));
            Wall(new Vector3(1,1,4),new Vector3(.15f,2,5));yield return null;Physics.SyncTransforms();
            var missile=(MissileProjectile)ProjectilePool.Spawn(true,"Contact missile",anchor+new Vector3(0,1.1f,1),Color.cyan);
            missile.Init(0,owner,Vector3.forward,20,72,1,4,0);missile.target=direct;
            for(int i=0;i<fps/3;i++)yield return null;
            check(covered.CurrentHealth==100&&exposed.CurrentHealth<100&&direct.CurrentHealth==80,
                "actual homing missile shares covered / exposed / direct blast rules "+fps);
            Clear();yield return null;

            foreach(int weapon in new[]{0,1,2,3})
            {
                owner=Target(new Vector3(0,1,0),0);var victim=Target(new Vector3(0,1,6));
                Wall(new Vector3(0,1,1.5f),new Vector3(3,2,.15f));var muzzle=Socket(new Vector3(0,3,2.8f));
                yield return null;Physics.SyncTransforms();
                if(weapon==0)Shot(owner,new Vector3(0,3,2.8f));
                if(weapon==1)ArsenalBeam.Fire(muzzle.position,Vector3.forward,owner,20,12,.4f,.3f);
                if(weapon==2)MinovskyBeam.Fire(muzzle,muzzle.position,Vector3.forward,victim.AimCenter,0,owner,20,0,0);
                if(weapon==3)HalbreakerBeam.Fire(muzzle,muzzle.position,Vector3.forward,victim.AimCenter,0,owner,20,3,0);
                for(int i=0;i<fps/2;i++)yield return null;
                check(victim.CurrentHealth==100,"muzzle beyond low wall cannot bypass cover weapon="+weapon+" fps="+fps);
                Clear();yield return null;
            }

            // Invulnerability and terminal state must not publish a success response.
            owner=Target(new Vector3(0,1,0),0);var immune=Target(new Vector3(0,1,4));immune.SetInvulnerable(5);
            var socket=Socket(new Vector3(0,1.1f,1));yield return null;Physics.SyncTransforms();
            var beam=MinovskyBeam.Fire(socket,socket.position,Vector3.forward,immune.AimCenter,0,owner,20,0,0);
            for(int i=0;i<Mathf.CeilToInt(fps*.25f);i++)yield return null;
            check(beam!=null&&beam.HasFired&&beam.DirectHitCount==0&&immune.CurrentHealth==100&&immune.GetComponent<CombatFeedback>().HitEvents==0,"invulnerable beam contact is not a confirmed hit "+fps);
            Clear();yield return null;

            // Overlapping colliders and piercing must preserve nearest-first, one-hit ownership.
            owner=Target(new Vector3(0,1,0),0);var front=Target(new Vector3(0,1,4));var rear=Target(new Vector3(0,1,6));
            front.gameObject.AddComponent<BoxCollider>();socket=Socket(new Vector3(0,3,1));yield return null;Physics.SyncTransforms();
            HalbreakerBeam.Fire(socket,socket.position,Vector3.forward,rear.AimCenter,0,owner,20,1,4);
            for(int i=0;i<fps;i++)yield return null;
            check(front.CurrentHealth==80&&rear.CurrentHealth==80,"piercing reserves direct targets before splash and ignores cosmetic muzzle height "+fps);
            check(front.GetComponent<CombatFeedback>().HitEvents==1&&rear.GetComponent<CombatFeedback>().HitEvents==1,"beam afterglow and multi-collider targets never duplicate damage "+fps);
            Clear();yield return null;

            foreach(bool piercing in new[]{false,true})
            {
                owner=Target(new Vector3(0,1,0),0);direct=Target(new Vector3(0,1,4));
                var bystander=Target(new Vector3(.9f,1,4));covered=Target(new Vector3(2.8f,1,4));
                exposed=Target(new Vector3(-2,1,4));Wall(new Vector3(1.8f,1,4),new Vector3(.06f,2,5));
                socket=Socket(new Vector3(0,1.1f,1));yield return null;Physics.SyncTransforms();
                if(piercing)HalbreakerBeam.Fire(socket,socket.position,Vector3.forward,direct.AimCenter,0,owner,20,0,4);
                else MinovskyBeam.Fire(socket,socket.position,Vector3.forward,direct.AimCenter,0,owner,20,0,4);
                for(int i=0;i<fps/2;i++)yield return null;
                check(covered.CurrentHealth==100&&bystander.CurrentHealth<100&&exposed.CurrentHealth<100,
                    "beam splash sees wall behind intervening actor, preserves exposed damage piercing="+piercing+" fps="+fps);
                Clear();yield return null;
            }

            owner=Target(new Vector3(0,1,0),0);immune=Target(new Vector3(0,1,3));immune.SetInvulnerable(5);
            yield return null;Physics.SyncTransforms();
            BeamFxKit.StarGlare(anchor,Color.white,1,.1f);
            var kit=Object.FindFirstObjectByType<BeamFxKit>();foreach(var ps in kit.GetComponentsInChildren<ParticleSystem>())ps.Clear();
            var rejected=Shot(owner,new Vector3(0,1.1f,1));rejected.GetComponent<EnergyBoltVisual>().enabled=false;
            for(int i=0;i<Mathf.CeilToInt(fps*.12f);i++)yield return null;
            int particles=0;foreach(var ps in kit.GetComponentsInChildren<ParticleSystem>())particles+=ps.particleCount;
            check(immune.CurrentHealth==100&&immune.GetComponent<CombatFeedback>().HitEvents==0&&particles==0,
                "rejected ordinary projectile creates no success flash or feedback "+fps);
            // Reuse the actual pooled projectile, with no invulnerability and a new ownership/strike history.
            immune.RestoreLife(100,100);var reuse=Shot(owner,new Vector3(0,1.1f,1));
            reuse.GetComponent<EnergyBoltVisual>().enabled=true;
            check(reuse==rejected,"contact test reuses the same pooled projectile "+fps);
            for(int i=0;i<fps/3;i++)yield return null;
            check(immune.CurrentHealth==80&&immune.GetComponent<CombatFeedback>().HitEvents==1,"pooled projectile clears old struck state "+fps);
            Clear();yield return null;

            owner=Target(new Vector3(0,1,0),0);var armored=Target(new Vector3(0,1,3));
            armored.gameObject.AddComponent<ArmorHealth>().Configure(30);yield return null;Physics.SyncTransforms();
            var feedback=armored.GetComponent<CombatFeedback>();
            for(int shotIndex=0;shotIndex<3;shotIndex++)
            {
                Shot(owner,new Vector3(0,1.1f,1));for(int i=0;i<fps/5;i++)yield return null;
                check(feedback.LastPresentation==(shotIndex==0?HitPresentation.Armor:shotIndex==1?HitPresentation.ArmorBreak:HitPresentation.Health),
                    "committed projectile result selects armor / break / health stage="+shotIndex+" fps="+fps);
                check(armored.CurrentHealth==(shotIndex<2?100:80),"breaking contact preserves no health overflow stage="+shotIndex+" fps="+fps);
            }
            armored.SetCurrentHealth(10);int deaths=0;armored.OnDied+=d=>deaths++;
            Shot(owner,new Vector3(0,1.1f,1));for(int i=0;i<fps/5;i++)yield return null;
            check(deaths==1&&armored.IsDead&&feedback.LastPresentation==HitPresentation.Kill,"lethal contact publishes exactly one kill "+fps);
            Clear();yield return null;

            // A committed charge must still respect pause and exit cancellation after query unification.
            owner=Target(new Vector3(0,1,0),0);front=Target(new Vector3(0,1,4));socket=Socket(new Vector3(0,1.1f,1));
            yield return null;Physics.SyncTransforms();
            var pending=HalbreakerBeam.Fire(socket,socket.position,Vector3.forward,front.AimCenter,0,owner,20,0,0);
            gm.SetPaused(true);for(int i=0;i<fps/4;i++)yield return null;
            check(!pending.HasFired&&front.CurrentHealth==100,"pause prevents pending beam discharge "+fps);
            gm.SetPaused(false);gm.EndWeaponTrial();for(int i=0;i<3;i++)yield return null;
            check(pending==null&&front.CurrentHealth==100,"leaving combat cancels pending damage "+fps);
            Clear();yield return null;gm.BeginWeaponTrial();for(int i=0;i<10;i++)yield return null;
            Object.FindFirstObjectByType<WeaponTrial>().ClearTargets();yield return null;
        }
        Time.captureDeltaTime=1f/60;gm.EndWeaponTrial();yield return null;
    }
}
