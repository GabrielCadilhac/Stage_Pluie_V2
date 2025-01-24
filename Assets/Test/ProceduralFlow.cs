using System;
using System.Collections.Generic;
using Rain_Wind_Interaction.Commons;
using UnityEngine;

namespace Test
{
    public class ProceduralFlow : MonoBehaviour
    {
        struct Primitive
        {
            public Vector3 Pos;
            public float Radius;
            public float Catchment;
        };
        
        private const int Size = 128;
        private const float InvSqrt2 = 0.70710678118f;
        private const float Epsilon = 0.000001f;
        private const float CatchmentCoeff = 0.01f;
        
        private readonly Vector2Int[] _neighbors =
        {
            new (-1, -1), // BL
            new (-1,  0), // L
            new (-1,  1), // TL
            new ( 0,  1), // T
            new ( 1,  1), // TR
            new ( 1,  0), // R
            new ( 1, -1), // BR
            new ( 0, -1), // B
        };

        private float[,] _heightMap;
        private float _maxHeight;
        private float[,] _catchment;
        private float _maxCatchment;

        private List<Primitive> _primitives;
        
        [SerializeField] [Range(0f, 10f)] private float testMax = 1f;
        [SerializeField] [Range(0f, 1f)] private float primitiveRadius = 1f;
        [SerializeField] [Range(0f, 1f)] private float waterLevel;
        private float _oldWaterLvl, _oldRadius, _oldMaxTest;

        [SerializeField] private MeshFilter waterMeshFilter;
        [SerializeField] private Texture2D heightMap;
        [SerializeField] private float heightScale;
        [SerializeField] private float meshScale;

        private Vector3[] _waterVertices;
        private readonly float _minWaterLvl = 0f;
        private readonly float _maxWaterLvl = 50f;

        private void Start()
        {
            _oldWaterLvl = waterLevel;
            _oldRadius = primitiveRadius;
            _oldMaxTest = testMax;
            _waterVertices = new Vector3[(Size + 1) * (Size + 1)];
            _primitives = new List<Primitive>();

            float textScale = (float)heightMap.width / Size;
            _catchment = new float[Size + 1, Size + 1];
            _heightMap = new float[Size + 1, Size + 1];
            for (int i = 0; i < Size + 1; i++)
            for (int j = 0; j < Size + 1; j++)
            {
                _catchment[i, j] = 0f;
                _heightMap[i, j] = heightMap.GetPixel((int)(i * textScale), (int)(j * textScale)).r * heightScale - 7.3f;
                if (_heightMap[i,j] > _maxHeight)
                    _maxHeight = _heightMap[i,j];
            }

            // for (int i = 0; i < Size + 1; i++)
            // for (int j = 0; j < Size + 1; j++)
            //     ComputeCatchment(i, j);
            // Debug.Log($"MaxCatchment {_maxCatchment}");
            

            float[,] newHeightMap = PriorityFlood();

            GetComponent<MeshFilter>().mesh = GenerateMesh(_heightMap);
            waterMeshFilter.mesh = GenerateMesh(newHeightMap);
            _waterVertices = waterMeshFilter.mesh.vertices;
            //UpdateWaterMesh();
        }

        // Detect and fill pits (or depression)
        private float[,] PriorityFlood()
        {
            PriorityQueue<Vector2Int> open = new();
            bool[,] closed = new bool[Size + 1, Size + 1];
            float[,] newHeight = new float[Size + 1, Size + 1];
            for (int i = 0; i < Size + 1; i++)
            for (int j = 0; j < Size + 1; j++)
                closed[i, j] = false;
            
            // For all terrain edges
            for (int i = 0; i < Size + 1; i++)
            {
                open.Enqueue(new Vector2Int(i, 0),       _heightMap[i, 0]);
                open.Enqueue(new Vector2Int(i, Size), _heightMap[i, Size]);
                open.Enqueue(new Vector2Int(0, i),       _heightMap[0, i]);
                open.Enqueue(new Vector2Int(Size, i), _heightMap[Size, i]);

                closed[i, 0]    = true;
                closed[i, Size] = true;
                closed[0, i]    = true;
                closed[Size, i] = true;
                
                newHeight[i, 0]    = _heightMap[i, 0];
                newHeight[i, Size] = _heightMap[i, Size];
                newHeight[0, i]    = _heightMap[0, i];
                newHeight[Size, i] = _heightMap[Size, i];
            }

            while (open.Count > 0)
            {
                Vector2Int c = open.Dequeue();
                for (int k = 0; k < 8; k++)
                {
                    Vector2Int n = c + _neighbors[k];
                    if (n.x < 0 || n.x >= Size+1 || n.y < 0 || n.y >= Size+1) continue;

                    if (!closed[n.x, n.y])
                    {
                        newHeight[n.x,n.y] = Mathf.Max(newHeight[c.x,c.y], _heightMap[n.x,n.y])-0.001f;
                        closed[n.x, n.y] = true;
                        open.Enqueue(n, newHeight[n.x, n.y]);
                    }
                }
            }

            return newHeight;
        }

