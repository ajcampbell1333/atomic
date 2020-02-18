Shader "Atomic/Pivot_SHADER"
{
	Properties
	{
		_TintColor("Tint Color", Color) = (1,1,1,1)
		_Transparency("Transparency", Range(0.0,1.0)) = 0.25
	}

		SubShader
	{
		Tags {"Queue" = "Transparent" "RenderType" = "Transparent" }
		LOD 100

		ZWrite Off
		Blend SrcAlpha OneMinusSrcAlpha

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
			};

			float4 _TintColor;
			float _Transparency;

			v2f vert(appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				return o;
			}

			fixed4 frag(v2f i) : SV_Target
			{
				fixed4 col = _TintColor;
				col.a = _Transparency;
				return col;
			}
			ENDCG
		}
		Pass
		{
			ZTest Always

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
			};

			float4 _TintColor;
			float _Transparency;

			v2f vert(appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				return o;
			}

			fixed4 frag(v2f i) : SV_Target
			{
				fixed4 col = _TintColor;
				col.a = _Transparency * 0.5;
				return col;
			}
			ENDCG
		}
	}
}