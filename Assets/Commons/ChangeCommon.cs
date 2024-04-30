using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class ChangeCommon : MonoBehaviour
{
    [SerializeField] private float _energyStrength, _energySpeed, _coeffDissip, _coeffTransfert;

    [SerializeField] private float _meanEnergy, _stdEnergyPrim, _minSizeTall, _minSizeMedium, _minSizeSmall;

    private void OnEnable()
    {
        _energyStrength = Constants.ENERGY_STRENGTH;
        _energySpeed = Constants.ENERGY_SPEED;
        _coeffDissip = Constants.COEFF_DISSIP;
        _coeffTransfert = Constants.COEFF_TRANSFERT;

        _meanEnergy = Constants.MEAN_ENERGY_PRIM;
        _stdEnergyPrim = Constants.STD_ENERGY_PRIM;
        _minSizeTall = Constants.MIN_SIZE_TALL;
        _minSizeMedium = Constants.MIN_SIZE_MEDIUM;
        _minSizeSmall = Constants.MIN_SIZE_SMALL;
    }

    public void ChangeConstants()
    {
        Constants.ENERGY_STRENGTH = _energyStrength;
        Constants.ENERGY_SPEED = _energySpeed;

        Constants.COEFF_DISSIP = _coeffDissip;
        Constants.COEFF_TRANSFERT = _coeffTransfert;

        Constants.MEAN_ENERGY_PRIM = _meanEnergy;
        Constants.STD_ENERGY_PRIM = _stdEnergyPrim;

        Constants.MIN_SIZE_TALL = _minSizeTall;
        Constants.MIN_SIZE_MEDIUM = _minSizeMedium;
        Constants.MIN_SIZE_SMALL = _minSizeSmall;
    }
}
