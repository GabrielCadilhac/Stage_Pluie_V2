using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class BezierCurve : MonoBehaviour
{
    public Vector3[] _points;

    public void Reset()
    {
        _points = new Vector3[] { new(1f, 0f, 0f),
                                  new(2f, 0f, 0f),
                                  new(3f, 0f, 0f),
                                  new(4f, 0f, 0f)};
    }
    
    public Vector3 GetPoint(float t, bool p_transformPoint = true)
    {
        if (p_transformPoint)
            return transform.TransformPoint(Bezier.GetPoint(_points[0], _points[1], _points[2], _points[3], t));
        return Bezier.GetPoint(_points[0], _points[1], _points[2], _points[3], t);
    }

    public Vector3 GetVelocity(float t)
    {
        return transform.TransformPoint(Bezier.GetFirstDerivatives(_points[0], _points[1], _points[2], _points[3], t)) -
            transform.position;
    }

    public Vector3 GetDirection(float t)
    {
        return GetVelocity(t).normalized;
    }
}