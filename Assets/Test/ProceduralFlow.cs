using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ProceduralFlow : MonoBehaviour
{
    private const int SIZE = 64;
    private Mesh _mesh;

    private Vector3[] _vertices;
    private List<int> _indeces;
    private Vector3[] _normals;

    [SerializeField] private Texture2D _heightMap;
    [SerializeField] private float _heightScale;
    [SerializeField] private float _meshScale;

    void Start()
    {
        _mesh = new Mesh()
        {
            name = "FlowMesh"
        };

        float textScale = (float)_heightMap.width / (float) SIZE;

        _vertices = new Vector3[(SIZE + 1) * (SIZE + 1)];
        _normals  = new Vector3[(SIZE + 1) * (SIZE + 1)];
        for (int i = 0; i < SIZE + 1; i++)
        {
            for (int j = 0; j < SIZE + 1; j++)
            {
                int id = i*(SIZE+1) + j;
                float height  = _heightMap.GetPixel((int) (i * textScale), (int) (j * textScale)).g;
                _vertices[id] = new Vector3(i * _meshScale, (height-0.451f) * _heightScale, j * _meshScale);
                _normals[id]  = Vector3.up;
            }
        }

        _indeces = new List<int>();
        for (int i = 0; i < SIZE; i++)
        {
            for (int j = 0; j < SIZE; j++)
            {
                int id = i * (SIZE+1) + j;
                _indeces.Add(id);
                _indeces.Add(id + 1);
                _indeces.Add(id + SIZE + 1);

                _indeces.Add(id + 1);
                _indeces.Add(id + SIZE + 1);
                _indeces.Add(id + SIZE + 2);
            }
        }

        _mesh.vertices  = _vertices;
        _mesh.triangles = _indeces.ToArray();
        _mesh.normals   = _normals.ToArray();

        GetComponent<MeshFilter>().mesh = _mesh;
    }

    void Update()
    {
        
    }
}
