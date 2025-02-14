using Drop_Impact;
using RainFlow;
using Test;
using UnityEngine;

public class RainGenerator
{
    //private ComputeBuffer _localWindBuffer;
    private ComputeBuffer _velBuffer, _sizeBuffer;
    private ComputeBuffer  _windBuffer, _splashColBuffer;
    private GraphicsBuffer _posBuffer;

    private int _nbBlocks;

    private Vector3 _initVel = new Vector3(0f, -5f, 0f);

    private ComputeShader _updateShader, _collisionShader;
    private ComputeBuffer _obbsBuffer, _obbsCollidedBuffer;

    public Obb[] Obbs;
    private GameObject[] _obbsGameObject;

    private int[] _collisionsId;
    private Vector4[] _splashPos;

    public RainGenerator(ComputeShader pUpdateShader,
                         ComputeShader pCollisionShader,
                         GraphicsBuffer pPosBuffer,
                         RainImpact pRainImpact,
                         ComputeBuffer pWindBuffer,
                         GameObject[] pObbsGameObject,
                         Bounds pWindGrid,
                         Vector3 pGlobalMin,
                         Vector3 pGlobalMax)
    {
        _posBuffer  = pPosBuffer;
        _velBuffer  = new ComputeBuffer(RainManager.NbParticles, 3 * sizeof(float));
        //_localWindBuffer = new ComputeBuffer(Common.NB_CELLS.x * Common.NB_CELLS.y * Common.NB_CELLS.z, 3 * sizeof(float));
        _sizeBuffer = new ComputeBuffer(RainManager.NbParticles, sizeof(float));

        _obbsCollidedBuffer = new ComputeBuffer(RainManager.NbParticles, sizeof(int));
        int[] obbsCollided = new int[RainManager.NbParticles];
        for (int i = 0; i < RainManager.NbParticles; i++)
            obbsCollided[i] = -1;
        _obbsCollidedBuffer.SetData(obbsCollided);

        _collisionsId = new int[RainManager.NbParticles];
        _splashPos = new Vector4[RainManager.NbParticles];

        _nbBlocks = Mathf.Clamp(Mathf.FloorToInt((float)RainManager.NbParticles / (float) Constants.BlockSize + 0.5f), 1, Constants.MaxBlocksNumber);

        _splashColBuffer = pRainImpact.GetPosBuffer();

        _obbsGameObject = pObbsGameObject;
        _obbsBuffer = new ComputeBuffer(_obbsGameObject.Length, 22 * sizeof(float));

        Obbs = new Obb[_obbsGameObject.Length];
        GameObject2Obb(_obbsGameObject);
        _obbsBuffer.SetData(Obbs);

        // Generate velocities
        Vector3[] tempVel = new Vector3[RainManager.NbParticles];
        float[] tempSize = new float[RainManager.NbParticles];
        for (int i = 0; i < RainManager.NbParticles; i++)
        {
            // Calcul des diam�tres
            float dmin = 0.5f;
            float I = 20f;
            float lamb = 4.1f * Mathf.Pow(I, -0.21f);
            tempSize[i] = dmin - Mathf.Log(Random.Range(0f, 1f)) / lamb;

            // Calcul des vitesses
            tempVel[i]  = new Vector3(0f, -9.65f + 10.3f * Mathf.Exp(-0.6f * tempSize[i]), 0f);
        }

        _velBuffer.SetData(tempVel);
        _sizeBuffer.SetData(tempSize);

        // Init update Compute Shader
        _updateShader = pUpdateShader;
        _windBuffer   = pWindBuffer;

        _updateShader.SetBuffer(0, "Positions", pPosBuffer);
        _updateShader.SetBuffer(0, "Velocities", _velBuffer);
        _updateShader.SetBuffer(0, "Winds", _windBuffer);
        _updateShader.SetBuffer(0, "Sizes", _sizeBuffer);

        _updateShader.SetInt("_NumParticles", RainManager.NbParticles);
        _updateShader.SetInt("_Resolution", _nbBlocks);
        _updateShader.SetFloat("_DeltaTime", 0f);
        _updateShader.SetVector("_Min", pGlobalMin);
        _updateShader.SetVector("_NbCells", (Vector3) Common.NbCells);
        _updateShader.SetVector("_WindGridSize", pWindGrid.size);

        // Init collision Compute Shader
        _collisionShader = pCollisionShader;

        _collisionShader.SetBuffer(0, "Positions",  pPosBuffer);
        _collisionShader.SetBuffer(0, "Velocities", _velBuffer);
        _collisionShader.SetBuffer(0, "Sizes", _sizeBuffer);
        
        _collisionShader.SetBuffer(0, "SplashPos",  _splashColBuffer);
        _collisionShader.SetBuffer(0, "SplashNormal", pRainImpact.GetNormalBuffer());
        _collisionShader.SetBuffer(0, "SplashTimes", pRainImpact.GetTimesBuffer());
        
        _collisionShader.SetBuffer(0, "Obbs", _obbsBuffer);
        _collisionShader.SetBuffer(0, "ObbsCollided", _obbsCollidedBuffer);

        _collisionShader.SetInt("_NumParticles", RainManager.NbParticles);
        _collisionShader.SetInt("_NumObbs", _obbsGameObject.Length);
        _collisionShader.SetInt("_Resolution", _nbBlocks);
        _collisionShader.SetVector("_InitialVel", _initVel);
        _collisionShader.SetVector("_Min", pGlobalMin);
        _collisionShader.SetVector("_Max", pGlobalMax);
    }

