using System.Collections;
using NUnit.Framework;
using UnityEngine;

public class RainManager : MonoBehaviour
{
    // General parameter
    private Bounds _bounds;
    [SerializeField] private BezierCurve _bezierCurve;
    [SerializeField] private bool _showGizmos;
    [SerializeField] private Hodograph _hodograph;

    // Wind parameters
    private WindGenerator _windGenerator;
    [SerializeField] private Vector3 _globalWind;
    [SerializeField] private float _localWindForce, _globalWindForce, _deltaTime;

    // Rain parameters
    private RainRenderer _renderer;
    private RainGenerator _rainGenerator;
    [SerializeField] private int _nbParticles = 20000;
    [SerializeField] private ComputeShader _updateShader;
    [SerializeField] private ComputeShader _collisionShader;
    [SerializeField] private float _forceRotation;

    // Splash
    [SerializeField] private GameObject _splashPlane;
    private SplashRenderer _splashRenderer;

    // Test
    [SerializeField] private Test _test;
    [SerializeField] private GameObject _lights;

    void Start()
    {
        // Init wind grid
        _bounds = GetComponent<BoxCollider>().bounds;
        _windGenerator = new WindGenerator(_bounds, _bezierCurve, _globalWindForce, _localWindForce, _deltaTime * Time.deltaTime);
        _windGenerator.SetHodograph(_hodograph.GetPoints());

        // Init rain shader
        Material material = GetComponent<Renderer>().material;
        Vector3 min = transform.localPosition - _bounds.size / 2f;
        Vector3 max = transform.localPosition + _bounds.size / 2f;
        _renderer = new RainRenderer(material, _bounds, min, max, transform, _nbParticles);

        // Init splash shader
        material = _splashPlane.GetComponent<Renderer>().material;
        Bounds splashBounds = new Bounds(_bounds.center, new Vector3(_bounds.size.x, 0f, _bounds.size.z));

        _splashRenderer = new SplashRenderer(material, transform, splashBounds, _nbParticles);
        GraphicsBuffer splashPosBuffer = _splashRenderer.GetPositions();
        ComputeBuffer  splashTime = _splashRenderer.GetTimeSplash();

        // Init rain generator (compute buffer)
        GraphicsBuffer posBuffer = _renderer.GetPositionsBuffer();
        _rainGenerator = new RainGenerator(_updateShader, _collisionShader, posBuffer, splashPosBuffer, splashTime, _bounds, transform, _deltaTime, _nbParticles);
        _rainGenerator.SetGlobalWind(_globalWind, _globalWindForce);

        _renderer.SetVelBuffer(_rainGenerator.GetVelBuffer());
        _renderer.SetSizeBuffer(_rainGenerator.GetSizeBuffer());
    }

    void Update()
    {
        // Permet de réinitialiser les gouttes à des positions aléatoires
        if (Input.GetKeyDown(KeyCode.Space))
            ResetParticles();

        // Permet d'afficher l'énergie des turbulences pour vérifier leur évolution
        if (Input.GetKeyDown(KeyCode.C))
            _windGenerator.CheckEnergy();
        
        _windGenerator.SetDeltaTime(_deltaTime * Time.deltaTime);
        _rainGenerator.SetDeltaTime(_deltaTime * Time.deltaTime);
        
        // Envoie l'hodographe au générateur de vent pour calculer le cisaillement du vent dans la grille
        _windGenerator.SetHodograph(_hodograph.GetPoints());
        _windGenerator.Update();

        _rainGenerator.SetWinds(_windGenerator.GetWinds());
        _rainGenerator.Dispatch();

        _renderer.SetWindRotation(_forceRotation);

        _renderer.UpdateLights();
        _renderer.Draw();
        _splashRenderer.Draw();

        //_test.AddSplashs(_splashRenderer.GetPositions());
    }

    public void LocalWindForceChanged()
    {
        _windGenerator?.SetLocalWindForce(_localWindForce);
    }

    public void GlobalWindForceChanged()
    {
        _rainGenerator?.SetGlobalWind(_globalWind, _globalWindForce);
        _windGenerator?.SetGlobalWindStrength(_globalWindForce);
    }

    public void OnDisable()
    {
        _rainGenerator.Disable();
        _splashRenderer.Disable();
        _renderer.Disable();
    }

    private void ResetParticles()
    {
        Vector3 min = _bounds.center - _bounds.size / 2f;
        Vector3 max = _bounds.center + _bounds.size / 2f;

        Vector3[] newPos = new Vector3[_nbParticles];
        for (int i = 0; i < _nbParticles; i++)
            newPos[i] = transform.InverseTransformPoint(new Vector3(
                Random.Range(min.x, max.x),
                Random.Range(min.y, max.y),
                Random.Range(min.z, max.z))
            );

        _renderer.SetParticles(newPos);
        _rainGenerator.ResetParticles(_nbParticles);

        Debug.Log("Particules réinitialisées !");
    }

    private void OnDrawGizmos() // Debugger la grille du vent (permet de voir les perturbations)
    {
        if (!_showGizmos || _windGenerator == null) return;

        Grid grid = _windGenerator.GetGrid();
        Vector3 globalMin = transform.TransformPoint(_bounds.min) - transform.localPosition;
        Vector3 cellSize = grid.GetCellSize();

        for (int j = 0; j < Common.NB_CELLS.x; j++)
        {
            for (int i = 0; i < Common.NB_CELLS.y; i++)
            {
                for (int k = 0; k < Common.NB_CELLS.z; k++)
                {
                    Vector3 newPos = new Vector3(j * cellSize.x, i * cellSize.y, k * cellSize.z) + globalMin;

                    Vector3 cellCenter = grid.GetCellCenter(newPos);
                    Vector3 wind = grid.Get(j, i, k);
                    
                    //Vector3 globWind = _globalWind * _globalWind.magnitude;
                    if (Mathf.Abs((wind.normalized - Vector3.zero).magnitude) <= 0.1f) continue;

                    Vector3 temp = wind * 0.5f + Vector3.one * 0.5f;
                    Color c = new Color(temp.x, temp.y, temp.z);

                    Gizmos.color = c;
                    Common.DrawArrow(cellCenter, wind, c, 0.5f, 5f);
                    Gizmos.DrawWireCube(cellCenter, grid.GetCellSize() - Vector3.one * 0.1f);
                }
            }
        }
    }
}
