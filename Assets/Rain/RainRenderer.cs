using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class RainRenderer
{
    struct StrLight
    {
        public Vector3 position;
        public Color color;
        public float intensity;
    }

    // Renderer elements
    private GraphicsBuffer _posBuffer;
    private Material _material;
    private Mesh _mesh;

    // Lights
    private ComputeBuffer _lightsBuffer;
    private List<StrLight> _lights;
    private Transform _lightTransform;


    public RainRenderer(Material p_material, Bounds p_minMax, Transform p_transform, int p_nbMaxParticles = 1000)
    {
        _material = p_material;
		_material.enableInstancing = true;
        _lightTransform = GameObject.Find("Lights").transform;
        _lights = new List<StrLight>();
        
        for (int i = 0; i < _lightTransform.transform.childCount; i++)
        {
            Transform currentLight = _lightTransform.transform.GetChild(i);
            StrLight strLight = new StrLight();
            strLight.position = currentLight.position;
            strLight.color = currentLight.GetComponent<Light>().color;
            strLight.intensity = currentLight.GetComponent<Light>().intensity;

            _lights.Add(strLight);
        }

        // Vector3 + Color + Float
        _lightsBuffer = new ComputeBuffer(_lightTransform.transform.childCount, sizeof(float) * 3 + sizeof(float) * 4 + sizeof(float));
        _material.SetInteger("_LightNumber", _lights.Count); 
        
        _lightsBuffer.SetData(_lights);
        _material.SetBuffer("Lights", _lightsBuffer);

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

    private bool LightChanged(Transform p_lightTransform, StrLight p_strLight)
    {
        return !((p_lightTransform.position == p_strLight.position) &&
               (p_lightTransform.GetComponent<Light>().color == p_strLight.color) &&
               (p_lightTransform.GetComponent<Light>().intensity == p_strLight.intensity));
    }

    public void UpdateLights()
    {
        bool lightChanged = false;
        for (int i = 0; i < _lightTransform.transform.childCount; i++)
        {
            Transform lightTransform = _lightTransform.transform.GetChild(i);
            StrLight strLight = _lights[i];

            if (LightChanged(lightTransform, strLight))
            {
                lightChanged = true;
                
                Light lightComp = lightTransform.GetComponent<Light>();
                strLight.position = lightTransform.position;
                strLight.color = lightComp.color;
                strLight.intensity = lightComp.intensity;

                _lights[i] = strLight;
            }
        }

        if (lightChanged)
        {
            _lightsBuffer.SetData(_lights);
            _material.SetBuffer("Lights", _lightsBuffer);
        }
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

    public void SetParticles(Vector3[] p_newPos)
    {
        _mesh.SetVertices(p_newPos);
    }

    public void Disable()
    {
        _posBuffer.Release();
        _posBuffer = null;

        _lightsBuffer.Release();
        _lightsBuffer = null;

        _lights.Clear();
    }
}