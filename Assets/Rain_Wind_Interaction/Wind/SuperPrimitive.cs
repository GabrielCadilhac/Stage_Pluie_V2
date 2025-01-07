using System.Collections;
using System.Collections.Generic;
using TMPro;
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

    private float _offsetRange = 14f;

    protected float _speed, _strength, _size;

    int _id;

    public SuperPrimitive(BezierCurve p_bezierCurve, WindPrimitive[] p_windComp, float p_energy, float p_lerp = 0f, int p_id = 0)
    {
        _bezierCurve = p_bezierCurve;
        _position = Vector3.zero;

        _size     = p_energy;
        _speed    = p_energy * Constants.ENERGY_SPEED;

        // La force dépend de l'énergie, de la taille et du cisaillement, plus un coefficient pour contrôler la force
        _strength = p_energy * _size * Constants.ENERGY_STRENGTH * 0.01f;

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
        primColor.a = 0.35f;

        _sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        _sphere.name = $"Debug Sphere {_id}";
        _sphere.transform.localScale = Vector3.one * _size * Constants.SPHERE_SIZE;

        //Material mat = new Material(Shader.Find("HDRP/Lit"));
        //mat.SetFloat("_Surface", 1.0f);
        //mat.SetOverrideTag("RenderType", "Transparent");
        //mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        //mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        //mat.SetInt("_ZWrite", 0);
        //mat.DisableKeyword("_ALPHATEST_ON");
        //mat.EnableKeyword("_ALPHABLEND_ON");
        //mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        //mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        //mat.SetFloat("_Mode", 1.0f);
        //mat.color = primColor;

        //_sphere.GetComponent<Renderer>().material = mat;
        _sphere.SetActive(Constants.RENDER_SPHERE);
    }
    
    public void Update(float p_deltaTime, Vector3 p_min, Vector3 p_max)
    {
        Vector3 point = _bezierCurve.GetPoint(_currentLerp, true);
        point += new Vector3(_randomOffset.x, _randomOffset.y, 0f);

        // Offset by p_min and normalize to [0,1]
        _position = Common.Divide((point - p_min), (p_max - p_min));

        // Mise a jour des positions des primitives
        foreach (BasePrimitive prim in _basePrimitives)
            prim.SetPosition(_position);

        // Mise a jour de la sphere
        _sphere.transform.position = point;
        _sphere.transform.localScale = Vector3.one * _size * Constants.SPHERE_SIZE;
        _sphere.SetActive(Constants.RENDER_SPHERE);
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

    public GPUTurbulence GetGpuTurbulence()
    {
        GPUTurbulence t = new GPUTurbulence();
        t.pos = _position;
        t.size = _size;
        t.param = _basePrimitives[0].GetParam();
        t.strength = _strength;

        if (_basePrimitives[0] is UniformPrimitive)
            t.type = 0;
        else if (_basePrimitives[0] is VortexPrimitive)
            t.type = 1;
        else
            t.type = 2;

        return t;
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
        //Debug.Log($"Turbulence supp");
        if (_sphere == null)
            return;

        _sphere.SetActive(true);
        _sphere.AddComponent<DestroyObject>();
    }
}
