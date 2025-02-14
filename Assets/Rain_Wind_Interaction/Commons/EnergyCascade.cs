using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;

public class EnergyCascade
{
    // Variables
    List<SuperPrimitive> _primitives;
    List<int> _nbTurbulence;

    float[] _energiesTransfert;
    float _energyDissip;

    int _turbID = 0; // Global ID pour toutes les turbulences

    int _nbMaxTurb = 0;

    // Autres
    BezierCurve _curve;

    public EnergyCascade(BezierCurve pCurve)
    {
        Random.InitState(0);

        _primitives = new List<SuperPrimitive>();
        _nbTurbulence = new List<int>();

        _energiesTransfert = new float[2] { 0f, 0f };
        _energyDissip      = 0f;

        _curve = pCurve;

        float xi = 1f;
        float du = 1f;
        Constants.CoeffDissip = 1f / (Constants.GlobalWind.magnitude * xi) * Mathf.Pow(du / 2f, 1.5f);
        //Debug.Log($"Constants.COEFF_DISSIP {Constants.COEFF_DISSIP}");
        
        /*
        float k = 0.15f * (p_globalWind.x * p_globalWind.x + p_globalWind.y * p_globalWind.y + p_globalWind.z * p_globalWind.z);
        while (k > 0f)
        {
            WindPrimitive[] primComp = GenerateWindComp();

            float randLerp = Random.Range(0f, 1f);
            float diff     = Constants.MEAN_ENERGY_PRIM * Constants.STD_ENERGY_PRIM;
            float energy   = Constants.MEAN_ENERGY_PRIM + Random.Range(-diff, diff);
            _primitives.Add(new SuperPrimitive(p_curve, primComp, energy, randLerp, _turbID));

            k -= energy;
            _turbID++;
        }
        _nbMaxTurb = _primitives.Count;
        */

        /* Ancienne g�n�ration de primitives
        for (int i = 0; i < _nbPrimitives; i++)
        {
            WindPrimitive[] primComp = GenerateWindComp();

            float randLerp = Random.Range(0f, 1f);
            float energy = Constants.MEAN_ENERGY_PRIM + Random.Range(-Constants.MEAN_ENERGY_PRIM * Constants.STD_ENERGY_PRIM, Constants.MEAN_ENERGY_PRIM * Constants.STD_ENERGY_PRIM);
            _primitives.Add(new SuperPrimitive(p_curve, primComp, energy, randLerp, _turbID));

            _turbID++;
        }
        */
    }

    // Calculer les �nergies des primitives en fonction des taille
    private void CollectEnergies(float pDeltaTime)
    {
        foreach (SuperPrimitive primitive in _primitives)
        {
            float energyTransfert = 0f;
            float energyDissip = 0f;
            if (primitive.GetSize() > Constants.MinSizeTall) // Grande
            {
                // Uniquement du transfert, pas de dissipation
                energyTransfert = primitive.GetTransferEnergy() * pDeltaTime;
                _energiesTransfert[1] += energyTransfert;
            } else if (primitive.GetSize() > Constants.MinSizeMedium) // Moyenne
            {
                // Transfert et dissipation
                energyDissip = primitive.GetDissipEnergy() * pDeltaTime;
                _energyDissip += energyDissip;

                energyTransfert = primitive.GetTransferEnergy() * pDeltaTime;
                _energiesTransfert[0] += energyTransfert;
            } else // Petite
            {
                // Uniquement de la dissipation, pas de transfert
                energyDissip = primitive.GetDissipEnergy() * pDeltaTime;
                _energyDissip += energyDissip;
            }

            primitive.SubEnergy(energyTransfert + energyDissip);
        }
    }

