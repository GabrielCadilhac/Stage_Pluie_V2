using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class EnergyCascade
{
    int _nbPrimitives = 5;

    // Variables
    List<SuperPrimitive> _primitives;
    float[] _energiesTransfert;
    float _energyDissip;

    int _turbID = 0; // Global ID pour toutes les turbulences
    float _lengthMax = 0f; // Longueur maximum de la courbe

    // Autres
    BezierCurve _curve;

    public EnergyCascade(BezierCurve p_curve, float p_lengthMax)
    {
        Random.InitState(0);

        _primitives = new List<SuperPrimitive>();
        _energiesTransfert = new float[2] { 0f, 0f };
        _energyDissip      = 0f;

        _curve = p_curve;
        _lengthMax = p_lengthMax;

        for (int i = 0; i < _nbPrimitives; i++)
        {
            WindPrimitive[] primComp = GenerateWindComp();

            float randLerp = Random.Range(0f, 1f);
            float energy = Constants.MEAN_ENERGY_PRIM + Random.Range(-Constants.MEAN_ENERGY_PRIM * Constants.STD_ENERGY_PRIM, Constants.MEAN_ENERGY_PRIM * Constants.STD_ENERGY_PRIM);
            _primitives.Add(new SuperPrimitive(p_curve, primComp, energy, _curve.GetLength() / p_lengthMax, randLerp, _turbID));

            _turbID++;
        }
    }

    // Calculer les énergies des primitives en fonction des taille
    private void CollectEnergies(float p_deltaTime)
    {
        foreach (SuperPrimitive primitive in _primitives)
        {
            float energyTransfert = 0f;
            float energyDissip = 0f;
            if (primitive.GetSize() > Constants.MIN_SIZE_TALL) // Grande
            {
                // Uniquement du transfert, pas de dissipation
                energyTransfert = primitive.GetTransferEnergy() * p_deltaTime;
                _energiesTransfert[1] += energyTransfert;
            } else if (primitive.GetSize() > Constants.MIN_SIZE_MEDIUM) // Moyenne
            {
                // Transfert et dissipation
                energyDissip = primitive.GetDissipEnergy() * p_deltaTime;
                _energyDissip += energyDissip;

                energyTransfert = primitive.GetTransferEnergy() * p_deltaTime;
                _energiesTransfert[0] += energyTransfert;
            } else // Petite
            {
                // Uniquement de la dissipation, pas de transfert
                energyDissip = primitive.GetDissipEnergy() * p_deltaTime;
                _energyDissip += energyDissip;
            }

            primitive.SubEnergy(energyTransfert + energyDissip);
        }
    }

    // Distribution des énergies
    private void DiffuseEnergies()
    {
        List<SuperPrimitive> primToRemove = new List<SuperPrimitive>();
        float transToDissip0 = _energiesTransfert[0] * Constants.COEFF_DISSIP;
        float transToDissip1 = _energiesTransfert[1] * Constants.COEFF_DISSIP;

        _energiesTransfert[0] -= transToDissip0;
        _energiesTransfert[1] -= transToDissip1;
        _energyDissip += transToDissip0 + transToDissip1;

        float energy0ToTransfert = _energiesTransfert[0] / (float)_primitives.Count;
        float energy1ToTransfert = _energiesTransfert[1] / (float)_primitives.Count;

        foreach (SuperPrimitive primitive in _primitives)
        {
            // Les grandes ne gagnent pas d'énergie
            if (Constants.MIN_SIZE_MEDIUM < primitive.GetSize() && primitive.GetSize() < Constants.MIN_SIZE_TALL) // Moyenne
            {
                _energiesTransfert[1] -= energy1ToTransfert;   
                primitive.AddEnergy(energy1ToTransfert);
            }
            else // Petite
            {
                if (primitive.GetSize() < Constants.MIN_SIZE_SMALL)
                    primToRemove.Add(primitive);
                else
                {
                    _energiesTransfert[0] -= energy0ToTransfert;
                    primitive.AddEnergy(energy0ToTransfert);
                }
            }
        }
        
        // Toute l'énergie qui n'est pas transférée devient dissipée
        _energyDissip += _energiesTransfert[0] + _energiesTransfert[1];
        _energiesTransfert = new float[2] { 0f, 0f };

        foreach (SuperPrimitive prim in primToRemove)
        {
            _energyDissip += prim.GetDissipEnergy();
            _primitives.Remove(prim);
            prim.DestroySphere();
        }

        if (_energyDissip >= Constants.MEAN_ENERGY_PRIM)
        {
            // Nouvelle primitives à partir de l'énergie dissipé
            float energy = Constants.MEAN_ENERGY_PRIM + Random.Range(-Constants.MEAN_ENERGY_PRIM * Constants.STD_ENERGY_PRIM, Constants.MEAN_ENERGY_PRIM * Constants.STD_ENERGY_PRIM); ;

            WindPrimitive[] primComp = GenerateWindComp();
            float randLerp = Random.Range(0f, 1f);
            _primitives.Add(new SuperPrimitive(_curve, primComp, energy, _curve.GetLength() / _lengthMax, randLerp, _turbID));
            _turbID++;
            
            _energyDissip -= energy;
        }
    }

    public void Update(float p_deltaTime, Vector3 p_min, Vector3 p_cellSize)
    {
        // Mettre à jours les primitives
        foreach (SuperPrimitive primitive in _primitives)
        {
            primitive.Update(p_deltaTime, p_min, p_cellSize);
            primitive.CheckCollision();
        }

        // Mettre à jour la cascade à énergie
        CollectEnergies(p_deltaTime);
        DiffuseEnergies();
    }

    // Utilitaire
    private WindPrimitive[] GenerateWindComp()
    {
        WindPrimitiveType[] newPrimitives = new WindPrimitiveType[3] { WindPrimitiveType.Uniform, WindPrimitiveType.Sink, WindPrimitiveType.Source };
        int newPrimId = Random.Range(0, newPrimitives.Length);
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

    public List<SuperPrimitive> GetPrimitives()
    {
        return _primitives;
    }

    public List<SuperTurbulence> GetTurbulences()
    {
        List<SuperTurbulence> turbs = new List<SuperTurbulence>();
        foreach (SuperPrimitive prim in _primitives)
        {
            SuperTurbulence superTurb;
            List<BasePrimitive> basePrimitives = prim.GetBasePrimitives();
            superTurb.turbulences = new Turbulence[basePrimitives.Count];
            for (int i = 0; i < basePrimitives.Count; i++)
            {
                BasePrimitive basePrim = basePrimitives[i];

                Turbulence turb;
                turb.position = prim.GetPosition();
                turb.size = prim.GetSize();
                turb.param = basePrim.GetParam();
                turb.strength = prim.GetStrength();
                turb.type = basePrim.GetPrimType();

                superTurb.turbulences[i] = turb;
            }

            turbs.Add(superTurb);
        }

        return turbs;
    }
}
