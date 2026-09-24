using System;
using System.Collections.Generic;
using UnityEngine;

// Split the six rigid units once, so the torso's LOD and bounds cannot cull deployed weapons.
public sealed class IndependentDroneMeshes : MonoBehaviour
{
    readonly List<Mesh> allocated=new List<Mesh>();
    public int Units {get;private set;}
    public void Initialize(Transform[] drones)
    {
        foreach(var skin in GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            if(!skin.name.Contains("Body_LOD"))continue;
            var source=skin.sharedMesh;var weights=source.boneWeights;var indices=new int[6];
            for(int i=0;i<6;i++)indices[i]=Array.IndexOf(skin.bones,drones[i]);
            int Unit(int vertex)=>Array.IndexOf(indices,weights[vertex].boneIndex0);
            var body=Instantiate(source);allocated.Add(body);body.name=source.name+"_Dockless";
            bool full=skin.name.EndsWith("LOD0");
            var subsets=new List<int>[6,source.subMeshCount];
            for(int i=0;i<6;i++)for(int s=0;s<source.subMeshCount;s++)subsets[i,s]=new List<int>();
            for(int s=0;s<source.subMeshCount;s++)
            {
                var triangles=source.GetTriangles(s);var keep=new List<int>();
                for(int t=0;t<triangles.Length;t+=3)
                {
                    int unit=Unit(triangles[t]);
                    if(unit>=0&&Unit(triangles[t+1])==unit&&Unit(triangles[t+2])==unit)
                    {for(int k=0;k<3;k++)subsets[unit,s].Add(triangles[t+k]);}
                    else for(int k=0;k<3;k++)keep.Add(triangles[t+k]);
                }
                body.SetTriangles(keep,s);
            }
            skin.sharedMesh=body;
            if(!full)continue;
            for(int i=0;i<6;i++)
            {
                if(indices[i]<0)throw new InvalidOperationException("Drone joint missing from body");
                var vertices=new List<Vector3>();var normals=new List<Vector3>();var uv=new List<Vector2>();
                var oldV=source.vertices;var oldN=source.normals;var oldUv=source.uv;var lookup=new Dictionary<int,int>();
                var matrix=source.bindposes[indices[i]];var mesh=new Mesh{name="Independent drone "+i};
                var tris=new int[source.subMeshCount][];
                for(int s=0;s<source.subMeshCount;s++)
                {
                    var list=subsets[i,s];tris[s]=new int[list.Count];
                    for(int j=0;j<list.Count;j++)
                    {
                        int original=list[j];
                        if(!lookup.TryGetValue(original,out int at))
                        {at=vertices.Count;lookup.Add(original,at);vertices.Add(matrix.MultiplyPoint3x4(oldV[original]));normals.Add(matrix.MultiplyVector(oldN[original]).normalized);uv.Add(oldUv.Length>original?oldUv[original]:Vector2.zero);}
                        tris[s][j]=at;
                    }
                }
                mesh.SetVertices(vertices);mesh.SetNormals(normals);mesh.SetUVs(0,uv);mesh.subMeshCount=source.subMeshCount;
                for(int s=0;s<tris.Length;s++)mesh.SetTriangles(tris[s],s);
                mesh.RecalculateBounds();allocated.Add(mesh);
                var go=new GameObject("Autonomous drone shell "+i);go.transform.SetParent(drones[i],false);
                go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterials=skin.sharedMaterials;
                MechEnergyPalette.Apply(go.transform);Units++;
            }
        }
    }
    void OnDestroy(){foreach(var mesh in allocated)if(mesh!=null)Destroy(mesh);}
}
