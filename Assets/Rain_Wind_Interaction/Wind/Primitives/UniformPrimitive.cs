using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UniformPrimitive : BasePrimitive
{
    public UniformPrimitive(Vector3 position, float pParam, float pSpeed, float pSize, float pStrength = 1f)
        : base(position, pParam, pSpeed, pSize, pStrength)
    {}

    public override Vector3 GetValue(float pI, float pJ, float pK)
    {
        Vector3 direction = new Vector3(-Param, 0f, 0f).normalized;
        Vector3 center = Position;
        //Vector3 center    = new Vector3(_position.x / Common.NB_CELLS.x, _position.y / Common.NB_CELLS.y, _position.z / Common.NB_CELLS.z);
        
        Vector3 op = Common.Abs(center - new Vector3(pJ, pI, pK));
        if (op.magnitude > Size)
            return Vector3.zero;

        Vector3 temp = Common.Multiply(Vector3.one - op, direction);
        return temp.normalized * Strength;
    }
}
