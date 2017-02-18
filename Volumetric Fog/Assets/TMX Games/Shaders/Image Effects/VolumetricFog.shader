Shader "Hidden/Volumetric Fog" {
Properties {
	_Color1("Fog Color 1", Color) = (.7, .7, .7, 1)
	_Color2("Fog Color 2", Color) = (.7, .7, .7, 1)
	//_MainTex ("Base (RGB)", 2D) = "black" {}
	_Noise ("3D Noise", 3D) = "black" {}

	[Space]
	_Anim1 ("Animation Speed 1", Range(0.0, 1.0)) = .1
	_Anim2 ("Animation Speed 2", Range(0.0, 1.0)) = .1

	[Space]
	_Rays ("Rays", Range(1, 512)) = 32
	_MaxDistance ("MaxDistance", Range(10, 20000.0)) = 500
	_NearWeight ("Near Weight", Range(0.0, 1.0)) = .5

	[Space]
	_NearFade ("Near Fade", Range(0.0, .3)) = .1
	//_FarFade ("Far Fade", Range(0.0, 1.0)) = .9
	//_FarFadeTrans ("Far Fade Trans", Range(0.0, 1.0)) = .4

	[Space]
    _LightRays ("Light Samples", Range(1, 8)) = 4
    _LightDistance ("Light Distance", Range(10, 1000.0)) = 200
	//_StepSize ("Step Size", Range(0.01, 50.0)) = .25

	[Space]
	_Cutoff ("Cutoff", Range(0.0, 1.0)) = .25
	_CutoffNoise ("Cutoff Noise Amount", Range(0.0, 1.0)) = .25
	_Density ("Density", Range(0.0, 10.0)) = .25
	_Saturation ("Saturation", Range(0.0, 10.0)) = .25
	_Scale1 ("Scale1", Range(0.0, 1.0)) = .15
	_Scale2 ("Scale2", Range(0.0, 1.0)) = .15
	_AxisScale ("Axis Scale", Vector) = (1, 1, 1, 0)

	[Space]
    _ShadowScatter("Shadow Scattering Coeff", Range(0.0, .125)) = 0.008
    _Scatter("Scattering Coeff", Range(0.005, .075)) = 0.008
    _HGCoeff("Henyey-Greenstein", Range(0.0, 1.0)) = 0.5
    _HGAmount("Henyey-Greenstein Amount", Range(0.0, 2.0)) = 0.5
    _ShadowAmount("Shadow Falloff", Range(0.6, 2.0)) = 0.8
	_Extinct ("Extinction Coeff", Range(0.01, .5)) = 0.01
}

CGINCLUDE

	#include "UnityCG.cginc"

	uniform sampler2D_float _MainTex;
	uniform float4 _MainTex_TexelSize;

	uniform sampler2D_float _CameraDepthTexture;
	uniform float4 _CameraDepthTexture_TexelSize;
	uniform sampler3D _Noise;

	uniform float _Anim1;
	uniform float _Anim2;

	uniform int _Rays;
	uniform float _MaxDistance;
	uniform float _NearWeight;

	uniform float _NearFade;
	//uniform float _FarFade;
	//uniform float _FarFadeTrans;

	uniform int _LightRays;
	uniform float _LightDistance;

	//uniform float _StepSize;
	uniform float _Cutoff;
	uniform float _CutoffNoise;
	uniform float _Density;
	uniform float _Saturation;
	uniform float _Scale1;
	uniform float _Scale2;
	uniform float4 _AxisScale;

	uniform float _ShadowScatter;
	uniform float _ShadowAmount;
	uniform float _Scatter;
	uniform float _HGCoeff;
	uniform float _HGAmount;
	uniform float _Extinct;

	float4 _Color1;
	float4 _Color2;
	
	// for fast world space reconstruction
	uniform float4x4 _FrustumCornersWS;

	struct v2f
	{
		float4 pos : SV_POSITION;
		float2 uv : TEXCOORD0;
		float2 uv_depth : TEXCOORD1;
		float4 interpolatedRay : TEXCOORD2;
	};
	
	v2f vert (appdata_img v)
	{
		v2f o;
		half index = v.vertex.z;
		v.vertex.z = 0.1;
		o.pos = mul(UNITY_MATRIX_MVP, v.vertex);
		o.uv = v.texcoord.xy;
		o.uv_depth = v.texcoord.xy;
		
		#if UNITY_UV_STARTS_AT_TOP
		if (_MainTex_TexelSize.y < 0)
			o.uv.y = 1-o.uv.y;
		#endif				
		
		o.interpolatedRay = _FrustumCornersWS[(int)index];
		o.interpolatedRay.w = index;
		
		return o;
	}

	float HenyeyGreenstein(float cosine)
	{
		float g2 = _HGCoeff * _HGCoeff;
		return 0.5 * (1 - g2) / pow(1 + g2 - 2 * _HGCoeff * cosine, 1.5);
	}

	float Beer(float density)
	{
		return exp(-_Extinct * density);
	}

	float BeerPowder(float depth)
	{
		return exp(-_Extinct * depth) * (1 - exp(-_Extinct * 2 * depth));
	}

	float2 NoiseAtPoint(float3 pos)
	{
		pos = pos * .00075 * _AxisScale.xyz;
		float3 pos2 = pos *_Scale2;
		pos *= _Scale1;

		float3 offset = float3(.42, .353, .273) * _Time.x;
		float3 value = tex3D(_Noise, pos + offset * _Anim1).rgb;
		float3 value2 = tex3D(_Noise, pos2 - offset * _Anim2).rgb;

		value = lerp(value, value2, .5);
		value.y = lerp(value.x, value.y, .5);

		float cutoff = _Cutoff * (_CutoffNoise * value.b + 1.0 - _CutoffNoise);
		value.x = saturate((value.x - cutoff) * (1.0 - cutoff));
		value.x *= value.x;
		//value.x *= ;
		return value.rg;
		//return tex3D(_Noise, pos * .001 * _Scale.xyz).rg;
	}

	float LightMarch(float3 pos, float densityMultiplier)
	{
		float3 light = _WorldSpaceLightPos0.xyz;
		float depth = 0.0;

		float invLightRays = 1.0 / _LightRays;
		float3 lightStep = _LightDistance * invLightRays * light;

		pos += lightStep;

		//[unroll(2)] for (int s = 0; s < _LightRays; s++)
		UNITY_LOOP for (int s = 0; s < _LightRays; s++)
		{
			depth += (NoiseAtPoint(pos)) * invLightRays;
			//depth += (NoiseAtPoint(pos) - _Cutoff) * invLightRays;
			pos += lightStep;
		}

		//float3 lightStep = _LightDistance * .5 * light;
		//pos += lightStep;
		//depth += (NoiseAtPoint(pos)) * .5;
		//pos += lightStep;

		//depth += (NoiseAtPoint(pos)) * .5;
		//pos += lightStep;
		
		return BeerPowder(depth);
	}

	half4 ComputeFog (v2f i) : SV_Target
	{
		half4 sceneColor = tex2D(_MainTex, UnityStereoTransformScreenSpaceTex(i.uv));
		
		// Reconstruct world space position & direction
		// towards this screen pixel.
		float rawDepth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, UnityStereoTransformScreenSpaceTex(i.uv_depth));
		//float2 tOff = _MainTex_TexelSize.xy * 2.0;
		////float2 tOff = _CameraDepthTexture_TexelSize.xy * 4.0;
		//rawDepth = min(rawDepth, SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, UnityStereoTransformScreenSpaceTex(i.uv_depth + tOff)));
		//rawDepth = min(rawDepth, SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, UnityStereoTransformScreenSpaceTex(i.uv_depth - tOff)));
		//tOff.y *= -1.0;
		//rawDepth = min(rawDepth, SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, UnityStereoTransformScreenSpaceTex(i.uv_depth + tOff)));
		//rawDepth = min(rawDepth, SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, UnityStereoTransformScreenSpaceTex(i.uv_depth - tOff)));
		float dpth = Linear01Depth(rawDepth);
		float4 wsDir = dpth * i.interpolatedRay;
		float3 normalizedDir = normalize(i.interpolatedRay);
		float3 wsPos = _WorldSpaceCameraPos.xyz + wsDir.xyz;

		float3 light = _WorldSpaceLightPos0.xyz;
		float ldot = abs(dot(normalizedDir, light));
		//ldot *= ldot;
		//ldot = 1.0 - (1.0 - ldot) * (1.0 - ldot);
		//float ldot = dot(normalizedDir, light);
		float hg = HenyeyGreenstein(ldot);
		//hg = pow(hg, .5);

		float depth = dpth * _ProjectionParams.z;
		depth = (dpth < .99) ? min(depth, _MaxDistance) : _MaxDistance;
		float normalDist = _MaxDistance / _Rays;
		float depthMult = depth / _MaxDistance;
		int rays = max(depthMult * _Rays, 2);
		//int rays = _Rays;

		//int rayCount = min(_Rays, 512);

		float invRays = 1.0 / (float)rays;

		float3 startPos = _WorldSpaceCameraPos.xyz + normalizedDir * _ProjectionParams.y;
		float3 position = startPos;
		float fogDepth = 0.0;

		float stepSize = depth * invRays;
		float densityMultiplier = _Density * 128.0 * invRays * depthMult;

		float fogAccum = 0.0;
		half4 fogColor = 0.0;
		//fixed4 fogColor = 0.0;


		//[unroll(_Rays)] for (int i = 0; i < _Rays; i++)
		UNITY_LOOP for (int i = 0; i < rays; i++)
		{
			if (fogAccum > 1.0)
			{
				break;
			}

			float dist = (i * invRays);
			dist = lerp(dist, dist * dist, _NearWeight);
			dist *= depth;
			float fade = saturate(dist / _MaxDistance / _NearFade);
			fade *= fade/* * fade*/;
			//float farFade = saturate((dist - _FarFade * _MaxDistance) / (1.0 - _FarFade));
			//farFade *= farFade;

			float2 noise = NoiseAtPoint(position);
			//noise.x = saturate((noise.x + _Cutoff) * lerp(1.0, 1.2, farFade)) - _Cutoff;
			//noise.x = lerp(noise.x, max(_FarFadeTrans, noise.x), farFade);
			if (noise.x > 0.0)
			{
				float beerPowder = BeerPowder(fogDepth);
				//if (i == 30)
				//	return noise.x * densityMultiplier * (1.0 - beerPowder);

				float lightMarch = LightMarch(position, densityMultiplier) * fogDepth * 256.0 * invRays * beerPowder;
				float scatter = _Scatter * hg * lightMarch;
				fogColor += scatter * _HGAmount;

				float shadowAmount = saturate(_ShadowScatter * lightMarch);
				shadowAmount = 1.0 - (1.0 - shadowAmount) * (1.0 - shadowAmount);
				//float shadowAmount = _ShadowScatter * (.65 + .35 * (1.0 - hg)) * lightMarch;
				fogColor -= shadowAmount;
				fogColor = saturate(fogColor);
				//float shadowAmount = saturate(_ShadowScatter * .5 * (.5 + .5 * (1.0 - hg)) * lightMarch);
				//fogColor -= pow(shadowAmount, _ShadowAmount);

				fogDepth += noise.x * densityMultiplier * fade;
				//if (beerPowder * densityMultiplier < 1.0)
				{
					//fogColor += lerp(_Color1, _Color2, noiseAtPoint.y) * density * densityMultiplier * _Saturation * .025 * LightMarch(position, densityMultiplier);
					//fogColor += lerp(_Color1, _Color2, noise.y) * noise.x * densityMultiplier * _Saturation * .025 * Beer(fogDepth);
					//float colorMultiplier = lerp(noise.x, .25, farFade) * densityMultiplier * _Saturation * .05 * beerPowder * fade;
					//colorMultiplier *= lerp(1.0, .15, farFade);
					float colorMultiplier = noise.x * densityMultiplier * beerPowder * fade  * _Saturation * .05;
					fogAccum += colorMultiplier;
					fogColor += lerp(_Color1, _Color2, noise.y) * colorMultiplier;
				}
			}

			position = startPos + normalizedDir * dist;
			//position += rayStep;
		}



		//fogDepth = saturate(fogDepth);

		//fogColor += Beer(fogDepth) * sceneColor;
		//return fogColor;
		return float4(fogColor.xyz, Beer(fogDepth));

		//return lerp(_Color, sceneColor, Beer(fogDepth));
		//return lerp(fogColor, sceneColor, Beer(fogDepth));
	}

