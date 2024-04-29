using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnergyCascade
{
    int _nbPrimitives = 5;

    // Variables
    List<SuperPrimitive> _primitives;
    float[] _energiesTransfert;
    float _energyDissip;

    int _turbID = 0;

    // Autres
    BezierCurve _curve;

    public EnergyCascade(BezierCurve p_curve)
    {
        Random.InitState(0);

        _primitives = new List<SuperPrimitive>();
        _energiesTransfert = new float[2];
        _energyDissip      = 0f;

        _curve = p_curve;

        for (int i = 0; i < _nbPrimitives; i++)
        {
            WindPrimitive[] primComp = GenerateWindComp();

            float randLerp = Random.Range(0f, 1f);
            float energy = Constants.MEAN_ENERGY_PRIM + Random.Range(-Constants.MEAN_ENERGY_PRIM * Constants.STD_ENERGY_PRIM, Constants.MEAN_ENERGY_PRIM * Constants.STD_ENERGY_PRIM);
            _primitives.Add(new SuperPrimitive(p_curve, primComp, energy, randLerp, _turbID));

            _turbID++;
        }
    }

    // Calculer les énergies des primitives en fonction des taille
    private void CollectEnergies(float p_deltaTime)
    {
        foreach (SuperPrimitive primitive in _primitives)
        {
            float energy;
            if (primitive.GetSize() > Constants.MIN_SIZE_TALL) // Grande
            {
                // Uniquement du transfert, pas de dissipation
                energy = primitive.GetTransferEnergy() * p_deltaTime;
                _energiesTransfert[1] = energy;
            } else if (primitive.GetSize() > Constants.MIN_SIZE_MEDIUM) // Moyenne
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
            _primitives.Add(new SuperPrimitive(_curve, primComp, energy * 1.25f, randLerp, _turbID));
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

        // Rajouter des primitives s'il en manque
        // Pas physiquement réaliste car perte compensation de perte d'énergie
        //if (_primitives.Count <= _nbPrimitives - 1)
        //{
        //    Debug.Log("Nouvelle primitives");

        //    WindPrimitive[] primComp = GenerateWindComp();

        //    float randLerp = Random.Range(0f, 1f);
        //    float energy = _meanEnergyNewPrim + Random.Range(-_meanEnergyNewPrim * _stdNewEnergy, _meanEnergyNewPrim * _stdNewEnergy);
        //    _primitives.Add(new SuperPrimitive(_primitives[0].GetCurve(), primComp, energy, randLerp));
        //}
    }

    // Utilitaire
    private WindPrimitive[] GenerateWindComp()
    {
        WindPrimitiveType[] newPrimitives = new WindPrimitiveType[2] { WindPrimitiveType.Uniform, WindPrimitiveType.Sink };
        int newPrimId = Random.Range(0, 100);
        newPrimId = newPrimId < 50 ? 0 : 1;
        //return new WindPrimitive[1] { new WindPrimitive(newPrimitives[newPrimId], 2f) };
        return new WindPrimitive[2] { new WindPrimitive(WindPrimitiveType.Vortex, 10f),
                                      new WindPrimitive(newPrimitives[newPrimId], 2f)
                                    };
    }

    public List<SuperPrimitive> GetPrimitives()
    {
        return _primitives;
    }
}
