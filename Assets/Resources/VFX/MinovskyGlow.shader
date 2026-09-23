Shader "MECH ROUGE/Minovsky Glow"
{
    Properties
    {
        _Color ("Tint", Color) = (1,1,1,1)
        _Radial ("Radial falloff", Float) = 0
        _Pulses ("Traveling lobes", Float) = 0
        _Flow ("Lobe flow speed", Float) = 0
        _PulseAmp ("Lobe brightness boost", Float) = 0
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
            float _Pulses;
            float _Flow;
            float _PulseAmp;
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
                // Traveling energy lobes: bright packets streaming muzzle to impact,
                // the "beam is alive" motion of anime particle beams.
                float lobes=1;
                if (_Pulses>.5)
                {
                    float travel=frac(i.uv.x*_Pulses-_Time.y*_Flow);
                    lobes=1+_PulseAmp*pow(.5-.5*cos(travel*6.28318),6);
                }
                return float4(i.color.rgb*lobes,i.color.a*alpha*lerp(1,lobes,.35));
            }
            ENDCG
        }
    }
}
