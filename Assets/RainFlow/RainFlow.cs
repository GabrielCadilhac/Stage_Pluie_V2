using System;
using System.Collections.Generic;
using Test;
using Unity.Mathematics;
using UnityEngine;

namespace RainFlow
{
    public class RainFlow
    {
        private struct Drop : IEquatable<Drop>
        {
            public Vector3 Pos;
            public Vector3 Vel;
            public float Mass;
            public float FreezeTime;

            public bool Equals(Drop other)
            {
                return Pos.Equals(other.Pos) && Vel.Equals(other.Vel) && Mass.Equals(other.Mass) && FreezeTime.Equals(other.FreezeTime);
            }

            public override bool Equals(object obj)
            {
                return obj is Drop other && Equals(other);
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(Pos, Vel, Mass, FreezeTime);
            }
        }

        struct Mesh
        {
            public float[] Axis; //xmin, xmax, ymin, ymax;
        }

        struct Collision
        {
            public float T;
            public int Axis;
        }

        private const float Alpha1  = 0.001f;
        private const float Alpha2  = 0.001f;
        private const float ThetaC = 85f * math.PI / 180f;
        private const float Epsilon = 0.0001f;
        private const float BetaS  = 0.004f;
        private const float DropMass = 10f;
        private const float DrippingThreshold = 30f;
        private readonly Vector3 _extForce = new (0f, -9.81f, 0f);

        private readonly float _deltaTime = 20f;
        private float _obsTreshold;

        private readonly Texture2D _texture;
    
        private readonly float[,] _flowMap;

        private readonly float[,] _affinityCoeff;
        private readonly int[,] _obstacles;
        private readonly Vector3[,] _normalMap;

        private readonly List<Drop> _drops;
        private readonly RainDripping _rainDripping;
        private readonly Transform _transform;

        private readonly Vector3[] _neighbors = {
            new (-1f, -1f, 0f),
            new (-1f,  0f, 0f),
            new (-1f,  1f, 0f),
            new ( 0f,  1f, 0f),
            new ( 1f,  1f, 0f),
            new ( 1f,  0f, 0f),
            new ( 1f, -1f, 0f),
            new ( 0f, -1f, 0f),
        };

        private readonly bool[,] _dropsContained;

        private Mesh[,] _mesh;

        public RainFlow(Transform pTransform, Material pDripMaterial, Texture2D pTexture, Texture2D pNormalMap, ComputeShader pDripComputeShader, float pTextureScale)
        {
            _texture = pTexture;

            _transform = pTransform;
            _rainDripping = new RainDripping(pTransform, pDripMaterial, pDripComputeShader);

            _flowMap        = new float[RainFlowMaps.Size, RainFlowMaps.Size];
            _affinityCoeff  = new float[RainFlowMaps.Size, RainFlowMaps.Size];
            _obstacles      = new int[RainFlowMaps.Size, RainFlowMaps.Size];
            _dropsContained = new bool[RainFlowMaps.Size, RainFlowMaps.Size];
            _normalMap      = new Vector3[RainFlowMaps.Size, RainFlowMaps.Size];

            // Initialize the _affinityCoeff to 0.5f
            for (int i = 0; i < RainFlowMaps.Size; i++)
            {
                for (int j = 0; j < RainFlowMaps.Size; j++)
                {
                    _flowMap[i, j]   = 0f;
                    _obstacles[i, j] = 0;

                    Color c = pNormalMap.GetPixel((int)(i * pTextureScale), (int)(j * pTextureScale));
                    _normalMap[i, j] = new Vector3(c.r, c.g, c.b);

                    _affinityCoeff[i, j] = UnityEngine.Random.Range(0f, 1f);
                    //_affinityCoeff[i, j] = 1f - p_roughnessMap.GetPixel((int)(i * p_textureScale), (int)(j * p_textureScale)).r;
                    //_affinityCoeff[i, j] = p_roughnessMap.GetPixel((int)(i * p_textureScale), (int)(j * p_textureScale)).r;
                    _dropsContained[i, j] = false;
                }
            }
            _drops = new List<Drop>();
        }

