using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Les types de primitives possibles
public enum WindPrimitiveType
{
    Uniform,
    Source,
    Sink,
    Vortex
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
    private float _offsetRange, _currentLerp;

    protected Vector3 _position;
    protected Vector2 _randomOffset;

    private List<BasePrimitive> _basePrimitives;
    private BezierCurve _bezierCurve;
    private GameObject _sphere;

    protected float _speed, _strength, _size;
    float _energySize     = 1f;
    float _energyStrength = 0.8f;
    float _energySpeed    = 0.25f;

    public SuperPrimitive(BezierCurve p_bezierCurve, WindPrimitive[] p_windComp, float p_energy, float p_lerp = 0f)
    {
        _bezierCurve = p_bezierCurve;
        _position = Vector3.zero;

        _size     = p_energy * _energySize;
        _strength = p_energy * _energyStrength;
        _speed    = p_energy * _energySpeed;

        Debug.Log($"Energy {p_energy} | size {_size} | force {_strength} | speed {_speed} ");

        _offsetRange = 8f;
        _randomOffset = new Vector2(Random.Range(-_offsetRange, _offsetRange), Random.Range(-_offsetRange, _offsetRange));

        _currentLerp = p_lerp;
        
        // Composition de la super primitive
        _basePrimitives = new List<BasePrimitive>();
        foreach (WindPrimitive prim in p_windComp)
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

        _sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        _sphere.name = "Debug Sphere";
        _sphere.transform.localScale = Vector3.one * _size * 10f;
        _sphere.SetActive(false);
    }
    
    // Retourne vraie s'il faut diviser la primitive
    public void Update(float p_deltaTime, Vector3 p_min, Vector3 p_cellSize)
    {
        Vector3 point = _bezierCurve.GetPoint(_currentLerp, true);
        point += new Vector3(_randomOffset.x, _randomOffset.y, 0f);

        float j = (point.x - p_min.x) * p_cellSize.x;
        float i = (point.y - p_min.y) * p_cellSize.y;
        float k = (point.z - p_min.z) * p_cellSize.z;

        _position = new Vector3(j, i, k);

        foreach (BasePrimitive prim in _basePrimitives)
            prim.SetPosition(_position);

        _sphere.transform.position = point;
        _currentLerp += _speed * p_deltaTime;
    }

    public void AddEnergy(float p_energy)
    {
        _size     += p_energy * _energySize;
        _strength += p_energy * _energyStrength;
        _speed    += p_energy * _energySpeed;
    }

    public void SubEnergy(float p_energy)
    {
        _size     -= p_energy * _energySize;
        _strength -= p_energy * _energyStrength;
        _speed    -= p_energy * _energySpeed;
    }

    public void CheckCollision()
    {
        if ( _currentLerp > 1f )
        {
            _currentLerp = 0f;
            _randomOffset = new Vector2(Random.Range(-_offsetRange, _offsetRange), Random.Range(_offsetRange, -_offsetRange));
        }
    }

    public Vector3 GetValue(float p_j, float p_i, float p_k)
    {
        Vector3 result = Vector3.zero;
        foreach (BasePrimitive prim in _basePrimitives)
            result += prim.GetValue(p_j, p_i, p_k);

        return result * _strength;
    }

    // Change how to compute the energy
    public float GetEnergy()
    {
        return _speed;
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
