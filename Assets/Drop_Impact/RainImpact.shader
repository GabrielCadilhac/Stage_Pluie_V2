Shader "Unlit/RainImpact"
{
    Properties
    {
        _Size ("Size", Vector) = (1., 1., 1., 1.)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
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
            uniform StructuredBuffer<float3> Normal;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
                float3 rayOrigin : TEXCOORD0;
                float3 rayDir : TEXCOORD1;
                float3 normal : TEXCOORD2;
            };

            #define MAX_DIST 100.
            #define MIN_DIST .001
            #define MAX_STEPS 100
            #define EPSILON 0.0001

            #define PI 3.1415926536
            #define TWO_PI 6.2831853072
            #define INV_PI 0.3183098862

            float _AnimSpeed;
            float2 _Size;
            float _iTime;
            float3 _Normal;
            float animTime;

            v2f vert (appdata vert, uint instanceID : SV_InstanceID, uint vertexId : SV_VertexID)
            {
                v2f o;
                float x = vertexId % 2;
                float y = vertexId / 2;
                x = y == 1 ? 1 - x : x;

                x = x * 2. -1.;
                y = y * 2. -1.;
                float3 u = float3(0., 1., 0.);
                float3 v = float3(1., 0., 0.);

                float4 worldPos = float4(x * _Size.x * u + y * _Size.y * v, 1.);

                float3 currentPos = Position[instanceID];
                float3 worldSpacePivot = currentPos;
                // offset between pivot and camera
                float3 worldSpacePivotToCamera = _WorldSpaceCameraPos.xyz - worldSpacePivot;

                // camera up vector
                // used as a somewhat arbitrary starting up orientation
                float3 upCam = UNITY_MATRIX_I_V._m01_m11_m21;
                // forward vector is the normalized offset
                // this it the direction from the pivot to the camera
                float3 forward = normalize(worldSpacePivotToCamera);
                if (length(upCam - forward) <= 0.1)
                    upCam = UNITY_MATRIX_I_V._m02_m12_m22;
 
                // cross product gets a vector perpendicular to the input vectors
                float3 rightCam = normalize(cross(forward, upCam));

                // another cross product ensures the up is perpendicular to both
                upCam = cross(rightCam, forward);

                // construct the rotation matrix
                float3x3 rotMat = float3x3(rightCam, upCam, forward);

                // the above rotate matrix is transposed, meaning the components are
                // in the wrong order, but we can work with that by swapping the
                // order of the matrix and vector in the mul()
                worldPos = float4(mul(float3(worldPos.xy, 0.3), rotMat) + worldSpacePivot, 1.);

                // ray direction
                float3 worldRayDir = worldPos.xyz - _WorldSpaceCameraPos.xyz;

                o.rayDir = mul(unity_WorldToObject, float4(worldRayDir, 0.0));

                o.vertex =  UnityWorldToClipPos(worldPos);
                o.rayOrigin = _WorldSpaceCameraPos - currentPos;
                o.normal = Normal[instanceID];

                UNITY_TRANSFER_FOG(o, o.vertex);
                return o;
            }

            float sdSphere( float3 p, float s )
            {
              return length(p)-s;
            }

            float sdRoundedCylinder( float3 p, float ra, float rb, float h )
            {
              float2 d = float2( length(p.xy)-2.0*ra+rb, abs(p.z) - h );
              return min(max(d.x,d.y),0.0) + length(max(d,0.0)) - rb;
            }

            float sdTorus( float3 p, float2 t )
            {
              float2 q = float2(length(p.xy)-t.x,p.z);
              return length(q)-t.y;
            }

            float sdEllipsoid( float3 p, float3 r )
            {
              float k0 = length(p.xzy/r);
              float k1 = length(p.xzy/(r*r));
              return k0*(k0-1.0)/k1;
            }

            float opDisplace(in float pDist, in float3 p, in float s, in float m)
            {
                float d2 = sin(p.x*s)*sin(p.y*s)*sin(p.z*s) * m;
                return pDist+d2;
            }

            // Smooth min
            float smin(float pDistA, float pDistB, float k)
            {
                float h = clamp(0.5 + 0.5*(pDistB - pDistA)/k, 0.0, 1.0);
                return lerp(pDistB, pDistA, h) - k*h*(1.0-h);
            }

            float Mod(float x, float y)
            {
                return x - y * floor(x/y);
            }

            float coronaSplash(float3 pos, float3x3 rotMat)
            {
                float3 mainDropPos = mul(rotMat, pos - float3(0.0, -2.0*clamp(tan(_iTime), -10.0, 10.0), 0.0));
                float mainDrop = sdSphere(mainDropPos, 0.25 );

                float deltaTime = Mod(-2.*_iTime/PI, 2.);
                float splashTorusMod  = deltaTime - 1.;
                float splashTorusWave = 2. *  splashTorusMod; // Smooth sawtooth
                float splashTorusRad  = (1. - deltaTime * deltaTime + PI) * 0.4; // Exp sawtooth
                float splashTorusY = splashTorusMod * (1.0 - pow(splashTorusMod, 6.0)) * 0.4; // Smooth sawtooth

                float3 torePos = mul(rotMat, (pos - splashTorusY*rotMat._m20_m21_m22)) * float3(1.,1.,.3);
                float toreDist = sdTorus(torePos, float2(splashTorusRad, 0.05 * max(splashTorusWave, 0.1)));

                float3 dispPos = mul(rotMat, pos);
                toreDist = opDisplace(toreDist, dispPos, 20., 0.005*max(splashTorusWave, 0.));
                toreDist = splashTorusRad > 1.2 ? MAX_DIST : toreDist;

                float3 cylPos = mul(rotMat, pos + float3(0.,0.02,0.));
                float cylDist = sdRoundedCylinder(cylPos, 0.6, 0.01, 0.01);
    
                cylDist = smin(toreDist, cylDist, 0.2*clamp(splashTorusWave, 0.0, 1.0));
                return smin(mainDrop,cylDist, 0.2);
            }

            float deposition(float3 pos, float3x3 rot)
            {
                float deltaTime = (_iTime*0.4) % 1.4;
                float dist = MAX_DIST;
    
                float dropHeight = lerp(-0.27, 0.27, 1.-min(deltaTime, 1.));
                float3 dropPos = mul(rot, pos-float3(0., dropHeight, 0.));
                float dropDist = sdSphere(dropPos, 0.25);
                dist = min(dist, dropDist);
    
                float diamLamella = min(deltaTime, 1.)*0.48;
                float3 posLamella = mul(rot, pos-float3(0., 0.02, 0.));
                float distLamella = sdRoundedCylinder(posLamella, diamLamella, 0.001, 0.01);
                dist = smin(dist, distLamella, 0.2);
    
                float3 posEllip = mul(rot, pos);
                float distEllip = sdEllipsoid(posEllip, float3(1.05, 0.1, 1.05));
    
                float maskEllipY = lerp(0.1, 0.4, max(deltaTime - 1., 0.)*3.);
                float3 posMaskEllip = mul(rot, pos-maskEllipY*rot._m20_m21_m22);
                float maskEllip = sdEllipsoid(posMaskEllip, float3(1.05, 0.2, 1.05));
                distEllip = max(distEllip, -maskEllip);
                
                dist = deltaTime >= 1. ? min(dist, distEllip) : dist;
    
                float torusDiam = 1.*(deltaTime >= 1. ? 0. : deltaTime);
                float3 torusPos = mul(rot, pos);
                float torusDist = sdTorus(torusPos, float2(torusDiam, 0.05));
                dist = min(dist, torusDist);
    
                return dist;
            }

            float map(float3 pos, float3x3 rot)
            {
                //return deposition(pos, rot);
                return coronaSplash(pos, rot);
            }

            float3 getNormal( float3 pos, float3x3 rot )
            {
                const float2 h = float2(EPSILON, 0.);
                return normalize( float3 (map(pos + h.xyy, rot) - map(pos - h.xyy, rot),
					                      map(pos + h.yxy, rot) - map(pos - h.yxy, rot),
                                          map(pos + h.yyx, rot) - map(pos - h.yyx, rot) ) );
            }

            float rayMarch(float3 ro, float3 rd, float3x3 rot)
            {
                float distO = 0.;
    
                for (int i = 0; i < MAX_STEPS; i++)
                {
                    float3 pos = ro + rd * distO;
                    float  dS  = map(pos, rot);
                    distO += dS;
        
                    if (distO >= MAX_DIST || dS <= MIN_DIST) break;
                }
                return distO;
            }

            fixed4 frag (v2f i, out float outDepth : SV_Depth) : SV_Target
            {
                float3 ro = i.rayOrigin;
                float3 rd = normalize(i.rayDir);
    
                animTime = (animTime + _iTime * _AnimSpeed) % 1.;

                float3 n = normalize(_Normal);
                float3 u   = normalize(cross(float3(1,0,0), n));
                if (length(u) < 0.1) u = normalize(cross(float3(0,1,0), n));
                float3 v = cross(n, u);
                float3x3 rotMat = float3x3(u, v, n);
                
                float dist = rayMarch(ro, rd, rotMat);
                if (dist >= MAX_DIST)
                    discard;
                
                float3 pos = ro + rd * dist;
    
                float3 normal = getNormal(pos, rotMat);
                normal *= sign(dot(normal, rd));
    
                fixed4 col = fixed4(normal * .5 + .5, 1.0);
                
                // apply fog
                UNITY_APPLY_FOG(i.fogCoord, col);

                float4 clipPos = UnityWorldToClipPos(pos);
                outDepth = clipPos.z / clipPos.w;
                return col;
            }
            ENDCG
        }
    }
}
