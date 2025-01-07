using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class WindGenerator
{
    private EnergyCascade _energyCascade;

    private Grid _windsGrid;
    private Vector3 _boxSize;
    private Vector3 _min, _max;

    //private Vector3[] _hodographPoints;

    // Test
    private ComputeShader _windShader;
    private ComputeBuffer _gpuWindBuffer, _gpuTurbBuffer;

    public WindGenerator(ComputeShader p_windShader,
                         Vector3 p_globalPosition,
                         Vector3 p_boxSize,
                         BezierCurve p_bezierCurve)
    {
        Random.InitState(0);

        _boxSize = p_boxSize;

        // Init Parameters
        _min = p_globalPosition - p_boxSize / 2f;
        _max = p_globalPosition + p_boxSize / 2f;
        _windsGrid = new Grid(Common.NB_CELLS, p_boxSize);

        _energyCascade = new EnergyCascade(p_bezierCurve);

        // TEST
        _windShader    = p_windShader;
        _gpuWindBuffer = new ComputeBuffer(Common.NB_CELLS.x * Common.NB_CELLS.y * Common.NB_CELLS.z, 3 * sizeof(float));
        _gpuTurbBuffer = new ComputeBuffer(200, 6 * sizeof(float) + sizeof(int));

        // Init GPU data
        List<GPUTurbulence> turbulence = new List<GPUTurbulence>();
        List<SuperPrimitive> prim = _energyCascade.GetPrimitives();
        foreach (SuperPrimitive p in prim)
            turbulence.Add(p.GetGpuTurbulence());
        _gpuTurbBuffer.SetData(turbulence);

        _windShader.SetBuffer(0, "Turbulence", _gpuTurbBuffer);
        _windShader.SetBuffer(0, "Wind", _gpuWindBuffer);
        _windShader.SetInt("NbTurbulence", _energyCascade.GetNbTurbulence());
        _windShader.SetFloat("LocalWindStrength", Constants.LOCAL_WIND_STRENGTH);
        _windShader.SetVector("GridSize", (Vector3) Common.NB_CELLS);
    }

    private (float, int) ComputeLerp(float p_t, int p_nbPoints)
    {
        float range = 1f / (float) p_nbPoints;
        float tempT = range;
        int id = 0;
        while (tempT < p_t)
        {
            tempT += range;
            id++;
        }

        return ((p_t - (float) id * range) / range, id);
    }

    public void UpdateCascade(Vector3 p_globalPos, float p_deltaTime)
    {
        _windsGrid.Reset();

        _min = p_globalPos - _boxSize / 2f;
        _max = p_globalPos + _boxSize / 2f;

        _energyCascade.Update(p_deltaTime, _min, _max);

    }

    public void Update()
    {
        List<SuperPrimitive> primitives = _energyCascade.GetPrimitives();

        // Ajouter des perturbations à la grille pour créer des rafales
        for (int j = 0; j < Common.NB_CELLS.x; j++)
            for (int i = 0; i < Common.NB_CELLS.y; i++)
                for (int k = 0; k < Common.NB_CELLS.z; k++)
                {
                    float x = (float)j / Common.NB_CELLS.x;
                    float y = (float)i / Common.NB_CELLS.y;
                    float z = (float)k / Common.NB_CELLS.z;

                    Vector3 direction = Vector3.zero;
                    foreach (SuperPrimitive prim in primitives)
                        direction += prim.GetValue(x, y, z);

                    _windsGrid.Add(i, j, k, direction * Constants.LOCAL_WIND_STRENGTH);
                }

    }

    public void UpdateGPU()
    {
        List<GPUTurbulence> turbulence = new List<GPUTurbulence>();
        List<SuperPrimitive> prim = _energyCascade.GetPrimitives();
        foreach (SuperPrimitive p in prim)
            turbulence.Add(p.GetGpuTurbulence());

        _gpuTurbBuffer.SetData(turbulence);

        _windShader.SetInt("NbTurbulence", _energyCascade.GetNbTurbulence());
        _windShader.Dispatch(0, 1, 1, 1);
    }

    public void CheckEnergy()
    {
        _energyCascade.CheckEnergy();
    }

    public void Reset(BezierCurve p_curve)
    {
        _energyCascade.Reset(p_curve);
    }

    public Grid GetGrid()
    {
        return _windsGrid;
    }

    public Vector3[] GetData()
    {
        Vector3[] localWinds = new Vector3[Common.NB_CELLS.x * Common.NB_CELLS.y * Common.NB_CELLS.z];
        _gpuWindBuffer.GetData(localWinds);
        return localWinds;
    }

    public ref ComputeBuffer GetGPUWind()
    {
        return ref _gpuWindBuffer;
    }

    public void DestroySphere()
    {
        _energyCascade.DestroySphere();
    }

    public void Disable()
    {
        _energyCascade.Disable();

        _gpuWindBuffer.Release();
        _gpuWindBuffer = null;

        _gpuTurbBuffer.Release();
        _gpuTurbBuffer = null;
    }
}
