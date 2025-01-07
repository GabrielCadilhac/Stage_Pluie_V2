Shader "Unlit/RainShader2"
{
    Properties
    {
        _UV ("UV", Vector) = (1.0, 1.0, 0., 0.)
        _Size ("Size", Vector) = (0.01, 1.65, 1., 1.)
        _RedThreshold ("Red Threshold", Float) = 2.
        _Depth ("Depth", Float) = 25.
        _Rotation("Rotation Strength", Float) = 0.15
        _Textured("Textured Quad", Integer) = 1
        _DropColor("Drop Color", Color) = (0.5, 0.5, 0.5, 1.0)
    }
    SubShader
    {
        Tags { "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent" }
        
        ZWrite Off
		Blend SrcAlpha OneMinusSrcAlpha
        Cull Off

        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            uniform StructuredBuffer<float3> Position;
            uniform StructuredBuffer<float3> Velocities;
            uniform StructuredBuffer<float> Sizes;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float dist : TEXCOORD1;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
                uint instanceID : SV_InstanceID;
            };

            float2 _UV;
            float2 _Size;
            float _Rotation;
            float _RedThreshold;
            float _Depth;
            int _Textured;
            fixed4 _DropColor;

            v2f vert (appdata v, uint instanceID : SV_InstanceID, uint vertexId : SV_VertexID)
            {
                v2f o;
                float x = vertexId % 2;
                float y = vertexId / 2;
                x = y == 1 ? 1-x : x;

                float4 dropSize = float4(_Size, 1., 1.);
                o.uv            = float2(x, y);

                float3 rot = -Velocities[instanceID] * float3(_Rotation, 1., _Rotation);
                float3 up = normalize(rot) * Sizes[instanceID] * _Size.x;
                float3 right = float3(1.0, 0.0, 0.0) * Sizes[instanceID] * _Size.y;
                
                float4 pos = float4((2. * o.uv.x - 1.) * right + o.uv.y * up, 1.);
                /*
                if (y == 0)
                {
                    pos += float4(Velocities[instanceID].x, 0., Velocities[instanceID].z, 0.) * _Rotation;
                    pos *= dropSize * Sizes[instanceID];
                }
                */

                float4x4 translation = {1., 0., 0., Position[instanceID].x,
                                        0., 1., 0., Position[instanceID].y,
                                        0., 0., 1., Position[instanceID].z,
                                        0., 0., 0., 1.};
                pos = mul(translation, pos);
                o.vertex = mul(UNITY_MATRIX_VP, pos);
                o.instanceID = instanceID;

                float3 posToCamera = abs(_WorldSpaceCameraPos - Position[instanceID]);
                o.dist = sqrt(posToCamera.x * posToCamera.x + posToCamera.z * posToCamera.z);

                UNITY_TRANSFER_FOG(o, o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                const float PI = 3.14159265359;

                i.uv *= _UV;
                
                fixed4 col;
                if (_Textured == 1)
                {
                    float3 fi  = float3(1., 0.5, 1.5);
                    float3 gi  = float3(1., 0., 0.);
                    float3 amp = float3(0.1, 0.2, 0.1);

                    float phi = 1.;

                    float c = dot(float3(1., 1., 1.), amp * cos(2.*PI*(fi * i.uv.x + gi * i.uv.y) + phi));
                    col = fixed4(c,c,c,c);

                } else {
                    col = _DropColor;
                }
                
                col.a *= clamp(i.dist * i.dist * _Depth * 0.0001, 0., 1.0);
                
                float Vmax = (9.65 - 10.3 * exp(-0.6 * Sizes[i.instanceID])) * _RedThreshold;
                
                if (length(Velocities[i.instanceID]) > Vmax && i.dist > 10.)
                    col = fixed4(1., 0., 0., 0.5);

                // apply fog
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }
}
