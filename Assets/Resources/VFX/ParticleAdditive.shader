// Textured additive particle shader for runtime-authored VFX.
// Kept in Resources so builds always include it even though materials are
// created from code instead of assets.
Shader "MECH ROUGE/Particle Additive"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Tint ("Tint", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "Queue"="Transparent+20" "RenderType"="Transparent" "IgnoreProjector"="True" "PreviewType"="Plane" }
        Blend SrcAlpha One
        ZWrite Off
        Cull Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct Input { float4 vertex : POSITION; float2 uv : TEXCOORD0; float4 color : COLOR; };
            struct Output { float4 vertex : SV_POSITION; float2 uv : TEXCOORD0; float4 color : COLOR; };
            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _Tint;
            Output vert(Input v)
            {
                Output o; o.vertex=UnityObjectToClipPos(v.vertex);
                o.uv=TRANSFORM_TEX(v.uv,_MainTex); o.color=v.color*_Tint; return o;
            }
            float4 frag(Output i) : SV_Target
            {
                float4 tex=tex2D(_MainTex,i.uv);
                return float4(i.color.rgb*tex.rgb,i.color.a*tex.a);
            }
            ENDCG
        }
    }
}
