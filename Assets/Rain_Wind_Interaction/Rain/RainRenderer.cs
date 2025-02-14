using UnityEngine;
using UnityEngine.Rendering;

public class RainRenderer
{
    // Renderer elements
    private GraphicsBuffer _posBuffer;
    private Material _material;
    private Mesh _mesh;
    private Bounds _bounds;
    private Transform _transform;

    public RainRenderer(Material pMaterial, Bounds pBounds, Vector3 pMin, Vector3 pMax, Transform pTransform)
    {
        _material = pMaterial;
		_material.enableInstancing = true;
        _bounds = pBounds;
        _transform = pTransform;
        
        _mesh = new Mesh() { name = "RainMesh" };
        
        // Generate positions
        Vector3[] positions = new Vector3[RainManager.NbParticles];
        for (int i = 0; i < RainManager.NbParticles; i++)
        {
            positions[i] = new Vector3(
                Random.Range(pMin.x, pMax.x),
                Random.Range(pMin.y, pMax.y),
                Random.Range(pMin.z, pMax.z));
        }

        // Mesh initialization
        var layout = new[]
        {
                new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3),
        };

        int[] indices = new int[RainManager.NbParticles];
        for (int i = 0; i < RainManager.NbParticles; i++)
            indices[i] = i;

        _mesh.SetVertices(positions);
        _mesh.SetIndices(indices, MeshTopology.Points, 0);
        _mesh.SetVertexBufferParams(RainManager.NbParticles, layout);
        _mesh.bounds = pBounds;
        _mesh.hideFlags = HideFlags.HideAndDontSave;
        _mesh.vertexBufferTarget |= GraphicsBuffer.Target.Raw;
        
        _posBuffer = _mesh.GetVertexBuffer(0);
        _material.SetInteger("_ParticlesNumber", RainManager.NbParticles);
    }

    public void Draw()
    {
        RenderParams rp = new RenderParams(_material);
        rp.worldBounds = new Bounds(_transform.position, _bounds.size); ;
        rp.matProps = new MaterialPropertyBlock();
        rp.matProps.SetMatrix("_ObjectToWorld", Matrix4x4.identity);
        rp.matProps.SetFloat("_NumInstances", 1f);
        Graphics.RenderMeshPrimitives(rp, _mesh, 0, 1);
    }

    public void SetWindRotation(float pForceRotation = 1f)
    {
        _material.SetFloat("_ForceRotation", pForceRotation);
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

    public Mesh GetMesh()
    {
        return _mesh;
    }

    public Material GetMaterial()
    {
        return _material;
    }

    public void SetParticles(Vector3[] pNewPos)
    {
        _mesh.SetVertices(pNewPos);
    }

    public void Disable()
    {
        _posBuffer.Release();
        _posBuffer = null;
    }
}