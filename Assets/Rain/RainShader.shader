Shader "Unlit/RainShader"
{
    Properties
    {
        _Color ("Drop Color", Color) = (1,1,1,1)
        _Size  ("Drop Size", Vector) = (1.2, .02, 0., 0.)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Blend SrcAlpha OneMinusSrcAlpha
        LOD 100

        Pass
        {
            Cull off // Pour voir les gouttes de derriere

            CGPROGRAM
            #pragma require geometry

            #pragma target 3.5
            #pragma vertex vert
            #pragma fragment frag
            #pragma geometry geom
            // make fog work
            #pragma multi_compile_fog
        
            #include "UnityCG.cginc"
    
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
                UNITY_FOG_COORDS(1)
            };

            fixed4 _Color;
            float2 _Size;

            uint   _ParticlesNumber;
            float  _ForceRotation; 
            float3 _WindRotation;

            v2g vert(appdata v)
            {
                v2g o;
                if (v.vertexId >= _ParticlesNumber) return o;

                o.vertex = v.vertex;
                o.normal = v.normal;
                o.vertexId.x = v.vertexId;
                return o;
            }

            // Return a random between 0 and 1
            float hash11(float p)
            {
	            p = frac(p * .1031);
                p *= p + 33.33;
                p *= p + p;
                return frac(p);
            }

            // Genere un nombre aleatoire entre p_min et p_max
            float range11(float p_p, float p_min, float p_max)
            {
                float r = hash11(p_p);
                return p_min * ( 1.0 - r) + p_max * r;
            }

            void AddVertex(float3 vertPos, float2 uv, inout TriangleStream<g2f> triStream)
            {
                g2f outVertex;

                UNITY_INITIALIZE_OUTPUT(g2f, outVertex);

                outVertex.vertex = UnityObjectToClipPos(vertPos);
                UNITY_TRANSFER_FOG(outVertex, outVertex.vertex);

                triStream.Append(outVertex);
            }

            void CreateQuad(float4 vertPos, float vertID, float3 up, float3 right, inout TriangleStream<g2f> triStream)
            {
                AddVertex(vertPos - right, float2(1, 1), triStream);
                AddVertex(vertPos + right, float2(0, 1), triStream);
                AddVertex(vertPos + up - right, float2(0, 0), triStream);
                AddVertex(vertPos + up + right, float2(1, 0), triStream);
                triStream.RestartStrip();
            }

            [maxvertexcount(4)] // retourne 4 vertex pour creer un quad
            void geom(point v2g vertIn[1], inout TriangleStream<g2f> triStream)
            {
                float3 pos = vertIn[0].vertex.xyz;
                _WindRotation.y -= 2.0 * _ForceRotation;
                float3 up = normalize(_WindRotation) * _Size.x;// + range11(vertIn[0].vertexId.x, -0.01, 0.01);
                float3 right = normalize(float3(1.0, 0.0, 0.0)) * _Size.y;// + range11(vertIn[0].vertexId.x, -0.01, 0.01);

                // ===== Verifier si la particule est derriere la camera =====
                // Calculer la position en world space de la particule
                // float3 worldPos = pos + float3(
                //                 unity_ObjectToWorld[0].w,
                //                 unity_ObjectToWorld[1].w,
                //                 unity_ObjectToWorld[2].w
                //             );

                // Calculer la de la position vers la camera
                // float2 pos2Camera = worldPos - _WorldSpaceCameraPos;
                // float distanceToCamera = length(pos2Camera);
                // pos2Camera /= distanceToCamera;

                // Calculer le vecteur forward (direction dans laquelle regarde la camera)
                // float3 camForward = normalize(mul((float3x3) unity_CameraToWorld, float3(0.0, 0.0, 1.0)));

                // Verifier si la particule se trouve devant en fonction de l'angle entre les deux vecteurs
                // if (dot(camForward, pos2Camera) < 0.0) return;
                // ===============================================================

                // Creer un Quad autour de la particule
                CreateQuad(vertIn[0].vertex, vertIn[0].vertexId.x, up, right, triStream);
            }

            fixed4 frag(g2f i) : SV_Target
            {
                // sample the texture
                fixed4 col = _Color;

                // apply fog
                UNITY_APPLY_FOG(i.fogCoord, col);

                return col;
            }

            ENDCG
        }
    }
}