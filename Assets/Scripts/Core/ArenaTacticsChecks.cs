using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.AI;
using Object=UnityEngine.Object;

public static class ArenaTacticsChecks
{
    static void Clear(EnemyBase enemy)
    {
        if(enemy!=null)Object.Destroy(enemy.gameObject);
        foreach(var shot in Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None))shot.Despawn();
    }
    public static IEnumerator Run(GameManager gm,PlayerController p,string output,Action<bool,string> check,Action<string> capture)
    {
        var metrics=new List<string>(); EnemyBase enemy=null,second=null;
        try
        {
            foreach(int sector in new[]{1,2})
            {
                gm.arenaSector.ShowSector(sector);yield return null;
                check((sector==1?gm.arenaSector.tacticalMaintenance:gm.arenaSector.tacticalReactor).activeSelf,"new tactical sector active "+sector);
                check(!gm.arenaSector.lunar.activeSelf&&!gm.arenaSector.commonDeck.activeSelf,"legacy floor and colliders disabled "+sector);
                var path=new NavMeshPath();int routes=0;
                var points=new[]{new Vector3(0,0,-12),new Vector3(0,0,18),new Vector3(-15,0,-15),new Vector3(15,0,-15),new Vector3(-15,0,18),new Vector3(15,0,18),new Vector3(-11,0,5),new Vector3(11,0,5)};
                foreach(var a in points)foreach(var b in points)
                {
                    if(a==b)continue;
                    check(NavMesh.SamplePosition(a,out var from,1.2f,NavMesh.AllAreas)&&NavMesh.SamplePosition(b,out _,1.2f,NavMesh.AllAreas),"entry and flank route lie on navigation "+sector);
                    NavMesh.SamplePosition(b,out var to,1.2f,NavMesh.AllAreas);
                    check(NavMesh.CalculatePath(from.position,to.position,NavMesh.AllAreas,path)&&path.status==NavMeshPathStatus.PathComplete,"entry-to-flank connected "+sector);routes++;
                }
                metrics.Add("sector="+sector+" connectedRoutes="+routes);
                p.RestoreAt(new Vector3(0,.1f,-4));for(int n=0;n<30;n++)yield return null;
                capture("arena-"+sector+"-combat.png");
                var cam=Camera.main;Vector3 position=cam.transform.position;Quaternion rotation=cam.transform.rotation;float size=cam.orthographicSize;
                cam.transform.SetPositionAndRotation(new Vector3(0,45,0),Quaternion.Euler(90,0,0));cam.orthographicSize=29;
                capture("arena-"+sector+"-overview.png");cam.transform.SetPositionAndRotation(position,rotation);cam.orthographicSize=size;
            }
            gm.arenaSector.ShowSector(1);yield return null;
            foreach(int fps in new[]{30,60,120})
            {
                Time.captureDeltaTime=1f/fps;p.RestoreAt(new Vector3(-3,.1f,0));p.stats.ResetStats();
                p.GetComponent<Damageable>().SetInvulnerable(120);
                enemy=gm.stageManager.enemySpawner.SpawnEnemy(EnemyKind.Ranged,new Vector3(0,0,9));enemy.moveSpeed=0;enemy.fireInterval=100;
                for(int n=0;n<fps;n++)
                {p.Simulate(new PlayerCommand{Move=Vector2.right},1f/fps);yield return null;}
                float measured=enemy.ObservedTargetVelocity.x;
                check(measured>5&&measured<=8.01f,"actual moving player is sampled at "+fps+" FPS: "+measured.ToString("F2"));
                metrics.Add("fps="+fps+" observedVelocity="+measured.ToString("F3"));
                Clear(enemy);enemy=null;yield return null;

                p.RestoreAt(new Vector3(0,.1f,0));
                enemy=gm.stageManager.enemySpawner.SpawnEnemy(EnemyKind.Melee,new Vector3(0,0,3.7f));
                for(int n=0;n<fps*3&&enemy.LungesCompleted==0;n++)yield return null;
                check(enemy.LungesCompleted==1,"melee completes real telegraphed lunge at "+fps);
                check(!EnemyTactics.HasAttackSlot(enemy)&&EnemyTactics.AttackersActive==0,"completed lunge releases attack budget at "+fps);
                Clear(enemy);enemy=null;yield return null;
            }
            Time.captureDeltaTime=1f/60;
            p.RestoreAt(new Vector3(0,.1f,0));
            enemy=gm.stageManager.enemySpawner.SpawnEnemy(EnemyKind.Ranged,new Vector3(-4,0,6));enemy.target=null;
            second=gm.stageManager.enemySpawner.SpawnEnemy(EnemyKind.Ranged,new Vector3(4,0,6));second.target=null;
            Vector3 a1=EnemyTactics.Anchor(enemy,Vector3.zero,enemy.transform.position);
            Vector3 a2=EnemyTactics.Anchor(enemy,Vector3.zero,new Vector3(2,0,4));
            check(Vector3.Distance(a1,a2)<.001f,"assigned flank stays fixed as enemy approaches it");
            Vector3 survivorAnchor=EnemyTactics.Anchor(second,Vector3.zero,second.transform.position);
            check(Vector3.Distance(a1,survivorAnchor)>3,"teammates occupy distinct firing bearings");
            Clear(enemy);enemy=null;yield return null;
            enemy=gm.stageManager.enemySpawner.SpawnEnemy(EnemyKind.Ranged,new Vector3(4,0,6));enemy.target=null;
            Vector3 replacement=EnemyTactics.Anchor(enemy,Vector3.zero,enemy.transform.position);
            check(Vector3.Distance(replacement,survivorAnchor)>3,"reinforcement does not duplicate survivor's flank after a death");
            Clear(enemy);Clear(second);enemy=second=null;yield return null;

            // Real authored obstacle, baked path and live AI: it must first leave
            // cover and find a sight line rather than spending volleys on a wall.
            p.RestoreAt(new Vector3(-4.5f,.1f,2.5f));p.GetComponent<Damageable>().SetInvulnerable(120);
            enemy=gm.stageManager.enemySpawner.SpawnEnemy(EnemyKind.Ranged,new Vector3(-12,0,2.5f));
            Vector3 start=enemy.transform.position;
            check(!EnemyTactics.ClearSight(start,p.transform.position),"authored relay blocks sight at projectile height");
            for(int n=0;n<12;n++)yield return null;
            check(enemy.ShotsCommitted==0&&!enemy.HasAttackWarning,"no windup or fire through relay cover");
            gm.SetPaused(true);Vector3 paused=enemy.transform.position;
            for(int n=0;n<12;n++)yield return null;
            check(Vector3.Distance(paused,enemy.transform.position)<.01f,"pause freezes tactical movement");gm.SetPaused(false);
            int peak=0;
            for(int n=0;n<60*12&&enemy.ShotsCommitted==0;n++)
            {peak=Mathf.Max(peak,EnemyTactics.AttackersActive);yield return null;}
            check(enemy.ShotsCommitted>0&&Vector3.Distance(start,enemy.transform.position)>2,"live rifleman navigates around cover and resumes fire");
            check(EnemyTactics.ClearSight(enemy.transform.position,p.transform.position),"recovered firing position has real sight");
            metrics.Add("coverReposition="+Vector3.Distance(start,enemy.transform.position).ToString("F3")+" shots="+enemy.ShotsCommitted);
            capture("arena-ai-flank.png");
            Clear(enemy);enemy=null;yield return null;
            gm.ExitPractice();yield return null;
            check(EnemyTactics.AttackersActive==0&&EnemyTactics.RingPopulation==0,"return to hangar clears tactical reservations");
        }
        finally
        {
            if(gm.IsPaused)gm.SetPaused(false);Clear(enemy);Clear(second);
            File.WriteAllLines(Path.Combine(output,"arena-tactics.txt"),metrics);
        }
    }
}
