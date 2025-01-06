Shader "Unlit/RainShader"
{
    Properties
    {
        _Color ("Drop Color", Color) = (1,1,1,1)
        _Size  ("Drop Size", Vector) = (1.0, .02, 0., 0.)
        _Depth ("Drop Depth", Float) = 1.
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

            struct StrLight
            {
                float3 position;
                fixed4 color;
                float intensity;
            };

            uniform StructuredBuffer<float3> Velocities;
            uniform StructuredBuffer<float> Sizes;
            uniform StructuredBuffer<StrLight> Lights;

            struct appdata
            {
                float4 position : POSITION;
                uint id : SV_VertexID;
                float3 normal : NORMAL;
            };

            struct v2g
            {
                float4 position : SV_POSITION;
                float2 id : TEXCOORD0;
                float3 normal : NORMAL;
                float3 rotation : TEXCOORD1;
                fixed4 color : COLOR;
            };

            struct g2f
            {
                float4 position : SV_POSITION;
                fixed4 color : COLOR;
                float distance : TEXCOORD0;
                UNITY_FOG_COORDS(1)
            };

            fixed4 _Color;
            float2 _Size;
            float _Depth;

            uint   _ParticlesNumber;
            uint   _NbLights;
            float  _ForceRotation; 

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
                if (v.id >= _ParticlesNumber) return o;

                o.position = v.position;
                o.normal = v.normal;
                o.id.x = v.id;

                fixed4 incomLight = fixed4(0., 0., 0., 0.);

                for (int i = 0; i < _NbLights; i++)
                {
                    incomLight +=  (Lights[i].color * Lights[i].intensity) / length(UnityObjectToClipPos(o.position) - Lights[i].position);
                }

                float colorVariation = range11(v.id, -0.2, 0.2);
                // o.color = (_Color + colorVariation) * incomLight;
                o.color = (_Color + colorVariation);

                o.rotation = -Velocities[v.id] * float3(_ForceRotation, 1., _ForceRotation);

                return o;
            }

            void AddVertex(float3 vertPos, fixed4 color, float2 uv, inout TriangleStream<g2f> triStream)
            {
                g2f outVertex;

                UNITY_INITIALIZE_OUTPUT(g2f, outVertex);

                outVertex.position = UnityObjectToClipPos(vertPos);
                outVertex.color = color;
                UNITY_TRANSFER_FOG(outVertex, outVertex.position);

                float3 worldPos = vertPos + float3(
                                unity_ObjectToWorld[0].w,
                                unity_ObjectToWorld[1].w,
                                unity_ObjectToWorld[2].w
                            );

                float3 pos2Camera = worldPos - _WorldSpaceCameraPos;

                outVertex.distance = pos2Camera.z;

                triStream.Append(outVertex);
            }

            void CreateQuad(float4 vertPos, fixed4 color, float3 up, float3 right, inout TriangleStream<g2f> triStream)
            {
                AddVertex(vertPos - right, color, float2(1, 1), triStream);
                AddVertex(vertPos + right, color, float2(0, 1), triStream);
                AddVertex(vertPos + up - right, color, float2(0, 0), triStream);
                AddVertex(vertPos + up + right, color, float2(1, 0), triStream);
                triStream.RestartStrip();
            }

            [maxvertexcount(4)] // retourne 4 vertex pour creer un quad
            void geom(point v2g vertIn[1], inout TriangleStream<g2f> triStream)
            {
                float3 pos   = vertIn[0].position.xyz;
                float3 up    = normalize(vertIn[0].rotation) * _Size.x + range11(vertIn[0].id.x, -0.01, 0.01);
                float3 right = float3(1.0, 0.0, 0.0) * _Size.y + range11(vertIn[0].id.x, -0.01, 0.01);

                up    *=  Sizes[vertIn[0].id.x];
                right *=  Sizes[vertIn[0].id.x];

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
                CreateQuad(vertIn[0].position, vertIn[0].color, up, right, triStream);
            }

            fixed4 frag(g2f i) : SV_Target
            {
                fixed4 col = i.color;
                col.a = clamp(i.distance * i.distance * _Depth, 0., 0.8);

                // apply fog
                UNITY_APPLY_FOG(i.fogCoord, col);

                return col;
            }

            ENDCG
        }
    }
}
