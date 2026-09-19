Shader "MECH ROUGE/Minovsky Glow"
{
    Properties
    {
        _Color ("Tint", Color) = (1,1,1,1)
        _Radial ("Radial falloff", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent+20" "RenderType"="Transparent" "IgnoreProjector"="True" }
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
            float4 _Color;
            float _Radial;
            Output vert(Input v)
            {
                Output o; o.vertex=UnityObjectToClipPos(v.vertex); o.uv=v.uv; o.color=v.color*_Color; return o;
            }
            float4 frag(Output i) : SV_Target
            {
                float acrossBeam=i.uv.y*2-1;
                float beamFalloff=pow(saturate(1-acrossBeam*acrossBeam),3);
                beamFalloff*=smoothstep(0,.015,i.uv.x)*(1-smoothstep(.965,1,i.uv.x));
                float2 p=i.uv*2-1;
                float disc=pow(saturate(1-dot(p,p)),3);
                float alpha=lerp(beamFalloff,disc,_Radial);
                return float4(i.color.rgb,i.color.a*alpha);
            }
            ENDCG
        }
    }
}
