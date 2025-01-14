Shader "Unlit/SplashShader"
{
    Properties
    {
        _MainTex   ("Texture", 2D)    = "" {}
        _Size      ("Size", Vector) = (.5,.5,0.,0.)
        _MainColor ("Color", Color) = (1., 1., 1.)
    }
    SubShader
    {
        // Tags pour faire le rendu des imposteurs
        Tags{ "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent" }

		ZWrite Off
		Blend SrcAlpha OneMinusSrcAlpha
        Cull Off

        Pass
        {
             
            CGPROGRAM
            #pragma require geometry
            #pragma require 2darray

            #pragma vertex vert
            #pragma fragment frag
            #pragma geometry geom

            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            uniform StructuredBuffer<float> TimeBuffer;

            struct appdata
            {
                float4 vertex : POSITION;
                uint vertexId : SV_VertexID;
                float3 normal : NORMAL;
            };

            struct v2g
            {
                float4 vertex : SV_POSITION;
                float2 vertexId : TEXCOORD0;
                float3 normal : NORMAL;
            };

            struct g2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR; 

                UNITY_FOG_COORDS(1)
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _MainColor;

            float2 _Size;

            v2g vert (appdata v)
            {
                v2g o;

                o.vertex     = v.vertex;
                o.normal     = v.normal;
                o.vertexId.x = v.vertexId;

                // Discard splash si la position est nulle
                if (TimeBuffer[v.vertexId] <= 0.0)
                    o.vertex = 0.0 / 0.0;

                return o;
            }

            void AddVertex(float3 vertPos, float2 uv, inout TriangleStream<g2f> triStream)
            {
                g2f outVertex;

                UNITY_INITIALIZE_OUTPUT(g2f, outVertex);

                outVertex.vertex = mul(UNITY_MATRIX_VP, float4(vertPos, 1.));// UnityObjectToClipPos(vertPos);
                outVertex.uv = uv;
                 
                UNITY_TRANSFER_FOG(outVertex, outVertex.vertex);

                triStream.Append(outVertex);
            }

            void CreateQuad(float4 vertPos, float3 up, float3 right, inout TriangleStream<g2f> triStream)
            {
                AddVertex(vertPos - right, float2(0., 0.), triStream);
                AddVertex(vertPos + right, float2(1., 0.), triStream);
                AddVertex(vertPos + up - right, float2(0., 1.), triStream);
                AddVertex(vertPos + up + right, float2(1., 1.), triStream);
                triStream.RestartStrip();
            }

            [maxvertexcount(4)] // retourne 4 vertex pour creer un quad
            void geom(point v2g vertIn[1], inout TriangleStream<g2f> triStream)
            {
                float3 up    = float3(0., 0., 1.) * _Size.x;
                float3 right = float3(1., 0., 0.) * _Size.y * 0.5;

                // Creer un Quad autour de la particule
                CreateQuad(vertIn[0].vertex, up, right, triStream);
            }

            fixed4 frag(g2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv.xy);
                col.a = clamp(col.x, 0., col.a);
                col.xyz = _MainColor;

                // apply fog
                UNITY_APPLY_FOG(i.fogCoord, col);
                
                return col;
            }
            ENDCG
        }
    }
}
