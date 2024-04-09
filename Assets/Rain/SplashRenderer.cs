using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class SplashRenderer
{
    private Material _material;
    private Mesh _mesh;
    private GraphicsBuffer _posBuffer;
    private ComputeBuffer _timeBuffer;
    public SplashRenderer(Material p_material, Bounds p_bounds, Transform p_transform, int p_nbMaxParticles = 1000)
    {
        _timeBuffer = new ComputeBuffer(p_nbMaxParticles, sizeof(float));

        _material = p_material;
        _material.enableInstancing = true;
        _material.SetBuffer("TimeBuffer", _timeBuffer);

        _mesh = new Mesh() { name = "SplashMesh" };

        Vector3[] positions = new Vector3[p_nbMaxParticles];
        int[] indices = new int[p_nbMaxParticles];
        for (int i = 0; i < p_nbMaxParticles; i++)
        {
            positions[i] = Vector3.zero;
            indices[i]   = i;
        }

        // Mesh initialization
        var layout = new[]
        {
                new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3),
        };

        _mesh.SetVertices(positions);
        _mesh.SetIndices(indices, MeshTopology.Points, 0);
        _mesh.SetVertexBufferParams(p_nbMaxParticles, layout);
        _mesh.bounds = p_bounds;
        _mesh.hideFlags = HideFlags.HideAndDontSave;
        _mesh.vertexBufferTarget |= GraphicsBuffer.Target.Raw;
        _posBuffer = _mesh.GetVertexBuffer(0);
    }

    public Mesh GetMesh()
    { 
        return _mesh;
    }

    public GraphicsBuffer GetPositions()
    {
        return _posBuffer;
    }

    public ComputeBuffer GetTimeSplash()
    {
        return _timeBuffer;
    }

    public void Disable()
    {
        _posBuffer.Release();
        _posBuffer = null;

        _timeBuffer.Release();
        _timeBuffer = null;
    }
}
