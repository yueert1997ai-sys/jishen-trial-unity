using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

[DefaultExecutionOrder(250)]
public sealed class CombatV5QuickCheck : MonoBehaviour
{
    private IEnumerator scenario;
    private string output;
    private bool finished,preview;
    private int errors,frame,shots,invalidShots;
    private float deadline;
    private GameManager gm;
    private PlayerController player;
    private RaikenBladePresentation blade;
    private ValkyrMotionDriver motion;
    private readonly List<string> report=new List<string>();
    private readonly List<string> trace=new List<string>{"frame,clip,time,stance,melee,grip_error,wrist,grip_x,grip_y,grip_z,tip_x,tip_y,tip_z,chest_y,hips_y"};
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if(Array.IndexOf(Environment.GetCommandLineArgs(),"-combatV5Check")>=0)
            DontDestroyOnLoad(new GameObject("CombatV5QuickCheck").AddComponent<CombatV5QuickCheck>());
    }
    private void Start()
    {
        string profile=Environment.GetEnvironmentVariable("MECH_EQUIPMENT_PROFILE");
        if(string.IsNullOrEmpty(profile)){Application.Quit(2);return;}
        output=Path.GetDirectoryName(profile);preview=Array.IndexOf(Environment.GetCommandLineArgs(),"-combatV5Preview")>=0;
        Directory.CreateDirectory(output);Directory.CreateDirectory(output+"/frames");Directory.CreateDirectory(output+"/game-frames");
        Application.logMessageReceived+=OnLog;PlayerInputRouter.AllowUnfocusedReplay=true;Application.runInBackground=true;
        Time.captureDeltaTime=1f/60;Application.targetFrameRate=120;deadline=Time.realtimeSinceStartup+300;scenario=Scenario();
    }
    private void LateUpdate()
    {
        if(finished||scenario==null)return;
        try{if(Time.realtimeSinceStartup>deadline)throw new Exception("V5 preview timeout");if(!scenario.MoveNext())Finish(null);}
        catch(Exception ex){Finish(ex.ToString());}
    }
    private PlayerCommand Command(bool fire=false,bool melee=false,Vector2 move=default,bool boost=false,bool skill=false)
        =>new PlayerCommand{Fire=fire,Melee=melee,Move=move,BoostHeld=boost,Skill=skill,HasAim=true,AimPoint=player.transform.position+Vector3.forward*18+Vector3.up*1.4f};
    private void Step(PlayerCommand command){player.Simulate(command,Time.deltaTime);}
    private IEnumerator Scenario()
    {
        for(int i=0;i<35;i++)yield return null;
        gm=GameManager.Instance;player=gm.playerController;player.InputRouter.readKeyboard=false;player.enabled=false;
        GamePreferences.SetLanguage(true);GamePreferences.SetMaster(0);
        blade=player.GetComponentInChildren<RaikenBladePresentation>();motion=player.GetComponentInChildren<ValkyrMotionDriver>();
        player.weaponController.BeamFired+=Shot;
        Check(player.Stance.State==WeaponStance.Sword,"initial stance is right-hand sword");
        EquipmentLoopQuickCheck.CaptureTo(output,"hangar",1280,720,true);
        gm.BeginWeaponTrial();var trial=gm.GetComponent<WeaponTrial>();trial.ClearTargets();
        for(int i=0;i<8;i++){Step(Command());yield return null;}
        Film("ready",true);
        int baselineOwned=gm.equipmentLoop.Warehouse.Profile.owned.Count;
        var near=trial.SpawnTarget(new Vector3(0,0,-9.6f),"",500).GetComponent<Damageable>();
        float maxError=0,maxWrist=0;
        for(int i=0;i<70;i++)
        {
            Step(Command(melee:i==7));yield return null;
            if(player.Melee.IsAttacking){maxError=Mathf.Max(maxError,motion.RightGripError);maxWrist=Mathf.Max(maxWrist,motion.RightWristAngle);}
            if(preview&&i%2==0)Film("single_hand_slash");
        }
        report.Add("MEASURE right grip="+maxError+" wrist="+maxWrist+" target hp="+near.CurrentHealth);
        Check(maxError<.13f,"right palm holds the physical sword through the cut");
        Check(maxWrist<35,"right wrist remains aligned with the forearm through the cut");
        Check(near.CurrentHealth==422,"ordinary white target hit exactly once for 78 damage");
        for(int i=0;i<50;i++)
        {
            Step(Command(fire:i<22));yield return null;
            if(preview&&i%2==0)Film("stow_fire");
        }
        Check(shots>0&&invalidShots==0&&player.Stance.State==WeaponStance.Ranged,"shooting waits for stow; releasing fire keeps ranged stance");
        Check(!blade.BeamEnabled&&blade.tip.position.y-player.transform.position.y>=.10f,"stowed sword has no beam and stays above ground");
        Film("back_mount",true,0);
        // A new melee pulse wins over a held fire button, which must then be re-pressed.
        for(int i=0;i<18;i++){Step(Command(fire:true));yield return null;}
        int beforeMelee=shots;
        for(int i=0;i<80;i++){Step(Command(fire:true,melee:i==0));yield return null;if(preview&&i%2==0)Film("draw_slash");}
        Check(shots==beforeMelee&&player.Stance.State==WeaponStance.Sword,"melee wins over sustained fire without mode oscillation");
        Step(Command());yield return null;
        for(int i=0;i<24;i++){Step(Command(fire:true));yield return null;}
        Check(shots>beforeMelee,"release and re-press switches back to ranged");
        // Buffer one ranged request during an early cut. It may only interrupt late recovery.
        player.RestoreAt(new Vector3(0,.1f,-12));float firstSheath=99;
        for(int i=0;i<65;i++)
        {
            Step(Command(melee:i==0,fire:i>=5));yield return null;
            if(player.Stance.State==WeaponStance.Sheathing)firstSheath=Mathf.Min(firstSheath,player.Melee.AttackElapsed);
        }
        Check(firstSheath>=.48f&&firstSheath<.55f,"one buffered ranged request waits until 0.48s recovery");
        trial.ClearTargets();
        foreach(int fps in new[]{30,60,120})
        {
            Time.captureDeltaTime=1f/fps;player.RestoreAt(new Vector3(0,.1f,-12));
            for(int i=0;i<3;i++){Step(Command());yield return null;}
            // Measure the actual tip path, then place an edge-contact target beyond that path.
            // Test close contacts separately: their colliders can physically block the lunge.
            Vector3 farthest=player.transform.position;
            Action<Vector3,Vector3> measure=(a,b)=>{if(b.z>farthest.z)farthest=b;};
            blade.CutSampled+=measure;
            for(int i=0;i<fps;i++){Step(Command(melee:i==2));yield return null;}
            blade.CutSampled-=measure;
            player.RestoreAt(new Vector3(0,.1f,-12));
            var tipEdge=trial.SpawnTarget(new Vector3(farthest.x,0,farthest.z+.42f),"",500).GetComponent<Damageable>();
            var outside=trial.SpawnTarget(new Vector3(farthest.x,0,farthest.z+1.1f),"",500).GetComponent<Damageable>();
            for(int i=0;i<fps*2;i++){Step(Command(melee:i==2));yield return null;}
            report.Add("MEASURE "+fps+"fps tip reach="+(farthest.z+12)+" edge="+tipEdge.CurrentHealth+" outside="+outside.CurrentHealth);
            Check(tipEdge.CurrentHealth==422&&outside.CurrentHealth==500,fps+"fps tip collider edge is hit; target beyond the swept blade is excluded");
            trial.ClearTargets();yield return null;player.RestoreAt(new Vector3(0,.1f,-12));
            var contacts=new List<Damageable>();
            foreach(float distance in new[]{1.25f,2.5f})contacts.Add(trial.SpawnTarget(new Vector3(0,0,-12+distance),"",500).GetComponent<Damageable>());
            for(int i=0;i<fps*2;i++){Step(Command(melee:i==2));yield return null;}
            Check(contacts.All(h=>h.CurrentHealth==422),fps+"fps close and middle targets receive one strike each in the same swing");
            trial.ClearTargets();yield return null;
        }
        Time.captureDeltaTime=1f/60;
        // A long frame must still cover the entire active interval, including ordinary ground targets.
        player.RestoreAt(new Vector3(0,.1f,-12));var skipped=trial.SpawnTarget(new Vector3(0,0,-9.5f),"",500).GetComponent<Damageable>();
        Step(Command(melee:true));yield return null;Time.captureDeltaTime=.46f;
        Step(Command());yield return null;Time.captureDeltaTime=1f/60;
        for(int i=0;i<30;i++){Step(Command());yield return null;}
        Check(skipped.CurrentHealth==422,"long frame crossing the full contact window still hits exactly once");
        trial.ClearTargets();yield return null;
        player.RestoreAt(new Vector3(0,.1f,-12));
        var hidden=trial.SpawnTarget(new Vector3(0,0,-8.8f),"",500).GetComponent<Damageable>();
        var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.name="AuditWall";wall.transform.position=new Vector3(0,2,-10);wall.transform.localScale=new Vector3(2,4,.35f);
        for(int i=0;i<75;i++){Step(Command(melee:i==2));yield return null;}
        Check(hidden.CurrentHealth==500,"wall blocks a target inside visible blade reach");Destroy(wall);trial.ClearTargets();yield return null;
        player.RestoreAt(new Vector3(0,.1f,-12));
        var cancelled=trial.SpawnTarget(new Vector3(0,0,-9.5f),"",500).GetComponent<Damageable>();
        for(int i=0;i<65;i++)
        {
            var c=Command(melee:i==2);c.Dash=i==7;c.Move=Vector2.right;Step(c);yield return null;
        }
        Check(cancelled.CurrentHealth==500&&!player.Melee.IsAttacking,"dash cancellation stops blade damage immediately");trial.ClearTargets();yield return null;
        // Normal-speed continuous movement and mode changes, including low-altitude boost.
        player.RestoreAt(new Vector3(0,.1f,-14));
        for(int i=0;i<120;i++){Step(Command(move:Vector2.up,fire:i>=70&&i<100));yield return null;if(preview&&i%2==0)Film("run_switch",false,100);}
        Check(player.Velocity.magnitude>5&&motion.RunBlend>.9f,"ground movement remains a powered run with moving limbs");
        player.RestoreAt(new Vector3(0,.1f,-12));
        var air=trial.SpawnTarget(new Vector3(0,0,-9.5f),"",500).GetComponent<Damageable>();
        for(int i=0;i<120;i++){Step(Command(boost:true,melee:i==32,fire:i>85));yield return null;if(preview&&i%2==0)Film("boost_cut_switch");}
        Check(air.CurrentHealth<500&&motion.FlightBlend>.9f,"boost slash hits a ground soldier while the visible mech remains airborne");
        Check(invalidShots==0,"all run/boost/melee switch shots happen in ranged stance only");
        // Trial kills never enter the persistent collection or kill reward counter.
        int kills=gm.Kills;air.Kill(null);yield return null;
        Check(gm.equipmentLoop.Pickups.Count==0&&gm.equipmentLoop.Warehouse.Profile.owned.Count==baselineOwned&&gm.Kills==kills,"weapon trial grants no gear, coins or kill rewards");
        gm.EndWeaponTrial();yield return null;
        gm.BeginRun();player.enabled=false;player.GetComponent<Damageable>().SetInvulnerable(90);
        for(int i=0;i<100;i++){Step(Command());yield return null;}
        var ordinary=FindObjectsByType<EnemyBase>(FindObjectsSortMode.None);
        Check(ordinary.Length>0&&ordinary.All(e=>e.GetComponent<E01SoldierMotion>()!=null&&e.kind==EnemyKind.Ranged),"ordinary deployment uses white rifle soldiers");
        var spawner=gm.stageManager.enemySpawner;
        Check(spawner.meleePrefab==spawner.rangedPrefab&&spawner.dronePrefab==spawner.rangedPrefab&&spawner.elitePrefab==spawner.rangedPrefab&&spawner.bossPrefab.GetComponent<BossController>()!=null,"all 4 ordinary spawn entries replaced; old-model boss retained");
        var soldier=ordinary[0];var soldierMotion=soldier.GetComponent<E01SoldierMotion>();
        var nav=soldier.GetComponent<NavMeshAgent>();nav.Warp(new Vector3(0,0,-6));
        player.RestoreAt(new Vector3(0,.1f,-12));player.GetComponent<Damageable>().SetInvulnerable(90);
        Vector3 enemyStart=soldier.transform.position;
        for(int i=0;i<170;i++){Step(Command());yield return null;if(preview&&i%2==0)Film("white_soldiers");}
        Check(soldierMotion.ShotsFired>0&&soldier.rifleMuzzle==soldierMotion.muzzle,"white soldier fires from the actual rifle muzzle");
        if(preview)
        {
            var cam=Camera.main;var pos=cam.transform.position;var rot=cam.transform.rotation;float size=cam.orthographicSize;
            cam.orthographicSize=1.9f;cam.transform.rotation=Quaternion.Euler(12,15,0);
            cam.transform.position=soldier.transform.position+Vector3.up*1.4f-cam.transform.forward*12;
            EquipmentLoopQuickCheck.CaptureTo(output,"e01_in_game",1280,720,false);
            cam.transform.SetPositionAndRotation(pos,rot);cam.orthographicSize=size;
        }
        report.Add("MEASURE soldier displacement="+Vector3.Distance(enemyStart,soldier.transform.position)+" shots="+soldierMotion.ShotsFired+" grip="+soldierMotion.GripError);
        var carrier=soldier.GetComponent<SalvageCarrier>();Check(carrier.gear.id=="e01_rifle"&&carrier.visual==soldierMotion.rifle.gameObject,"white soldier carries its real rifle as the sole recoverable part");
        var hp=soldier.GetComponent<Damageable>();hp.TakeDamage(999,new DamageInfo(player.gameObject,player.transform.position,player.GetComponent<Damageable>(),999));
        for(int i=0;i<30;i++){Step(Command());yield return null;if(preview&&i%2==0)Film("rifle_drop");}
        Check(gm.equipmentLoop.Pickups.Count==1&&gm.equipmentLoop.Pickups[0].Gear.id=="e01_rifle","defeated soldier drops one actual rifle mesh");
        gm.equipmentLoop.AbsorbNearby(true);
        for(int i=0;i<36;i++){Step(Command());yield return null;if(preview&&i%2==0)Film("absorb");}
        Check(gm.equipmentLoop.Warehouse.Owns("e01_rifle")&&new SalvageWarehouse(gm.equipmentLoop.Warehouse.Path).Owns("e01_rifle"),"rifle absorption persists in the isolated equipment warehouse");
        gm.stageManager.StopStage();gm.EnterHangar();
        Check(gm.equipmentLoop.TryEquip("e01_rifle"),"collected E01 rifle can be equipped from the warehouse");
        gm.BeginWeaponTrial();trial=gm.GetComponent<WeaponTrial>();trial.ClearTargets();
        Check(!FindObjectsByType<Projectile>(FindObjectsSortMode.None).Any(p=>p.team==1),"entering the weapon trial clears projectiles left over from combat");
        var skillTarget=trial.SpawnTarget(new Vector3(0,0,-7),"",1000).GetComponent<Damageable>();
        for(int i=0;i<60;i++){Step(Command(fire:i<40));yield return null;if(preview&&i%2==0)Film("equipped_rifle");}
        var recovered=player.GetComponent<E01PlayerRifle>();
        Check(recovered.Equipped&&recovered.Model.activeSelf&&player.weaponController.muzzle.IsChildOf(recovered.Model.transform)&&!blade.BeamEnabled,"equipped rifle is held in the right hand, fires from its muzzle and stows the sword");
        for(int i=0;i<65;i++){Step(Command(melee:i==0));yield return null;}
        Check(!recovered.Model.activeSelf&&player.Stance.State==WeaponStance.Sword,"drawing the sword hides the recovered rifle");
        player.weaponController.ResetCooldowns();Step(Command(skill:true));yield return null;
        Check(player.weaponController.SkillCooldownRemaining==0,"cannon skill does not consume its cooldown before stowing");
        for(int i=0;i<25;i++){Step(Command());yield return null;}
        Check(player.weaponController.SkillCooldownRemaining>0&&player.Stance.State==WeaponStance.Ranged,"cannon skill activates after stowing");
        gm.EndWeaponTrial();
        report.Add("Rendered frame pairs="+frame+" GPU="+SystemInfo.graphicsDeviceName);
    }
    private void Shot(){shots++;if(player.Stance.State!=WeaponStance.Ranged||player.Melee.IsAttacking)invalidShots++;}
    private void Film(string clip,bool still=false,float yaw=145)
    {
        if(!preview&&!still)return;
        var camera=Camera.main;var position=camera.transform.position;var rotation=camera.transform.rotation;float size=camera.orthographicSize;
        string file=still?clip:"frame_"+frame.ToString("D4");
        EquipmentLoopQuickCheck.CaptureTo(still?output:output+"/game-frames",file,960,540,true);
        camera.orthographicSize=3.85f;camera.transform.rotation=Quaternion.Euler(18,yaw,0);
        camera.transform.position=player.transform.position+Vector3.up*2.3f-camera.transform.forward*22;
        EquipmentLoopQuickCheck.CaptureTo(still?output:output+"/frames",still?clip+"_close":file,960,540,false);
        camera.transform.SetPositionAndRotation(position,rotation);camera.orthographicSize=size;
        if(!still)
        {
            Vector3 g=blade.grip.position,t=blade.tip.position;
            trace.Add(string.Join(",",frame,clip,player.Melee.AttackElapsed,player.Stance.State,player.Melee.IsAttacking,motion.RightGripError,motion.RightWristAngle,g.x,g.y,g.z,t.x,t.y,t.z,motion.Chest.eulerAngles.y,motion.Pelvis.eulerAngles.y));frame++;
        }
    }
    private void Check(bool ok,string message){report.Add((ok?"PASS ":"FAIL ")+message);if(!ok)throw new Exception(message);}
    private void OnLog(string message,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert){errors++;report.Add("ERROR "+message+"\n"+stack);}}
    private void Finish(string failure)
    {
        if(finished)return;finished=true;if(failure!=null)report.Add(failure);
        int code=errors==0&&failure==null?0:1;report.Add("COMBAT_V5_CHECK code="+code+" errors="+errors);
        File.WriteAllLines(output+"/quick-check.txt",report);File.WriteAllLines(output+"/motion.csv",trace);
        Application.logMessageReceived-=OnLog;Application.Quit(code);
    }
}
