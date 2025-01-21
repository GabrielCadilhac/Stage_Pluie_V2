using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ProceduralFlow : MonoBehaviour
{
    private const int SIZE = 64;

    [SerializeField] private Texture2D _heightMap;
    [SerializeField] private float _heightScale;
    [SerializeField] private float _meshScale;

    void Start()
    {
        GetComponent<MeshFilter>().mesh = GenerateMesh(_heightMap, _heightScale);
    }

    Mesh GenerateMesh(Texture2D p_heightMap, float p_heightScale)
    {
        Mesh mesh = new Mesh()
        {
            name = "FlowMesh"
        };

        float textScale = (float)p_heightMap.width / (float)SIZE;

        Vector3[] vertices = new Vector3[(SIZE + 1) * (SIZE + 1)];
        Vector3[] normals = new Vector3[(SIZE + 1) * (SIZE + 1)];
        List<int> indeces = new List<int>();

        for (int i = 0; i < SIZE + 1; i++)
        {
            for (int j = 0; j < SIZE + 1; j++)
            {
                int id = i * (SIZE + 1) + j;
                float height = p_heightMap.GetPixel((int)(i * textScale), (int)(j * textScale)).g;
                vertices[id] = new Vector3(i * _meshScale, (height - 0.451f) * p_heightScale, j * _meshScale);
                normals[id] = Vector3.up;
            }
        }

        for (int i = 0; i < SIZE; i++)
        {
            for (int j = 0; j < SIZE; j++)
            {
                int id = i * (SIZE + 1) + j;
                indeces.Add(id);
                indeces.Add(id + 1);
                indeces.Add(id + SIZE + 1);

                indeces.Add(id + 1);
                indeces.Add(id + SIZE + 1);
                indeces.Add(id + SIZE + 2);
            }
        }

        mesh.vertices = vertices;
        mesh.triangles = indeces.ToArray();
        mesh.normals = normals.ToArray();

        return mesh;
    }

    void Update()
    {
        
    }
}
