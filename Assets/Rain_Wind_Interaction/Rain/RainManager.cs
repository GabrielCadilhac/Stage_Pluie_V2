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
    private GraphicsBuffer _splashNormalBuffer;// _splashPosBuffer;
    private SplashRenderer2 _splashRenderer;

    // Synchro
    private GraphicsFence _fence;

    // Test
    public static int _nbParticles = 10000;
    private GameObject[] _obbs;

    public RainManager(Transform p_transform, Bounds p_bounds, GameObject[] p_obbs, BezierCurve p_curve, ComputeShader p_windShader, ComputeShader p_rainUpdate, ComputeShader p_rainCollision, Material p_rainMaterial, Material p_splashMaterial)
    {
        // GameObject parameter
        _transform = p_transform;
        _bounds = p_bounds;
        _bezierCurve = p_curve;

        _obbs = p_obbs;

        // Compute shaders
        _windShader = p_windShader;
        _updateShader = p_rainUpdate;
        _collisionShader = p_rainCollision;

        // Materials
        _material = p_rainMaterial;
        _splashMaterial = p_splashMaterial;

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
        if (_nbParticles <= 0)
            return;

        // World space min and max rain box
        _globalMin = _transform.position - _bounds.size / 2f;
        _globalMax = _transform.position + _bounds.size / 2f;
        Debug.Log($"Min {_globalMin} et max {_globalMax}");

        // Init rain shader (object shader)
        _renderer = new RainRenderer2(_material, _bounds, _globalMin, _globalMax, _transform);

        // Init splash shader (object shader)
        _splashRenderer = new SplashRenderer2(_splashMaterial);
        //_splashPosBuffer = _splashRenderer.GetPosBuffer();
        _splashNormalBuffer = _splashRenderer.GetNormalBuffer();

        // Init rain generator (compute buffer)
        GraphicsBuffer posBuffer = _renderer.GetPositionsBuffer();
        ComputeBuffer windBuffer = _windGenerator.GetGPUWind();
        _rainGenerator = new RainGenerator(_updateShader, _collisionShader, posBuffer, _splashRenderer.GetPosBuffer(), _splashNormalBuffer, windBuffer, _obbs, _bounds, _globalMin, _globalMax) ;
        _rainGenerator?.ChangeGlobalWind();

        //_rainGenerator.SetWinds(_windGenerator.GetWinds());

        _renderer.SetVelBuffer(_rainGenerator.GetVelBuffer());
        _renderer.SetSizeBuffer(_rainGenerator.GetSizeBuffer());
    }

    public void UpdateCascade(float p_deltaTime)
    {
        _windGenerator.UpdateCascade(_transform.position, p_deltaTime);
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

    public void RainCollision(float p_deltaTime)
    {
        _rainGenerator.DispatchCollision(p_deltaTime, _transform);
        Graphics.WaitOnAsyncGraphicsFence(_fence);
    }

    public void RenderRain()
    {
        _renderer.Draw();
        Graphics.WaitOnAsyncGraphicsFence(_fence);
    }

    public void RenderSplatch()
    {
        _splashRenderer.Draw();
        Graphics.WaitOnAsyncGraphicsFence(_fence);
    }

    public void Update(float p_deltaTime)
    {
        // Rain box dynamique quand la caméra se déplace, les collisions avec les gouttes sont mises à jour
        
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
        Vector3[] newPos = new Vector3[_nbParticles];

        for (int i = 0; i < _nbParticles; i++)
            newPos[i] = new Vector3(
                Random.Range(_globalMin.x, _globalMax.x),
                Random.Range(_globalMin.y, _globalMax.y),
                Random.Range(_globalMin.z, _globalMax.z)
            );

        _renderer.SetParticles(newPos);
        _rainGenerator.ResetParticles(_nbParticles);

        Debug.Log("Particules réinitialisées !");
    }

    public void Disable()
    {
        _rainGenerator.Disable();
        _renderer.Disable();
        _windGenerator.Disable();
        _splashRenderer.Disable();

        //_splashPosBuffer.Release();
        //_splashPosBuffer = null;

        _splashNormalBuffer.Release();
        _splashNormalBuffer = null;
    }

    public void DestroySphere()
    {
        _windGenerator.DestroySphere();
    }

    public void DrawGizmos() // Debugger la grille du vent (permet de voir les perturbations)
    {
        //if (!Constants.DRAW_DEBUG_GRID || _windGenerator == null) return;
        /*
        Grid grid = _windGenerator.GetGrid();
        Vector3 cellSize = grid.GetCellSize();

        Vector3[] localWinds = _windGenerator.GetData();

        for (int j = 0; j < Common.NB_CELLS.x; j++)
        {
            for (int i = 0; i < Common.NB_CELLS.y; i++)
            {
                for (int k = 0; k < Common.NB_CELLS.z; k++)
                {
                    Vector3 newPos = new Vector3(j * cellSize.x, i * cellSize.y, k * cellSize.z) + _globalMin;

                    Vector3 cellCenter = grid.GetCellCenter(newPos);
                    int index = (k * Common.NB_CELLS.y + i) * Common.NB_CELLS.x + j;
                    //Vector3 wind = grid.Get(j, i, k);
                    Vector3 wind = localWinds[index];

                    //Vector3 globWind = _globalWind * _globalWind.magnitude;
                    if (Mathf.Abs((wind.normalized - Vector3.zero).magnitude) <= 0.1f) continue;

                    Vector3 temp = wind * 0.5f + Vector3.one * 0.5f;
                    Color c = new Color(temp.x, temp.y, temp.z);

                    Gizmos.color = c;
                    Common.DrawArrow(cellCenter, wind, c, 0.5f, 5f);
                    Gizmos.DrawWireCube(cellCenter, cellSize - Vector3.one * 0.1f);
                }
            }
        }
        */
    }
}