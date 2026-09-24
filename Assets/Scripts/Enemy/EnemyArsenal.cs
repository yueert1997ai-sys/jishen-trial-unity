using System.Linq;
using UnityEngine;

[DefaultExecutionOrder(100)]
public sealed class EnemyArsenal : MonoBehaviour
{
    public EnemySpawnSpec Spec {get;private set;}
    public ImportedEnemyModel Model {get;private set;}
    public GameObject WeaponObject {get;private set;}
    public Transform Muzzle {get;private set;}
    public bool Heavy=>Spec.Heavy;
    public float Windup=>Heavy?.9f:Spec.weapon==EnemyWeapon.M14?.72f:.58f;
    public int BurstCount=>Spec.weapon==EnemyWeapon.M7?3:Spec.weapon==EnemyWeapon.Missiles?3:1;
    public float ShotSpeed=>Spec.weapon==EnemyWeapon.M14?55:Spec.weapon==EnemyWeapon.Rocket?18:Spec.weapon==EnemyWeapon.Missiles?15:36;
    public string GearId=>Id(Spec.weapon);
    EnemyBase enemy;Damageable owner;E01SoldierMotion soldier;Transform chest,upper,fore,hand,support;
    Vector3 committed;float commitUntil,recoil;
    public static string Id(EnemyWeapon weapon)=>new[]{"m7","m14","type08","ax01","rocket","missile_rack","back_cannon"}[(int)weapon];
    public static string Prefab(EnemyWeapon weapon)=>new[]{"M7","M14","TYPE08","HALBREAKER","J01_LAUNCHER","J01_LAUNCHER","GC_BACK_CANNON"}[(int)weapon];
    public void Configure(EnemySpawnSpec spec)
    {
        Spec=spec;enemy=GetComponent<EnemyBase>();owner=GetComponent<Damageable>();soldier=GetComponent<E01SoldierMotion>();
        if(spec.body!="E01")
        {
            var prefab=Resources.Load<GameObject>("Enemies/Arsenal/"+spec.body);
            if(prefab==null)throw new System.InvalidOperationException("Enemy model missing "+spec.body);
            if(soldier!=null){soldier.enabled=false;soldier.visual.gameObject.SetActive(false);}
            Model=Instantiate(prefab,transform).GetComponent<ImportedEnemyModel>();
            chest=Model.Chest;upper=Model.Find("UpperArm.R");fore=Model.Find("Forearm.R");hand=Model.Find("Hand.R");
            var cap=GetComponent<CapsuleCollider>();if(cap!=null){cap.height=4.5f;cap.center=Vector3.up*2.25f;cap.radius=.82f;}
            var nav=GetComponent<UnityEngine.AI.NavMeshAgent>();if(nav!=null){nav.height=4.5f;nav.radius=.82f;}
        }
        else chest=soldier.visual.GetComponentsInChildren<Transform>().First(t=>t.name=="E01_CHEST");
        if(soldier!=null&&soldier.rifle!=null)soldier.rifle.gameObject.SetActive(false);
        WeaponObject=Instantiate(Resources.Load<GameObject>("Hangar/"+Prefab(spec.weapon)),transform);
        WeaponObject.name="Enemy_"+spec.weapon;
        foreach(var c in WeaponObject.GetComponentsInChildren<Collider>())c.enabled=false;
        var parts=WeaponObject.GetComponentsInChildren<Transform>(true);
        Muzzle=parts.FirstOrDefault(t=>t.name=="Muzzle");support=parts.FirstOrDefault(t=>t.name=="Support")??WeaponObject.transform;
        if(Muzzle==null)throw new System.InvalidOperationException("Enemy weapon lacks muzzle "+spec.weapon);
        Muzzle.rotation=WeaponObject.transform.rotation;
        if(soldier!=null&&spec.body=="E01")
        {soldier.rifle=WeaponObject.transform;soldier.muzzle=Muzzle;soldier.support=support;}
        if(spec.weapon==EnemyWeapon.BackCannon&&Model!=null)
        {
            var ports=Model.muzzles.Where(t=>t.name.StartsWith("efLocator_body_10")).ToArray();
            if(ports.Length>0){WeaponObject.SetActive(false);Muzzle=ports[0];}
        }
        enemy.rifleMuzzle=Muzzle;
        if(spec.body=="DOM")enemy.moveSpeed=5.2f;
        if(spec.body=="GUNCANNON"){enemy.moveSpeed=2.6f;enemy.attackRange=18;}
        var carrier=GetComponent<SalvageCarrier>()??gameObject.AddComponent<SalvageCarrier>();carrier.gear=SalvageGear.Find(GearId);carrier.visual=WeaponObject;
        EnemyArmorPalette.Apply(gameObject);
        enemy.CombatBrain=GetComponent<EnemyCombatBrain>()??gameObject.AddComponent<EnemyCombatBrain>();
        enemy.CombatBrain.Configure(spec);
    }
    void LateUpdate()
    {
        if(GameManager.Instance?.IsPaused==true||owner==null||owner.IsDead)return;
        recoil=Mathf.MoveTowards(recoil,0,Time.deltaTime*2);
        if(Model==null||WeaponObject==null)return;
        Vector3 direction=Time.time<commitUntil?committed:enemy.WeaponAimDirection.sqrMagnitude>.01f?enemy.WeaponAimDirection:enemy.target!=null?enemy.target.position-transform.position:transform.forward;
        Pose(direction);
    }
    void Pose(Vector3 direction)
    {
        direction=PlanarCombat.Direction(direction,transform.forward);var rotation=Quaternion.LookRotation(direction,transform.up);
        if(Model==null){soldier?.SynchronizeForFire(direction);return;}
        if(Spec.weapon==EnemyWeapon.BackCannon)
        {
            foreach(var port in Model.muzzles.Where(t=>t.name.StartsWith("efLocator_body_10")))
            {var hinge=port.parent;hinge.rotation=Quaternion.FromToRotation(port.position-hinge.position,direction)*hinge.rotation;port.rotation=rotation;}
            return;
        }
        Vector3 grip=upper.position+transform.right*.12f-transform.up*.47f+direction*.50f-direction*recoil;
        var shoulder=WeaponObject.GetComponent<HalbreakerMount>();
        if(shoulder!=null)grip=chest.position+transform.right*.85f+Vector3.up*.6f-rotation*shoulder.shoulder.localPosition;
        WeaponObject.transform.SetPositionAndRotation(grip,rotation);
        Solve(upper,fore,hand,grip,upper.position+transform.right*.8f-Vector3.up*.5f);
        hand.rotation=rotation*Quaternion.Euler(0,0,90);
        if(support!=null)Solve(Model.Find("UpperArm.L"),Model.Find("Forearm.L"),Model.Find("Hand.L"),support.position,Model.Find("UpperArm.L").position-transform.right*.8f);
    }
    static void Solve(Transform a,Transform b,Transform c,Vector3 target,Vector3 hint)
    {
        float x=Vector3.Distance(a.position,b.position),y=Vector3.Distance(b.position,c.position);var v=target-a.position;
        float d=Mathf.Clamp(v.magnitude,Mathf.Abs(x-y)+.001f,x+y-.001f);var axis=v.normalized;
        float along=(x*x-y*y+d*d)/(2*d);var elbow=a.position+axis*along+Vector3.ProjectOnPlane(hint-a.position,axis).normalized*Mathf.Sqrt(Mathf.Max(0,x*x-along*along));
        a.rotation=Quaternion.FromToRotation(b.position-a.position,elbow-a.position)*a.rotation;
        b.rotation=Quaternion.FromToRotation(c.position-b.position,target-b.position)*b.rotation;
    }
    public void Fire(Vector3 direction)
    {
        committed=direction.normalized;commitUntil=Time.time+.65f;Pose(committed);recoil=.12f;
        Vector3 origin=Muzzle.position;float damage=Spec.weapon==EnemyWeapon.M14?12:Heavy?18:Spec.weapon==EnemyWeapon.M7?5:8;
        if(Spec.weapon==EnemyWeapon.Type08)MinovskyBeam.Fire(Muzzle,origin,committed,origin+committed*40,1,owner,damage,0,0);
        else if(Spec.weapon==EnemyWeapon.Ax01)HalbreakerBeam.Fire(Muzzle,origin,committed,origin+committed*45,1,owner,damage,1,0);
        else if(Spec.weapon==EnemyWeapon.BackCannon)
        {
            var ports=Model!=null?Model.muzzles.Where(t=>t.name.StartsWith("efLocator_body_10")).Take(2).ToArray():new[]{Muzzle};
            foreach(var port in ports)ArsenalBeam.Fire(port.position,committed,owner,damage/ports.Length,40,.55f,.40f);
        }
        else
        {
            bool missile=Spec.weapon==EnemyWeapon.Missiles;var shot=ProjectilePool.Spawn(missile,"Enemy_"+Spec.weapon,origin,EnergyBoltVisual.EnemyRed,missile||Spec.weapon==EnemyWeapon.Rocket?.22f:.13f);
            shot.Init(1,owner,committed,damage,ShotSpeed,3f,missile||Spec.weapon==EnemyWeapon.Rocket?1.25f:0,Spec.weapon==EnemyWeapon.M14?1:0);
            if(missile){var homing=(MissileProjectile)shot;homing.target=enemy.target.GetComponent<Damageable>();homing.turnRate=1.3f;}
        }
        BeamFxKit.MuzzleBlast(origin,committed,EnergyBoltVisual.EnemyRed,Heavy?.85f:.40f);
        GameAudio.PlayAt(Heavy?GameAudioCue.Beam:GameAudioCue.EnemyShot,origin,Heavy?.45f:.28f,Heavy?.85f:1);
    }
    public GameObject DetachWeapon(){var result=WeaponObject;WeaponObject=null;if(soldier!=null)soldier.rifle=null;return result;}
}
