Shader "Mech/M7 Machined Coating"
{
 Properties {
  _Color("Coating",Color)=(.57,.47,.32,1)
  _Metallic("Metal",Range(0,1))=.25
  _Glossiness("Smoothness",Range(0,1))=.38
  _Grain("Coating grain",Range(0,1))=.15
 }
 SubShader {
 Tags {"RenderType"="Opaque"} LOD 300
 CGPROGRAM
 #pragma surface surf Standard fullforwardshadows
 #pragma target 3.0
 struct Input {float3 worldPos;float3 worldNormal;};
 fixed4 _Color;half _Metallic,_Glossiness,_Grain;
 float hash(float3 p){p=frac(p*.1031);p+=dot(p,p.yzx+33.33);return frac((p.x+p.y)*p.z);}
 void surf(Input IN,inout SurfaceOutputStandard o){
  float3 p=mul(unity_WorldToObject,float4(IN.worldPos,1)).xyz;
  float grain=hash(floor(p*2400));
  float visibility=saturate(1-length(fwidth(p*2400))*.35);
  float fine=(grain-.5)*visibility;
  float brushed=sin(p.z*3800+sin(p.y*970)*2)*.5;
  brushed*=saturate(1-abs(fwidth(p.z*3800))*.3);
  float mottling=sin(p.x*129+sin(p.z*61))*sin(p.y*173+p.z*127);
  o.Albedo=_Color.rgb*(1+fine*_Grain+brushed*_Grain*.25+mottling*.025);
  o.Metallic=_Metallic;
  o.Smoothness=saturate(_Glossiness+fine*.13+brushed*.025);
  o.Occlusion=1;o.Alpha=1;
 }
 ENDCG
 }
 FallBack "Standard"
}
