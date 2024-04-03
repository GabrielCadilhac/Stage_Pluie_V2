using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class RainManager : MonoBehaviour
{
    // General parameter
    private GraphicsBuffer _posBuffer;
    [SerializeField] private BezierCurve _bezierCurve;
    [SerializeField] private BoxCollider _box;
    [SerializeField] private bool _showGizmos;

    // Wind parameters
    private WindGenerator _windGenerator;
    [SerializeField] private Vector3 _globalWind;
    [SerializeField] private float _localWindForce, _deltaTime;

    // Rain parameters
    private RainRenderer _renderer;
    private RainGenerator _rainGenerator;
    private int _nbParticles = 10000;
    [SerializeField] private ComputeShader _updateShader;
    [SerializeField] private ComputeShader _collisionShader;
    [SerializeField] private float _forceRotation;

    void Start()
    {
        _windGenerator = new WindGenerator(_box.bounds, _bezierCurve, _globalWind, 0, 1, _localWindForce, _deltaTime);
        
        Material material = GetComponent<Renderer>().material;
        _renderer = new RainRenderer(material, _box.bounds, transform, _nbParticles);

        GetComponent<MeshFilter>().sharedMesh = _renderer.GetMesh();

        _posBuffer = _renderer.GetPositionsBuffer();
        _rainGenerator = new RainGenerator(_updateShader, _collisionShader, _posBuffer, _box.bounds, transform, _deltaTime, _nbParticles);

        _posBuffer.Release();
        _posBuffer = null;
    }

    void Update()
    {
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

    public void OnDisable()
    {
        _rainGenerator.Disable();
        _renderer.Disable();
    }

    private void OnDrawGizmos()
    {
        if (!_showGizmos || _windGenerator == null) return;

        Grid grid = _windGenerator.GetGrid();
        Vector3 offset = Common.Divide(_box.center - _box.size / 2f, _box.size);

        for (int i = 0; i < Common.NB_CELLS.x; i++)
        {
            for (int j = 0; j < Common.NB_CELLS.y; j++)
            {
                for (int k = 0; k < Common.NB_CELLS.z; k++)
                {
                    float x = i + Common.NB_CELLS.x * offset.x;
                    float y = j + Common.NB_CELLS.y * offset.y;
                    float z = k + Common.NB_CELLS.z * offset.z;

                    Vector3 cellCenter = grid.GetCellCenter(new Vector3(x, y, z));
                    Vector3 wind = grid.Get(i, j, k);
                    //if (Mathf.Abs((wind.normalized - _globalWind.normalized).magnitude) <= 0.01f) continue;

                    Vector3 temp = wind * 0.5f + Vector3.one * 0.5f;
                    Color c = new Color(temp.x, temp.y, temp.z);

                    Common.DrawArrow(cellCenter, wind, c, 0.5f, 5f);
                    Gizmos.color = c;
                    Gizmos.DrawWireCube(cellCenter, grid.GetCellSize());
                }
            }
        }
    }
}
