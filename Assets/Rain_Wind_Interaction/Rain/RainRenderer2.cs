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

    public RainRenderer2(Material pMaterial, Bounds pBounds, Vector3 pMin, Vector3 pMax, Transform pTransform)
    {
        _material = pMaterial;
        _material.enableInstancing = true;
        _bounds = pBounds;
        _transform = pTransform;

        Vector3[] tempPos = new Vector3[RainManager.NbParticles];
        for (int i = 0; i < RainManager.NbParticles; i++)
            tempPos[i] = new Vector3(
                            Random.Range(pMin.x, pMax.x),
                            Random.Range(pMin.y, pMax.y),
                            Random.Range(pMin.z, pMax.z));

        _posBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Raw, RainManager.NbParticles, 3*sizeof(float));
        _posBuffer.SetData(tempPos);

        _material.SetBuffer("Position", _posBuffer);
    }

    public void Draw()
    {
        RenderParams rp = new RenderParams(_material);
        rp.worldBounds  = new Bounds(_transform.position, _bounds.size);
        rp.matProps     = new MaterialPropertyBlock();
        Graphics.RenderPrimitives(rp, MeshTopology.Quads, 4, RainManager.NbParticles);
    }

    public void SetVelBuffer(ComputeBuffer pVelBuffer)
    {
        _material.SetBuffer("Velocities", pVelBuffer);
    }

    public void SetSizeBuffer(ComputeBuffer pSizeBuffer)
    {
        _material.SetBuffer("Sizes", pSizeBuffer);
    }

    public GraphicsBuffer GetPositionsBuffer()
    {
        return _posBuffer;
    }

    public void SetParticles(Vector3[] pNewPos)
    {
        //_mesh.SetVertices(p_newPos);
    }

    public void Disable()
    {
        _posBuffer.Release();
        _posBuffer = null;
    }
}