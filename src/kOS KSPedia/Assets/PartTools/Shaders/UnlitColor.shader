Shader "KSP/UnlitColor"
{
	Properties 
	{
		_Color("Color", Color) = (1,1,1,1)
	}
	
	SubShader 
	{
        Tags{ "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent" }

        Pass
        {
            ZWrite On
            ColorMask 0
        }
		Tags { "RenderType"="Opaque" }
		ZWrite On
		ZTest LEqual
		Blend SrcAlpha OneMinusSrcAlpha 

		CGPROGRAM
        #include "../LightingKSP.cginc"
		#pragma surface surf Unlit noforwardadd noshadow noambient novertexlights alpha:fade
		#pragma target 3.0
        struct Input
        {
            float4 color : COLOR; //vertex color
        };
		void surf (Input IN, inout SurfaceOutput o)
		{
			o.Albedo = _Color.rgb * IN.color.rgb;
			o.Alpha = _Color.a * IN.color.a;
		}
		ENDCG
	}
	Fallback "Unlit/Color"
}