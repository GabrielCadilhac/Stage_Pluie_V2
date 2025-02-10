using UnityEngine;

namespace Water_Ripples
{
    public class Ripples : MonoBehaviour
    {
        [SerializeField] private Vector2Int resolution;
        [SerializeField] private Vector2 size;
        [SerializeField] private Material material;
        [SerializeField] private float c;
        [SerializeField] private float dampingFactor;
        
        private Texture2D _rippleTexture;
        /* OLD
        private float[,] _waterHeight0, _waterHeight1, _waterHeight2;
        
        private float _dx, _dy;
        */

        private float[,] _oldValues;
        private float[,] _newValues;
        
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
            
            // _waterHeight0 = new float[resolution.x, resolution.y];
            // _waterHeight1 = new float[resolution.x, resolution.y];
            // _waterHeight2 = new float[resolution.x, resolution.y];
            //
            // for (int i = 1; i < resolution.x-1; i++)
            // for (int j = 1; j < resolution.y - 1; j++)
            // {
            //     _waterHeight0[i,j] = 0f;
            //     _waterHeight1[i,j] = 0f;
            //     _waterHeight2[i,j] = 0f;
            // }
            //
            // _dx = 0.1f;
            // _dy = 0.1f;
            
            material.mainTexture = _rippleTexture;

            _camera = GameObject.Find("Camera").GetComponent<Camera>();
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                for (int i = 1; i < resolution.x - 1; i++)
                for (int j = 1; j < resolution.y - 1; j++)
                {
                    float newVal = (_oldValues[i+1, j] + _oldValues[i-1, j] + _oldValues[i, j+1] + _oldValues[i, j-1])*0.5f - _oldValues[i,j];
                    _newValues[i, j] = newVal * dampingFactor;
                    _rippleTexture.SetPixel(i,j,new Color(newVal, newVal, newVal, 1f));
                }
                _rippleTexture.Apply();
            }
            
            if (Input.GetMouseButtonDown(0))
            {
                Ray ray = _camera.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hit,  Mathf.Infinity))
                {
                    if (_oldValues != null)
                    {
                        int x = (int)(hit.textureCoord.x * resolution.x);
                        int y = (int)(hit.textureCoord.y * resolution.y);
                        _newValues[x,y] += 1f;
                    }
                }
            }
            _oldValues = _newValues.Clone() as float[,];
        }
        
        // void Update() OLD
        // {
        //     float dt = 0.025f;
        //     for (int i = 1; i < resolution.x-1; i++)
        //     for (int j = 1; j < resolution.y-1; j++)
        //     {
        //         float newHeight = c*c * dt*dt * ((_waterHeight1[i+1,j] - 2f*_waterHeight1[i,j] + _waterHeight1[i-1,j])/(_dx*_dx) + (_waterHeight1[i,j+1] - 2f*_waterHeight1[i,j] + _waterHeight1[i,j-1])/(_dy*_dy)) + 2f * _waterHeight1[i,j] - _waterHeight0[i,j];
        //         _waterHeight2[i, j] = newHeight * dampingFactor;
        //         _rippleTexture.SetPixel(i, j, new Color(newHeight, newHeight, newHeight, 1f));
        //     }
        //     _rippleTexture.Apply();
        //     
        //     _waterHeight0 = _waterHeight1.Clone() as float[,];
        //     _waterHeight1 = _waterHeight2.Clone() as float[,];
        //
        //     if (Input.GetMouseButtonDown(0))
        //     {
        //         Ray ray = _camera.ScreenPointToRay(Input.mousePosition);
        //         if (Physics.Raycast(ray, out RaycastHit hit,  Mathf.Infinity))
        //         {
        //             if (_waterHeight1 != null)
        //             {
        //                 int x = (int)(hit.textureCoord.x * resolution.x);
        //                 int y = (int)(hit.textureCoord.y * resolution.y);
        //                 _waterHeight1[x,y] = 1f;
        //             }
        //         }
        //     }
        // }
    }
}
