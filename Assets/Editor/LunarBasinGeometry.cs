using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using Object=UnityEngine.Object;

public static partial class LunarBasinBuild
{
    static void MakeMaterials()
    {
        var texture=MakeRegolithTexture();
        dust=Mat("Regolith",new Color(.52f,.50f,.46f),0,.05f);dust.mainTexture=texture;dust.mainTextureScale=new Vector2(1,1);
        rock=Mat("Basalt",new Color(.36f,.38f,.40f),.05f,.12f);rock.mainTexture=texture;
        darkRock=Mat("CompactedDust",new Color(.32f,.32f,.32f),0,.04f);darkRock.mainTexture=texture;
        metal=Mat("WornTitanium",new Color(.31f,.34f,.37f),.55f,.35f);
        ivory=Mat("CeramicArmor",new Color(.68f,.68f,.62f),.25f,.28f);
        graphite=Mat("Machinery",new Color(.12f,.15f,.19f),.45f,.28f);
        copper=Mat("InsulatedCopper",new Color(.53f,.28f,.11f),.35f,.3f);
        blue=Mat("SolarCells",new Color(.065f,.14f,.23f),.45f,.5f);
        black=Mat("Shadow",new Color(.048f,.052f,.058f),0,.02f);
        paint=Mat("FadedSafety",new Color(.62f,.46f,.24f),0,.1f);
        signal=Mat("GuidanceLight",new Color(.18f,.69f,.73f),.1f,.3f);
        signal.EnableKeyword("_EMISSION");signal.SetColor("_EmissionColor",new Color(.07f,.62f,.69f)*1.4f);EditorUtility.SetDirty(signal);
    }
    static Material Mat(string name,Color color,float metallic,float gloss)
    {
        string path=Folder+"/"+name+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(mat==null){mat=new Material(Shader.Find("Standard"));AssetDatabase.CreateAsset(mat,path);}
        mat.name=name;mat.color=color;mat.SetFloat("_Metallic",metallic);mat.SetFloat("_Glossiness",gloss);EditorUtility.SetDirty(mat);return mat;
    }
    static Texture2D MakeRegolithTexture()
    {
        const int size=512;var tex=new Texture2D(size,size,TextureFormat.RGB24,false);
        var colors=new Color[size*size];
        for(int y=0;y<size;y++)for(int x=0;x<size;x++)
        {
            float u=x/(float)size,v=y/(float)size;
            // Periodic cosine noise avoids visible tile seams. This is an
            // original material texture, independent of reference-game art.
            float grain=Hash(x,y)*.13f;
            float n=.76f+grain;
            for(int octave=0;octave<5;octave++)
            {
                float frequency=1<<octave;
                n+=Mathf.Sin((u*frequency+v*(frequency+1))*Mathf.PI*2+.71f*octave)
                    *Mathf.Cos((v*frequency-u*(frequency+2))*Mathf.PI*2+octave)*(.085f/(1+octave));
            }
            colors[y*size+x]=new Color(n,n*.98f,n*.95f,1);
        }
        tex.SetPixels(colors);tex.Apply();string path=Folder+"/Textures/Regolith.png";
        File.WriteAllBytes(path,tex.EncodeToPNG());Object.DestroyImmediate(tex);AssetDatabase.ImportAsset(path);
        var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.wrapMode=TextureWrapMode.Repeat;importer.anisoLevel=4;
        importer.textureCompression=TextureImporterCompression.CompressedHQ;importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }
    static float Hash(int x,int y)
    {unchecked{uint h=(uint)(x*374761393+y*668265263);h=(h^(h>>13))*1274126177;return(h&65535)/65535f;}}
    static float Height(float x,float z)
    {
        float radius=Mathf.Sqrt(x*x*.86f+z*z);
        float outer=Mathf.SmoothStep(0,1,Mathf.InverseLerp(23,33,radius));
        float h=(Mathf.PerlinNoise(x*.092f+41,z*.092f+87)*4.4f+Mathf.PerlinNoise(x*.21f+11,z*.21f+19)*1.5f-.9f)*outer;
        // A smooth compressed regolith floor retains just shallow relief in the
        // fight space. Tall/steep relief is outside the authored room boundary.
        h+=(Mathf.PerlinNoise(x*.64f+9,z*.64f+28)-.5f)*.075f*(1-outer);
        h+=CraterHeight(x,z,-33,-4,8.5f,5.5f,2.7f);
        h+=CraterHeight(x,z,32,2,7.4f,8.2f,3.5f);
        h+=CraterHeight(x,z,5,35,10,7.4f,3.8f);
        h+=CraterHeight(x,z,-4,-31,6.5f,4.9f,1.9f);
        for(int n=0;n<17;n++)
        {
            float a=n*2.399963f;float r=8+(n%5)*2.5f;
            h+=CraterHeight(x,z,Mathf.Cos(a)*r,Mathf.Sin(a)*r,.5f+(n%3)*.23f,.46f+(n%4)*.14f,.085f);
        }
        return h;
    }
    static float CraterHeight(float x,float z,float cx,float cz,float rx,float rz,float depth)
    {
        float dx=(x-cx)/rx,dz=(z-cz)/rz;float r=Mathf.Sqrt(dx*dx+dz*dz);
        if(r>1.4f)return 0;
        float bowl=-depth*Mathf.Pow(Mathf.Max(0,1-r*r),.8f);
        float rim=depth*.22f*Mathf.Exp(-Mathf.Pow((r-1)*7,2));
        return bowl+rim;
    }
    static void Terrain()
    {
        const int n=192;const float extent=58;
        var vertices=new Vector3[(n+1)*(n+1)];var uv=new Vector2[vertices.Length];var triangles=new int[n*n*6];
        for(int z=0;z<=n;z++)for(int x=0;x<=n;x++)
        {
            int at=z*(n+1)+x;float px=Mathf.Lerp(-extent,extent,x/(float)n),pz=Mathf.Lerp(-extent,extent,z/(float)n);
            vertices[at]=new Vector3(px,Height(px,pz),pz);uv[at]=new Vector2(px,pz)*.28f;
        }
        int t=0;for(int z=0;z<n;z++)for(int x=0;x<n;x++)
        {
            int a=z*(n+1)+x,b=a+1,c=a+n+1,d=c+1;
            triangles[t++]=a;triangles[t++]=c;triangles[t++]=b;triangles[t++]=b;triangles[t++]=c;triangles[t++]=d;
        }
        var mesh=new Mesh{name="RegolithSurface",indexFormat=IndexFormat.UInt32};mesh.vertices=vertices;mesh.uv=uv;mesh.triangles=triangles;mesh.RecalculateNormals();mesh.RecalculateBounds();
        var go=MeshObject("LunarRegolith",SaveMesh(mesh,"Regolith_"+sectorIndex),Vector3.zero,Vector3.one,dust);
        go.AddComponent<MeshCollider>().sharedMesh=mesh;
        // Only the combat floor is walkable. The high-detail exterior is scenery;
        // one closed rock boundary prevents the player reaching navigation islands.
        sources.Add(new NavMeshBuildSource{shape=NavMeshBuildSourceShape.Box,transform=Matrix4x4.TRS(new Vector3(0,-.16f,0),Quaternion.identity,Vector3.one),size=new Vector3(49,.30f,49),area=0});
    }
    static Mesh RockMesh(int seed)
    {
        var rng=new System.Random(seed);const int sides=9,levels=4;
        var vertices=new List<Vector3>();var triangles=new List<int>();var uv=new List<Vector2>();
        float phase=(float)rng.NextDouble()*6;
        for(int level=0;level<levels;level++)for(int i=0;i<sides;i++)
        {
            float angle=i*Mathf.PI*2/sides+phase;
            float radius=(level==0?.76f:level==1?1:level==2?.72f:.36f)*(.83f+(float)rng.NextDouble()*.32f)*.5f;
            float y=level/(float)(levels-1)-.40f;if(level>0)y+=((float)rng.NextDouble()-.5f)*.16f;
            vertices.Add(new Vector3(Mathf.Cos(angle)*radius,y,Mathf.Sin(angle)*radius));uv.Add(new Vector2(i/(float)sides,level/(float)levels));
        }
        for(int l=0;l<levels-1;l++)for(int i=0;i<sides;i++)
        {
            int a=l*sides+i,b=l*sides+(i+1)%sides,c=a+sides,d=b+sides;
            triangles.AddRange(new[]{a,c,b,b,c,d});
        }
        int topIndex=vertices.Count;vertices.Add(new Vector3(0,.65f,0));uv.Add(Vector2.one*.5f);
        for(int i=0;i<sides;i++)triangles.AddRange(new[]{(levels-1)*sides+i,topIndex,(levels-1)*sides+(i+1)%sides});
        int bottom=vertices.Count;vertices.Add(new Vector3(0,-.4f,0));uv.Add(Vector2.zero);
        for(int i=0;i<sides;i++)triangles.AddRange(new[]{i,(i+1)%sides,bottom});
        // Split faces give the basalt authored facets and clean hard ridges.
        var split=new Vector3[triangles.Count];var splitUv=new Vector2[split.Length];var ids=new int[split.Length];
        for(int i=0;i<split.Length;i++){split[i]=vertices[triangles[i]];splitUv[i]=uv[triangles[i]];ids[i]=i;}
        var mesh=new Mesh();mesh.vertices=split;mesh.uv=splitUv;mesh.triangles=ids;mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
    }
    static Mesh SaveMesh(Mesh mesh,string name)
    {
        mesh.name=name;string path=Folder+"/Meshes/"+name+".asset";
        var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if(existing!=null){EditorUtility.CopySerialized(mesh,existing);Object.DestroyImmediate(mesh);EditorUtility.SetDirty(existing);return existing;}
        AssetDatabase.CreateAsset(mesh,path);return mesh;
    }
    static GameObject MeshObject(string name,Mesh mesh,Vector3 pos,Vector3 size,Material mat,float yaw=0)
    {
        var go=new GameObject(name);go.transform.SetParent(root,false);go.transform.localPosition=pos;
        go.transform.localRotation=Quaternion.Euler(0,yaw,0);go.transform.localScale=size;
        go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterial=mat;go.isStatic=true;return go;
    }
    static GameObject Box(string name,Vector3 pos,Vector3 size,Material mat,bool solid=false,float yaw=0)
    {
        var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(root,false);
        go.transform.localPosition=pos;go.transform.localRotation=Quaternion.Euler(0,yaw,0);go.transform.localScale=size;
        go.GetComponent<Renderer>().sharedMaterial=mat;go.isStatic=true;
        if(!solid)Object.DestroyImmediate(go.GetComponent<Collider>());
        else sources.Add(new NavMeshBuildSource{shape=NavMeshBuildSourceShape.Box,transform=Matrix4x4.TRS(pos,Quaternion.Euler(0,yaw,0),Vector3.one),size=size,area=1});
        return go;
    }
    static GameObject Cylinder(string name,Vector3 pos,float radius,float height,Material mat,bool solid=false)
    {
        var go=GameObject.CreatePrimitive(PrimitiveType.Cylinder);go.name=name;go.transform.SetParent(root,false);
        go.transform.localPosition=pos;go.transform.localScale=new Vector3(radius*2,height*.5f,radius*2);go.GetComponent<Renderer>().sharedMaterial=mat;go.isStatic=true;
        Object.DestroyImmediate(go.GetComponent<Collider>());
        if(solid)
        {
            var col=go.AddComponent<MeshCollider>();col.sharedMesh=go.GetComponent<MeshFilter>().sharedMesh;col.convex=true;
            sources.Add(new NavMeshBuildSource{shape=NavMeshBuildSourceShape.Mesh,sourceObject=col.sharedMesh,transform=go.transform.localToWorldMatrix,area=1});
        }
        return go;
    }
    static void Boulder(Vector3 position,Vector3 scale,bool solid,Material material)
    {
        var mesh=rockMeshes[random.Next(rockMeshes.Count)];var go=MeshObject("Basalt",mesh,position,scale,material,Rand()*360);
        if(solid)
        {
            var collider=go.AddComponent<MeshCollider>();collider.sharedMesh=mesh;collider.convex=true;
            sources.Add(new NavMeshBuildSource{shape=NavMeshBuildSourceShape.Mesh,sourceObject=mesh,transform=go.transform.localToWorldMatrix,area=1});
        }
    }
    static void Ridge(Vector3 pos,Vector3 size,float yaw)
    {
        Box("RockCore",pos+Vector3.up*size.y*.42f,new Vector3(size.x*.72f,size.y*.84f,size.z*.72f),rock,true,yaw);
        for(int i=0;i<4;i++)
        {
            Vector3 off=Quaternion.Euler(0,yaw,0)*new Vector3((i-1.5f)*size.x*.18f,0,(i%2==0?-.14f:.14f)*size.z);
            Boulder(pos+off+Vector3.up*size.y*.3f,new Vector3(size.x*.57f,size.y*(.8f+Rand()*.3f),size.z),true,i%2==0?rock:darkRock);
        }
    }
    static void CraterIsland(Vector3 pos,Vector2 radius)
    {
        // Raised ejecta and exposed rock seal this non-traversable crater. Its
        // visible rim exceeds projectile height, so cover and collision agree.
        Cylinder("ImpactBasin",pos+Vector3.up*.25f,Mathf.Min(radius.x,radius.y)*.85f,.5f,black);
        for(int i=0;i<10;i++)
        {
            float a=i*Mathf.PI*2/10;Vector3 offset=new Vector3(Mathf.Cos(a)*radius.x*.68f,0,Mathf.Sin(a)*radius.y*.68f);
            float h=1.65f+Rand()*.6f;
            Boulder(pos+offset+Vector3.up*h*.34f,new Vector3(radius.x*.88f,h,radius.y*.82f),true,rock);
        }
        // Continuous core is contained inside the visible rim. It prevents a
        // dodge or knockback from slipping into the decorative depression.
        Cylinder("CraterBedrock",pos+Vector3.up*.8f,Mathf.Min(radius.x,radius.y)*.68f,1.6f,darkRock,true);
    }
    static void Cable(Vector3[] points,float radius,Material mat)
    {
        for(int i=1;i<points.Length;i++)
        {
            Vector3 a=points[i-1]+Vector3.up*.095f,b=points[i]+Vector3.up*.095f,delta=b-a;
            float yaw=Mathf.Atan2(delta.x,delta.z)*Mathf.Rad2Deg;
            Box("BuriedCable",(a+b)*.5f,new Vector3(radius*2,.10f,delta.magnitude),mat,false,yaw);
        }
    }
    static void Label(string text,Vector3 pos,float size,Material unused)
    {
        var go=new GameObject("Stencil");go.transform.SetParent(root,false);go.transform.localPosition=pos;go.transform.localRotation=Quaternion.Euler(90,0,0);
        var label=go.AddComponent<TextMesh>();label.text=text;label.fontSize=64;label.characterSize=size;label.color=new Color(.63f,.61f,.51f);
    }
}
