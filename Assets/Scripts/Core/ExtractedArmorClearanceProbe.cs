using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Object=UnityEngine.Object;

// Test-only probes made from the rendered armor vertices. Temporary triggers
// live outside the arena; ComputePenetration uses the actual explicit poses.
public sealed class ExtractedArmorClearanceProbe : IDisposable
{
    sealed class Part { public Transform frame; public MeshCollider collider; public Mesh mesh; }
    readonly List<Part> parts=new List<Part>();
    readonly Part chest,weapon;
    readonly Part[] arms;
    public string WorstPair {get;private set;}
    public ExtractedArmorClearanceProbe(LoadoutVisual held)
    {
        var skin=held.GetComponentsInChildren<SkinnedMeshRenderer>().Single(s=>s.name=="VALKYR_Body_LOD0");
        chest=Armor(skin,"Thorax");
        arms=new[]{Armor(skin,"Forearm.R"),Armor(skin,"Forearm.L"),Armor(skin,"Hand.R"),Armor(skin,"Hand.L")};
        var filter=held.WeaponObject.GetComponentInChildren<MeshFilter>();
        weapon=Hull("Rifle",filter.transform,filter.sharedMesh.vertices);
    }
    Part Armor(SkinnedMeshRenderer skin,string name)
    {
        int bone=Array.FindIndex(skin.bones,t=>t.name==name);
        var mesh=skin.sharedMesh;var weights=mesh.boneWeights;var vertices=mesh.vertices;var bind=mesh.bindposes[bone];
        var points=new List<Vector3>();
        for(int i=0;i<vertices.Length;i++)
            if(weights[i].boneIndex0==bone&&weights[i].weight0>.98f)points.Add(bind.MultiplyPoint3x4(vertices[i]));
        return Hull(name,skin.bones[bone],points.ToArray());
    }
    Part Hull(string name,Transform frame,Vector3[] points)
    {
        // The convex cooker only consumes referenced vertices. A triangle fan
        // references every source point without approximating it by a box.
        var unique=points.Distinct().ToArray();var indices=new int[(unique.Length-2)*3];
        for(int i=1;i<unique.Length-1;i++){indices[(i-1)*3]=0;indices[(i-1)*3+1]=i;indices[(i-1)*3+2]=i+1;}
        var mesh=new Mesh{name=name+" clearance hull",indexFormat=UnityEngine.Rendering.IndexFormat.UInt32};
        mesh.vertices=unique;mesh.triangles=indices;mesh.RecalculateBounds();
        var obj=new GameObject(name+" clearance probe");var collider=obj.AddComponent<MeshCollider>();
        obj.transform.position=new Vector3(0,-1000,0);obj.transform.localScale=frame.lossyScale;
        collider.convex=true;collider.isTrigger=true;collider.sharedMesh=mesh;
        var part=new Part{frame=frame,collider=collider,mesh=mesh};parts.Add(part);return part;
    }
    float Depth(Part part)
    {
        return Physics.ComputePenetration(chest.collider,chest.frame.position,chest.frame.rotation,
            part.collider,part.frame.position,part.frame.rotation,out _,out float depth)?depth:0;
    }
    public float WeaponDepth()=>Depth(weapon);
    public float ValidateOverlap()
    {
        Vector3 saved=weapon.frame.position;
        weapon.frame.position+=chest.frame.TransformPoint(chest.mesh.bounds.center)-weapon.frame.TransformPoint(weapon.mesh.bounds.center);
        float depth=WeaponDepth();weapon.frame.position=saved;return depth;
    }
    public float MaxDepth()
    {
        float max=0;WorstPair="none";
        foreach(var part in arms.Concat(new[]{weapon}))
        {float d=Depth(part);if(d>max){max=d;WorstPair=part.frame.name;}}
        return max;
    }
    public void Dispose(){foreach(var part in parts){Object.Destroy(part.collider.gameObject);Object.Destroy(part.mesh);}}
}
