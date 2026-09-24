using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using UnityEditor.SceneManagement;
using Object=UnityEngine.Object;

public static class ExtractedHeroesIntegration
{
    const string Art="Assets/Art/ExtractedHeroesR2";
    [Serializable] public class Manifest {public string hero,sourceFile,sourceSha256,sourceWeapon; public bool standaloneShoulderCannon; public Bone[] bones; public Mat[] materials; public Markers markers;}
    [Serializable] public class Bone {public string name;public int parent;public float[] position,rotation;}
    [Serializable] public class Mat {public string name,albedo,normal,emissionMap;public float[] color,emission;public float metallic,roughness;}
    [Serializable] public class Markers {public float[] rightGrip,leftGrip,cannonMuzzle,rifleMuzzle,rifleSupport,swordTip;public float[][] droneMuzzles;}
    internal sealed class Surface {public string kind;public int lod;public Mesh mesh;}
    static Vector3 V(float[] a)=>new Vector3(a[0],a[1],a[2]);
    static Quaternion Q(float[] a)=>new Quaternion(a[0],a[1],a[2],a[3]);
    static Transform Find(GameObject root,string name)=>root.GetComponentsInChildren<Transform>(true).Single(t=>t.name==name);
    static Transform Marker(string name,Transform parent,Vector3 world)
    {var t=new GameObject(name).transform;t.SetParent(parent,false);t.position=world;return t;}
    internal static Mesh Store(Mesh mesh,string path)
    {
        var old=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if(old==null){AssetDatabase.CreateAsset(mesh,path);return mesh;}
        // CopySerialized can leave the native mesh buffers at the previous
        // revision. Replace every buffer explicitly while retaining the GUID.
        old.Clear(false);old.name=mesh.name;old.indexFormat=mesh.indexFormat;
        old.vertices=mesh.vertices;old.normals=mesh.normals;old.uv=mesh.uv;
        old.tangents=mesh.tangents;old.boneWeights=mesh.boneWeights;old.bindposes=mesh.bindposes;
        old.subMeshCount=mesh.subMeshCount;
        for(int s=0;s<mesh.subMeshCount;s++)old.SetTriangles(mesh.GetTriangles(s),s,false);
        old.bounds=mesh.bounds;EditorUtility.SetDirty(old);AssetDatabase.SaveAssetIfDirty(old);
        if(old.vertexCount!=mesh.vertexCount||old.triangles.Length!=mesh.triangles.Length)throw new Exception("Stale mesh buffers: "+path);
        return old;
    }
    static void Texture(Material m,string property,string path,bool normal=false)
    {
        if(string.IsNullOrEmpty(path))return;
        var importer=(TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType=normal?TextureImporterType.NormalMap:TextureImporterType.Default;
        importer.sRGBTexture=!normal;importer.maxTextureSize=1024;importer.mipmapEnabled=true;importer.textureCompression=TextureImporterCompression.CompressedHQ;importer.SaveAndReimport();
        m.SetTexture(property,AssetDatabase.LoadAssetAtPath<Texture2D>(path));
        if(normal)m.EnableKeyword("_NORMALMAP");
    }
    internal static List<Surface> ReadSurfaces(string path)
    {
        var result=new List<Surface>();using(var r=new BinaryReader(File.OpenRead(path)))
        {
            if(r.ReadInt32()!=0x45484D31)throw new Exception("Extracted hero signature mismatch");int count=r.ReadInt32();
            for(int k=0;k<count;k++)
            {
                string kind=System.Text.Encoding.UTF8.GetString(r.ReadBytes(r.ReadInt32()));int lod=r.ReadInt32(),n=r.ReadInt32(),subs=r.ReadInt32();
                var v=new Vector3[n];var normal=new Vector3[n];var uv=new Vector2[n];var weights=new BoneWeight[n];
                for(int i=0;i<n;i++)
                {
                    v[i]=new Vector3(r.ReadSingle(),r.ReadSingle(),r.ReadSingle());normal[i]=new Vector3(r.ReadSingle(),r.ReadSingle(),r.ReadSingle());uv[i]=new Vector2(r.ReadSingle(),r.ReadSingle());
                    weights[i]=new BoneWeight{boneIndex0=r.ReadInt32(),boneIndex1=r.ReadInt32(),boneIndex2=r.ReadInt32(),boneIndex3=r.ReadInt32(),weight0=r.ReadSingle(),weight1=r.ReadSingle(),weight2=r.ReadSingle(),weight3=r.ReadSingle()};
                }
                var mesh=new Mesh{name=kind+"_LOD"+lod,indexFormat=IndexFormat.UInt32};mesh.vertices=v;mesh.normals=normal;mesh.uv=uv;mesh.boneWeights=weights;mesh.subMeshCount=subs;
                for(int s=0;s<subs;s++){int size=r.ReadInt32();var idx=new int[size];for(int i=0;i<size;i++)idx[i]=r.ReadInt32();mesh.SetTriangles(idx,s,false);}
                mesh.RecalculateBounds();mesh.RecalculateTangents();result.Add(new Surface{kind=kind,lod=lod,mesh=mesh});
            }
        }return result;
    }
    static GameObject Weapon(string name,Surface surface,Material[] mats,string folder,Vector3 muzzle,Vector3 tip,bool sword,Vector3 support=default)
    {
        var root=new GameObject(name);var mesh=surface.mesh;mesh.boneWeights=Array.Empty<BoneWeight>();var solid=new GameObject("Surface");solid.transform.SetParent(root.transform,false);
        var all=(int[])mesh.triangles.Clone();var proxy=Object.Instantiate(mesh);proxy.subMeshCount=1;proxy.SetTriangles(all,0);proxy=Store(proxy,folder+"/Meshes/"+name+"_Ghost.asset");root.AddComponent<NemesisGhostMesh>().proxy=proxy;
        if(sword)
        {
            var energy=Object.Instantiate(mesh);var baseMesh=Object.Instantiate(mesh);
            for(int i=0;i<mats.Length;i++){bool lit=mats[i].GetColor("_EmissionColor").maxColorComponent>1.01f;baseMesh.SetTriangles(lit?Array.Empty<int>():mesh.GetTriangles(i),i);energy.SetTriangles(lit?mesh.GetTriangles(i):Array.Empty<int>(),i);}
            solid.AddComponent<MeshFilter>().sharedMesh=Store(baseMesh,folder+"/Meshes/"+name+"_Solid.asset");solid.AddComponent<MeshRenderer>().sharedMaterials=mats;
            var glow=new GameObject("ReplacementBladeEnergy");glow.transform.SetParent(root.transform,false);glow.AddComponent<MeshFilter>().sharedMesh=Store(energy,folder+"/Meshes/"+name+"_Energy.asset");glow.AddComponent<MeshRenderer>().sharedMaterials=mats;
            Marker("RAIKEN_GRIP_SOCKET",root.transform,Vector3.zero);Marker("RAIKEN_BLADE_TIP",root.transform,tip);Marker("V3B_Blade_Edge_Frame",root.transform,Vector3.right*.2f);
        }
        else
        {
            solid.AddComponent<MeshFilter>().sharedMesh=Store(mesh,folder+"/Meshes/"+name+".asset");solid.AddComponent<MeshRenderer>().sharedMaterials=mats;
            Marker("Muzzle",root.transform,muzzle);Marker("Support",root.transform,support);
        }
        var prefab=PrefabUtility.SaveAsPrefabAsset(root,"Assets/Resources/Hangar/"+name+".prefab");Object.DestroyImmediate(root);return prefab;
    }
    public static void Build(){Integrate();CombatLoopV2Build.Build();}
    public static void Integrate()
    {
        PlayerSettings.meshDeformation=MeshDeformation.GPUBatched;AssetDatabase.Refresh();
        var armory=AssetDatabase.LoadAssetAtPath<HangarArmory>("Assets/Resources/Hangar/Armory.asset");GameObject rifle=null;var evidence=new List<string>();
        foreach(string hero in new[]{"NEMESIS","VALKYR"})
        {
            bool nemesis=hero=="NEMESIS";string folder=Art+"/"+hero;Directory.CreateDirectory(folder+"/Meshes");Directory.CreateDirectory(folder+"/Materials");AssetDatabase.Refresh();
            var data=JsonUtility.FromJson<Manifest>(File.ReadAllText(folder+"/manifest.json"));var mats=new Material[data.materials.Length];
            for(int i=0;i<mats.Length;i++)
            {
                var row=data.materials[i];string path=folder+"/Materials/"+i.ToString("00")+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
                if(m==null){m=new Material(Shader.Find("Standard"));AssetDatabase.CreateAsset(m,path);}m.name=row.name;m.color=new Color(row.color[0],row.color[1],row.color[2],row.color[3]).gamma;
                m.SetFloat("_Metallic",row.metallic);m.SetFloat("_Glossiness",1-row.roughness);m.SetColor("_EmissionColor",new Color(row.emission[0],row.emission[1],row.emission[2]).gamma);
                if(row.emission.Any(v=>v>0))m.EnableKeyword("_EMISSION");else m.DisableKeyword("_EMISSION");
                if(!string.IsNullOrEmpty(row.albedo))Texture(m,"_MainTex",folder+"/"+row.albedo);
                if(!string.IsNullOrEmpty(row.emissionMap))Texture(m,"_EmissionMap",folder+"/"+row.emissionMap);
                if(!string.IsNullOrEmpty(row.normal))Texture(m,"_BumpMap",folder+"/"+row.normal,true);
                EditorUtility.SetDirty(m);mats[i]=m;
            }
            var surfaces=ReadSurfaces(folder+"/model.ehm");var template=AssetDatabase.LoadAssetAtPath<GameObject>(nemesis?"Assets/Resources/Hangar/J01_NEMESIS.prefab":"Assets/Resources/Hangar/VALKYR_V9.prefab");var model=Object.Instantiate(template);model.name=hero+"_EXTRACTED_R1";model.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);
            var pose=model.GetComponent<RigidMechPoseDriver>();var adapter=model.GetComponent<RiggedMechAnimator>();var blade=model.GetComponent<RaikenBladePresentation>();var visual=model.GetComponent<LoadoutVisual>();var old=pose.assemblyRoot;
            var oldSword=nemesis?Object.Instantiate(blade.bladeRoot.gameObject):null;
            var assembly=Marker(hero+"_Extracted_Assembly",model.transform,Vector3.zero);var bones=new Transform[data.bones.Length];var map=new Dictionary<string,Transform>();
            for(int i=0;i<bones.Length;i++){var b=data.bones[i];bones[i]=Marker(b.name,assembly,V(b.position));bones[i].rotation=Q(b.rotation);map[b.name]=bones[i];}
            for(int i=0;i<bones.Length;i++)if(data.bones[i].parent>=0)bones[i].SetParent(bones[data.bones[i].parent],true);
            var bind=bones.Select(t=>t.worldToLocalMatrix*assembly.localToWorldMatrix).ToArray();var skins=new List<SkinnedMeshRenderer>();
            foreach(var surface in surfaces.Where(s=>s.kind=="Body"||s.kind=="Ghost"))
            {
                surface.mesh.bindposes=bind;
                if(surface.kind=="Ghost"){var idx=surface.mesh.triangles;surface.mesh.subMeshCount=1;surface.mesh.SetTriangles(idx,0);}
                var mesh=Store(surface.mesh,folder+"/Meshes/"+surface.kind+"_LOD"+surface.lod+".asset");
                if(surface.kind=="Ghost")continue;
                var t=Marker((nemesis?"J01":"VALKYR")+"_Body_LOD"+surface.lod,assembly,Vector3.zero);var skin=t.gameObject.AddComponent<SkinnedMeshRenderer>();skin.sharedMesh=mesh;skin.bones=bones;skin.rootBone=map["Pelvis"];skin.sharedMaterials=mats;skin.quality=SkinQuality.Bone4;skin.localBounds=new Bounds(new Vector3(0,2.5f,0),new Vector3(13,10,13));skin.updateWhenOffscreen=false;skins.Add(skin);
            }
            var lod=assembly.gameObject.AddComponent<LODGroup>();lod.SetLODs(new[]{new LOD(.52f,new Renderer[]{skins[0]}),new LOD(.32f,new Renderer[]{skins[1]}),new LOD(.008f,new Renderer[]{skins[2]})});lod.localReferencePoint=new Vector3(0,2.3f,0);lod.size=4.7f;
            // Apply the retained anatomical toe-out after binding, so the actual
            // source geometry and its rest pose turn together.
            foreach(string side in new[]{"R","L"})map["Foot."+side].rotation=Quaternion.Euler(0,side=="R"?12:-12,0);
            rifle=Weapon("EXTRACTED_"+hero+"_RIFLE",surfaces.Single(s=>s.kind=="RIFLE"),mats,folder,V(data.markers.rifleMuzzle),Vector3.zero,false,V(data.markers.rifleSupport));
            var sword=oldSword;
            if(!nemesis)sword=Object.Instantiate(Weapon("EXTRACTED_VALKYR_SWORD",surfaces.Single(s=>s.kind=="SWORD"),mats,folder,Vector3.zero,V(data.markers.swordTip),true));
            sword.transform.SetParent(assembly,true);
            foreach(var segment in pose.segments){segment.target=map[segment.target.name];segment.targetRest=segment.target.rotation;segment.localPosition=segment.target.localPosition;segment.calibration=Quaternion.identity;}
            pose.assemblyRoot=assembly;pose.assemblyRestPosition=Vector3.zero;pose.pelvis=map["Pelvis"];pose.cannon=map["Cannon_Pitch_Trunnion"];pose.cannonRest=pose.cannon.localRotation;
            foreach(var follower in pose.followers){follower.target=map[follower.target.name];follower.from=map["Thorax"];follower.to=map[follower.to.name];follower.rest=Quaternion.Inverse(follower.from.rotation)*follower.target.rotation;}
            var contacts=new List<RigidMechPoseDriver.Contact>();var bodyMesh=surfaces.Single(s=>s.kind=="Body"&&s.lod==0).mesh;var bodyV=bodyMesh.vertices;var bodyW=bodyMesh.boneWeights;
            foreach(var side in new[]{"R","L"})
            {
                var foot=map["Foot."+side];int index=Array.IndexOf(bones,foot);var pts=bodyV.Where((v,i)=>bodyW[i].boneIndex0==index&&bodyW[i].weight0>.5f).Select(v=>bind[index].MultiplyPoint3x4(v)).ToArray();if(pts.Length==0)throw new Exception("Missing soles "+hero+side);
                var hull=new HashSet<Vector3>();foreach(float pitch in new[]{-65f,-40,-20,0,20,40,65})foreach(float roll in new[]{-20f,0,20}){var q=foot.rotation*Quaternion.Euler(pitch,0,roll);hull.Add(pts.OrderBy(v=>(q*v).y).First());}contacts.AddRange(hull.Select(v=>new RigidMechPoseDriver.Contact{part=foot,localPoint=v}));
                var hand=map["Hand."+side];var socket=Marker(side=="R"?"V3B_Sword_Grip_Socket":"V3B_Left_Support_Grip_Socket",hand,V(side=="R"?data.markers.rightGrip:data.markers.leftGrip));var grip=hand.gameObject.AddComponent<ValkyrHandGrip>();grip.grip=hand.InverseTransformPoint(socket.position);grip.fingerAxis=Vector3.right;
            }
            pose.soleContacts=contacts.ToArray();blade.bladeRoot=sword.transform;blade.grip=Find(sword,"RAIKEN_GRIP_SOCKET");blade.tip=Find(sword,"RAIKEN_BLADE_TIP");blade.hand=map["Hand.R"];
            blade.beam=Find(sword,nemesis?"J01_BladeEnergy":"ReplacementBladeEnergy").gameObject;pose.beam=blade.beam;adapter.bladeTip=blade.tip;blade.bladeRoot.position+=V(data.markers.rightGrip)-blade.grip.position;
            visual.assembly=assembly;visual.upperR=map["UpperArm.R"];visual.lowerR=map["Forearm.R"];visual.handR=map["Hand.R"];visual.upperL=map["UpperArm.L"];visual.lowerL=map["Forearm.L"];visual.handL=map["Hand.L"];
            visual.gripOffsetR=visual.handR.InverseTransformPoint(V(data.markers.rightGrip));visual.gripOffsetL=visual.handL.InverseTransformPoint(V(data.markers.leftGrip));visual.handRestR=visual.handR.rotation;visual.handRestL=visual.handL.rotation;
            visual.authoredGripFrames=true;visual.rightGripInWeapon=Quaternion.LookRotation(Vector3.left,Vector3.back);visual.leftGripInWeapon=Quaternion.LookRotation(Vector3.right,Vector3.down);
            visual.rifleSupportInWeapon=nemesis?visual.leftGripInWeapon:Quaternion.LookRotation(Vector3.right,Vector3.back);
            visual.neutral=assembly.GetComponentsInChildren<Transform>(true).Select(t=>new LoadoutVisual.Rest{part=t,position=t.localPosition,rotation=t.localRotation}).ToArray();
            adapter.muzzle=Marker(hero+"_Support_Muzzle",pose.cannon,nemesis?map["BACKPACK_MOUNT"].position+new Vector3(0,.2f,-.12f):V(data.markers.cannonMuzzle));
            var thrusters=new List<Transform>();foreach(var side in new[]{"L","R"})
            {
                float sign=side=="R"?1:-1;var t=Marker(hero+"_BackpackExhaust_"+side,map["BACKPACK_MOUNT"],map["BACKPACK_MOUNT"].position+new Vector3(sign*.35f,-.48f,-.25f));t.rotation=Quaternion.LookRotation(Vector3.back);thrusters.Add(t);
                if(nemesis)foreach(float height in new[]{.667f,.462f}){t=Marker(hero+"_CalfExhaust_"+side+height,map["Shin."+side],new Vector3(sign*.12662f*2.3f,height*2.3f,-.30f*2.3f));t.rotation=Quaternion.LookRotation(Vector3.back);thrusters.Add(t);}
            }pose.thrusters=thrusters.ToArray();
            Object.DestroyImmediate(old.gameObject);
            var identity=model.AddComponent<ExtractedHeroIdentity>();identity.hero=hero;identity.sourceSha256=data.sourceSha256;identity.sourceWeapon=data.sourceWeapon;identity.standaloneShoulderCannon=data.standaloneShoulderCannon;
            if(nemesis)
            {
                model.GetComponent<NemesisMotionRig>().sourceSha256=data.sourceSha256;model.GetComponent<NemesisAfterimage>().bodyProxy=AssetDatabase.LoadAssetAtPath<Mesh>(folder+"/Meshes/Ghost_LOD0.asset");
                // JsonUtility does not deserialize jagged arrays; use actual weighted vertices.
                var controller=model.GetComponent<NemesisDroneController>();controller.muzzleLocal=new Vector3[6];
                for(int i=0;i<6;i++){var t=map["DRONE_"+(i+1).ToString("00")+"_ROOT"];int index=Array.IndexOf(bones,t);controller.muzzleLocal[i]=bodyV.Where((v,j)=>bodyW[j].boneIndex0==index).Select(v=>t.InverseTransformPoint(v)).OrderBy(v=>v.y).First();}
            }
            var prefab=PrefabUtility.SaveAsPrefabAsset(model,"Assets/Resources/Hangar/EXTRACTED_"+hero+".prefab");Object.DestroyImmediate(model);
            if(nemesis){armory.heroPrefab=prefab;armory.Find(PrimaryWeapon.M7).prefab=rifle;armory.Find(PrimaryWeapon.M7).rightHandOnly=true;}
            else
            {
                armory.valkyrPrefab=prefab;
                armory.valkyrRifle=new HangarArmory.Entry{weapon=PrimaryWeapon.M7,prefab=rifle,damage=18,interval=.18f,speed=38,rightHandOnly=false};
            }
            evidence.Add(hero+" source="+data.sourceSha256+" bones="+bones.Length+" skins="+skins.Count+" soleContacts="+contacts.Count+"\n"+string.Join("\n",surfaces.Select(s=>s.kind+" lod="+s.lod+" triangles="+s.mesh.triangles.Length/3)));
        }
        EditorUtility.SetDirty(armory);var scene=EditorSceneManager.OpenScene("Assets/Scenes/Demo_Main.unity");var loader=Object.FindFirstObjectByType<PlayerMechLoader>();loader.defaultMechPrefab=armory.heroPrefab;PrefabUtility.RecordPrefabInstancePropertyModifications(loader);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        NemesisRaikenIntegration.Integrate();
        var output=Environment.GetEnvironmentVariable("MECH_LOOP_V2_EVIDENCE");Directory.CreateDirectory(output);File.WriteAllText(Path.Combine(output,"hero-integration.txt"),string.Join("\n\n",evidence));Debug.Log("EXTRACTED_HEROES_INTEGRATED");
    }
}
