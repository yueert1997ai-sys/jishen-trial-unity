Shader "Mech/NemesisGhost"
{
    Properties { _Color("Afterimage",Color)=(.30,.045,.65,.25) }
    SubShader
    {
        Tags { "Queue"="Transparent-10" "RenderType"="Transparent" }
        Blend SrcAlpha One
        ZWrite Off
        Cull Back
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            fixed4 _Color;
            struct App { float4 vertex:POSITION;float3 normal:NORMAL; };
            struct Vary { float4 vertex:SV_POSITION;float3 normal:TEXCOORD0;float3 world:TEXCOORD1; };
            Vary vert(App v)
            {
                Vary o;o.vertex=UnityObjectToClipPos(v.vertex);
                o.normal=UnityObjectToWorldNormal(v.normal);o.world=mul(unity_ObjectToWorld,v.vertex).xyz;return o;
            }
            fixed4 frag(Vary i):SV_Target
            {
                float rim=pow(1-saturate(abs(dot(normalize(i.normal),normalize(_WorldSpaceCameraPos-i.world)))),1.6);
                return fixed4(_Color.rgb*(.45+rim*1.4),_Color.a*(.12+rim*.88));
            }
            ENDCG
        }
    }
}
