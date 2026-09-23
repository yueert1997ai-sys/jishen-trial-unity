Shader "Mech/CombatContrast"
{
    SubShader
    {
        Tags { "Queue"="Transparent-1" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct Input { float4 vertex:POSITION; float4 color:COLOR; };
            struct Output { float4 vertex:SV_POSITION; float4 color:COLOR; };
            Output vert(Input v) { Output o;o.vertex=UnityObjectToClipPos(v.vertex);o.color=v.color;return o; }
            half4 frag(Output i):SV_Target { return half4(.014,.025,.075,i.color.a*.65); }
            ENDCG
        }
    }
}
