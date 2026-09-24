using System;
using UnityEngine;
using UnityEngine.Rendering;

// A bounded pool; each discharge resolves damage once, independent of the afterglow.
public sealed class ArsenalBeam : MonoBehaviour
{
    const int Capacity=8;static ArsenalBeam[] pool;static int next;static Material material;
    static Texture2D strip;
    public static Texture2D BeamStrip
    {
        get
        {
            if(strip==null)
            {
                strip=new Texture2D(2,32,TextureFormat.RGBA32,false){name="Beam cross-section",wrapMode=TextureWrapMode.Clamp};
                for(int y=0;y<32;y++)for(int x=0;x<2;x++)strip.SetPixel(x,y,new Color(1,1,1,Mathf.Pow(Mathf.Sin(y/31f*Mathf.PI),2)));
                strip.Apply(false,true);
            }
            return strip;
        }
    }
    LineRenderer[] lines;float age,life,width;Vector3 from,to;Color tint,core;
    readonly BeamContactResolver contacts=new BeamContactResolver();
    readonly System.Collections.Generic.List<Vector3> impactPoints=new System.Collections.Generic.List<Vector3>();
    public static void Fire(Vector3 origin,Vector3 direction,Damageable owner,float damage,float range,float width,float life)
    {
        if(pool==null)pool=new ArsenalBeam[Capacity];int slot=next++%Capacity;
        if(pool[slot]==null)pool[slot]=new GameObject("Arsenal beam "+slot).AddComponent<ArsenalBeam>();
        var beam=pool[slot];beam.gameObject.SetActive(true);beam.age=0;beam.life=life;beam.width=width;beam.from=origin;
        beam.tint=owner.team==1?EnergyBoltVisual.EnemyRed:MechEnergyPalette.Sheath(owner.gameObject);beam.core=owner.team==1?new Color(1,.86f,.8f):MechEnergyPalette.Core(owner.gameObject);
        direction=PlanarCombat.Direction(direction,owner.transform.forward);beam.to=origin+direction*range;
        beam.contacts.Resolve(owner.gameObject,owner,owner.team,origin,direction,range,width*.35f,0,damage,0,
            CombatHitKind.HeavyRifle,true,beam.impactPoints,out beam.to);
        if(beam.contacts.BarrelObstructed)beam.from=beam.to;
        foreach(var point in beam.impactPoints)BeamFxKit.ImpactBurst(point,direction,beam.tint,width*1.5f);
        BeamFxKit.MuzzleBlast(beam.from,direction,beam.tint,width*1.5f);beam.Display();
    }
    void Awake()
    {
        if(material==null){material=OverdriveVfx.CreateJetMaterial();material.mainTexture=BeamStrip;}lines=new LineRenderer[3];
        for(int i=0;i<3;i++){var l=new GameObject("Beam layer "+i).AddComponent<LineRenderer>();l.transform.SetParent(transform,false);l.sharedMaterial=material;l.positionCount=2;l.useWorldSpace=true;l.shadowCastingMode=ShadowCastingMode.Off;lines[i]=l;}
    }
    void Update()
    {
        var gm=GameManager.Instance;if(gm==null||gm.Phase!=GamePhase.Combat){gameObject.SetActive(false);return;}if(gm.IsPaused)return;
        age+=Time.deltaTime;if(age>=life){gameObject.SetActive(false);return;}Display();
    }
    void Display()
    {
        float fade=Mathf.Clamp01((life-age)/.22f);
        for(int i=0;i<3;i++){var l=lines[i];l.SetPosition(0,from);l.SetPosition(1,to);l.startWidth=l.endWidth=width*(i==0?2.5f:i==1?1:.30f)*fade;var c=i==2?core:tint;c.a=fade*(i==0?.25f:1);l.startColor=l.endColor=c;}
    }
}
