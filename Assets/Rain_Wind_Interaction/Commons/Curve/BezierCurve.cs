using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

public class BezierCurve : MonoBehaviour
{
    [FormerlySerializedAs("_points")] public Vector3[] points;

    public void Reset()
    {
        points = new Vector3[] { new(1f, 0f, 0f),
                                  new(2f, 0f, 0f),
                                  new(3f, 0f, 0f),
                                  new(4f, 0f, 0f)};
    }

    public void SetPoint(Vector3[] pNewPoints)
    {
        points = pNewPoints;
    }

    public void SetPoint(int pIndex, Vector3 pPoint)
    {
        points[pIndex] = pPoint;
    }
    
    public Vector3 GetPoint(float t, bool pTransformPoint = true)
    {
        if (pTransformPoint)
            return transform.TransformPoint(Bezier.GetPoint(points[0], points[1], points[2], points[3], t));
        return Bezier.GetPoint(points[0], points[1], points[2], points[3], t);
    }

    public Vector3 GetVelocity(float t)
    {
        return transform.TransformPoint(Bezier.GetFirstDerivatives(points[0], points[1], points[2], points[3], t)) -
            transform.position;
    }

    public Vector3 GetDirection(float t)
    {
        return GetVelocity(t).normalized;
    }

    public float GetLength()
    {
        return Mathf.Round(Vector3.Distance(points[0], points[1]) +
               Vector3.Distance(points[1], points[2]) +
               Vector3.Distance(points[2], points[3]));
    }
}
