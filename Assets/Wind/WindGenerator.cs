using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class WindGenerator
{
    private EnergyCascade _energyCascade;

    private Grid _windsGrid;
    private Bounds _box;
    private Vector3 _min, _cellSize;

    private float   _deltaTime;
    private float _localWindForce, _globalWindForce;

    private Vector3[] _hodographPoints;

    public WindGenerator(Bounds p_box,
                         BezierCurve p_bezierCurve,
                         float p_globalWindForce = 1f,
                         float p_localWindForce = 1f,
                         float p_deltaTime = 1f)
    {
        Random.InitState(0);

        // Init Parameters
        _box = p_box;
        _min = _box.center - _box.size / 2.0f;
        _cellSize = Common.Divide(Common.NB_CELLS, _box.size);

        _localWindForce = p_localWindForce;

        _globalWindForce = p_globalWindForce;
        _deltaTime = p_deltaTime;

        _windsGrid = new Grid(Common.NB_CELLS, p_box);

        _energyCascade = new EnergyCascade(p_bezierCurve);
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

    private void ComputeGlobalWind()
    {
        for (int j = 0; j < Common.NB_CELLS.x; j++)
            for (int i = 0; i < Common.NB_CELLS.y; i++)
                for (int k = 0; k < Common.NB_CELLS.z; k++)
                {
                    float t = (float) j / (float) (Common.NB_CELLS.x-1);
                    int hodoId;

                    (t, hodoId) = ComputeLerp(t, Constants.HODOGRAPH_POINTS - 1);

                    Vector3 newWind = t * _hodographPoints[hodoId + 1] + (1f - t) * _hodographPoints[hodoId];
                    newWind = Common.Multiply(newWind, new Vector3(1f, 0f, 1f));

                    _windsGrid.Set(i, j, k, newWind * _globalWindForce);
                }
    }

    public void Update()
    {
        ComputeGlobalWind();

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

                    _windsGrid.Add(i, j, k, direction * _localWindForce);
                }
    }

    public void SetLocalWindForce(float p_localWindForce)
    {
        _localWindForce = p_localWindForce;
    }
    public void SetGlobalWindForce(float p_windForce)
    {
        _globalWindForce = p_windForce;
    }

    public void SetDeltaTime(float p_deltaTime)
    {
        _deltaTime = p_deltaTime;
    }

    public void SetHodograph(Vector3[] p_hodographPoints)
    {
        _hodographPoints = p_hodographPoints;
    }

    public Vector3[] GetWinds()
    {
        return _windsGrid.GetGrid();
    }

    public Grid GetGrid()
    {
        return _windsGrid;
    }
}