        public void AddDrop(int pI, int pJ, Vector3 pInitialVel)
        {
            Vector3 pos = new(pI, pJ, 0f);
            Drop drop = new Drop
            {
                Pos = pos, // initial position
                Vel = pInitialVel, //new Vector3(-transform.right.y, -transform.up.y, 0f); // initial velocity
                FreezeTime = 0f, // initial freeze time
                Mass = DropMass + _flowMap[(int) pos.x, (int) pos.y] // initial mass
            };

            _flowMap[(int)drop.Pos.x, (int)drop.Pos.y] = drop.Mass;
            _dropsContained[(int)drop.Pos.x, (int)drop.Pos.y] = true;

            _drops.Add(drop);
        }

        public void AddObstacle(int pI, int pJ)
        {
            _obstacles[pI, pJ] = 1;
        }

        public void GenerateMesh(float pLocalScale)
        {
            float cellSize = pLocalScale / RainFlowMaps.Size;
            _mesh = new Mesh[RainFlowMaps.Size, RainFlowMaps.Size];
            for (int i = 0; i < RainFlowMaps.Size; i++)
            {
                for (int j = 0; j < RainFlowMaps.Size; j++)
                {
                    _mesh[i, j].Axis    = new float[4];
                    _mesh[i, j].Axis[0] = i * cellSize;
                    _mesh[i, j].Axis[1] = (i + 1) * cellSize;
                    _mesh[i, j].Axis[2] = j * cellSize;
                    _mesh[i, j].Axis[3] = (j + 1) * cellSize;
                }
            }
        }

        public void DrawFlowMap(float pDotThreshold1, bool pShowObstacles)
        {
            Vector3 normal = _transform.forward;
            float ratio = _transform.localScale.x / RainFlowMaps.Size;
            
            _obsTreshold = pDotThreshold1;
            for (int i = 0; i < RainFlowMaps.Size; i++)
            for (int j = 0; j < RainFlowMaps.Size; j++)
            {
                // if (_obstacles[i, j] == 1)
                //     _texture.SetPixel(i, j, Color.red);
                // else
                // {
                //     float flow = _flowMap[i, j] / DrippingThreshold;
                //     _texture.SetPixel(i, j, new Color(flow,flow,flow, 1f));
                // }

                //float d = ComputeObstacles(i,j);
                //float r = _affinityCoeff[i, j] < _obsTreshold1 ? 0f : 1f;
                //float f = _flowMap[i, j];// / DROP_MASS;

                //float v = r;
                //Color col  = new Color(v / (v + f), v, v / (v + f), 1f);
                
                Vector3 n = _normalMap[i, j];
                Color normalColor = new Color(n.x, n.y, n.z, 1f);
                n = n * 2f - Vector3.one;

                float c = math.abs(Vector3.Dot(normal, n));
                float f = _flowMap[i, j] / DrippingThreshold;
                
                Color flowCol  = new Color(1f / (1f + f), 1f / (1f + f), 1f, 1f);
                Color col;
                if (pShowObstacles)
                    col = c < pDotThreshold1 ? Color.red : flowCol;
                else
                    col = flowCol;

                _texture.SetPixel(i, j, col);
                
                if (i % 4 == 0 && j % 4 == 0)
                {
                    float x = (i + 0.5f) * ratio;
                    float y = (j + 0.5f) * ratio;
                    Debug.DrawLine(new Vector3(x, y, 0f), new Vector3(x, y, 0f) + n * 0.1f, normalColor, 0.1f);
                }
            }
            _texture.Apply();
        }

