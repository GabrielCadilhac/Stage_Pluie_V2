using Drop_Impact;
using UnityEngine;
using UnityEngine.Rendering;

public class RainManager
{
    // General parameter
    private BezierCurve _bezierCurve;
    private Bounds _bounds;
    private Vector3 _globalMin, _globalMax;
    private Transform _transform;

    // Wind parameters
    private WindGenerator _windGenerator;
    private ComputeShader _windShader;

    // Rain parameters
    private RainRenderer2 _renderer;
    private RainGenerator _rainGenerator;
    private ComputeShader _updateShader;
    private ComputeShader _collisionShader;
    private Material _material;

    // Splash
    private Material _splashMaterial;
    //private SplashRenderer2 _splashRenderer;
    private RainImpact _rainImpact;

    // Synchro
    private GraphicsFence _fence;

    // Test
    public const int NbParticles = 100;

    private GameObject[] _obbs;

    public RainManager(Transform pTransform, Bounds pBounds, GameObject[] pObbs, BezierCurve pCurve, ComputeShader pWindShader, ComputeShader pRainUpdate, ComputeShader pRainCollision, Material pRainMaterial, Material pSplashMaterial)
    {
        // GameObject parameter
        _transform = pTransform;
        _bounds = pBounds;
        _bezierCurve = pCurve;

        _obbs = pObbs;

        // Compute shaders
        _windShader = pWindShader;
        _updateShader = pRainUpdate;
        _collisionShader = pRainCollision;

        // Materials
        _material = pRainMaterial;
        _splashMaterial = pSplashMaterial;

        // Synchronize compute and vertex/fragment shaders
        _fence = Graphics.CreateGraphicsFence(GraphicsFenceType.AsyncQueueSynchronisation, SynchronisationStageFlags.VertexProcessing | SynchronisationStageFlags.ComputeProcessing | SynchronisationStageFlags.PixelProcessing);
        if (!SystemInfo.supportsGraphicsFence)
            Debug.LogError("Fence synchornisation not supported");

        InitAnimation();
    }

    public void InitAnimation()
    {
        // Init wind grid
        _windGenerator = new WindGenerator(_windShader, _transform.position, _bounds.size, _bezierCurve);
        _windGenerator.Reset(_bezierCurve);

        // Init rain shader
        if (NbParticles <= 0)
            return;

        // World space min and max rain box
        _globalMin = _transform.position - _bounds.size / 2f;
        _globalMax = _transform.position + _bounds.size / 2f;
        Debug.Log($"Min {_globalMin} et max {_globalMax}");

        // Init rain shader (object shader)
        _renderer = new RainRenderer2(_material, _bounds, _globalMin, _globalMax, _transform);

        // Init splash shader (object shader)
        //_splashRenderer = new SplashRenderer2(_splashMaterial);
        //_splashNormalBuffer = _splashRenderer.GetNormalBuffer();

        _rainImpact = new RainImpact(_splashMaterial, _transform, NbParticles);

        // Init rain generator (compute buffer)
        GraphicsBuffer posBuffer = _renderer.GetPositionsBuffer();
        ComputeBuffer windBuffer = _windGenerator.GetGPUWind();
        _rainGenerator = new RainGenerator(_updateShader, _collisionShader, posBuffer, _rainImpact, windBuffer, _obbs, _bounds, _globalMin, _globalMax) ;
        _rainGenerator?.ChangeGlobalWind();

        //_rainGenerator.SetWinds(_windGenerator.GetWinds());

        _renderer.SetVelBuffer(_rainGenerator.GetVelBuffer());
        _renderer.SetSizeBuffer(_rainGenerator.GetSizeBuffer());
    }

    public void UpdateCascade(float pDeltaTime)
    {
        _windGenerator.UpdateCascade(_transform.position, pDeltaTime);
    }

    public void UpdateWind()
    {
        _windGenerator.UpdateGPU();
        Graphics.WaitOnAsyncGraphicsFence(_fence);
    }

    public void UpdateRain()
    {
        _rainGenerator.DispatchUpdate();
        Graphics.WaitOnAsyncGraphicsFence(_fence);
    }

    public void RainCollision(float pDeltaTime)
    {
        _rainGenerator.DispatchCollision(pDeltaTime);
        Graphics.WaitOnAsyncGraphicsFence(_fence);
    }

    public void RenderRain()
    {
        _renderer.Draw();
        Graphics.WaitOnAsyncGraphicsFence(_fence);
    }

    public void RenderSplatch()
    {
        _rainImpact.Update(NbParticles, 2f);
        Graphics.WaitOnAsyncGraphicsFence(_fence);
    }

    public void Update(float pDeltaTime)
    {
        // Rain box dynamique quand la cam�ra se d�place, les collisions avec les gouttes sont mises � jour
        
        //_otherSampler.Begin();
        //_globalMin = _transform.position - _bounds.size / 2f;
        //_globalMax = _transform.position + _bounds.size / 2f;
        //_rainGenerator.UpdateBoxBounds(_globalMin, _globalMax);
        //_otherSampler.End();
        
        // Envoie l'hodographe au générateur de vent pour calculer le cisaillement du vent dans la grille
        //_windGenerator.SetHodograph(_hodograph.GetPoints());
    }

    public void CheckWindEnergy()
    {
        _windGenerator.CheckEnergy();
    }

    public void ResetParticles()
    {
        Vector3[] newPos = new Vector3[NbParticles];

        for (int i = 0; i < NbParticles; i++)
            newPos[i] = new Vector3(
                Random.Range(_globalMin.x, _globalMax.x),
                Random.Range(_globalMin.y, _globalMax.y),
                Random.Range(_globalMin.z, _globalMax.z)
            );

        _renderer.SetParticles(newPos);
        _rainGenerator.ResetParticles(NbParticles);

        Debug.Log("Particules r�initialis�es !");
    }

    public void Disable()
    {
        _rainGenerator.Disable();
        _renderer.Disable();
        _windGenerator.Disable();
        _rainImpact.Disable();
    }

    public void DestroySphere()
    {
        _windGenerator.DestroySphere();
    }

    public void DrawGizmos() // Debugger la grille du vent (permet de voir les perturbations)
    {
        if (!Constants.DrawDebugGrid || _windGenerator == null) return;
        
        Grid grid = _windGenerator.GetGrid();
        Vector3 cellSize = grid.GetCellSize();

        Vector3[] localWinds = _windGenerator.GetData();

        Vector3 newPos, cellCenter, wind, temp;
        Color c;
        for (int j = 0; j < Common.NbCells.x; j++)
        {
            for (int i = 0; i < Common.NbCells.y; i++)
            {
                for (int k = 0; k < Common.NbCells.z; k++)
                {
                    newPos = new Vector3(j * cellSize.x, i * cellSize.y, k * cellSize.z) + _globalMin;

                    cellCenter = grid.GetCellCenter(newPos);
                    int index = (k * Common.NbCells.y + i) * Common.NbCells.x + j;
                    //Vector3 wind = grid.Get(j, i, k);
                    wind = localWinds[index];

                    //Vector3 globWind = _globalWind * _globalWind.magnitude;
                    if (Mathf.Abs((wind.normalized - Vector3.zero).magnitude) <= 0.1f) continue;

                    temp = wind * 0.5f + Vector3.one * 0.5f;
                    c = new Color(temp.x, temp.y, temp.z);

                    Gizmos.color = c;
                    Common.DrawArrow(cellCenter, wind, c, 0.5f, 5f);
                    Gizmos.DrawWireCube(cellCenter, cellSize - Vector3.one * 0.1f);
                }
            }
        }
        
    }
}