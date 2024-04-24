using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

public class EnergyCascade
{
    // Constantes
    float _minEnergyNewPrim = 0.3f;    // Energie minimale pour créer une nouvelle primitive
    float _stdNewEnergy     = 0.5f;   // Pourcentage de différence entre les nouvelles énergies
    float _minSizeTall    = 0.2f;    // Taille minimale d'une grande primitive
    float _minSizeMedium     = 0.1f;    // Taille minimale d'une primitive moyenne
    float _minSizeDestroy   = 0.005f;   // Taille minimale d'une petite primitive (avant destruction)

    int _nbPrimitives = 6;

    // Variables
    List<SuperPrimitive> _primitives;
    float[] _energiesTransfert;
    float _energyDissip;

    public EnergyCascade(BezierCurve p_curve)
    {
        _primitives = new List<SuperPrimitive>();
        _energiesTransfert = new float[2];
        _energyDissip      = 0f;

        for (int i = 0; i < _nbPrimitives; i++)
        {
            WindPrimitive[] primComp = GenerateWindComp();

            float randLerp = Random.Range(0f, 1f);
            float energy   = _minEnergyNewPrim + Random.Range(-_minEnergyNewPrim * _stdNewEnergy, _minEnergyNewPrim * _stdNewEnergy);
            _primitives.Add(new SuperPrimitive(p_curve, primComp, energy, randLerp));
        }
    }

    // Calculer les énergies des primitives en fonction des taille
    private void CollectEnergies(float p_deltaTime)
    {
        foreach (SuperPrimitive primitive in _primitives)
        {
            float energy;
            if (primitive.GetSize() > _minSizeTall) // Grande
            {
                // Uniquement du transfert, pas de dissipation
                energy = primitive.GetTransferEnergy() * p_deltaTime;
                _energiesTransfert[1] = energy;
            } else if (primitive.GetSize() > _minSizeMedium) // Moyenne
            {
                // Transfert et dissipation
                energy = primitive.GetDissipEnergy() * p_deltaTime;
                _energyDissip += energy;

                _energiesTransfert[0] = primitive.GetTransferEnergy() * p_deltaTime;
                energy += _energiesTransfert[0];
            } else // Petite
            {
                // Uniquement de la dissipation, pas de transfert
                energy = primitive.GetDissipEnergy() * p_deltaTime;
                _energyDissip += energy;
            }

            primitive.SubEnergy(energy);
        }
    }

    // Distribution des énergies
    private void DiffuseEnergies()
    {
        List<SuperPrimitive> primToRemove = new List<SuperPrimitive>();
        float energy0ToTransfert = _energiesTransfert[0] / (float)_primitives.Count;
        float energy1ToTransfert = _energiesTransfert[1] / (float)_primitives.Count;

        foreach (SuperPrimitive primitive in _primitives)
        {
            // Les grandes ne gagnent pas d'énergie
            if (_minSizeMedium < primitive.GetSize() && primitive.GetSize() < _minSizeTall) // Moyenne
            {
                _energiesTransfert[1] -= energy1ToTransfert;   
                primitive.AddEnergy(energy1ToTransfert);
            }
            else // Petite
            {
                if (primitive.GetSize() < _minSizeDestroy)
                    primToRemove.Add(primitive);
                else
                {
                    _energiesTransfert[0] -= energy0ToTransfert;
                    primitive.AddEnergy(energy0ToTransfert);
                }
            }
        }
        
        foreach (SuperPrimitive prim in primToRemove)
        {
            _energyDissip += prim.GetDissipEnergy();
            _primitives.Remove(prim);
            prim.DestroySphere();
        }

        if (_energyDissip >= _minEnergyNewPrim)
        {
            // Protection pour éviter de trop grosses primitives
            float maxEnergyDissip = 0.4f;
            float newEnergy = maxEnergyDissip < _energyDissip ? maxEnergyDissip : _energyDissip;

            WindPrimitive[] primComp = GenerateWindComp();
            _primitives.Add(new SuperPrimitive(_primitives[0].GetCurve(), primComp, newEnergy));

            _energyDissip -= newEnergy;
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
        WindPrimitiveType[] newPrimitives = new WindPrimitiveType[3] { WindPrimitiveType.Sink, WindPrimitiveType.Source, WindPrimitiveType.Uniform};
        int newPrimId = 2;
        return new WindPrimitive[2] { new WindPrimitive(newPrimitives[newPrimId], 1f),
                                      new WindPrimitive(WindPrimitiveType.Vortex, 10f)
                                    };
    }

    public List<SuperPrimitive> GetPrimitives()
    {
        return _primitives;
    }
}
