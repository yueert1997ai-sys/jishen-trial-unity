Shader "Hidden/Mech/CombatBloom"
{
    Properties { _MainTex ("Source",2D)="white" {} }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        CGINCLUDE
        #include "UnityCG.cginc"
        sampler2D _MainTex, _Glow;
        float4 _Direction;
        float _Strength;
        half4 extract(v2f_img i):SV_Target
        {
            half3 c=tex2D(_MainTex,i.uv).rgb;
            half peak=max(c.r,max(c.g,c.b));
            half low=min(c.r,min(c.g,c.b));
            // Favor colored emission; avoid washing white armor and gray lunar ground.
            half mask=saturate((peak-.62)*3.0)*saturate((peak-low)*3.0);
            return half4(c*mask,1);
        }
        half4 blur(v2f_img i):SV_Target
        {
            half4 c=tex2D(_MainTex,i.uv)*.227027;
            c+=tex2D(_MainTex,i.uv+_Direction.xy*1.384615)*.316216;
            c+=tex2D(_MainTex,i.uv-_Direction.xy*1.384615)*.316216;
            c+=tex2D(_MainTex,i.uv+_Direction.xy*3.230769)*.070270;
            c+=tex2D(_MainTex,i.uv-_Direction.xy*3.230769)*.070270;
            return c;
        }
        half4 composite(v2f_img i):SV_Target
        { half4 c=tex2D(_MainTex,i.uv);c.rgb+=tex2D(_Glow,i.uv).rgb*_Strength;return c; }
        ENDCG
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment extract
            ENDCG
        }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment blur
            ENDCG
        }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment composite
            ENDCG
        }
    }
}
