using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using Object=UnityEngine.Object;

// Original project-authored lunar environment. Tactical scale follows this
// game's 5 m dash and physical blade; no reference-game geometry is imported.
public static partial class LunarBasinBuild
{
    const string Folder="Assets/Art/LunarBasinR1";
    static Transform root;
    static int sectorIndex, meshSerial;
    static readonly List<NavMeshBuildSource> sources=new List<NavMeshBuildSource>();
    static readonly List<Mesh> rockMeshes=new List<Mesh>();
    static Material dust, rock, darkRock, metal, ivory, graphite, copper, signal, blue, black, paint;
    static System.Random random;

    public static void Build()
    {
        var scene=EditorSceneManager.OpenScene("Assets/Scenes/Demo_Main.unity");
        var sector=Object.FindFirstObjectByType<ArenaSector>();
        if(sector==null)throw new Exception("Missing arena owner");
        Directory.CreateDirectory(Folder+"/Meshes");Directory.CreateDirectory(Folder+"/Textures");AssetDatabase.Refresh();
        MakeMaterials();meshSerial=0;rockMeshes.Clear();
        for(int i=0;i<8;i++)rockMeshes.Add(SaveMesh(RockMesh(903+i),"Basalt_"+i));
        for(sectorIndex=1;sectorIndex<=2;sectorIndex++)
        {
            var previous=sectorIndex==1?sector.tacticalMaintenance:sector.tacticalReactor;
            // Retain the old room as a disabled scene root, without enabling it
            // through the new active references or deleting its source assets.
            if(previous!=null)
            {
                if(previous.GetComponent<LunarArenaLayout>()!=null)Object.DestroyImmediate(previous);
                else {previous.SetActive(false);previous.name+="_Retained";previous.transform.SetParent(null,true);}
            }
            var prior=scene.GetRootGameObjects().FirstOrDefault(g=>g.name=="LunarBasinGenerated_"+sectorIndex);
            if(prior!=null)Object.DestroyImmediate(prior);
            root=new GameObject("LunarBasinGenerated_"+sectorIndex).transform;
            root.SetParent(sector.transform,false);sources.Clear();random=new System.Random(92300+sectorIndex);
            var layout=root.gameObject.AddComponent<LunarArenaLayout>();
            layout.roomName=sectorIndex==1?"SELENE-07 / EXCAVATION BASIN":"SELENE-07 / RELAY CALDERA";
            layout.playerEntry=new Vector3(0,.15f,-9);
            layout.entries=new[]{new Vector3(-16,0,3),new Vector3(0,0,17),new Vector3(16,0,3),new Vector3(0,0,-16)};
            layout.landmarks=new[]{new Vector3(0,0,-12),new Vector3(0,0,18),new Vector3(-15,0,-15),new Vector3(15,0,-15),new Vector3(-15,0,18),new Vector3(15,0,18),new Vector3(-12,0,3),new Vector3(12,0,3),new Vector3(0,0,0)};
            layout.coverNear=sectorIndex==1?new Vector3(-3,0,4):new Vector3(-3,0,-3);
            layout.coverFar=sectorIndex==1?new Vector3(-12,0,4):new Vector3(-13,0,-3);
            Terrain();Perimeter();
            if(sectorIndex==1)Excavation();else Relay();
            EntryAprons(layout);SurfaceDetails();Backdrop();
            var settings=NavMesh.GetSettingsByIndex(0);settings.agentRadius=.84f;settings.agentHeight=3.4f;
            settings.agentClimb=.45f;settings.agentSlope=45;settings.overrideVoxelSize=true;settings.voxelSize=.1f;
            var data=NavMeshBuilder.BuildNavMeshData(settings,sources,new Bounds(Vector3.zero,new Vector3(55,18,55)),Vector3.zero,Quaternion.identity);
            if(data==null)throw new Exception("Lunar navigation failed");
            string navPath=Folder+"/Sector"+sectorIndex+"Navigation.asset";
            var saved=AssetDatabase.LoadAssetAtPath<NavMeshData>(navPath);
            if(saved!=null){EditorUtility.CopySerialized(data,saved);Object.DestroyImmediate(data);data=saved;EditorUtility.SetDirty(saved);}
            else AssetDatabase.CreateAsset(data,navPath);
            CombineEnvironment();
            if(sectorIndex==1){sector.tacticalMaintenance=root.gameObject;sector.tacticalMaintenanceNavigation=data;}
            else{sector.tacticalReactor=root.gameObject;sector.tacticalReactorNavigation=data;}
            root.gameObject.SetActive(false);
        }
        sector.ShowSector(1);EditorUtility.SetDirty(sector);
        var light=Object.FindObjectsByType<Light>(FindObjectsSortMode.None).FirstOrDefault(l=>l.type==LightType.Directional);
        if(light!=null)
        {light.transform.rotation=Quaternion.Euler(47,-38,0);light.intensity=1.23f;light.color=new Color(1,.98f,.93f);light.shadows=LightShadows.Soft;light.shadowStrength=.83f;light.shadowBias=.025f;}
        RenderSettings.ambientMode=AmbientMode.Trilight;
        RenderSettings.ambientSkyColor=new Color(.26f,.32f,.43f);RenderSettings.ambientEquatorColor=new Color(.18f,.22f,.30f);RenderSettings.ambientGroundColor=new Color(.10f,.12f,.16f);
        RenderSettings.fog=false;Camera.main.backgroundColor=new Color(.009f,.014f,.025f);
        QualitySettings.shadowDistance=85;QualitySettings.shadowResolution=ShadowResolution.High;
        AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(scene);
        string ev=Environment.GetEnvironmentVariable("MECH_LOOP_V2_EVIDENCE")??"AuditEvidence/lunar-basin-r1/release";
        Directory.CreateDirectory(ev);File.WriteAllText(Path.Combine(ev,"environment-build.txt"),"Two original lunar rooms; flat readable combat floor with shallow regolith relief, continuous rock boundaries, authored entries, physical landmarks, baked navigation; previous rooms retained inactive.");
        CombatLoopV2Build.Build();
    }

