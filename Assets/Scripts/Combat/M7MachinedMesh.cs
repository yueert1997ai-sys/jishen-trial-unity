using UnityEngine;

// Shared revolved profiles: open-neck spent cartridge and pointed copper projectile.
public static class M7MachinedMesh
{
    public static Mesh Revolve(string name, Vector2[] profile, int sides=24)
    {
        var vertices=new Vector3[profile.Length*sides];var uv=new Vector2[vertices.Length];
        var triangles=new int[(profile.Length-1)*sides*6];int t=0;
        for(int j=0;j<profile.Length;j++)for(int i=0;i<sides;i++)
        {
            float a=i*Mathf.PI*2/sides;int k=j*sides+i;
            vertices[k]=new Vector3(Mathf.Cos(a)*profile[j].x,Mathf.Sin(a)*profile[j].x,profile[j].y);
            uv[k]=new Vector2((float)i/sides,(float)j/(profile.Length-1));
            if(j==profile.Length-1)continue;int n=j*sides+(i+1)%sides;
            triangles[t++]=k;triangles[t++]=n;triangles[t++]=k+sides;
            triangles[t++]=n;triangles[t++]=n+sides;triangles[t++]=k+sides;
        }
        var mesh=new Mesh{name=name,vertices=vertices,uv=uv,triangles=triangles};mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
    }
    public static Mesh Casing()=>Revolve("M7_HollowBrass_Case",new[]{new Vector2(0,-.5f),new Vector2(.48f,-.5f),new Vector2(.48f,-.44f),new Vector2(.38f,-.44f),new Vector2(.38f,-.39f),new Vector2(.46f,-.36f),new Vector2(.43f,.22f),new Vector2(.31f,.33f),new Vector2(.31f,.5f),new Vector2(.24f,.5f),new Vector2(.24f,.29f),new Vector2(0,.24f)});
    public static Mesh Round()=>Revolve("M7_Copper_Ogive",new[]{new Vector2(0,-.5f),new Vector2(.61f,-.5f),new Vector2(.66f,-.38f),new Vector2(.66f,.08f),new Vector2(.59f,.23f),new Vector2(.39f,.39f),new Vector2(0,.55f)});
}
