Shader "Unlit/Tex3DPreview"
{
	Properties
	{
		_MainTex ("Texture", 3D) = "white" {}
		_Scale ("Scale", Range(0.0, 10.0)) = 1.0
		_X ("X", Range(0.0, 1.0)) = 0.0
		_Y ("Y", Range(0.0, 1.0)) = 0.0
		_Z ("Z", Range(0.0, 1.0)) = 0.0
		_UseLOD ("UseLOD", int) = 0
	}
	SubShader
	{
		Tags { "RenderType"="Opaque" }
		LOD 100

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			// make fog work
			#pragma multi_compile_fog
			
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
			};

			struct v2f
			{
				float3 pos : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};

			sampler3D _MainTex;
			float _X;
			float _Y;
			float _Z;
			float _Scale;
			int _UseLOD;
			
			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.pos = mul(v.vertex, unity_ObjectToWorld) * _Scale;
				o.pos += float3(_X, _Y, _Z);
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				// sample the texture
				fixed4 col;
				if(_UseLOD)
					col = tex3Dlod(_MainTex, float4(i.pos.xyz, 0.0)).r;
				else
					col = tex3D(_MainTex, i.pos).r;
				return col;
			}
			ENDCG
		}
	}
}
