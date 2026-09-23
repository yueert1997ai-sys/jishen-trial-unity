using UnityEngine;

[RequireComponent(typeof(Camera))]
public sealed class CombatBloom : MonoBehaviour
{
    Material material;
    void Awake()
    {
        var shader=Resources.Load<Shader>("CombatBloom");
        if(shader!=null && shader.isSupported)material=new Material(shader);
    }
    void OnRenderImage(RenderTexture source,RenderTexture destination)
    {
        if(material==null || GameManager.Instance==null || GameManager.Instance.Phase!=GamePhase.Combat) { Graphics.Blit(source,destination);return; }
        int w=Mathf.Max(1,source.width/4),h=Mathf.Max(1,source.height/4);
        var a=RenderTexture.GetTemporary(w,h,0,source.format);
        var b=RenderTexture.GetTemporary(w,h,0,source.format);
        a.filterMode=b.filterMode=FilterMode.Bilinear;
        try
        {
            Graphics.Blit(source,a,material,0);
            for(int i=0;i<2;i++)
            {
                material.SetVector("_Direction",new Vector4(1f/w,0,0,0));Graphics.Blit(a,b,material,1);
                material.SetVector("_Direction",new Vector4(0,1f/h,0,0));Graphics.Blit(b,a,material,1);
            }
            material.SetTexture("_Glow",a);material.SetFloat("_Strength",OverdriveVfx.Intense?1.3f:.45f);
            Graphics.Blit(source,destination,material,2);
        }
        finally { RenderTexture.ReleaseTemporary(a);RenderTexture.ReleaseTemporary(b); }
    }
    void OnDestroy(){if(material!=null)Destroy(material);}
}
