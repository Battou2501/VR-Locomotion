Shader "Unlit/TerrainShader2"
{
	Properties
	{
		_BiomeMapTex ("Biome Map Texture", 2D) = "white" {}
		_NoiseTex ("Noise Texture", 2D) = "white" {}
		_LevelBaseTex ("Level Base Texture", 2D) = "white" {}
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
			
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
				fixed4 color : COLOR;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				UNITY_FOG_COORDS(1)
				float4 vertex : SV_POSITION;
				fixed level_mask : TEXCOORD1;
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
			
			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				o.level_mask = v.color.r;
				UNITY_TRANSFER_FOG(o,o.vertex);
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				// sample the texture
				fixed4 biome_map_col = tex2D(_BiomeMapTex, i.uv * _BiomeMapTex_ST.xy);

				fixed noise_col = tex2D(_NoiseTex, i.uv * _NoiseTex_ST.xy).r;
				
				fixed4 biome_1_col = tex2D(_Biome1Tex, i.uv * _Biome1Tex_ST.xy);
				fixed4 biome_2_col = tex2D(_Biome2Tex, i.uv * _Biome2Tex_ST.xy);
				fixed4 biome_3_col = tex2D(_Biome3Tex, i.uv * _Biome3Tex_ST.xy);
				fixed4 biome_4_col = tex2D(_Biome4Tex, i.uv * _Biome4Tex_ST.xy);
				fixed4 biome_base_col = tex2D(_BiomeBaseTex, i.uv * _BiomeBaseTex_ST.xy);
				fixed4 level_base_col = tex2D(_LevelBaseTex, i.uv * _LevelBaseTex_ST.xy);

				const fixed border = _BorderThickness;
				const fixed border_thickness_multiplier = 1.0 / _BorderThickness;
				const fixed border_low = _BorderLow;
				const fixed border_high_multiplier = 1.0 / (_BorderHigh - _BorderLow);
				
				fixed noise = 1.0 - noise_col;
				noise = 1.0 - noise*noise*noise;
				
				fixed level_mask = saturate((noise - (1.0 - i.level_mask*(1.0 + border))) * border_thickness_multiplier);
				
				fixed biome_layer_1_mask = saturate((biome_map_col.r-border_low) * border_high_multiplier);//biome_map_col.r;
				fixed biome_layer_2_mask = saturate((biome_map_col.g-border_low) * border_high_multiplier);//biome_map_col.g;
				fixed biome_layer_3_mask = saturate((biome_map_col.b-border_low) * border_high_multiplier);//biome_map_col.b;
				fixed biome_layer_4_mask = saturate((biome_map_col.a-border_low) * border_high_multiplier);//biome_map_col.a;

				biome_layer_1_mask = saturate((noise - (1.0 - biome_layer_1_mask*(1.0 + border))) * border_thickness_multiplier) * level_mask;//biome_map_col.r;
				biome_layer_2_mask = saturate((noise - (1.0 - biome_layer_2_mask*(1.0 + border))) * border_thickness_multiplier) * level_mask;//biome_map_col.g;
				biome_layer_3_mask = saturate((noise - (1.0 - biome_layer_3_mask*(1.0 + border))) * border_thickness_multiplier) * level_mask;//biome_map_col.b;
				biome_layer_4_mask = saturate((noise - (1.0 - biome_layer_4_mask*(1.0 + border))) * border_thickness_multiplier) * level_mask;//biome_map_col.a;



				
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