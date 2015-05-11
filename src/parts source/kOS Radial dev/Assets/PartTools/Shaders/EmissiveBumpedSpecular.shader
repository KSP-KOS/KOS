Shader "KSP/Emissive/Bumped Specular"
{
	Properties 
	{
		_MainTex("_MainTex (RGB spec(A))", 2D) = "white" {}
		_BumpMap("_BumpMap", 2D) = "bump" {}
		
		_SpecColor ("Specular Color", Color) = (0.5, 0.5, 0.5, 1)
		_Shininess ("Shininess", Range (0.03, 1)) = 0.078125

		_EmissiveColor("_EmissiveColor", Color) = (0,0,0,1)
		_Emissive("_Emissive", 2D) = "white" {}
		
		_Opacity("_Opacity", Range(0,1) ) = 1
		_RimFalloff("_RimFalloff", Range(0.01,5) ) = 0.1
		_RimColor("_RimColor", Color) = (0,0,0,0)
	}
	
	SubShader 
	{
		ZWrite On
		ZTest LEqual
		Blend SrcAlpha OneMinusSrcAlpha 

		CGPROGRAM

		#pragma surface surf BlinnPhong
		#pragma target 2.0
		
		half _Shininess;

		sampler2D _MainTex;
		sampler2D _BumpMap;

		float4 _EmissiveColor;
		sampler2D _Emissive;

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
			float4 color = tex2D(_MainTex,(IN.uv_MainTex));
			float3 normal = UnpackNormal(tex2D(_BumpMap, IN.uv_BumpMap));

			half rim = 1.0 - saturate(dot (normalize(IN.viewDir), normal));

			float3 emission = (_RimColor.rgb * pow(rim, _RimFalloff)) * _RimColor.a;
			emission += (tex2D(_Emissive, IN.uv_Emissive).rgb * _EmissiveColor.rgb) * _EmissiveColor.a;

			o.Albedo = color.rgb;
			o.Emission = emission;
			o.Gloss = color.a;
			o.Specular = _Shininess;
			o.Normal = normal;

			o.Emission *= _Opacity;
			o.Alpha = _Opacity;
		}
		ENDCG
	}
	Fallback "Diffuse"
}