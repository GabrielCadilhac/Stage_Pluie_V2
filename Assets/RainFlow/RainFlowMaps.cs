using UnityEngine;

namespace RainFlow
{
    public class RainFlowMaps : MonoBehaviour
    {
        public const int Size = 64;
        
        private static readonly int NormalMap = Shader.PropertyToID("_BumpMap");
        private static readonly int HeightMap = Shader.PropertyToID("_ParallaxMap");

        private RainFlow _rainFlows;

        private Texture2D _texture;

        [SerializeField] private Material dripMaterial;
        [SerializeField] private ComputeShader dripComputeShader;
        [SerializeField] private Texture2D roughnessMap;
        [SerializeField] private Texture2D normalMap;
        [SerializeField] [Range(0f,1f)] private float dotThreshold;
        [SerializeField] [Range(0f,8f)] private float textureScale = 1f; // Max = NormalMap.width / Size = 512 / 64 = 8
        [SerializeField] private bool showObstacles;

        private Vector2[,] _vectorField;
        private readonly Vector2[] _neighbors = {
            new (-1f, -1f),
            new (-1f,  0f),
            new (-1f,  1f),
            new ( 0f,  1f),
            new ( 1f,  1f),
            new ( 1f,  0f),
            new ( 1f, -1f),
            new ( 0f, -1f),
        };
        
        private Camera _camera;

        void Start()
        {
            _texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point
            };

            GetComponent<Renderer>().material.mainTexture = _texture;

            _rainFlows = new RainFlow(transform, dripMaterial, _texture, normalMap, dripComputeShader, textureScale);
            _rainFlows.GenerateMesh(transform.localScale.x);
            _camera = Camera.main;

            ScaleNormalMap();

            VectorFieldFromHeight();
        }

        public void AddDrop(int pI, int pJ)
        {
            _rainFlows.AddDrop(pI, pJ, new Vector3(-transform.right.y, -transform.up.y, 0f));
        }

        private void ScaleNormalMap()
        {
            Material material = GetComponent<Renderer>().material;
            Texture2D currentNormalMap = material.GetTexture(NormalMap) as Texture2D;
            if (currentNormalMap == null)
                return;
            
            Texture2D newNormalMap = new Texture2D((int) (Size * textureScale),(int) (Size * textureScale), TextureFormat.RGBA32, true, true);
            
            
            float ratio = (float) currentNormalMap.width / newNormalMap.width;
            
            Color32[] cols = currentNormalMap.GetPixels32(0);
            Color32[] newCols = new Color32[newNormalMap.width * newNormalMap.height];
            for (int i = 0; i < newNormalMap.width; i++)
            for (int j = 0; j < newNormalMap.height; j++)
            {
                newCols[i * newNormalMap.height + j] = cols[(int) (i * newNormalMap.height * ratio) + j];
            }
            newNormalMap.SetPixels32(newCols);
            newNormalMap.Apply();
            material.SetTexture(NormalMap, newNormalMap);
        }

        private void VectorFieldFromHeight()
        {
            Material material = GetComponent<Renderer>().material;
            Texture2D currentHeightMap = material.GetTexture(HeightMap) as Texture2D;
            if (currentHeightMap == null)
                return;
            
            _vectorField = new Vector2[currentHeightMap.width, currentHeightMap.height];
            for (int i = 0; i < currentHeightMap.height; i++)
            for (int j = 0; j < currentHeightMap.width; j++)
            {
                float lowestHeight = Mathf.Infinity;
                Vector2 lowestNeighbor = Vector2.zero;
                Vector2 pos = new Vector2(i, j);
                for (int k = 0; k < _neighbors.Length; k++)
                {
                    Vector2 currentNeighbor = pos + _neighbors[k];
                    Color colorNeighbor = currentHeightMap.GetPixel((int) currentNeighbor.x, (int) currentNeighbor.y);
                    if (colorNeighbor.r < lowestHeight)
                    {
                        lowestHeight = colorNeighbor.r;
                        lowestNeighbor = _neighbors[k];
                    }
                }
                _vectorField[i, j] = lowestNeighbor;
            }
        }

        private Vector2Int ComputeCellCoord(Vector3 pLocalPoint, Vector3 pLocalNormal)
        {
            Vector3 e1 = Vector3.Cross(pLocalNormal, Vector3.right).normalized;
            float u, v;
            if (e1.magnitude < 0.001f)
                e1 = Vector3.Cross(pLocalNormal, Vector3.up).normalized;
        
            Vector3 e2 = Vector3.Cross(pLocalNormal, e1).normalized;
        
            u = -Vector3.Dot(pLocalPoint, e2) * 2f;
            v = Vector3.Dot(pLocalPoint, e1) * 2f;
        
            u = Mathf.Clamp01(u * 0.5f + 0.5f);
            v = Mathf.Clamp01(v * 0.5f + 0.5f);

            return new Vector2Int((int)(v * Size), (int)(u * Size));
        }
        
        void Update()
        {
            _rainFlows.Update(-transform.forward);
            _rainFlows.DrawFlowMap(dotThreshold, showObstacles);
            float ratio = transform.localScale.x / Size;
            
            // Draw the vector field
            for (int i = 0; i < 512; i++)
            for (int j = 0; j < 512; j++)
            {
                float x = (i + 0.5f) * ratio;
                float y = (j + 0.5f) * ratio;
                Vector2 pos = new Vector2(x, y);
                Vector2 n = _vectorField[i, j].normalized;
                Debug.DrawLine(pos, pos + _vectorField[i,j] * 0.1f, new Color(n.x, n.y, 0, 1), 0.1f);
            }

            // Raycast mouse to get the uv of the plane to add a drop
            if (Input.GetMouseButtonDown(0))
            {
                Ray ray = _camera.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    Vector2 uv = hit.textureCoord;
                    int i = (int)(uv.x * Size);
                    int j = (int)(uv.y * Size);

                    // _rainFlows.AddDrop(i, j, new Vector3(-transform.right.y, -transform.up.y, 0f));
                    
                    int dropSize = 5;
                    for (int k = i - dropSize; k < i + dropSize; k++)
                    for (int l = j - dropSize; l < j + dropSize; l++)
                    {
                        if (k is >= 0 and < Size && l is >= 0 and < Size)
                            _rainFlows.AddDrop(k, l, new Vector3(-transform.right.y, -transform.up.y, 0f));
                    }
                }
            }

            // Raycast mouse to get the uv of the plane to add obstacles
            if (!Input.GetMouseButtonDown(1))
                return;

            {
                Ray ray = _camera.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    Vector2 uv = hit.textureCoord;
                    int i = (int)(uv.x * Size);
                    int j = (int)(uv.y * Size);

                    _rainFlows.AddObstacle(i, j);
                }
            }
        }

        private void OnDisable()
        {
            _rainFlows.OnDisable();
        }
        
    }
}
