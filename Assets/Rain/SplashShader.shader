Shader "Unlit/SplashShader"
{
    Properties
    {
        _MainTex ("Splash Texture", 2D)    = "" {}
        _Size    ("Splash Size", Vector) = (.5,.5,0.,0.)
    }
    SubShader
    {
        // Tags pour faire le rendu des imposteurs
        Tags{ "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent" "DisableBatching" = "True" }

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

            struct StrLight
            {
                float3 position;
                fixed4 color;
                float intensity;
            };

            uniform StructuredBuffer<float> TimeBuffer;
            uniform StructuredBuffer<StrLight> Lights;

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
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;

                UNITY_FOG_COORDS(1)
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            
            float2 _Size;
            uint _NbLights;

            v2g vert (appdata v)
            {
                v2g o;

                o.vertex     = v.vertex;
                o.normal     = v.normal;
                o.vertexId.x = v.vertexId;

                // Discard splash si la position est nulle
                if (TimeBuffer[v.vertexId] <= 0.0)
                    o.vertex = 0.0 / 0.0;

                fixed4 incomLight = fixed4(0., 0., 0., 0.);
                for (int i = 0; i < _NbLights; i++)
                {
                    incomLight +=  (1.0/length(UnityObjectToClipPos(o.vertex) - Lights[i].position)) * Lights[i].color * Lights[i].intensity;
                }
                o.color = incomLight;

                return o;
            }

            void AddVertex(float3 vertPos, fixed4 color, float2 uv, inout TriangleStream<g2f> triStream)
            {
                g2f outVertex;

                UNITY_INITIALIZE_OUTPUT(g2f, outVertex);

                outVertex.vertex = UnityObjectToClipPos(vertPos);
                outVertex.uv = uv;
                outVertex.color = color;
                 
                UNITY_TRANSFER_FOG(outVertex, outVertex.vertex);

                triStream.Append(outVertex);
            }

            void CreateQuad(float4 vertPos, fixed4 color, float3 up, float3 right, inout TriangleStream<g2f> triStream)
            {
                AddVertex(vertPos - right, color, float2(0., 0.), triStream);
                AddVertex(vertPos + right, color, float2(1., 0.), triStream);
                AddVertex(vertPos + up - right, color, float2(0., 1.), triStream);
                AddVertex(vertPos + up + right, color, float2(1., 1.), triStream);
                triStream.RestartStrip();
            }

            [maxvertexcount(4)] // retourne 4 vertex pour creer un quad
            void geom(point v2g vertIn[1], inout TriangleStream<g2f> triStream)
            {
                float3 up    = float3(0., 0., 1.) * _Size.x;
                float3 right = float3(1., 0., 0.) * _Size.y * 0.5;

                // Creer un Quad autour de la particule
                CreateQuad(vertIn[0].vertex, vertIn[0].color, up, right, triStream);
            }

            fixed4 frag(g2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv.xy);
                col.a = col.x;
                col *= i.color;

                col = min(col, fixed4(1., 1., 1., 1.));

                // apply fog
                UNITY_APPLY_FOG(i.fogCoord, col);
                
                return col;
            }
            ENDCG
        }
    }
}
