Shader "KSP/Specular (Transparent)"
{
	Properties 
	{
        [Header(Texture Maps)]
		_MainTex("_MainTex (RGB spec(A))", 2D) = "white" {}
		_MainColor ("Main Color", Color) = (1,1,1,1)
        _Color("_Color", Color) = (1,1,1,1)
        [Header(Specularity)]
        _SpecColor("_SpecColor", Color) = (0.5, 0.5, 0.5, 1)
		_Shininess ("_Shininess", Range (0.03, 1)) = 0.4
        [Header(Transparency)]
		_Opacity("_Opacity", Range(0,1) ) = 1
		_Fresnel("_Fresnel", Range(0,10)) = 0
        [Header(Effects)]
		[PerRendererData]_RimFalloff("_RimFalloff", Range(0.01,5) ) = 0.1
			[PerRendererData]_RimColor("_RimColor", Color) = (0,0,0,0)
			[PerRendererData] _TemperatureColor("_TemperatureColor", Color) = (0,0,0,0)
			[PerRendererData]_BurnColor ("Burn Color", Color) = (1,1,1,1)
			[PerRendererData]_UnderwaterFogFactor("Underwater Fog Factor", Range(0,1)) = 0
	}
	
	SubShader 
	{
		Tags {"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent"}
		
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
		
		half _Shininess;

		sampler2D _MainTex;
		
		float4 _MainColor;
		float _Opacity;
		float _RimFalloff;
		float4 _RimColor;
		float4 _TemperatureColor;
		float4 _BurnColor;
		float _Fresnel;
		
		struct Input
		{
			float2 uv_MainTex;
			float3 viewDir;
			float3 worldNormal;
		};

		void surf (Input IN, inout SurfaceOutput o)
		{
			float4 color = tex2D(_MainTex,(IN.uv_MainTex)) * _MainColor * _BurnColor;
			half rim = 1.0 - saturate(dot (normalize(IN.viewDir), IN.worldNormal));

			float3 fresnel = pow(1 - rim, _Fresnel);

			float3 emission = (_RimColor.rgb * pow(rim, _RimFalloff)) * _RimColor.a;
			emission += _TemperatureColor.rgb * _TemperatureColor.a;

			o.Albedo = color.rgb * _Color.rgb;
			o.Emission = emission;
            o.Gloss = color.a;
            o.Specular = _Shininess;
			o.Emission *= _Opacity;
			o.Alpha = _Opacity * color.a * fresnel;
		}
		ENDCG
		
	}
	Fallback "Standard"
}