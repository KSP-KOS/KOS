Shader "KSP/Particles/Alpha Blended Emissive Cutout"
{
    Properties
    {
        _MainTex("Particle Texture", 2D) = "white" {}
        _Cutoff("Alpha cutoff", Range(0,1)) = 0.5
			[PerRendererData]_UnderwaterFogFactor("Underwater Fog Factor", Range(0,1)) = 0
    }

    Category
    {
        Tags{ "Queue" = "AlphaTest" "IgnoreProjector" = "True" "RenderType" = "TransparentCutout" }
        Cull Off

       Blend SrcAlpha OneMinusSrcAlpha

        SubShader
        {

            CGPROGRAM

			#include "../LightingKSP.cginc"
            #pragma surface surf Lambert alphatest:_Cutoff keepalpha
			#pragma target 3.0

            sampler2D _MainTex;

            struct Input
            {
                float2 uv_MainTex;
				float3 worldPos;
                float4 color : COLOR;
            };



            void surf(Input IN, inout SurfaceOutput o)
            {
                float4 tex = tex2D(_MainTex, IN.uv_MainTex);
                float4 col = tex *  IN.color;

				float4 fog = UnderwaterFog(IN.worldPos, col);

                o.Albedo = fog.rgb;
                o.Emission = col.rgb * (tex.a* IN.color.a);
                o.Alpha = col.a * fog.a;
            }

            ENDCG
        }

        Fallback "Diffuse"
    }
}
