using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RainFlowMaps : MonoBehaviour
{
    public static int SIZE = 32;

    private RainFlow _rainFlows;

    private Texture2D _texture;

    [SerializeField] private Material _dripMaterial;
    [SerializeField] private ComputeShader _dripComputeShader;
    [SerializeField] private Texture2D _roughnessMap;
    [SerializeField] private Texture2D _normalMap;
    [SerializeField] [Range(0f,1f)] private float _dotThreshold = 0f;
    [SerializeField] [Range(0f,50f)] private float _textureScale = 1f;

	void Start()
    {
        _texture = new Texture2D(SIZE, SIZE, TextureFormat.RGBA32, false);
        _texture.filterMode = FilterMode.Point;

        GetComponent<Renderer>().material.mainTexture = _texture;

        _rainFlows = new RainFlow(transform, _dripMaterial, _texture, _roughnessMap, _normalMap, _dripComputeShader, _textureScale);
        _rainFlows.GenerateMesh(transform.localScale.x);
        //_rainFlows.DrawFlowMap(_dotThreshold);
    }

    public void AddDrop(int p_i, int p_j)
    {
        _rainFlows.AddDrop(p_i, p_j, new Vector3(-transform.right.y, -transform.up.y, 0f));
    }

    void Update()
    {
        _rainFlows.Update(-transform.forward);
        _rainFlows.DrawFlowMap(_dotThreshold);

        // Raycast mouse to get the uv of the plane to add a drop
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit))
            {
                Vector2 uv = hit.textureCoord;
                int i = (int)(uv.x * SIZE);
                int j = (int)(uv.y * SIZE);

                int dropSize = 5;
                for (int k = i - dropSize; k < i + dropSize; k++)
                {
                    for (int l = j - dropSize; l < j + dropSize; l++)
                    {
                        _rainFlows.AddDrop(k, l, new Vector3(-transform.right.y, -transform.up.y, 0f));
                    }
                }
            }
        }

        // Raycasst mouse to get the uv of the plane to add obstacles
        if (Input.GetMouseButtonDown(1))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit))
            {
                Vector2 uv = hit.textureCoord;
                int i = (int)(uv.x * SIZE);
                int j = (int)(uv.y * SIZE);

                _rainFlows.AddObstacle(i, j);
            }
        }
    }

    private Vector2Int ComputeCellCoord(Vector3 p_localPoint, Vector3 p_localNormal)
    {
        Vector3 e1 = Vector3.Cross(p_localNormal, Vector3.right).normalized;
        float u, v;
        if (e1.magnitude < 0.001f)
            e1 = Vector3.Cross(p_localNormal, Vector3.up).normalized;
        
        Vector3 e2 = Vector3.Cross(p_localNormal, e1).normalized;
        
        u = -Vector3.Dot(p_localPoint, e2) * 2f;
        v = Vector3.Dot(p_localPoint, e1) * 2f;
        
        u = Mathf.Clamp01(u * 0.5f + 0.5f);
        v = Mathf.Clamp01(v * 0.5f + 0.5f);

        return new Vector2Int((int)(v * SIZE), (int)(u * SIZE));
    }

    private void OnDisable()
    {
        _rainFlows.OnDisable();
    }
}
