Shader "MECH ROUGE/E01 Living Metal"
{
    Properties
    {
        _Color ("Surface", Color) = (1,1,1,1)
        _Metallic ("Metallic", Range(0,1)) = .7
        _Glossiness ("Smoothness", Range(0,1)) = .65
        [HDR] _EmissionColor ("Life Veins", Color) = (0,0,0,1)
        _Living ("Living Surface", Range(0,1)) = 0
        _Wear ("Paint Wear", Range(0,2)) = 0
        _LifePulse ("Life Pulse", Float) = 1
        _FlowTime ("Flow Time", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 250
        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows vertex:vert addshadow
        #pragma target 3.0
        #include "UnityCG.cginc"
        fixed4 _Color;
        half _Metallic, _Glossiness, _Living, _Wear, _LifePulse;
        half4 _EmissionColor;
        float _FlowTime;
        struct Input { float3 bindPosition; float4 color : COLOR; };
        void vert(inout appdata_full v, out Input o) { UNITY_INITIALIZE_OUTPUT(Input,o); o.bindPosition=v.vertex.xyz*100; o.color=v.color; }
        float hash(float3 p) { p=frac(p*.1031); p+=dot(p,p.yzx+33.33); return frac((p.x+p.y)*p.z); }
        float noise(float3 p)
        {
            float3 i=floor(p),f=frac(p); f=f*f*(3-2*f);
            return lerp(lerp(lerp(hash(i),hash(i+float3(1,0,0)),f.x),lerp(hash(i+float3(0,1,0)),hash(i+float3(1,1,0)),f.x),f.y),
                        lerp(lerp(hash(i+float3(0,0,1)),hash(i+float3(1,0,1)),f.x),lerp(hash(i+float3(0,1,1)),hash(i+float3(1,1,1)),f.x),f.y),f.z);
        }
        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            float n=noise(IN.bindPosition*115);
            float chips=smoothstep(.69,.80,n);
            float3 color=_Color.rgb;
            if (_Wear>.5) color=lerp(color,float3(.025,.030,.033),chips*.75);
            if (_Wear>1.5) color=lerp(color,float3(.012,.016,.017),saturate(IN.color.a*1.04+n*.13));
            float life=noise(IN.bindPosition*12+float3(0,-_FlowTime*.23,_FlowTime*.07));
            o.Albedo=color;
            o.Metallic=_Metallic;
            o.Smoothness=saturate(_Glossiness + _Living*(life-.5)*.13 - chips*_Wear*.035);
            o.Emission=_EmissionColor.rgb*max(.05,_LifePulse)*lerp(1,.83+life*.34,_Living);
            o.Alpha=1;
        }
        ENDCG
    }
    FallBack "Standard"
}
