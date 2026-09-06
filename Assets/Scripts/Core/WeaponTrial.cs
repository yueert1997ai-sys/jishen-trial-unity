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
        var hint=RuntimeUIFactory.MenuText(safe,"TrialHint","",17,new Vector2(245,-45),new Vector2(450,74));
        hint.text=EquipmentWarehouseUI.T("武器试场 · 靶机不会反击\nQ / 右键连按三刀 · 左键收刀射击 · R 重置", "WEAPON RANGE · Passive targets\nTap Q / RMB for a 3-hit combo · LMB fire · R reset");
        hint.color=new Color(.05f,.10f,.15f);
        RuntimeUIFactory.MenuButton(safe,"ResetTrial","重置靶机  [R]",new Vector2(115,-113),new Vector2(210,46)).onClick.AddListener(ResetTargets);
        RuntimeUIFactory.MenuButton(safe,"LeaveTrial","返回机库",new Vector2(335,-113),new Vector2(195,46)).onClick.AddListener(()=>owner.EndWeaponTrial());
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
        SpawnTarget(new Vector3(0,0,-8.0f),"刀尖 / TIP");
        SpawnTarget(new Vector3(-4,0,-8.0f),"近身 / CLOSE");
        SpawnTarget(new Vector3(4,0,-8.0f),"距离 / RANGE");
        owner.playerController.AimAt(new Vector3(0,1.4f,-8));
    }
    private void OnDestroy(){ClearTargets();if(canvas!=null)Destroy(canvas.gameObject);}
}
