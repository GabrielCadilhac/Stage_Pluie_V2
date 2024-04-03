using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WindGenerator
{
    private BasePrimitive[] _primitives;

    private Grid _windsGrid;
    private Bounds _box;

    private List<Vector3Int> _curvePos;

    private Vector3 _globalWind;
    private float   _deltaTime;
    private float   _localWindForce;

    public WindGenerator(Bounds p_box,
                         BezierCurve p_bezierCurve,
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
        _localWindForce = p_localWindForce;

        _curvePos = new List<Vector3Int>();

        _windsGrid = new Grid(Common.NB_CELLS, p_box, Vector3.zero);
        
        // Init Prmitives
        _primitives = new BasePrimitive[p_nbPrimitives];
        for (int i = 0; i < p_nbPrimitives; i++)
        {
            Vector3 randPos = new Vector3(Random.Range(0f, Common.NB_CELLS.x), Random.Range(0f, Common.NB_CELLS.y), 0f);
            float randSpeed = Random.Range(0f, p_primitiveSpeed);
            _primitives[i] = new UniformPrimitive(p_bezierCurve, randPos, randSpeed, 0.4f);
        }
    }

    public void Update()
    {
        Vector3 min = _box.center - _box.size / 2.0f;
        Vector3 temp = Common.Divide(Common.NB_CELLS, _box.size);

        // Reset Grid
        foreach (Vector3Int idx in _curvePos)
            _windsGrid.Set(idx.x, idx.y, idx.z, _globalWind * _globalWind.magnitude);

        _curvePos.Clear();

        //Update Primitives
        for (int i = 0; i < _primitives.Length; i++)
        {
            _primitives[i].Update(_deltaTime, min, temp);
            _primitives[i].CheckCollision();
        }

        // Update Wind Grid
        for (int i = 0; i < Common.NB_CELLS.x; i++)
            for (int j = 0; j < Common.NB_CELLS.y; j++)
                for (int k = 0; k < Common.NB_CELLS.z; k++)
                {
                    float x = (float)i / Common.NB_CELLS.x;
                    float y = (float)j / Common.NB_CELLS.y;
                    float z = (float)k / Common.NB_CELLS.z;

                    Vector3 direction = Vector3.zero;
                    foreach (BasePrimitive prim in _primitives)
                        direction += prim.GetValue(x, y, z);

                    if (direction.magnitude != 0f)
                        _windsGrid.Set(i, j, k, direction * direction.magnitude * _localWindForce);
                }
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
