using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

public class SplashRenderer
{
    private GraphicsBuffer _posBuffer;
    private ComputeBuffer _timeBuffer;
    private Material _material;
    private Mesh _mesh;

    private Transform _transform;
    private Bounds _bounds;
    public SplashRenderer(Material p_material, Bounds p_bounds, Transform p_transform)
    {
        Vector3[] positions = new Vector3[RainManager._nbParticles];
        float[] times = new float[RainManager._nbParticles];
        int[] indices = new int[RainManager._nbParticles];
        for (int i = 0; i < RainManager._nbParticles; i++)
        {
            positions[i] = new Vector3(-168f, 50f, 68f);
            indices[i]   = i;
            times[i]     = 0f;
        }

        _timeBuffer = new ComputeBuffer(RainManager._nbParticles, sizeof(float));
        _timeBuffer.SetData(times);

        _material = p_material;
        _material.enableInstancing = true;
        _material.SetBuffer("TimeBuffer", _timeBuffer);

        _mesh = new Mesh() { name = "SplashMesh" };

        _transform = p_transform;
        _bounds = p_bounds;

        // Mesh initialization
        var layout = new[]
        {
            new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3),
        };

        _mesh.SetVertices(positions);
        _mesh.SetIndices(indices, MeshTopology.Points, 0);
        _mesh.SetVertexBufferParams(RainManager._nbParticles, layout);
        _mesh.bounds = p_bounds;
        _mesh.hideFlags = HideFlags.HideAndDontSave;
        _mesh.vertexBufferTarget |= GraphicsBuffer.Target.Raw;

        _posBuffer = _mesh.GetVertexBuffer(0);
    }

    public void Draw()
    {
        RenderParams rp = new RenderParams(_material);
        rp.worldBounds = new Bounds(_transform.position, _bounds.center); ;
        rp.matProps = new MaterialPropertyBlock();
        rp.matProps.SetMatrix("_ObjectToWorld", Matrix4x4.identity);
        rp.matProps.SetFloat("_NumInstances", 1f);
        Graphics.RenderMeshPrimitives(rp, _mesh, 0, 1);
    }

    public Mesh GetMesh()
    { 
        return _mesh;
    }

    public GraphicsBuffer GetPosBuffer()
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
