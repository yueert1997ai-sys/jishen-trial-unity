using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
using Object=UnityEngine.Object;

// Regression fixtures use real actors and UI controls; live combat replays remain separate.
public static class AiUiChecks
{
    static Vector3 Fixture()
    {
        for(int z=-12;z<=10;z+=2)for(int x=-10;x<=10;x+=2)
        {
            Vector3 a=new Vector3(x,0,z),b=a+Vector3.forward*8;
            if(NavMesh.SamplePosition(a,out var start,.2f,NavMesh.AllAreas)
                &&NavMesh.SamplePosition(b,out var end,.2f,NavMesh.AllAreas)
                &&!NavMesh.Raycast(start.position,end.position,out _,NavMesh.AllAreas)
                &&EnemyTactics.ClearSight(a,b))return start.position;
        }
        throw new Exception("No clear eight-metre AI fixture in active arena");
    }
    static void Clear(EnemyBase enemy)
    {
        if(enemy!=null)Object.Destroy(enemy.gameObject);
        foreach(var shot in Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None))shot.Despawn();
    }
    static T Find<T>(string name) where T:Component => Object.FindObjectsByType<T>(FindObjectsInactive.Include,FindObjectsSortMode.None).First(x=>x.name==name);
    static Button Button(string name)=>Find<Button>(name);
    public static IEnumerator Run(GameManager gm,PlayerController p,string output,Action<bool,string> check,Action<string> capture)
    {
        EnemyBase enemy=null;
        Vector3 origin=Fixture();
        try
        {
            foreach(int fps in new[]{30,60,120})
            {
                Time.captureDeltaTime=1f/fps;p.RestoreAt(origin+Vector3.up*.1f);p.stats.ResetStats();p.GetComponent<Damageable>().SetInvulnerable(120);
                enemy=gm.stageManager.enemySpawner.SpawnEnemy(EnemyKind.Drone,origin+Vector3.forward);
                enemy.moveSpeed=0;
                for(int n=0;n<fps&&enemy.AttackPhase!=EnemyAttackPhase.Windup;n++)yield return null;
                check(enemy.IsDetonating,"drone enters real detonation windup at "+fps);
                enemy.GetComponent<Damageable>().ApplyDamage(1,new DamageInfo(p.gameObject,p.transform.position,p.GetComponent<Damageable>(),1){MeleeStrike=true});
                check(!enemy.IsDetonating&&!EnemyTactics.HasAttackSlot(enemy),"interrupt resets detonation latch and reservation at "+fps);
                enemy.moveSpeed=4; // The stronger hit now pushes it out of contact range; allow its real approach after recovery.
                for(int n=0;n<fps*3&&enemy!=null&&!enemy.GetComponent<Damageable>().IsDead;n++)yield return null;
                check(enemy==null||enemy.GetComponent<Damageable>().IsDead,"interrupted drone recovers and completes a fresh attack at "+fps);
                Clear(enemy);enemy=null;yield return null;
                enemy=gm.stageManager.enemySpawner.SpawnEnemy(EnemyKind.Melee,origin+Vector3.forward*3.7f);
                for(int n=0;n<fps*5&&enemy.MeleeStrikesCompleted==0;n++)yield return null;
                check(enemy.LungesCompleted>0,"melee gap closer completes at "+fps);
                check(enemy.MeleeStrikesCompleted>0,"melee follows lunge with close attack instead of orbiting at "+fps);
                Clear(enemy);enemy=null;yield return null;
            }
            Time.captureDeltaTime=1f/60;
            p.RestoreAt(origin+Vector3.up*.1f);
            enemy=gm.stageManager.enemySpawner.SpawnEnemy(EnemyKind.Melee,origin+Vector3.forward*3.7f);
            for(int n=0;n<120&&enemy.AttackPhase!=EnemyAttackPhase.Windup;n++)yield return null;
            check(enemy.AttackPhase==EnemyAttackPhase.Windup,"lunge announces before trail-entry fixture");
            p.RestoreAt(origin+Vector3.right*3+Vector3.up*.1f);
            for(int n=0;n<120&&enemy.transform.position.z>origin.z+2;n++)yield return null;
            p.RestoreAt(origin+Vector3.forward*4.4f+Vector3.up*.1f);
            p.GetComponent<Damageable>().RestoreLife(p.stats.MaxHp,p.stats.MaxHp);float beforeTrail=p.stats.CurrentHp;
            for(int n=0;n<20&&enemy.LungesCompleted==0;n++)yield return null;
            check(enemy.LungesCompleted==1&&Mathf.Abs(p.stats.CurrentHp-beforeTrail)<.01f,"entering an already traversed lunge trail causes no retroactive hit");
            Clear(enemy);enemy=null;yield return null;
            p.RestoreAt(origin+Vector3.up*.1f);p.GetComponent<Damageable>().SetInvulnerable(120);
            enemy=gm.stageManager.enemySpawner.SpawnEnemy(EnemyKind.Ranged,origin+Vector3.forward*7);
            for(int n=0;n<180&&enemy.ShotsEmitted==0;n++)yield return null;
            check(enemy.ShotsEmitted==1,"shot telemetry records first real emission immediately");
            var projectile=Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None).First(s=>s.team==1);
            check(enemy.rifleMuzzle!=null&&Vector3.Dot(enemy.rifleMuzzle.forward,projectile.direction)>.985f,"projectile matches posed rifle muzzle");
            int shots=enemy.ShotsEmitted;gm.SetPaused(true);for(int n=0;n<12;n++)yield return null;
            check(!enemy.HasAttackWarning&&!EnemyTactics.HasAttackSlot(enemy),"pause cancels unfinished enemy attacks and warnings");
            check(enemy.ShotsEmitted==shots,"pause emits no late burst shots");
            capture("production-ui-pause.png");gm.SetPaused(false);
            enemy.target=null;for(int n=0;n<12;n++)yield return null;
            check(EnemyTactics.AttackersActive==0&&!enemy.HasAttackWarning,"lost target releases attack work");
            Clear(enemy);enemy=null;yield return null;
            check(gm.combatHUD.IsVisible,"short combat uses production HUD");
            check(!p.transform.Find("PlayerGroundMarker").gameObject.activeSelf,"player ground marker disabled");
            check(Find<RectTransform>("CombatCrosshair").GetComponent<ControlRingGraphic>()==null,"aim uses corners without a circle");
            var disc=CombatEffects.Disc(origin,2,1,Color.red);
            check(!disc.line.enabled&&disc.transform.Find("Fill").gameObject.activeSelf,"area warning is a filled field without a line hoop");
            disc.Cancel(disc.Generation);
            var line=CombatEffects.Line(origin,Vector3.forward,4,.15f,1,Color.red);
            check(line.line.enabled&&!line.transform.Find("Fill").gameObject.activeSelf,"pooled line restores visibility after disc reuse");line.Cancel(line.Generation);
            p.GetComponent<Damageable>().RestoreLife(p.stats.MaxHp,p.stats.MaxHp*.21f);
            p.stats.TrySpendEnergy(35);p.AimAt(origin+new Vector3(2,0,6));
            for(int n=0;n<15;n++)yield return null;
            check(Find<Text>("HpText").text.Contains(Mathf.CeilToInt(p.stats.CurrentHp).ToString()),"HUD reflects actual damaged health");
            Canvas.ForceUpdateCanvases();
            check(Find<Text>("HpText").cachedTextGenerator.vertexCount>4,"health digits generate visible glyphs without line-height clipping");
            var hp=Find<RectTransform>("HpBar").Find("Fill").GetComponent<RectTransform>();
            check(Mathf.Abs(hp.anchorMax.x-p.stats.CurrentHp/p.stats.MaxHp)<.01f,"health bar matches actual damage ratio");
            capture("production-ui-combat.png");
            foreach(var size in new[]{new Vector2Int(1280,720),new Vector2Int(1280,1024),new Vector2Int(1600,720),new Vector2Int(1920,1080),new Vector2Int(1600,900)})
            {
                Screen.SetResolution(size.x,size.y,false);
                for(int n=0;n<8;n++)yield return null;
                check(Screen.width==size.x&&Screen.height==size.y,"requested UI viewport was applied "+size);
                Canvas.ForceUpdateCanvases();
                foreach(string name in new[]{"StatusPanel","ActionPanel","MissionPanel","PauseButton"})
                {
                    var corners=new Vector3[4];Find<RectTransform>(name).GetWorldCorners(corners);
                    check(corners.All(c=>c.x>=-1&&c.x<=Screen.width+1&&c.y>=-1&&c.y<=Screen.height+1),name+" remains inside viewport "+Screen.width+"x"+Screen.height);
                }
                check(Find<Text>("HpText").cachedTextGenerator.vertexCount>4,"health glyphs remain visible at "+Screen.width+"x"+Screen.height);
            }
            Button("PauseButton").onClick.Invoke();yield return null;
            check(gm.IsPaused,"HUD pause button pauses game");Button("ResumeButton").onClick.Invoke();yield return null;check(!gm.IsPaused,"resume button returns to battle");
            gm.EnterResult(false);for(int n=0;n<12;n++)yield return null;
            capture("production-ui-result.png");
            gm.ExitPractice();for(int n=0;n<20;n++)yield return null;capture("production-ui-hangar.png");
            check(gm.hangarUI.IsVisible&&!gm.combatHUD.IsVisible,"hangar shows menu and hides combat HUD");
            gm.BeginShortCombat();for(int n=0;n<25;n++)yield return null;
            gm.EnterResult(false);for(int n=0;n<12;n++)yield return null;
            check(Find<Canvas>("ResultCanvas").gameObject.activeSelf,"trial no longer hides result menu");
            Button("ReturnHangarButton").onClick.Invoke();for(int n=0;n<12;n++)yield return null;
            check(gm.IsCombatActive&&Object.FindFirstObjectByType<P0CombatDemo>()!=null&&gm.combatHUD.IsVisible,"retry button restarts short trial without switching to campaign");
            gm.SetPaused(true);yield return null;Button("RestartButton").onClick.Invoke();for(int n=0;n<12;n++)yield return null;
            check(gm.Phase==GamePhase.Hangar&&!gm.IsPaused&&EnemyTactics.AttackersActive==0,"pause return goes to hangar and clears enemies");
        }
        finally{if(gm.IsPaused)gm.SetPaused(false);Clear(enemy);}
    }
}
