using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

public class RainManager : MonoBehaviour
{
    // General parameter
    private Bounds _bounds;
    [SerializeField] private BezierCurve _bezierCurve;
    [SerializeField] private bool _showGizmos;

    // Wind parameters
    private WindGenerator _windGenerator;
    [SerializeField] private Vector3 _globalWind;
    [SerializeField] private float _localWindForce, _deltaTime, _primitiveSpeed;

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

    void Start()
    {
        // Init wind grid
        _bounds = GetComponent<BoxCollider>().bounds;
        _windGenerator = new WindGenerator(_bounds, _bezierCurve, _globalWind, 1, _primitiveSpeed, _localWindForce, _deltaTime);

        // Init rain shader
        Material material = GetComponent<Renderer>().material;
        _renderer = new RainRenderer(material, _bounds, transform, _nbParticles);

        GetComponent<MeshFilter>().sharedMesh = _renderer.GetMesh();

        // Init splash shader
        material = _splashPlane.GetComponent<Renderer>().material;
        Bounds splashBounds = new Bounds(_bounds.center, new Vector3(_bounds.size.x, 0f, _bounds.size.z));
        _splashRenderer = new SplashRenderer(material, splashBounds, transform, _nbParticles);
        GraphicsBuffer splashPosBuffer = _splashRenderer.GetPositions();
        ComputeBuffer  splashTime = _splashRenderer.GetTimeSplash();

        _splashPlane.GetComponent<MeshFilter>().sharedMesh = _splashRenderer.GetMesh();

        // Init rain generator (compute buffer)
        GraphicsBuffer posBuffer = _renderer.GetPositionsBuffer();
        _rainGenerator = new RainGenerator(_updateShader, _collisionShader, posBuffer, splashPosBuffer, splashTime, _bounds, transform, _deltaTime, _nbParticles);

    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
            ResetParticles();

        _windGenerator.SetDeltaTime(_deltaTime);
        _rainGenerator.SetDeltaTime(_deltaTime);

        _windGenerator.Update();

        _rainGenerator.SetWinds(_windGenerator.GetWinds());
        _rainGenerator.Dispatch();

        _renderer.SetWindRotation(_globalWind, _forceRotation);
    }

    public void GlobalWindChanged()
    {
        _windGenerator?.SetGlobalWind(_globalWind);
    }

    public void LocalWindForceChanged()
    {
        _windGenerator?.SetLocalWindForce(_localWindForce);
    }

    public void PrimitiveSpeedChanged()
    {
        _windGenerator?.SetPrimitiveSpeed(_primitiveSpeed);
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
            Random.Range(min.z, max.z)));

        _renderer.SetParticles(newPos);
        _rainGenerator.ResetParticles(_nbParticles);

        Debug.Log("Particules réinitialisées !");
    }

    private void OnDrawGizmos()
    {
        if (!_showGizmos || _windGenerator == null) return;

        Grid grid = _windGenerator.GetGrid();
        Vector3 offset = Common.Divide(_bounds.center - _bounds.size / 2f, _bounds.size);

        for (int j = 0; j < Common.NB_CELLS.x; j++)
        {
            for (int i = 0; i < Common.NB_CELLS.y; i++)
            {
                for (int k = 0; k < Common.NB_CELLS.z; k++)
                {
                    float x = j + Common.NB_CELLS.x * offset.x;
                    float y = i + Common.NB_CELLS.y * offset.y;
                    float z = k + Common.NB_CELLS.z * offset.z;

                    Vector3 cellCenter = grid.GetCellCenter(new Vector3(x, y, z));
                    Vector3 wind = grid.Get(j, i, k);
                    if (Mathf.Abs((wind.normalized - _globalWind.normalized).magnitude) <= 0.01f) continue;

                    Vector3 temp = wind * 0.5f + Vector3.one * 0.5f;
                    Color c = new Color(temp.x, temp.y, temp.z);

                    Gizmos.color = c;
                    Common.DrawArrow(cellCenter, wind, c, 0.5f, 5f);
                    Gizmos.DrawWireCube(cellCenter, grid.GetCellSize()-Vector3.one * 0.1f);
                }
            }
        }
    }
}
