using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class M7RefinementBuild
{
    const string Folder="Assets/Art/M7Refined";
    const string Evidence="AuditEvidence/m7-refinement";
    public static void Prepare()
    {
        Directory.CreateDirectory(Folder);Directory.CreateDirectory(Evidence);AssetDatabase.Refresh();
        var root=PrefabUtility.LoadPrefabContents("Assets/Resources/Hangar/M7.prefab");
        try{
            var render=root.GetComponentInChildren<MeshRenderer>();
            File.WriteAllText(Evidence+"/mesh.txt",render.bounds.ToString()+"\n"+string.Join("\n",render.sharedMaterials.Select(m=>m.name)));
            var current=render.sharedMaterials;
            render.sharedMaterials=current.Select(m=>AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/Hangar/Materials/"+m.name+".mat")??m).ToArray();
            var previousDetails=root.transform.Find("M7_MachinedDetails");if(previousDetails!=null)previousDetails.gameObject.SetActive(false);
            Capture(root,"m7_before.png");render.sharedMaterials=current;
            var shader=Shader.Find("Mech/M7 Machined Coating");if(shader==null)throw new System.Exception("M7 coating missing");
            var mats=render.sharedMaterials;
            for(int i=0;i<mats.Length;i++)
            {
                var source=mats[i];string name=source.name;string path=Folder+"/"+name+".mat";
                var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
                if(mat==null){mat=new Material(source);AssetDatabase.CreateAsset(mat,path);}
                mat.shader=shader;mat.SetFloat("_Grain",.16f);
                if(name.Contains("Desert")){mat.color=new Color(.58f,.47f,.32f);mat.SetFloat("_Metallic",.28f);mat.SetFloat("_Glossiness",.4f);}
                else if(name.Contains("Sand_Edge")){mat.color=new Color(.68f,.57f,.41f);mat.SetFloat("_Metallic",.35f);mat.SetFloat("_Glossiness",.46f);}
                else if(name.Contains("Sand_Dark")){mat.color=new Color(.38f,.31f,.23f);mat.SetFloat("_Metallic",.24f);mat.SetFloat("_Glossiness",.32f);}
                else if(name.Contains("Rubber")||name.Contains("Polymer")||name.Contains("Stipple")){mat.color=new Color(.095f,.105f,.10f);mat.SetFloat("_Metallic",.02f);mat.SetFloat("_Glossiness",.21f);mat.SetFloat("_Grain",.38f);}
                else if(name.Contains("Steel")||name.Contains("Hardware")||name.Contains("Alloy")){mat.color=name.Contains("Alloy")?new Color(.48f,.49f,.46f):new Color(.23f,.26f,.28f);mat.SetFloat("_Metallic",.82f);mat.SetFloat("_Glossiness",.6f);}
                else if(name.Contains("Black")){mat.color=new Color(.105f,.125f,.14f);mat.SetFloat("_Metallic",.68f);mat.SetFloat("_Glossiness",.46f);}
                EditorUtility.SetDirty(mat);mats[i]=mat;
            }
            render.sharedMaterials=mats;
            // Root local +Z follows the barrel; receiver remains in its original authored position.
            var detail=root.transform.Find("M7_MachinedDetails");if(detail!=null)Object.DestroyImmediate(detail.gameObject);
            var details=new GameObject("M7_MachinedDetails");details.transform.SetParent(root.transform,false);
            var steel=mats.First(m=>m.name.Contains("Dark_Steel"));var dark=mats.First(m=>m.name.Contains("Black_Hardware"));
            for(int side=-1;side<=1;side+=2)
            {
                // Captive fastener heads with recessed sockets; only a few readable service details.
                foreach(float z in new[]{.045f,.30f})
                {
                    var bolt=Part(details.transform,"Captive_receiver_fastener",PrimitiveType.Cylinder,new Vector3(side*.038f,.151f,z),new Vector3(.017f,.0025f,.017f),steel);
                    bolt.transform.localRotation=Quaternion.Euler(0,0,90);
                    var socket=Part(details.transform,"Hex_socket",PrimitiveType.Cylinder,new Vector3(side*.041f,.151f,z),new Vector3(.008f,.0008f,.008f),dark);socket.transform.localRotation=Quaternion.Euler(0,0,90);
                }
            }
            var port=root.transform.Find("EjectionPort");if(port==null){port=new GameObject("EjectionPort").transform;port.SetParent(root.transform,false);}
            port.localPosition=new Vector3(.051f,.193f,.20f);port.localRotation=Quaternion.identity;
            PrefabUtility.SaveAsPrefabAsset(root,"Assets/Resources/Hangar/M7.prefab");AssetDatabase.SaveAssets();
            Capture(root,"m7_after.png");
        }finally{PrefabUtility.UnloadPrefabContents(root);}
    }
    static GameObject Part(Transform root,string name,PrimitiveType type,Vector3 pos,Vector3 size,Material mat)
    {
        var go=GameObject.CreatePrimitive(type);go.name=name;go.transform.SetParent(root,false);go.transform.localPosition=pos;go.transform.localScale=size;
        Object.DestroyImmediate(go.GetComponent<Collider>());go.GetComponent<Renderer>().sharedMaterial=mat;return go;
    }
    static void Capture(GameObject root,string file)
    {
        var scene=root.scene;var go=new GameObject("M7ReviewCamera");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go,scene);
        var camera=go.AddComponent<Camera>();camera.scene=scene;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.065f,.08f,.10f);camera.fieldOfView=32;
        var bounds=root.GetComponentInChildren<Renderer>().bounds;Vector3 target=bounds.center;
        go.transform.position=target+new Vector3(3.6f,1.15f,1.8f);go.transform.LookAt(target);
        var lightGo=new GameObject("M7ReviewKey");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(lightGo,scene);
        var light=lightGo.AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.5f;lightGo.transform.rotation=Quaternion.Euler(35,-45,0);
        var fillGo=new GameObject("M7ReviewFill");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(fillGo,scene);var fill=fillGo.AddComponent<Light>();fill.type=LightType.Directional;fill.intensity=.65f;fillGo.transform.rotation=Quaternion.Euler(15,140,0);
        var rt=new RenderTexture(1800,1000,24){antiAliasing=4};camera.targetTexture=rt;var old=RenderTexture.active;camera.Render();RenderTexture.active=rt;
        var tex=new Texture2D(1800,1000,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1800,1000),0,0);tex.Apply();File.WriteAllBytes(Evidence+"/"+file,tex.EncodeToPNG());
        camera.targetTexture=null;RenderTexture.active=old;Object.DestroyImmediate(tex);rt.Release();Object.DestroyImmediate(rt);Object.DestroyImmediate(go);Object.DestroyImmediate(lightGo);Object.DestroyImmediate(fillGo);
    }
    public static void Build()
    {
        Prepare();
        const string output="Builds/P0_10_M7/MECH_TRIAL_P0.exe";Directory.CreateDirectory(Path.GetDirectoryName(output));
        string previous=PlayerSettings.bundleVersion;
        try{
            PlayerSettings.bundleVersion="p0.10-m7-20260911";
            var result=UnityEditor.BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/Scenes/Demo_Main.unity"},locationPathName=output,target=BuildTarget.StandaloneWindows64,options=BuildOptions.StrictMode});
            File.WriteAllText(Evidence+"/build-result.txt",result.summary.result+" errors="+result.summary.totalErrors+" warnings="+result.summary.totalWarnings);
            if(result.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded)throw new System.Exception("M7 build failed");
        }finally{PlayerSettings.bundleVersion=previous;}
    }
}