        private float WaterHeight(int pI, int pJ, float pWaterLevel)
        {
            float waterLerp = (1f - pWaterLevel) * _minWaterLvl + pWaterLevel * _maxWaterLvl;
            
            for (int i = 0; i < _primitives.Count; i++)
            {
                Primitive prim  = _primitives[i];
                Vector2 primPos = new Vector2(prim.Pos.x, prim.Pos.z);
                float dist = Vector2.Distance(primPos, new Vector2(pI, pJ)*meshScale); 
                if (dist <= prim.Radius)
                    return _heightMap[pI, pJ] + Mathf.Min(testMax,(_catchment[pI,pJ]/_maxCatchment)*waterLerp - 0.1f);

                // height -= Mathf.Min((primPos - new Vector2(pI, pJ) * meshScale).magnitude - primitiveRadius, 0f);
            }
            // height = Mathf.Clamp(height, 0f, waterLerp);
            // return Mathf.Min(testMax,height + _heightMap[pI, pJ] - 0.1f);

            return _heightMap[pI, pJ] - 0.1f;
        }

        private float ComputeCatchment(int pI, int pJ)
        {
            if (_catchment[pI, pJ] > 0) return _catchment[pI, pJ];

            _catchment[pI, pJ] = 1;
            float currentHeight = _heightMap[pI, pJ];
            for (int k = 0; k < 8; k++)
            {
                int x = pI + _neighbors[k].x;
                int y = pJ + _neighbors[k].y;
                if (x < 0 || x >= Size+1 || y < 0 || y >= Size+1) continue;
            
                if (_heightMap[x, y] > currentHeight)
                {
                    float fractFlow = FractFlow(pI, pJ, x, y);
                    if (fractFlow > 0f)
                        _catchment[pI, pJ] += fractFlow * ComputeCatchment(x, y);
                }
            }

            if (_catchment[pI, pJ] > _maxCatchment)
                _maxCatchment = _catchment[pI, pJ];
            return _catchment[pI, pJ];
        }
        
        private float FractFlow(int pI, int pJ, int pX, int pY)
        {
            float sum = 0f;
            for (int k = 0; k < 8; k++)
            {
                int pX2 = pX + _neighbors[k].x;
                int pY2 = pY + _neighbors[k].y;
                if(pX2 < 0 || pX2 >= Size+1 || pY2 < 0 || pY2 >= Size+1) continue;
                
                if (_heightMap[pX, pY] > _heightMap[pX2, pY2])
                    sum += DownSlope(pX, pY, pX2, pY2);
            }

            if (sum == 0f)
                return DownSlope(pX, pY, pI, pJ);
            return  DownSlope(pX, pY, pI, pJ) / sum;
        }

        private float DownSlope(int pX1, int pY1, int pX2, int pY2, float pP = 1.1f)
        {
            float div = pX2 == 0 || pY2 == 0 ? 1f : InvSqrt2;
            return Mathf.Pow((_heightMap[pX1, pY1] - _heightMap[pX2, pY2]) * div, pP);
        }

        private Mesh GenerateMesh(float[,] pHeightMap)
        {
            Mesh mesh = new Mesh()
            {
                name = "FlowMesh"
            };

            Vector3[] vertices = new Vector3[(Size + 1) * (Size + 1)];
            Vector3[] normals = new Vector3[(Size + 1) * (Size + 1)];
            List<int> indices = new List<int>();
            Color[] colors = new Color[(Size + 1) * (Size + 1)];

            for (int i = 0; i < Size + 1; i++)
            for (int j = 0; j < Size + 1; j++)
            {
                int id = i * (Size + 1) + j;
                float height = pHeightMap != null ? pHeightMap[i,j] : 0f;
                vertices[id] = new Vector3(i * meshScale, height, j * meshScale);
                normals[id] = Vector3.up;
                if (pHeightMap == null)
                {
                    colors[id] = Color.blue;
                }
                else
                {
                    colors[id] = new Color(height/_maxHeight,height/_maxHeight,height/_maxHeight, 1f);
                    // colors[id] = new Color(_catchment[i,j]/_maxCatchment, 0f, 0f, 1f);
                    // if (_catchment[i, j] >= _maxCatchment - 500f)
                    // {
                    //     colors[id] = Color.green;
                    //     Primitive p = new Primitive
                    //     {
                    //         Pos = vertices[id],
                    //         Radius = primitiveRadius,
                    //         Catchment = _catchment[i,j],
                    //     };
                    //     _primitives.Add(p);
                    // }
                }
            }

            for (int i = 0; i < Size; i++)
            for (int j = 0; j < Size; j++)
            {
                int id = i * (Size + 1) + j;
                indices.Add(id);
                indices.Add(id + 1);
                indices.Add(id + Size + 1);

                indices.Add(id + 1);
                indices.Add(id + Size + 1);
                indices.Add(id + Size + 2);
            }

            mesh.vertices = vertices;
            mesh.triangles = indices.ToArray();
            mesh.normals = normals;
            mesh.colors = colors;

            return mesh;
        }

        private void UpdateWaterMesh()
        {
            for (int i = 0; i < Size + 1; i++)
            for (int j = 0; j < Size + 1; j++)
            {
                int id = i * (Size + 1) + j;
                _waterVertices[id].y = WaterHeight(i, j, waterLevel);
            }
            waterMeshFilter.mesh.vertices = _waterVertices;
        }

        void Update()
        {
            if (Math.Abs(_oldWaterLvl - waterLevel) > Epsilon || Mathf.Abs(_oldRadius - primitiveRadius) > Epsilon || Mathf.Abs(_oldMaxTest - testMax)  > Epsilon)
            {
                _oldWaterLvl = waterLevel;
                _oldRadius = primitiveRadius;
                _oldMaxTest = testMax;

                for (int i = 0; i < _primitives.Count; i++)
                {
                    Primitive p = _primitives[i];
                    p.Radius = primitiveRadius * p.Catchment * CatchmentCoeff;
                    _primitives[i] = p;
                }
                UpdateWaterMesh();
            }
        }
    }
}