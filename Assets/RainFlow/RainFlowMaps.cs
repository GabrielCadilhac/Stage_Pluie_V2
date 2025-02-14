using UnityEngine;

namespace RainFlow
{
    public class RainFlowMaps : MonoBehaviour
    {
        public const int Size = 64;
        
        private static readonly int NormalMap = Shader.PropertyToID("_BumpMap");

        private RainFlow _rainFlows;

        private Texture2D _texture;

        [SerializeField] private Material dripMaterial;
        [SerializeField] private ComputeShader dripComputeShader;
        [SerializeField] private Texture2D roughnessMap;
        [SerializeField] private Texture2D normalMap;
        [SerializeField] [Range(0f,1f)] private float dotThreshold;
        [SerializeField] [Range(0f,50f)] private float textureScale = 1f;
        [SerializeField] private bool showObstacles;

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
        }

        public void AddDrop(int pI, int pJ)
        {
            _rainFlows.AddDrop(pI, pJ, new Vector3(-transform.right.y, -transform.up.y, 0f));
        }

        void Update()
        {
            _rainFlows.Update(-transform.forward);
            _rainFlows.DrawFlowMap(dotThreshold, showObstacles);

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
            if (!Input.GetMouseButtonDown(1)) return;
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

        private void ScaleNormalMap()
        {
            Material material = GetComponent<Renderer>().material;
            Texture2D currentNormalMap = material.GetTexture(NormalMap) as Texture2D;
            if (currentNormalMap == null)
                return;
            
            Texture2D newNormalMap = new Texture2D(Size, Size, TextureFormat.RGBA32, true, true);
            
            Color32[] cols = currentNormalMap.GetPixels32(0);
            Color32[] newCols = new Color32[Size * Size];
            for (int i = 0; i < Size * Size; i++)
            {
                int c = (int) (i % Size * textureScale);
                int l = (int) ((float) i / Size * textureScale);
                newCols[i] = cols[c+l*currentNormalMap.width];
            }
            newNormalMap.SetPixels32(newCols);
            newNormalMap.Apply();
            material.SetTexture(NormalMap, newNormalMap);
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

        private void OnDisable()
        {
            _rainFlows.OnDisable();
        }
    }
}
