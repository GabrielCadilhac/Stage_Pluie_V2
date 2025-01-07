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

    public RainRenderer(Material p_material, Bounds p_bounds, Vector3 p_min, Vector3 p_max, Transform p_transform)
    {
        _material = p_material;
		_material.enableInstancing = true;
        _bounds = p_bounds;
        _transform = p_transform;
        
        _mesh = new Mesh() { name = "RainMesh" };
        
        // Generate positions
        Vector3[] positions = new Vector3[RainManager._nbParticles];
        for (int i = 0; i < RainManager._nbParticles; i++)
        {
            positions[i] = new Vector3(
                Random.Range(p_min.x, p_max.x),
                Random.Range(p_min.y, p_max.y),
                Random.Range(p_min.z, p_max.z));
        }

        // Mesh initialization
        var layout = new[]
        {
                new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3),
        };

        int[] indices = new int[RainManager._nbParticles];
        for (int i = 0; i < RainManager._nbParticles; i++)
            indices[i] = i;

        _mesh.SetVertices(positions);
        _mesh.SetIndices(indices, MeshTopology.Points, 0);
        _mesh.SetVertexBufferParams(RainManager._nbParticles, layout);
        _mesh.bounds = p_bounds;
        _mesh.hideFlags = HideFlags.HideAndDontSave;
        _mesh.vertexBufferTarget |= GraphicsBuffer.Target.Raw;
        
        _posBuffer = _mesh.GetVertexBuffer(0);
        _material.SetInteger("_ParticlesNumber", RainManager._nbParticles);
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

    public void SetWindRotation(float p_forceRotation = 1f)
    {
        _material.SetFloat("_ForceRotation", p_forceRotation);
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

    public Mesh GetMesh()
    {
        return _mesh;
    }

    public Material GetMaterial()
    {
        return _material;
    }

    public void SetParticles(Vector3[] p_newPos)
    {
        _mesh.SetVertices(p_newPos);
    }

    public void Disable()
    {
        _posBuffer.Release();
        _posBuffer = null;
    }
}