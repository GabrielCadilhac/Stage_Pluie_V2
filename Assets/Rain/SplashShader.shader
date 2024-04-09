Shader "Unlit/SplashShader"
{
    Properties
    {
        _DropSeq ("Drop Sequence", 2DArray) = "" {}
        _UVScale ("UV SCale", Vector) = (1.,1.,0.,0.)
        _OffSet  ("OffSet", Vector)   = (0.5, 0.5, 0., 0.)
        _SliceRange ("Slices", Range(0,21)) = 0
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
                fixed4 color : COLOR;
            };

            struct g2f
            {
                float4 vertex : SV_POSITION;
                float3 uv : TEXCOORD0;
                fixed4 color : COLOR; 

                UNITY_FOG_COORDS(1)
            };

            fixed4 _Color;
            float2 _Size;
            float2 _UVScale;
            float _SliceRange;
            float2 _OffSet;

            v2g vert (appdata v)
            {
                v2g o;

                o.vertex     = v.vertex;
                o.normal     = v.normal;
                o.vertexId.x = v.vertexId;

                o.color      = _Color;
                o.color.a    = TimeBuffer[v.vertexId];

                // Discard splash si la position est nulle
                if (TimeBuffer[v.vertexId] <= 0.0)
                    o.vertex = 0.0 / 0.0;

                return o;
            }

            UNITY_DECLARE_TEX2DARRAY(_DropSeq);

            void AddVertex(float3 vertPos, fixed4 color, float2 uv, inout TriangleStream<g2f> triStream)
            {
                g2f outVertex;

                UNITY_INITIALIZE_OUTPUT(g2f, outVertex);

                outVertex.vertex = UnityObjectToClipPos(vertPos);
                outVertex.color = color;
                outVertex.uv.xy = (uv.xy + _OffSet) * _UVScale;
                outVertex.uv.z = (1.0 - color.a) * 20.;
                // outVertex.uv.z = _SliceRange;

                UNITY_TRANSFER_FOG(outVertex, outVertex.vertex);

                triStream.Append(outVertex);
            }

            void CreateQuad(float4 vertPos, fixed4 color, float3 up, float3 right, inout TriangleStream<g2f> triStream)
            {
                AddVertex(vertPos - right, color, float2(1., 0.), triStream);
                AddVertex(vertPos + right, color, float2(0., 0.), triStream);
                AddVertex(vertPos + up - right, color, float2(0., 1.), triStream);
                AddVertex(vertPos + up + right, color, float2(1., 1.), triStream);
                triStream.RestartStrip();
            }

            [maxvertexcount(4)] // retourne 4 vertex pour creer un quad
            void geom(point v2g vertIn[1], inout TriangleStream<g2f> triStream)
            {
                float3 up    = float3(0., 1., 0.) * _Size.x;
                float3 right = float3(1., 0., 0.) * _Size.y * 0.5;

                // Creer un Quad autour de la particule
                CreateQuad(vertIn[0].vertex, vertIn[0].color, up, right, triStream);
            }

            fixed4 frag(g2f i) : SV_Target
            {
                // fixed4 col = i.color;
                fixed4 col = UNITY_SAMPLE_TEX2DARRAY(_DropSeq, i.uv);
                col.a = col.x / 2.;

                // apply fog
                UNITY_APPLY_FOG(i.fogCoord, col);
                
                return col;
            }
            ENDCG
        }
    }
}
