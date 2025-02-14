using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VortexPrimitive : BasePrimitive
{
    public VortexPrimitive(Vector3 position, float pParam, float pSpeed, float pSize, float pStrength = 1f)
        : base(position, pParam, pSpeed, pSize, pStrength)
    { }

    public override Vector3 GetValue(float pI, float pJ, float pK)
    {
        // Vortex parameters
        //Vector3 center = new Vector3(_position.x / Common.NB_CELLS.x, _position.y / Common.NB_CELLS.y, _position.z / Common.NB_CELLS.z);
        //Vector3 center = new Vector3(_position.x / 67.93f, _position.y / 35f, _position.z / 64.5f);
        Vector3 center = Position;

        Vector3 op = new Vector3(pJ, pI, pK) - center;
        if (op.magnitude > Size) return Vector3.zero;

        Vector3 cylCoord = Common.Cart2Cyl(op);
        cylCoord.y += Param / (2f * Mathf.PI * Size);

        Vector3 cartCoord = new Vector3(cylCoord.x * Mathf.Cos(cylCoord.y), cylCoord.x * Mathf.Sin(cylCoord.y), -cylCoord.z);

        return cartCoord.normalized * Strength;
    }
}
