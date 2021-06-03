Shader "KSP/Alpha/CutoffBackground" 
	{
		Properties
		{
			_MainTex("Base (RGB) Trans (A)", 2D) = "white" {}
			_Color("Main Color", Color) = (1,1,1,1)
			_BackColor("Back Color", Color) = (1,1,1,1)
			_BackTex("Base (RGB) Trans (A)", 2D) = "white" {}
			_Cutoff("Alpha cutoff", Range(0,1)) = 0.5
				[PerRendererData]_RimFalloff("_RimFalloff", Range(0.01,5)) = 0.1
				[PerRendererData]_RimColor("_RimColor", Color) = (0,0,0,0)
				[PerRendererData]_TemperatureColor("_TemperatureColor", Color) = (0,0,0,0)
				[PerRendererData]_BurnColor("Burn Color", Color) = (1,1,1,1)
				[PerRendererData]_UnderwaterFogFactor("Underwater Fog Factor", Range(0,1)) = 0	
			_Opacity("Opacity", Range(0,1)) = 1.0
		}
		SubShader
		{
			Tags {"Queue" = "AlphaTest+50" "IgnoreProjector" = "True" "RenderType" = "Transparent"}						
			Cull Off
			LOD 300		
			Blend SrcAlpha OneMinusSrcAlpha
			ZWrite Off ColorMask RGB
			Pass 
			{ 
				Name "FORWARDBACK"
				Tags {"LightMode" = "ForwardBase"}				
				ZWrite On
				CGPROGRAM
				#pragma vertex vert 
				#pragma fragment frag						
				#pragma multi_compile_fog
				#pragma multi_compile_fwdbasealpha noshadow
				#define UNITY_INSTANCED_LOD_FADE
				#define UNITY_INSTANCED_SH
				#define UNITY_INSTANCED_LIGHTMAPSTS
				#include "UnityCG.cginc"
				#include "AutoLight.cginc"
				#include "Lighting.cginc"
				#include "../LightingKSP.cginc"					
				#pragma target 3.0					
				uniform sampler2D _BackTex;
				uniform float4 _BackTex_ST;
				float _Cutoff;
				float _RimFalloff;				
				float4 _RimColor;
				float4 _TemperatureColor;
				float4 _BurnColor;
				float4 _BackColor;
				float _Opacity;

				struct VertexOutput
				{
					float4 pos : SV_POSITION;
					float2 uv0 : TEXCOORD0;
					float4 tSpace0 : TEXCOORD1;
					float4 tSpace1 : TEXCOORD2;
					float4 tSpace2 : TEXCOORD3;
					#if UNITY_SHOULD_SAMPLE_SH
					half3 sh : TEXCOORD4; // SH
					#endif
					DECLARE_LIGHT_COORDS(5)
					#if SHADER_TARGET >= 30
					float4 lmap : TEXCOORD6;
					#endif
					UNITY_FOG_COORDS(5)
					DECLARE_LIGHT_COORDS(6)												
				};

				VertexOutput vert(appdata_full v)
				{
					VertexOutput o = (VertexOutput)0;
					o.uv0 = v.texcoord;						
					o.pos = UnityObjectToClipPos(v.vertex);						
					float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
					float3 worldNormal = UnityObjectToWorldNormal(v.normal);
					fixed3 worldTangent = UnityObjectToWorldDir(v.tangent.xyz);
					fixed tangentSign = v.tangent.w * unity_WorldTransformParams.w;
					fixed3 worldBinormal = cross(worldNormal, worldTangent) * tangentSign;
					o.tSpace0 = float4(worldTangent.x, worldBinormal.x, worldNormal.x, worldPos.x);
					o.tSpace1 = float4(worldTangent.y, worldBinormal.y, worldNormal.y, worldPos.y);
					o.tSpace2 = float4(worldTangent.z, worldBinormal.z, worldNormal.z, worldPos.z);
					#ifdef DYNAMICLIGHTMAP_ON
					o.lmap.zw = v.texcoord2.xy * unity_DynamicLightmapST.xy + unity_DynamicLightmapST.zw;
					#endif
					#ifdef LIGHTMAP_ON
					o.lmap.xy = v.texcoord1.xy * unity_LightmapST.xy + unity_LightmapST.zw;
					#endif
					// SH/ambient and vertex lights					
					#ifndef LIGHTMAP_ON
					#if UNITY_SHOULD_SAMPLE_SH && !UNITY_SAMPLE_FULL_SH_PER_PIXEL
					o.sh = 0;
					// Approximated illumination from non-important point lights
					#ifdef VERTEXLIGHT_ON
					o.sh += Shade4PointLights(
						unity_4LightPosX0, unity_4LightPosY0, unity_4LightPosZ0,
						unity_LightColor[0].rgb, unity_LightColor[1].rgb, unity_LightColor[2].rgb, unity_LightColor[3].rgb,
						unity_4LightAtten0, worldPos, worldNormal);
					#endif
					o.sh = ShadeSHPerVertex(worldNormal, o.sh);
					#endif
					#endif // !LIGHTMAP_ON

					COMPUTE_LIGHT_COORDS(o); // pass light cookie coordinates to pixel shader
					#ifdef FOG_COMBINED_WITH_TSPACE
					UNITY_TRANSFER_FOG_COMBINED_WITH_TSPACE(o, o.pos); // pass fog coordinates to pixel shader
					#elif defined (FOG_COMBINED_WITH_WORLD_POS)
					UNITY_TRANSFER_FOG_COMBINED_WITH_WORLD_POS(o, o.pos); // pass fog coordinates to pixel shader
					#else
					UNITY_TRANSFER_FOG(o, o.pos); // pass fog coordinates to pixel shader
					#endif
					return o;
				}

				float4 frag(VertexOutput IN, float vface : VFACE) :SV_Target
				{
					#ifdef FOG_COMBINED_WITH_TSPACE
					UNITY_EXTRACT_FOG_FROM_TSPACE(IN);
					#elif defined (FOG_COMBINED_WITH_WORLD_POS)
					UNITY_EXTRACT_FOG_FROM_WORLD_POS(IN);
					#else
					UNITY_EXTRACT_FOG(IN);
					#endif
					#ifdef FOG_COMBINED_WITH_TSPACE
					UNITY_RECONSTRUCT_TBN(IN);
					#else
					UNITY_EXTRACT_TBN(IN);
					#endif
					#ifndef USING_DIRECTIONAL_LIGHT
					fixed3 lightDir = normalize(UnityWorldSpaceLightDir(worldPos));
					#else
					fixed3 lightDir = _WorldSpaceLightPos0.xyz;
					#endif
					float3 worldPos = float3(IN.tSpace0.w, IN.tSpace1.w, IN.tSpace2.w);
					float3 worldViewDir = normalize(UnityWorldSpaceViewDir(worldPos));
					float3 viewDir = _unity_tbn_0 * worldViewDir.x + _unity_tbn_1 * worldViewDir.y + _unity_tbn_2 * worldViewDir.z;
					#if UNITY_VFACE_FLIPPED
					vface = -vface;
					#endif
					SurfaceOutput o = (SurfaceOutput)0;
					o.Albedo = 0.0;
					o.Emission = 0.0;
					o.Specular = 0.0;
					o.Alpha = 0.0;
					o.Gloss = 0.0;					
					o.Normal = fixed3(0, 0, 1);
						
					//Compute Surface values here.
					fixed4 b = tex2D(_BackTex, TRANSFORM_TEX(IN.uv0, _BackTex)) * _BackColor * _BurnColor;
					clip(b.a - _Cutoff);
					float4 fog = UnderwaterFog(worldPos, b);
					half rim = 1.0 - saturate(dot(normalize(viewDir), o.Normal));
					float3 emission = (_RimColor.rgb * pow(rim, _RimFalloff)) * _RimColor.a;
					emission += _TemperatureColor.rgb * _TemperatureColor.a;						
					o.Albedo = fog.rgb;
					o.Emission = emission * fog.a;	
					float alpha = b.a * fog.a;
					o.Alpha = min(alpha, _Opacity);
					if (vface < 0.5)
					{
						o.Normal.z *= -1.0;
					}

					// compute lighting & shadowing factor
					UNITY_LIGHT_ATTENUATION(atten, i, worldPos)
						
					float3 worldN;
					worldN.x = dot(_unity_tbn_0, o.Normal);
					worldN.y = dot(_unity_tbn_1, o.Normal);
					worldN.z = dot(_unity_tbn_2, o.Normal);
					worldN = normalize(worldN);
					o.Normal = worldN;
								
					// Setup lighting environment
					UnityGI gi;
					UNITY_INITIALIZE_OUTPUT(UnityGI, gi);
					gi.indirect.diffuse = 0;
					gi.indirect.specular = 0;
					gi.light.color = _LightColor0.rgb;
					gi.light.dir = lightDir;
					// Call GI (lightmaps/SH/reflections) lighting function
					UnityGIInput giInput;
					UNITY_INITIALIZE_OUTPUT(UnityGIInput, giInput);
					giInput.light = gi.light;
					giInput.worldPos = worldPos;
					giInput.atten = atten;
					#if defined(LIGHTMAP_ON) || defined(DYNAMICLIGHTMAP_ON)
					giInput.lightmapUV = IN.lmap;
					#else
					giInput.lightmapUV = 0.0;
					#endif
					#if UNITY_SHOULD_SAMPLE_SH && !UNITY_SAMPLE_FULL_SH_PER_PIXEL
					giInput.ambient = IN.sh;
					#else
					giInput.ambient.rgb = 0.0;
					#endif
					giInput.probeHDR[0] = unity_SpecCube0_HDR;
					giInput.probeHDR[1] = unity_SpecCube1_HDR;
					#if defined(UNITY_SPECCUBE_BLENDING) || defined(UNITY_SPECCUBE_BOX_PROJECTION)
					giInput.boxMin[0] = unity_SpecCube0_BoxMin; // .w holds lerp value for blending
					#endif
					#ifdef UNITY_SPECCUBE_BOX_PROJECTION
					giInput.boxMax[0] = unity_SpecCube0_BoxMax;
					giInput.probePosition[0] = unity_SpecCube0_ProbePosition;
					giInput.boxMax[1] = unity_SpecCube1_BoxMax;
					giInput.boxMin[1] = unity_SpecCube1_BoxMin;
					giInput.probePosition[1] = unity_SpecCube1_ProbePosition;
					#endif
					LightingLambert_GI(o, giInput, gi);
						
					// realtime lighting: call lighting function
					float4 c = 0;
					c += LightingLambert (o, gi);
					c.rgb += o.Emission;					
					UNITY_APPLY_FOG(_unity_fogCoord, c); // apply fog
					return c;						
				}
				ENDCG
			}

			Pass
			{
				Name "FORWARDBACK"
				Tags {"LightMode" = "ForwardAdd"}
				ZWrite Off Blend One One
				Blend SrcAlpha One
				CGPROGRAM
				#pragma vertex vert 
				#pragma fragment frag 						
				#pragma multi_compile_fog				
				#pragma multi_compile_fwdadd noshadow
				#include "UnityCG.cginc"
				#include "AutoLight.cginc"
				#include "Lighting.cginc"
				#include "../LightingKSP.cginc"					
				#pragma target 3.0					
				uniform sampler2D _BackTex;
				uniform float4 _BackTex_ST;
				float _Cutoff;
				float _RimFalloff;
				float4 _RimColor;
				float4 _TemperatureColor;
				float4 _BurnColor;
				float4 _BackColor;
				float _Opacity;

				struct VertexOutput
				{
					float4 pos : SV_POSITION;
					float2 uv0 : TEXCOORD0;
					float4 tSpace0 : TEXCOORD1;
					float4 tSpace1 : TEXCOORD2;
					float4 tSpace2 : TEXCOORD3;
					float3 worldPos : TEXCOORD4;
					UNITY_FOG_COORDS(5)
					DECLARE_LIGHT_COORDS(6)
				};

				VertexOutput vert(appdata_full v)
				{
					VertexOutput o = (VertexOutput)0;
					o.uv0 = v.texcoord;
					o.pos = UnityObjectToClipPos(v.vertex);
					float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
					float3 worldNormal = UnityObjectToWorldNormal(v.normal);
					fixed3 worldTangent = UnityObjectToWorldDir(v.tangent.xyz);
					fixed tangentSign = v.tangent.w * unity_WorldTransformParams.w;
					fixed3 worldBinormal = cross(worldNormal, worldTangent) * tangentSign;
					o.tSpace0 = float4(worldTangent.x, worldBinormal.x, worldNormal.x, worldPos.x);
					o.tSpace1 = float4(worldTangent.y, worldBinormal.y, worldNormal.y, worldPos.y);
					o.tSpace2 = float4(worldTangent.z, worldBinormal.z, worldNormal.z, worldPos.z);
					o.worldPos.xyz = worldPos;
					COMPUTE_LIGHT_COORDS(o); // pass light cookie coordinates to pixel shader
					UNITY_TRANSFER_FOG(o, o.pos); // pass fog coordinates to pixel shader
					return o;
				}

				float4 frag(VertexOutput IN, float vface : VFACE) :SV_Target
				{


					#if UNITY_VFACE_FLIPPED
					vface = -vface;
					#endif
					#ifdef FOG_COMBINED_WITH_TSPACE
					UNITY_EXTRACT_FOG_FROM_TSPACE(IN);
					#elif defined (FOG_COMBINED_WITH_WORLD_POS)
					UNITY_EXTRACT_FOG_FROM_WORLD_POS(IN);
					#else
					UNITY_EXTRACT_FOG(IN);
					#endif
					#ifdef FOG_COMBINED_WITH_TSPACE
					UNITY_RECONSTRUCT_TBN(IN);
					#else
					UNITY_EXTRACT_TBN(IN);
					#endif
					#ifndef USING_DIRECTIONAL_LIGHT
					fixed3 lightDir = normalize(UnityWorldSpaceLightDir(IN.worldPos));
					#else
					fixed3 lightDir = _WorldSpaceLightPos0.xyz;
					#endif
					float3 worldPos = IN.worldPos.xyz;
					float3 worldViewDir = normalize(UnityWorldSpaceViewDir(worldPos));
					float3 viewDir = _unity_tbn_0 * worldViewDir.x + _unity_tbn_1 * worldViewDir.y + _unity_tbn_2 * worldViewDir.z;
					SurfaceOutput o = (SurfaceOutput)0;
					o.Albedo = 0.0;
					o.Emission = 0.0;
					o.Specular = 0.0;
					o.Alpha = 0.0;
					o.Gloss = 0.0;
					o.Normal = fixed3(0, 0, 1);

					//Compute Surface values here.
					fixed4 b = tex2D(_BackTex, TRANSFORM_TEX(IN.uv0, _BackTex)) * _BackColor * _BurnColor;
					float4 fog = UnderwaterFog(worldPos, b);
					half rim = 1.0 - saturate(dot(normalize(viewDir), o.Normal));
					float3 emission = (_RimColor.rgb * pow(rim, _RimFalloff)) * _RimColor.a;
					emission += _TemperatureColor.rgb * _TemperatureColor.a;
					o.Albedo = fog.rgb;
					o.Emission = emission * fog.a;
					o.Alpha = min(b.a, _Opacity);
					if (vface < 0.5)
					{
						o.Normal.z *= -1.0;
					}
					clip(b.a - _Cutoff);

					// compute lighting & shadowing factor
					UNITY_LIGHT_ATTENUATION(atten, i, worldPos)

					float3 worldN;
					worldN.x = dot(_unity_tbn_0, o.Normal);
					worldN.y = dot(_unity_tbn_1, o.Normal);
					worldN.z = dot(_unity_tbn_2, o.Normal);
					worldN = normalize(worldN);
					o.Normal = worldN;
					
					// Setup lighting environment
					UnityGI gi;
					UNITY_INITIALIZE_OUTPUT(UnityGI, gi);
					gi.indirect.diffuse = 0;
					gi.indirect.specular = 0;
					gi.light.color = _LightColor0.rgb;
					gi.light.dir = lightDir;
					gi.light.color *= atten;

					float4 c = 0;
					c += LightingLambert(o, gi);
					UNITY_APPLY_FOG(_unity_fogCoord, c); // apply fog
					return c;
				}
				ENDCG
			}
			
			Pass
			{
				Name "FORWARDBACK"
				Tags {"LightMode" = "Meta"}
				Cull Off

				CGPROGRAM
				#pragma vertex vert 
				#pragma fragment frag						
				#pragma skip_variants FOG_LINEAR FOG_EXP FOG_EXP2
				#pragma shader_feature EDITOR_VISUALIZATION
				#define UNITY_INSTANCED_LOD_FADE
				#define UNITY_INSTANCED_SH
				#define UNITY_INSTANCED_LIGHTMAPSTS
				#include "UnityCG.cginc"
				#include "AutoLight.cginc"
				#include "Lighting.cginc"
				#include "../LightingKSP.cginc"		
				#include "UnityMetaPass.cginc"
				#pragma target 3.0					
				uniform sampler2D _BackTex;
				uniform float4 _BackTex_ST;
				float _Cutoff;
				float _RimFalloff;
				float4 _RimColor;
				float4 _TemperatureColor;
				float4 _BurnColor;
				float4 _BackColor;
				float _Opacity;

				struct VertexOutput
				{
					float4 pos : SV_POSITION;
					float2 uv0 : TEXCOORD0;
					float4 tSpace0 : TEXCOORD1;
					float4 tSpace1 : TEXCOORD2;
					float4 tSpace2 : TEXCOORD3;
					#ifdef EDITOR_VISUALIZATION
					float2 vizUV : TEXCOORD4;
					float4 lightCoord : TEXCOORD5;
					#endif					
				};

				VertexOutput vert(appdata_full v)
				{
					VertexOutput o = (VertexOutput)0;
					o.uv0 = v.texcoord;
					o.pos = UnityObjectToClipPos(v.vertex);
					#ifdef EDITOR_VISUALIZATION
					o.vizUV = 0;
					o.lightCoord = 0;
					if (unity_VisualizationMode == EDITORVIZ_TEXTURE)
						o.vizUV = UnityMetaVizUV(unity_EditorViz_UVIndex, v.texcoord.xy, v.texcoord1.xy, v.texcoord2.xy, unity_EditorViz_Texture_ST);
					else if (unity_VisualizationMode == EDITORVIZ_SHOWLIGHTMASK)
					{
						o.vizUV = v.texcoord1.xy * unity_LightmapST.xy + unity_LightmapST.zw;
						o.lightCoord = mul(unity_EditorViz_WorldToLight, mul(unity_ObjectToWorld, float4(v.vertex.xyz, 1)));
					}
					#endif
					float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
					float3 worldNormal = UnityObjectToWorldNormal(v.normal);
					fixed3 worldTangent = UnityObjectToWorldDir(v.tangent.xyz);
					fixed tangentSign = v.tangent.w * unity_WorldTransformParams.w;
					fixed3 worldBinormal = cross(worldNormal, worldTangent) * tangentSign;
					o.tSpace0 = float4(worldTangent.x, worldBinormal.x, worldNormal.x, worldPos.x);
					o.tSpace1 = float4(worldTangent.y, worldBinormal.y, worldNormal.y, worldPos.y);
					o.tSpace2 = float4(worldTangent.z, worldBinormal.z, worldNormal.z, worldPos.z);
					return o;
				}

				float4 frag(VertexOutput IN, float vface : VFACE) :SV_Target
				{


					#if UNITY_VFACE_FLIPPED
					vface = -vface;
					#endif
					#ifdef FOG_COMBINED_WITH_TSPACE
					UNITY_EXTRACT_FOG_FROM_TSPACE(IN);
					#elif defined (FOG_COMBINED_WITH_WORLD_POS)
					UNITY_EXTRACT_FOG_FROM_WORLD_POS(IN);
					#else
					UNITY_EXTRACT_FOG(IN);
					#endif
					#ifdef FOG_COMBINED_WITH_TSPACE
					UNITY_RECONSTRUCT_TBN(IN);
					#else
					UNITY_EXTRACT_TBN(IN);
					#endif

					float3 worldPos = float3(IN.tSpace0.w, IN.tSpace1.w, IN.tSpace2.w);
					#ifndef USING_DIRECTIONAL_LIGHT
					fixed3 lightDir = normalize(UnityWorldSpaceLightDir(IN.worldPos));
					#else
					fixed3 lightDir = _WorldSpaceLightPos0.xyz;
					#endif
					float3 worldViewDir = normalize(UnityWorldSpaceViewDir(worldPos));
					float3 viewDir = _unity_tbn_0 * worldViewDir.x + _unity_tbn_1 * worldViewDir.y + _unity_tbn_2 * worldViewDir.z;
					SurfaceOutput o = (SurfaceOutput)0;
					o.Albedo = 0.0;
					o.Emission = 0.0;
					o.Specular = 0.0;
					o.Alpha = 0.0;
					o.Gloss = 0.0;					
					o.Normal = fixed3(0, 0, 1);

					//Compute Surface values here.
					fixed4 b = tex2D(_BackTex, TRANSFORM_TEX(IN.uv0, _BackTex)) * _BackColor * _BurnColor;
					float4 fog = UnderwaterFog(worldPos, b);
					half rim = 1.0 - saturate(dot(normalize(viewDir), o.Normal));
					float3 emission = (_RimColor.rgb * pow(rim, _RimFalloff)) * _RimColor.a;
					emission += _TemperatureColor.rgb * _TemperatureColor.a;
					o.Albedo = fog.rgb;
					o.Emission = emission * fog.a;
					o.Alpha = min(b.a, _Opacity);
					if (vface < 0.5)
					{
						o.Normal.z *= -1.0;
					}

					UnityMetaInput metaIN;
					UNITY_INITIALIZE_OUTPUT(UnityMetaInput, metaIN);
					metaIN.Albedo = o.Albedo;
					metaIN.Emission = o.Emission;
					metaIN.SpecularColor = o.Specular;
					#ifdef EDITOR_VISUALIZATION
					metaIN.VizUV = IN.vizUV;
					metaIN.LightCoord = IN.lightCoord;
					#endif
					return UnityMetaFragment(metaIN);
				}
				ENDCG
			}
						
			Pass
			{
				Name "FORWARDFLAG"
				Tags {"LightMode" = "ForwardBase"}
				Cull Off				
				ZWrite Off Blend One One
				Blend SrcAlpha OneMinusSrcAlpha				
				CGPROGRAM
				#pragma vertex vert 
				#pragma fragment frag							
				#pragma multi_compile_fog
				#pragma multi_compile_fwdbasealpha noshadow
				#define UNITY_INSTANCED_LOD_FADE
				#define UNITY_INSTANCED_SH
				#define UNITY_INSTANCED_LIGHTMAPSTS
				#include "UnityCG.cginc"
				#include "AutoLight.cginc"
				#include "Lighting.cginc"
				#include "../LightingKSP.cginc"					
				#pragma target 3.0					
				uniform sampler2D _MainTex;
				uniform float4 _MainTex_ST;
				float _Cutoff;
				float _RimFalloff;
				float4 _RimColor;
				float4 _TemperatureColor;
				float4 _BurnColor;
				float4 _BackColor;
				float _Opacity;

				struct VertexOutput
				{
					float4 pos : SV_POSITION;
					float2 uv0 : TEXCOORD0;
					float4 tSpace0 : TEXCOORD1;
					float4 tSpace1 : TEXCOORD2;
					float4 tSpace2 : TEXCOORD3;
					#if UNITY_SHOULD_SAMPLE_SH
					half3 sh : TEXCOORD4; // SH
					#endif
					DECLARE_LIGHT_COORDS(5)
					#if SHADER_TARGET >= 30
					float4 lmap : TEXCOORD6;
					#endif
					UNITY_FOG_COORDS(5)
					DECLARE_LIGHT_COORDS(6)
				};

				VertexOutput vert(appdata_full v)
				{
					VertexOutput o = (VertexOutput)0;
					o.uv0 = v.texcoord;
					o.pos = UnityObjectToClipPos(v.vertex);
					float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
					float3 worldNormal = UnityObjectToWorldNormal(v.normal);
					fixed3 worldTangent = UnityObjectToWorldDir(v.tangent.xyz);
					fixed tangentSign = v.tangent.w * unity_WorldTransformParams.w;
					fixed3 worldBinormal = cross(worldNormal, worldTangent) * tangentSign;
					o.tSpace0 = float4(worldTangent.x, worldBinormal.x, worldNormal.x, worldPos.x);
					o.tSpace1 = float4(worldTangent.y, worldBinormal.y, worldNormal.y, worldPos.y);
					o.tSpace2 = float4(worldTangent.z, worldBinormal.z, worldNormal.z, worldPos.z);
					#ifdef DYNAMICLIGHTMAP_ON
					o.lmap.zw = v.texcoord2.xy * unity_DynamicLightmapST.xy + unity_DynamicLightmapST.zw;
					#endif
					#ifdef LIGHTMAP_ON
					o.lmap.xy = v.texcoord1.xy * unity_LightmapST.xy + unity_LightmapST.zw;
					#endif
					// SH/ambient and vertex lights					
					#ifndef LIGHTMAP_ON
					#if UNITY_SHOULD_SAMPLE_SH && !UNITY_SAMPLE_FULL_SH_PER_PIXEL
					o.sh = 0;
					// Approximated illumination from non-important point lights
					#ifdef VERTEXLIGHT_ON
					o.sh += Shade4PointLights(
						unity_4LightPosX0, unity_4LightPosY0, unity_4LightPosZ0,
						unity_LightColor[0].rgb, unity_LightColor[1].rgb, unity_LightColor[2].rgb, unity_LightColor[3].rgb,
						unity_4LightAtten0, worldPos, worldNormal);
					#endif
					o.sh = ShadeSHPerVertex(worldNormal, o.sh);
					#endif
					#endif // !LIGHTMAP_ON

					COMPUTE_LIGHT_COORDS(o); // pass light cookie coordinates to pixel shader
					#ifdef FOG_COMBINED_WITH_TSPACE
					UNITY_TRANSFER_FOG_COMBINED_WITH_TSPACE(o, o.pos); // pass fog coordinates to pixel shader
					#elif defined (FOG_COMBINED_WITH_WORLD_POS)
					UNITY_TRANSFER_FOG_COMBINED_WITH_WORLD_POS(o, o.pos); // pass fog coordinates to pixel shader
					#else
					UNITY_TRANSFER_FOG(o, o.pos); // pass fog coordinates to pixel shader
					#endif
					return o;
				}

				float4 frag(VertexOutput IN, float vface : VFACE) :SV_Target
				{
					#ifdef FOG_COMBINED_WITH_TSPACE
					UNITY_EXTRACT_FOG_FROM_TSPACE(IN);
					#elif defined (FOG_COMBINED_WITH_WORLD_POS)
					UNITY_EXTRACT_FOG_FROM_WORLD_POS(IN);
					#else
					UNITY_EXTRACT_FOG(IN);
					#endif
					#ifdef FOG_COMBINED_WITH_TSPACE
					UNITY_RECONSTRUCT_TBN(IN);
					#else
					UNITY_EXTRACT_TBN(IN);
					#endif
					#ifndef USING_DIRECTIONAL_LIGHT
					fixed3 lightDir = normalize(UnityWorldSpaceLightDir(worldPos));
					#else
					fixed3 lightDir = _WorldSpaceLightPos0.xyz;
					#endif
					float3 worldPos = float3(IN.tSpace0.w, IN.tSpace1.w, IN.tSpace2.w);
					float3 worldViewDir = normalize(UnityWorldSpaceViewDir(worldPos));
					float3 viewDir = _unity_tbn_0 * worldViewDir.x + _unity_tbn_1 * worldViewDir.y + _unity_tbn_2 * worldViewDir.z;
					#if UNITY_VFACE_FLIPPED
					vface = -vface;
					#endif
					SurfaceOutput o = (SurfaceOutput)0;
					o.Albedo = 0.0;
					o.Emission = 0.0;
					o.Specular = 0.0;
					o.Alpha = 0.0;
					o.Gloss = 0.0;					
					o.Normal = fixed3(0, 0, 1);

					//Compute Surface values here.
					fixed4 b = tex2D(_MainTex, TRANSFORM_TEX(IN.uv0, _MainTex)) * _Color * _BurnColor;					
					float4 fog = UnderwaterFog(worldPos, b);
					half rim = 1.0 - saturate(dot(normalize(viewDir), o.Normal));
					float3 emission = (_RimColor.rgb * pow(rim, _RimFalloff)) * _RimColor.a;
					emission += _TemperatureColor.rgb * _TemperatureColor.a;
					o.Albedo = fog.rgb;
					o.Emission = emission * fog.a;
					float alpha = b.a * fog.a;
					o.Alpha = min(alpha, _Opacity);
					if (vface < 0.5)
					{
						o.Normal.z *= -1.0;
					}

					// compute lighting & shadowing factor
					UNITY_LIGHT_ATTENUATION(atten, i, worldPos)

					float3 worldN;
					worldN.x = dot(_unity_tbn_0, o.Normal);
					worldN.y = dot(_unity_tbn_1, o.Normal);
					worldN.z = dot(_unity_tbn_2, o.Normal);
					worldN = normalize(worldN);
					o.Normal = worldN;
					
					// Setup lighting environment
					UnityGI gi;
					UNITY_INITIALIZE_OUTPUT(UnityGI, gi);
					gi.indirect.diffuse = 0;
					gi.indirect.specular = 0;
					gi.light.color = _LightColor0.rgb;
					gi.light.dir = lightDir;
					// Call GI (lightmaps/SH/reflections) lighting function
					UnityGIInput giInput;
					UNITY_INITIALIZE_OUTPUT(UnityGIInput, giInput);
					giInput.light = gi.light;
					giInput.worldPos = worldPos;
					giInput.atten = atten;
					#if defined(LIGHTMAP_ON) || defined(DYNAMICLIGHTMAP_ON)
					giInput.lightmapUV = IN.lmap;
					#else
					giInput.lightmapUV = 0.0;
					#endif
					#if UNITY_SHOULD_SAMPLE_SH && !UNITY_SAMPLE_FULL_SH_PER_PIXEL
					giInput.ambient = IN.sh;
					#else
					giInput.ambient.rgb = 0.0;
					#endif
					giInput.probeHDR[0] = unity_SpecCube0_HDR;
					giInput.probeHDR[1] = unity_SpecCube1_HDR;
					#if defined(UNITY_SPECCUBE_BLENDING) || defined(UNITY_SPECCUBE_BOX_PROJECTION)
					giInput.boxMin[0] = unity_SpecCube0_BoxMin; // .w holds lerp value for blending
					#endif
					#ifdef UNITY_SPECCUBE_BOX_PROJECTION
					giInput.boxMax[0] = unity_SpecCube0_BoxMax;
					giInput.probePosition[0] = unity_SpecCube0_ProbePosition;
					giInput.boxMax[1] = unity_SpecCube1_BoxMax;
					giInput.boxMin[1] = unity_SpecCube1_BoxMin;
					giInput.probePosition[1] = unity_SpecCube1_ProbePosition;
					#endif
					LightingLambert_GI(o, giInput, gi);

					// realtime lighting: call lighting function
					float4 c = 0;
					c += LightingLambert(o, gi);
					c.rgb += o.Emission;
					UNITY_APPLY_FOG(_unity_fogCoord, c); // apply fog
					return c;
				}
				ENDCG
			}
							
			Pass
			{
				Name "FORWARDFLAG"
				Tags {"LightMode" = "ForwardAdd"}
				ZWrite Off Blend One One
				Blend SrcAlpha One
				CGPROGRAM
				#pragma vertex vert 
				#pragma fragment frag						
				#pragma multi_compile_fog				
				#pragma multi_compile_fwdadd noshadow
				#include "UnityCG.cginc"
				#include "AutoLight.cginc"
				#include "Lighting.cginc"
				#include "../LightingKSP.cginc"					
				#pragma target 3.0					
				uniform sampler2D _MainTex;
				uniform float4 _MainTex_ST;
				float _Cutoff;
				float _RimFalloff;
				float4 _RimColor;
				float4 _TemperatureColor;
				float4 _BurnColor;
				float4 _BackColor;
				float _Opacity;

				struct VertexOutput
				{
					float4 pos : SV_POSITION;
					float2 uv0 : TEXCOORD0;
					float4 tSpace0 : TEXCOORD1;
					float4 tSpace1 : TEXCOORD2;
					float4 tSpace2 : TEXCOORD3;
					float3 worldPos : TEXCOORD4;
					UNITY_FOG_COORDS(5)
					DECLARE_LIGHT_COORDS(6)
				};

				VertexOutput vert(appdata_full v)
				{
					VertexOutput o = (VertexOutput)0;
					o.uv0 = v.texcoord;
					o.pos = UnityObjectToClipPos(v.vertex);
					float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
					float3 worldNormal = UnityObjectToWorldNormal(v.normal);
					fixed3 worldTangent = UnityObjectToWorldDir(v.tangent.xyz);
					fixed tangentSign = v.tangent.w * unity_WorldTransformParams.w;
					fixed3 worldBinormal = cross(worldNormal, worldTangent) * tangentSign;
					o.tSpace0 = float4(worldTangent.x, worldBinormal.x, worldNormal.x, worldPos.x);
					o.tSpace1 = float4(worldTangent.y, worldBinormal.y, worldNormal.y, worldPos.y);
					o.tSpace2 = float4(worldTangent.z, worldBinormal.z, worldNormal.z, worldPos.z);
					o.worldPos.xyz = worldPos;
					COMPUTE_LIGHT_COORDS(o); // pass light cookie coordinates to pixel shader
					UNITY_TRANSFER_FOG(o, o.pos); // pass fog coordinates to pixel shader
					return o;
				}

				float4 frag(VertexOutput IN, float vface : VFACE) :SV_Target
				{
					#if UNITY_VFACE_FLIPPED
					vface = -vface;
					#endif
					#ifdef FOG_COMBINED_WITH_TSPACE
					UNITY_EXTRACT_FOG_FROM_TSPACE(IN);
					#elif defined (FOG_COMBINED_WITH_WORLD_POS)
					UNITY_EXTRACT_FOG_FROM_WORLD_POS(IN);
					#else
					UNITY_EXTRACT_FOG(IN);
					#endif
					#ifdef FOG_COMBINED_WITH_TSPACE
					UNITY_RECONSTRUCT_TBN(IN);
					#else
					UNITY_EXTRACT_TBN(IN);
					#endif
					#ifndef USING_DIRECTIONAL_LIGHT
					fixed3 lightDir = normalize(UnityWorldSpaceLightDir(IN.worldPos));
					#else
					fixed3 lightDir = _WorldSpaceLightPos0.xyz;
					#endif
					float3 worldPos = IN.worldPos.xyz;
					float3 worldViewDir = normalize(UnityWorldSpaceViewDir(worldPos));
					float3 viewDir = _unity_tbn_0 * worldViewDir.x + _unity_tbn_1 * worldViewDir.y + _unity_tbn_2 * worldViewDir.z;
					SurfaceOutput o = (SurfaceOutput)0;
					o.Albedo = 0.0;
					o.Emission = 0.0;
					o.Specular = 0.0;
					o.Alpha = 0.0;
					o.Gloss = 0.0;
					o.Normal = fixed3(0, 0, 1);

					//Compute Surface values here.
					fixed4 b = tex2D(_MainTex, TRANSFORM_TEX(IN.uv0, _MainTex)) * _Color * _BurnColor;
					float4 fog = UnderwaterFog(worldPos, b);
					half rim = 1.0 - saturate(dot(normalize(viewDir), o.Normal));
					float3 emission = (_RimColor.rgb * pow(rim, _RimFalloff)) * _RimColor.a;
					emission += _TemperatureColor.rgb * _TemperatureColor.a;
					o.Albedo = fog.rgb;
					o.Emission = emission * fog.a;
					o.Alpha = min(b.a, _Opacity);
					if (vface < 0.5)
					{
						o.Normal.z *= -1.0;
					}
					// compute lighting & shadowing factor
					UNITY_LIGHT_ATTENUATION(atten, i, worldPos)

					float3 worldN;
					worldN.x = dot(_unity_tbn_0, o.Normal);
					worldN.y = dot(_unity_tbn_1, o.Normal);
					worldN.z = dot(_unity_tbn_2, o.Normal);
					worldN = normalize(worldN);
					o.Normal = worldN;
					
					// Setup lighting environment
					UnityGI gi;
					UNITY_INITIALIZE_OUTPUT(UnityGI, gi);
					gi.indirect.diffuse = 0;
					gi.indirect.specular = 0;
					gi.light.color = _LightColor0.rgb;
					gi.light.dir = lightDir;
					gi.light.color *= atten;

					float4 c = 0;
					c += LightingLambert(o, gi);
					UNITY_APPLY_FOG(_unity_fogCoord, c); // apply fog
					return c;
				}
				ENDCG
			}
			
			Pass
			{
				Name "FORWARDFLAG"
				Tags {"LightMode" = "Meta"}
				Cull Off

				CGPROGRAM
				#pragma vertex vert 
				#pragma fragment frag					
				#pragma skip_variants FOG_LINEAR FOG_EXP FOG_EXP2
				#pragma shader_feature EDITOR_VISUALIZATION
				#define UNITY_INSTANCED_LOD_FADE
				#define UNITY_INSTANCED_SH
				#define UNITY_INSTANCED_LIGHTMAPSTS
				#include "UnityCG.cginc"
				#include "AutoLight.cginc"
				#include "Lighting.cginc"
				#include "../LightingKSP.cginc"		
				#include "UnityMetaPass.cginc"
				#pragma target 3.0					
				uniform sampler2D _MainTex;
				uniform float4 _MainTex_ST;
				float _Cutoff;
				float _RimFalloff;
				float4 _RimColor;
				float4 _TemperatureColor;
				float4 _BurnColor;
				float4 _BackColor;
				float _Opacity;

				struct VertexOutput
				{
					float4 pos : SV_POSITION;
					float2 uv0 : TEXCOORD0;
					float4 tSpace0 : TEXCOORD1;
					float4 tSpace1 : TEXCOORD2;
					float4 tSpace2 : TEXCOORD3;
					#ifdef EDITOR_VISUALIZATION
					float2 vizUV : TEXCOORD4;
					float4 lightCoord : TEXCOORD5;
					#endif					
				};

				VertexOutput vert(appdata_full v)
				{
					VertexOutput o = (VertexOutput)0;
					o.uv0 = v.texcoord;
					o.pos = UnityObjectToClipPos(v.vertex);
					#ifdef EDITOR_VISUALIZATION
					o.vizUV = 0;
					o.lightCoord = 0;
					if (unity_VisualizationMode == EDITORVIZ_TEXTURE)
						o.vizUV = UnityMetaVizUV(unity_EditorViz_UVIndex, v.texcoord.xy, v.texcoord1.xy, v.texcoord2.xy, unity_EditorViz_Texture_ST);
					else if (unity_VisualizationMode == EDITORVIZ_SHOWLIGHTMASK)
					{
						o.vizUV = v.texcoord1.xy * unity_LightmapST.xy + unity_LightmapST.zw;
						o.lightCoord = mul(unity_EditorViz_WorldToLight, mul(unity_ObjectToWorld, float4(v.vertex.xyz, 1)));
					}
					#endif
					float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
					float3 worldNormal = UnityObjectToWorldNormal(v.normal);
					fixed3 worldTangent = UnityObjectToWorldDir(v.tangent.xyz);
					fixed tangentSign = v.tangent.w * unity_WorldTransformParams.w;
					fixed3 worldBinormal = cross(worldNormal, worldTangent) * tangentSign;
					o.tSpace0 = float4(worldTangent.x, worldBinormal.x, worldNormal.x, worldPos.x);
					o.tSpace1 = float4(worldTangent.y, worldBinormal.y, worldNormal.y, worldPos.y);
					o.tSpace2 = float4(worldTangent.z, worldBinormal.z, worldNormal.z, worldPos.z);
					return o;
				}

				float4 frag(VertexOutput IN, float vface : VFACE) :SV_Target
				{
					#if UNITY_VFACE_FLIPPED
					vface = -vface;
					#endif
					#ifdef FOG_COMBINED_WITH_TSPACE
					UNITY_EXTRACT_FOG_FROM_TSPACE(IN);
					#elif defined (FOG_COMBINED_WITH_WORLD_POS)
					UNITY_EXTRACT_FOG_FROM_WORLD_POS(IN);
					#else
					UNITY_EXTRACT_FOG(IN);
					#endif
					#ifdef FOG_COMBINED_WITH_TSPACE
					UNITY_RECONSTRUCT_TBN(IN);
					#else
					UNITY_EXTRACT_TBN(IN);
					#endif

					float3 worldPos = float3(IN.tSpace0.w, IN.tSpace1.w, IN.tSpace2.w);
					#ifndef USING_DIRECTIONAL_LIGHT
					fixed3 lightDir = normalize(UnityWorldSpaceLightDir(IN.worldPos));
					#else
					fixed3 lightDir = _WorldSpaceLightPos0.xyz;
					#endif
					float3 worldViewDir = normalize(UnityWorldSpaceViewDir(worldPos));
					float3 viewDir = _unity_tbn_0 * worldViewDir.x + _unity_tbn_1 * worldViewDir.y + _unity_tbn_2 * worldViewDir.z;
					SurfaceOutput o = (SurfaceOutput)0;
					o.Albedo = 0.0;
					o.Emission = 0.0;
					o.Specular = 0.0;
					o.Alpha = 0.0;
					o.Gloss = 0.0;
					o.Normal = fixed3(0, 0, 1);

					//Compute Surface values here.
					fixed4 b = tex2D(_MainTex, TRANSFORM_TEX(IN.uv0, _MainTex)) * _Color * _BurnColor;
					float4 fog = UnderwaterFog(worldPos, b);
					half rim = 1.0 - saturate(dot(normalize(viewDir), o.Normal));
					float3 emission = (_RimColor.rgb * pow(rim, _RimFalloff)) * _RimColor.a;
					emission += _TemperatureColor.rgb * _TemperatureColor.a;
					o.Albedo = fog.rgb;
					o.Emission = emission * fog.a;
					o.Alpha = min(b.a, _Opacity);
					if (vface < 0.5)
					{
						o.Normal.z *= -1.0;
					}

					UnityMetaInput metaIN;
					UNITY_INITIALIZE_OUTPUT(UnityMetaInput, metaIN);
					metaIN.Albedo = o.Albedo;
					metaIN.Emission = o.Emission;
					metaIN.SpecularColor = o.Specular;
					#ifdef EDITOR_VISUALIZATION
					metaIN.VizUV = IN.vizUV;
					metaIN.LightCoord = IN.lightCoord;
					#endif
					return UnityMetaFragment(metaIN);
				}
				ENDCG
			}			
		}
		FallBack "Legacy Shaders/Transparent/Cutout/Diffuse"
	}