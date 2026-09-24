using UnityEngine;

public static class MechEnergyPalette
{
    // Match the existing drone sheath and hot core, including their original linear values.
    public static readonly Color Nemesis=new Color(.46f,.055f,1f);
    public static readonly Color NemesisCore=new Color(.86f,.64f,1f);
    public static Color Sheath(GameObject owner)=>owner!=null&&owner.GetComponentInChildren<NemesisMotionRig>()!=null?Nemesis:new Color(.06f,.7f,1);
    public static Color Core(GameObject owner)=>owner!=null&&owner.GetComponentInChildren<NemesisMotionRig>()!=null?NemesisCore:new Color(.8f,1,1);
    public static void Apply(Transform root)
    {
        var block=new MaterialPropertyBlock();
        foreach(var renderer in root.GetComponentsInChildren<Renderer>(true))
        {
            if(renderer is ParticleSystemRenderer||renderer is LineRenderer||renderer is TrailRenderer)continue;
            var materials=renderer.sharedMaterials;
            for(int i=0;i<materials.Length;i++)
            {
                var material=materials[i];if(material==null||!material.IsKeywordEnabled("_EMISSION")||!material.HasProperty("_EmissionColor"))continue;
                var original=material.GetColor("_EmissionColor");float intensity=Mathf.Max(original.maxColorComponent,.7f);
                block.Clear();renderer.GetPropertyBlock(block,i);
                block.SetColor("_EmissionColor",Nemesis*intensity);
                // The eye lens has an authored violet base; recolor it as well without changing metal.
                if(material.name.ToLowerInvariant().Contains("eye"))block.SetColor("_Color",Nemesis*.20f);
                renderer.SetPropertyBlock(block,i);
            }
        }
    }
}
