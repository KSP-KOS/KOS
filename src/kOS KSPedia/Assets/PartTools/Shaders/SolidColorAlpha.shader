// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

Shader "Solid Color (Alpha)" {
	Properties 
	{
		_Color ("Color", Color) = (0.5, 0.5, 0.5, 0.5)
		[PerRendererData]_UnderwaterFogFactor("Underwater Fog Factor", Range(0,1)) = 0
	}
	SubShader
	{
		
		ZWrite On
        GrabPass { }
		Pass
		{
			Fog { Mode Off }
			Blend SrcAlpha OneMinusSrcAlpha

			CGPROGRAM

			#pragma vertex vert
			#pragma fragment frag
	
			fixed4 _Color;
			sampler2D _GrabTexture;

			float4 _LocalCameraPos;
			float4 _LocalCameraDir;
			float4 _UnderwaterFogColor;
			float _UnderwaterMinAlphaFogDistance;
			float _UnderwaterMaxAlbedoFog;
			float _UnderwaterMaxAlphaFog;
			float _UnderwaterAlbedoDistanceScalar;
			float _UnderwaterAlphaDistanceScalar;

			float4 UnderwaterFog(float3 worldPos, float3 color)
			{
				float3 toPixel = worldPos - _LocalCameraPos.xyz;
				float toPixelLength = length(toPixel);
				//float angleDot = dot(_LocalCameraDir.xyz, toPixel / toPixelLength);
				//angleDot = lerp(0.00000001, angleDot, saturate(sign(angleDot)));
				//float waterDist = -_LocalCameraPos.w / angleDot;
				//float dist = min(toPixelLength, waterDist);


				float underwaterDetection = _LocalCameraDir.w;
				float albedoLerpValue = underwaterDetection * (_UnderwaterMaxAlbedoFog * saturate(toPixelLength * _UnderwaterAlbedoDistanceScalar));
				float alphaFactor = 1 - underwaterDetection * (_UnderwaterMaxAlphaFog * saturate((toPixelLength - _UnderwaterMinAlphaFogDistance) * _UnderwaterAlphaDistanceScalar));

				return float4(lerp(color, _UnderwaterFogColor.rgb, albedoLerpValue), alphaFactor);
			}

			struct appdata
			{
				float4 vertex : POSITION;
			};
			struct v2f
			{
				float4 pos : SV_POSITION;
				float4 uv : TEXCOORD0;
				float3 worldPos : TEXCOORD1;
			};
			v2f vert (appdata v)
			{
				v2f o;
				o.pos = UnityObjectToClipPos(v.vertex);
				o.uv = o.pos;
				o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
				return o;
			}
			half4 frag(v2f i) : COLOR
			{
				float2 coord = 0.5 + 0.5 * i.uv.xy / i.uv.w;
				fixed4 tex = tex2D(_GrabTexture, float2(coord.x, 1 - coord.y));

				float4 rgba = fixed4(lerp(tex.rgb, _Color.rgb, _Color.a), 1);

				float4 fog = UnderwaterFog(i.worldPos, rgba.rgb);

				return float4(fog.rgb, rgba.a * fog.a);
			}
			ENDCG
		}
	}
}