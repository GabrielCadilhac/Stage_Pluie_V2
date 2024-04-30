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
    private float _currentLerp;

    protected Vector3 _position;
    protected Vector2 _randomOffset;

    private List<BasePrimitive> _basePrimitives;
    private BezierCurve _bezierCurve;
    private GameObject _sphere;

    private float _offsetRange = 8f;
    private float _sphereSize = 20f;

    protected float _speed, _strength, _size;

    int _id;

    public SuperPrimitive(BezierCurve p_bezierCurve, WindPrimitive[] p_windComp, float p_energy, float p_lerp = 0f, int p_id = 0)
    {
        _bezierCurve = p_bezierCurve;
        _position = Vector3.zero;

        _size     = p_energy;
        _strength = p_energy * Constants.ENERGY_STRENGTH * _size;
        _speed    = p_energy * Constants.ENERGY_SPEED;

        _id = p_id;

        _randomOffset = new Vector2(Random.Range(-_offsetRange, _offsetRange), Random.Range(-_offsetRange, _offsetRange));

        _currentLerp = p_lerp;
        
        // Composition de la super primitive
        _basePrimitives = new List<BasePrimitive>();
        Color primColor = Color.black;
        foreach (WindPrimitive prim in p_windComp)
        {
            switch (prim.type)
            {
                case WindPrimitiveType.Source:
                    primColor = Color.yellow;
                    _basePrimitives.Add(new SourcePrimitive(_position, prim.parameter, _speed, _size, Constants.SOURCE_STRENGTH));
                    primColor = Color.red;
                    break;
                case WindPrimitiveType.Sink:
                    primColor = Color.green;
                    _basePrimitives.Add(new SourcePrimitive(_position, -prim.parameter, _speed, _size, Constants.SINK_STRENGTH));
                    primColor = Color.green;
                    break;
                case WindPrimitiveType.Vortex:
                    primColor = Color.blue;
                    _basePrimitives.Add(new VortexPrimitive(_position, prim.parameter, _speed, _size, Constants.VORTEX_STRENGTH));
                    primColor = Color.blue;
                    break;
                case WindPrimitiveType.Uniform:
                    primColor = Color.red;
                    _basePrimitives.Add(new UniformPrimitive(_position, prim.parameter, _speed, _size, Constants.UNIFORM_STRENGTH));
                    primColor = Color.yellow;
                    break;
                default:
                    primColor = Color.black;
                    break;
            }
        }
        primColor.a = 0.5f;

        _sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        _sphere.name = $"Debug Sphere {_id}";
        _sphere.transform.localScale = Vector3.one * _size * _sphereSize;

        Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.color = primColor;
        
        _sphere.GetComponent<Renderer>().material = mat;
        _sphere.SetActive(true);
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

        // Mise a jour des positions des primitives
        foreach (BasePrimitive prim in _basePrimitives)
            prim.SetPosition(_position);

        // Mise a jour de la sph�re
        _sphere.transform.position = point;
        _sphere.transform.localScale = Vector3.one * _size * _sphereSize;
        _currentLerp += _speed * p_deltaTime;
    }

    public void AddEnergy(float p_energy)
    {
        _strength += p_energy * Constants.ENERGY_STRENGTH * _size;
        _size     += p_energy;
        _speed    += p_energy * Constants.ENERGY_SPEED;
    }

    public void SubEnergy(float p_energy)
    {
        _strength -= p_energy * Constants.ENERGY_STRENGTH * _size;
        _size     -= p_energy;
        _speed    -= p_energy * Constants.ENERGY_SPEED;
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

    public float GetDissipEnergy()
    {
        return (_speed / Constants.ENERGY_SPEED) * Constants.COEFF_DISSIP / _size;
    }

    public float GetTransferEnergy()
    {
        return (_speed / Constants.ENERGY_SPEED) * _size * Constants.COEFF_TRANSFERT;
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
        Debug.Log($"Turbulence supp");
        _sphere.SetActive(true);
        _sphere.AddComponent<DestroyObject>();
    }
}
