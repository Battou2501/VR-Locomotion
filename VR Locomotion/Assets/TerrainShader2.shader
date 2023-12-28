// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Unlit/TerrainShader2"
{
	Properties
	{
		_BiomeMapTex ("Biome Map Texture", 2D) = "white" {}
		[KeywordEnum(Biome_Map,UV_SETS)] _Use ("Biomes source", int) = 0
		_NoiseTex ("Noise Texture", 2D) = "white" {}
		_LevelBaseTex ("Level Base Texture", 2D) = "white" {}
		[KeywordEnum(UV,TRI_PLANAR)] _Map ("Level base texture mapping", int) = 0
		_TriPlanarBorderThickness("Tri planar border thickness", Range(0.001,2)) = 0.3
		_BiomeBaseTex ("Biome Base Layer Texture", 2D) = "white" {}
		_Biome1Tex ("Biome Layer 1 Texture", 2D) = "white" {}
		_Biome2Tex ("Biome Layer 2 Texture", 2D) = "white" {}
		_Biome3Tex ("Biome Layer 3 Texture", 2D) = "white" {}
		_Biome4Tex ("Biome Layer 4 Texture", 2D) = "white" {}
		
		_BorderThickness ("Biome Transition Border Thickness", Range(0.01, 1.0)) = 0.1
		
		_BorderLow ("Biome Transition Border Low Threshold", Range(0, 1.0)) = 0
		
		_BorderHigh ("Biome Transition Border High Threshold", Range(0, 1.0)) = 1
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
			#pragma target 3.0
			#pragma shader_feature _USE_BIOME_MAP _USE_UV_SETS
			#pragma shader_feature _MAP_UV _MAP_TRI_PLANAR
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
				float2 uv1 : TEXCOORD1;
				float2 uv2 : TEXCOORD2;
				float2 uv3 : TEXCOORD3;
				float2 uv4 : TEXCOORD4;
				float3 normal : NORMAL;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float2 uv_base : TEXCOORD1;
				UNITY_FOG_COORDS(1)
				float4 vertex : SV_POSITION;
				fixed level_mask : TEXCOORD2;
				fixed4 biome_map : TEXCOORD3;
				float3 normal : TEXCOORD4;
				float3 pos : TEXCOORD5;
			};

			sampler2D _BiomeMapTex;
			float4 _BiomeMapTex_ST;

			sampler2D _Biome1Tex;
			float4 _Biome1Tex_ST;

			sampler2D _Biome2Tex;
			float4 _Biome2Tex_ST;

			sampler2D _Biome3Tex;
			float4 _Biome3Tex_ST;

			sampler2D _Biome4Tex;
			float4 _Biome4Tex_ST;

			sampler2D _BiomeBaseTex;
			float4 _BiomeBaseTex_ST;

			sampler2D _LevelBaseTex;
			float4 _LevelBaseTex_ST;

			sampler2D _NoiseTex;
			float4 _NoiseTex_ST;

			fixed _BorderThickness;
			fixed _BorderLow; 
			fixed _BorderHigh;
			float _TriPlanarBorderThickness;

			fixed3 TriPlanarBlendWeightsConstantOverlap(const float3 normal) {

				fixed3 blend_weights = normal*normal;//or abs(normal) for linear falloff(and adjust BlendZone)
				const fixed maxBlend = max(blend_weights.x, max(blend_weights.y, blend_weights.z));
				
			    const fixed BlendZone = 1.0 - _TriPlanarBorderThickness;
				blend_weights = blend_weights - maxBlend*BlendZone;
				blend_weights = max(blend_weights, 0.0);
				blend_weights *= blend_weights;
				blend_weights *= blend_weights;
				blend_weights *= blend_weights;
				
				const fixed rcpBlend = 1.0 / (blend_weights.x + blend_weights.y + blend_weights.z);
				return blend_weights*rcpBlend;
			}

			
			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				o.uv_base = TRANSFORM_TEX(v.uv1, _LevelBaseTex);
				o.level_mask = saturate(v.uv2.y);
				o.biome_map = fixed4(v.uv3.x, v.uv3.y, v.uv4.x, v.uv4.y);
				o.normal = mul(unity_ObjectToWorld,v.normal);
				o.pos = mul(unity_ObjectToWorld,v.vertex).xyz-mul(unity_ObjectToWorld,float4(0,0,0,1)).xyz;
				UNITY_TRANSFER_FOG(o,o.vertex);
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{

				const fixed3  noise_col = tex2D(_NoiseTex, i.uv * _NoiseTex_ST.xy).rgb;
				
				#ifdef _MAP_TRI_PLANAR
			        fixed3 blend = TriPlanarBlendWeightsConstantOverlap(normalize(i.normal));
					
					fixed4 level_base_tri_planar = tex2D(_LevelBaseTex, i.pos.yz* _LevelBaseTex_ST.xy)*blend.x;
					level_base_tri_planar += tex2D(_LevelBaseTex, i.pos.xz* _LevelBaseTex_ST.xy)*blend.y;
					level_base_tri_planar += tex2D(_LevelBaseTex, i.pos.xy* _LevelBaseTex_ST.xy)*blend.z;

					const fixed4 level_base_col = level_base_tri_planar;
				#else
					const fixed4 level_base_col = tex2D(_LevelBaseTex, i.uv_base);
				#endif
				
				// sample the texture

				#ifdef _USE_BIOME_MAP
					const fixed4 biome_map_col  = tex2D(_BiomeMapTex, i.uv * _BiomeMapTex_ST.xy);
					const fixed biome_1_coef = biome_map_col.r;
					const fixed biome_2_coef = biome_map_col.g;
					const fixed biome_3_coef = biome_map_col.b;
					const fixed biome_4_coef = biome_map_col.a;
				#else
					const fixed biome_1_coef = i.biome_map.r;
					const fixed biome_2_coef = i.biome_map.g;
					const fixed biome_3_coef = i.biome_map.b;
					const fixed biome_4_coef = i.biome_map.a;
				#endif
				
				const fixed4 biome_1_col    = tex2D(_Biome1Tex, i.uv * _Biome1Tex_ST.xy);
				const fixed4 biome_2_col    = tex2D(_Biome2Tex, i.uv * _Biome2Tex_ST.xy);
				const fixed4 biome_3_col    = tex2D(_Biome3Tex, i.uv * _Biome3Tex_ST.xy);
				const fixed4 biome_4_col    = tex2D(_Biome4Tex, i.uv * _Biome4Tex_ST.xy);
				const fixed4 biome_base_col = tex2D(_BiomeBaseTex, i.uv * _BiomeBaseTex_ST.xy);

				const fixed border = _BorderThickness;
				const fixed border_thickness_multiplier = 1.0 / _BorderThickness;
				const fixed border_low = _BorderLow;
				const fixed border_high_multiplier = 1.0 / (_BorderHigh - _BorderLow);
				const fixed noise = 1.0- (1.0 - noise_col.r)*(1.0 - noise_col.r)*(1.0 - noise_col.r);
				
				fixed level_mask = saturate((i.level_mask-border_low) * border_high_multiplier);
				//level_mask = 1.0- ((1.0-level_mask)*(1.0-level_mask)); //CONSIDER
				level_mask = saturate((noise - (1.0 - level_mask*(1.0 + border))) * border_thickness_multiplier);
				
				fixed biome_layer_1_mask = saturate((biome_1_coef-border_low) * border_high_multiplier);
				fixed biome_layer_2_mask = saturate((biome_2_coef-border_low) * border_high_multiplier);
				fixed biome_layer_3_mask = saturate((biome_3_coef-border_low) * border_high_multiplier);
				fixed biome_layer_4_mask = saturate((biome_4_coef-border_low) * border_high_multiplier);

				biome_layer_1_mask = saturate((noise - (1.0 - biome_layer_1_mask*(1.0 + border))) * border_thickness_multiplier) * level_mask;
				biome_layer_2_mask = saturate((noise - (1.0 - biome_layer_2_mask*(1.0 + border))) * border_thickness_multiplier) * level_mask;
				biome_layer_3_mask = saturate((noise - (1.0 - biome_layer_3_mask*(1.0 + border))) * border_thickness_multiplier) * level_mask;
				biome_layer_4_mask = saturate((noise - (1.0 - biome_layer_4_mask*(1.0 + border))) * border_thickness_multiplier) * level_mask;



				
				fixed4 col = level_base_col * (1.0-level_mask) + biome_base_col * level_mask;

				col = col * (1.0-biome_layer_1_mask) + biome_1_col * biome_layer_1_mask;
				col = col * (1.0-biome_layer_2_mask) + biome_2_col * biome_layer_2_mask;
				col = col * (1.0-biome_layer_3_mask) + biome_3_col * biome_layer_3_mask;
				col = col * (1.0-biome_layer_4_mask) + biome_4_col * biome_layer_4_mask;
				
				// apply fog
				UNITY_APPLY_FOG(i.fogCoord, col);
				
				return col;
			}
			ENDCG
		}
	}
}