ENDCG

SubShader
{
	ZTest Always Cull Off ZWrite Off Fog { Mode Off }

	Pass
	{
		CGPROGRAM
		#pragma vertex vert
		#pragma fragment frag
		half4 frag (v2f i) : SV_Target { return ComputeFog (i); }
		ENDCG
	}

	Pass
	{
		CGPROGRAM

		#pragma vertex vert
		#pragma fragment fragBlur

		uniform float4 _Offset;

		float4 fragBlur(v2f i) : SV_Target
		{
			float4 sum = 0.0f;

			float2 offset = _Offset.xy;

			sum += tex2D(_MainTex, i.uv - 4.0f * offset) * 0.0162162162f;
			sum += tex2D(_MainTex, i.uv - 3.0f * offset) * 0.0540540541f;
			sum += tex2D(_MainTex, i.uv - 2.0f * offset) * 0.1216216216f;
			sum += tex2D(_MainTex, i.uv - 1.0f * offset) * 0.1945945946f;

			sum += tex2D(_MainTex, i.uv) * 0.2270270270f;

			sum += tex2D(_MainTex, i.uv + 1.0f * offset) * 0.1945945946f;
			sum += tex2D(_MainTex, i.uv + 2.0f * offset) * 0.1216216216f;
			sum += tex2D(_MainTex, i.uv + 3.0f * offset) * 0.0540540541f;
			sum += tex2D(_MainTex, i.uv + 4.0f * offset) * 0.0162162162f;

			return sum;
		}
		ENDCG
	}

	Pass
	{
		CGPROGRAM

		#pragma vertex vert
		#pragma fragment opacityBlit

		uniform sampler2D_float _FogTex;
		uniform sampler2D_float _SceneTex;

		float4 opacityBlit(v2f i) : SV_Target
		{
			float4 main = tex2D(_FogTex, i.uv);
			float4 scene = tex2D(_SceneTex, i.uv);

			//return scene;
			return lerp(main, scene, main.a);
		}
		ENDCG
	}
}

Fallback off

}
