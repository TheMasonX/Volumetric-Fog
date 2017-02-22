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
    _LightOffset ("Light Offset Size", Range(0.0, 1.0)) = .025
    _LightFalloff ("Light Falloff", Range(0.0, 1.0)) = .025
	//_StepSize ("Step Size", Range(0.01, 50.0)) = .25

	[Space]
	_Cutoff ("Cutoff", Range(0.0, 1.0)) = .25
	_CutoffNoise ("Cutoff Noise Amount", Range(0.0, 1.0)) = .25
	_Density ("Density", Range(0.0, 1.0)) = .25
	_Sharpness ("Sharpness", Range(0.0, 1.0)) = .25
	_Saturation ("Saturation", Range(0.0, 1.0)) = .25
	_Scale1 ("Scale1", Range(0.0, 1.0)) = .15
	_Scale2 ("Scale2", Range(0.0, 1.0)) = .15
	_AxisScale ("Axis Scale", Vector) = (1, 1, 1, 0)

	[Space]
    _Scatter("Scattering Coeff", Range(0.005, .075)) = 0.008
    _HGCoeff("Henyey-Greenstein", Range(0.0, 0.999)) = 0.5
    _HGAmount("Henyey-Greenstein Amount", Range(0.0, 2.0)) = 0.5

	//_MieCoeff("Mie Coefficient", Range(-.999, 0.999)) = 0.5
 //   _MieAmount("Mie Amount", Range(0.0, 2.0)) = 0.5

	[Space]
	_ShadowMultiplier("Shadow Multiplier", Range(0.0, 1.0)) = .2
    _MaxShadow("Max Shadow", Range(0.0, 1.0)) = .9

	[Space]
	_LightAmount("Light Multiplier", Range(0.0, 2.0)) = .2
    _LightEmpty("Light Empty Cutoff", Range(0.0, 0.3)) = .05
    _MinLight("Min Light", Range(0.0, 1.0)) = .5
    _MaxLight("Max Light", Range(0.0, 1.0)) = .9

	[Space]
	_Extinct ("Extinction Coeff", Range(0.01, .5)) = 0.01
}

