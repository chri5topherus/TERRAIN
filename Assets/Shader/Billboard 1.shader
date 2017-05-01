Shader "Custom/UV Billboard" {
 Properties {
  _MainTex ("Albedo (RGB)", 2D) = "white" {}
  _Scale ("Scale", Float) = 1
  _Cutoff("Cutoff", Range(0,1)) = 0.5
 }
 
 /*
 //surface shader variant that I'm not using
 SubShader {
  Tags { "RenderType" = "Opaque" }
  
  CGPROGRAM
   #pragma surface surf Lambert vertex:vert addshadow
   
   sampler2D _MainTex;
   float _Cutoff;
   float _Scale;
   
   struct Input {
    float2 uv_MainTex;
   };
   
   void vert (inout appdata_full v)
   { 
    float3 eyeVector = ObjSpaceViewDir(v.vertex);
    
    float3 upVector = float3(0,1,0) * _Scale;
    float3 sideVector = normalize(cross(eyeVector, upVector)) * _Scale;
    
    float3 finalposition = v.vertex.xyz;
    finalposition += (v.texcoord.x - 0.5) * sideVector;
    finalposition += (v.texcoord.y) * upVector;
    
    float4 pos = float4(finalposition, 1);
    
    v.vertex = pos;
   }
   
   void surf(Input IN, inout SurfaceOutput o) {
    clip(tex2D(_MainTex, IN.uv_MainTex).a - _Cutoff);
    o.Albedo = tex2D(_MainTex, IN.uv_MainTex).rgb;
   }
  ENDCG
 }
 */
 
 
 SubShader {
  Pass{
  
   Tags { "RenderType"="Opaque"}
   
   CGPROGRAM
   #include "UnityCG.cginc"
   
   #pragma vertex vert
   #pragma fragment frag
   
   uniform sampler2D _MainTex;
   uniform float _Scale;
   uniform float _Cutoff;
   
   struct vertexOutput {
    float4 pos : SV_POSITION;
    float4 tex : TEXCOORD0;
   };
   
   vertexOutput vert(appdata_full input)
   {
    /*
     test stuff from 
     http://forum.unity3d.com/threads/billboard-shader-using-vertex-offsets-in-color-or-uv2-data.192652/#post-1312516
    */
   
    vertexOutput output;
    
    float3 eyeVector = ObjSpaceViewDir(input.vertex);
    
    float3 upVector = float3(0,1,0) * _Scale;
    float3 sideVector = normalize(cross(eyeVector, upVector)) * _Scale;
    
    float3 finalposition = input.vertex;
    finalposition += (input.texcoord.x - 0.5) * sideVector;
    finalposition += (input.texcoord.y) * upVector;
    
    float4 pos = float4(finalposition, 1);
    
    output.tex = input.texcoord;
    
    output.pos = mul(UNITY_MATRIX_MVP, pos);
    
    return output;
   }
   
   float4 frag(vertexOutput input) : COLOR
   {
    float4 color = tex2D(_MainTex, float2(input.tex.xy));
    clip(color.a - _Cutoff);
    return color;
   }
   ENDCG
  }
  
  //shadow pass, use light direction rather than view direction
  Pass{
  
   Tags {"LightMode" = "ShadowCaster"}
   
   CGPROGRAM
   #include "UnityCG.cginc"
   #include "AutoLight.cginc"
   
   #pragma vertex vert
   #pragma fragment frag
   
   uniform sampler2D _MainTex;
   uniform float _Scale;
   uniform float _Cutoff;
   
   struct vertexOutput {
    float4 pos : SV_POSITION;
    float4 tex : TEXCOORD0;
    
    LIGHTING_COORDS(1,2)
   };
   
   vertexOutput vert(appdata_full input)
   {
    /*
     test stuff from 
     http://forum.unity3d.com/threads/billboard-shader-using-vertex-offsets-in-color-or-uv2-data.192652/#post-1312516
    */
   
    vertexOutput output;
    
    //calculate direction to light
    // ripped from 
    // http://www.geekyhamster.com/2013/04/lighting-calculations-inside-unitys-cg.html
    float3 eyeVector = normalize(
     mul(unity_LightPosition[0], UNITY_MATRIX_IT_MV).xyz
    );
    //eyeVector = normalize(mul(unity_LightPosition[0],UNITY_MATRIX_IT_MV).xyz);
    
    float3 upVector = float3(0,1,0) * _Scale;
    float3 sideVector = normalize(cross(eyeVector, upVector)) * _Scale;
    
    float3 finalposition = input.vertex;
    finalposition += (input.texcoord.x - 0.5) * sideVector;
    finalposition += (input.texcoord.y) * upVector;
    
    float4 pos = float4(finalposition, 1);
    
    output.tex = input.texcoord;
    
    output.pos = mul(UNITY_MATRIX_MVP, pos);
    
    TRANSFER_VERTEX_TO_FRAGMENT(output);
    
    return output;
   }
   
   float4 frag(vertexOutput input) : COLOR
   {
    float atten = LIGHT_ATTENUATION(input);
    float4 color = tex2D(_MainTex, float2(input.tex.xy))*atten;
    clip(color.a - _Cutoff);
    return color;
   }
   ENDCG 
  }
 }
 
 Fallback "Diffuse"
}