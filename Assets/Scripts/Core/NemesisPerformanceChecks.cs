using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEngine;
using Object=UnityEngine.Object;

// Opt-in, sequential, synchronized rendering benchmark. Never a display-FPS claim.
public static class NemesisPerformanceChecks
{
    [Serializable] sealed class Sample
    {public string phase;public float meanMs,p50Ms,p95Ms,maxMs;public int frames,ghosts;public long managedBytes;}
    [Serializable] sealed class Report
    {public string gpu,unity,version,method;public int width=1600,height=900;public Sample[] phases;}
    public static IEnumerator Run(GameManager gm,PlayerController p,string output,Action<bool,string> check,Action<string> capture)
    {
        gm.ExitPractice();yield return null;gm.BeginWeaponTrial();yield return null;
        Object.FindFirstObjectByType<WeaponTrial>().ClearTargets();
        p.enabled=false;p.InputRouter.readKeyboard=false;
        var cam=Camera.main;var follow=cam.GetComponent<CameraFollow>();follow.enabled=false;
        var original=cam.targetTexture;bool enabled=cam.enabled;
        var rt=new RenderTexture(1600,900,24){antiAliasing=4};rt.Create();
        var pixel=new Texture2D(1,1,TextureFormat.RGB24,false);
        var oldActive=RenderTexture.active;var ghost=p.GetComponentInChildren<NemesisAfterimage>();
        var report=new Report{gpu=SystemInfo.graphicsDeviceName,unity=Application.unityVersion,version=Application.version,
            method="Fixed 60-Hz commands; complete simulation and 1600x900 scene render with GPU synchronization each frame; hidden window, no presentation/VSync. Sequential runs, no concurrent builds/tests. 45 warmup frames, then a 240-frame window (239 inter-frame intervals) per phase."};
        var phases=new List<Sample>();
        Time.captureDeltaTime=1f/60;Application.targetFrameRate=-1;QualitySettings.vSyncCount=0;
        try
        {
            foreach(var phase in new[]{"idle","boost-no-ghost","boost-ghost","boost-support","arsenal-crossfire","fazz-missiles"})
            {
                p.RestoreAt(new Vector3(0,.1f,-4));p.stats.ResetStats();p.weaponController.ResetCooldowns();
                p.GetComponent<Damageable>().SetInvulnerable(1000);
                ghost.enabled=phase!="boost-no-ghost";
                if(phase=="arsenal-crossfire")
                {
                    string[] bodies={"E01","GM","ZAKU","DOM","GUNCANNON"};EnemyWeapon[] weapons={EnemyWeapon.M7,EnemyWeapon.M14,EnemyWeapon.Rocket,EnemyWeapon.Missiles,EnemyWeapon.BackCannon};
                    for(int i=0;i<5;i++)
                    {var enemy=gm.stageManager.enemySpawner.SpawnEnemy(EnemyKind.Ranged,new Vector3((i-2)*4,0,8));enemy.gameObject.AddComponent<EnemyArsenal>().Configure(new EnemySpawnSpec(bodies[i],weapons[i],EnemyKind.Ranged));enemy.GetComponent<Damageable>().RestoreLife(100000,100000);}
                    yield return null;p.weaponController.TryFireSkill(null);
                }
                if(phase=="fazz-missiles")
                {var boss=gm.stageManager.enemySpawner.SpawnFazz(0);boss.GetComponent<UnityEngine.AI.NavMeshAgent>().Warp(new Vector3(0,0,8));yield return null;boss.GetComponent<FazzBossController>().Begin(2);}
                if(phase=="boost-support")
                {
                    var trial=Object.FindFirstObjectByType<WeaponTrial>();
                    var target=trial.SpawnTarget(new Vector3(0,.1f,12),"",100000).GetComponent<Damageable>();
                    p.weaponController.TryFireSkill(target);
                }
                var values=new List<float>(240);long start=0,alloc=0;int maxGhost=0;
                for(int frame=0;frame<285;frame++)
                {
                    long now=Stopwatch.GetTimestamp();
                    if(frame>45)values.Add((float)((now-start)*1000.0/Stopwatch.Frequency));
                    start=now;
                    if(frame==45)alloc=GC.GetAllocatedBytesForCurrentThread();
                    bool boost=phase!="idle";
                    // Oscillate on a short clear pad; the camera follows the same
                    // offset so character screen coverage remains comparable.
                    var move=boost?new Vector2(frame%100<50?1:-1,0):Vector2.zero;
                    p.Simulate(new PlayerCommand{Move=move,BoostHeld=boost,Dash=boost&&frame==0,
                        HasAim=true,AimPoint=p.transform.position+Vector3.forward*16},Time.deltaTime);
                    yield return null;
                    cam.orthographic=true;cam.orthographicSize=CameraFollow.StandardCombatSize;
                    cam.transform.rotation=Quaternion.Euler(68,0,0);cam.transform.position=p.transform.position-cam.transform.forward*24;
                    cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;
                    pixel.ReadPixels(new Rect(800,450,1,1),0,0);RenderTexture.active=oldActive;
                    maxGhost=Mathf.Max(maxGhost,ghost.ActiveCount);
                }
                long allocated=GC.GetAllocatedBytesForCurrentThread()-alloc;
                values.Sort();var sample=new Sample{phase=phase,meanMs=values.Average(),p50Ms=values[values.Count/2],
                    p95Ms=values[(int)(values.Count*.95f)],maxMs=values[values.Count-1],frames=values.Count,ghosts=maxGhost,managedBytes=allocated};
                phases.Add(sample);report.phases=phases.ToArray();
                File.WriteAllText(Path.Combine(output,"performance.json"),JsonUtility.ToJson(report,true));
                check(values.Count>=239&&values.All(v=>v>0)&&SystemInfo.graphicsDeviceType!=UnityEngine.Rendering.GraphicsDeviceType.Null,
                    $"{phase}: measured simulation + synchronized scene frame mean={sample.meanMs:F2} ms p95={sample.p95Ms:F2} ms; ghosts={maxGhost}");
                cam.targetTexture=original;capture("performance-"+phase+".png");
                Object.FindFirstObjectByType<WeaponTrial>().ClearTargets();
                gm.stageManager.StopStage();
            }
        }
        finally
        {
            ghost.enabled=true;cam.targetTexture=original;cam.enabled=enabled;follow.enabled=true;
            RenderTexture.active=oldActive;rt.Release();Object.Destroy(rt);Object.Destroy(pixel);gm.ExitPractice();
        }
    }
}
