using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

// Real Player frame order and public inputs; probes never attack the player.
public static class CombatRhythmChecks
{
    static void Drive(PlayerController p,bool melee=false,bool fire=false,bool dash=false,Vector2 move=default)
    {p.Simulate(new PlayerCommand{Melee=melee,Fire=fire,Dash=dash,Move=move,HasAim=true,AimPoint=new Vector3(0,1.6f,18)},Time.deltaTime);}
    static void Reset(PlayerController p)
    {p.RestoreAt(new Vector3(0,.1f,0));p.AimAt(new Vector3(0,1.6f,18));p.stats.ResetStats();}
    static IEnumerator Reach(PlayerController p,int stage)
    {
        Reset(p);Drive(p,melee:true);yield return null;
        for(int i=0;i<180;i++)
        {
            if(p.Melee.IsAttacking&&p.Melee.ComboStage==stage)yield break;
            Drive(p,melee:p.Melee.IsAttacking&&p.Melee.ComboStage<stage&&p.Melee.AttackElapsed>.07f);yield return null;
        }
        throw new Exception("Could not reach requested combo stage "+stage);
    }
    public static IEnumerator Run(GameManager gm,PlayerController p,string output,Action<bool,string> check,Action<string> capture)
    {
        check(CombatSliceSettings.Enabled,"R3 rhythm uses actual combat slice rules");
        var rows=new List<string>{"fps,test,stage,seconds"};
        var probes=new List<Damageable>();
        int shots=0;Action shot=()=>shots++;p.weaponController.BeamFired+=shot;
        try
        {
            foreach(int fps in new[]{30,60,120})
            {
                Time.captureDeltaTime=1f/fps;yield return null;
                for(int stage=0;stage<3;stage++)for(int phase=0;phase<3;phase++)
                {
                    var reach=Reach(p,stage);while(reach.MoveNext())yield return reach.Current;
                    float at=phase==0?.01f:phase==1?p.Melee.CurrentStroke.contactStart+.025f:p.Melee.CurrentStroke.contactEnd+.025f;
                    for(int i=0;p.Melee.AttackElapsed<at&&i<180;i++){Drive(p);yield return null;}
                    check(p.Melee.IsAttacking,"reached live cancel pose "+fps+"/"+stage+"/"+phase);
                    float energy=p.stats.CurrentEnergy;
                    if(phase==1)p.Melee.HoldImpact(.095f);
                    check(p.TryDash(Vector2.right),"dash accepts stroke/phase "+fps+"/"+stage+"/"+phase);
                    check(Mathf.Abs(energy-p.stats.CurrentEnergy-25)<.001f,"accepted cancel spends exactly 25 EN");
                    check(!p.Melee.IsAttacking&&!p.Melee.NextQueued&&!p.Melee.ImpactHeld&&p.Melee.ConsumeRootAdvance()==Vector3.zero,"cancel clears hitstop queue and pending root motion");
                    check(!p.TryDash(Vector2.left)&&Mathf.Abs(energy-p.stats.CurrentEnergy-25)<.001f,"repeat cancel cannot spend EN twice");
                    float start=Time.time;int before=shots;
                    for(int i=0;i<Mathf.CeilToInt(fps*.4f)&&shots==before;i++){Drive(p,fire:true,move:Vector2.right);yield return null;}
                    check(shots>before&&Time.time-start<=p.dashDuration+1f/fps+.002f,"cancel returns to held fire by dash exit plus one frame at "+fps+" measured="+(Time.time-start));
                    rows.Add(string.Format(CultureInfo.InvariantCulture,"{0},cancel_to_fire,{1},{2:F4}",fps,stage,Time.time-start));
                }
                Reset(p);int initialShots=shots;
                for(int i=0;i<fps/5;i++){Drive(p,fire:true);yield return null;}
                check(shots>initialShots,"held rifle fire begins");
                float drawStart=Time.time;Drive(p,melee:true,fire:true);yield return null;
                for(int i=0;i<fps/4&&!p.Melee.IsAttacking;i++){Drive(p,fire:true);yield return null;}
                check(p.Melee.IsAttacking&&Time.time-drawStart<=.10f+1f/fps,"held fire yields to manual slash in at most 100ms plus one frame");
                int shotsAtSlash=shots;float contactEnd=p.Melee.CurrentStroke.contactEnd;
                for(int i=0;i<fps&&!p.Melee.CanSheathe;i++)
                {Drive(p,fire:true);yield return null;check(shots==shotsAtSlash,"no rifle round during active sword damage window");}
                float recoveryStart=Time.time;
                for(int i=0;i<fps/3&&shots==shotsAtSlash;i++){Drive(p,fire:true);yield return null;}
                check(shots>shotsAtSlash&&!p.Melee.IsAttacking&&Time.time-recoveryStart<.16f,"held fire resumes after contact without release or full recovery");
                rows.Add(string.Format(CultureInfo.InvariantCulture,"{0},contact_to_fire,0,{1:F4}",fps,Time.time-recoveryStart));
                // Even an emergency cancel must respect exhausted EN and keep its attack intact on rejection.
                var heavy=Reach(p,2);while(heavy.MoveNext())yield return heavy.Current;
                var snapshot=p.stats.Capture();snapshot.energy=0;p.stats.Restore(snapshot);
                check(!p.TryDash(Vector2.right)&&p.Melee.IsAttacking&&p.Melee.ComboStage==2,"empty EN rejects dash without silently canceling heavy stroke");
                Reset(p);Drive(p,dash:true,move:Vector2.up);yield return null;Drive(p,melee:true);yield return null;
                for(int i=0;i<Mathf.CeilToInt(fps*.22f)&&!p.Melee.IsAttacking;i++){Drive(p);yield return null;}
                check(p.Melee.IsAttacking&&!p.IsDashing,"slash buffered through approach dash at "+fps);
            }
            Time.captureDeltaTime=1f/60;yield return null;Reset(p);
            for(int i=0;i<12;i++)
            {
                float a=i*Mathf.PI/6;var go=GameObject.CreatePrimitive(PrimitiveType.Capsule);go.name="R3_ContactProbe";
                go.transform.position=new Vector3(Mathf.Sin(a)*3.2f,1,Mathf.Cos(a)*3.2f+1);
                var hp=go.AddComponent<Damageable>();hp.team=1;hp.destroyOnDeath=false;hp.RestoreLife(10000,10000);probes.Add(hp);
            }
            yield return null;
            var hits=new Dictionary<string,int>();var stageHits=new int[3];
            Action<Damageable,float> struck=(d,a)=>{string k=p.Melee.ComboStage+"/"+d.GetInstanceID();hits[k]=hits.TryGetValue(k,out int n)?n+1:1;stageHits[p.Melee.ComboStage]++;};
            p.Melee.StrikeHit+=struck;
            try
            {
                Drive(p,melee:true,fire:true);yield return null;
                for(int i=0;i<110;i++)
                {Drive(p,fire:true,melee:p.Melee.IsAttacking&&p.Melee.ComboStage<2&&p.Melee.AttackElapsed>.07f);yield return null;}
                check(stageHits[0]>0&&stageHits[1]>0&&stageHits[2]>0,"held rifle input does not steal explicitly queued three-cut chain");
                foreach(var n in hits.Values)check(n==1,"one actual blade hit per target per stroke");
                check(p.Stance.CanFire&&!p.Melee.IsAttacking,"three-cut chain flows back into held rifle");
                capture("r3_chain_to_rifle.png");
                Reset(p);Drive(p,melee:true);yield return null;
                for(int i=0;i<20&&!p.Melee.IsAttacking;i++){Drive(p);yield return null;}
                check(p.TryDash(Vector2.left),"cancel before first contact");
                float[] health=new float[probes.Count];for(int i=0;i<health.Length;i++)health[i]=probes[i].CurrentHealth;
                p.Melee.SampleBladeSweep(new Vector3(-8,0,0),new Vector3(8,0,0),new Vector3(-8,0,5),new Vector3(8,0,5));
                for(int i=0;i<25;i++){Drive(p);yield return null;}
                for(int i=0;i<health.Length;i++)check(probes[i].CurrentHealth==health[i],"cancel leaves no ghost blade damage or deferred slash");
            }
            finally{p.Melee.StrikeHit-=struck;}
            File.WriteAllLines(Path.Combine(output,"rhythm-timings.csv"),rows);
        }
        finally
        {
            p.weaponController.BeamFired-=shot;
            foreach(var probe in probes)if(probe!=null)UnityEngine.Object.Destroy(probe.gameObject);
            Time.captureDeltaTime=1f/60;
        }
    }
}
