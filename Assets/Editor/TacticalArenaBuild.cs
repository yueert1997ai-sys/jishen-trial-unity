using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using Object = UnityEngine.Object;

// Two authored combat spaces. The old lunar/industrial roots and their assets
// remain in the scene, disabled; this builder never rewrites their geometry.
public static class TacticalArenaBuild
{
    const string Folder = "Assets/Art/TacticalArenaR1";
    static Transform root;
    static readonly List<NavMeshBuildSource> sources = new List<NavMeshBuildSource>();
    static Material floor, seam, shell, top, dark, stripe, accent;

    public static void Build()
    {
        var scene=EditorSceneManager.OpenScene("Assets/Scenes/Demo_Main.unity");
        var sector=Object.FindFirstObjectByType<ArenaSector>();
        if(sector==null) throw new Exception("Missing arena owner");
        Directory.CreateDirectory(Folder); AssetDatabase.Refresh();
        floor=Mat("Deck",new Color(.22f,.27f,.30f),.25f);
        seam=Mat("DeckInlay",new Color(.12f,.16f,.19f),.1f);
        shell=Mat("CoverHull",new Color(.45f,.51f,.54f),.4f);
        top=Mat("CoverArmor",new Color(.62f,.64f,.60f),.25f);
        dark=Mat("Structure",new Color(.10f,.13f,.16f),.2f);
        stripe=Mat("Safety",new Color(.70f,.46f,.12f),.15f);
        for(int sectorIndex=1;sectorIndex<=2;sectorIndex++)
        {
            var old=sectorIndex==1?sector.tacticalMaintenance:sector.tacticalReactor;
            if(old!=null)Object.DestroyImmediate(old);
            accent=Mat(sectorIndex==1?"DockSignal":"ReactorSignal",
                sectorIndex==1?new Color(.15f,.57f,.62f):new Color(.67f,.37f,.17f),.1f);
            root=new GameObject(sectorIndex==1?"Tactical_Dock_R1":"Tactical_Reactor_R1").transform;
            root.SetParent(sector.transform,false);sources.Clear();
            Box("DeckFoundation",new Vector3(0,-.30f,0),new Vector3(56,.6f,56),floor,true,true);
            for(int x=-27;x<=27;x+=3)
                Box("DeckJoint",new Vector3(x,.012f,0),new Vector3(.025f,.02f,54),seam);
            for(int z=-27;z<=27;z+=3)
                Box("DeckJoint",new Vector3(0,.013f,z),new Vector3(54,.02f,.025f),seam);
            for(int side=-1;side<=1;side+=2)
            {
                Box("Perimeter",new Vector3(side*27.6f,1.1f,0),new Vector3(.6f,2.2f,56),dark,true);
                Box("Perimeter",new Vector3(0,1.1f,side*27.6f),new Vector3(56,2.2f,.6f),dark,true);
                Box("PerimeterSignal",new Vector3(side*27.22f,.7f,0),new Vector3(.10f,.15f,54),accent);
                Box("PerimeterSignal",new Vector3(0,.7f,side*27.22f),new Vector3(54,.15f,.10f),accent);
                // Open peripheral bypass connects every cover island. All tall
                // machinery stays outside the main fighting and camera lanes.
                for(int z=-18;z<=18;z+=12) Cover("ServiceBank",side*22,z,3.2f,6f,3.6f);
                for(int z=-18;z<=18;z+=3)
                {
                    Box("FlankRoute",new Vector3(side*15,.025f,z),new Vector3(.14f,.025f,1.3f),accent);
                    Box("CenterRoute",new Vector3(side*4.8f,.025f,z),new Vector3(.10f,.025f,.8f),top);
                }
            }
            if(sectorIndex==1)
            {
                Cover("WestRelay",-8.6f,2.5f,3.6f,6f,2.5f);
                Cover("EastBreaker",8,-3.5f,4.4f,3.4f,1.8f);
                Cover("EastCooling",8.6f,8.2f,4f,5.8f,2.5f);
                Cover("SouthFreight",-12,-10,4f,3f,1.8f);
                Cover("NorthFreight",-8.2f,13,4.2f,3f,1.8f);
                Cover("OuterRelay",15,0,2.8f,5f,2.5f);
            }
            else
            {
                Cover("WestCapacitor",-8.6f,-4.5f,4f,5.2f,2.5f);
                Cover("EastCapacitor",8.6f,5.5f,4f,6.2f,2.5f);
                Cover("WestShield",-9,9.5f,4.8f,3.2f,1.8f);
                Cover("EastShield",11,-10,4.8f,3.2f,1.8f);
                Cover("WestOuter",-16,1.5f,2.8f,5f,2.5f);
                Cover("EastOuter",16,14,2.8f,4f,2.5f);
            }
            for(int i=0;i<4;i++)
            {
                Vector3 pos=Quaternion.Euler(0,i*90,0)*new Vector3(0,0,18);
                Box("EntryApron",pos+Vector3.up*.024f,new Vector3(i%2==0?6:2,.028f,i%2==0?2:6),seam);
                for(int n=-2;n<=2;n++)
                {
                    Vector3 p=pos+(i%2==0?Vector3.right:Vector3.forward)*n*.85f;
                    Box("EntryStripe",p+Vector3.up*.043f,new Vector3(.4f,.018f,.4f),stripe);
                }
            }
            Box("Deployment",new Vector3(0,.02f,-12),new Vector3(4,.025f,2.5f),seam);
            Label(sectorIndex==1?"07 / RELAY DOCK":"02 / REACTOR",new Vector3(-3,.055f,-9),.055f);
            Label("FLANK",new Vector3(-16.5f,.055f,-6.5f),.05f);
            Label("FLANK",new Vector3(13,.055f,12),.05f);
            var settings=NavMesh.GetSettingsByIndex(0);
            settings.agentRadius=.8f;settings.agentHeight=3.4f;settings.agentClimb=.45f;
            settings.overrideVoxelSize=true;settings.voxelSize=.1f;
            var data=NavMeshBuilder.BuildNavMeshData(settings,sources,new Bounds(Vector3.zero,new Vector3(58,18,58)),Vector3.zero,Quaternion.identity);
            if(data==null)throw new Exception("Tactical navigation bake failed");
            string path=Folder+"/Sector"+sectorIndex+".asset";
            var existing=AssetDatabase.LoadAssetAtPath<NavMeshData>(path);
            if(existing!=null){EditorUtility.CopySerialized(data,existing);Object.DestroyImmediate(data);data=existing;EditorUtility.SetDirty(existing);}
            else AssetDatabase.CreateAsset(data,path);
            if(sectorIndex==1){sector.tacticalMaintenance=root.gameObject;sector.tacticalMaintenanceNavigation=data;}
            else{sector.tacticalReactor=root.gameObject;sector.tacticalReactorNavigation=data;}
            root.gameObject.SetActive(false);
        }
        sector.ShowSector(1);EditorUtility.SetDirty(sector);
        AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(scene);
        CombatLoopV2Build.Build();
    }

