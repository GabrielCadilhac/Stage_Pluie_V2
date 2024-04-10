using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

public class RainRenderer
{
    // Renderer elements
    private GraphicsBuffer _posBuffer;
    private Material _material;
    private Mesh _mesh;

    public RainRenderer(Material p_material, Bounds p_minMax, Transform p_transform, int p_nbMaxParticles = 1000)
    {
        _material = p_material;
		_material.enableInstancing = true;
        
        _mesh = new Mesh() { name = "RainMesh" };

        Vector3 min = p_minMax.center - p_minMax.size / 2f;
        Vector3 max = p_minMax.center + p_minMax.size / 2f;

        // Generate positions
        Vector3[] positions = new Vector3[p_nbMaxParticles];
        for (int i = 0; i < p_nbMaxParticles; i++)
        {
            positions[i] = p_transform.InverseTransformPoint( new Vector3(
                Random.Range(min.x, max.x),
                Random.Range(min.y, max.y),
                Random.Range(min.z, max.z)) );
        }

        // Mesh initialization
        var layout = new[]
        {
                new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3),
        };

        int[] indices = new int[p_nbMaxParticles];
        for (int i = 0; i < p_nbMaxParticles; i++)
            indices[i] = i;

        _mesh.SetVertices(positions);
        _mesh.SetIndices(indices, MeshTopology.Points, 0);
        _mesh.SetVertexBufferParams(p_nbMaxParticles, layout);
        _mesh.bounds = p_minMax;
        _mesh.hideFlags = HideFlags.HideAndDontSave;
        _mesh.vertexBufferTarget |= GraphicsBuffer.Target.Raw;
        _posBuffer = _mesh.GetVertexBuffer(0);

        _material.SetInteger("_ParticlesNumber", p_nbMaxParticles);
    }

    public void SetWindRotation(Vector3 p_rotation, float p_forceRotation = 1f)
    {
        _material.SetVector("_WindRotation", p_rotation);
        _material.SetFloat("_ForceRotation", p_forceRotation);
    }

    public GraphicsBuffer GetPositionsBuffer()
    {
        return _posBuffer;
    }

    public Mesh GetMesh()
    {
        return _mesh;
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