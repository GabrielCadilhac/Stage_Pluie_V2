Shader "Unlit/SplashShader2"
{
    Properties
    {
        _MainTex   ("Texture", 2D)  = "" {}
        _Size      ("Size", Vector) = (.5,.5,0.,0.)
        _MainColor ("Color", Color) = (1., 1., 1.)
    }
    SubShader
    {
        Tags { "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent" }
        LOD 100

    	ZWrite Off
		Blend SrcAlpha OneMinusSrcAlpha
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            uniform StructuredBuffer<float4> Position;
            uniform StructuredBuffer<float3> Normale;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _MainColor;

            float2 _Size;

            v2f vert (appdata vertex, uint instanceID : SV_InstanceID, uint vertexId : SV_VertexID)
            {
                v2f o;
                float x = vertexId % 2;
                float z = vertexId / 2;
                x = z == 1 ? 1-x : x;
                o.uv = float2(x,z);

                x = 2.*x-1.;
                z = 2.*z-1.;

                float3 n = normalize(Normale[instanceID]);
                float3 r = (n - float3(0., 0., 1.)==0.) ? float3(1., 0., 0.) : float3(0., 0., 1.);
                float3 u = normalize(cross(n, r));
                float3 v = cross(n, u);

                //float4 pos = float4(x * _Size.x, 0., z * _Size.y, 1.);
                float4 pos = float4(x * (_Size.x/2.) * u + z * (_Size.y/2.) * v, 1.);
                
                float4x4 translation = {1., 0., 0., Position[instanceID].x,
                                        0., 1., 0., Position[instanceID].y,
                                        0., 0., 1., Position[instanceID].z,
                                        0., 0., 0., 1.};
                pos = mul(translation, pos);
                o.vertex = mul(UNITY_MATRIX_VP, pos);

                UNITY_TRANSFER_FOG(o, o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv.xy);
                //col.a = clamp(col.x, 0., col.a);
                col.xyz = _MainColor;

                // apply fog
                UNITY_APPLY_FOG(i.fogCoord, col);

                return col;
            }
            ENDCG
        }
    }
}
