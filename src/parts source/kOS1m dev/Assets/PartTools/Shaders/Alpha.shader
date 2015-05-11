Shader "KSP/Alpha/Translucent"
{
	Properties 
	{
		_MainTex("MainTex (RGBA)", 2D) = "white" {}

		_RimFalloff("Rim Falloff", Range(0.01,5) ) = 0.1
		_RimColor("Rim Color", Color) = (0,0,0,0)
	}
	
	SubShader 
	{
		Tags { "Queue" = "Transparent" }

		ZWrite On
		ZTest LEqual
		Blend SrcAlpha OneMinusSrcAlpha 

		CGPROGRAM

		#pragma surface surf Lambert alpha
		#pragma target 2.0

		sampler2D _MainTex;

		float _Opacity;
		float _RimFalloff;
		float4 _RimColor;
		
		struct Input
		{
			float2 uv_MainTex;
			float2 uv_BumpMap;
			float2 uv_Emissive;
			float3 viewDir;
		};

		void surf (Input IN, inout SurfaceOutput o)
		{
			float4 color = tex2D(_MainTex, (IN.uv_MainTex));

			float3 normal = float3(0,0,1);
			half rim = 1.0 - saturate(dot (normalize(IN.viewDir), normal));

			float3 emission = (_RimColor.rgb * pow(rim, _RimFalloff)) * _RimColor.a;

			o.Albedo = color.rgb;
			o.Emission = emission;
			o.Gloss = 0;
			o.Specular = 0;
			o.Normal = normal;

			o.Alpha = color.a;
		}

		ENDCG
	}
	Fallback "Diffuse"
}