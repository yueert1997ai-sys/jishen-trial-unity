using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class P0BallisticsChecks
{
    public static IEnumerator Run(GameManager gm,PlayerController p,LoadoutVisual visual,Action<bool,string> check,Action<string> capture)
    {
        var played=new List<GameAudioCue>();Action<GameAudioCue> listener=cue=>played.Add(cue);GameAudio.CuePlayed+=listener;
        try
        {
            // Each test drives the normal input/pose/fire path; no shot directions are patched by the test.
            foreach(bool boost in new[]{false,true})foreach(float height in new[]{-4f,1.1f,7f})
            {
                p.RestoreAt(new Vector3(0,.1f,0));p.stats.ResetStats();
                var go=GameObject.CreatePrimitive(PrimitiveType.Capsule);go.name="P0_AltitudeProbe";go.transform.position=new Vector3(0,height,9);
                var hp=go.AddComponent<Damageable>();hp.team=1;hp.RestoreLife(100,100);hp.destroyOnDeath=false;
                Vector3 target=new Vector3(0,height,9);
                for(int i=0;i<25;i++){p.Simulate(new PlayerCommand{HasAim=true,AimPoint=target,BoostHeld=boost},Time.deltaTime);yield return null;}
                check(Mathf.Abs(visual.WeaponMuzzle.forward.y)<.001f,$"barrel has no elevation: boost={boost}, targetHeight={height}");
                p.weaponController.ResetCooldowns();p.weaponController.TryFireBeam();
                string shotName=p.GetComponentInChildren<NemesisMotionRig>()!=null?"J01_BeamRifle":"M7_KineticRound";
                var bullet=UnityEngine.Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None).Single(b=>b.name==shotName);
                check(bullet.Planar&&Mathf.Abs(bullet.direction.y)<.00001f&&bullet.speed==72&&bullet.explosionRadius==0,"M7 is a fast physical round on the combat plane");
                var renderer=bullet.GetComponent<Renderer>();var trail=bullet.GetComponent<TrailRenderer>();
                var energy=bullet.GetComponent<EnergyBoltVisual>();
                check(energy!=null&&energy.Active&&!renderer.enabled&&!trail.enabled&&energy.GetComponentsInChildren<LineRenderer>().All(l=>l.enabled&&l.sharedMaterial.shader.isSupported),"M7 uses layered energy presentation over unchanged ballistic simulation");
                check(Vector3.Dot(visual.WeaponMuzzle.forward,bullet.direction)>.998f,"barrel and actual shot direction agree");
                float launchHeight=bullet.transform.position.y;
                for(int i=0;i<20;i++)
                {
                    p.Simulate(new PlayerCommand{HasAim=true,AimPoint=target,BoostHeld=boost},Time.deltaTime);yield return null;
                    if(bullet.gameObject.activeSelf)check(Mathf.Abs(bullet.transform.position.y-launchHeight)<.00001f,"round never climbs or falls");
                }
                check(hp.CurrentHealth==82,$"one shot hits footprint regardless of target height: boost={boost}, height={height}, hp={hp.CurrentHealth}");
                UnityEngine.Object.Destroy(go);yield return null;
            }
            check(played.Contains(GameAudioCue.RifleShot)&&!played.Contains(GameAudioCue.Beam),"M7 emits ballistic gunshot cue and no laser cue");
            p.RestoreAt(new Vector3(0,.1f,0));
            Vector3 near=new Vector3(0,100,.1f);
            for(int i=0;i<18;i++){p.Simulate(new PlayerCommand{HasAim=true,AimPoint=near},Time.deltaTime);yield return null;}
            check(Vector3.Dot(visual.WeaponMuzzle.forward,p.AimDirection)>.99f,"cursor inside barrel reach cannot flip gun backward");
            var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.name="P0_BarrelBlocker";
            var midpoint=(PlanarCombat.Point(p.transform.position)+PlanarCombat.Point(visual.WeaponMuzzle.position))*.5f;
            wall.transform.position=midpoint;wall.transform.localScale=new Vector3(8,4,.12f);
            var behind=GameObject.CreatePrimitive(PrimitiveType.Capsule);behind.transform.position=new Vector3(0,1,8);
            var blockedHp=behind.AddComponent<Damageable>();blockedHp.RestoreLife(100,100);blockedHp.team=1;
            yield return null;p.weaponController.ResetCooldowns();p.weaponController.TryFireBeam();
            for(int i=0;i<15;i++)yield return null;
            check(blockedHp.CurrentHealth==100,"cover between body and protruding barrel blocks a shot");
            UnityEngine.Object.Destroy(wall);UnityEngine.Object.Destroy(behind);yield return null;
            // Fire/slide rendering evidence uses the real camera, without opening a window.
            p.RestoreAt(new Vector3(0,.1f,0));
            for(int i=0;i<20;i++){p.Simulate(new PlayerCommand{HasAim=true,AimPoint=new Vector3(0,1.1f,12),Fire=true,Move=Vector2.right},Time.deltaTime);yield return null;}
            capture("m7_kinetic_fire.png");
            foreach(var b in UnityEngine.Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None))b.Despawn();
            p.RestoreAt(new Vector3(0,.1f,0));played.Clear();
            // Empty combo: only three whooshes and the heavy load; no phantom regrip or hit sounds.
            for(int i=0;i<110;i++)
            {
                bool next=i==0||(p.Melee.IsAttacking&&p.Melee.ComboStage<2&&p.Melee.AttackElapsed>.07f);
                p.Simulate(new PlayerCommand{Melee=next,HasAim=true,AimPoint=new Vector3(0,1.1f,12)},Time.deltaTime);yield return null;
            }
            check(played.Count(c=>c==GameAudioCue.SwordCut1)==1&&played.Count(c=>c==GameAudioCue.SwordCut2)==1&&played.Count(c=>c==GameAudioCue.SwordCut3)==1,"one correctly timed swing sound per stroke");
            check(played.Count(c=>c==GameAudioCue.SwordWindup)==1&&!played.Contains(GameAudioCue.SwordRegrip)&&!played.Contains(GameAudioCue.SwordHit)&&!played.Contains(GameAudioCue.SwordHitHeavy),"empty combo has one heavy preparation and no fake contact/regrip sounds");
            foreach(var name in new[]{"saber_swing_1","saber_swing_2","saber_heavy","saber_load","saber_hit","saber_hit_heavy","m7_attack","m7_tail"})
            {
                var clip=Resources.Load<AudioClip>("Audio/Combat/"+(name=="saber_load"||name=="m7_tail"?"R9/":"ImpactR2/")+name);
                check(clip!=null&&clip.loadState==AudioDataLoadState.Loaded,$"material ready: {name}, {clip?.length:F3}s");
            }
            var music=GameAudio.Instance.GetComponents<AudioSource>().Where(s=>s.clip!=null&&s.clip.name=="P0_HeavyBattle").ToArray();
            var activeSources=GameAudio.Instance.GetComponents<AudioSource>();
            foreach(var cut in new[]{"saber_swing_1","saber_swing_2","saber_heavy"})
                check(activeSources.Any(s=>s.clip==Resources.Load<AudioClip>("Audio/Combat/ImpactR2/"+cut)),"actual sword voice uses authored swing material: "+cut);
            check(activeSources.Count(s=>s.clip!=null&&s.clip.name.StartsWith("m7_attack")&&s.pitch==1f&&!s.loop)==2,"rifle has dedicated original-pitch non-looping shot voices");
            check(music.Length==2&&music.All(s=>s.loop&&s.clip.length>80),"new battle score loaded as an intact musical loop");
            check(!GameAudio.Instance.GetComponents<AudioSource>().Any(s=>s.clip!=null&&s.clip.name=="Subspace_Loop"),"rejected BGM is absent from active P0 mix");
        }
        finally {GameAudio.CuePlayed-=listener;}
    }
}
