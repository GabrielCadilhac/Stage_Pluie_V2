using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UniformPrimitive : BasePrimitive
{
    public UniformPrimitive(BezierCurve bezierCurve, Vector3 position, float p_speed, float size)
        : base(bezierCurve, position, p_speed, size)
    {}

    public override Vector3 GetValue(float p_i, float p_j, float p_k)
    {
        Vector3 direction = new Vector3(-1f, 0f, 0f).normalized;
        Vector3 center = new Vector3(_position.x / Common.NB_CELLS.x, _position.y / Common.NB_CELLS.y, _position.z / Common.NB_CELLS.z);
        
        Vector3 OP = Common.Abs(center - new Vector3(p_j, p_i, p_k));
        if (OP.magnitude > _size)
            return Vector3.zero;

        Vector3 temp = Common.Multiply(Vector3.one - OP, direction.normalized);
        return temp;
    }
}