        // Compute the probability based on the Newton's Law
        void NewtonsLaw(Vector3 pVp, float[] pProbas)
        {
            // Compute Dsum
            float dl1 = 0f;
            float dl2 = 0f;
            Vector3 d1 = Vector3.zero;
            Vector3 d2 = Vector3.zero;

            for (int k = 0; k < 8; k++)
            {
                Vector3 d = _neighbors[k];
                float newDl = Mathf.Max(Vector3.Dot(d.normalized, pVp.normalized), 0f);
                if (newDl > dl1)
                {
                    dl2 = dl1;
                    d2 = d1;
                    dl1 = newDl;
                    d1 = d;
                }
                else if (newDl > dl2)
                {
                    dl2 = newDl;
                    d2 = d;
                }
            }

            float dsum = 1f / ((Vector3.Cross(d1, pVp).magnitude) + Epsilon) + 1f / ((Vector3.Cross(d2, pVp).magnitude) + Epsilon);

            for (int k = 0; k < 8; k++)
            {
                Vector3 d = _neighbors[k];
                float newDl = Mathf.Max(Vector3.Dot(d.normalized, pVp.normalized), 0f);
                if (Math.Abs(newDl - dl1) < 0.1f || Math.Abs(newDl - dl2) < 0.1f)
                    pProbas[k] = 1f / (dsum * (Vector3.Cross(d, pVp).magnitude + Epsilon));
                else
                    pProbas[k] = 0f;
            }
        }

        float UStepFunction(Vector3 pDk, Vector3 pVp)
        {
            return math.step(0f, ThetaC - Mathf.Acos(Vector3.Dot(pDk.normalized, pVp.normalized)));
        }

        // Compute the probability based on the surface affinity
        void Affinity(Vector3 pVp, int pI, int pJ, float[] pProbas)
        {
            float asum = 0f;

            // Compute Asum
            for (int k = 0; k < 8; k++)
            {
                Vector3 dl = _neighbors[k] + new Vector3(pI, pJ, 0f);
                if (dl.x >= 0 && dl.x < RainFlowMaps.Size && dl.y >= 0 && dl.y < RainFlowMaps.Size)
                    asum += _affinityCoeff[(int) dl.x, (int) dl.y] * UStepFunction(_neighbors[k], pVp);
            }

            if (asum == 0f)
                return;

            // Compute the probability Ak
            for (int k = 0; k < 8; k++)
            {
                Vector3 dl = _neighbors[k] + new Vector3(pI, pJ, 0f);
                if (dl.x >= 0 && dl.x < RainFlowMaps.Size && dl.y >= 0 && dl.y < RainFlowMaps.Size)
                    pProbas[k] = (_affinityCoeff[(int) dl.x, (int) dl.y] / asum) * UStepFunction(_neighbors[k], pVp);
            }
        }

        // Compute the probability based on the wet/dry conditions
        void WetDryConditions(Vector3 pVp, int pI, int pJ, float[] pProbas)
        {
            // Compute Wsum
            float wsum = 0f;

            for (int k = 0; k < 8; k++)
            {
                Vector3 dl = _neighbors[k] + new Vector3(pI, pJ, 0f);
                float gdk = 0f;
                if (dl.x >= 0 && dl.x < RainFlowMaps.Size && dl.y >= 0 && dl.y < RainFlowMaps.Size)
                    gdk = _flowMap[(int)dl.x, (int)dl.y] > 0f ? 1f : 0f;
                wsum += gdk * UStepFunction(dl, pVp);
            }

            if (wsum == 0f)
                return;

            // Compute the probability Wk
            for (int k = 0; k < 8; k++)
            {
                Vector3 dl = _neighbors[k] + new Vector3(pI, pJ, 0f);
                if (dl.x >= 0 && dl.x < RainFlowMaps.Size && dl.y >= 0 && dl.y < RainFlowMaps.Size)
                    pProbas[k] = (_flowMap[(int)(_neighbors[k].x + pI), (int)(_neighbors[k].y + pJ)] / wsum) * UStepFunction(_neighbors[k], pVp);
                else
                    pProbas[k] = 0f;
            }
        }

        // Compute the probability based on the obstacles existance
        void ObstaclesExistance(int pI, int pJ, float[] pProbas)
        {
            // Compute Ek
            for (int k = 0; k < 8; k++)
            {
                Vector3 dl = _neighbors[k] + new Vector3(pI, pJ, 0f);
                if (dl.x is >= 0 and < RainFlowMaps.Size && dl.y is >= 0 and < RainFlowMaps.Size)
                {
                    // Obstacles places manually by users
                    // pProbas[k] = _obstacles[(int)dl.x, (int)dl.y] > 0 ? 0f : 1f;
                    
                    // Compute obstacles from the normal map
                    Vector3 n = _normalMap[(int)dl.x, (int)dl.y];
                    n = n * 2f - Vector3.one;
                    
                    float cosTheta = math.abs(Vector3.Dot(_transform.forward, n));
                    pProbas[k] = cosTheta < _obsTreshold ? 0f : 1f;
                }
                else
                    pProbas[k] = 1f;
            }
        }

