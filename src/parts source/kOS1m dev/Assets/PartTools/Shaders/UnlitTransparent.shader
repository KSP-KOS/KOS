Shader "KSP/Alpha/Unlit Transparent"
{
	Properties 
	{
		_MainTex("MainTex (RGB Alpha(A))", 2D) = "white" {}
		_Color("Color", Color) = (1,1,1,1)
	}
	
	SubShader 
	{
		ZWrite On
		ZTest LEqual
		Blend SrcAlpha OneMinusSrcAlpha 
		Cull Off 

		CGPROGRAM

		#pragma surface surf Unlit alpha
		#pragma target 2.0

		sampler2D _MainTex;
		float4 _Color;

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
        };

		void surf (Input IN, inout SurfaceOutput o)
		{
			float4 color = tex2D(_MainTex, (IN.uv_MainTex));
			float alpha = _Color.a * color.a;

			o.Albedo = _Color.rgb * color.rgb;
			o.Alpha = _Color.a * color.a;
		}
		ENDCG
	}
	Fallback "Diffuse"
}