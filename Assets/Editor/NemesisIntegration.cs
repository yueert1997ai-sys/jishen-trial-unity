using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering;
using Object=UnityEngine.Object;

public static class NemesisIntegration
{
    public const string Evidence="AuditEvidence/nemesis-m02";
    const string Art="Assets/Art/NemesisM02";
    public static void Build(){Integrate();CombatLoopV2Build.Build();}
    [Serializable] public class Manifest { public Bone[] bones; public Mat[] materials; public Sockets markers; public string sourceSha256; }
    [Serializable] public class Bone {public string name;public int parent;public float[] position,rotation;}
    [Serializable] public class Mat {public string name,smoothnessMap;public float[] color,emission;public float metallic,roughness;}
    [Serializable] public class Sockets {public Socket RIFLE,SWORD,LAUNCHER;}
    [Serializable] public class Socket {public float[] RIFLE_MUZZLE,RIFLE_SUPPORT_GRIP,SWORD_BLADE_TIP,LAUNCHER_MUZZLE,LAUNCHER_SUPPORT;}
    sealed class Surface {public string kind;public int lod;public Mesh mesh;public int[] bones;}
    static Vector3 V(float[] a)=>new Vector3(a[0],a[1],a[2]);
    static Transform Part(GameObject root,string name)=>root.GetComponentsInChildren<Transform>(true).Single(t=>t.name==name);
    static Transform Marker(string name,Transform parent,Vector3 position)
    {var t=new GameObject(name).transform;t.SetParent(parent,false);t.localPosition=position;return t;}
    static void StoreMesh(Mesh mesh,string name)
    {
        string path=Art+"/Meshes/"+name+".asset";
        var prior=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if(prior==null)AssetDatabase.CreateAsset(mesh,path);
        else {EditorUtility.CopySerialized(mesh,prior);EditorUtility.SetDirty(prior);}
    }
    public static void Integrate()
    {
        PlayerSettings.meshDeformation=MeshDeformation.GPUBatched;
        Directory.CreateDirectory(Evidence);Directory.CreateDirectory(Art+"/Meshes");Directory.CreateDirectory(Art+"/Materials");
        AssetDatabase.Refresh();
        var manifest=JsonUtility.FromJson<Manifest>(File.ReadAllText(Art+"/manifest.json"));
        var materials=new List<Material>();
        for(int i=0;i<manifest.materials.Length;i++)
        {
            var row=manifest.materials[i];string path=Art+"/Materials/J01_"+i.ToString("00")+".mat";
            var m=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(m==null){m=new Material(Shader.Find("Standard"));AssetDatabase.CreateAsset(m,path);}
            m.name=row.name;m.color=new Color(row.color[0],row.color[1],row.color[2],row.color[3]).gamma;
            m.SetFloat("_Metallic",row.metallic);m.SetFloat("_Glossiness",1-row.roughness);
            m.SetFloat("_GlossMapScale",1);m.SetFloat("_SmoothnessTextureChannel",0);
            var emission=new Color(row.emission[0],row.emission[1],row.emission[2]);m.SetColor("_EmissionColor",emission.gamma);
            if(emission.maxColorComponent>0)m.EnableKeyword("_EMISSION");else m.DisableKeyword("_EMISSION");
            if(!string.IsNullOrEmpty(row.smoothnessMap))
            {
                var ti=(TextureImporter)AssetImporter.GetAtPath(Art+"/"+row.smoothnessMap);ti.sRGBTexture=false;ti.alphaSource=TextureImporterAlphaSource.FromInput;ti.SaveAndReimport();
                m.SetTexture("_MetallicGlossMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Art+"/"+row.smoothnessMap));m.EnableKeyword("_METALLICGLOSSMAP");
            }
            materials.Add(m);EditorUtility.SetDirty(m);
        }
        var surfaces=new List<Surface>();
        using(var input=new BinaryReader(File.OpenRead(Art+"/J01.j01mesh")))
        {
            if(input.ReadInt32()!=0x4A303131)throw new Exception("J01 surface signature mismatch");
            int count=input.ReadInt32();
            for(int j=0;j<count;j++)
            {
                string name=System.Text.Encoding.UTF8.GetString(input.ReadBytes(input.ReadInt32()));int lod=input.ReadInt32(),n=input.ReadInt32(),subs=input.ReadInt32();
                var vertices=new Vector3[n];var normals=new Vector3[n];var uv=new Vector2[n];var bones=new int[n];
                for(int i=0;i<n;i++)
                {
                    vertices[i]=new Vector3(input.ReadSingle(),input.ReadSingle(),input.ReadSingle());
                    normals[i]=new Vector3(input.ReadSingle(),input.ReadSingle(),input.ReadSingle());
                    uv[i]=new Vector2(input.ReadSingle(),input.ReadSingle());bones[i]=input.ReadInt32();
                }
                var mesh=new Mesh{name="J01_"+name+"_LOD"+lod,indexFormat=IndexFormat.UInt32};
                mesh.vertices=vertices;mesh.normals=normals;mesh.uv=uv;mesh.subMeshCount=subs;
                for(int s=0;s<subs;s++){int size=input.ReadInt32();var indices=new int[size];for(int i=0;i<size;i++)indices[i]=input.ReadInt32();for(int i=0;i<size;i+=3){int v=indices[i];indices[i]=indices[i+2];indices[i+2]=v;}mesh.SetTriangles(indices,s,false);}
                mesh.RecalculateBounds();mesh.RecalculateTangents();surfaces.Add(new Surface{kind=name,lod=lod,mesh=mesh,bones=bones});
            }
        }
        var model=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Hangar/VALKYR_V9.prefab"));
        model.name="J01_NEMESIS_M02";
        var pose=model.GetComponent<RigidMechPoseDriver>();var adapter=model.GetComponent<RiggedMechAnimator>();var blade=model.GetComponent<RaikenBladePresentation>();var visual=model.GetComponent<LoadoutVisual>();
        var old=pose.assemblyRoot;
        var assembly=Marker("J01_NEMESIS_Assembly",model.transform,Vector3.zero);
        var joints=new Transform[manifest.bones.Length];
        for(int i=0;i<joints.Length;i++)
        {
            var row=manifest.bones[i];var t=Marker(row.name,assembly,Vector3.zero);
            t.SetPositionAndRotation(V(row.position),new Quaternion(row.rotation[0],row.rotation[1],row.rotation[2],row.rotation[3]));joints[i]=t;
        }
        for(int i=0;i<joints.Length;i++)if(manifest.bones[i].parent>=0)joints[i].SetParent(joints[manifest.bones[i].parent],true);
        var skins=new List<SkinnedMeshRenderer>();
        foreach(var surface in surfaces.Where(s=>s.kind=="Body"))
        {
            surface.mesh.boneWeights=surface.bones.Select(b=>new BoneWeight{boneIndex0=b,weight0=1}).ToArray();
            surface.mesh.bindposes=joints.Select(t=>t.worldToLocalMatrix).ToArray();
            StoreMesh(surface.mesh,surface.mesh.name);
            var t=Marker(surface.mesh.name,assembly,Vector3.zero);var skin=t.gameObject.AddComponent<SkinnedMeshRenderer>();
            skin.sharedMesh=AssetDatabase.LoadAssetAtPath<Mesh>(Art+"/Meshes/"+surface.mesh.name+".asset");skin.bones=joints;skin.rootBone=Part(assembly.gameObject,"Pelvis");skin.sharedMaterials=materials.ToArray();
            skin.quality=SkinQuality.Bone1;skin.localBounds=new Bounds(new Vector3(0,2.4f,0),new Vector3(10,8,10));skin.updateWhenOffscreen=false;skins.Add(skin);
        }
        var lodGroup=assembly.gameObject.AddComponent<LODGroup>();
        lodGroup.SetLODs(new[]{new LOD(.52f,new Renderer[]{skins[0]}),new LOD(.32f,new Renderer[]{skins[1]}),new LOD(.008f,new Renderer[]{skins[2]})});
        // Culling bounds include detached drones; LOD distance uses body height,
        // so a large flying-appendage bound cannot force the inspection mesh in combat.
        lodGroup.localReferencePoint=new Vector3(0,2.2f,0);lodGroup.size=4.7f;
        foreach(var surface in surfaces.Where(s=>s.kind=="Ghost"||s.kind!="Body"&&s.lod==1))
        {
            if(surface.kind=="Ghost")
            {
                surface.mesh.boneWeights=surface.bones.Select(b=>new BoneWeight{boneIndex0=b,weight0=1}).ToArray();
                surface.mesh.bindposes=joints.Select(t=>t.worldToLocalMatrix).ToArray();
            }
            var triangles=surface.mesh.triangles;surface.mesh.subMeshCount=1;surface.mesh.SetTriangles(triangles,0);
            StoreMesh(surface.mesh,surface.mesh.name);
        }
        var weapons=new Dictionary<string,GameObject>();
        foreach(var surface in surfaces.Where(s=>s.kind!="Body"&&s.kind!="Ghost"&&s.lod==0))
        {
            StoreMesh(surface.mesh,surface.mesh.name);
            var root=new GameObject("J01_"+surface.kind);var meshRoot=Marker("Surface",root.transform,Vector3.zero);
            meshRoot.gameObject.AddComponent<MeshFilter>().sharedMesh=AssetDatabase.LoadAssetAtPath<Mesh>(Art+"/Meshes/"+surface.mesh.name+".asset");
            meshRoot.gameObject.AddComponent<MeshRenderer>().sharedMaterials=materials.ToArray();
            root.AddComponent<NemesisGhostMesh>().proxy=AssetDatabase.LoadAssetAtPath<Mesh>(Art+"/Meshes/J01_"+surface.kind+"_LOD1.asset");
            if(surface.kind!="SWORD")
            {
                bool rifle=surface.kind=="RIFLE";var data=rifle?manifest.markers.RIFLE:manifest.markers.LAUNCHER;
                Marker("Muzzle",root.transform,V(rifle?data.RIFLE_MUZZLE:data.LAUNCHER_MUZZLE));
                Marker("Support",root.transform,rifle?V(data.RIFLE_SUPPORT_GRIP):new Vector3(-.035f,.225f,.38f));
            }
            else
            {
                Marker("RAIKEN_GRIP_SOCKET",root.transform,Vector3.zero);
                Marker("RAIKEN_BLADE_TIP",root.transform,V(manifest.markers.SWORD.SWORD_BLADE_TIP));
                Marker("V3B_Blade_Edge_Frame",root.transform,new Vector3(0,-.2f,0));
                // Separate the energy edge from the physical blade, so stowing suppresses only light.
                var indices=new List<int>();var baseMesh=Object.Instantiate(surface.mesh);var edgeMesh=Object.Instantiate(surface.mesh);
                for(int i=0;i<materials.Count;i++)
                {
                    bool lit=manifest.materials[i].emission.Any(v=>v>.01f);
                    baseMesh.SetTriangles(lit?Array.Empty<int>():surface.mesh.GetTriangles(i),i);
                    edgeMesh.SetTriangles(lit?surface.mesh.GetTriangles(i):Array.Empty<int>(),i);
                }
                StoreMesh(baseMesh,"SwordSolid");StoreMesh(edgeMesh,"SwordEdge");
                meshRoot.GetComponent<MeshFilter>().sharedMesh=AssetDatabase.LoadAssetAtPath<Mesh>(Art+"/Meshes/SwordSolid.asset");
                var edge=Marker("J01_BladeEnergy",root.transform,Vector3.zero);edge.gameObject.AddComponent<MeshFilter>().sharedMesh=AssetDatabase.LoadAssetAtPath<Mesh>(Art+"/Meshes/SwordEdge.asset");edge.gameObject.AddComponent<MeshRenderer>().sharedMaterials=materials.ToArray();
            }
            weapons[surface.kind]=PrefabUtility.SaveAsPrefabAsset(root,"Assets/Resources/Hangar/J01_"+surface.kind+".prefab");Object.DestroyImmediate(root);
        }
        var sword=Object.Instantiate(weapons["SWORD"],assembly);sword.name="J01_Sword";
        foreach(var s in pose.segments){s.target=Part(assembly.gameObject,s.target.name);s.targetRest=s.target.rotation;s.localPosition=s.target.localPosition;s.calibration=Quaternion.identity;}
        pose.pelvis=Part(assembly.gameObject,"Pelvis");pose.assemblyRoot=assembly;pose.assemblyRestPosition=Vector3.zero;
        pose.cannon=Marker("Cannon_Pitch_Trunnion",Part(assembly.gameObject,"BACKPACK_MOUNT"),Vector3.zero);pose.cannonRest=Quaternion.identity;
        adapter.muzzle=Marker("J01_Support_Muzzle",pose.cannon,new Vector3(0,.2f,-.12f));
        pose.thrusters=new[]{Marker("J01_Exhaust_L",Part(assembly.gameObject,"BACKPACK_MOUNT"),new Vector3(-.2f,-.15f,-.3f)),Marker("J01_Exhaust_R",Part(assembly.gameObject,"BACKPACK_MOUNT"),new Vector3(.2f,-.15f,-.3f))};
        foreach(var f in pose.followers){f.target=Part(assembly.gameObject,f.target.name);f.from=Part(assembly.gameObject,"Thorax");f.to=Part(assembly.gameObject,f.to.name);f.rest=Quaternion.Inverse(f.from.rotation)*f.target.rotation;}
        var contacts=new List<RigidMechPoseDriver.Contact>();var source=surfaces.First(s=>s.kind=="Body"&&s.lod==0);
        foreach(var side in new[]{"L","R"})
        {
            var foot=Part(assembly.gameObject,"Foot."+side);int index=Array.IndexOf(joints,foot);
            var points=source.mesh.vertices.Where((v,i)=>source.bones[i]==index).Select(v=>foot.InverseTransformPoint(v)).ToArray();
            var hull=new HashSet<Vector3>();
            foreach(float pitch in new[]{-65f,-40,-20,0,20,40,65})foreach(float roll in new[]{-20f,0,20})
            {var rotation=foot.rotation*Quaternion.Euler(pitch,0,roll);hull.Add(points.OrderBy(v=>(rotation*v).y).First());}
            contacts.AddRange(hull.Select(v=>new RigidMechPoseDriver.Contact{part=foot,localPoint=v}));
            var hand=Part(assembly.gameObject,"Hand."+side);var contact=Part(assembly.gameObject,"GRIP_SOCKET."+side);
            contact.name=side=="R"?"V3B_Sword_Grip_Socket":"V3B_Left_Support_Grip_Socket";
            var grip=hand.gameObject.AddComponent<ValkyrHandGrip>();grip.grip=hand.InverseTransformPoint(contact.position);grip.fingerAxis=Vector3.right;
            grip.fingers=hand.GetComponentsInChildren<Transform>().Where(t=>t.name.StartsWith("Finger_")||t.name.StartsWith("Thumb_")).ToArray();
        }
        pose.soleContacts=contacts.ToArray();
        blade.bladeRoot=sword.transform;blade.grip=Part(sword,"RAIKEN_GRIP_SOCKET");blade.tip=Part(sword,"RAIKEN_BLADE_TIP");blade.hand=Part(assembly.gameObject,"Hand.R");blade.beam=Part(sword,"J01_BladeEnergy").gameObject;pose.beam=blade.beam;adapter.bladeTip=blade.tip;
        visual.assembly=assembly;visual.upperR=Part(assembly.gameObject,"UpperArm.R");visual.lowerR=Part(assembly.gameObject,"Forearm.R");visual.handR=blade.hand;
        visual.upperL=Part(assembly.gameObject,"UpperArm.L");visual.lowerL=Part(assembly.gameObject,"Forearm.L");visual.handL=Part(assembly.gameObject,"Hand.L");
        visual.gripOffsetR=visual.handR.InverseTransformPoint(Part(assembly.gameObject,"V3B_Sword_Grip_Socket").position);visual.gripOffsetL=visual.handL.InverseTransformPoint(Part(assembly.gameObject,"V3B_Left_Support_Grip_Socket").position);
        visual.handRestR=visual.handR.rotation;visual.handRestL=visual.handL.rotation;
        visual.neutral=assembly.GetComponentsInChildren<Transform>(true).Select(t=>new LoadoutVisual.Rest{part=t,position=t.localPosition,rotation=t.localRotation}).ToArray();
        Object.DestroyImmediate(old.gameObject);
        var nemesis=model.AddComponent<NemesisMotionRig>();nemesis.sourceSha256=manifest.sourceSha256;
        model.AddComponent<NemesisAfterimage>().bodyProxy=AssetDatabase.LoadAssetAtPath<Mesh>(Art+"/Meshes/J01_Ghost_LOD0.asset");
        var droneController=model.AddComponent<NemesisDroneController>();
        droneController.muzzleLocal=new Vector3[6];
        for(int i=0;i<6;i++)
        {
            var joint=Part(assembly.gameObject,"DRONE_"+(i+1).ToString("00")+"_ROOT");int index=Array.IndexOf(joints,joint);
            // The long wing's pointed end is its firing end, along local -Y.
            var points=source.mesh.vertices.Where((v,n)=>source.bones[n]==index).Select(v=>joint.InverseTransformPoint(v)).ToArray();
            var tip=points.OrderBy(v=>v.y).First();droneController.muzzleLocal[i]=tip+Vector3.down*.025f;
        }
        var prefab=PrefabUtility.SaveAsPrefabAsset(model,"Assets/Resources/Hangar/J01_NEMESIS.prefab");
        Object.DestroyImmediate(model);
        var armory=AssetDatabase.LoadAssetAtPath<HangarArmory>("Assets/Resources/Hangar/Armory.asset");armory.heroPrefab=prefab;armory.startingWeapon=PrimaryWeapon.M7;
        armory.valkyrPrefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Hangar/VALKYR_V9.prefab");
        armory.valkyrRifle=new HangarArmory.Entry{weapon=PrimaryWeapon.M7,prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Hangar/M7.prefab"),damage=18,interval=.18f,speed=38,rightHandOnly=false};
        armory.Find(PrimaryWeapon.M7).prefab=weapons["RIFLE"];armory.Find(PrimaryWeapon.M7).rightHandOnly=true;armory.Find(PrimaryWeapon.Greatsword).prefab=weapons["SWORD"];
        var launcher=armory.Find(PrimaryWeapon.NemesisLauncher);
        if(launcher==null){launcher=new HangarArmory.Entry{weapon=PrimaryWeapon.NemesisLauncher};armory.weapons=armory.weapons.Concat(new[]{launcher}).ToArray();}
        launcher.prefab=weapons["LAUNCHER"];launcher.damage=70;launcher.interval=.95f;launcher.speed=32;launcher.blastRadius=1.8f;launcher.rightHandOnly=false;
        EditorUtility.SetDirty(armory);
        var scene=EditorSceneManager.OpenScene("Assets/Scenes/Demo_Main.unity");var gm=Object.FindFirstObjectByType<GameManager>();var loader=gm.playerController.GetComponent<PlayerMechLoader>();loader.defaultMechPrefab=prefab;
        PrefabUtility.RecordPrefabInstancePropertyModifications(loader);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        File.WriteAllText(Evidence+"/import-result.txt","Source B20="+manifest.sourceSha256+"\nRigid bones="+joints.Length+"\n"+string.Join("\n",surfaces.Select(s=>{var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(Art+"/Meshes/J01_"+s.kind+"_LOD"+s.lod+".asset");return s.kind+" LOD"+s.lod+" vertices="+mesh.vertexCount+" triangles="+mesh.triangles.Length/3;}))+"\nSole contacts="+contacts.Count);
        Debug.Log("NEMESIS_INTEGRATED");
    }
    public static void Inspect()
    {
        Directory.CreateDirectory(Evidence);
        var armory=AssetDatabase.LoadAssetAtPath<HangarArmory>("Assets/Resources/Hangar/Armory.asset");
        var go=Object.Instantiate(armory.heroPrefab);
        var pose=go.GetComponent<RigidMechPoseDriver>();
        var rows=pose.assemblyRoot.GetComponentsInChildren<Transform>(true).Where(t=>t.GetComponent<Renderer>()==null)
            .Select(t=>t.name+" parent="+t.parent?.name+" p="+t.position.ToString("F4")+" r="+t.eulerAngles.ToString("F2")+" scale="+t.lossyScale);
        var r=go.GetComponentsInChildren<Renderer>().ToArray();var b=r[0].bounds;foreach(var q in r)b.Encapsulate(q.bounds);
        File.WriteAllText(Evidence+"/baseline-rig.txt","Source="+AssetDatabase.GetAssetPath(armory.heroPrefab)+"\nBounds="+b+"\n"+string.Join("\n",rows));
        Object.DestroyImmediate(go);
        Debug.Log("NEMESIS_INSPECT_DONE");
    }
}