        // Compute the roulette direction and return the index of the direction
        int RouletteDirection(Vector3 pVp, float[] pNewtonProbas, float[] pAffinityProbas, float[] pWetDryProbas, float[] pObstaclesProbas)
        {
            float rsum = 0f;
            for (int k = 0; k < 8; k++)
            {
                float D = pNewtonProbas[k];
                float A = pAffinityProbas[k];
                float W = pWetDryProbas[k];
                float E = pObstaclesProbas[k];

                // Compute Rsum
                rsum += E * (Alpha1 * pVp.magnitude * D + Alpha2 * A + W);
            }

            if (rsum == 0f)
                return -1;

            float[] Rk = new float[8];
            for (int k = 0; k < 8; k++)
            {
                float D = pNewtonProbas[k];
                float A = pAffinityProbas[k];
                float W = pWetDryProbas[k];
                float E = pObstaclesProbas[k];

                Rk[k] = E * (Alpha1 * pVp.magnitude * D + Alpha2 * A + W) / rsum;
            }

            float r = UnityEngine.Random.Range(0f, 1f);
            int i = 0;
            float rk = Rk[i];
            while (rk < r)
            {
                i++;
                rk += Rk[i];
            }

            return i;
        }

        private static float H(float pX)
        {
            return pX >= 0.4f ? 0.1f : math.log(math.sqrt(pX) + 1f) / 2.17f;
        }

        // Compute the collision with a Mesh
        Collision ComputeMinT(Drop pDrop, Mesh pMesh, Vector3 extForce)
        {
            Collision c = new Collision
            {
                T = Mathf.Infinity,
                Axis = 0
            };
            
            float[] meshAxis = pMesh.Axis;
            //Loop through the mesh
            for (int i = 0; i < 4; i++)
            {
                int currentAxis = i < 2 ? 0 : 1; // if i equals 0 or 1, the axis is x, otherwise y
                float massForce = extForce[currentAxis] / pDrop.Mass;
                float t1 = (-pDrop.Vel[currentAxis] + math.sqrt(pDrop.Vel[currentAxis] * pDrop.Vel[currentAxis] - 4f * massForce * (pDrop.Pos[currentAxis] - meshAxis[i]))) / (2f * massForce + Epsilon);
                float t2 = (-pDrop.Vel[currentAxis] - math.sqrt(pDrop.Vel[currentAxis] * pDrop.Vel[currentAxis] - 4f * massForce * (pDrop.Pos[currentAxis] - meshAxis[i]))) / (2f * massForce + Epsilon);
                if (t1 >= 0f && c.T > t1)
                {
                    c.Axis = currentAxis;
                    c.T = t1;
                }
                if (t2 >= 0f && c.T > t2)
                {
                    c.Axis = currentAxis;
                    c.T = t2;
                }
            }
            return c;
        }

        float ComputeTravelTime(Vector3 pDp, Vector3 pAcc)
        {
            return math.sqrt(math.pow(pDp.x, 2) + math.pow(pDp.y, 2)) / pAcc.magnitude;
        }

