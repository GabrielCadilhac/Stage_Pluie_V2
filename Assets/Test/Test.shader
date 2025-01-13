Shader "Unlit/Test"
{
    Properties
    {
        _SliceRange ("Slices", Range(0,16)) = 6
        _UVScale ("UVScale", Float) = 1.0
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
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float3 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
                float3 normal : NORMAL;
            };

            UNITY_DECLARE_TEX2DARRAY(_Textures);
            float _SliceRange;
            float _UVScale;
            sampler2D _MainTex;
            float4 _MainTex_ST;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.normal = v.normal;
                o.uv.xy = v.uv;
                o.uv.z  = 0;
                
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                if (i.normal.y < 0.0) // bottom
				    i.uv.z = 0;
				else if (i.normal.y > 0.0) // top
                    i.uv.z = 1;
                else if (i.normal.x > 0.0) // right
				    i.uv.z = 2;
				else if (i.normal.x < 0.0) // left
                    i.uv.z = 3;
                else if (i.normal.z > 0.0) // front
                    i.uv.z = 4;
				else if (i.normal.z < 0.0) // back
				    i.uv.z = 5;
				else
                    i.uv.z = 6;

                return UNITY_SAMPLE_TEX2DARRAY(_Textures, i.uv);
            }
            ENDCG
        }
    }
}
