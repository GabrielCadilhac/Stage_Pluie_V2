using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SplashRenderer2
{
    private GraphicsBuffer _posBuffer, _normalBuffer;
    private Material _material;

    public SplashRenderer2(Material p_material)
    {
        Vector3[] positions = new Vector3[RainManager._nbParticles];
        for (int i = 0; i  < RainManager._nbParticles; i++)
            positions[i] = Vector3.zero;

        _posBuffer    = new GraphicsBuffer(GraphicsBuffer.Target.Raw, RainManager._nbParticles, 3 * sizeof(float));
        _normalBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Raw, RainManager._nbParticles, 3 * sizeof(float));

        _material = p_material;
        _material.enableInstancing = true;
        _material.SetBuffer("Position", _posBuffer);
        _material.SetBuffer("Normale", _normalBuffer);
    }

    public void Draw()
    {
        RenderParams rp = new RenderParams(_material);
        rp.worldBounds = new Bounds(Vector3.zero, Vector3.one * 1000f);
        rp.matProps = new MaterialPropertyBlock();
        Graphics.RenderPrimitives(rp, MeshTopology.Quads, 4, RainManager._nbParticles);
    }

    public GraphicsBuffer GetPosBuffer()
    {
        return _posBuffer;
    }

    public GraphicsBuffer GetNormalBuffer()
    {
        return _normalBuffer;
    }

    public void Disable()
    {
        _posBuffer.Dispose();
        _posBuffer = null;

        _normalBuffer.Release();
        _normalBuffer = null;
    }
}
