Shader "KSP/Alpha/Alpha Half Sphere" 
{
	Properties 
	{
		_TintColor ("Tint Color", Color) = (0.1, 0.82, 0.1, 0.5)
		_StartFadeFraction ("Start Fade At Fraction", Range(0.0, 1.0)) = 0.25
		_MaxOpacity("Maximum Opacity", Range(0.0, 1.0)) = 0.75
		_Fresnel("_Fresnel", Range(0,10)) = 1.0
	}

	SubShader 
	{
		Tags{ "Queue" = "Transparent"}
		Blend SrcAlpha OneMinusSrcAlpha
		ZWrite Off

		CGPROGRAM
		#include "../LightingKSP.cginc"
		#pragma surface surf BlinnPhongSmooth alpha:fade
		#pragma target 3.0

		fixed4 _TintColor;
		float4 _SphereCentre;
		float4 _SphereUp;
		float _Radius;
		float _StartFadeFraction;
		float _MaxOpacity;
		float _Fresnel;

		struct Input
		{
			float3 worldPos;
			float3 viewDir;
		};

		void surf(Input IN, inout SurfaceOutput o)
		{
			o.Emission = _TintColor;

			//Calculate fresnel
			float3 normal = float3(0, 0, 1);
			half rim = saturate(dot(normalize(IN.viewDir), normal));
			float fresnel = pow(1 - rim, _Fresnel);

			//Calculate colour that fades as we advance along the sphere
			float3 toPoint = IN.worldPos - (_SphereCentre - (_SphereUp * _Radius));
			float3 projection = (dot(toPoint, _SphereUp) / dot(_SphereUp, _SphereUp)) * _SphereUp;
			float fade = min(smoothstep(_StartFadeFraction, 1.0f, saturate(length(projection) / (_Radius * 2.0f))), _MaxOpacity);

			o.Normal = normal;
			o.Alpha = min(fade, fresnel);
		}
		ENDCG 
	}
	Fallback "Standard"
}