using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SourcePrimitive : BasePrimitive
{
    public SourcePrimitive(BezierCurve bezierCurve, Vector3 position, float p_speed, float size)
        : base(bezierCurve, position, p_speed, size)
    { }

    public override Vector3 GetValue(float p_i, float p_j, float p_k)
    {
        // Vortex parameters
        float a = -2f;
        Vector3 center = new Vector3(_position.x / Common.NB_CELLS.x, _position.y / Common.NB_CELLS.y, _position.z / Common.NB_CELLS.z);

        Vector3 OP = new Vector3(p_j, p_i, p_k) - center;
        if (OP.magnitude > _size)
            return Vector3.zero;

        Vector3 cylCoord = Common.Cart2Cyl(OP);
        cylCoord.x = a / (2f * Mathf.PI * _size);

        return new Vector3(cylCoord.x * Mathf.Cos(cylCoord.y), cylCoord.x * Mathf.Sin(cylCoord.y), -cylCoord.z);
    }
}