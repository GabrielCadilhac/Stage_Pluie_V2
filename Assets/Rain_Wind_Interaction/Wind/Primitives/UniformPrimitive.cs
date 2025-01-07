using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UniformPrimitive : BasePrimitive
{
    public UniformPrimitive(Vector3 position, float p_param, float p_speed, float p_size, float p_strength = 1f)
        : base(position, p_param, p_speed, p_size, p_strength)
    {}

    public override Vector3 GetValue(float p_i, float p_j, float p_k)
    {
        Vector3 direction = new Vector3(-_param, 0f, 0f).normalized;
        Vector3 center = _position;
        //Vector3 center    = new Vector3(_position.x / Common.NB_CELLS.x, _position.y / Common.NB_CELLS.y, _position.z / Common.NB_CELLS.z);
        
        Vector3 OP = Common.Abs(center - new Vector3(p_j, p_i, p_k));
        if (OP.magnitude > _size)
            return Vector3.zero;

        Vector3 temp = Common.Multiply(Vector3.one - OP, direction);
        return temp.normalized * _strength;
    }
}