    // Distribution des �nergies
    private void DiffuseEnergies()
    {
        List<SuperPrimitive> primToRemove = new List<SuperPrimitive>();
        float transToDissip0 = _energiesTransfert[0] * Constants.CoeffDissip;
        float transToDissip1 = _energiesTransfert[1] * Constants.CoeffDissip;

        _energiesTransfert[0] -= transToDissip0;
        _energiesTransfert[1] -= transToDissip1;
        _energyDissip += transToDissip0 + transToDissip1;

        float energy0ToTransfert = _energiesTransfert[0] / (float)_primitives.Count;
        float energy1ToTransfert = _energiesTransfert[1] / (float)_primitives.Count;

        foreach (SuperPrimitive primitive in _primitives)
        {
            // Les grandes ne gagnent pas d'�nergie
            if (Constants.MinSizeMedium < primitive.GetSize() && primitive.GetSize() < Constants.MinSizeTall) // Moyenne
            {
                _energiesTransfert[1] -= energy1ToTransfert;   
                primitive.AddEnergy(energy1ToTransfert);
            }
            else // Petite
            {
                if (primitive.GetSize() < Constants.MinSizeSmall)
                    primToRemove.Add(primitive);
                else
                {
                    _energiesTransfert[0] -= energy0ToTransfert;
                    primitive.AddEnergy(energy0ToTransfert);
                }
            }
        }
        
        // Toute l'�nergie qui n'est pas transf�r�e est dissip�e
        _energyDissip += _energiesTransfert[0] + _energiesTransfert[1];
        _energiesTransfert = new float[2] { 0f, 0f };

        foreach (SuperPrimitive prim in primToRemove)
        {
            _energyDissip += prim.GetDissipEnergy();
            _primitives.Remove(prim);
            prim.DestroySphere();
        }

        if (_energyDissip >= Constants.MeanEnergyPrim)
        {
            // Nouvelle primitives � partir de l'�nergie dissip�
            float energy = Constants.MeanEnergyPrim + Random.Range(-Constants.MeanEnergyPrim * Constants.StdEnergyPrim, Constants.MeanEnergyPrim * Constants.StdEnergyPrim); ;

            WindPrimitive[] primComp = GenerateWindComp();
            float randLerp = Random.Range(0f, 1f);
            _primitives.Add(new SuperPrimitive(_curve, primComp, energy, randLerp, _turbID));
            _turbID++;
            
            _energyDissip -= energy;
        }
    }

    public void Update(float pDeltaTime, Vector3 pMin, Vector3 pMax)
    {
        // Mettre � jours les primitives
        foreach (SuperPrimitive primitive in _primitives)
        {
            primitive.Update(pDeltaTime, pMin, pMax);
            primitive.CheckCollision();
        }

        // Mettre � jour la cascade � �nergie
        CollectEnergies(pDeltaTime);
        DiffuseEnergies();

        _nbTurbulence.Add(_primitives.Count);
        if (_nbMaxTurb < _primitives.Count)
            _nbMaxTurb = _primitives.Count;
    }

    // Utilitaire
    private WindPrimitive[] GenerateWindComp()
    {
        WindPrimitiveType[] newPrimitives = new WindPrimitiveType[3] { WindPrimitiveType.Uniform, WindPrimitiveType.Sink, WindPrimitiveType.Source };
        int newPrimId = Random.Range(0, newPrimitives.Length);
        //return new WindPrimitive[1] { new WindPrimitive(newPrimitives[newPrimId], 1f) };
        return new WindPrimitive[2] { new WindPrimitive(WindPrimitiveType.Vortex, 10f),
                                      new WindPrimitive(newPrimitives[newPrimId], 1f)
                                    };
    }

    public void CheckEnergy()
    {
        float total = 0f;
        foreach (SuperPrimitive primitive in _primitives)
            total += primitive.GetSize();

        Debug.Log($"Total energy dissip {total / (float) _primitives.Count}");
    }

    public void Reset(BezierCurve pCurve)
    {
        DestroySphere();
        _primitives.Clear();

        Vector3 wind = Constants.GlobalWind;
        float k = 0.1f * (wind.x * wind.x + wind.y * wind.y + wind.z * wind.z);
        while (k > 0f)
        {
            WindPrimitive[] primComp = GenerateWindComp();

            float randLerp = Random.Range(0f, 1f);
            float diff = Constants.MeanEnergyPrim * Constants.StdEnergyPrim;
            float energy = Constants.MeanEnergyPrim + Random.Range(-diff, diff);
            _primitives.Add(new SuperPrimitive(pCurve, primComp, energy, randLerp, _turbID));

            k -= energy;
            _turbID++;
        }
        _nbMaxTurb = _primitives.Count;
    }

    public List<SuperPrimitive> GetPrimitives()
    {
        return _primitives;
    }

    public int GetNbTurbulence()
    {
        return _primitives.Count;
    }

    public void DestroySphere()
    {
        foreach (SuperPrimitive prim in _primitives)
            prim.DestroySphere();
    }

    public void Disable()
    {
        DestroySphere();

        int total = 0;
        foreach (int nb in _nbTurbulence)
            total += nb;

        if (_nbTurbulence.Count > 0)
            Debug.Log($"Nombre moyen de turbulence {total / _nbTurbulence.Count}");
        Debug.Log($"Max turbulence number {_nbMaxTurb}");
    }
}
