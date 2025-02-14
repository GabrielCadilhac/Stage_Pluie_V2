using UnityEngine;
using UnityEngine.Serialization;

[ExecuteInEditMode]
public class ChangeCommon : MonoBehaviour
{
    [FormerlySerializedAs("_energyStrength")] [SerializeField] private float energyStrength;
    [FormerlySerializedAs("_energySpeed")] [SerializeField] private float energySpeed;
    [FormerlySerializedAs("_coeffDissip")] [SerializeField] private float coeffDissip;
    [FormerlySerializedAs("_coeffTransfert")] [SerializeField] private float coeffTransfert;

    [FormerlySerializedAs("_meanEnergy")] [SerializeField] private float meanEnergy;
    [FormerlySerializedAs("_stdEnergyPrim")] [SerializeField] private float stdEnergyPrim;
    [FormerlySerializedAs("_minSizeTall")] [SerializeField] private float minSizeTall;
    [FormerlySerializedAs("_minSizeMedium")] [SerializeField] private float minSizeMedium;
    [FormerlySerializedAs("_minSizeSmall")] [SerializeField] private float minSizeSmall;

    [FormerlySerializedAs("_sphereSize")] [SerializeField] private float sphereSize;
    [FormerlySerializedAs("_renderSphere")] [SerializeField] private bool renderSphere;
    [FormerlySerializedAs("_renderSplash")] [SerializeField] private bool renderSplash;
    [FormerlySerializedAs("_drawDebugGrid")] [SerializeField] private bool drawDebugGrid;

    [FormerlySerializedAs("_deltaTime")] [SerializeField] private float deltaTime;

    [FormerlySerializedAs("_localWindStrength")] [SerializeField] private float localWindStrength;
    [FormerlySerializedAs("_globalWind")] [SerializeField] private Vector3 globalWind;

    private void Awake()
    {
        ChangeConstants(renderSphere);
    }

    public void ChangeConstants(bool pRenderSphere)
    {
        Constants.DeltaTime = deltaTime;

        Constants.EnergyStrength = energyStrength;
        Constants.EnergySpeed    = energySpeed;

        //Constants.COEFF_DISSIP = _coeffDissip;
        Constants.CoeffTransfert = coeffTransfert;

        Constants.MeanEnergyPrim = meanEnergy;
        Constants.StdEnergyPrim  = stdEnergyPrim;

        Constants.MinSizeTall   = minSizeTall;
        Constants.MinSizeMedium = minSizeMedium;
        Constants.MinSizeSmall  = minSizeSmall;

        Constants.RenderSphere   = pRenderSphere;
        Constants.SphereSize     = sphereSize;
        Constants.DrawDebugGrid = drawDebugGrid;
        Constants.RenderSplash   = renderSplash;

        Constants.LocalWindStrength = localWindStrength;
        Constants.GlobalWind = globalWind;
    }
}
