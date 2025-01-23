using System;
using System.Collections.Generic;
using UnityEngine;

namespace Test
{
    public class ProceduralFlow : MonoBehaviour
    {
        private const int Size = 128;
        private const float InvSqrt2 = 0.70710678118f;
        private const float Epsilon = 0.000001f;

        private readonly Vector2Int[] _neighbors =
        {
            new (-1, -1), // BL
            new (-1, 0),  // L
            new (-1, 1),  // TL
            new (0, 1),   // T
            new (1, 1),   // TR
            new (1, 0),   // R
            new (1, -1),  // BR
            new (0, -1),  // B
        };

        private float[,] _heightMap;
        private float _maxHeight;

        private float[,] _catchment;
        private float _maxCatchment;
        
        [SerializeField] [Range(0f, 1f)] private float waterLevel;
        private float _oldWaterLvl;

        [SerializeField] private MeshFilter waterMeshFilter;
        [SerializeField] private Texture2D heightMap;
        [SerializeField] private float heightScale;
        [SerializeField] private float meshScale;

        private Vector3[] _waterVertices;
        private float _minWaterLvl, _maxWaterLvl;

        private void Start()
        {
            _maxCatchment = 0;
            _minWaterLvl = 0f;
            _maxWaterLvl = 50f;
            _oldWaterLvl = waterLevel;
            _waterVertices = new Vector3[(Size + 1) * (Size + 1)];

            float textScale = (float)heightMap.width / Size;
            _catchment = new float[Size + 1, Size + 1];
            _heightMap = new float[Size + 1, Size + 1];
            _maxHeight = 0f;
            for (int i = 0; i < Size + 1; i++)
            for (int j = 0; j < Size + 1; j++)
            {
                _catchment[i, j] = 0f;
                _heightMap[i, j] = heightMap.GetPixel((int)(i * textScale), (int)(j * textScale)).r * heightScale - 7.3f;
                if (_heightMap[i,j] > _maxHeight)
                    _maxHeight = _heightMap[i,j];
            }

            for (int i = 0; i < Size + 1; i++)
            for (int j = 0; j < Size + 1; j++)
                ComputeCatchment(i, j);
            Debug.Log($"{_maxCatchment}");
            
            GetComponent<MeshFilter>().mesh = GenerateMesh(_heightMap);
            waterMeshFilter.mesh = GenerateMesh(null);
            _waterVertices = waterMeshFilter.mesh.vertices;
        }

        private float WaterHeight(int pI, int pJ, float pWaterLevel)
        {
            return (1f - pWaterLevel) * _minWaterLvl + pWaterLevel * _maxWaterLvl;
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
                // colors[id] = new Color(height/_maxHeight, height/_maxHeight, height/_maxHeight, 1f);
                colors[id] = new Color(_catchment[i,j]/_maxCatchment, 0f, 0f, 1f);
                if (_catchment[i,j] >= _maxCatchment-500f)
                    colors[id] = Color.green;
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
            if (Math.Abs(_oldWaterLvl - waterLevel) > Epsilon)
            {
                _oldWaterLvl = waterLevel;
                UpdateWaterMesh();
            }
        }
    }
}