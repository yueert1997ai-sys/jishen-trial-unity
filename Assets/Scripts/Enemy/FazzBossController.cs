using System.Linq;
using UnityEngine;
using UnityEngine.AI;

public readonly struct FazzBossProfile
{
    public readonly float health,gap;
    public FazzBossProfile(int room){room=Mathf.Clamp(room,0,2);health=1800+400*room;gap=new[]{1.2f,.85f,.55f}[room];}
}

// These clocks are the extracted montage segments and AnimNotify firing times.
// The existing BossController retains health, death, rewards and encounter ownership.
[DefaultExecutionOrder(135)]
public sealed class FazzBossController : MonoBehaviour
{
    public ImportedEnemyModel model;
    public int Room {get;private set;}
    public int Pattern {get;private set;}=-1;
    public float Age {get;private set;}
    public int MegaShots {get;private set;}
    public int TwinShots {get;private set;}
    public int Missiles {get;private set;}
    public bool Active {get;private set;}
    BossController boss;Damageable health;NavMeshAgent navigation;FazzBossProfile profile;
    Vector3 direction;float nextAction=1.8f;int cycle,fired;TelegraphVisual warning;int warningGeneration;
    static readonly string[][] Clips={new[]{"sht_HMCannonSetUp","sht_HMCannon_s","sht_HMCannon_l","sht_HMCannon_e"},new[]{"sht_BeamCannonSetUp","sht_BeamCannon_s","sht_BeamCannon_e"},new[]{"sht_BackMissile_SetUp","sht_BackMissile_l","sht_BackMissile_e"}};
    static readonly float[][] Segments={new[]{.4666667f,.4666667f,1f,.6333333f},new[]{.3333333f,.5f,.5f},new[]{.6f,.9f,.8333333f}};
    public void Configure(int room){Room=Mathf.Clamp(room,0,2);profile=new FazzBossProfile(Room);nextAction=Time.time+1.8f;}
    public void Tick()
    {
        if(boss==null){boss=GetComponent<BossController>();health=GetComponent<Damageable>();navigation=GetComponent<NavMeshAgent>();}
        if(health.IsDead||GameManager.Instance?.IsCombatActive!=true){Cancel();return;}
        if(boss.target==null)return;
        if(Active)
        {
            Age+=Time.deltaTime;float time=Age;int at=0;
            while(at<Segments[Pattern].Length-1&&time>=Segments[Pattern][at])time-=Segments[Pattern][at++];
            model.Play(Clips[Pattern][at],time/Segments[Pattern][at]);
            if(navigation.isOnNavMesh)navigation.isStopped=true;
            return;
        }
        model.Play(null,0);
        var delta=boss.target.position-transform.position;delta.y=0;
        if(delta.sqrMagnitude>.1f)transform.rotation=Quaternion.RotateTowards(transform.rotation,Quaternion.LookRotation(delta),140*Time.deltaTime);
        if(navigation.isOnNavMesh){navigation.isStopped=delta.magnitude<13; if(!navigation.isStopped)navigation.SetDestination(boss.target.position);}
        if(Time.time>=nextAction)Begin(cycle++%3);
    }
    public bool Begin(int pattern)
    {
        if(Active||boss==null||health.IsDead||boss.target==null)return false;
        Pattern=Mathf.Clamp(pattern,0,2);Age=0;fired=0;Active=true;
        direction=PlanarCombat.Direction(boss.target.position-transform.position,transform.forward);transform.rotation=Quaternion.LookRotation(direction);
        boss.SetFazzAction(true,false,(BossPattern)(4+Pattern));
        float delay=Pattern==0?.6620942f:Pattern==1?.3333333f:.6f;
        warning=CombatEffects.Line(transform.position,direction,40,Pattern==0?1.4f:1f,delay,EnergyBoltVisual.EnemyRed);warningGeneration=warning.Generation;
        GameAudio.PlayAt(GameAudioCue.Warning,transform.position,.45f,.75f);model.Play(Clips[Pattern][0],0);return true;
    }
    Transform Port(string prefix)=>model.muzzles.FirstOrDefault(t=>t.name.StartsWith(prefix));
    void LateUpdate()
    {
        if(!Active||boss==null||health.IsDead||GameManager.Instance?.IsCombatActive!=true)return;
        if(Pattern==0&&fired==0&&Age>=.6620942f)
        {
            var port=Port("efLocator_back_505");ArsenalBeam.Fire(port.position,direction,health,32,45,1.05f,1.27f);
            MegaShots++;fired=1;GameAudio.PlayAt(GameAudioCue.Beam,port.position,.8f,.66f);
        }
        else if(Pattern==1&&fired==0&&Age>=.3333333f)
        {
            foreach(var prefix in new[]{"efLocator_back_507","efLocator_back_508"})
            {var port=Port(prefix);ArsenalBeam.Fire(port.position,direction,health,15,40,.60f,.5f);TwinShots++;}
            fired=1;GameAudio.PlayAt(GameAudioCue.Beam,transform.position,.65f,.86f);
        }
        else if(Pattern==2)
        {
            while(fired<9&&Age>=.6f+fired*.1f)
            {
                for(int side=0;side<2;side++)
                {
                    var port=Port("efLocator_back_"+(501+(fired%2)*2+side));
                    if(port==null)port=Port("efLocator_back_506");
                    var missile=(MissileProjectile)ProjectilePool.Spawn(true,"FAZZ 18-tube missile",port.position,EnergyBoltVisual.EnemyRed,.20f);
                    var launch=(Quaternion.AngleAxis((side==0?-1:1)*(14+fired*1.4f),Vector3.up)*direction+Vector3.up*.65f).normalized;
                    missile.Init(1,health,launch,4.5f,15,4,1.15f,0);missile.target=boss.target.GetComponent<Damageable>();missile.turnRate=1.15f;
                    BeamFxKit.MuzzleBlast(port.position,launch,EnergyBoltVisual.EnemyRed,.35f);Missiles++;
                }
                fired++;GameAudio.PlayAt(GameAudioCue.Missile,transform.position,.18f,.9f);
            }
        }
        if(Age>=Segments[Pattern].Sum())
        {Active=false;ClearWarning();model.Play(null,0);nextAction=Time.time+profile.gap;boss.SetFazzAction(false,true,(BossPattern)(4+Pattern));}
    }
    void ClearWarning(){if(warning!=null)warning.Cancel(warningGeneration);warning=null;}
    public void Cancel(){Active=false;ClearWarning();if(model!=null)model.Play(null,0);}
    void OnDisable(){Cancel();}
}
