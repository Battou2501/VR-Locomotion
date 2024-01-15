Shader "Unlit/TreeLightShader"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
	}
	SubShader
	{
		Tags { "RenderType"="Opaque" "LightMode"="ForwardBase"}
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
				float2 uv : TEXCOORD0;
				float3 normal : NORMAL;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float3 viewDir : TEXCOORD1;
				float3 lightDir : TEXCOORD2;
				float3 normal : TEXCOORD3;
				UNITY_FOG_COORDS(1)
				float4 vertex : SV_POSITION;
			};

			sampler2D _MainTex;
			float4 _MainTex_ST;
			
			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);

				o.normal = normalize(mul(unity_ObjectToWorld,v.normal));
				o.viewDir = normalize(ObjSpaceViewDir(v.vertex));
				o.lightDir = normalize(_WorldSpaceLightPos0.xyz);
				
				UNITY_TRANSFER_FOG(o,o.vertex);
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				// sample the texture
				fixed4 col = tex2D(_MainTex, i.uv);

				fixed t = saturate(dot(i.normal, i.lightDir) * 0.5 + 0.5);
				
				//fixed n = 1.0-saturate(dot(fixed3(0,-1,0), i.normal)* 0.5 + 0.5);
				//n= 1-n;
				//n = n*n*n;
				//n = 1-n;

				//t = lerp(t,n,0.5);
				
				fixed coef = saturate(dot(-i.viewDir, i.lightDir)*1.75-0.75);
				
				fixed c1 = saturate(1-floor(t/(0.15-0.075*coef)));
				fixed c2 = saturate(1-floor(t/(0.45-0.25*coef))-c1);
				fixed c3 = saturate(1-floor(t/(0.9-0.6*coef))-c1-c2);
				fixed c4 = saturate(1-floor(t)-c1-c2-c3);
				
				//fixed n = dot(-i.viewDir, i.lightDir); 
				col = c4+c3*0.9 + c2*0.5+ c1*0.1;
				col.a = 1;
				// apply fog
				UNITY_APPLY_FOG(i.fogCoord, col);
				return col;
			}
			ENDCG
		}
	}
}