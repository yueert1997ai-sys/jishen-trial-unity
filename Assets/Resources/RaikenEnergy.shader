Shader "Mech/RaikenEnergy"
{
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha One
        Cull Off
        ZWrite Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct AppData { float4 vertex : POSITION; float4 color : COLOR; };
            struct Varying { float4 position : SV_POSITION; float4 color : COLOR; };
            Varying vert(AppData v)
            {
                Varying o;
                o.position = UnityObjectToClipPos(v.vertex);
                o.color = v.color;
                return o;
            }
            half4 frag(Varying i) : SV_Target { return half4(i.color.rgb * 1.6, i.color.a); }
            ENDCG
        }
    }
}
