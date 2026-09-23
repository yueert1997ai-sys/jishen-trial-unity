using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

public sealed class WeaponTrial : MonoBehaviour
{
    private readonly List<GameObject> targets=new List<GameObject>();
    private GameManager owner;
    private Canvas canvas;
    public void Open(GameManager gm)
    {
        owner=gm;
        canvas=RuntimeUIFactory.CreateCanvas("WeaponTrialCanvas",960);canvas.sortingOrder=80;
        var safe=SafeAreaLayout.Create(canvas);
        var strip=RuntimeUIFactory.CreatePanel(safe,"CompactRangeControls",Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero,new Color(.035f,.055f,.079f,.94f));
        RuntimeUIFactory.Place(strip,new Vector2(.5f,1),new Vector2(0,-42),new Vector2(790,52));
        var hint=RuntimeUIFactory.MenuText(strip,"TrialHint","",15,new Vector2(235,-26),new Vector2(438,34));
        hint.text=EquipmentWarehouseUI.T("武器试场 · 左键开火 / Q 挥刀", "WEAPON RANGE · LMB fire / Q slash");
        RuntimeUIFactory.MenuButton(strip,"ResetTrial",EquipmentWarehouseUI.T("重置 [R]","Reset [R]"),new Vector2(550,-26),new Vector2(120,34)).onClick.AddListener(ResetTargets);
        RuntimeUIFactory.MenuButton(strip,"LeaveTrial",EquipmentWarehouseUI.T("返回机库","Hangar"),new Vector2(699,-26),new Vector2(154,34)).onClick.AddListener(()=>owner.EndWeaponTrial());
        ResetTargets();
    }
    private void Update(){if(owner!=null&&!owner.IsPaused&&Input.GetKeyDown(KeyCode.R))ResetTargets();}
    public EnemyBase SpawnTarget(Vector3 position,string label,float health=500)
    {
        var go=Instantiate(owner.stageManager.enemySpawner.rangedPrefab,position,Quaternion.Euler(0,180,0));targets.Add(go);
        var enemy=go.GetComponent<EnemyBase>();enemy.TrainingTarget=true;enemy.target=null;
        var nav=go.GetComponent<NavMeshAgent>();if(nav!=null)nav.enabled=false;
        var motion=go.GetComponent<E01SoldierMotion>();motion.TrainingTarget=true;
        var hp=go.GetComponent<Damageable>();hp.destroyOnDeath=false;hp.RestoreLife(health,health);
        go.name="Trial_"+label;
        if(!string.IsNullOrEmpty(label))
        {
            var text=new GameObject("TargetLabel").AddComponent<TextMesh>();text.transform.SetParent(go.transform,false);
            text.text=label;text.characterSize=.16f;text.fontSize=45;text.anchor=TextAnchor.MiddleCenter;
            text.transform.position=position+Vector3.up*3.12f;text.transform.rotation=Camera.main.transform.rotation;text.color=new Color(.06f,.20f,.28f);
        }
        return enemy;
    }
    public void ClearTargets(){foreach(var obj in targets)if(obj!=null)Destroy(obj);targets.Clear();}
    public void ResetTargets()
    {
        CombatEffects.ClearTelegraphs();
        foreach(var projectile in FindObjectsByType<Projectile>(FindObjectsSortMode.None))projectile.Despawn();
        ClearTargets();owner.playerController.RestoreAt(new Vector3(0,.1f,-12));
        if(owner.playerController.Loadout.Selected==PrimaryWeapon.Halbreaker)
        {
            foreach(float z in new[]{0f,3f,6f,9f})SpawnTarget(new Vector3(0,0,z),"");
            owner.playerController.AimAt(new Vector3(0,1.7f,9));
            return;
        }
        SpawnTarget(new Vector3(0,0,-8.0f),"刀尖 / TIP");
        SpawnTarget(new Vector3(-4,0,-8.0f),"近身 / CLOSE");
        SpawnTarget(new Vector3(4,0,-8.0f),"距离 / RANGE");
        owner.playerController.AimAt(new Vector3(0,1.4f,-8));
    }
    private void OnDestroy(){ClearTargets();if(canvas!=null)Destroy(canvas.gameObject);}
}
