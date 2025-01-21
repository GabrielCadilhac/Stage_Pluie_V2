using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class RainRenderer2
{
    // Renderer elements
    private GraphicsBuffer _posBuffer;
    private Material _material;
    private Bounds _bounds;
    private Transform _transform;

    public RainRenderer2(Material p_material, Bounds p_bounds, Vector3 p_min, Vector3 p_max, Transform p_transform)
    {
        _material = p_material;
        _material.enableInstancing = true;
        _bounds = p_bounds;
        _transform = p_transform;

        Vector3[] tempPos = new Vector3[RainManager._nbParticles];
        for (int i = 0; i < RainManager._nbParticles; i++)
            tempPos[i] = new Vector3(
                            Random.Range(p_min.x, p_max.x),
                            Random.Range(p_min.y, p_max.y),
                            Random.Range(p_min.z, p_max.z));

        _posBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Raw, RainManager._nbParticles, 3*sizeof(float));
        _posBuffer.SetData(tempPos);

        _material.SetBuffer("Position", _posBuffer);
    }

    public void Draw()
    {
        RenderParams rp = new RenderParams(_material);
        rp.worldBounds  = new Bounds(_transform.position, _bounds.size);
        rp.matProps     = new MaterialPropertyBlock();
        Graphics.RenderPrimitives(rp, MeshTopology.Quads, 4, RainManager._nbParticles);
    }

    public void SetVelBuffer(ComputeBuffer p_velBuffer)
    {
        _material.SetBuffer("Velocities", p_velBuffer);
    }

    public void SetSizeBuffer(ComputeBuffer p_sizeBuffer)
    {
        _material.SetBuffer("Sizes", p_sizeBuffer);
    }

    public GraphicsBuffer GetPositionsBuffer()
    {
        return _posBuffer;
    }

    public void SetParticles(Vector3[] p_newPos)
    {
        //_mesh.SetVertices(p_newPos);
    }

    public void Disable()
    {
        _posBuffer.Release();
        _posBuffer = null;
    }
}