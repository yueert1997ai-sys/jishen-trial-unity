using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

// Evidence only: drives the same public command path as the player. No protection or forced kills.
public static class CombatSliceReplay
{
    public static IEnumerator Run(GameManager gm,PlayerController p,string output,Action<bool,string> check)
    {
        bool rhythm=Array.IndexOf(Environment.GetCommandLineArgs(),"-rhythmReplay")>=0;
        int firstView=Array.IndexOf(Environment.GetCommandLineArgs(),"-sliceReplayC")>=0?2:0;
        CombatSliceSettings.SelectView(firstView==2?2:1);
        gm.EnterHangar();yield return null;
        var demo=new GameObject("SliceReplay").AddComponent<P0CombatDemo>();
        for(int i=0;i<5;i++)yield return null;
        p.enabled=false;p.InputRouter.readKeyboard=false;
        Time.captureDeltaTime=1f/60;
        var cam=Camera.main;
        // Start offline mixer capture BEFORE restoring listener volume: no speaker playback.
        var audio=new PlayerAudioCapture();AudioListener.volume=1;AudioListener.pause=false;
        GamePreferences.SetEffects(1);GamePreferences.SetMusic(.6f);
        var rows=new List<string>{"frame,time,x,z,aimX,aimZ,fire,slash,dash,hp,energy,alive,kills,viewSize,boost"};
        int filmed=0;int peak=0;var visible=new int[3];var enemies=new int[3];
        var previousTarget=cam.targetTexture;var previousActive=RenderTexture.active;
        var rt=new RenderTexture(960,540,24){antiAliasing=2};rt.Create();
        var pixels=new Texture2D(960,540,TextureFormat.RGB24,false);
        for(int view=firstView;view<3;view++)Directory.CreateDirectory(Path.Combine(output,"view-"+view));
        try
        {
            for(int frame=0;frame<5400;frame++)
            {
                if(!gm.IsCombatActive||demo.Finished)break;
                if(gm.stageManager.EnemiesAlive==0&&gm.equipmentLoop.Absorption.Offering)
                    gm.equipmentLoop.Absorption.CollectWithoutInstalling();
                float t=frame/60f;
                Damageable nearest=null;float distance=float.MaxValue;
                foreach(var actor in Damageable.Active)
                {
                    if(actor==null||actor.team==0||actor.IsDead)continue;
                    float d=(actor.transform.position-p.transform.position).sqrMagnitude;
                    if(d<distance){distance=d;nearest=actor;}
                }
                Vector3 aim=nearest!=null?nearest.AimCenter:new Vector3(0,1,10);
                Vector3 waypoint=new Vector3(Mathf.Sin(t*.31f)*6,0,Mathf.Cos(t*.31f)*5);
                Vector3 delta=waypoint-p.transform.position;delta.y=0;
                bool slash=nearest!=null&&distance<4.3f*4.3f&&frame%22==0;
                if(rhythm && nearest!=null && distance<4.3f*4.3f && p.Melee.IsAttacking && p.Melee.ComboStage<2)
                    slash=p.Melee.AttackElapsed>.07f&&!p.Melee.NextQueued;
                var command=new PlayerCommand{Move=new Vector2(delta.x,delta.z).normalized,HasAim=true,AimPoint=aim,Fire=nearest!=null,Melee=slash,Dash=frame%110==0,BoostHeld=rhythm&&t%7f<.8f&&p.stats.CurrentEnergy>30};
                p.Simulate(command,Time.deltaTime);
                yield return null;
                peak=Mathf.Max(peak,gm.stageManager.EnemiesAlive);
                audio.Advance(frame%2==1);
                if(frame%2==0)continue;
                rows.Add(string.Format(CultureInfo.InvariantCulture,"{0},{1:F4},{2:F4},{3:F4},{4:F4},{5:F4},{6},{7},{8},{9:F2},{10:F2},{11},{12},{13},{14}",frame,demo.Elapsed,p.transform.position.x,p.transform.position.z,aim.x,aim.z,command.Fire,command.Melee,command.Dash,p.stats.CurrentHp,p.stats.CurrentEnergy,gm.stageManager.EnemiesAlive,gm.Kills,cam.orthographicSize,command.BoostHeld));
                float size=cam.orthographicSize;
                // All three views render the identical simulation frame and focus, so only coverage changes.
                for(int view=firstView;view<3;view++)
                {
                    cam.orthographicSize=firstView==2?size:view==0?9:view==1?10.35f:11.25f;
                    cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;
                    pixels.ReadPixels(new Rect(0,0,960,540),0,0);pixels.Apply();
                    File.WriteAllBytes(Path.Combine(output,"view-"+view,"frame_"+filmed.ToString("D4")+".png"),pixels.EncodeToPNG());
                    foreach(var actor in Damageable.Active)
                    {
                        if(actor==null||actor.team==0||actor.IsDead)continue;
                        enemies[view]++;var point=cam.WorldToViewportPoint(actor.AimCenter);
                        if(point.z>0&&point.x>=0&&point.x<=1&&point.y>=0&&point.y<=1)visible[view]++;
                    }
                }
                cam.orthographicSize=size;cam.targetTexture=previousTarget;RenderTexture.active=previousActive;filmed++;
            }
        }
        finally
        {
            audio.Save(Path.Combine(output,"game-mix.wav"));AudioListener.volume=0;
            cam.targetTexture=previousTarget;RenderTexture.active=previousActive;
            rt.Release();UnityEngine.Object.Destroy(rt);UnityEngine.Object.Destroy(pixels);
            File.WriteAllLines(Path.Combine(output,"input-trace.csv"),rows);
        }
        File.WriteAllText(Path.Combine(output,"replay-result.txt"),string.Format(CultureInfo.InvariantCulture,"Frames={0}\nVideoSeconds={1:F3}\nAudioSeconds={2:F3}\nAudioPeak={3:F4}\nKills={4}\nHP={5:F2}\nPeakAlive={6}\nWon={7}\nVisibleSamples A/B/C={8} / {9} / {10} of {11}\nNo invulnerability, forced kills, buffs, or slow motion. Scripted inputs at fixed 60Hz; renders at 30fps, three views of each same frame. Captures exclude overlay HUD. Not a human playtest or presented-FPS benchmark.\n",filmed,filmed/30f,audio.Duration,audio.Peak,gm.Kills,p.stats.CurrentHp,peak,demo.SliceWon,visible[0],visible[1],visible[2],enemies[firstView]));
        File.AppendAllText(Path.Combine(output,"replay-result.txt"),"Capture firstView="+firstView+"; rhythm inputs="+rhythm+"; C-only capture has no A/B samples.\n");
        check(filmed>0&&audio.SampleCount>0,"actual Player frames and mixer samples recorded");
        check(peak<=7&&gm.upgradeSystem.Count==0,"natural-input replay stays within enemy and zero-upgrade limits");
    }
}
