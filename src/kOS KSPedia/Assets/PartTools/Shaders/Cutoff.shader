Shader "KSP/Alpha/Cutoff"
{
    Properties{
        _Color("Main Color", Color) = (1,1,1,1)
        _MainTex("Base (RGB) Trans (A)", 2D) = "white" {}
        _Cutoff("Alpha cutoff", Range(0,1)) = 0.5
			[PerRendererData]_RimFalloff("_RimFalloff", Range(0.01,5)) = 0.1
			[PerRendererData]_RimColor("_RimColor", Color) = (0,0,0,0)

			[PerRendererData]_TemperatureColor("_TemperatureColor", Color) = (0,0,0,0)
			[PerRendererData]_BurnColor("Burn Color", Color) = (1,1,1,1)
			[PerRendererData]_UnderwaterFogFactor("Underwater Fog Factor", Range(0,1)) = 0
    }

        SubShader{
        Tags{ "Queue" = "AlphaTest" "IgnoreProjector" = "True" "RenderType" = "TransparentCutout" }
        LOD 300

        CGPROGRAM
		#include "../LightingKSP.cginc"
        #pragma surface surf Lambert alphatest:_Cutoff
		#pragma target 3.0

        sampler2D _MainTex;
        sampler2D _BumpMap;
        float _RimFalloff;
        float4 _RimColor;
        float4 _TemperatureColor;
        float4 _BurnColor;

        struct Input {
        float2 uv_MainTex;
        float2 uv_Emissive;
        float3 viewDir;
		float3 worldPos;
        };

    void surf(Input IN, inout SurfaceOutput o)
    {
        fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color * _BurnColor;
        float3 normal = float3(0, 0, 1);
        half rim = 1.0 - saturate(dot(normalize(IN.viewDir), normal));
        float3 emission = (_RimColor.rgb * pow(rim, _RimFalloff)) * _RimColor.a;
        emission += _TemperatureColor.rgb * _TemperatureColor.a;


		float4 fog = UnderwaterFog(IN.worldPos, c);

        o.Albedo = fog.rgb;
        o.Emission = emission * fog.a;
        o.Alpha = c.a * fog.a;
        o.Normal = normal;
    }
    ENDCG
    }

        FallBack "Legacy Shaders/Transparent/Cutout/Diffuse"
}