using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public static class OverdriveChecks
{
    public static IEnumerator Run(GameManager gm,PlayerController p,Action<bool,string> check,Action<string> capture)
    {
        var armorChecks=ArmorBreakChecks.Run(gm,p,check,capture);while(armorChecks.MoveNext())yield return armorChecks.Current;
        Time.captureDeltaTime=1f/60;
        foreach(var role in new[]{EnemyKind.Melee,EnemyKind.Ranged,EnemyKind.Elite})
        {
            var spawned=gm.stageManager.enemySpawner.SpawnEnemy(role,new Vector3(12,.1f,10));spawned.enabled=false;
            check(spawned.kind==role&&(role==EnemyKind.Elite?spawned.Armor?.Current==CombatRules.Current.EliteCapacity:spawned.Armor==null||spawned.Armor.Current==0),"shared prefab preserves production role and finite armor: "+role);
            check(spawned.GetComponent<WorldHealthBar>()!=null,"production enemy has the correct health/armor display: "+role);
            int deaths=OverdriveVfx.DeathBursts;
            spawned.GetComponent<Damageable>().Kill(new DamageInfo(p.gameObject,p.transform.position,p.GetComponent<Damageable>(),9999));
            check(OverdriveVfx.DeathBursts==deaths+1,"one real death emits one layered effect");
            UnityEngine.Object.Destroy(spawned.gameObject);yield return null;
        }
        p.RestoreAt(new Vector3(0,.1f,0));p.stats.ResetStats();
        int dashes=OverdriveVfx.DashBursts;
        p.Simulate(new PlayerCommand { Dash=true,Move=Vector2.up,HasAim=true,AimPoint=new Vector3(0,0,15) },Time.deltaTime);
        for(int i=0;i<5;i++){p.Simulate(new PlayerCommand{Move=Vector2.up},Time.deltaTime);yield return null;}
        check(OverdriveVfx.DashBursts==dashes+1,"one accepted dash has one launch burst");
        capture("overdrive_dash.png");
        check(Camera.main.GetComponent<CombatBloom>()==null,"clear presentation has no fullscreen bloom component");
        for(int i=0;i<32;i++)OverdriveVfx.Death(p.transform.position+Vector3.forward*5);
        yield return null;
        check(OverdriveVfx.LiveParticles<=OverdriveVfx.ParticleCapacity,"simultaneous explosion storm stays within particle capacity");
        for(int i=0;i<120;i++){p.Simulate(new PlayerCommand(),Time.deltaTime);yield return null;}
        check(OverdriveVfx.LiveParticles==0,"pooled effects expire without residual particles");
        // Synchronized offscreen render cost; do not interpret this as display FPS or full-game frame time.
        var camera=Camera.main;var oldTarget=camera.targetTexture;var active=RenderTexture.active;
        var rt=new RenderTexture(1600,900,24);rt.Create();
        var pixel=new Texture2D(1,1,TextureFormat.RGB24,false);var samples=new float[80];
        var watch=new System.Diagnostics.Stopwatch();
        try
        {
            camera.targetTexture=rt;
            for(int frame=0;frame<samples.Length;frame++)
            {
                if(frame%12==0)for(int j=0;j<7;j++)OverdriveVfx.Death(new Vector3(Mathf.Sin(j)*4,1,5+Mathf.Cos(j)*3));
                yield return null;
                watch.Restart();camera.Render();RenderTexture.active=rt;
                pixel.ReadPixels(new Rect(800,450,1,1),0,0);watch.Stop();samples[frame]=(float)watch.Elapsed.TotalMilliseconds;
                RenderTexture.active=active;
            }
        }
        finally {camera.targetTexture=oldTarget;RenderTexture.active=active;rt.Release();UnityEngine.Object.Destroy(rt);UnityEngine.Object.Destroy(pixel);}
        Array.Sort(samples);float mean=0;foreach(float sample in samples)mean+=sample/samples.Length;
        check(true,$"1600x900 seven-explosion offscreen render+GPU readback: mean={mean:F2}ms p95={samples[76]:F2}ms; not presentation FPS");
        OverdriveVfx.Clear();
        OverdriveVfx.ToggleIntensity();check(!OverdriveVfx.Intense,"F2 intensity can be reduced");OverdriveVfx.ToggleIntensity();
        p.RestoreAt(new Vector3(0,.1f,0));
    }
}
