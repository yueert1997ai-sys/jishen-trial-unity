using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.AI;

public static class LunarBasinChecks
{
    public static IEnumerator Run(GameManager gm,PlayerController p,string output,Action<bool,string> check,Action<string> capture)
    {
        var records=new List<string>();Time.captureDeltaTime=1f/60;
        foreach(int sector in new[]{1,2})
        {
            gm.arenaSector.ShowSector(sector);yield return null;
            var layout=gm.arenaSector.ActiveLunarLayout;
            check(layout!=null&&layout.roomName.Contains("SELENE"),"lunar room owns active environment "+sector);
            check(!gm.arenaSector.lunar.activeInHierarchy&&!gm.arenaSector.maintenance.activeInHierarchy
                &&!gm.arenaSector.reactor.activeInHierarchy&&!gm.arenaSector.commonDeck.activeInHierarchy,"legacy geometry is not colliding or rendering "+sector);
            int visible=0,triangles=0,colliders=layout.GetComponentsInChildren<Collider>().Length;
            foreach(var renderer in layout.GetComponentsInChildren<MeshRenderer>())if(renderer.enabled)visible++;
            foreach(var filter in layout.GetComponentsInChildren<MeshFilter>())
            {
                var renderer=filter.GetComponent<MeshRenderer>();
                if(renderer==null||!renderer.enabled)continue;
                // Static batching may discard CPU vertex buffers. Index-count
                // metadata remains readable and covers this renderer's submesh.
                int first=renderer.subMeshStartIndex;
                for(int s=0;s<renderer.sharedMaterials.Length;s++)triangles+=(int)filter.sharedMesh.GetIndexCount(first+s)/3;
            }
            check(visible<24&&triangles<160000,"bounded static rendering batches and geometry "+sector+": "+visible+" / "+triangles);
            records.Add("sector="+sector+" batches="+visible+" triangles="+triangles+" physicalShapes="+colliders);
            foreach(var entry in layout.entries)
            {
                check(NavMesh.SamplePosition(entry,out var nav,.8f,NavMesh.AllAreas),"authored reinforcement entry is walkable "+sector);
                Vector3 alternate=layout.SelectEntry(entry,entry);
                check(Vector3.Distance(entry,alternate)>7,"occupied entry selects a distant readable reinforcement lane "+sector);
            }
            Vector3 crater=sector==1?new Vector3(8.5f,0,-3.8f):new Vector3(9,0,5.8f);
            check(!NavMesh.SamplePosition(crater,out _,.25f,NavMesh.AllAreas),"deep crater cannot receive path endpoints or spawns "+sector);
            check(!EnemyTactics.ClearSight(layout.coverNear,layout.coverFar),"landmark blocks real projectile-height sight "+sector);
            p.RestoreAt(layout.playerEntry);p.stats.ResetStats();
            for(int f=0;f<45;f++)yield return null;capture("lunar-"+sector+"-arrival.png");
            var cam=Camera.main;var follow=cam.GetComponent<CameraFollow>();
            Vector3 prior=cam.transform.position;Quaternion rotation=cam.transform.rotation;float size=cam.orthographicSize;
            follow.enabled=false;cam.transform.SetPositionAndRotation(new Vector3(0,63,-4),Quaternion.Euler(83,0,0));cam.orthographicSize=30;
            capture("lunar-"+sector+"-overview.png");cam.transform.SetPositionAndRotation(prior,rotation);cam.orthographicSize=size;follow.enabled=true;
            // Real controller follows navigation routes around both sides of the
            // landmarks. Path connectivity alone cannot prove body clearance.
            Vector3[] circuit={new Vector3(-15,0,-15),new Vector3(-15,0,18),new Vector3(15,0,18),new Vector3(15,0,-15),new Vector3(0,0,-12),new Vector3(0,0,18)};
            p.RestoreAt(new Vector3(0,.2f,-12));
            foreach(var destination in circuit)
            {
                var path=new NavMeshPath();
                NavMesh.SamplePosition(p.transform.position,out var start,1.2f,NavMesh.AllAreas);
                check(NavMesh.SamplePosition(destination,out var end,1,NavMesh.AllAreas)
                    &&NavMesh.CalculatePath(start.position,end.position,NavMesh.AllAreas,path)
                    &&path.status==NavMeshPathStatus.PathComplete,"lunar circuit has a complete route "+sector+" "+destination);
                foreach(var corner in path.corners)
                {
                    int frame=0;Vector3 delta=corner-p.transform.position;delta.y=0;
                    while(delta.magnitude>.4f&&frame++<600)
                    {
                        p.Simulate(new PlayerCommand{Move=new Vector2(delta.x,delta.z).normalized},Time.deltaTime);
                        yield return null;delta=corner-p.transform.position;delta.y=0;
                    }
                    check(delta.magnitude<=.4f,"player capsule clears actual lunar route "+sector+" "+corner+" remaining="+delta.magnitude.ToString("F3"));
                }
            }
            p.RestoreAt(new Vector3(0,.2f,-7));p.stats.ResetStats();float began=p.transform.position.z;
            for(int frame=0;frame<Mathf.RoundToInt(p.dashDuration*60);frame++)
            {p.Simulate(new PlayerCommand{Move=Vector2.up,Dash=frame==0},Time.deltaTime);yield return null;}
            check(Mathf.Abs(p.transform.position.z-began-p.stats.DashDistance)<.1f,"central lunar lane supports a full dash "+sector);
            foreach(var corner in new[]{new Vector3(-19,.2f,-15),new Vector3(19,.2f,-15),new Vector3(-17,.2f,19),new Vector3(17,.2f,19)})
            {
                if(!NavMesh.SamplePosition(corner,out var point,2,NavMesh.AllAreas))continue;
                p.RestoreAt(point.position+Vector3.up*.15f);for(int n=0;n<30;n++)yield return null;
                Vector3 view=cam.WorldToViewportPoint(p.transform.position+Vector3.up*1.5f);
                check(view.z>0&&view.x>.03f&&view.x<.97f&&view.y>.03f&&view.y<.97f,"player stays visible at lunar boundary "+sector+" "+corner);
            }
            p.RestoreAt(new Vector3(0,.2f,-4));
            for(int n=0;n<45;n++)yield return null;capture("lunar-"+sector+"-combat.png");
            File.WriteAllLines(Path.Combine(output,"lunar-environment.txt"),records);
        }
        gm.arenaSector.ShowSector(1);p.RestoreAt(gm.arenaSector.PlayerEntry);
    }
}
