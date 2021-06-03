// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

Shader "KSP/Particles/Additive"
{
	Properties
	{
		_TintColor ("Tint Color", Color) = (0.5,0.5,0.5,0.5)
		_MainTex ("Particle Texture", 2D) = "white" {}
		_InvFade ("Soft Particles Factor", Range(0.01,3.0)) = 1.0
			[PerRendererData]_UnderwaterFogFactor("Underwater Fog Factor", Range(0,1)) = 0
	}

	Category
	{
		Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
		Blend SrcAlpha One
		AlphaTest Greater .01
		ColorMask RGB
		Cull Off Lighting Off ZWrite Off Fog { Color (0,0,0,0) }
		BindChannels {
			Bind "Color", color
			Bind "Vertex", vertex
			Bind "TexCoord", texcoord
	}
	
		// ---- Fragment program cards
		SubShader
		{
			Pass
			{
		
				CGPROGRAM
				#pragma vertex vert
				#pragma fragment frag
				#pragma fragmentoption ARB_precision_hint_fastest
				#pragma multi_compile_particles

				#include "UnityCG.cginc"

				sampler2D _MainTex;
				fixed4 _TintColor;
			

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

				struct appdata_t
				{
					float4 vertex : POSITION;
					fixed4 color : COLOR;
					float2 texcoord : TEXCOORD0;
				};

				struct v2f
				{
					float4 vertex : POSITION;
					fixed4 color : COLOR;
					float2 texcoord : TEXCOORD0;
					#ifdef SOFTPARTICLES_ON
					float4 projPos : TEXCOORD1;
					#endif
					float3 worldPos : TEXCOORD2;
				};
			
				float4 _MainTex_ST;

				v2f vert (appdata_t v)
				{
					v2f o;
					o.vertex = UnityObjectToClipPos(v.vertex);
					#ifdef SOFTPARTICLES_ON
					o.projPos = ComputeScreenPos (o.vertex);
					COMPUTE_EYEDEPTH(o.projPos.z);
					#endif
					o.color = v.color;
					o.texcoord = TRANSFORM_TEX(v.texcoord,_MainTex);

					o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;

					return o;
				}

				sampler2D _CameraDepthTexture;
				float _InvFade;
			
				fixed4 frag (v2f i) : COLOR
				{
					#ifdef SOFTPARTICLES_ON
					float sceneZ = LinearEyeDepth (UNITY_SAMPLE_DEPTH(tex2Dproj(_CameraDepthTexture, UNITY_PROJ_COORD(i.projPos))));
					float partZ = i.projPos.z;
					float fade = saturate (_InvFade * (sceneZ-partZ));
					i.color.a *= fade;
					#endif

					float4 rgba = 2.0f * i.color * _TintColor * tex2D(_MainTex, i.texcoord);

					float4 fog = UnderwaterFog(i.worldPos, rgba.rgb);

					return float4(fog.rgb, rgba.a * fog.a);
				}
				ENDCG 
			}
		} 	
	
		// ---- Dual texture cards
		SubShader {
			Pass {
				SetTexture [_MainTex] {
					constantColor [_TintColor]
					combine constant * primary
				}
				SetTexture [_MainTex] {
					combine texture * previous DOUBLE
				}
			}
		}
	
		// ---- Single texture cards (does not do color tint)
		SubShader
		{
			Pass
			{
				SetTexture [_MainTex] { 
					combine texture * primary 
				}
			}
		}
	}
}
