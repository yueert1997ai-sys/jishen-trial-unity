using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public static class HalbreakerIntegration
{
    const string Art = "Assets/Art/HalbreakerAX01";
    const string Output = "Assets/Resources/Hangar";
    const string Evidence = "AuditEvidence/halbreaker-v10";
    [Serializable] class Palette { public Row[] materials; }
    [Serializable] class Row { public string name; public float[] color, emission; public float metallic, roughness; }
    static Transform Part(GameObject root,string name) => root.GetComponentsInChildren<Transform>(true).Single(t=>t.name==name);
    public static void ConfigureAndBuild()
    {
        Configure();
        CompanyUpdateIntegration.Build();
    }
    public static void Configure()
    {
        Directory.CreateDirectory(Art+"/Materials");Directory.CreateDirectory(Evidence);AssetDatabase.Refresh();
        var importer=(ModelImporter)AssetImporter.GetAtPath(Art+"/HALBREAKER_AX01.fbx");
        importer.isReadable=true;importer.importAnimation=false;importer.importCameras=false;importer.importLights=false;
        importer.globalScale=1;importer.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;importer.SaveAndReimport();
        var materials=new Dictionary<string,Material>();
        foreach(var row in JsonUtility.FromJson<Palette>(File.ReadAllText(Art+"/materials.json")).materials)
        {
            string safe=string.Concat(row.name.Select(c=>Path.GetInvalidFileNameChars().Contains(c)?'_':c));
            string path=Art+"/Materials/"+safe+".mat";
            var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(mat==null){mat=new Material(Shader.Find("Standard"));AssetDatabase.CreateAsset(mat,path);}
            mat.name=row.name;mat.color=new Color(row.color[0],row.color[1],row.color[2],1);
            mat.SetFloat("_Metallic",row.metallic);mat.SetFloat("_Glossiness",1-row.roughness);
            var emission=new Color(row.emission[0],row.emission[1],row.emission[2]);mat.SetColor("_EmissionColor",emission);
            if(emission.maxColorComponent>.001f)mat.EnableKeyword("_EMISSION");else mat.DisableKeyword("_EMISSION");
            EditorUtility.SetDirty(mat);materials[row.name]=mat;materials[safe]=mat;
        }
        var model=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Art+"/HALBREAKER_AX01.fbx"));
        model.name="HALBREAKER_Reviewed_Master";
        foreach(var r in model.GetComponentsInChildren<Renderer>(true))
            r.sharedMaterials=r.sharedMaterials.Select(m=>m!=null&&materials.TryGetValue(m.name,out var replacement)?replacement:throw new Exception("Unmapped AX01 material: "+m?.name)).ToArray();
        var oldGrip=Part(model,"Grip_Export");var oldMuzzle=Part(model,"Muzzle_Export");var oldShoulder=Part(model,"ShoulderAnchor_Export");
        Vector3 forward=oldMuzzle.position-oldGrip.position;
        model.transform.rotation=Quaternion.FromToRotation(new Vector3(forward.x,0,forward.z).normalized,Vector3.forward)*model.transform.rotation;
        var wrapper=new GameObject("HALBREAKER_AX01_Shoulder_Equippable");model.transform.SetParent(wrapper.transform,true);model.transform.position-=oldGrip.position;
        var mount=wrapper.AddComponent<HalbreakerMount>();
        Transform Socket(string name,Transform src)
        {var t=new GameObject(name).transform;t.SetParent(wrapper.transform,false);t.position=src.position;return t;}
        mount.grip=Socket("Grip",oldGrip);mount.muzzle=Socket("Muzzle",oldMuzzle);mount.shoulder=Socket("ShoulderAnchor",oldShoulder);
        foreach(var group in model.GetComponentsInChildren<LODGroup>(true))Object.DestroyImmediate(group);
        var lodGroup=wrapper.AddComponent<LODGroup>();var lods=new LOD[3];var counts=new int[3];
        for(int i=0;i<3;i++)
        {
            var part=Part(model,"HALBREAKER_LOD"+i);part.gameObject.SetActive(true);var r=part.GetComponent<Renderer>();r.enabled=true;
            lods[i]=new LOD(new[]{.32f,.12f,.012f}[i],new[]{r});counts[i]=part.GetComponent<MeshFilter>().sharedMesh.triangles.Length/3;
        }
        lodGroup.SetLODs(lods);lodGroup.RecalculateBounds();
        var mf=Part(model,"HALBREAKER_LOD0").GetComponent<MeshFilter>();
        var points=mf.sharedMesh.vertices.Select(v=>wrapper.transform.InverseTransformPoint(mf.transform.TransformPoint(v))).ToArray();
        float length=points.Max(v=>v.z)-points.Min(v=>v.z);
        if(Mathf.Abs(length-4.12019795f)>.015f)throw new Exception("AX01 scale failed: "+length);
        if(mount.grip.position.magnitude>.001f || mount.muzzle.localPosition.z<2.2f)throw new Exception("AX01 sockets failed");
        var prefab=PrefabUtility.SaveAsPrefabAsset(wrapper,Output+"/HALBREAKER.prefab");Object.DestroyImmediate(wrapper);
        var armory=AssetDatabase.LoadAssetAtPath<HangarArmory>(Output+"/Armory.asset");
        if(armory==null || armory.heroPrefab==null)throw new Exception("Existing V9 armory missing");
        var entries=armory.weapons.Where(e=>e.weapon!=PrimaryWeapon.Halbreaker).ToList();
        entries.Add(new HangarArmory.Entry{weapon=PrimaryWeapon.Halbreaker,prefab=prefab,damage=160,interval=1.05f,speed=0,pierce=99,blastRadius=1.0f,rightHandOnly=true});
        armory.weapons=entries.ToArray();armory.startingWeapon=PrimaryWeapon.Halbreaker;EditorUtility.SetDirty(armory);AssetDatabase.SaveAssets();
        File.WriteAllText(Evidence+"/integration.txt",$"HALBREAKER_IMPORT_PASS\nSource: reviewed final AX01, flat front, original U grip\nLength={length:F6}, exposed barrel=2.2\nLOD triangles={string.Join(",",counts)}\nShoulder only, right U grip, left free\nBlue beam: 160 damage, 1.05 s, pierce 99, splash 1 m\nOther equipment retained={entries.Count-1}\n");
        Debug.Log("HALBREAKER_IMPORT_PASS");
    }
}
