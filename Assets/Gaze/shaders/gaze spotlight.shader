Shader "Unlit/gaze spotlight"
{
	Properties
	{
		_Color ("Color", Color) = (0, 0, 0, 1)
		_Size("Size", float) = 50
		_Shadow("Shadow", float) = 0.8
	}
	SubShader
	{
		Tags{ "Queue" = "Transparent" }
		Blend SrcAlpha OneMinusSrcAlpha // Alpha blend
		LOD 100

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			#include "UnityCG.cginc"

			uniform float2 _GazePoint = float2(200, 200);

			float _Size = 50;
			float _Shadow = 0.8;

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};

			float4 _Color;
			float4 _MainTex_ST;
			
			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = mul(unity_ObjectToWorld, v.vertex).xyz;
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				float dist = distance(i.uv, _GazePoint.xy);
				float d2e = dist - _Size;
				if (d2e < 0) {
					return fixed4(_Color.x, _Color.y, _Color.z, 0);
				}
				else {
					float d = (d2e - 30) / 6;
					float alpha = 1 / (1 + exp(-d)) * _Shadow;
					return fixed4(_Color.x, _Color.y, _Color.z, alpha);
				}
			}
			ENDCG
		}
	}

	Fallback "Diffuse"
}
