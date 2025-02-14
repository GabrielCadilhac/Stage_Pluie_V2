using UnityEngine;

namespace Test
{
    public class RainDripping
    {
        private const int NbDrops = 200;
        private Vector3 _gravity = new Vector3(0f, -9.81f, 0f);

        private float _dripFallSpeed = 2f;

        private GraphicsBuffer _dripsPosBuffer, _drawTestBuffer;
        private Bounds _bounds;
        private Material _material;

        private Vector3[] _dripPos;
        private int[] _existDrops;

        private ComputeBuffer _dripVelBuffer;
        private ComputeShader _dripsShader;

        private int _nbBlocks;

        public RainDripping(Transform pTransform, Material pMaterial, ComputeShader pDripComputeShader)
        {
            _dripsShader = pDripComputeShader;

            // Create buffers
            _bounds         = new Bounds(pTransform.position, new Vector3(10f, 10f, 10f));
            _dripsPosBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Raw, NbDrops, 3 * sizeof(float));
            _drawTestBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Raw, NbDrops, sizeof(int));
            _dripVelBuffer  = new ComputeBuffer(NbDrops, 3 * sizeof(float));

            // Init buffers
            _dripPos = new Vector3[NbDrops];
            _existDrops = new int[NbDrops];
            Vector3[] dropsVel = new Vector3[NbDrops];
            for (int i = 0; i < NbDrops; i++)
            {
                _existDrops[i] = 0;
                dropsVel[i] = Vector3.zero;
            }
            _drawTestBuffer.SetData(_existDrops);
            _dripVelBuffer.SetData(dropsVel);

            // Init drip material
            _material = pMaterial;
            _material.SetBuffer("Positions", _dripsPosBuffer);
            _material.SetBuffer("CanBeDrawn", _drawTestBuffer);

            _nbBlocks = Mathf.Clamp(Mathf.FloorToInt((float)NbDrops / (float)Constants.BlockSize + 0.5f), 1, Constants.MaxBlocksNumber);

            // Init drip shader
            _dripsShader.SetBuffer(0, "Positions", _dripsPosBuffer);
            _dripsShader.SetBuffer(0, "Velocities", _dripVelBuffer);
            _dripsShader.SetBuffer(0, "CanBeDrawn", _drawTestBuffer);

            _dripsShader.SetInt("_NumParticles", NbDrops);
            _dripsShader.SetInt("_Resolution", _nbBlocks);
            _dripsShader.SetFloat("_DeltaTime", Time.deltaTime * _dripFallSpeed);
            _dripsShader.SetVector("_Min", _bounds.min);
            _dripsShader.SetVector("_Max", _bounds.max);
            _dripsShader.SetVector("_Gravity", _gravity);
        }

        public void Draw(Transform pTransform)
        {
            _bounds.center = pTransform.position;

            // Update drip position
            _dripsShader.Dispatch(0, _nbBlocks, 1, 1);

            // Render drip material
            RenderParams rp = new RenderParams(_material);
            rp.worldBounds = new Bounds(pTransform.position, _bounds.size);
            rp.matProps = new MaterialPropertyBlock();
            Graphics.RenderPrimitives(rp, MeshTopology.Quads, 4, NbDrops);
        }

        // Add drips to the system
        public void GenerateDripping(Transform pTransform, Vector3 pPos, float pSize)
        {
            // Compute the drip initial position
            Vector3 origin = pTransform.position - (pTransform.localScale.x / 2f) * (pTransform.up + pTransform.right);

            float cellSize = pTransform.localScale.x / pSize;
            Vector3 halfSize = (pTransform.up + pTransform.right) * cellSize / 2f;

            Vector3 offset = pPos.x * cellSize * pTransform.right + pPos.y * cellSize * pTransform.up;

            // Add a new drips by searching free space in _existDrops
            int i = 0;
            while (i < NbDrops && _existDrops[i] == 1)
                i++;

            if (i < NbDrops)
            {
                _dripsPosBuffer.GetData(_dripPos);
                _drawTestBuffer.GetData(_existDrops);
            
                _dripPos[i] = origin + halfSize + offset;
                _existDrops[i] = 1;

                _drawTestBuffer.SetData(_existDrops);
                _dripsPosBuffer.SetData(_dripPos);
            }
        }

        public void OnDisable()
        {
            _dripsPosBuffer.Release();
            _dripsPosBuffer = null;

            _dripVelBuffer.Release();
            _dripVelBuffer = null;

            _drawTestBuffer.Release();
            _drawTestBuffer = null;
        }
    }
}
