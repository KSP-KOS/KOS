Shader "KSP/Unlit"
{
	Properties 
	{
		_MainTex("_MainTex (RGB)", 2D) = "white" {}
		_Color("Color", Color) = (1,1,1,1)
		
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

		#pragma surface surf Unlit
		#pragma target 2.0

		sampler2D _MainTex;
		float4 _Color;

		float _Opacity;
		float _RimFalloff;
		float4 _RimColor;
		
		inline half4 LightingUnlit (SurfaceOutput s, half3 lightDir, half atten)
		{
            half diff = max (0, dot (s.Normal, lightDir));

            half4 c;
            c.rgb = s.Albedo;
            c.a = s.Alpha;
            return c;
        }

        inline half4 LightingUnlit_PrePass (SurfaceOutput s, half4 light)
		{
            half4 c;
            c.rgb = s.Albedo;
            c.a = s.Alpha;
            return c;
        }

		struct Input
		{
			float2 uv_MainTex;
			float2 uv_Emissive;
			float3 viewDir;
		};

		void surf (Input IN, inout SurfaceOutput o)
		{
			float4 color = tex2D(_MainTex,(IN.uv_MainTex));
			float3 normal = float3(0,0,1);

			half rim = 1.0 - saturate(dot (normalize(IN.viewDir), normal));

			float3 emission = (_RimColor.rgb * pow(rim, _RimFalloff)) * _RimColor.a;

			o.Albedo = _Color.rgb * color.rgb;
			o.Emission = emission;
			o.Gloss = 0;
			o.Specular = 0;
			o.Normal = normal;

			o.Emission *= _Opacity;
			o.Alpha = _Opacity;
		}
		ENDCG
	}
	Fallback "Diffuse"
}