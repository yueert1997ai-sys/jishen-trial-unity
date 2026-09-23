using System;
using System.Collections;
using System.Linq;
using UnityEngine;

public static class M7RefinementChecks
{
    public static IEnumerator Run(GameManager gm,PlayerController p,LoadoutVisual visual,Action<bool,string> check,Action<string> capture)
    {
        check(visual.EjectionPort!=null,"authored receiver-side casing socket is equipped");
        check(visual.WeaponObject.GetComponentInChildren<MeshRenderer>().sharedMaterials.All(m=>m.shader.name=="Mech/M7 Machined Coating"),"actual equipped desert M7 uses detailed coating materials");
        var aim=new Vector3(0,1,30);int events=0;Action fired=()=>events++;p.weaponController.BeamFired+=fired;
        try{
            foreach(int fps in new[]{30,60,120})
            {
                Time.captureDeltaTime=1f/fps;p.RestoreAt(new Vector3(0,.1f,0));yield return null;
                p.weaponController.ResetCooldowns();int startEvents=events;
                var feedback=p.weaponController.GetComponent<KineticRifleFeedback>();int initial=feedback!=null?feedback.EjectedCount:0;
                for(int i=0;i<fps*3;i++)
                {
                    p.Simulate(new PlayerCommand{HasAim=true,AimPoint=aim,Fire=true},Time.deltaTime);
                    p.GetComponent<Damageable>().SetInvulnerable(1);yield return null;
                    if(fps==60&&i==fps*2)capture("m7_continuous_fire.png");
                }
                feedback=p.weaponController.GetComponent<KineticRifleFeedback>();
                check(events-startEvents>=15&&feedback.EjectedCount-initial==events-startEvents,$"{fps} FPS continuous real fire ejects one case per discharge: {events-startEvents}");
                check(feedback.Casings.particleCount>=15&&feedback.Casings.particleCount<=160,"casings persist through bursts within a fixed particle budget");
                var data=new ParticleSystem.Particle[160];int count=feedback.Casings.GetParticles(data);
                check(data.Take(count).All(c=>c.position.y>=.05f)&&data.Take(count).Any(c=>c.position.y<.07f&&c.velocity.magnitude<.3f),"spent brass settles on the arena floor without penetrating it");
                gm.SetPaused(true);yield return null;
                count=feedback.Casings.GetParticles(data);var positions=data.Take(count).Select(c=>c.position).ToArray();
                for(int i=0;i<12;i++)yield return null;
                int pausedCount=feedback.Casings.GetParticles(data);
                check(count==pausedCount&&positions.Select((v,i)=>Vector3.Distance(v,data[i].position)).All(d=>d<.0001f),"pause freezes all airborne and resting cases");
                gm.SetPaused(false);yield return null;
            }
            Time.captureDeltaTime=1f/60;
            var fx=p.weaponController.GetComponent<KineticRifleFeedback>();
            p.weaponController.ResetCooldowns();p.weaponController.TryFireBeam();
            var bullet=UnityEngine.Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None).First(b=>b.name=="M7_KineticRound");
            check(bullet.GetComponent<MeshFilter>().sharedMesh.name=="M7_Copper_Ogive"&&bullet.speed==72&&bullet.explosionRadius==0,"pointed solid copper round retains production ballistic speed and damage path");
            foreach(var b in UnityEngine.Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None))b.Despawn();
            var pooled=ProjectilePool.Spawn(false,"ReuseCheck",Vector3.zero,Color.cyan);
            check(pooled.GetComponent<MeshFilter>().sharedMesh.name!="M7_Copper_Ogive"&&pooled.GetComponent<Renderer>().sharedMaterial.shader.name=="Sprites/Default","pool reuse restores other weapons' original mesh and material");pooled.Despawn();
            // Freeze the real shot briefly for a readable close-up, using the equipped production model.
            var cam=Camera.main;var position=cam.transform.position;var rotation=cam.transform.rotation;float fov=cam.fieldOfView;bool ortho=cam.orthographic;cam.orthographic=false;
            var follow=cam.GetComponent<CameraFollow>();bool enabled=follow!=null&&follow.enabled;if(follow!=null)follow.enabled=false;
            Vector3 target=visual.WeaponObject.GetComponentInChildren<Renderer>().bounds.center;
            cam.transform.position=target+visual.WeaponObject.transform.right*3.2f+Vector3.up*.7f-visual.WeaponObject.transform.forward*.4f;cam.transform.LookAt(target);cam.fieldOfView=48;
            capture("m7_equipped_close.png");
            for(int i=0;i<90;i++){p.Simulate(new PlayerCommand{HasAim=true,AimPoint=aim,Fire=true},Time.deltaTime);yield return null;}
            capture("m7_ejection_close.png");
            cam.transform.SetPositionAndRotation(position,rotation);cam.fieldOfView=fov;cam.orthographic=ortho;if(follow!=null)follow.enabled=enabled;
            for(int i=0;i<660;i++){p.GetComponent<Damageable>().SetInvulnerable(1);yield return null;}
            check(fx.Casings.particleCount==0,"released trigger allows every case to expire without leaked particles");
            gm.EnterHangar();yield return null;check(fx.Casings.particleCount==0,"return to hangar leaves no combat debris");
        }finally{p.weaponController.BeamFired-=fired;gm.SetPaused(false);Time.captureDeltaTime=1f/60;}
    }
}
