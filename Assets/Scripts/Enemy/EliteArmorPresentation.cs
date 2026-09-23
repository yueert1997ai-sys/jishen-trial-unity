using UnityEngine;
using UnityEngine.Rendering;

// One bounded ground arc shows the guarded side without covering the mech silhouette.
[DisallowMultipleComponent]
public sealed class EliteArmorPresentation : MonoBehaviour
{
    private EnemyBase enemy;
    private LineRenderer arc;
    private Material material;
    private void Awake()
    {
        enemy=GetComponent<EnemyBase>();
        var marker=new GameObject("FrontArmorArc");marker.transform.SetParent(transform,false);
        arc=marker.AddComponent<LineRenderer>();arc.useWorldSpace=false;arc.positionCount=13;
        arc.widthMultiplier=.065f;arc.shadowCastingMode=ShadowCastingMode.Off;arc.receiveShadows=false;
        material=new Material(Shader.Find("Sprites/Default"));arc.sharedMaterial=material;
        for(int i=0;i<13;i++)
        {
            float angle=Mathf.Lerp(0,360,i/12f)*Mathf.Deg2Rad;
            arc.SetPosition(i,new Vector3(Mathf.Sin(angle)*1.3f,.12f,Mathf.Cos(angle)*1.3f));
        }
    }
    private void LateUpdate()
    {
        var health=GetComponent<Damageable>();
        arc.enabled=enemy!=null && enemy.UsesDirectionalArmor && health!=null && !health.IsDead;
        if(!arc.enabled)return;
        Color color=enemy.GuardActive?new Color(1,.61f,.13f,.82f):new Color(.25f,.95f,1,.35f);
        arc.startColor=arc.endColor=color;
    }
    private void OnDisable(){if(arc!=null)arc.enabled=false;}
    private void OnDestroy(){if(material!=null)Destroy(material);}
}
