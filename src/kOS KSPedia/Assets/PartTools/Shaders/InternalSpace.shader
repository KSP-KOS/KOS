Shader "KSP/InternalSpace"
{
	Properties 
	{
        [Header(Texture Maps)]
		_MainTex("_MainTex (RGB spec(A))", 2D) = "white" {}
		_BumpMap("_BumpMap", 2D) = "bump" {}
		_LightMap ("_LightMap", 2D) = "gray" {}

        [Header(Specular)]
		_SpecColor ("Specular Color", Color) = (0.5, 0.5, 0.5, 1)
        _Shininess ("Shininess", Range (0.03, 1)) = 0.4

        [Header(Lightmaps and Occlusion)]
        _LightColor1 ("_LightColor1", Color) = (0,0,0,0)
        _LightColor2 ("_LightColor2", Color) = (0,0,0,0)
        _LightAmbient("Ambient Boost", Range(0, 3)) = 1
        _Occlusion("Occlusion Tightness", Range(0, 3)) = 1
	}
	
	SubShader 
	{
        Tags{ "RenderType" = "Opaque" }
        LOD 200
		//ZWrite On
		//ZTest LEqual
		//Blend SrcAlpha OneMinusSrcAlpha 

		CGPROGRAM

        #include "../LightingKSP.cginc"

        #pragma surface surf BlinnPhongSmooth
		#pragma target 3.0

		sampler2D _MainTex;
		sampler2D _BumpMap;
		sampler2D _LightMap;

        uniform float _Shininess;
        uniform float4 _SpecularColor;

        uniform float4 _LightColor1;
        uniform float4 _LightColor2;
        uniform float _LightAmbient;
        uniform float _Occlusion;

		struct Input
		{
			float2 uv_MainTex;
            float4 color : COLOR;   //vertex color
			float2 uv_BumpMap;
			float2 uv2_LightMap;
		};

		void surf (Input IN, inout SurfaceOutput o)
		{

			float4 c = tex2D(_MainTex,(IN.uv_MainTex));

			float3 normal = UnpackNormal(tex2D(_BumpMap, IN.uv_BumpMap));

            float4 lightmap = tex2D(_LightMap, IN.uv2_LightMap);
            float3 light1 = lightmap.r * _LightColor1;
            float3 light2 = lightmap.g * _LightColor2;
			float3 light3 = lightmap.b * UNITY_LIGHTMODEL_AMBIENT * _LightAmbient;
			float3 AO = lerp(pow(lightmap.a, _Occlusion), 1, light1 + light2);
            float3 finalLight = (light1 + light2 + light3) * c.rgb * AO;

			o.Albedo = c.rgb * IN.color.rgb * AO;
			o.Gloss = c.a * _SpecColor * 2;
			o.Specular = _Shininess;
			o.Normal = normal;
			o.Alpha = 1;
            o.Emission = finalLight;
		}
		ENDCG
	}
	Fallback "Diffuse"
}