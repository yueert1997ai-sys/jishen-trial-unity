using System;
using System.Collections;
using System.IO;
using UnityEngine;

// Staged stationary production enemies for inspecting contacts, alongside the separate natural-input battle replay.
public static class ContactPolishReplay
{
    public static IEnumerator Run(GameManager gm,PlayerController p,string output,Action<bool,string> check)
    {
        Time.captureDeltaTime=1f/60;p.RestoreAt(new Vector3(0,.1f,0));p.stats.ResetStats();yield return null;
        var cam=Camera.main;var rt=new RenderTexture(1280,720,24){antiAliasing=4};rt.Create();
        var image=new Texture2D(1280,720,TextureFormat.RGB24,false);var previous=cam.targetTexture;var active=RenderTexture.active;
        var audio=new PlayerAudioCapture();AudioListener.volume=1;AudioListener.pause=false;GamePreferences.SetEffects(1);GamePreferences.SetMusic(.5f);
        foreach(string view in new[]{"far","detail"})Directory.CreateDirectory(Path.Combine(output,view));
        GameObject target=null;Damageable hp=null;int frames=0;bool first=false,second=false,armorOnly=false,broken=false;
        try
        {
            for(int frame=0;frame<540;frame++)
            {
                if(frame==0||frame==240)
                {
                    if(target!=null)UnityEngine.Object.Destroy(target);
                    p.RestoreAt(new Vector3(0,.1f,0));
                    target=UnityEngine.Object.Instantiate(gm.stageManager.enemySpawner.meleePrefab,new Vector3(0,.1f,frame==0?7.5f:3.3f),Quaternion.identity);
                    var enemy=target.GetComponent<EnemyBase>();enemy.ConfigureP0Role(frame==0?EnemyKind.Elite:EnemyKind.Melee);enemy.TrainingTarget=true;enemy.Init(p.transform,null);enemy.enabled=false;
                    hp=target.GetComponent<Damageable>();hp.destroyOnDeath=false;target.GetComponent<E01SoldierMotion>().TrainingTarget=true;
                    hp.OnResolved+=(actor,result)=>{armorOnly|=result.ArmorDamage>0&&result.HealthDamage==0;broken|=result.BrokeArmor;};
                }
                bool fire=frame<240&&!hp.IsDead&&(target.GetComponent<ArmorHealth>()?.Intact??false);
                float distance=Vector3.Distance(p.transform.position,target.transform.position);
                bool close=!hp.IsDead&&!fire&&distance>3.4f;
                bool cut=!hp.IsDead&&!fire&&distance<4.1f&&(!p.Melee.IsAttacking||(p.Melee.ComboStage<2&&!p.Melee.NextQueued&&p.Melee.AttackElapsed>.07f));
                p.Simulate(new PlayerCommand{Fire=fire,Melee=cut,Dash=close&&distance>6&&p.IsDashReady,Move=close?Vector2.up:Vector2.zero,HasAim=true,AimPoint=hp.AimCenter},Time.deltaTime);
                yield return null;
                if(hp.IsDead){if(frame<240)first=true;else second=true;}
                audio.Advance(frame%2==1);if(frame%2==0)continue;
                var position=cam.transform.position;var rotation=cam.transform.rotation;float size=cam.orthographicSize;
                for(int view=0;view<2;view++)
                {
                    if(view==1)
                    {
                        cam.orthographicSize=4.9f;cam.transform.rotation=Quaternion.Euler(52,0,0);
                        Vector3 focus=(p.transform.position+target.transform.position)*.5f+Vector3.up*1.4f;cam.transform.position=focus-cam.transform.forward*24;
                    }
                    cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,1280,720),0,0);image.Apply();
                    File.WriteAllBytes(Path.Combine(output,view==0?"far":"detail","frame_"+frames.ToString("D4")+".png"),image.EncodeToPNG());
                }
                cam.transform.SetPositionAndRotation(position,rotation);cam.orthographicSize=size;cam.targetTexture=previous;RenderTexture.active=active;frames++;
            }
        }
        finally
        {
            audio.Save(Path.Combine(output,"game-mix.wav"));AudioListener.volume=0;cam.targetTexture=previous;RenderTexture.active=active;
            rt.Release();UnityEngine.Object.Destroy(rt);UnityEngine.Object.Destroy(image);if(target!=null)UnityEngine.Object.Destroy(target);
        }
        File.WriteAllText(Path.Combine(output,"detail-result.txt"),"Frames="+frames+"\nSeconds="+(frames/30f)+"\nAudioPeak="+audio.Peak+"\nMixedHeavyKill="+first+"\nBladeOrdinaryKill="+second+"\nArmorOnly="+armorOnly+"\nArmorBroken="+broken+"\nStationary production actors; real bullets and swept blade; paired cameras render the same simulation frame; no forced damage or execution gate.\n");
        check(first&&second&&armorOnly&&broken&&frames==270,"paired native close/far capture includes finite armor, mixed heavy kill and independent blade kill");
    }
}
