using System.Collections.Generic;
using UnityEngine;
using Object=UnityEngine.Object;

public static partial class LunarBasinBuild
{
    static void Extractor(Vector3 position,float radius)
    {
        Cylinder("ExtractorFoot",position+Vector3.up*.32f,radius,.64f,graphite,true);
        Cylinder("ExtractorPressureVessel",position+Vector3.up*1.48f,radius*.80f,2.3f,ivory,true);
        Cylinder("ExtractorLowerBand",position+Vector3.up*.63f,radius*.84f,.24f,metal);
        Cylinder("ExtractorUpperBand",position+Vector3.up*2.51f,radius*.84f,.22f,metal);
        Cylinder("ExtractorTopPlate",position+Vector3.up*2.67f,radius*.7f,.15f,graphite);
        Cylinder("ExtractorCore",position+Vector3.up*2.85f,radius*.31f,.38f,metal);
        Cylinder("ExtractorCoreWindow",position+Vector3.up*3.065f,radius*.22f,.06f,blue);
        for(int i=0;i<12;i++)
        {
            float angle=i*30;Vector3 outward=Quaternion.Euler(0,angle,0)*Vector3.forward;
            Box("ExtractorRib",position+outward*(radius*.79f)+Vector3.up*1.5f,new Vector3(.16f,1.74f,.22f),graphite,false,angle);
            Box("ExtractorVent",position+outward*(radius*.815f)+Vector3.up*1.83f,new Vector3(.35f,.47f,.06f),metal,false,angle);
            Box("ExtractorWarning",position+outward*(radius*.835f)+Vector3.up*.83f,new Vector3(.37f,.12f,.065f),paint,false,angle);
            if(i%3==0)Box("ExtractorStatus",position+outward*(radius*.84f)+Vector3.up*2.24f,new Vector3(.31f,.12f,.07f),signal,false,angle);
            Cylinder("PressureBolt",position+outward*radius*.65f+Vector3.up*2.82f,.075f,.18f,metal);
        }
        for(int i=0;i<3;i++)
        {
            float a=i*120+25;Vector3 side=Quaternion.Euler(0,a,0)*Vector3.forward;
            Box("ExtractorSupport",position+side*radius*.94f+Vector3.up*.55f,new Vector3(.62f,1.1f,.78f),metal,true,a);
            Box("ExtractorFootArmor",position+side*(radius+ .15f)+Vector3.up*.15f,new Vector3(.85f,.3f,.95f),graphite,false,a);
        }
        Label("He-3 / 07",position+new Vector3(-1,2.78f,.3f),.054f,paint);
    }
    static void Cargo(Vector3 pos,float yaw)
    {
        Quaternion rot=Quaternion.Euler(0,yaw,0);
        for(int i=0;i<2;i++)
        {
            Vector3 p=pos+rot*new Vector3((i-.5f)*1.7f,0,0);
            Box("CargoBody",p+Vector3.up*.92f,new Vector3(1.53f,1.84f,2.8f),ivory,true,yaw);
            Box("CargoTop",p+Vector3.up*1.89f,new Vector3(1.42f,.12f,2.7f),metal,false,yaw);
            for(int side=-1;side<=1;side+=2)
            {
                Box("CargoRib",p+rot*new Vector3(side*.65f,.9f,0),new Vector3(.18f,1.9f,2.9f),graphite,false,yaw);
                Box("CargoLatch",p+rot*new Vector3(0,.7f,side*1.43f),new Vector3(.5f,.33f,.07f),graphite,false,yaw);
                Box("CargoTag",p+rot*new Vector3(.24f,1.23f,side*1.44f),new Vector3(.54f,.13f,.08f),paint,false,yaw);
            }
            for(int n=-2;n<=2;n++)Box("CargoTopRib",p+rot*new Vector3(0,1.985f,n*.45f),new Vector3(1.37f,.06f,.065f),graphite,false,yaw);
        }
    }
    static void SolarArray(Vector3 position,Vector2 size,float yaw,bool solid)
    {
        float ground=solid?0:Height(position.x,position.z);position.y=ground;
        Quaternion rot=Quaternion.Euler(0,yaw,0);
        Box("SolarPlinth",position+Vector3.up*.52f,new Vector3(size.x+.3f,1.04f,size.y+.3f),graphite,solid,yaw);
        Box("SolarFrame",position+Vector3.up*1.08f,new Vector3(size.x+.15f,.17f,size.y+.18f),metal,false,yaw);
        int columns=Mathf.CeilToInt(size.x/1.2f);const int rows=3;
        for(int x=0;x<columns;x++)for(int z=0;z<rows;z++)
        {
            Vector3 local=new Vector3((x-(columns-1)*.5f)*size.x/columns,1.21f,(z-1)*size.y/rows);
            Vector3 p=position+rot*local;
            Box("PhotovoltaicCell",p,new Vector3(size.x/columns-.08f,.06f,size.y/rows-.06f),blue,false,yaw);
            for(int n=-1;n<=1;n++)
                Box("CellBusbar",p+rot*new Vector3(n*size.x/columns*.23f,.035f,0),new Vector3(.018f,.01f,size.y/rows-.07f),metal,false,yaw);
        }
        for(int side=-1;side<=1;side+=2)
            Box("SolarSafetyEdge",position+rot*new Vector3(side*(size.x*.5f+.03f),1.20f,0),new Vector3(.08f,.12f,size.y),paint,false,yaw);
    }
    static void RelayDish(Vector3 pos)
    {
        if(Mathf.Abs(pos.z)>25||Mathf.Abs(pos.x)>25)pos.y=Height(pos.x,pos.z);
        bool solid=Mathf.Abs(pos.x)<23&&Mathf.Abs(pos.z)<23;
        Cylinder("RelayFoundation",pos+Vector3.up*.3f,1.95f,.6f,graphite,solid);
        Cylinder("RelayPedestal",pos+Vector3.up*1.32f,1.2f,2.05f,ivory,solid);
        Cylinder("RelayNeck",pos+Vector3.up*2.42f,.52f,.65f,metal);
        for(int side=-1;side<=1;side+=2)
        {
            Box("RelayBrace",pos+new Vector3(side*.95f,1.1f,0),new Vector3(.28f,1.8f,1.6f),graphite);
            Box("RelayStatus",pos+new Vector3(side*1.205f,1.45f,0),new Vector3(.05f,.19f,.43f),signal);
        }
        var vertices=new List<Vector3>();var uv=new List<Vector2>();var tris=new List<int>();const int radial=7,around=32;
        for(int ring=0;ring<=radial;ring++)for(int i=0;i<=around;i++)
        {
            float radius=ring/(float)radial*2.35f,angle=i*Mathf.PI*2/around;
            vertices.Add(new Vector3(Mathf.Cos(angle)*radius,radius*radius*.19f,Mathf.Sin(angle)*radius));
            uv.Add(new Vector2(.5f+Mathf.Cos(angle)*radius*.2f,.5f+Mathf.Sin(angle)*radius*.2f));
        }
        for(int ring=0;ring<radial;ring++)for(int i=0;i<around;i++)
        {int a=ring*(around+1)+i,b=a+1,c=a+around+1,d=c+1;tris.AddRange(new[]{a,b,c,b,d,c});}
        var mesh=new Mesh();mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetTriangles(tris,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
        var dish=MeshObject("UplinkDish",SaveMesh(mesh,"Dish_"+sectorIndex+"_"+(meshSerial++)),pos+Vector3.up*2.7f,Vector3.one,ivory);
        dish.transform.localRotation=Quaternion.Euler(-15,24,0);
        Cylinder("DishFeed",pos+Vector3.up*3.85f,.1f,1.8f,graphite);
        Cylinder("FeedReceiver",pos+Vector3.up*4.8f,.22f,.3f,copper);
    }
    static void Habitat(Vector3 pos,float yaw)
    {
        pos.y=Height(pos.x,pos.z);Quaternion rot=Quaternion.Euler(0,yaw,0);
        Box("HabitatFoundation",pos+Vector3.up*.4f,new Vector3(7.7f,.8f,5.9f),graphite,false,yaw);
        Box("HabitatHull",pos+Vector3.up*2.25f,new Vector3(6.8f,3.5f,5),ivory,false,yaw);
        Box("HabitatRoof",pos+Vector3.up*4.04f,new Vector3(7.1f,.22f,5.3f),metal,false,yaw);
        for(int x=-2;x<=2;x++)
        {
            Vector3 pane=pos+rot*new Vector3(x*1.1f,2.55f,-2.54f);
            Box("HabitatWindow",pane,new Vector3(.8f,.75f,.08f),blue,false,yaw);
            Box("HabitatFrame",pos+rot*new Vector3(x*1.2f,2.25f,-2.58f),new Vector3(.12f,3.35f,.12f),graphite,false,yaw);
        }
        Box("Airlock",pos+rot*new Vector3(0,1.28f,-3.15f),new Vector3(1.9f,2.56f,1.1f),graphite,false,yaw);
        Box("AirlockInner",pos+rot*new Vector3(0,1.28f,-3.73f),new Vector3(1.4f,2.2f,.09f),metal,false,yaw);
        Box("AirlockLamp",pos+rot*new Vector3(0,2.65f,-3.74f),new Vector3(1.3f,.13f,.1f),signal,false,yaw);
        for(int n=-1;n<=1;n++)Cylinder("HabitatVent",pos+rot*new Vector3(n*1.7f,4.33f,.6f),.45f,.38f,graphite);
    }
}
