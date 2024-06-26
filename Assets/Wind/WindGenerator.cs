using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class WindGenerator
{
    private EnergyCascade _energyCascade;

    private ComputeShader _windShearShader;
    private ComputeBuffer _windShearBuffer, _hodoPointsBuffer;

    private Grid _windsGrid;
    private Bounds _box;
    private Vector3 _min, _max, _cellSize;

    private float   _deltaTime;
    private float _localWindStrength;

    public WindGenerator(Bounds p_box,
                         ComputeShader p_windShearShader,
                         BezierCurve p_bezierCurve,
                         float p_globalWindStrength = 1f,
                         float p_localWindStrength  = 1f,
                         float p_deltaTime = 1f)
    {
        Random.InitState(0);

        // Init Parameters
        _box = p_box;
        _min = _box.center - _box.size / 2f;
        _max = _box.center + _box.size / 2f;
        _cellSize = Common.Divide(Common.NB_CELLS, _box.size);

        _localWindStrength = p_localWindStrength;

        _deltaTime = p_deltaTime;

        _windsGrid = new Grid(Common.NB_CELLS, p_box);

        _windShearBuffer  = new ComputeBuffer(Common.NB_CELLS.x * Common.NB_CELLS.y * Common.NB_CELLS.z, 3 * sizeof(float));
        _hodoPointsBuffer = new ComputeBuffer(Constants.HODOGRAPH_POINTS, 3 * sizeof(float));
        
        _windShearShader = p_windShearShader;
        _windShearShader.SetBuffer(0, "_WindShear", _windShearBuffer);
        _windShearShader.SetBuffer(0, "_HodoPoints", _hodoPointsBuffer);

        Vector3 nbCells = new Vector3(Common.NB_CELLS.x, Common.NB_CELLS.y, Common.NB_CELLS.z);
        _windShearShader.SetVector("_Resolution", nbCells);
        _windShearShader.SetFloat("_WindShearStrength", p_globalWindStrength);

        float distMax = Vector3.Distance(_min, _max);
        _energyCascade = new EnergyCascade(p_bezierCurve, distMax);
    }

    public void Update()
    {
        _windShearShader.Dispatch(0, 2, 2, 2); // TODO calculer automatiquement

        _energyCascade.Update(_deltaTime, _min, _cellSize);
        
        List<SuperPrimitive> primitives = _energyCascade.GetPrimitives();

        // Ajouter des perturbations à la grille pour créer des rafales
        for (int j = 0; j < Common.NB_CELLS.x; j++)
            for (int i = 0; i < Common.NB_CELLS.y; i++)
                for (int k = 0; k < Common.NB_CELLS.z; k++)
                {
                    float x = (float) j / Common.NB_CELLS.x;
                    float y = (float) i / Common.NB_CELLS.y;
                    float z = (float) k / Common.NB_CELLS.z;

                    Vector3 direction = Vector3.zero;
                    foreach (SuperPrimitive prim in primitives)
                        direction += prim.GetValue(x, y, z);

                    _windsGrid.Set(i, j, k, direction * _localWindStrength);
                }
    }

    public void CheckEnergy()
    {
        _energyCascade.CheckEnergy();
    }

    public void SetLocalWindForce(float p_localWindForce)
    {
        _localWindStrength = p_localWindForce;
    }
    public void SetWindShearStrength(float p_windStrength)
    {
        _windShearShader.SetFloat("_WindShearStrength", p_windStrength);
    }

    public void SetDeltaTime(float p_deltaTime)
    {
        _deltaTime = p_deltaTime;
    }

    public void SetHodograph(Vector3[] p_hodographPoints)
    {
        //_hodographPoints = p_hodographPoints;
        _hodoPointsBuffer.SetData(p_hodographPoints);
        _windShearShader.SetInt("_NumHodoPoints", p_hodographPoints.Length);
    }

    public Vector3[] GetWinds()
    {
        return _windsGrid.GetGrid();
    }

    public Grid GetGrid()
    {
        return _windsGrid;
    }

    public ComputeBuffer GetShearBuffer()
    {
        return _windShearBuffer;
    }

    public void Disable()
    {
        _windShearBuffer.Release();
        _windShearBuffer = null;

        _hodoPointsBuffer.Release();
        _hodoPointsBuffer = null;
    }
}
