using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum WindPrimitiveType
{
    Uniform = 0,
    Source = 1,
    Sink = 2,
    Vortex = 3
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
    private readonly float _randStrenghtPerc = 0.15f;
    private readonly float _speedVarPerc = 0.1f;

    private Vector3 _position;
    private float _speed, _force;
    private readonly float _size;

    private float _currentLerp;
    private readonly float _baseSpeed;
    private float _baseForce;

    private BezierCurve _bezierCurve;

    List<BasePrimitive> _basePrimitives;

    public SuperPrimitive(BezierCurve bezierCurve, Primitive[] p_WindPrimitiveType, Vector3 position, float p_speed, float p_strenght, float p_size)
    {
        _bezierCurve = bezierCurve;
        _position = position;

        _baseSpeed = p_speed;
        _speed = _baseSpeed + Random.Range(-_baseSpeed * _speedVarPerc, _baseSpeed * _speedVarPerc);

        _baseForce = p_strenght;
        _force = _baseForce + Random.Range(-_baseForce * _randStrenghtPerc, _baseForce * _randStrenghtPerc);

        _size = p_size;

        _currentLerp = 0.5f;

        _basePrimitives = new List<BasePrimitive>();
        foreach (Primitive prim in p_WindPrimitiveType)
        {
            switch (prim.type)
            {
                case WindPrimitiveType.Source:
                    _basePrimitives.Add(new SourcePrimitive(_position, prim.parameter, _speed, _size));
                    break;
                case WindPrimitiveType.Sink:
                    _basePrimitives.Add(new SourcePrimitive(_position, -prim.parameter, _speed, _size));
                    break;
                case WindPrimitiveType.Vortex:
                    _basePrimitives.Add(new VortexPrimitive(_position, prim.parameter, _speed, _size));
                    break;
                case WindPrimitiveType.Uniform:
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

        _position = point;
        //_position = new Vector3(j, i, k);

        foreach (BasePrimitive prim in _basePrimitives)
            prim.SetPosition(_position);

        _currentLerp += _speed * p_deltaTime;
    }

    public void CheckCollision()
    {
        if (_currentLerp > 1f)
        {
            _currentLerp = 0f;
            _speed = _baseSpeed + Random.Range(-_baseSpeed * _speedVarPerc, _baseSpeed * _speedVarPerc);
            _force = _baseForce + Random.Range(-_baseForce * _randStrenghtPerc, _baseForce * _randStrenghtPerc);
        }
    }

    public void SetSpeed(float p_newSpeed)
    {
        _speed = p_newSpeed;
    }

    public void SetForce(float p_newForce)
    {
        _baseForce = p_newForce;
    }

    public Vector3 GetValue(float p_j, float p_i, float p_k)
    {
        Vector3 result = Vector3.zero;
        foreach (BasePrimitive prim in _basePrimitives)
            result += prim.GetValue(p_j, p_i, p_k);

        return result * _force;
    }

    public Vector3 GetPosition()
    {
        return _position;
    }

    public GPUPrimitive[] GetGpuPrimitive()
    {
        GPUPrimitive[] gpuPrim = new GPUPrimitive[_basePrimitives.Count];
        for (int i = 0; i < _basePrimitives.Count; i++)
        {
            BasePrimitive basePrim = _basePrimitives[i];
            GPUPrimitive newPrim = new GPUPrimitive();
            newPrim.position = _position;
            newPrim.param = basePrim.GetParam();
            
            switch (basePrim)
            {
                case UniformPrimitive:
                    newPrim.type = 0;
                    break;
                case SourcePrimitive:
                    newPrim.type = 1;
                    break;
                case VortexPrimitive:
                    newPrim.type = 2;
                    break;
            }

            gpuPrim[i] = newPrim;
        }

        return gpuPrim;
    }
}