CGINCLUDE

	#include "UnityCG.cginc"

	uniform sampler2D_float _MainTex;
	uniform float4 _MainTex_TexelSize;

	uniform sampler2D_float _CameraDepthTexture;
	uniform float4 _CameraDepthTexture_TexelSize;
	uniform sampler3D _Noise;

	uniform half _Anim1;
	uniform half _Anim2;

	uniform int _Rays;
	uniform half _MaxDistance;
	uniform half _NearWeight;

	uniform half _NearFade;
	//uniform half _FarFade;
	//uniform half _FarFadeTrans;

	uniform int _LightRays;
	uniform half _LightDistance;
	uniform float _LightOffset;
	uniform float _LightFalloff;

	//uniform half _StepSize;
	uniform half _Cutoff;
	uniform half _CutoffNoise;
	uniform half _Density;
	uniform half _Sharpness;
	uniform half _Saturation;
	uniform float _Scale1;
	uniform float _Scale2;
	uniform float4 _AxisScale;

	uniform half _ShadowMultiplier;
	uniform half _MaxShadow;

	uniform half _LightAmount;
	uniform half _LightEmpty;
	uniform half _MinLight;
	uniform half _MaxLight;

	uniform half _Scatter;
	uniform half _HGCoeff;
	uniform half _HGAmount;
	uniform half _MieCoeff;
	uniform half _MieAmount;
	uniform half _Extinct;

	float4 _Color1;
	float4 _Color2;
	
	// for fast world space reconstruction
	uniform float4x4 _FrustumCornersWS;

	static const float3 Offsets[8] = {
		float3(-0.0373, 0.0015, 0.0126),
		float3(0.3152, 0.0882, 0.7360),
		float3(0.0871, 0.1706, -0.3436),
		float3(0.1184, 0.4140, 0.3061),
		float3(-0.8586, -0.3685, -0.0858),
		float3(0.0092, -0.0042, -0.0128),
		float3(-0.1204, -0.7397, 0.1588),
		float3(-0.1393, 0.8495, 0.2251),
	};

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

	inline float ValueNoise(float3 pos)
	{
		float3 Noise_skew = pos + 0.2127 + pos.x * pos.y * pos.z * 0.3713;
		float3 Noise_rnd = 4.789 * sin(489.123 * (Noise_skew));
		return frac(Noise_rnd.x * Noise_rnd.y * Noise_rnd.z * (1.0 + Noise_skew.x));
	}

	half HenyeyGreenstein(half cosine)
	{
		half g2 = _HGCoeff * _HGCoeff;
		//return 0.5 * (1.0 - g2) / pow(1.0 + g2 - 2.0 * _HGCoeff * cosine, 1.5);
		half base = 1.0 + g2 - 2.0 * _HGCoeff * cosine;
		return 0.5 * (1.0 - g2) / (base  * sqrt(base));
	}

	//half Mie(half cosine)
	//{
	//	half g2 = _MieCoeff * _MieCoeff;
	//	half left = (3.0 * 1.0 - g2) / (2.0 * (2.0 + g2));
	//	half base = 1.0 + g2 - 2.0 * _MieCoeff * cosine;
	//	return left * (1.0 - g2 / base * sqrt(base));
	//}

	half NoiseBeer(half density)
	{
		return exp(-_Density * density);
	}

	half Beer(half density)
	{
		return exp(-_Extinct * density);
	}

	half BeerPowder(half depth)
	{
		return exp(-_Extinct * depth) * (1 - exp(-_Extinct * 2 * depth));
	}

	float Coverage(float a, float sharpness)
	{
		a = 1.0 - exp(-(a - (1.0 - _Density)) * sharpness);
		return saturate(a);
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

		value.x = Coverage(value.x, lerp(1.0, value.b, _CutoffNoise) * _Sharpness);
		//value.x = 1.0 - Beer(value.x * _Density);

		//float cutoff = _Cutoff * (_CutoffNoise * value.b + 1.0 - _CutoffNoise);
		//value.x = saturate((value.x - cutoff) * (1.0 - cutoff));
		//value.x *= value.x;
		//value.x *= ;
		return value.rg;
		//return tex3D(_Noise, pos * .001 * _Scale.xyz).rg;
	}

	//float2 LightMarch(float3 pos, float rand)
	float2 LightMarch(float3 pos, float density)
	{
		float3 light = _WorldSpaceLightPos0.xyz;
		float depth = 0.0;
		float empty = 0.0;

		float invLightRays = 1.0 / _LightRays;
		float3 lightStep = _LightDistance * invLightRays * light;

		pos += lightStep;

		UNITY_LOOP for (int s = 0; s < _LightRays; s++)
		{
			//float percent = ((s + 1.0) * invLightRays);
			//percent = lerp(percent, percent * percent, _LightFalloff);

			//float3 newPos = pos + (light) * percent * _LightDistance;

			//float3 randomOffset = Offsets[(s + 16 * rand) % 8] * _LightOffset;
			//float3 newPos = pos + (light + randomOffset) * percent * _LightDistance;
			//float noise = NoiseAtPoint(newPos).x;

			float noise = NoiseAtPoint(pos).x;
			depth += noise;
			empty += 1.0 - saturate(noise / _LightEmpty);
			pos += lightStep;
		}

		//empty *= empty;
		empty = empty * invLightRays;
		//empty = saturate(1.0 - empty);
		empty = 1.0 - cos(1.570796327 * empty);

		return float2(BeerPowder(depth * invLightRays), empty);
		//return empty * invLightRays;
		//return BeerPowder(depth * invLightRays);
	}

	half4 ComputeFog (v2f i) : SV_Target
	{
		half4 sceneColor = tex2D(_MainTex, UnityStereoTransformScreenSpaceTex(i.uv));
		
		float rawDepth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, UnityStereoTransformScreenSpaceTex(i.uv_depth));
		float dpth = Linear01Depth(rawDepth);
		float4 wsDir = dpth * i.interpolatedRay;
		float3 normalizedDir = normalize(i.interpolatedRay);
		float3 wsPos = _WorldSpaceCameraPos.xyz + wsDir.xyz;

		float rand = 0;
		//float rand = ValueNoise(normalizedDir * 50.0);
		//return half4(rand.xxx, 0);

		float3 lightDir = _WorldSpaceLightPos0.xyz;
		half ldot = abs(dot(normalizedDir, lightDir));
		//ldot = 1.0 - (1.0 - ldot) * (1.0 - ldot);
		half hg = HenyeyGreenstein(ldot);

		float depth = dpth * _ProjectionParams.z;
		depth = (dpth < .99) ? min(depth, _MaxDistance) : _MaxDistance;
		half depthMult = depth / _MaxDistance;
		int rays = max(depthMult * _Rays, 2);
		//int rays = _Rays;

		half invRays = 1.0 / (float)rays;

		float3 startPos = _WorldSpaceCameraPos.xyz + normalizedDir * _ProjectionParams.y;
		float3 position = startPos;
		half fogDepth = 0.0;

		half stepSize = depth * invRays;
		half densityMultiplier = 256.0 * invRays * depthMult;

		half3 fogColor = 0.0;
		float light = 0.0;
		float shadow = 0.0;


		UNITY_LOOP for (int i = 0; i < rays; i++)
		{
			UNITY_BRANCH if (Beer(fogDepth) < 0.001)
			{
				break;
			}

			half dist = (i * invRays);
			dist = lerp(dist, dist * dist, _NearWeight);
			dist *= depth;
			half fade = saturate(dist / _MaxDistance / _NearFade);
			fade *= fade/* * fade*/;

			float2 noise = NoiseAtPoint(position);

			if (noise.x > 0.0)
			{
				half beerPowder = BeerPowder(fogDepth);
				half2 lightMarch = LightMarch(position, _Density) * 128.0;
				half lightDepth = lightMarch.x * fogDepth * beerPowder;
				light += lightMarch.y;

				half shadowAmount = lightDepth;
				//half shadowAmount = _ShadowMultiplier * (.65 + .35 * (1.0 - hg)) * lightMarch;
				//shadow += shadowAmount;

				shadowAmount = saturate(lightDepth * .2 * _ShadowMultiplier);
				shadowAmount = sin(1.570796327 * shadowAmount);
				shadowAmount = (1.0 - shadowAmount * _MaxShadow) + (1.0 - _MaxShadow);
				//shadowAmount *= _MaxShadow; 

				//fogColor -= shadowAmount;
				//fogColor = saturate(fogColor);

				half scatter = _Scatter * hg * lightDepth * _HGAmount;
				//fogColor += scatter * invRays;

				fogDepth += noise.x * densityMultiplier;

				half colorMultiplier = noise.x * densityMultiplier * beerPowder * fade * _Saturation * .5;
				//half colorMultiplier = noise.x * densityMultiplier * beerPowder * fade  * _Saturation * .05;
				half3 lerpedColor = lerp(_Color1.xyz, _Color2.xyz, noise.y) * colorMultiplier;
				float lightLerp = lerp(_MinLight, _MaxLight, lightMarch.y);
				lightLerp = lerp(1.0, lightLerp, _LightAmount);
				lightLerp += scatter;
				fogColor += lerpedColor * lightLerp * shadowAmount;
			}

			position = startPos + normalizedDir * dist;
			//position += rayStep;
		}

		//light = saturate(light * _LightAmount);
		//light *= light * (3.0 - 2.0 * light);
		//light = 1.0 - cos(1.570796327 * light);
		//light *= light;
		//light *= _MaxLight;

		//fogColor += light;

		//shadow = saturate(shadow * _ShadowMultiplier);
		//shadow = sin(1.570796327 * shadow);
		//shadow *= _MaxShadow;
		//
		//fogColor = lerp(fogColor.xyz, half3(0,0,0), shadow);

		//fogDepth = saturate(fogDepth);

		//fogColor += Beer(fogDepth) * sceneColor;
		//return fogColor;
		return half4(fogColor.xyz, Beer(fogDepth));

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

		uniform half4 _Offset;

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
