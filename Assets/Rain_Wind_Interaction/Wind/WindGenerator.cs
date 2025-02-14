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

    public WindGenerator(ComputeShader pWindShader,
                         Vector3 pGlobalPosition,
                         Vector3 pBoxSize,
                         BezierCurve pBezierCurve)
    {
        Random.InitState(0);

        _boxSize = pBoxSize;

        // Init Parameters
        _min = pGlobalPosition - pBoxSize / 2f;
        _max = pGlobalPosition + pBoxSize / 2f;
        _windsGrid = new Grid(Common.NbCells, pBoxSize);

        _energyCascade = new EnergyCascade(pBezierCurve);

        // TEST
        _windShader    = pWindShader;
        _gpuWindBuffer = new ComputeBuffer(Common.NbCells.x * Common.NbCells.y * Common.NbCells.z, 3 * sizeof(float));
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
        _windShader.SetFloat("LocalWindStrength", Constants.LocalWindStrength);
        _windShader.SetVector("GridSize", (Vector3) Common.NbCells);
    }

    private (float, int) ComputeLerp(float pT, int pNbPoints)
    {
        float range = 1f / (float) pNbPoints;
        float tempT = range;
        int id = 0;
        while (tempT < pT)
        {
            tempT += range;
            id++;
        }

        return ((pT - (float) id * range) / range, id);
    }

    public void UpdateCascade(Vector3 pGlobalPos, float pDeltaTime)
    {
        _windsGrid.Reset();

        _min = pGlobalPos - _boxSize / 2f;
        _max = pGlobalPos + _boxSize / 2f;

        _energyCascade.Update(pDeltaTime, _min, _max);

    }

    public void Update()
    {
        List<SuperPrimitive> primitives = _energyCascade.GetPrimitives();

        // Ajouter des perturbations � la grille pour cr�er des rafales
        for (int j = 0; j < Common.NbCells.x; j++)
            for (int i = 0; i < Common.NbCells.y; i++)
                for (int k = 0; k < Common.NbCells.z; k++)
                {
                    float x = (float)j / Common.NbCells.x;
                    float y = (float)i / Common.NbCells.y;
                    float z = (float)k / Common.NbCells.z;

                    Vector3 direction = Vector3.zero;
                    foreach (SuperPrimitive prim in primitives)
                        direction += prim.GetValue(x, y, z);

                    _windsGrid.Add(i, j, k, direction * Constants.LocalWindStrength);
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

    public void Reset(BezierCurve pCurve)
    {
        _energyCascade.Reset(pCurve);
    }

    public Grid GetGrid()
    {
        return _windsGrid;
    }

    public Vector3[] GetData()
    {
        Vector3[] localWinds = new Vector3[Common.NbCells.x * Common.NbCells.y * Common.NbCells.z];
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