        void UpdateParticle(List<Drop> pDrops, int pIDDrop, Vector3 pNormal)
        {
            Drop drop = pDrops[pIDDrop];
            int i = (int)drop.Pos.x;
            int j = (int)drop.Pos.y;

            if (!DropletsShouldMove(_extForce, i, j, pNormal))
                return;

            if (drop.FreezeTime > 0f)
            {
                drop.FreezeTime -= Time.deltaTime * _deltaTime;
                pDrops[pIDDrop] = drop;
                return;
            }

            _dropsContained[i, j] = false;

            Mesh currentMesh = _mesh[i, j];
            Collision col    = ComputeMinT(drop, currentMesh, _extForce);

            Vector3 vp = (_extForce / drop.Mass) * col.T + drop.Vel;

            float[] newtonProbas    = {0f,0f,0f,0f,0f,0f,0f,0f};
            float[] affinityProbas  = {0f,0f,0f,0f,0f,0f,0f,0f};
            float[] wetDryProbas    = {0f,0f,0f,0f,0f,0f,0f,0f};
            float[] obstaclesProbas = {0f,0f,0f,0f,0f,0f,0f,0f};

            NewtonsLaw(vp, newtonProbas);
            Affinity(vp, i, j, affinityProbas);
            WetDryConditions(vp, i, j, wetDryProbas);
            ObstaclesExistance(i, j, obstaclesProbas);

            int p = RouletteDirection(vp, newtonProbas, affinityProbas, wetDryProbas, obstaclesProbas);

            if (p == -1)
            {
                _drops.Remove(drop);
                return;
            }

            Vector3 dp = new Vector3(i,j,0f) + _neighbors[p];

            // if dp is outside the mesh, remove the drop from the list
            if (dp.x < 0 || dp.x >= RainFlowMaps.Size || dp.y < 0 || dp.y >= RainFlowMaps.Size)
            {
                // And generate dripping
                // if (_flowMap[i, j] >= DrippingThreshold)
                // {
                //     _flowMap[i, j] -= DrippingThreshold - 5f;
                //     _rainDripping.GenerateDripping(_transform, drop.Pos, RainFlowMaps.SIZE);
                // }
                _drops.Remove(drop);
                return;
            }

            float remainingMass = H(_affinityCoeff[i, j]) * _flowMap[i, j];

            drop.Mass -= remainingMass;
            _flowMap[i, j] = remainingMass;

            drop.Mass += _flowMap[(int)dp.x, (int)dp.y];
            _flowMap[(int)dp.x, (int)dp.y] = drop.Mass;

            // Update the drop position, velocity and freezeTime
            drop.Pos = dp;

            // Merging the two drops
            Drop oldDrop = new Drop();
            bool existedDrop = false;
            if (_dropsContained[(int)dp.x, (int)dp.y])
            { 
                foreach(Drop d in _drops)
                {
                    if (d.Pos.Equals(dp))
                    {
                        existedDrop = true;
                        oldDrop = d;
                        drop.Vel = (drop.Mass * drop.Vel + oldDrop.Mass * oldDrop.Vel) / (drop.Mass + oldDrop.Mass);
                    }
                }
            }
        
            _dropsContained[(int)dp.x, (int)dp.y] = true;

            Vector3 v0P = Vector3.Dot(drop.Vel, _neighbors[p]) * _neighbors[p].normalized;
            Vector3 v0Perp = drop.Vel - v0P;
            drop.Vel = v0P + v0Perp;

            drop.FreezeTime = ComputeTravelTime(_neighbors[p], _extForce / drop.Mass);
            pDrops[pIDDrop] = drop;
        
            if (existedDrop)
                _drops.Remove(oldDrop);
        }

        bool DropletsShouldMove(Vector3 pExtForce, int pI, int pJ, Vector3 pNormal)
        {
            // Compute the distance between the external force and the plane
            float dist = Vector3.Dot(pExtForce, pNormal);

            // Compute the projection of the external force on the plane normal
            Vector3 proj = pExtForce - dist * pNormal;

            // Compute the critical force
            float fcrit = BetaS * _affinityCoeff[pI, pJ];

            // Return true if the projection magnitude is greater than the critical force
            return proj.magnitude >= fcrit;
        }

        public void Update(Vector3 pNormal)
        {
            // Loop through the drops and call UpdateParticle for each
            for (int i = 0; i < _drops.Count; i++)
                UpdateParticle(_drops, i, pNormal);

            _rainDripping.Draw(_transform);
        }

        public Texture2D GetTexture()
        {
            return _texture;
        }

        public void OnDisable()
        {
            _rainDripping.OnDisable();
        }
    }
}
