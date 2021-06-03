Shader "KSP/Alpha/Translucent"
{
	Properties 
	{
        [Header(Texture Maps)]
		_MainTex("MainTex (RGBA)", 2D) = "white" {}
		_Color("_Color", Color) = (1,1,1,1)
        [Header(Transparency)]
		[PerRendererData]_Opacity("_Opacity", Range(0,1)) = 1
		_Fresnel("_Fresnel", Range(0,10)) = 0
        [Header(Effects)]
		[PerRendererData]_RimFalloff("Rim Falloff", Range(0.01,5) ) = 0.1
			[PerRendererData]_RimColor("Rim Color", Color) = (0,0,0,0)
			[PerRendererData]_TemperatureColor("_TemperatureColor", Color) = (0,0,0,0)
			[PerRendererData]_BurnColor ("Burn Color", Color) = (1,1,1,1)
			[PerRendererData]_UnderwaterFogFactor("Underwater Fog Factor", Range(0,1)) = 0
	}
	
	SubShader 
	{
		Tags { "Queue" = "Transparent" }

		Pass
		{
			ZWrite On
			ColorMask 0
		}

		//ZWrite On
		ZTest LEqual
		Blend SrcAlpha OneMinusSrcAlpha 

		CGPROGRAM
        #include "../LightingKSP.cginc"
		#pragma surface surf BlinnPhongSmooth alpha:fade
		#pragma target 3.0

		sampler2D _MainTex;

		float _Opacity;
		float _RimFalloff;
		float _Fresnel;
		float _FresnelInvert;
		float4 _RimColor;
		float4 _TemperatureColor;
		float4 _BurnColor;

		
		struct Input
		{
			float2 uv_MainTex;
			float2 uv_BumpMap;
			float2 uv_Emissive;
			float3 viewDir;
		};

		void surf (Input IN, inout SurfaceOutput o)
		{
			float4 color = tex2D(_MainTex, (IN.uv_MainTex)) * _BurnColor * _Color;

			float3 normal = float3(0,0,1);
			half rim = 1.0 - saturate(dot (normalize(IN.viewDir), normal));

            float3 fresnel = pow(1 - rim, _Fresnel);

			float3 emission = (_RimColor.rgb * pow(rim, _RimFalloff)) * _RimColor.a;
			emission += _TemperatureColor.rgb * _TemperatureColor.a;

			o.Albedo = color.rgb;
			o.Emission = emission;
            o.Gloss = 0;
            o.Specular = 0;
			o.Normal = normal;
			o.Emission *= _Opacity;
			o.Alpha = _Opacity * color.a * fresnel;
		}

		ENDCG
	}
	Fallback "Standard"
}