using System;
using System.Linq;
using UnityEngine;
using Object=UnityEngine.Object;

public static class NemesisEyeChecks
{
    static Color[] Read(Camera cam,int width,int height)
    {
        var target=RenderTexture.GetTemporary(width,height,24,RenderTextureFormat.ARGB32);
        var previous=cam.targetTexture;var active=RenderTexture.active;
        var image=new Texture2D(width,height,TextureFormat.RGB24,false);
        try {cam.targetTexture=target;cam.Render();RenderTexture.active=target;image.ReadPixels(new Rect(0,0,width,height),0,0);image.Apply();return image.GetPixels();}
        finally {cam.targetTexture=previous;RenderTexture.active=active;RenderTexture.ReleaseTemporary(target);Object.Destroy(image);}
    }
    public static void Run(LoadoutVisual held,Camera cam,Action<bool,string> check,Action<string> capture)
    {
        var skins=held.GetComponentsInChildren<SkinnedMeshRenderer>().Where(s=>s.name.StartsWith("J01_Body_LOD")).OrderBy(s=>s.name).ToArray();
        var group=held.assembly.GetComponent<LODGroup>();var head=skins[0].bones.Single(t=>t.name=="Head");
        var oldPosition=cam.transform.position;var oldRotation=cam.transform.rotation;float oldSize=cam.orthographicSize;
        const int width=640,height=360;int[] baseline=null;
        try
        {
            for(int lod=0;lod<skins.Length;lod++)
            {
                group.ForceLOD(lod);var skin=skins[lod];var materials=skin.sharedMaterials;
                int index=Array.FindIndex(materials,m=>m.name=="R06 original-surface eye lens");
                check(index>=0,"original eye surface material exists on NEMESIS LOD "+lod);
                var indices=skin.sharedMesh.GetTriangles(index).Distinct().ToArray();
                var source=skin.sharedMesh.vertices;var baked=new Mesh();skin.BakeMesh(baked);var posed=baked.vertices;
                var eyes=new[]{-1,1}.Select(side=>indices.Where(i=>source[i].x*side>0).Select(i=>skin.transform.TransformPoint(posed[i])).ToArray()).ToArray();
                Object.Destroy(baked);
                check(eyes.All(e=>e.Length>3),"both original eye surfaces survive NEMESIS LOD "+lod);
                Vector3 center=eyes.SelectMany(e=>e).Aggregate(Vector3.zero,(sum,p)=>sum+p)/eyes.Sum(e=>e.Length);
                cam.orthographicSize=.68f;Vector3 focus=center+head.up*.24f;cam.transform.position=focus+head.forward*3;cam.transform.LookAt(focus,head.up);
                var original=materials[index];var test=new Material(original);materials[index]=test;skin.sharedMaterials=materials;
                Color[] off,on;
                var saved=new MaterialPropertyBlock();skin.GetPropertyBlock(saved,index);
                var dark=new MaterialPropertyBlock();skin.GetPropertyBlock(dark,index);dark.SetColor("_EmissionColor",Color.black);
                try {skin.SetPropertyBlock(dark,index);off=Read(cam,width,height);skin.SetPropertyBlock(saved,index);on=Read(cam,width,height);}
                finally {skin.SetPropertyBlock(saved,index);materials[index]=original;skin.sharedMaterials=materials;Object.Destroy(test);}
                int[] visible=new int[2];
                for(int side=0;side<2;side++)
                {
                    var vp=eyes[side].Select(p=>cam.WorldToViewportPoint(p)).ToArray();
                    int minX=Mathf.Clamp(Mathf.FloorToInt(vp.Min(p=>p.x)*width)-2,0,width-1),maxX=Mathf.Clamp(Mathf.CeilToInt(vp.Max(p=>p.x)*width)+2,0,width-1);
                    int minY=Mathf.Clamp(Mathf.FloorToInt(vp.Min(p=>p.y)*height)-2,0,height-1),maxY=Mathf.Clamp(Mathf.CeilToInt(vp.Max(p=>p.y)*height)+2,0,height-1);
                    Color hue=Color.black;
                    for(int y=minY;y<=maxY;y++)for(int x=minX;x<=maxX;x++)
                    {int p=y*width+x;if(on[p].grayscale-off[p].grayscale>.08f){visible[side]++;hue+=on[p];}}
                    check(visible[side]>=6,"NEMESIS eye emits visible pixels through the actual brow at LOD "+lod+" side="+side+" pixels="+visible[side]);
                    hue/=Mathf.Max(1,visible[side]);
                    check(hue.b>hue.r+.12f&&hue.r>hue.g+.12f&&hue.g<.65f,"NEMESIS eye uses the same saturated purple-blue as the drone sheath at LOD "+lod+" side="+side+" RGB="+hue);
                }
                if(baseline==null)baseline=visible;else check(visible.Select((n,i)=>Mathf.Abs(n-baseline[i])<=2).All(v=>v),"eye visibility stays stable across body LOD "+lod);
                capture("nemesis-eyes-lod"+lod+".png");
                if(lod==0){cam.transform.position=focus+head.rotation*new Vector3(1.25f,.15f,3);cam.transform.LookAt(focus,head.up);capture("nemesis-eyes-quarter.png");}
            }
        }
        finally {group.ForceLOD(-1);cam.transform.SetPositionAndRotation(oldPosition,oldRotation);cam.orthographicSize=oldSize;}
    }
}
