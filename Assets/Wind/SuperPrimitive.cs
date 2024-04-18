using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using TMPro;
using UnityEngine;

public enum WindPrimitiveType
{
    UNIFORM,
    SOURCE,
    SINK,
    VORTEX
}

public struct WindPrimitive
{
    public WindPrimitiveType type;
    public float parameter;

    public WindPrimitive(WindPrimitiveType p_type, float p_param)
    {
        type = p_type;
        parameter = p_param;
    }
}

public class SuperPrimitive
{
    private float _randStrenghtPerc = 0.15f;
    //private float _speedVarPerc     = 0.1f;
    private float _dissipativeCoeff = 0.1f;

    protected Vector3 _position;
    protected Vector2 _randomOffset;
    protected float _speed, _force, _size, _currentLerp, _lifeTime;
    protected float _baseSpeed, _baseForce, _offsetRange;

    private BezierCurve _bezierCurve;

    List<BasePrimitive> _basePrimitives;

    GameObject _sphere;

    public SuperPrimitive(BezierCurve p_bezierCurve, WindPrimitive[] p_WindPrimitiveType, float p_speed, float p_strenght, float p_size)
    {
        _bezierCurve = p_bezierCurve;
        _size = p_size;
        _position = Vector3.zero;

        _speed = p_speed;
        //_baseSpeed = p_speed;
        //_speed = _baseSpeed + Random.Range(-_baseSpeed * _speedVarPerc, _baseSpeed * _speedVarPerc);

        _baseForce = p_strenght;
        _force = _baseForce + Random.Range(-_baseForce * _randStrenghtPerc, _baseForce * _randStrenghtPerc);

        _offsetRange = 5f;
        _randomOffset = new Vector2(Random.Range(-_offsetRange, _offsetRange), Random.Range(-_offsetRange, _offsetRange));

        _currentLerp = 0f;

        _basePrimitives = new List<BasePrimitive>();
        foreach (WindPrimitive prim in p_WindPrimitiveType)
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

        float k = (_speed * _speed) * Constants.COEFF_KINETIC;
        _dissipativeCoeff = (k * k * k) / _size;
        _lifeTime = k / _dissipativeCoeff;

        Debug.Log($"kineticEnergy {k} | lifeTime {_lifeTime} | dissipativeCoeff {_dissipativeCoeff}");

        _sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        _sphere.name = "Debug Sphere";
        _sphere.transform.localScale = Vector3.one * _size * 10f;
    }
    
    // Retourne vraie s'il faut diviser la primitive
    public bool Update(float p_deltaTime, Vector3 p_min, Vector3 p_nDivSize)
    {
        Vector3 point = _bezierCurve.GetPoint(_currentLerp, true);
        point += new Vector3(_randomOffset.x, _randomOffset.y, 0f);

        float j = (point.x - p_min.x) * p_nDivSize.x;
        float i = (point.y - p_min.y) * p_nDivSize.y;
        float k = (point.z - p_min.z) * p_nDivSize.z;

        _position = new Vector3(j, i, k);

        foreach (BasePrimitive prim in _basePrimitives)
            prim.SetPosition(_position);

        _sphere.transform.position = point;
        _currentLerp += _speed * p_deltaTime;

        _lifeTime -= _dissipativeCoeff;

        return _lifeTime <= 0f;
    }

    public void CheckCollision()
    {
        if ( _currentLerp > 1f )
        {
            _currentLerp = 0f;
            //_speed = _baseSpeed + Random.Range(-_baseSpeed * _speedVarPerc, _baseSpeed * _speedVarPerc);
            _force = _baseForce + Random.Range(-_baseForce * _randStrenghtPerc, _baseForce * _randStrenghtPerc);
            _randomOffset = new Vector2(Random.Range(-_offsetRange, _offsetRange), Random.Range(_offsetRange, -_offsetRange));
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

    public float GetSpeed()
    {
        return _speed;
    }

    public float GetSize()
    {
        return _size;
    }

    public void DestroySphere()
    {
        _sphere.AddComponent<DestroyObject>();
    }
}
