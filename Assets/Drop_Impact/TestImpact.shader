Shader "Unlit/TestImpact"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            #define MAX_DIST 10.
            #define MIN_DIST .001
            #define MAX_STEPS 100

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
                float3 rayDir : TEXCOORD1;
                float3 rayOrigin : TEXCOORD2;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            v2f vert (appdata v)
            {
                v2f o;

                float3 worldSpacePivot = unity_ObjectToWorld._m03_m13_m23;
                // offset between pivot and camera
                float3 worldSpacePivotToCamera = _WorldSpaceCameraPos.xyz - worldSpacePivot;

                // camera up vector
                // used as a somewhat arbitrary starting up orientation
                float3 up = UNITY_MATRIX_I_V._m01_m11_m21;

                // forward vector is the normalized offset
                // this it the direction from the pivot to the camera
                float3 forward = normalize(worldSpacePivotToCamera);

                // cross product gets a vector perpendicular to the input vectors
                float3 right = normalize(cross(forward, up));

                // another cross product ensures the up is perpendicular to both
                up = cross(right, forward);

                // construct the rotation matrix
                float3x3 rotMat = float3x3(right, up, forward);

                // the above rotate matrix is transposed, meaning the components are
                // in the wrong order, but we can work with that by swapping the
                // order of the matrix and vector in the mul()
                float3 worldPos = mul(v.vertex.xyz, rotMat) + worldSpacePivot;

                // ray direction
                float3 worldRayDir = worldPos - _WorldSpaceCameraPos.xyz;

                o.rayDir = mul(unity_WorldToObject, float4(worldRayDir, 0.0));
                // clip space position output
                o.vertex = UnityWorldToClipPos(worldPos);

                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.rayOrigin = _WorldSpaceCameraPos.xyz;
                o.vertex = UnityWorldToClipPos(worldPos);
                
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            float GetDist(float3 pos)
            {
                return length(pos) - .5;
            }

            float RayMarch(float3 ro, float3 rd)
            {
                float distO = 0.;
                float dS;
                int i = 0;
                while (i < MAX_STEPS && dS > MIN_DIST && distO < MAX_DIST)
                {
                    float3 p = ro + rd * distO;
                    dS = GetDist(p);
                    distO += dS;
                    i++;
                }
                return distO;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 uv = i.uv-.5;

                // sample the texture
                fixed4 col = fixed4(0., 0., 0., 1.);

                float3 ro = i.rayOrigin;
                float3 rd = i.rayDir;

                float d = RayMarch(ro, rd);
                if (d < MAX_DIST)
                    col.r = 1.;

                // apply fog
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }
}
