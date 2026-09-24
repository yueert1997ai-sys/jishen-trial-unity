Shader "Mech/EnemyWhiteArmor"
{
    Properties
    {
        _Color ("Armor tint", Color) = (1,1,1,1)
        _MainTex ("Armor detail", 2D) = "white" {}
        _Metallic ("Metallic", Range(0,1)) = 0.35
        _Glossiness ("Smoothness", Range(0,1)) = 0.48
        _EmissionMap ("Sensors", 2D) = "black" {}
        _EmissionColor ("Sensor light", Color) = (0,0,0,1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200
        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0
        sampler2D _MainTex, _EmissionMap;
        fixed4 _Color, _EmissionColor;
        half _Metallic, _Glossiness;
        struct Input { float2 uv_MainTex; float2 uv_EmissionMap; };
        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            fixed4 detail=tex2D(_MainTex,IN.uv_MainTex);
            half hi=max(detail.r,max(detail.g,detail.b)),lo=min(detail.r,min(detail.g,detail.b));
            half gray=dot(detail.rgb,half3(.2126,.7152,.0722));
            // Repaint colored armor; retain dark joints, panel lines and neutral surface detail.
            half paint=smoothstep(.045,.16,hi-lo)*smoothstep(.09,.27,hi);
            half white=lerp(gray,.70+.23*sqrt(saturate(gray)),paint);
            o.Albedo=white*_Color.rgb;
            o.Metallic=_Metallic;o.Smoothness=_Glossiness;o.Alpha=1;
            o.Emission=tex2D(_EmissionMap,IN.uv_EmissionMap).rgb*_EmissionColor.rgb;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
