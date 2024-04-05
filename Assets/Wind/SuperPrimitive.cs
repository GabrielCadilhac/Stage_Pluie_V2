using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum WindPrimitiveType
{
    UNIFORM,
    SOURCE,
    SINK,
    VORTEX
}

public struct Primitive
{
    public WindPrimitiveType type;
    public float parameter;

    public Primitive(WindPrimitiveType p_type, float p_param)
    {
        type = p_type;
        parameter = p_param;
    }
}

public class SuperPrimitive
{

    protected Vector3 _position;
    protected float _speed, _size, _currentLerp;

    private BezierCurve _bezierCurve;

    List<BasePrimitive> _basePrimitives;

    public SuperPrimitive(BezierCurve bezierCurve, Primitive[] p_WindPrimitiveType, Vector3 position, float p_speed, float size)
    {
        _bezierCurve = bezierCurve;
        _position = position;

        _speed = p_speed;
        _size = size;

        _currentLerp = 0f;

        _basePrimitives = new List<BasePrimitive>();
        foreach (Primitive prim in p_WindPrimitiveType)
        {
            switch (prim.type)
            {
                case WindPrimitiveType.SOURCE:
                    _basePrimitives.Add(new SourcePrimitive(_position, prim.parameter, _speed, _size));
                    break;
                case WindPrimitiveType.SINK:
                    _basePrimitives.Add(new SourcePrimitive(_position, -prim.parameter, _speed, _size));
                    break;
                case WindPrimitiveType.VORTEX:
                    _basePrimitives.Add(new VortexPrimitive(_position, prim.parameter, _speed, _size));
                    break;
                case WindPrimitiveType.UNIFORM:
                    _basePrimitives.Add(new UniformPrimitive(_position, prim.parameter, _speed, _size));
                    break;
            }
        }
    }

    public void Update(float p_deltaTime, Vector3 p_min, Vector3 p_nDivSize)
    {
        Vector3 point = _bezierCurve.GetPoint(_currentLerp, true);

        float j = (point.x - p_min.x) * p_nDivSize.x;
        float i = (point.y - p_min.y) * p_nDivSize.y;
        float k = (point.z - p_min.z) * p_nDivSize.z;

        _position = new Vector3(j, i, k);

        foreach (BasePrimitive prim in _basePrimitives)
            prim.SetPosition(_position);

        _currentLerp += _speed * p_deltaTime * Time.deltaTime;
    }

    public void CheckCollision()
    {
        _currentLerp = _currentLerp > 1f ? 0f : _currentLerp;
    }

    public void SetSpeed(float p_newSpeed)
    {
        _speed = p_newSpeed;
    }

    public Vector3 GetValue(float p_j, float p_i, float p_k)
    {
        Vector3 result = Vector3.zero;
        foreach (BasePrimitive prim in _basePrimitives)
            result += prim.GetValue(p_j, p_i, p_k);

        return result;
    }
}
