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
    private float _dissipativeCoeff;
    private float _strengthFactor, _offsetRange, _currentLerp;

    protected Vector3 _position;
    protected Vector2 _randomOffset;
    protected float _speed, _force, _size, _lifeTime;

    private BezierCurve _bezierCurve;

    private List<BasePrimitive> _basePrimitives;

    private GameObject _sphere;

    public SuperPrimitive(BezierCurve p_bezierCurve, WindPrimitive[] p_WindPrimitiveType, float p_speed, float p_strenght, float p_size, float p_lerp = 0f)
    {
        _bezierCurve = p_bezierCurve;
        _size = p_size;
        _position = Vector3.zero;

        _speed = p_speed;

        float k = (_speed * _speed) * Constants.KINETIC_COEFF;
        _dissipativeCoeff = (k * k * k) / _size;
        _lifeTime = k / _dissipativeCoeff;

        _strengthFactor = p_strenght;
        _force = k;

        _offsetRange = 8f;
        _randomOffset = new Vector2(Random.Range(-_offsetRange, _offsetRange), Random.Range(-_offsetRange, _offsetRange));

        _currentLerp = p_lerp;

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

        //Debug.Log($"kineticEnergy {k} | lifeTime {_lifeTime} | dissipativeCoeff {_dissipativeCoeff}");

        _sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        _sphere.name = "Debug Sphere";
        _sphere.transform.localScale = Vector3.one * _size * 10f;
        _sphere.SetActive(false);
    }
    
    // Retourne vraie s'il faut diviser la primitive
    public void Update(float p_deltaTime, Vector3 p_min, Vector3 p_nDivSize)
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
    }

    public void AddEnergy(float p_energy)
    {
        float energySize = 0.1f;
        float energyStrength = 0.1f;
        float energySpeed = 0.1f;

        _size  += p_energy * energySize;
        _force += p_energy * energyStrength;
        _speed += p_energy * energySpeed;
    }

    public void SubEnergy(float p_energy)
    {
        float energySize = 0.1f;
        float energyStrength = 0.1f;
        float energySpeed = 0.1f;

        _size  -= p_energy * energySize;
        _force -= p_energy * energyStrength;
        _speed -= p_energy * energySpeed;
    }

    public void CheckCollision()
    {
        if ( _currentLerp > 1f )
        {
            _currentLerp = 0f;
            _randomOffset = new Vector2(Random.Range(-_offsetRange, _offsetRange), Random.Range(_offsetRange, -_offsetRange));
        }
    }

    public void SetSpeed(float p_newSpeed)
    {
        _speed = p_newSpeed;
    }

    public void SetForce(float p_newForce)
    {
        _strengthFactor = p_newForce;
    }

    public bool NeedDivide()
    {
        return _lifeTime <= 0f;
    }

    public Vector3 GetValue(float p_j, float p_i, float p_k)
    {
        Vector3 result = Vector3.zero;
        foreach (BasePrimitive prim in _basePrimitives)
            result += prim.GetValue(p_j, p_i, p_k);

        return result * _force * _strengthFactor;
    }

    public float GetSpeed()
    {
        return _speed;
    }

    public float GetSize()
    {
        return _size;
    }

    public float GetLerp()
    {
        return _currentLerp;
    }

    public BezierCurve GetCurve()
    {
        return _bezierCurve;
    }

    public void DestroySphere()
    {
        _sphere.SetActive(true);
        _sphere.AddComponent<DestroyObject>();
    }
}
