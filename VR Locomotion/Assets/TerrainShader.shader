Shader "Unlit/TerrainShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _BiomeMap ("Texture", 2D) = "white" {}
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
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _MainTex_TexelSize;

            sampler2D _BiomeMap;
            float4 _BiomeMap_ST;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);

                //float2 frac_uv = frac(v.uv * 0.999999);
                
                //frac_uv.x = floor(frac_uv.x);
                
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);

                //o.uv  = frac(o.uv * 0.25);
                
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // sample the texture
                
                const float gridSize = 4;
                const float margin = (1.0 / gridSize) / 4;//For 16x16 textures, margin of 1px, 32x32 => 2px etc.
                const float cellSize = (1.0 / gridSize) - (margin * 2);
                float blockId = int(0.3124 * gridSize * gridSize) / gridSize;
                float2 uv = i.uv*50;
    
                float2 dx = ddx(uv * cellSize);
                float2 dy = ddy(uv * cellSize);
    
                dx = float2(min(0.1,abs(dx.x)) * sign(dx.x), min(0.1,abs(dx.y)) * sign(dx.y));
                dy = float2(min(0.1,abs(dy.x)) * sign(dy.x), min(0.1,abs(dy.y)) * sign(dy.y));
                        
                fixed4 col = tex2Dgrad (_MainTex, frac(uv) * cellSize + float2(blockId + margin, floor(blockId) / gridSize + margin), dx, dy);
                
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }
}
