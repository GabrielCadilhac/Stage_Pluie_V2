using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SourcePrimitive : BasePrimitive
{
    public SourcePrimitive(Vector3 position, float p_param, float p_speed, float p_size, float p_strength = 1f)
        : base(position, p_param, p_speed, p_size, p_strength)
    {}

    public override Vector3 GetValue(float p_i, float p_j, float p_k)
    {
        // Vortex parameters
        //Vector3 center = _position;
        //Vector3 center = new Vector3(_position.x / Common.NB_CELLS.x, _position.y / Common.NB_CELLS.y, _position.z / Common.NB_CELLS.z);

        Vector3 OP = new Vector3(p_j, p_i, p_k) - _position;
        if (OP.magnitude > _size)
            return Vector3.zero;

        Vector3 cylCoord = Common.Cart2Cyl(OP);
        cylCoord.x = _param / (2f * Mathf.PI * _size);

        float sign = Mathf.Sign(_param);
        Vector3 cartCoord = new Vector3(cylCoord.x * Mathf.Cos(cylCoord.y), cylCoord.x * Mathf.Sin(cylCoord.y), sign * cylCoord.z);

        return cartCoord.normalized * _strength;
    }
}
