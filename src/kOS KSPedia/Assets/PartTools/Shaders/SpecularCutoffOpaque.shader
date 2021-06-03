Shader "KSP/Specular Opaque (Cutoff)"
{
	Properties
	{
		_MainTex("_MainTex (RGB spec(A))", 2D) = "gray" {}
		_Color("MainColor", Color) = (1,1,1,1)
		_SpecColor("Specular Color", Color) = (0.5, 0.5, 0.5, 1)
		_Shininess("Shininess", Range(0.03, 1)) = 0.078125
		_Cutoff("Alpha cutoff", Range(0,1)) = 0.5
			[PerRendererData]_Opacity("_Opacity", Range(0,1)) = 1


			[PerRendererData]_RimFalloff("_RimFalloff", Range(0.01,5)) = 0.1
			[PerRendererData]_RimColor("_RimColor", Color) = (0,0,0,0)

			[PerRendererData]_TemperatureColor("_TemperatureColor", Color) = (0,0,0,0)
			[PerRendererData]_BurnColor("Burn Color", Color) = (1,1,1,1)
			[PerRendererData]_UnderwaterFogFactor("Underwater Fog Factor", Range(0,1)) = 0
	}

		SubShader
	{
		Tags{ "Queue" = "AlphaTest" "IgnoreProjector" = "True" "RenderType" = "Opaque" }
		ZWrite On
		ZTest LEqual
		Blend SrcAlpha OneMinusSrcAlpha

		CGPROGRAM



#include "../LightingKSP.cginc"
#pragma surface surf BlinnPhongSmooth  alphatest:_Cutoff
#pragma target 3.0

		half _Shininess;

	sampler2D _MainTex;

	float _Opacity;
	float _RimFalloff;
	float4 _RimColor;
	float4 _TemperatureColor;
	float4 _BurnColor;


	struct Input
	{
		float2 uv_MainTex;
		float3 viewDir;
		float3 worldPos;
	};

	void surf(Input IN, inout SurfaceOutput o)
	{
		float4 color = tex2D(_MainTex,(IN.uv_MainTex)) * _BurnColor * _Color;
		float3 normal = float3(0,0,1);

		half rim = 1.0 - saturate(dot(normalize(IN.viewDir), normal));
		float3 emission = (_RimColor.rgb * pow(rim, _RimFalloff)) * _RimColor.a;
		emission += _TemperatureColor.rgb * _TemperatureColor.a;

		float4 fog = UnderwaterFog(IN.worldPos, color);

		o.Albedo = fog.rgb;
		o.Emission = emission;
		o.Gloss = color.a;
		o.Specular = _Shininess;
		o.Normal = normal;

		o.Emission *= _Opacity * fog.a;
		o.Alpha = _Opacity * fog.a;
	}
	ENDCG
	}
		Fallback "Standard"
}