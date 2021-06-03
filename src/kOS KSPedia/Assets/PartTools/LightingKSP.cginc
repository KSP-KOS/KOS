inline fixed4 LightingBlinnPhongSmooth(SurfaceOutput s, fixed3 lightDir, half3 viewDir, fixed atten)
{
	s.Normal = normalize(s.Normal);
	half3 h = normalize(lightDir + viewDir);

	fixed diff = max(0, dot(s.Normal, lightDir));

	float nh = max(0, dot(s.Normal, h));
	float spec = pow(nh, s.Specular*128.0) * s.Gloss;

	fixed4 c;
	c.rgb = (s.Albedo * _LightColor0.rgb * diff + _LightColor0.rgb * _SpecColor.rgb * spec) * (atten);
	c.a = s.Alpha + _LightColor0.a * _SpecColor.a * spec * atten;
	return c;
}




inline half4 LightingUnlit(SurfaceOutput s, half3 lightDir, half atten)
{
	// half diff = max (0, dot (s.Normal, lightDir));

	half4 c;
	c.rgb = s.Albedo;
	c.a = s.Alpha;
	return c;
}




inline half4 LightingUnlit_PrePass(SurfaceOutput s, half4 light)
{
	half4 c;
	c.rgb = s.Albedo;
	c.a = s.Alpha;
	return c;
}




fixed4 LightingNoLighting(SurfaceOutput s, fixed3 lightDir, fixed atten) { return fixed4(0, 0, 0, 0); }





float4 _Color;
half _LightBoost;
half4 LightingLightWrapped(SurfaceOutput s, half3 lightDir, half3 viewDir, half atten) {
	float3 w = _Color.rgb*0.5;
	half3 NdotL = dot(s.Normal, lightDir);

	//Specular term
	half3 h = normalize(lightDir + viewDir);
	s.Normal = normalize(s.Normal);
	float NdotH = dot(s.Normal, h);
	float spec = pow(max(NdotH, 0), s.Specular * 128.0) * s.Gloss;
	fixed3 specColor = _SpecColor.rgb * _LightColor0.rgb;

	half3 diff = NdotL * (1 - w) + w;
	half4 c;
	c.rgb = ((s.Albedo * _LightColor0.rgb * diff) + (specColor * spec)) * (atten * _LightBoost);
	c.a = s.Alpha + (_LightColor0.a * _SpecColor.a * spec * atten);
	return c;
}



float4 _LocalCameraPos;
float4 _LocalCameraDir;
float4 _UnderwaterFogColor;
float _UnderwaterMinAlphaFogDistance;
float _UnderwaterMaxAlbedoFog;
float _UnderwaterMaxAlphaFog;
float _UnderwaterAlbedoDistanceScalar;
float _UnderwaterAlphaDistanceScalar;
float _UnderwaterFogFactor;

float4 UnderwaterFog(float3 worldPos, float3 color)
{
	float3 toPixel = worldPos - _LocalCameraPos.xyz;
	float toPixelLength = length(toPixel); ///< Comment out the math--looks better without it.
	//float angleDot = dot(_LocalCameraDir.xyz, toPixel / toPixelLength);
	//angleDot = lerp(0.00000001, angleDot, saturate(sign(angleDot)));
	//float waterDist = -_LocalCameraPos.w / angleDot;
	//float dist = min(toPixelLength, waterDist);


	float underwaterDetection = _UnderwaterFogFactor * _LocalCameraDir.w; ///< sign(1 - sign(_LocalCameraPos.w));
	float albedoLerpValue = underwaterDetection * (_UnderwaterMaxAlbedoFog * saturate(toPixelLength * _UnderwaterAlbedoDistanceScalar));
	float alphaFactor = 1 - underwaterDetection * (_UnderwaterMaxAlphaFog * saturate((toPixelLength - _UnderwaterMinAlphaFogDistance) * _UnderwaterAlphaDistanceScalar));

	return float4(lerp(color, _UnderwaterFogColor.rgb, albedoLerpValue), alphaFactor);
}