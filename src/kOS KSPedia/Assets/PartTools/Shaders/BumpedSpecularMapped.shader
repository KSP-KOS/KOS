Shader "KSP/Bumped Specular (Mapped)"
{
	Properties 
	{
        [Header(Texture Maps)]
		_MainTex("Albedo (RGB)", 2D) = "gray" {}
		_BumpMap("Bump Map", 2D) = "bump" {}
        [Header(Specularity)]
		_SpecMap ("Specular Map", 2D) = "white"{}
		_SpecTint ("Specular Tint", Range (0, 0.1)) = 0.05
		_Shininess ("Shininess", Range (0.03, 1)) = 0.4
		_AmbientMultiplier("Ambient Multiplier", Range(0.00, 2)) = 1.0
        [Header(Effects)]
		[PerRendererData]_Opacity("_Opacity", Range(0,1) ) = 1
			[PerRendererData]_RimFalloff("_RimFalloff", Range(0.01,5) ) = 0.1
			[PerRendererData]_RimColor("_RimColor", Color) = (0,0,0,0)
			[PerRendererData]_TemperatureColor("_TemperatureColor", Color) = (0,0,0,0)
			[PerRendererData]_BurnColor ("Burn Color", Color) = (1,1,1,1)
			[PerRendererData]_UnderwaterFogFactor ("Underwater Fog Factor", Range(0,1)) = 0
	}
	
	SubShader 
	{
		Tags { "RenderType"="Opaque" }
		ZWrite On
		ZTest LEqual
		Blend SrcAlpha OneMinusSrcAlpha 
		ColorMask RGBA

		CGPROGRAM		
        #include "../LightingKSP.cginc"
        #pragma surface surf  StandardSpecular keepalpha
		#pragma target 3.0
		
		half _Shininess;
		half _SpecTint;

		sampler2D _MainTex;
		sampler2D _BumpMap;
		sampler2D _SpecMap;

		float _Opacity;
		float _RimFalloff;
		float4 _RimColor;
		float4 _TemperatureColor;
		float4 _BurnColor;

		float _SpecularAmbientBoostDiffuse;
		float _AmbientMultiplier;
		float _SpecularAmbientBoostEmissive;
		
		struct Input
		{
			float2 uv_MainTex;
			float2 uv_BumpMap;
			float2 uv_Emissive;
			float2 uv_SpecMap;
			float3 viewDir;
			float3 worldPos;
			float4 color : COLOR;
		};

		void surf (Input IN, inout SurfaceOutputStandardSpecular o)
		{
			float4 color = tex2D(_MainTex, (IN.uv_MainTex)) * _BurnColor * IN.color;
			color = color + color * _SpecularAmbientBoostDiffuse * _AmbientMultiplier;
			float3 normal = UnpackNormalDXT5nm(tex2D(_BumpMap, IN.uv_BumpMap));
			float3 specularMap = tex2D(_SpecMap,(IN.uv_SpecMap)).rgb;

			half rim = 1.0 - saturate(dot (normalize(IN.viewDir), normal));

			float3 emission = (_RimColor.rgb * pow(rim, _RimFalloff)) * _RimColor.a;
			emission += _TemperatureColor.rgb * _TemperatureColor.a;

			float4 fog = UnderwaterFog(IN.worldPos, color);

			o.Albedo = fog.rgb;
			o.Emission = emission + (specularMap * _SpecTint) + (color * _SpecularAmbientBoostEmissive);
		    //o.Gloss = color.a;
			o.Smoothness = _Shininess;
			o.Specular = specularMap;
			o.Normal = normal;
			o.Emission *= _Opacity * fog.a;
			o.Alpha = _Opacity * fog.a;
		}
		ENDCG
	}
	Fallback "Standard"
}