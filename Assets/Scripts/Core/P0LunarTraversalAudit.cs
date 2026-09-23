using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.AI;

public sealed class LunarContactProbe:MonoBehaviour
{
    public string Last;
    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        Last=hit.collider.name+" point="+hit.point.ToString("F2")+" normal="+hit.normal.ToString("F2");
    }
}

public static class P0LunarTraversalAudit
{
    struct Route{public Vector3 from,to;}
    public static IEnumerator Run(PlayerController p,string output,Action<bool,string> check)
    {
        var probe=p.gameObject.AddComponent<LunarContactProbe>();
        var routes=new List<Route>();var lines=new List<string>();
        var motor=p.Motor;float step=motor.stepOffset,skin=motor.skinWidth;
        for(float x=-24;x<=24;x+=3)for(float z=-24;z<=24;z+=3)
        {
            if(!NavMesh.SamplePosition(new Vector3(x,0,z),out var a,.6f,NavMesh.AllAreas)||a.position.y>.65f)continue;
            for(int d=0;d<8;d++)
            {
                var dir=Quaternion.Euler(0,d*45,0)*Vector3.forward;
                var end=a.position+dir*4;
                if(Mathf.Abs(end.x)>26||Mathf.Abs(end.z)>26)continue;
                if(!NavMesh.SamplePosition(end,out var b,.45f,NavMesh.AllAreas)||b.position.y>.65f)continue;
                if(NavMesh.Raycast(a.position,b.position,out _,NavMesh.AllAreas))continue;
                routes.Add(new Route{from=a.position,to=b.position});
            }
        }
        lines.Add($"motor radius={motor.radius}, height={motor.height}, center={motor.center}, slope={motor.slopeLimit}, step={step}, skin={skin}; routes={routes.Count}");
        check(routes.Count>100,"lunar audit has broad clear-navmesh route coverage");
        try
        {
            for(int pass=0;pass<2;pass++)
            {
                motor.stepOffset=pass==0?.25f:.45f;motor.skinWidth=pass==0?.04f:.08f;
                int failed=0;float worst=1;
                for(int r=0;r<routes.Count;r++)
                {
                    var route=routes[r];p.RestoreAt(route.from+Vector3.up*.06f);p.stats.ResetStats();probe.Last="";
                    for(int i=0;i<4;i++)motor.Move(Vector3.down*.025f);
                    Vector3 before=p.transform.position;var direction=route.to-route.from;direction.y=0;direction.Normalize();
                    for(int frame=0;frame<60;frame++)p.Simulate(new PlayerCommand{Move=new Vector2(direction.x,direction.z)},1f/60);
                    float progress=Vector3.Dot(p.transform.position-before,direction);float ratio=progress/4f;worst=Mathf.Min(worst,ratio);
                    var planarDistance=Vector3.ProjectOnPlane(route.to-route.from,Vector3.up).magnitude;
                    if(progress<planarDistance-.22f)
                    {
                        failed++;lines.Add($"FAIL pass={pass} from={route.from.ToString("F2")} to={route.to.ToString("F2")} progress={progress:F3} end={p.transform.position.ToString("F2")} {probe.Last}");
                        if(pass==1&&progress<2)
                        {
                            foreach(var h in Physics.RaycastAll(p.transform.position+Vector3.up*8,Vector3.down,10,~0,QueryTriggerInteraction.Ignore))
                                if(h.collider!=motor)lines.Add($"SURFACE {h.collider.name} y={h.point.y:F3} normal={h.normal.ToString("F2")} triangle={h.triangleIndex}");
                        }
                    }
                    if(r%25==0)yield return null;
                }
                lines.Add($"SUMMARY pass={pass} step={motor.stepOffset} skin={motor.skinWidth} failures={failed}/{routes.Count} worstRatio={worst:F3}");
                if(pass==1)check(failed==0,$"all {routes.Count} lunar clear-path movement probes reach their target tolerance");
            }
            var fixture=GameObject.CreatePrimitive(PrimitiveType.Cube);fixture.name="Lunar_Traversal_Fixture";
            try
            {
                // A .35 m curb above the apron must be traversable without sacrificing solid cover.
                fixture.transform.position=new Vector3(0,.25f,0);fixture.transform.localScale=new Vector3(4,.5f,.8f);Physics.SyncTransforms();
                p.RestoreAt(new Vector3(0,.2f,-3));
                for(int i=0;i<60;i++)p.Simulate(new PlayerCommand{Move=Vector2.up},1f/60);
                check(p.transform.position.z>1.5f,"walk crosses a low lunar curb");
                fixture.transform.position=new Vector3(0,.675f,0);fixture.transform.localScale=new Vector3(4,1.35f,.8f);Physics.SyncTransforms();
                p.RestoreAt(new Vector3(0,.2f,-3));
                for(int i=0;i<60;i++)p.Simulate(new PlayerCommand{Move=Vector2.up},1f/60);
                check(p.transform.position.z<-.8f&&p.transform.position.y<.7f,"full cover remains solid and is not climbed");
                fixture.transform.position=new Vector3(0,1.6f,1.5f);fixture.transform.localScale=new Vector3(12,4,.3f);Physics.SyncTransforms();
                p.RestoreAt(new Vector3(0,.2f,0));p.stats.ResetStats();
                for(int i=0;i<12;i++)p.Simulate(new PlayerCommand{Move=Vector2.one,Dash=i==0},1f/60);
                check(p.transform.position.x>2.7f&&p.transform.position.z<1f,"grazing dash slides along wall without early cancellation or tunneling");
                p.RestoreAt(new Vector3(0,.2f,0));p.stats.ResetStats();
                for(int i=0;i<5;i++)p.Simulate(new PlayerCommand{Move=Vector2.up,Dash=i==0},1f/60);
                check(!p.IsDashing&&p.transform.position.z<1f,"frontal wall impact stops dash");
                float before=p.transform.position.z;
                for(int i=0;i<30;i++)p.Simulate(new PlayerCommand{Move=Vector2.down},1f/60);
                check(p.transform.position.z<before-.7f,"player can reverse away from wall contact");
            }
            finally{UnityEngine.Object.Destroy(fixture);}
        }
        finally
        {
            motor.stepOffset=step;motor.skinWidth=skin;
            File.WriteAllLines(Path.Combine(output,"lunar-traversal.txt"),lines);
            UnityEngine.Object.Destroy(probe);p.RestoreAt(new Vector3(0,.1f,-4));
        }
    }
}
