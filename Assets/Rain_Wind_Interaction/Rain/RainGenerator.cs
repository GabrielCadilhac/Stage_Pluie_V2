using UnityEngine;

public class RainGenerator
{
    //private ComputeBuffer _localWindBuffer;
    private ComputeBuffer _velBuffer, _sizeBuffer;
    private ComputeBuffer  _windBuffer;
    private GraphicsBuffer _posBuffer;

    private int _nbBlocks;

    private Vector3 _initVel = new Vector3(0f, -5f, 0f);

    private ComputeShader _updateShader, _collisionShader;
    private ComputeBuffer _obbsBuffer;

    public OBB[] _obbs;
    private GameObject[] _obbsGameObject;

    public RainGenerator(ComputeShader p_updateShader,
                         ComputeShader p_collisionShader,
                         GraphicsBuffer p_posBuffer,
                         GraphicsBuffer p_splashPosBuffer,
                         GraphicsBuffer p_splashNormalBuffer,
                         ComputeBuffer p_windBuffer,
                         GameObject[] p_obbsGameObject,
                         Bounds p_windGrid,
                         Vector3 p_globalMin,
                         Vector3 p_globalMax)
    {
        _posBuffer  = p_posBuffer;
        _velBuffer  = new ComputeBuffer(RainManager._nbParticles, 3 * sizeof(float));
        //_localWindBuffer = new ComputeBuffer(Common.NB_CELLS.x * Common.NB_CELLS.y * Common.NB_CELLS.z, 3 * sizeof(float));
        _sizeBuffer = new ComputeBuffer(RainManager._nbParticles, sizeof(float));

        _nbBlocks  = Mathf.Clamp(Mathf.FloorToInt((float)RainManager._nbParticles / (float) Constants.BLOCK_SIZE + 0.5f), 1, Constants.MAX_BLOCKS_NUMBER);

        _obbsGameObject = p_obbsGameObject;
        _obbsBuffer = new ComputeBuffer(_obbsGameObject.Length, 22 * sizeof(float));
        _obbsBuffer.SetData(GameObject2OBB(_obbsGameObject));

        // Generate velocities
        Vector3[] tempVel = new Vector3[RainManager._nbParticles];
        float[] tempSize = new float[RainManager._nbParticles];
        for (int i = 0; i < RainManager._nbParticles; i++)
        {
            // Calcul des diamètres
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
        _updateShader = p_updateShader;
        _windBuffer = p_windBuffer;

        _updateShader.SetBuffer(0, "Positions", p_posBuffer);
        _updateShader.SetBuffer(0, "Velocities", _velBuffer);
        _updateShader.SetBuffer(0, "Winds", _windBuffer);
        _updateShader.SetBuffer(0, "Sizes", _sizeBuffer);

        _updateShader.SetInt("_NumParticles", RainManager._nbParticles);
        _updateShader.SetInt("_Resolution", _nbBlocks);
        _updateShader.SetFloat("_DeltaTime", 0f);
        _updateShader.SetVector("_Min", p_globalMin);
        _updateShader.SetVector("_NbCells", (Vector3) Common.NB_CELLS);
        _updateShader.SetVector("_WindGridSize", p_windGrid.size);

        // Init collision Compute Shader
        _collisionShader = p_collisionShader;

        _collisionShader.SetBuffer(0, "Positions",  p_posBuffer);
        _collisionShader.SetBuffer(0, "Velocities", _velBuffer);
        _collisionShader.SetBuffer(0, "Sizes", _sizeBuffer);
        _collisionShader.SetBuffer(0, "SplashPos",  p_splashPosBuffer);
        _collisionShader.SetBuffer(0, "SplashNormal", p_splashNormalBuffer);
        _collisionShader.SetBuffer(0, "Obbs", _obbsBuffer);

        _collisionShader.SetInt("_NumParticles", RainManager._nbParticles);
        _collisionShader.SetInt("_NumObbs", _obbsGameObject.Length);
        _collisionShader.SetInt("_Resolution", _nbBlocks);
        _collisionShader.SetVector("_InitialVel", _initVel);
        _collisionShader.SetVector("_Min", p_globalMin);
        _collisionShader.SetVector("_Max", p_globalMax);
    }

    private OBB[] GameObject2OBB(GameObject[] p_obbsGameObject)
    {
        OBB[] oBBs = new OBB[p_obbsGameObject.Length];
        for (int i = 0; i < oBBs.Length; i++)
        {
            GameObject go = p_obbsGameObject[i];

            // Define the OBB
            OBB oBB = new OBB();
            oBB.rotation = go.GetComponent<CustomRotation>().GetRotation();
            oBB.center   = go.transform.position;
            oBB.size     = go.transform.localScale / 2f;

            oBBs[i] = oBB;
        }
        _obbs = oBBs;

        return oBBs;
    }

    public void UpdateBoxBounds(Vector3 p_globalMin, Vector3 p_globalMax)
    {
        _collisionShader.SetVector("_Min", p_globalMin);
        _collisionShader.SetVector("_Max", p_globalMax);
    }

    public void DispatchUpdate()
    {
        _updateShader.Dispatch(0, _nbBlocks, 1, 1);
    }

    public void DispatchCollision(float p_deltaTime)
    {
        _obbsBuffer.SetData(GameObject2OBB(_obbsGameObject));
        _updateShader.SetFloat("_DeltaTime", p_deltaTime);
        _collisionShader.Dispatch(0, _nbBlocks, 1, 1);
    }

    public ComputeBuffer GetVelBuffer()
    {
        return _velBuffer;
    }

    public ComputeBuffer GetSizeBuffer()
    {
        return _sizeBuffer;
    }

    //public void SetWinds(Vector3[] p_winds)
    //{
    //    if (p_winds != null)
    //    {
    //        _localWindBuffer.SetData(p_winds);
    //        _updateShader.SetBuffer(0, "Winds", _localWindBuffer);
    //    }
    //}

    public void ChangeGlobalWind()
    {
        _collisionShader.SetVector("_GlobalWind", Constants.GLOBAL_WIND);
        _updateShader.SetVector("_GlobalWind", Constants.GLOBAL_WIND);
    }

    public void ResetParticles(int p_nbParticles)
    {
        Vector3[] newVel = new Vector3[p_nbParticles];
        for (int i = 0; i < p_nbParticles; i++)
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
    }
}