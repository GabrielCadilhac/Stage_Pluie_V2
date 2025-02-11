using UnityEngine;

namespace Water_Ripples
{
    public class Ripples : MonoBehaviour
    {
        [SerializeField] private Vector2Int resolution;
        [SerializeField] private Material material;
        [SerializeField] private float dampingFactor;
        
        private Texture2D _rippleTexture;
        private float[,] _oldValues,_newValues;
        
        private Camera _camera;
        void Start()
        {
            _rippleTexture = new Texture2D(resolution.x, resolution.y)
            {
                filterMode = FilterMode.Point
            };

            _oldValues = new float[resolution.x, resolution.y];
            _newValues = new float[resolution.x, resolution.y];
            for (int i = 1; i < resolution.x - 1; i++)
            for (int j = 1; j < resolution.y - 1; j++)
            {
                _oldValues[i, j] = 0f;
                _newValues[i, j] = 0f;
            }
            
            material.mainTexture = _rippleTexture;

            _camera = GameObject.Find("Camera").GetComponent<Camera>();
        }

        void Update()
        {
            for (int i = 1; i < resolution.x - 1; i++)
            for (int j = 1; j < resolution.y - 1; j++)
            {
                float newVal = (_oldValues[i+1, j] + _oldValues[i-1, j] + _oldValues[i, j+1] + _oldValues[i, j-1])/2f - _newValues[i,j];
                _newValues[i, j] = newVal * dampingFactor;
                _rippleTexture.SetPixel(i,j,new Color(newVal, newVal, newVal, 1f));
            }
            _rippleTexture.Apply();
            
            // Swapping buffers
            (_oldValues, _newValues) = (_newValues, _oldValues);
            
            if (Input.GetMouseButtonDown(0))
            {
                Ray ray = _camera.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hit,  Mathf.Infinity))
                {
                    if (_oldValues != null)
                    {
                        int x = (int)(hit.textureCoord.x * resolution.x);
                        int y = (int)(hit.textureCoord.y * resolution.y);
                        _oldValues[x,y] += 10f;
                    }
                }
            }
        }
    }
}
