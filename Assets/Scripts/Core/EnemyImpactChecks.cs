using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;
using UnityEngine.AI;

public static class EnemyImpactChecks
{
    public static IEnumerator Run(GameManager gm,PlayerController p,string output,Action<bool,string> check,Action<string> capture)
    {
        check(CombatSliceSettings.Enabled,"enemy impact checks use slice rules");
        var rows=new List<string>{"fps,kind,distance"};
        GameObject go=null;
        try
        {
            p.RestoreAt(new Vector3(0,.1f,-3));
            check(NavMesh.SamplePosition(new Vector3(0,0,5),out var spot,2,NavMesh.AllAreas),"production arena has probe location");
            go=UnityEngine.Object.Instantiate(gm.stageManager.enemySpawner.meleePrefab,spot.position,Quaternion.identity);
            var enemy=go.GetComponent<EnemyBase>();enemy.TrainingTarget=true;enemy.Init(p.transform,null);enemy.ConfigureP0Role(EnemyKind.Melee);
            var hp=go.GetComponent<Damageable>();hp.destroyOnDeath=false;hp.RestoreLife(1000,1000);
            var motion=go.GetComponent<E01SoldierMotion>();motion.TrainingTarget=true;
            var nav=go.GetComponent<NavMeshAgent>();var source=p.GetComponent<Damageable>();
            enemy.enabled=false;yield return null;
            check(motion!=null,"production E01 authored motion loaded");
            var armor=motion.visual.GetComponentInChildren<MeshRenderer>();
            var marker=new MaterialPropertyBlock();armor.GetPropertyBlock(marker);marker.SetFloat("_R4Probe",.37f);marker.SetColor("_Color",new Color(.31f,.41f,.51f));armor.SetPropertyBlock(marker);
            float bulletWeight=0,lightWeight=0,heavyWeight=0;
            for(int type=0;type<3;type++)
            {
                hp.RestoreLife(1000,1000);
                var position=go.transform.position;
                hp.TakeDamage(1,new DamageInfo(type==0?null:p.gameObject,position+Vector3.left*5,source,1){Impact=0,HeavyImpact=type==2});
                yield return null;yield return null;
                float weight=motion.ReactionWeight;
                if(type==0)bulletWeight=weight;else if(type==1)lightWeight=weight;else heavyWeight=weight;
                check(Vector3.Dot(motion.HitDirection,Vector3.right)>.99f,"impact follows actual incoming direction");
                var block=new MaterialPropertyBlock();armor.GetPropertyBlock(block);
                check(Mathf.Abs(block.GetFloat("_R4Probe")-.37f)<.001f&&block.GetColor("_Color")==marker.GetColor("_Color"),"armor flash preserves material tint and unrelated overrides");
                capture("r4_hit_"+type+".png");
                gm.SetPaused(true);float paused=motion.ReactionWeight;
                for(int i=0;i<5;i++)yield return null;
                check(Mathf.Abs(motion.ReactionWeight-paused)<.0001f,"pause freezes hit response");gm.SetPaused(false);
                for(int i=0;i<24;i++)yield return null;
                armor.GetPropertyBlock(block);
                check(motion.ReactionWeight<.001f&&block.GetColor("_Color")==marker.GetColor("_Color")&&Mathf.Abs(block.GetFloat("_R4Probe")-.37f)<.001f,"recoil and armor recover without persistent tint");
            }
            check(heavyWeight>lightWeight*1.3f&&lightWeight>bulletWeight*1.5f,"bullet light slash heavy slash have distinct measured response strengths");
            foreach(int fps in new[]{30,60,120})foreach(bool heavy in new[]{false,true})
            {
                Time.captureDeltaTime=1f/fps;yield return null;
                nav.Warp(spot.position);hp.RestoreLife(1000,1000);enemy.enabled=true;
                // A finite armor break precedes the same production melee impulse.
                if(heavy){var layer=go.GetComponent<ArmorHealth>()??go.AddComponent<ArmorHealth>();layer.Configure(1);hp.ApplyDamage(1,new DamageInfo(p.gameObject,go.transform.position-Vector3.right*5,source,1){HeavyImpact=true});}
                enemy.ReceiveMeleeImpact(Vector3.right,heavy);Vector3 start=go.transform.position;
                check(enemy.HitStaggerRemaining>0,"melee hit opens brief reaction window");
                for(int i=0;i<Mathf.CeilToInt((heavy?.30f:.18f)*fps);i++)yield return null;
                float distance=Vector3.Dot(go.transform.position-start,Vector3.right);
                check(distance>(heavy?1.40f:.47f)&&distance<(heavy?1.60f:.54f),"frame-independent "+(heavy?"heavy":"light")+" push at "+fps+": "+distance.ToString("F3"));
                check(NavMesh.SamplePosition(go.transform.position,out var landed,.15f,NavMesh.AllAreas),"knockback stays on production navigation surface");
                if(heavy)check(!go.GetComponent<ArmorHealth>().Intact,"heavy impulse works after armor depletion without regenerating the layer");
                rows.Add(string.Format(CultureInfo.InvariantCulture,"{0},{1},{2:F4}",fps,heavy?"heavy":"light",distance));
                enemy.enabled=false;
            }
            Time.captureDeltaTime=1f/60;yield return null;
            hp.RestoreLife(1000,1000);nav.Warp(spot.position);enemy.ConfigureP0Role(EnemyKind.Ranged);enemy.enabled=true;
            for(int i=0;i<150&&!enemy.HasAttackWarning;i++)yield return null;
            check(enemy.HasAttackWarning,"production ranged enemy enters telegraphed attack");
            int shots=motion.ShotsFired;
            enemy.ReceiveMeleeImpact(Vector3.right,true);
            check(!enemy.HasAttackWarning,"interrupt removes only this enemy's attack warning");
            for(int i=0;i<24;i++)yield return null;
            check(motion.ShotsFired==shots,"interrupted windup does not fire a delayed projectile");
            for(int i=0;i<180&&motion.ShotsFired==shots;i++)yield return null;
            check(motion.ShotsFired>shots,"enemy can resume attacking after recovering");
            // Pool-generation guard: an old owner cannot cancel a reused warning.
            CombatEffects.ClearTelegraphs();
            var warning=CombatEffects.Disc(Vector3.zero,1,1,Color.red);int old=warning.Generation;warning.Cancel(old);
            var reused=CombatEffects.Disc(Vector3.zero,1,1,Color.red);warning.Cancel(old);
            check(reused.gameObject.activeSelf,"stale warning handle cannot hide reused telegraph");reused.Cancel(reused.Generation);
            enemy.enabled=false;hp.RestoreLife(1000,1000);nav.Warp(spot.position);enemy.enabled=true;
            enemy.TrainingTarget=false;motion.TrainingTarget=false;
            for(int i=0;i<150&&!enemy.HasAttackWarning;i++)yield return null;
            int deaths=OverdriveVfx.DeathBursts,kills=gm.Kills;shots=motion.ShotsFired;
            hp.TakeDamage(10000,new DamageInfo(p.gameObject,go.transform.position-Vector3.right*5,source,10000){HeavyImpact=true});
            check(hp.IsDead&&!enemy.HasAttackWarning&&enemy.HitStaggerRemaining==0,"lethal hit clears attack and movement state immediately");
            foreach(var collider in go.GetComponents<Collider>())check(!collider.enabled,"corpse cannot block further movement or shots");
            check(gm.Kills==kills+1&&OverdriveVfx.DeathBursts==deaths+1,"death counts and effects occur exactly once");
            hp.TakeDamage(10000,null);check(gm.Kills==kills+1,"corpse damage cannot award another kill");
            for(int i=0;i<12;i++)yield return null;
            check(motion.DeathProgress>0&&Vector3.Dot(go.transform.TransformVector(motion.visual.localPosition),Vector3.right)>.05f&&motion.ShotsFired==shots,"corpse falls in hit direction without firing");capture("r4_directional_death.png");
            for(int i=0;i<35;i++)yield return null;
            check(go==null,"production corpse cleans up after its short fall");
            File.WriteAllLines(Path.Combine(output,"impact-distances.csv"),rows);
        }
        finally{if(go!=null)UnityEngine.Object.Destroy(go);Time.captureDeltaTime=1f/60;}
    }
}
