using System.Collections.Generic;
using UnityEngine;

// One shared variant per source material. Never alter hero/source materials or build textures.
public static class EnemyArmorPalette
{
    static readonly Dictionary<Material,Material> shared=new Dictionary<Material,Material>();
    public static int MaterialCount=>shared.Count;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Reset(){shared.Clear();}
    public static void Apply(GameObject root)
    {
        var shader=Resources.Load<Shader>("EnemyWhiteArmor");
        if(shader==null)throw new System.InvalidOperationException("Missing enemy armor shader");
        foreach(var renderer in root.GetComponentsInChildren<Renderer>(true))
        {
            if(renderer is LineRenderer||renderer is TrailRenderer||renderer is ParticleSystemRenderer)continue;
            var mats=renderer.sharedMaterials;bool changed=false;
            for(int i=0;i<mats.Length;i++)
            {
                var original=mats[i];if(original==null||original.shader==shader||original.shader.name!="Standard")continue;
                if(!shared.TryGetValue(original,out var white))
                {
                    white=new Material(shader){name=original.name+" | White enemy armor"};
                    white.CopyPropertiesFromMaterial(original);white.shader=shader;
                    Color tint=original.color;float gray=tint.grayscale;
                    float spread=Mathf.Max(tint.r,Mathf.Max(tint.g,tint.b))-Mathf.Min(tint.r,Mathf.Min(tint.g,tint.b));
                    if(spread>.06f&&tint.maxColorComponent>.2f)gray=.82f;
                    white.color=new Color(gray,gray,gray,1);shared.Add(original,white);
                }
                mats[i]=white;changed=true;
            }
            if(changed)renderer.sharedMaterials=mats;
        }
    }
}
