Shader "KSP/FX/ScrollingUnlit"
{
	Properties 
	{
        _MainTex("MainTex (RGB Alpha(A))", 2D) = "white" {}
		_Color("Color (RGB Alpha(A))", Color) = (1,1,1,1)
        [Space]
		_SpeedX("Scroll Speed X", Float) = 0
        _SpeedY("Scroll Speed Y", Float) = 1
        [Space]
        [KeywordEnum(UV, Screen Space)] _TexCoord("Texture Coordinates", Float) = 0
        _TileX("ScreenSpace Tiling X", Float) = 1
        _TileY("ScreenSpace Tiling Y", Float) = 1
        [Space]
        [KeywordEnum(Off, Outer, Inner)] _Fresnel("Fresnel Fade", Float) = 0
        _FresnelPow("Fresnel Falloff", Range(0,5)) = 1
        _FresnelInt("Fresnel Intensity", Range(0,1)) = 1
			[PerRendererData]_UnderwaterFogFactor("Underwater Fog Factor", Range(0,1)) = 0
	}
	
	SubShader 
	{
		Tags{ "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent" }

		Pass
		{
			ZWrite On
			ColorMask 0
		}

		ZWrite On
		ZTest LEqual
		Blend SrcAlpha OneMinusSrcAlpha 

		CGPROGRAM

        #include "../LightingKSP.cginc"
		#pragma surface surf Unlit noforwardadd noshadow noambient novertexlights alpha:fade
		#pragma target 3.0
        #pragma multi_compile _TEXCOORD_UV _TEXCOORD_SCREEN_SPACE
        #pragma multi_compile _FRESNEL_OFF _FRESNEL_OUTER _FRESNEL_INNER


		sampler2D _MainTex;

        float _SpeedX;
        float _SpeedY;
        float _TileX;
        float _TileY;

        #if !_FRESNEL_OFF
        float _FresnelPow;
        float _FresnelInt;
        #endif

        struct Input
		{
            #if _TEXCOORD_UV
            float2 uv_MainTex;
            #endif

            #if _TEXCOORD_SCREEN_SPACE
            float4 screenPos;
            #endif

            #if !_FRESNEL_OFF
            float3 viewDir;
            #endif

			float3 worldPos;
        };



		void surf (Input IN, inout SurfaceOutput o)
		{
            #if _TEXCOORD_SCREEN_SPACE
            float2 screenUV = IN.screenPos.xy / IN.screenPos.w;
            screenUV.x += _Time * _SpeedX;
            screenUV.y += _Time * _SpeedY;
            screenUV.x *= _TileX;
            screenUV.y *= _TileY;
            float4 c = tex2D(_MainTex, (screenUV));
            #endif


            #if _TEXCOORD_UV
            fixed2 scrollUV = IN.uv_MainTex ;            
            fixed xScrollValue = _SpeedX * _Time.x;
            fixed yScrollValue = _SpeedY * _Time.x;
            scrollUV += fixed2(xScrollValue, yScrollValue);
            half4 c = tex2D(_MainTex, scrollUV);
            #endif
		    float3 normal = float3(0,0,1);

			float4 fog = UnderwaterFog(IN.worldPos, c.rgb * _Color.rgb);

			o.Albedo = fog.rgb;
			o.Normal = normal;
			o.Alpha = c.a * _Color.a * fog.a;

            #if _FRESNEL_INNER
            half rim = 1.0 - saturate(dot(normalize(IN.viewDir), float3(0, 0, 1)));
            o.Alpha *= lerp(pow(rim, _FresnelPow), 1, 1- _FresnelInt) * fog.a;
            #endif
            #if _FRESNEL_OUTER
            half rim = 1.0 - saturate(dot(normalize(IN.viewDir), float3(0, 0, 1)));
            o.Alpha *= lerp(pow(1 - rim, _FresnelPow), 1, 1 - _FresnelInt) * fog.a;
            #endif

		}
		ENDCG
	}
}