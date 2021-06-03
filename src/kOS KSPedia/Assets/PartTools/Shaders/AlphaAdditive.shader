Shader "KSP/Alpha/Translucent Additive"
{
	Properties 
	{
		_MainTex("MainTex (RGBA)", 2D) = "white" {}
		_TintColor("TintColor", Color) = (1,1,1,1)
		_Fresnel("Fresnel", Range(0,10)) = 0
		//_FresnelInvert("Inverse Fresnel", Range(0,10)) = 0

			[PerRendererData]_UnderwaterFogFactor("Underwater Fog Factor", Range(0,1)) = 0
	}



	SubShader
	{
		Tags{ "Queue" = "Transparent" }

		ZWrite Off
			ZTest LEqual
			Blend SrcAlpha One

		CGPROGRAM

        #include "../LightingKSP.cginc"
		#pragma surface surf NoLighting noshadow noambient novertexlights nolightmap
		#pragma target 3.0


		sampler2D _MainTex;
		float _Fresnel;
		float _FresnelInvert;
		float4 _TintColor;

		struct Input
		{
			float2 uv_MainTex;
			float2 uv_BumpMap;
			float2 uv_Emissive;
			float3 viewDir;
		};

		void surf(Input IN, inout SurfaceOutput o)
		{
			float4 color = tex2D(_MainTex, (IN.uv_MainTex)) * _TintColor;

			float3 normal = float3(0,0,1);
			half rim = 1.0 - saturate(dot(normalize(IN.viewDir), normal));

			o.Albedo = 0;
			//o.Emission = color.rgb*(_FresnelColor+(_FresnelInvert + (pow(1 - rim, _Fresnel))) * (1 - _FresnelInvert + 1 - (pow(1 - rim, _Fresnel))));
			//o.Emission = color.rgb*(pow(1 - rim, _Fresnel))*((pow(rim, _FresnelInvert)));
			o.Emission = color.rgb * color.a * (pow(1 - rim, _Fresnel));

			o.Normal = normal;


		}

		ENDCG
	}
	Fallback "KSP/Particles/Additive"
		
}
