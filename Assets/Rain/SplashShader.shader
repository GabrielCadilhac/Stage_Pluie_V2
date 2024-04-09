Shader "Unlit/SplashShader"
{
    Properties
    {
        _Color ("Drop Color", Color) = (1,1,1,1)
        _Size  ("Drop Size", Vector) = (0.5, 0.5, 0., 0.)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Blend SrcAlpha OneMinusSrcAlpha
        LOD 100

        Pass
        {
            Cull off
             
            CGPROGRAM
            #pragma require geometry

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
                float2 vertexId : TEXCOORD0;

                UNITY_FOG_COORDS(1)
            };

            fixed4 _Color;
            float2 _Size;

            v2g vert (appdata v)
            {
                v2g o;

                o.vertex = v.vertex;
                o.normal = v.normal;
                o.vertexId.x = v.vertexId;

                // Discard splash si la position est nulle
                // if (TimeBuffer[v.vertexId] <= 0.0)
                // {
                //     o.vertex = 0.0 / 0.0;
                // }

                return o;
            }

            void AddVertex(float3 vertPos, float2 uv, float vertId, inout TriangleStream<g2f> triStream)
            {
                g2f outVertex;

                UNITY_INITIALIZE_OUTPUT(g2f, outVertex);

                outVertex.vertex = UnityObjectToClipPos(vertPos);
                outVertex.vertexId.x = vertId;
                UNITY_TRANSFER_FOG(outVertex, outVertex.vertex);

                triStream.Append(outVertex);
            }

            void CreateQuad(float4 vertPos, float vertID, float3 up, float3 right, inout TriangleStream<g2f> triStream)
            {
                AddVertex(vertPos - right, vertID, float2(1, 1), triStream);
                AddVertex(vertPos + right, vertID, float2(0, 1), triStream);
                AddVertex(vertPos + up - right, vertID, float2(0, 0), triStream);
                AddVertex(vertPos + up + right, vertID, float2(1, 0), triStream);
                triStream.RestartStrip();
            }

            [maxvertexcount(4)] // retourne 4 vertex pour creer un quad
            void geom(point v2g vertIn[1], inout TriangleStream<g2f> triStream)
            {
                float3 pos = vertIn[0].vertex.xyz;
                float3 up = _Size.x;// + range11(vertIn[0].vertexId.x, -0.01, 0.01);
                float3 right = float3(1.0, 0.0, 0.0) * _Size.y;// + range11(vertIn[0].vertexId.x, -0.01, 0.01);

                // Creer un Quad autour de la particule
                CreateQuad(vertIn[0].vertex, vertIn[0].vertexId.x, up, right, triStream);
            }

            fixed4 frag(g2f i) : SV_Target
            {
                fixed4 col = _Color;

                // apply fog
                UNITY_APPLY_FOG(i.fogCoord, col);

                col.a = 0.1;//TimeBuffer[i.vertexId.x];

                return col;
            }
            ENDCG
        }
    }
}
