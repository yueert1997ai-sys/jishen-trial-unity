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
        dust=Mat("Regolith",new Color(.53f,.535f,.54f),0,.05f);dust.mainTexture=texture;dust.mainTextureScale=new Vector2(1,1);
        rock=Mat("Basalt",new Color(.36f,.38f,.40f),.05f,.12f);rock.mainTexture=texture;
        darkRock=Mat("CompactedDust",new Color(.32f,.32f,.32f),0,.04f);darkRock.mainTexture=texture;
        var bump=AssetDatabase.LoadAssetAtPath<Texture2D>(Folder+"/Textures/RegolithNormal.png");
        foreach(var surface in new[]{dust,rock,darkRock})
        {surface.SetTexture("_BumpMap",bump);surface.SetFloat("_BumpScale",.38f);surface.EnableKeyword("_NORMALMAP");EditorUtility.SetDirty(surface);}
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
        var colors=new Color[size*size];var heights=new float[size*size];
        for(int y=0;y<size;y++)for(int x=0;x<size;x++)
        {
            float u=x/(float)size,v=y/(float)size;
            // Periodic hashed lattice noise gives irregular mineral grains;
            // there is no repeating directional weave or checkerboard pattern.
            float n=.63f+Noise(u,v,4)*.14f+Noise(u,v,16)*.11f+Noise(u,v,64)*.07f+Hash(x,y)*.075f;
            colors[y*size+x]=new Color(n,n,n,1);heights[y*size+x]=Noise(u,v,64)*.6f+Noise(u,v,128)*.25f+Hash(x,y)*.15f;
        }
        tex.SetPixels(colors);tex.Apply();string path=Folder+"/Textures/Regolith.png";
        File.WriteAllBytes(path,tex.EncodeToPNG());Object.DestroyImmediate(tex);AssetDatabase.ImportAsset(path);
        var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.wrapMode=TextureWrapMode.Repeat;importer.anisoLevel=4;
        importer.textureCompression=TextureImporterCompression.CompressedHQ;importer.SaveAndReimport();
        var normals=new Color[size*size];
        for(int y=0;y<size;y++)for(int x=0;x<size;x++)
        {
            float dx=heights[y*size+(x+1)%size]-heights[y*size+(x+size-1)%size];
            float dy=heights[((y+1)%size)*size+x]-heights[((y+size-1)%size)*size+x];
            Vector3 normal=new Vector3(-dx*2,-dy*2,1).normalized;
            normals[y*size+x]=new Color(normal.x*.5f+.5f,normal.y*.5f+.5f,normal.z*.5f+.5f);
        }
        var normalTex=new Texture2D(size,size,TextureFormat.RGB24,false);normalTex.SetPixels(normals);normalTex.Apply();
        string normalPath=Folder+"/Textures/RegolithNormal.png";File.WriteAllBytes(normalPath,normalTex.EncodeToPNG());Object.DestroyImmediate(normalTex);AssetDatabase.ImportAsset(normalPath);
        var normalImporter=(TextureImporter)AssetImporter.GetAtPath(normalPath);normalImporter.textureType=TextureImporterType.NormalMap;
        normalImporter.wrapMode=TextureWrapMode.Repeat;normalImporter.anisoLevel=4;normalImporter.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }
    static float Noise(float u,float v,int frequency)
    {
        float px=u*frequency,pz=v*frequency;int x=Mathf.FloorToInt(px),z=Mathf.FloorToInt(pz);
        float fx=px-x,fz=pz-z;fx=fx*fx*(3-2*fx);fz=fz*fz*(3-2*fz);
        return Mathf.Lerp(Mathf.Lerp(Hash(x%frequency,z%frequency),Hash((x+1)%frequency,z%frequency),fx),
            Mathf.Lerp(Hash(x%frequency,(z+1)%frequency),Hash((x+1)%frequency,(z+1)%frequency),fx),fz);
    }
    static float Hash(int x,int y)
    {unchecked{uint h=(uint)(x*374761393+y*668265263);h=(h^(h>>13))*1274126177;return(h&65535)/65535f;}}
    static float Height(float x,float z)
    {
        float radius=Mathf.Sqrt(x*x*.86f+z*z);
        float outer=Mathf.SmoothStep(0,1,Mathf.InverseLerp(28.5f,36,radius));
        float h=(Mathf.PerlinNoise(x*.092f+41,z*.092f+87)*4.4f+Mathf.PerlinNoise(x*.21f+11,z*.21f+19)*1.5f-.9f)*outer;
        // A smooth compressed regolith floor retains just shallow relief in the
        // fight space. Tall/steep relief is outside the authored room boundary.
        h+=(Mathf.PerlinNoise(x*.64f+9,z*.64f+28)-.5f)*.075f*(1-outer);
        h+=CraterHeight(x,z,-33,-4,8.5f,5.5f,2.7f);
        h+=CraterHeight(x,z,32,2,7.4f,8.2f,3.5f);
        h+=CraterHeight(x,z,5,35,10,7.4f,3.8f);
        h+=CraterHeight(x,z,-4,-31,6.5f,4.9f,1.9f);
        h+=sectorIndex==1?CraterHeight(x,z,8.5f,-3.8f,3.4f,2.9f,1.8f)
            :CraterHeight(x,z,9,5.8f,3.2f,3.4f,1.8f);
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
        mesh=SaveMesh(mesh,"Regolith_"+sectorIndex);
        var go=MeshObject("LunarRegolith",mesh,Vector3.zero,Vector3.one,dust);
        go.AddComponent<MeshCollider>().sharedMesh=mesh;
        // Only the combat floor is walkable. The high-detail exterior is scenery;
        // one closed rock boundary prevents the player reaching navigation islands.
        var footprint=new[]{Vector3.zero,new Vector3(-24,0,-13),new Vector3(-17,0,-23),new Vector3(12,0,-23),new Vector3(24,0,-14),new Vector3(24,0,14),new Vector3(14,0,24),new Vector3(-13,0,24),new Vector3(-24,0,13)};
        var floorTriangles=new int[24];for(int i=0;i<8;i++){floorTriangles[i*3]=0;floorTriangles[i*3+1]=(i+1)%8+1;floorTriangles[i*3+2]=i+1;}
        var footprintMesh=new Mesh();footprintMesh.vertices=footprint;footprintMesh.triangles=floorTriangles;footprintMesh.RecalculateNormals();footprintMesh.RecalculateBounds();
        footprintMesh=SaveMesh(footprintMesh,"WalkableFootprint_"+sectorIndex);
        sources.Add(new NavMeshBuildSource{shape=NavMeshBuildSourceShape.Mesh,transform=Matrix4x4.identity,sourceObject=footprintMesh,area=0});
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
        if(mesh.uv.Length==mesh.vertexCount)mesh.RecalculateTangents();
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
    static void RockBoundary(Vector3 pos,float length,float height,float yaw,int segment)
    {
        int slices=Mathf.CeilToInt(length/1.7f);
        const int profile=5;
        var vertices=new List<Vector3>();var triangles=new List<int>();
        for(int n=0;n<=slices;n++)
        {
            float z=Mathf.Lerp(-length*.5f,length*.5f,n/(float)slices);
            float h=height*(.83f+Rand()*.36f),shift=(Rand()-.5f)*.32f;
            vertices.Add(new Vector3(-1.15f-shift,-.15f,z));
            vertices.Add(new Vector3(-.95f-shift,h*(.48f+Rand()*.16f),z));
            vertices.Add(new Vector3(-.22f+shift,h,z));
            vertices.Add(new Vector3(.7f+shift,h*(.60f+Rand()*.22f),z));
            vertices.Add(new Vector3(1.4f+shift,-.15f,z));
        }
        for(int n=0;n<slices;n++)for(int j=0;j<profile;j++)
        {
            int a=n*profile+j,b=a+profile,c=n*profile+(j+1)%profile,d=c+profile;
            triangles.AddRange(new[]{a,b,c,c,b,d});
        }
        for(int j=1;j<profile-1;j++)
        {
            triangles.AddRange(new[]{0,j,j+1});
            int end=slices*profile;triangles.AddRange(new[]{end,end+j+1,end+j});
        }
        var split=new Vector3[triangles.Count];var uv=new Vector2[split.Length];var ids=new int[split.Length];
        for(int n=0;n<split.Length;n++)
        {split[n]=vertices[triangles[n]];uv[n]=new Vector2(split[n].z,split[n].x+split[n].y)*.28f;ids[n]=n;}
        var mesh=new Mesh();mesh.vertices=split;mesh.uv=uv;mesh.triangles=ids;mesh.RecalculateNormals();mesh.RecalculateBounds();
        mesh=SaveMesh(mesh,"Boundary_"+sectorIndex+"_"+segment);
        var go=MeshObject("BasaltEscarpment",mesh,pos,Vector3.one,rock,yaw);
        go.AddComponent<MeshCollider>().sharedMesh=mesh;
        sources.Add(new NavMeshBuildSource{shape=NavMeshBuildSourceShape.Mesh,sourceObject=mesh,transform=go.transform.localToWorldMatrix,area=1});
    }
    static void Ridge(Vector3 pos,Vector3 size,float yaw)
    {
        var core=Box("RockCore",pos+Vector3.up*size.y*.30f,new Vector3(size.x*.72f,size.y*.60f,size.z*.72f),rock,true,yaw);
        core.GetComponent<MeshRenderer>().enabled=false;
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
        for(int i=0;i<10;i++)
        {
            float a=i*Mathf.PI*2/10;Vector3 offset=new Vector3(Mathf.Cos(a)*radius.x*.68f,0,Mathf.Sin(a)*radius.y*.68f);
            float h=1.65f+Rand()*.6f;
            Boulder(pos+offset+Vector3.up*h*.34f,new Vector3(radius.x*.88f,h,radius.y*.82f),true,rock);
        }
        // Continuous core is contained inside the visible rim. It prevents a
        // dodge or knockback from slipping into the decorative depression.
        var core=Cylinder("CraterBedrock",pos+Vector3.up*.8f,Mathf.Min(radius.x,radius.y)*.68f,1.6f,darkRock,true);
        core.GetComponent<MeshRenderer>().enabled=false;
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
