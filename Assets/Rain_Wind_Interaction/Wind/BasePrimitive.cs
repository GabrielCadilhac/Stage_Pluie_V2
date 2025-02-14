using System.Collections;
using UnityEngine;
public abstract class BasePrimitive
{
    protected Vector3 Position;
    protected float Speed, Size, Param, Strength;

    public BasePrimitive(Vector3 position, float pParam, float pSpeed, float pSize, float pStrength)
    {
        Position = position;
        Speed = pSpeed;
        Size = pSize;
        Param = pParam;
        Strength = pStrength;
    }

    public void SetPosition(Vector3 pPosition)
    {
        Position = pPosition;
    }

    public void SetSpeed(float pNewSpeed)
    {
        Speed = pNewSpeed;
    }

    public float GetParam()
    {
        return Param;
    }

    public abstract Vector3 GetValue(float pJ, float pI, float pK);
}
