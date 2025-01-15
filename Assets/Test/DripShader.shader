Shader "Unlit/DripShader"
{
    Properties
    {
        _UV ("UV", Vector)     = (1.0, 1.0, 0., 0.)
        _Size ("Size", Vector) = (0.5, 0.5, 1., 1.)
        _Color("Color", Color) = (1.0, 0., 0., 1.0)
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

            uniform StructuredBuffer<float3> Positions;
            uniform StructuredBuffer<int> CanBeDrawn;

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

            float2 _Size;
            fixed4 _Color;

            v2f vert (appdata v, uint instanceID : SV_InstanceID, uint vertexId : SV_VertexID)
            {
                v2f o;
                if (CanBeDrawn[instanceID] != 1)
					return o;

                float x = vertexId % 2;
                float y = vertexId / 2;
                x = y == 1 ? 1-x : x;

                o.uv = float2(x, y);

                float3 up = float3(0., 1., 0.) * _Size.x;
                float3 right = float3(1.0, 0.0, 0.0) * _Size.y;
                
                float4 pos = float4((2. * o.uv.x - 1.) * right + o.uv.y * up, 1.);

                float4x4 translation = {1., 0., 0., Positions[instanceID].x,
                                        0., 1., 0., Positions[instanceID].y,
                                        0., 0., 1., Positions[instanceID].z,
                                        0., 0., 0., 1.};
                pos = mul(translation, pos);

                o.vertex = mul(UNITY_MATRIX_VP, pos);
                o.instanceID = instanceID;

                UNITY_TRANSFER_FOG(o, o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = _Color;

                // apply fog
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }
}