    static void Cover(string name,float x,float z,float width,float depth,float height)
    {
        Box("SolidCover_"+name,new Vector3(x,height*.5f,z),new Vector3(width,height,depth),shell,true);
        Box(name+"_Cap",new Vector3(x,height+.035f,z),new Vector3(width+.06f,.07f,depth+.06f),top);
        Box(name+"_Recess",new Vector3(x,height*.58f,z-depth*.5f-.012f),new Vector3(width*.74f,height*.28f,.025f),dark);
        Box(name+"_Signal",new Vector3(x,height*.82f,z-depth*.5f-.028f),new Vector3(width*.65f,.10f,.03f),accent);
        for(int side=-1;side<=1;side+=2)
        {
            Box(name+"_Brace",new Vector3(x+side*(width*.5f-.16f),height*.5f,z-depth*.5f-.035f),new Vector3(.22f,height,.07f),dark);
            Box(name+"_Mark",new Vector3(x+side*(width*.5f+.22f),.03f,z),new Vector3(.14f,.03f,depth+.4f),stripe);
        }
    }
    static GameObject Box(string name,Vector3 position,Vector3 size,Material material,bool solid=false,bool walkable=false)
    {
        var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(root,false);
        go.transform.localPosition=position;go.transform.localScale=size;go.GetComponent<Renderer>().sharedMaterial=material;
        go.isStatic=true;
        if(!solid)Object.DestroyImmediate(go.GetComponent<Collider>());
        else sources.Add(new NavMeshBuildSource{shape=NavMeshBuildSourceShape.Box,
            transform=Matrix4x4.TRS(position,Quaternion.identity,Vector3.one),size=size,area=walkable?0:1});
        return go;
    }
    static Material Mat(string name,Color color,float metal)
    {
        string path=Folder+"/"+name+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(mat==null){mat=new Material(Shader.Find("Standard"));AssetDatabase.CreateAsset(mat,path);}
        mat.color=color;mat.SetFloat("_Metallic",metal);mat.SetFloat("_Glossiness",.22f);EditorUtility.SetDirty(mat);return mat;
    }
    static void Label(string text,Vector3 position,float size)
    {
        var go=new GameObject("DeckLabel");go.transform.SetParent(root,false);go.transform.localPosition=position;
        go.transform.localRotation=Quaternion.Euler(90,0,0);
        var label=go.AddComponent<TextMesh>();label.text=text;label.fontSize=64;label.characterSize=size;label.color=new Color(.53f,.61f,.62f);
    }
}
