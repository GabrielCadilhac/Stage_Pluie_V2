using UnityEngine;

[ExecuteInEditMode]
public class ChangeCommon : MonoBehaviour
{
    [SerializeField] private float _energyStrength, _energySpeed, _coeffDissip, _coeffTransfert;

    [SerializeField] private float _meanEnergy, _stdEnergyPrim, _minSizeTall, _minSizeMedium, _minSizeSmall;

    [SerializeField] private float _sphereSize;
    [SerializeField] private bool _renderSphere, _renderSplash, _drawDebugGrid;

    [SerializeField] private float _deltaTime;

    [SerializeField] private float _localWindStrength;
    [SerializeField] private Vector3 _globalWind;

    private void Awake()
    {
        ChangeConstants(_renderSphere);
    }

    public void ChangeConstants(bool p_renderSphere)
    {
        Constants.DELTA_TIME = _deltaTime;

        Constants.ENERGY_STRENGTH = _energyStrength;
        Constants.ENERGY_SPEED    = _energySpeed;

        //Constants.COEFF_DISSIP = _coeffDissip;
        Constants.COEFF_TRANSFERT = _coeffTransfert;

        Constants.MEAN_ENERGY_PRIM = _meanEnergy;
        Constants.STD_ENERGY_PRIM  = _stdEnergyPrim;

        Constants.MIN_SIZE_TALL   = _minSizeTall;
        Constants.MIN_SIZE_MEDIUM = _minSizeMedium;
        Constants.MIN_SIZE_SMALL  = _minSizeSmall;

        Constants.RENDER_SPHERE   = p_renderSphere;
        Constants.SPHERE_SIZE     = _sphereSize;
        Constants.DRAW_DEBUG_GRID = _drawDebugGrid;
        Constants.RENDER_SPLASH   = _renderSplash;

        Constants.LOCAL_WIND_STRENGTH = _localWindStrength;
        Constants.GLOBAL_WIND = _globalWind;
    }
}
