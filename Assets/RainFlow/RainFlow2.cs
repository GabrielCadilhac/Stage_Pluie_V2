using UnityEngine;
using Unity.Mathematics;

namespace RainFlow
{
    public class RainFlow2 : MonoBehaviour
    {
        private readonly Vector3 ExternalForce = new Vector3(0f, -9.81f, 0f);
        private const float Alpha = 0.99f;

        [SerializeField] private float deltaTime;
        [SerializeField] private float textureScale = 1f;
        [SerializeField] private new Camera camera;
        [SerializeField] private float bumpThreshold;
        
        private static readonly int NormalMap = Shader.PropertyToID("_BumpMap");
        
        private Material _material;
        private Texture2D _normalMap, _mainTexture;

        private Vector3 _partVel, _partPos;
        private float _mass  = 1f;

        private GameObject _sphereTest;
        private float[,] _wetness;
        
        void Start()
        {
            _material = GetComponent<Renderer>().material;
            _normalMap = _material.GetTexture(NormalMap) as Texture2D;
        
            if (_normalMap == null)
            {
                Debug.LogError("Normal map is not defined.");
                return;
            }

            _wetness = new float[_normalMap.width, _normalMap.height];
            
            _mainTexture = new Texture2D(_normalMap.width, _normalMap.height, TextureFormat.RGBA32, false);
            _material.mainTexture = _mainTexture;
            
            _sphereTest = GameObject.Find("Sphere");
            
            _partVel = Vector3.zero;
            _partPos = _sphereTest.transform.position;
        }

        private void DrawWetness()
        {
            for (int i = 0; i < _normalMap.width; i++)
            for (int j = 0; j < _normalMap.height; j++) 
            {
                float wet = 1f - _wetness[i, j];
                 _mainTexture.SetPixel(i, j, new Vector4(wet,wet,wet,1));
                //Vector4 n = _normalMap.GetPixel(i, j);
                //_mainTexture.SetPixel(i, j, n);
            }
            _mainTexture.Apply();
        }

        private void UpdateSphere()
        {
            Vector3 globalNormal = transform.up.normalized;
            Vector3 extForce = ExternalForce;

            Vector3 pos = Vector3.one - (_sphereTest.transform.position * 0.1f + Vector3.one * 0.5f);

            int x = (int) (pos.x * _normalMap.width);
            int y = (int) (pos.z * _normalMap.height);

            if (x < 0 || x >= _normalMap.width || y < 0 || y >= _normalMap.height)
                return;

            Vector4 temp = _normalMap.GetPixel(x, y);
            Vector3 localNormal = new Vector3(temp.x, temp.y, temp.z).normalized * 2f - Vector3.one;

            localNormal = Vector3.Dot(localNormal, globalNormal) > 0f ? localNormal : -localNormal;  
            
            // Debug.DrawLine(_partPos, _partPos + localNormal, Color.red, 0.01f);
            if (Mathf.Abs(Vector3.Dot(localNormal, _partVel.normalized)) < bumpThreshold || _mass <= 0f)
                return;

            float cosTheta = Vector3.Dot(localNormal, extForce);
            Vector3 dp = extForce - localNormal * cosTheta;
            Debug.DrawLine(_partPos, _partPos + extForce, Color.red, 0.01f);
            Debug.DrawLine(_partPos, _partPos + localNormal, Color.green, 0.01f);
            
            Vector3 intForce = -Alpha * dp;

            Vector3 tangent = extForce - globalNormal * Vector3.Dot(globalNormal, extForce);
            Vector3 bitangent = Vector3.Cross(tangent, globalNormal);
            
            Vector3 accP = Vector3.Dot(intForce + extForce, dp) / _mass * dp;
            _partVel += accP * (Time.deltaTime * deltaTime);
            Vector3 vp = tangent * Vector3.Dot(tangent, _partVel)  + bitangent * Vector3.Dot(bitangent, _partVel);
            _partPos += vp * (Time.deltaTime * deltaTime);

            _mass = 0.8f * _mass;
            Debug.Log(_mass);
            
            Debug.DrawLine(_partPos, _partPos + dp.normalized, Color.blue, 0.01f);
            int range = 2;
            for (int i = x - range; i <= x + range; i++)
            for (int j = y - range; j <= y + range; j++)
            {
                if (i >= 0 && i < _normalMap.width && j >= 0 && j < _normalMap.height)
                    _wetness[i, j] = 1f;
            }
        }
        
        private static float H(float pX)
        {
            return pX >= 0.4f ? 0.1f : math.log(math.sqrt(pX) + 1f) / 2.17f;
        }

        void Update()
        {
            if (Input.GetMouseButton(0))
            {
                Ray ray = camera.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    _partPos = hit.point;
                    _partVel = Vector3.zero;
                }
            }
            
            UpdateSphere();
            _sphereTest.transform.position = _partPos;
            DrawWetness();
            
            // for (int i = 0; i <= _normalMap.width; i+=10)
            // for (int j = 0; j <= _normalMap.height; j+=10)
            // {
            //     Vector4 t = _normalMap.GetPixel(i, j);
            //     Vector3 n = new Vector3(t.x, t.y, t.z).normalized * 2f - Vector3.one;
            //     
            //     float x = 10f * i / _normalMap.width;
            //     float z = 10f * j / _normalMap.height;
            //     Vector3 pos = new Vector3(x - 5f, 0f, z - 5f);
            //     Debug.DrawLine(pos, pos + n*0.5f, Color.red, 0.01f);
            // }
        }
    }
}