    private void GameObject2Obb(GameObject[] pObbsGameObject)
    {
        for (int i = 0; i < Obbs.Length; i++)
        {
            GameObject go = pObbsGameObject[i];

            // Define the OBB
            Obb oBb = new Obb();
            oBb.Rotation = go.GetComponent<CustomRotation>().GetRotation();
            oBb.Center   = go.transform.position;
            oBb.Size     = go.transform.localScale / 2f;

            Obbs[i] = oBb;
        }
    }

    public void UpdateBoxBounds(Vector3 pGlobalMin, Vector3 pGlobalMax)
    {
        _collisionShader.SetVector("_Min", pGlobalMin);
        _collisionShader.SetVector("_Max", pGlobalMax);
    }

    public void DispatchUpdate()
    {
        _updateShader.Dispatch(0, _nbBlocks, 1, 1);
    }

    public void DispatchCollision(float pDeltaTime)
    {
        GameObject2Obb(_obbsGameObject);
        _obbsBuffer.SetData(Obbs);
        _updateShader.SetFloat("_DeltaTime", pDeltaTime);
        _collisionShader.Dispatch(0, _nbBlocks, 1, 1);

        _obbsCollidedBuffer.GetData(_collisionsId);
        _splashColBuffer.GetData(_splashPos);
        for (int k = 0; k < RainManager.NbParticles; k++)
        {
            int col = _collisionsId[k];
            RainFlowMaps rainFlowMaps = col > -1 ? _obbsGameObject[col].GetComponent<RainFlowMaps>() : null;
            if (col > -1 && rainFlowMaps != null)
            {
                Obb obb = Obbs[col];
                Matrix4x4 rot = obb.Rotation;
                Vector4 t1 = rot.GetColumn(0);
                Vector4 t2 = rot.GetColumn(1);
                Vector4 t3 = rot.GetColumn(2);

                Vector3 e1 = new Vector3(t1.x, t1.y, t1.z);
                Vector3 e2 = new Vector3(t2.x, t2.y, t2.z);
                Vector3 n  = new Vector3(t3.x, t3.y, t3.z);

                Vector4 sPos = _splashPos[k];
                Vector3 point = new Vector3(sPos.x, sPos.y, sPos.z);
                Vector3 localPoint = point - obb.Center;
                localPoint -= Vector3.Dot(n, localPoint) * n;

                float u = Vector3.Dot(localPoint, e1.normalized) / obb.Size.x;
                float v = Vector3.Dot(localPoint, e2.normalized) / obb.Size.y;

                float i = Mathf.Clamp01(u * 0.5f + 0.5f) * (float)(RainFlowMaps.Size);
                float j = Mathf.Clamp01(v * 0.5f + 0.5f) * (float)(RainFlowMaps.Size);

                i = Mathf.Clamp(i, 0f, (float)RainFlowMaps.Size - 1f);
                j = Mathf.Clamp(j, 0f, (float)RainFlowMaps.Size - 1f);

                rainFlowMaps.AddDrop((int)i, (int)j);
            }
        }
    }

    public ComputeBuffer GetVelBuffer()
    {
        return _velBuffer;
    }

    public ComputeBuffer GetSizeBuffer()
    {
        return _sizeBuffer;
    }

    public void ChangeGlobalWind()
    {
        _collisionShader.SetVector("_GlobalWind", Constants.GlobalWind);
        _updateShader.SetVector("_GlobalWind", Constants.GlobalWind);
    }

    public void ResetParticles(int pNbParticles)
    {
        Vector3[] newVel = new Vector3[pNbParticles];
        for (int i = 0; i < pNbParticles; i++)
            newVel[i] = new Vector3(0.0f, Random.Range(_initVel.y, _initVel.y + 1f), 0f);

        _velBuffer.SetData(newVel);
    }

    public void Disable()
    {
        _posBuffer.Release();
        _posBuffer = null;

        _velBuffer.Release();
        _velBuffer  = null;

        _windBuffer.Release();
        _windBuffer = null;

        _sizeBuffer.Release();
        _sizeBuffer = null;

        _obbsBuffer.Release();
        _obbsBuffer = null;

        _splashColBuffer.Release();
        _splashColBuffer = null;

        _obbsCollidedBuffer.Release();
        _obbsCollidedBuffer = null;
    }
}