// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// Upgrade NOTE: replaced '_World2Object' with 'unity_WorldToObject'

Shader "Unlit/RainShader"
{
    Properties
    {
        _Color ("Drop Color", Color) = (1,1,1,1)
        _Size  ("Drop Size", Vector) = (1.0, .02, 0., 0.)
        _Depth ("Drop Depth", Float) = 1.
        _RedThreshold ("Speed Threshold", Float) = 3.
    }
    SubShader
    {
        Tags{ "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent" "DisableBatching" = "True" }

		ZWrite Off
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

            uniform StructuredBuffer<float3> Velocities;
            uniform StructuredBuffer<float> Sizes;

            struct appdata
            {
                float4 vertex : POSITION;
                uint vertexId : SV_VertexID;
                float3 normal : NORMAL;
            };

            struct v2g
            {
                float4 vertex : SV_POSITION;
                float vertexId : TEXCOORD0;
                float3 normal : NORMAL;
                float3 rotation : TEXCOORD1;
            };

            struct g2f
            {
                float4 vertex : SV_POSITION;
                float distance : TEXCOORD0;
                float2 uv : TEXCOORD2;
                UNITY_FOG_COORDS(1)
                float vertexId : TEXCOORD3;
            };

            fixed4 _Color;
            float2 _Size;
            float _Depth;

            uint   _ParticlesNumber;
            float  _ForceRotation; 

            float _RedThreshold;

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
                return p_min * (1.0 - r) + p_max * r;
            }

            v2g vert(appdata v)
            {
                v2g o;
                if (v.vertexId >= _ParticlesNumber) return o;

                o.vertex = v.vertex;
                o.normal = v.normal;
                o.vertexId.x = v.vertexId;

                float colorVariation = range11(v.vertexId, -0.2, 0.2);

                o.rotation = -Velocities[v.vertexId] * float3(_ForceRotation, 1., _ForceRotation);

                return o;
            }

            void AddVertex(float vertexId, float3 vertPos, float2 uv, inout TriangleStream<g2f> triStream)
            {
                g2f outVertex;

                UNITY_INITIALIZE_OUTPUT(g2f, outVertex);

                outVertex.vertex = mul(UNITY_MATRIX_VP, float4(vertPos, 1.));

                UNITY_TRANSFER_FOG(outVertex, outVertex.vertex);

                float3 pos2Camera = abs(_WorldSpaceCameraPos - vertPos);

                outVertex.uv = uv;
                outVertex.distance = sqrt(pos2Camera.z * pos2Camera.z + pos2Camera.x * pos2Camera.x);
                outVertex.vertexId = vertexId;

                triStream.Append(outVertex);
            }

            void CreateQuad(float vertexId, float4 vertPos, float3 up, float3 right, inout TriangleStream<g2f> triStream)
            {
                AddVertex(vertexId, vertPos - right, float2(1, 1), triStream);
                AddVertex(vertexId, vertPos + right, float2(0, 1), triStream);
                AddVertex(vertexId, vertPos + up - right, float2(0, 0), triStream);
                AddVertex(vertexId, vertPos + up + right, float2(1, 0), triStream);
                triStream.RestartStrip();
            }

            [maxvertexcount(4)] // retourne 4 vertex pour creer un quad
            void geom(point v2g vertIn[1], inout TriangleStream<g2f> triStream)
            {
                float3 pos = vertIn[0].vertex.xyz;
                float3 up = normalize(vertIn[0].rotation) * Sizes[vertIn[0].vertexId] * _Size.x + range11(vertIn[0].vertexId, -0.01, 0.01);
                float3 right = float3(1.0, 0.0, 0.0) * Sizes[vertIn[0].vertexId] * _Size.y + range11(vertIn[0].vertexId, -0.01, 0.01);

                // ===== Verifier si la particule est derriere la camera =====
                // Calculer la position en world space de la particule
                // float3 worldPos = pos + float3(
                //                 unity_ObjectToWorld[0].w,
                //                 unity_ObjectToWorld[1].w,
                //                 unity_ObjectToWorld[2].w
                //             );

                // Calculer la de la position vers la camera
                // float3 pos2Camera = worldPos - _WorldSpaceCameraPos;
                // float distanceToCamera = length(pos2Camera);
                // pos2Camera /= distanceToCamera;

                // Calculer le vecteur forward (direction dans laquelle regarde la camera)
                // float3 camForward = normalize(mul((float3x3) unity_CameraToWorld, float3(0.0, 0.0, 1.0)));

                // Verifier si la particule se trouve devant en fonction de l'angle entre les deux vecteurs
                // if (dot(camForward, pos2Camera) < 0.0) return;
                // ===========================================================

                // Creer un Quad autour de la particule
                CreateQuad(vertIn[0].vertexId, vertIn[0].vertex, up, right, triStream);
            }

            fixed4 frag(g2f i) : SV_Target
            {
                const float PI = 3.14159265359;

                float3 fi  = float3(1., 0.5, 1.5);
                float3 gi  = float3(1., 0., 0.);
                float3 amp = float3(0.1, 0.2, 0.1);

                float phi = 1.;

                float c = dot(float3(1., 1., 1.), amp * cos(2.*PI*(fi * i.uv.x + gi * i.uv.y) + phi));
                fixed4 col = fixed4(c,c,c,c);
                
                col.a *= clamp(i.distance * i.distance *_Depth * 0.0001, 0.05, 0.75);
                
                //float Vmax = (9.65 - 10.3 * exp(-0.6 * Sizes[i.vertexId])) * _RedThreshold;
                
                /*
                if (length(Velocities[i.vertexId]) > Vmax && i.distance > 10.0)
                {
                    col = fixed4(1., 0., 0., 0.5);
                }*/

                // apply fog
                UNITY_APPLY_FOG(i.fogCoord, col);

                return col;
            }

            ENDCG
        }
    }
}
