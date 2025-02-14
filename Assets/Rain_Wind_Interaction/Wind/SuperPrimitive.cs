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
    public WindPrimitiveType Type;
    public float Parameter;

    public WindPrimitive(WindPrimitiveType pType, float pParam)
    {
        Type = pType;
        Parameter = pParam;
    }
}

public class SuperPrimitive
{
    private float _currentLerp;

    protected Vector3 Position;
    protected Vector2 RandomOffset;

    private List<BasePrimitive> _basePrimitives;
    private BezierCurve _bezierCurve;
    private GameObject _sphere;

    private float _offsetRange = 14f;

    protected float Speed, Strength, Size;

    int _id;

    public SuperPrimitive(BezierCurve pBezierCurve, WindPrimitive[] pWindComp, float pEnergy, float pLerp = 0f, int pID = 0)
    {
        _bezierCurve = pBezierCurve;
        Position = Vector3.zero;

        Size     = pEnergy;
        Speed    = pEnergy * Constants.EnergySpeed;

        // La force dépend de l'énergie, de la taille et du cisaillement, plus un coefficient pour contrôler la force
        Strength = pEnergy * Size * Constants.EnergyStrength * 0.01f;

        _id = pID;

        RandomOffset = new Vector2(Random.Range(-_offsetRange, _offsetRange), Random.Range(-_offsetRange, _offsetRange));

        _currentLerp = pLerp;
        
        // Composition de la super primitive
        _basePrimitives = new List<BasePrimitive>();
        Color primColor = Color.black;
        foreach (WindPrimitive prim in pWindComp)
        {
            switch (prim.Type)
            {
                case WindPrimitiveType.Source:
                    primColor = Color.yellow;
                    _basePrimitives.Add(new SourcePrimitive(Position, prim.Parameter, Speed, Size, Constants.SourceStrength));
                    primColor = Color.red;
                    break;
                case WindPrimitiveType.Sink:
                    primColor = Color.green;
                    _basePrimitives.Add(new SourcePrimitive(Position, -prim.Parameter, Speed, Size, Constants.SinkStrength));
                    primColor = Color.green;
                    break;
                case WindPrimitiveType.Vortex:
                    primColor = Color.blue;
                    _basePrimitives.Add(new VortexPrimitive(Position, prim.Parameter, Speed, Size, Constants.VortexStrength));
                    primColor = Color.blue;
                    break;
                case WindPrimitiveType.Uniform:
                    primColor = Color.red;
                    _basePrimitives.Add(new UniformPrimitive(Position, prim.Parameter, Speed, Size, Constants.UniformStrength));
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
        _sphere.transform.localScale = Vector3.one * Size * Constants.SphereSize;

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
        _sphere.SetActive(Constants.RenderSphere);
    }
    
    public void Update(float pDeltaTime, Vector3 pMin, Vector3 pMax)
    {
        Vector3 point = _bezierCurve.GetPoint(_currentLerp, true);
        point += new Vector3(RandomOffset.x, RandomOffset.y, 0f);

        // Offset by p_min and normalize to [0,1]
        Position = Common.Divide((point - pMin), (pMax - pMin));

        // Mise a jour des positions des primitives
        foreach (BasePrimitive prim in _basePrimitives)
            prim.SetPosition(Position);

        // Mise a jour de la sphere
        _sphere.transform.position = point;
        _sphere.transform.localScale = Vector3.one * Size * Constants.SphereSize;
        _sphere.SetActive(Constants.RenderSphere);
        _currentLerp += Speed * pDeltaTime;
    }

    public void AddEnergy(float pEnergy)
    {
        Strength += pEnergy * Constants.EnergyStrength * Size;
        Size     += pEnergy;
        Speed    += pEnergy * Constants.EnergySpeed;
    }

    public void SubEnergy(float pEnergy)
    {
        Strength -= pEnergy * Constants.EnergyStrength * Size;
        Size     -= pEnergy;
        Speed    -= pEnergy * Constants.EnergySpeed;
    }

    public void CheckCollision()
    {
        if ( _currentLerp > 1f )
        {
            _currentLerp = 0f;
            RandomOffset = new Vector2(Random.Range(-_offsetRange, _offsetRange), Random.Range(_offsetRange, -_offsetRange));
        }
    }

    public Vector3 GetValue(float pJ, float pI, float pK)
    {
        Vector3 result = Vector3.zero;
        foreach (BasePrimitive prim in _basePrimitives)
            result += prim.GetValue(pJ, pI, pK);
        return result * Strength;
    }

    public float GetDissipEnergy()
    {
        return (Speed / Constants.EnergySpeed) * Constants.CoeffDissip / Size;
    }

    public float GetTransferEnergy()
    {
        return (Speed / Constants.EnergySpeed) * Size * Constants.CoeffTransfert;
    }

    public GPUTurbulence GetGpuTurbulence()
    {
        GPUTurbulence t = new GPUTurbulence();
        t.Pos = Position;
        t.Size = Size;
        t.Param = _basePrimitives[0].GetParam();
        t.Strength = Strength;

        if (_basePrimitives[0] is UniformPrimitive)
            t.Type = 0;
        else if (_basePrimitives[0] is VortexPrimitive)
            t.Type = 1;
        else
            t.Type = 2;

        return t;
    }

    public float GetSpeed()
    {
        return Speed;
    }

    public float GetSize()
    {
        return Size;
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
