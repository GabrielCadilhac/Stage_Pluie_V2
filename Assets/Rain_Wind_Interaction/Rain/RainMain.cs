using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.UIElements;

public class RainMain : MonoBehaviour
{
    [SerializeField] BezierCurve _bezierCurve;
    [SerializeField] ComputeShader _rainUpdateShader, _rainCollisionShader, _windShader;
    [SerializeField] Material _rainMaterial, _splashMaterial;
    [SerializeField] GameObject[] _obbs;

    RainManager _rainManager;

    private List<CustomSampler> _samplers;
    private CustomSampler _windSampler, _rainUpdateSampler, _rainCollisionSampler, _rainRendererSampler, _splatchRendererSampler, _turbSampler, _otherSampler;

    void Start()
    {
        Bounds bounds = gameObject.GetComponent<BoxCollider>().bounds;

        _rainManager = new RainManager(transform, bounds, _obbs, _bezierCurve, _windShader, _rainUpdateShader, _rainCollisionShader, _rainMaterial, _splashMaterial);

        // custom profilling sampler
        _windSampler = CustomSampler.Create("Wind_Update");
        _rainUpdateSampler = CustomSampler.Create("Rain_Update");
        _rainCollisionSampler = CustomSampler.Create("Rain_Collision");
        _rainRendererSampler = CustomSampler.Create("Rain_Renderer");
        _splatchRendererSampler = CustomSampler.Create("Splatch_Renderer");
        _turbSampler = CustomSampler.Create("Turbulence_Update");
        _otherSampler = CustomSampler.Create("Other");

        // Sampler to get times
        _samplers = new List<CustomSampler>();
        _samplers.AddRange(new CustomSampler[7] {
            _windSampler, _rainUpdateSampler, _rainCollisionSampler, _rainRendererSampler, _splatchRendererSampler, _turbSampler, _otherSampler});
    }

    public void Restart()
    {
        _rainManager.Disable();
        _rainManager.InitAnimation();
    }

    // Update is called once per frame
    void Update()
    {
        // Permet de réinitialiser les gouttes à des positions aléatoires
        //if (Input.GetKeyDown(KeyCode.Space))
        //    _rainManager.ResetParticles();

        //// Permet d'afficher l'énergie des turbulences pour vérifier leur évolution
        //if (Input.GetKeyDown(KeyCode.C))
        //    _rainManager.CheckWindEnergy();

        //if (Input.GetKeyDown(KeyCode.S))
        //{
        //    ScreenCapture.CaptureScreenshot($"./Assets/Results/results_{DateTime.Now.ToString("dd_MM_hh_mm_ss")}.png", 2);
        //    Debug.Log($"Screen captured : ./Assets/Results/results_{DateTime.Now.ToString("dd_MM_hh_mm_ss")}.png");
        //}

        float dt = Constants.DELTA_TIME * Time.deltaTime;

        //_turbSampler.Begin();
        _rainManager.UpdateCascade(dt);
        //_turbSampler.End();

        //_windSampler.Begin();
        _rainManager.UpdateWind();
        //_windSampler.End();

        //_rainUpdateSampler.Begin();
        _rainManager.UpdateRain();
        //_rainUpdateSampler.End();

        //_rainCollisionSampler.Begin();
        _rainManager.RainCollision(dt);
        //_rainCollisionSampler.End();

        //_rainRendererSampler.Begin();
        _rainManager.RenderRain();
        //_rainRendererSampler.End();

        //if (Constants.RENDER_SPLASH)
        //{
        //    _splatchRendererSampler.Begin();
            _rainManager.RenderSplatch();
        //    _splatchRendererSampler.End();            
        //}
    }

    public List<CustomSampler> GetCustomSamplers()
    {
        return _samplers;
    }

    private void OnDisable()
    {
        _rainManager.Disable();
    }

    private void OnDrawGizmos()
    {
        _rainManager?.DrawGizmos();
    }
}