    static void Excavation()
    {
        // Three asymmetric islands leave a central exchange and two continuous
        // flank loops. No mandatory stairs, narrow doorway or raised fighting deck.
        Extractor(new Vector3(-7.8f,0,4),2.4f);
        CraterIsland(new Vector3(8.5f,0,-3.8f),new Vector2(3.4f,2.9f));
        Ridge(new Vector3(7.4f,0,10.3f),new Vector3(5,2.35f,3.2f),-16);
        Cargo(new Vector3(-11.5f,0,-9),-12);
        SolarArray(new Vector3(-13.5f,0,13.2f),new Vector2(6,3.3f),-14,true);
        Cable(new[]{new Vector3(-8,0,5),new Vector3(-11,0,8),new Vector3(-14,0,9),new Vector3(-14,0,12)},.09f,copper);
        Cable(new[]{new Vector3(-8,0,3),new Vector3(-11,0,0),new Vector3(-14,0,-4),new Vector3(-14,0,-8)},.06f,graphite);
    }
    static void Relay()
    {
        Ridge(new Vector3(-8.5f,0,-3),new Vector3(5.4f,2.4f,4),22);
        CraterIsland(new Vector3(9,0,5.8f),new Vector2(3.2f,3.4f));
        RelayDish(new Vector3(-9.5f,0,10.8f));
        Cargo(new Vector3(12,0,-10.3f),15);
        SolarArray(new Vector3(14,0,14.8f),new Vector2(4.7f,3),12,true);
        Cable(new[]{new Vector3(-10,0,10),new Vector3(-13,0,7),new Vector3(-14,0,2),new Vector3(-13,0,-4)},.1f,copper);
    }
    static void Perimeter()
    {
        Vector3[] rim={new Vector3(-24,0,-13),new Vector3(-17,0,-23),new Vector3(12,0,-23),new Vector3(24,0,-14),new Vector3(24,0,14),new Vector3(14,0,24),new Vector3(-13,0,24),new Vector3(-24,0,13)};
        for(int i=0;i<rim.Length;i++)
        {
            Vector3 from=rim[i],to=rim[(i+1)%rim.Length],delta=to-from;
            float length=delta.magnitude;float height=(from.z+to.z)<-15?1.65f:3.5f;
            float yaw=Mathf.Atan2(delta.x,delta.z)*Mathf.Rad2Deg;
            // The visible faceted escarpment is also the continuous collider;
            // no exposed box or invisible gaps separate art from the boundary.
            RockBoundary((from+to)*.5f,length+1,height,yaw,i);
            int count=Mathf.CeilToInt(length/2.8f);
            for(int n=0;n<=count;n++)
            {
                Vector3 p=Vector3.Lerp(from,to,n/(float)count);p+=p.normalized*(.25f+Rand()*.7f);
                float h=height*(.72f+Rand()*.55f);
                Boulder(p+Vector3.up*h*.36f,new Vector3(3.5f+Rand(),h,3.1f+Rand()),false,rock);
            }
        }
    }
    static void EntryAprons(LunarArenaLayout layout)
    {
        for(int i=0;i<layout.entries.Length;i++)
        {
            Vector3 pos=layout.entries[i],toward=-pos.normalized,side=Vector3.Cross(Vector3.up,toward);
            float yaw=Mathf.Atan2(toward.x,toward.z)*Mathf.Rad2Deg;
            for(int n=-1;n<=1;n++)
                Box("ArrivalTread",pos+side*n*1.3f+Vector3.up*.04f,new Vector3(1.15f,.08f,2.6f),metal,false,yaw);
            for(int sign=-1;sign<=1;sign+=2)
            {
                Vector3 lightPos=pos+side*sign*3.7f-toward*1.7f;
                Cylinder("EntryBeacon",lightPos+Vector3.up*.45f,.14f,.9f,graphite);
                Cylinder("EntryLight",lightPos+Vector3.up*.93f,.18f,.09f,signal);
            }
        }
        // A dust-covered loading apron is a landmark, not a full metal floor.
        for(int i=-2;i<=2;i++)Box("ArrivalSlab",new Vector3(i*1.22f,.024f,-11),new Vector3(1.17f,.045f,3.3f),metal);
        Label("SELENE / 07",new Vector3(-2.5f,.055f,-10.5f),.057f,paint);
    }
    static void SurfaceDetails()
    {
        // Tire tracks and buried plates guide the eye without adding collisions.
        for(int line=-1;line<=1;line+=2)
        for(int n=0;n<28;n++)
        {
            float z=-17+n*1.3f;float x=line*(2.7f+.35f*Mathf.Sin(z*.24f));
            Box("PressedRegolithTrack",new Vector3(x,.027f,z),new Vector3(.17f,.014f,.62f),darkRock,false,-7*Mathf.Cos(z*.24f));
        }
        for(int n=0;n<95;n++)
        {
            float angle=Rand()*Mathf.PI*2,range=16+Rand()*5.5f;
            Vector3 p=new Vector3(Mathf.Cos(angle)*range,0,Mathf.Sin(angle)*range);
            if(Mathf.Abs(p.x)<5||Mathf.Abs(p.z)<2.5f)continue;
            float scale=.14f+Rand()*.48f;p.y=Height(p.x,p.z)+scale*.16f;
            Boulder(p,new Vector3(scale,scale*.38f,scale*.7f),false,rock);
        }
    }
    static void Backdrop()
    {
        for(int n=0;n<65;n++)
        {
            float angle=Rand()*Mathf.PI*2,range=29+Rand()*15;
            Vector3 p=new Vector3(Mathf.Cos(angle)*range,0,Mathf.Sin(angle)*range);
            float s=1.1f+Rand()*5.5f;p.y=Height(p.x,p.z)+s*.23f;
            Boulder(p,new Vector3(s,s*.5f,s*.8f),false,n%3==0?darkRock:rock);
        }
        Habitat(new Vector3(-31,0,5),12);Habitat(new Vector3(30,0,13),-15);
        SolarArray(new Vector3(31,0,-10),new Vector2(9,5),-25,false);
        SolarArray(new Vector3(-29,0,-13),new Vector2(7,4),25,false);
        RelayDish(new Vector3(0,0,29));
    }
    static void CombineEnvironment()
    {
        var groups=new Dictionary<Material,List<MeshFilter>>();
        foreach(var mf in root.GetComponentsInChildren<MeshFilter>())
        {
            var renderer=mf.GetComponent<MeshRenderer>();
            if(renderer==null||!renderer.enabled||renderer.sharedMaterials.Length!=1||mf.sharedMesh==null)continue;
            Material mat=renderer.sharedMaterial;
            if(!groups.TryGetValue(mat,out var list))groups[mat]=list=new List<MeshFilter>();list.Add(mf);
        }
        foreach(var group in groups)
        {
            var combines=group.Value.Select(m=>new CombineInstance{mesh=m.sharedMesh,transform=root.worldToLocalMatrix*m.transform.localToWorldMatrix}).ToArray();
            var mesh=new Mesh{indexFormat=IndexFormat.UInt32};mesh.CombineMeshes(combines,true,true);mesh.RecalculateBounds();
            mesh=SaveMesh(mesh,"Sector"+sectorIndex+"_"+group.Key.name);
            var go=new GameObject("LunarBatch_"+group.Key.name);go.transform.SetParent(root,false);
            go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterial=group.Key;go.isStatic=true;
            foreach(var mf in group.Value){Object.DestroyImmediate(mf.GetComponent<MeshRenderer>());Object.DestroyImmediate(mf);}
        }
    }
    static float Rand()=> (float)random.NextDouble();
}
