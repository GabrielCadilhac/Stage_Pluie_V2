using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnergyCascade
{
    // Constantes
    float _coeffTransfert   = 0.002f;
    float _coeffDissip      = 0.002f;
    float _minEnergyNewPrim = 0.4f;
    float _minSizeMedium    = 0.2f;
    float _minSizeSmall     = 0.1f;
    float _minSizeDestroy   = 0.01f;

    // Variables
    List<SuperPrimitive> _primitives;
    float[] _energiesTransfert;
    float _energyDissip;

    public EnergyCascade(BezierCurve p_curve)
    {
        _primitives = new List<SuperPrimitive>();
        _energiesTransfert = new float[2];
        _energyDissip      = 0f;

        float baseEnergy = 0.4f;
        int nbPrimitives = 4;
        for (int i = 0; i < nbPrimitives; i++)
        {
            WindPrimitive[] primComp = GenerateWindComp();

            float randLerp = Random.Range(0f, 1f);
            _primitives.Add(new SuperPrimitive(p_curve, primComp, baseEnergy, randLerp));
        }
    }

    // Calculer les énergies des primitives en fonction des taille
    private void CollectEnergies()
    {
        foreach (SuperPrimitive primitive in _primitives)
        {
            float energy = 0f;
            if (primitive.GetSize() > _minSizeMedium) // Grande
            {
                // Uniquement du transfert, pas de dissipation
                energy = primitive.GetEnergy() * _coeffTransfert;
                _energiesTransfert[1] = energy;
            } else if (primitive.GetSize() > _minSizeSmall) // Moyenne
            {
                // Transfert et dissipation
                energy = primitive.GetEnergy() * _coeffDissip;
                _energyDissip += energy;

                _energiesTransfert[0] = primitive.GetEnergy() * _coeffTransfert;
                energy += _energiesTransfert[0];
            }
            else // Petite
            {
                // Uniquement de la dissipation, pas de transfert
                energy = primitive.GetEnergy() * _coeffDissip;
                _energyDissip += energy;
            }

            primitive.SubEnergy(energy);
        }
    }

    // Distribution des énergies
    private void DiffuseEnergies()
    {
        //Debug.Log($"Dissipation {_energyDissip} | Transfert 1 {_energiesTransfert[1]} | Transfert 0 {_energiesTransfert[0]}");

        List<SuperPrimitive> primToRemove = new List<SuperPrimitive>();
        float energy0ToTransfert = _energiesTransfert[0] / (float)_primitives.Count;
        float energy1ToTransfert = _energiesTransfert[1] / (float)_primitives.Count;

        foreach (SuperPrimitive primitive in _primitives)
        {
            // Les grandes ne gagnent pas d'énergie
            if (_minSizeSmall < primitive.GetSize() && primitive.GetSize() < _minSizeMedium) // Moyenne
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
            _energyDissip += prim.GetEnergy() * _coeffDissip;
            _primitives.Remove(prim);
            prim.DestroySphere();
        }

        if (_energyDissip >= _minEnergyNewPrim)
        {
            WindPrimitive[] primComp = GenerateWindComp();
            _primitives.Add(new SuperPrimitive(_primitives[0].GetCurve(), primComp, _energyDissip * 2f));

            _energyDissip = 0f;
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
        CollectEnergies();
        DiffuseEnergies();
    }

    // Utilitaire
    private WindPrimitive[] GenerateWindComp()
    {
        WindPrimitiveType[] newPrimitives = new WindPrimitiveType[3] { WindPrimitiveType.Sink, WindPrimitiveType.Source, WindPrimitiveType.Uniform};
        int newPrimId = Random.Range(0, newPrimitives.Length);
        return new WindPrimitive[2] { new WindPrimitive(newPrimitives[newPrimId], 2f),
                                                              new WindPrimitive(WindPrimitiveType.Vortex, 10f)
                                                            };
    }

    public List<SuperPrimitive> GetPrimitives()
    {
        return _primitives;
    }
}
