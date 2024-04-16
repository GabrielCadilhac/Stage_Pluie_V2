using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WindGenerator
{
    private SuperPrimitive[] _primitives;

    private Grid _windsGrid;
    private Bounds _box;

    private List<Vector3Int> _curvePos;

    private Vector3 _globalWind;
    private float   _deltaTime;

    public WindGenerator(Bounds p_box,
                         BezierCurve p_bezierCurve,
                         Transform p_transform,
                         Vector3? p_globalWind = null,
                         int p_nbPrimitives = 1,
                         float p_primitiveSpeed = 1f,
                         float p_localWindForce = 1f,
                         float p_deltaTime = 1f)
    {
        // Init Parameters
        _box = p_box;
        _globalWind = p_globalWind == null ? Vector3.zero : (Vector3) p_globalWind;
        _deltaTime = p_deltaTime;

        _curvePos = new List<Vector3Int>();

        _windsGrid = new Grid(Common.NB_CELLS, p_box);
        ComputeGlobalWind();
        InitBezierCurve(p_bezierCurve, p_box, _windsGrid, p_transform);

        // Init Prmitives
        _primitives = new SuperPrimitive[p_nbPrimitives];

        Vector3 randPos = new Vector3(Random.Range(0f, Common.NB_CELLS.x), Random.Range(0f, Common.NB_CELLS.y), 0f);

        // A super primitive composed with a vortex and a source
        Primitive[] primComp = new Primitive[2] { new Primitive(WindPrimitiveType.SINK, 1f),
                                                  new Primitive(WindPrimitiveType.VORTEX, 10f)
                                                };
        _primitives[0]  = new SuperPrimitive(p_bezierCurve, primComp, randPos, p_primitiveSpeed, p_localWindForce, 0.3f);
    }

    private void ComputeGlobalWind()
    {
        for (int j = 0; j < Common.NB_CELLS.x; j++)
            for (int i = 0; i < Common.NB_CELLS.y; i++)
                for (int k = 0; k < Common.NB_CELLS.z; k++)
                {
                    float t = ((float) j / (float) (Common.NB_CELLS.x-1));
                    Vector3 newWind = new Vector3(t * _globalWind.x, 0f, (1f - t) * _globalWind.z);
                    _windsGrid.Set(i, j, k, newWind * _globalWind.magnitude);
                }
    }

    private void InitBezierCurve(BezierCurve p_bezierCurve, Bounds p_bounds, Grid p_windGrid, Transform p_transform)
    {
        Vector3 min = p_bezierCurve.transform.InverseTransformPoint(p_bounds.min);
        Vector3 max = p_bezierCurve.transform.InverseTransformPoint(p_bounds.max);

        float t    = 0.25f;
        Vector3 c1 = (1f - t) * min + t * max;
        Vector3 c2 = t * min + (1f - t) * max;

        //Vector3    windC1   = p_windGrid.FloatTo01(c1 - new Vector3(0f, 0f, 15f)); // TODO mettre la position de la boite
        //Vector3Int tempWind = Vector3Int.FloorToInt(Common.Multiply(windC1, Common.NB_CELLS - Vector3.one));
        //Vector3 changeC1 = p_windGrid.Get(tempWind.x, tempWind.y, tempWind.z);

        Vector3 testC2 = c2 - p_transform.position;
        Debug.Log($"vraie c2 {c2} et faux {testC2}");
        Debug.Log($"vraie c1 {c1}");

        Vector3 coordC2 = p_windGrid.FloatTo01(c2 - new Vector3(0f, 0f, 15f)); // TODO mettre la position de la boite
        Vector3Int tempWind = Vector3Int.FloorToInt(Common.Multiply(coordC2, Common.NB_CELLS - Vector3.one));
        Vector3 windC2 = p_windGrid.Get(tempWind.x, tempWind.y, tempWind.z) * 20f;

        // Initialiser la courbe comme une ligne droite
        p_bezierCurve.SetPoint(0, min);
        p_bezierCurve.SetPoint(1, c1);
        p_bezierCurve.SetPoint(2, c2 + windC2);
        p_bezierCurve.SetPoint(3, max);
    }

    public void Update()
    {
        Vector3 min  = _box.center - _box.size / 2.0f;
        Vector3 temp = Common.Divide(Common.NB_CELLS, _box.size);

        // Reset Grid
        foreach (Vector3Int idx in _curvePos)
        {
            float t = ((float) idx.y / (float) (Common.NB_CELLS.x-1));
            Vector3 newWind = Common.Multiply(new Vector3(t, 0f, (1f - t)), _globalWind);
            _windsGrid.Set(idx.x, idx.y, idx.z, newWind * _globalWind.magnitude);
        }

        _curvePos.Clear();

        //Update Primitives
        for (int i = 0; i < _primitives.Length; i++)
        {
            _primitives[i].Update(_deltaTime, min, temp);
            _primitives[i].CheckCollision();
        }

        // Update Wind Grid
        for (int j = 0; j < Common.NB_CELLS.x; j++)
            for (int i = 0; i < Common.NB_CELLS.y; i++)
                for (int k = 0; k < Common.NB_CELLS.z; k++)
                {
                    float x = (float) j / Common.NB_CELLS.x;
                    float y = (float) i / Common.NB_CELLS.y;
                    float z = (float) k / Common.NB_CELLS.z;

                    Vector3 direction = Vector3.zero;
                    foreach (SuperPrimitive prim in _primitives)
                        direction += prim.GetValue(x, y, z);

                    if (direction.magnitude != 0f)
                    {
                        //_windsGrid.Add(i, j, k, direction * direction.magnitude);
                        //_curvePos.Add(new Vector3Int(i, j, k));
                    }
                }
    }

    public void SetGlobalWind(Vector3 p_globalWind)
    {
        _globalWind = p_globalWind;
        ComputeGlobalWind();
    }

    public void SetLocalWindForce(float p_localWindForce)
    {
        foreach (SuperPrimitive prim in _primitives)
            prim.SetForce(p_localWindForce);
    }

    public void SetDeltaTime(float p_deltaTime)
    {
        _deltaTime = p_deltaTime;
    }

    public void SetPrimitiveSpeed(float p_primitiveSpeed)
    {
        foreach (SuperPrimitive prim in _primitives)
            prim.SetSpeed(p_primitiveSpeed);
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
