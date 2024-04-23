using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static UnityEngine.EventSystems.EventTrigger;

public class EnergyCascade
{
    // Constantes
    float _coeffTransfert = 0.1f;
    float _coeffDissip = 0.1f;
    float _minEnergyNewPrim = 0.5f;
    float _maxPrimSize = 0.4f;
    float _minSizeMedium = 0.2f;
    float _minSizeSmall  = 0.1f;
    float _energyStrengthRatio = 1f;

    List<SuperPrimitive> _primitives;
    float[] _energiesTransfert;
    float _energyDissip;


    public EnergyCascade()
    {
        _primitives = new List<SuperPrimitive>();
        _energiesTransfert = new float[2];
        _energyDissip      = 0f;
    }

    // Calculer les énergies des primitives en fonction des taille
    private void CollectEnergies()
    {
        foreach (SuperPrimitive primitive in _primitives)
        {
            float energy;
            if (primitive.GetSize() > _minSizeMedium) // Grande
            {
                // Uniquement du transfert, pas de dissipation
                energy = primitive.GetSpeed() * _coeffTransfert;
                _energiesTransfert[1] = energy;
            } else if (primitive.GetSize() > _minSizeSmall) // Moyenne
            {
                // Transfert et dissipation
                energy = primitive.GetSpeed() * _coeffDissip;
                _energyDissip += energy;

                _energiesTransfert[1] = primitive.GetSpeed() * _coeffTransfert;
                energy += _energiesTransfert[1];
            }
            else // Petite
            {
                // Uniquement de la dissipation, pas de transfert
                energy = primitive.GetSpeed() * _coeffDissip;
                _energyDissip += energy;
            }

            primitive.SubEnergy(energy);
        }
    }

    // Distribution des énergies
    private void DiffuseEnergies()
    {
        foreach (SuperPrimitive primitive in _primitives)
        {
            // Les grandes ne gagnent pas d'énergie
            if (primitive.GetSize() > _minSizeSmall) // Moyenne
                primitive.AddEnergy(_energiesTransfert[1] / (float) _primitives.Count);
            else // Petite
                primitive.AddEnergy(_energiesTransfert[0] / (float) _primitives.Count);
        }

        if (_energyDissip >= _minEnergyNewPrim)
        {
            WindPrimitiveType[] newPrimitives = new WindPrimitiveType[3] { WindPrimitiveType.SINK, WindPrimitiveType.SOURCE, WindPrimitiveType.UNIFORM };
            int newPrimId = Random.Range(0, newPrimitives.Length);
            WindPrimitive[] primComp = new WindPrimitive[2] { new WindPrimitive(newPrimitives[newPrimId], 2f),
                                                              new WindPrimitive(WindPrimitiveType.VORTEX, 10f)
                                                            };
            float newSpeed = _energyDissip / _coeffDissip;
            _primitives.Add(new SuperPrimitive(_primitives[0].GetCurve(), primComp, newSpeed, _energyDissip * _energyStrengthRatio, _maxPrimSize));

            _energyDissip = 0f;
        }
    }

    public List<SuperPrimitive> CheckPrimitives()
    {
        List<SuperPrimitive> newPrimitives = new List<SuperPrimitive>();
        for (int i = 0; i < _primitives.Count; i++)
        {
            // Si la primitive est en fin de vie, la diviser en primitives plus petite
            if (_primitives[i].NeedDivide())
            {
                List<SuperPrimitive> newSubPrimitives = DividePrimitive(_primitives[i]);
                _primitives[i].DestroySphere();
                _primitives.RemoveAt(i);

                foreach (SuperPrimitive prim in newSubPrimitives)
                    newPrimitives.Add(prim);
            }
        }

        foreach (SuperPrimitive prim in newPrimitives)
            _primitives.Add(prim);

        return _primitives;
    }

    private List<SuperPrimitive> DividePrimitive(SuperPrimitive p_superPrim)
    {
        WindPrimitiveType[] newPrimitives = new WindPrimitiveType[3] { WindPrimitiveType.SINK, WindPrimitiveType.SOURCE, WindPrimitiveType.UNIFORM };
        int nbNewPrim = Random.Range(2, 6);

        // Ajouter un facteur de dissipation car il y a toujours un peu d'énergie perdu
        float k = p_superPrim.GetSpeed() * p_superPrim.GetSpeed() * Constants.KINETIC_COEFF * Constants.KINETIC_DUMPING;

        // Fonction discrète de valeurs cumulé pour distribuer l'énergie cinétique
        float[] kinEnergies = new float[nbNewPrim];
        float totalEnergy = 0f;
        for (int i = 0; i < nbNewPrim; i++)
        {
            kinEnergies[i] = Random.Range(0f, 1f);
            totalEnergy += kinEnergies[i];
        }

        List<SuperPrimitive> windPrimitives = new List<SuperPrimitive>();
        for (int i = 0; i < nbNewPrim; i++)
        {
            float newEnergy = k * kinEnergies[i] / totalEnergy;
            // Créer une nouvelle primitive si assez d'énergie
            // Sinon l'énergie est dissipé par la viscosité
            if (newEnergy > Constants.KINETIC_MIN)
            {
                int newPrimId = Random.Range(0, newPrimitives.Length);
                WindPrimitive[] primComp = new WindPrimitive[2] { new WindPrimitive(newPrimitives[newPrimId], 2f),
                                                                  new WindPrimitive(WindPrimitiveType.VORTEX, 10f)
                                                                };
                float newSize = kinEnergies[i] * p_superPrim.GetSize();
                float newSpeed = Mathf.Sqrt(newEnergy / Constants.KINETIC_COEFF);
                windPrimitives.Add(new SuperPrimitive(p_superPrim.GetCurve(), primComp, newSpeed, newEnergy * 8f, newSize, p_superPrim.GetLerp()));
            }
        }

        return windPrimitives;
    }


}
