using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class SplashRenderer
{
    // Transform
    private Transform _transform;
    private Bounds _bounds;

    // Buffers
    private GraphicsBuffer _posBuffer;
    private ComputeBuffer _timeBuffer, _lightsBuffer;
    
    // Render
    private Material _material;
    private Mesh _mesh;

    // Lights
    private List<StrLight> _lights;
    private Transform _lightTransform;

    public SplashRenderer(Material p_material, Transform p_transform, Bounds p_bounds, int p_nbMaxParticles = 1000)
    {
        _transform = p_transform;
        _bounds = p_bounds;

        Vector3[] positions = new Vector3[p_nbMaxParticles];
        float[] times = new float[p_nbMaxParticles];
        int[] indices = new int[p_nbMaxParticles];
        for (int i = 0; i < p_nbMaxParticles; i++)
        {
            positions[i] = Vector3.zero;
            indices[i]   = i;
            times[i]     = 0f;
        }

        _timeBuffer = new ComputeBuffer(p_nbMaxParticles, sizeof(float));
        _timeBuffer.SetData(times);

        _material = p_material;
        _material.enableInstancing = true;
        _material.SetBuffer("TimeBuffer", _timeBuffer);

        _mesh = new Mesh() { name = "SplashMesh" };

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

        _lights = new List<StrLight>();
        _lightTransform = GameObject.Find("Lights").transform;
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
        _material.SetInteger("_NbLights", _lights.Count);

        _lightsBuffer.SetData(_lights);
        _material.SetBuffer("Lights", _lightsBuffer);
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

    public void Draw()
    {
        RenderParams rp = new RenderParams(_material);
        rp.worldBounds = new Bounds(_transform.position, _bounds.center); ;
        rp.matProps = new MaterialPropertyBlock();
        rp.matProps.SetMatrix("_ObjectToWorld", Matrix4x4.Translate(Vector3.zero));
        rp.matProps.SetFloat("_NumInstances", 1f);
        Graphics.RenderMeshPrimitives(rp, _mesh, 0, 1);
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

        _lightsBuffer.Release();
        _lightsBuffer = null;
    }
}
