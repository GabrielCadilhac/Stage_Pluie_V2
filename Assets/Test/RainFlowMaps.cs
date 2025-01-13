using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class RainFlowMaps : MonoBehaviour
{
    private const int SIZE = 16;

    private Vector3[] _normals;
    private Vector3[] _localNormals;
    private RainFlow[] _rainFlows;

    private Texture2DArray _texturesArray;

    [SerializeField] private Material _dripMaterial;
    // Start is called before the first frame update
    void Start()
    {
        _texturesArray = new Texture2DArray(SIZE, SIZE, 6, TextureFormat.RGBA32, false);
        _texturesArray.filterMode = FilterMode.Point;
        _dripMaterial.SetTexture("_Textures", _texturesArray);

        _normals = new Vector3[6] { -transform.up, transform.up, transform.right, -transform.right, transform.forward, -transform.forward };
        _localNormals = new Vector3[6];
        for (int i = 0; i < 6; i++)
            _localNormals[i] = transform.InverseTransformDirection(_normals[i]);

        _rainFlows = new RainFlow[6];
        for (int i = 0; i < 6; i++)
        {
            _rainFlows[i] = new RainFlow(transform, transform.position, _normals[i]);
            _rainFlows[i].GenerateMesh(transform.localScale.x);
            _rainFlows[i].DrawFlowMap();
        }
        UpdateTextures();
    }

    void UpdateTextures()
    {
        // Init _texturesArray with a new textures
        for (int i = 0; i < 6; i++)
            _texturesArray.SetPixels(_rainFlows[i].GetTexture().GetPixels(), i, 0);
        _texturesArray.Apply();
    }

    // Update is called once per frame
    void Update()
    {
        for (int i = 0; i < 6; i++)
        {
            _rainFlows[i].Update(_normals[i]);
            _rainFlows[i].DrawFlowMap();
        }
        UpdateTextures();

        // Raycasst mouse to get the uv of the plane to add a drop
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit))
            {
                int k = 0;
                while (hit.normal != _normals[k])
                    k++;

                Vector3 localPoint = transform.InverseTransformPoint(hit.point);
                Vector3 localNormal = transform.InverseTransformDirection(hit.normal);

                Vector3 e1 = Vector3.Cross(hit.normal, Vector3.right).normalized;
                Vector3 e2;
                if (e1.magnitude < 0.001f)
                {
                    e2 = Vector3.Cross(hit.normal, Vector3.up).normalized;
                    e1 = Vector3.Cross(hit.normal, e2).normalized;

                } else
                {
                    e2 = Vector3.Cross(hit.normal, e1).normalized;
                }


                Vector2Int uv = ComputeCellCoord(localPoint, localNormal);
                _rainFlows[k].AddDrop(uv.x, uv.y, new Vector3(-transform.right.y, -transform.up.y * e1.y, 0f));
            }
        }

        // Raycasst mouse to get the uv of the plane to add obstacles
        if (Input.GetMouseButtonDown(1))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit))
            {
                int k = 0;
                while (hit.normal != _normals[k])
                    k++;

                Vector3 localPoint  = transform.InverseTransformPoint(hit.point);
                Vector3 localNormal = transform.InverseTransformDirection(hit.normal);

                Vector2Int uv = ComputeCellCoord(localPoint, localNormal);

                _rainFlows[k].AddObstacle(uv.x, uv.y);
            }
        }
    }

    private Vector2Int ComputeCellCoord(Vector3 p_localPoint, Vector3 p_localNormal)
    {
        Vector3 e1 = Vector3.Cross(p_localNormal, Vector3.right).normalized;
        float u, v;
        if (e1.magnitude < 0.001f)
        {
            e1 = Vector3.Cross(p_localNormal, Vector3.up).normalized;
            Vector3 e2 = Vector3.Cross(p_localNormal, e1).normalized;

            u = -Vector3.Dot(p_localPoint, e2) * 2f;
            v = Vector3.Dot(p_localPoint, e1) * 2f;
        }
        else
        {
            Vector3 e2 = Vector3.Cross(p_localNormal, e1).normalized;

            u = Vector3.Dot(p_localPoint, e1) * 2f;
            v = Vector3.Dot(p_localPoint, e2) * 2f;
        }
        u = Mathf.Clamp01(u * 0.5f + 0.5f);
        v = Mathf.Clamp01(v * 0.5f + 0.5f);

        return new Vector2Int((int)(v * SIZE), (int)(u * SIZE));
    }

    private void OnDrawGizmos()
    {
        //if (_rainFlows == null)
        //    return;
        // Draw the normal array for each faces
        _normals = new Vector3[6] { -transform.up, transform.up, transform.right, -transform.right, transform.forward, -transform.forward };
        for (int i = 0; i < 6; i++)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawRay(transform.position, _normals[i] * 2f);
        }
    }

    private void OnDisable()
    {
        for (int i = 0; i < 6; i++)
            _rainFlows[i].OnDisable();
    }
}
