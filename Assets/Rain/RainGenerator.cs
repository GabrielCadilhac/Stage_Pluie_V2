using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class RainGenerator
{
    private ComputeBuffer _velBuffer, _windBuffer;
    private GraphicsBuffer _posBuffer;

    private float _deltaTime;
    private int _nbBlocks;

    private Vector3 _initVel = new Vector3(0f,-5f,0f);

    [SerializeField] private ComputeShader _updateShader, _collisionShader;

    public RainGenerator(ComputeShader p_updateShader,
                        ComputeShader p_collisionShader,
                        GraphicsBuffer p_posBuffer,
                        GraphicsBuffer p_splashPosBuffer,
                        ComputeBuffer p_timeSplash,
                        Bounds p_windGrid,
                        Transform p_transform,
                        float p_deltaTime = 1f,
                        int p_nbMaxParticles = 1000)
    {
        _posBuffer = p_posBuffer;
        _velBuffer  = new ComputeBuffer(p_nbMaxParticles, 3 * sizeof(float));
        _windBuffer = new ComputeBuffer(Common.NB_CELLS.x * Common.NB_CELLS.y * Common.NB_CELLS.z, 3 * sizeof(float));

        _deltaTime = p_deltaTime;
        _nbBlocks  = Mathf.Clamp(Mathf.FloorToInt((float) p_nbMaxParticles / (float) Constants.BLOCK_SIZE + 0.5f), 1, Constants.MAX_BLOCKS_NUMBER);

        Vector3 min = p_windGrid.center - p_windGrid.size / 2f;
        Vector3 max = p_windGrid.center + p_windGrid.size / 2f;

        Debug.Log(string.Format("Min {0} et max {1}", min, max));

        // Generate velocities
        Vector3[] tempVel = new Vector3[p_nbMaxParticles];
        for (int i = 0; i < p_nbMaxParticles; i++)
            tempVel[i] = new Vector3(0.0f, Random.Range(_initVel.y, _initVel.y+1f), 0f);
        _velBuffer.SetData(tempVel);

        // Init update Compute Shader
        _updateShader = p_updateShader;

        _updateShader.SetBuffer(0, "Positions", p_posBuffer);
        _updateShader.SetBuffer(0, "Velocities", _velBuffer);
        _updateShader.SetBuffer(0, "Winds", _windBuffer);

        _updateShader.SetInt("_NumParticles", p_nbMaxParticles);
        _updateShader.SetInt("_Resolution", _nbBlocks);
        _updateShader.SetFloat("_DeltaTime", _deltaTime * Time.deltaTime);
        _updateShader.SetFloat("_DropsCX", 0.42f);
        _updateShader.SetFloat("_DropDiam", 0.78f);
        _updateShader.SetVector("_Min", p_transform.InverseTransformPoint(min));
        _updateShader.SetVector("_NbCells", (Vector3) Common.NB_CELLS);
        _updateShader.SetVector("_WindGridSize", p_windGrid.size);

        // Init collision Compute Shader
        _collisionShader = p_collisionShader;

        _collisionShader.SetBuffer(0, "Positions",  p_posBuffer);
        _collisionShader.SetBuffer(0, "Velocities", _velBuffer);
        _collisionShader.SetBuffer(0, "SplashPos",  p_splashPosBuffer);
        _collisionShader.SetBuffer(0, "SplashTime", p_timeSplash);

        _collisionShader.SetInt("_NumParticles", p_nbMaxParticles);
        _collisionShader.SetInt("_Resolution", _nbBlocks);
        _collisionShader.SetVector("_InitialVel", _initVel);
        _collisionShader.SetVector("_Min", p_transform.InverseTransformPoint(min));
        _collisionShader.SetVector("_Max", p_transform.InverseTransformPoint(max));
    }

    public void Dispatch()
    {
        _updateShader.Dispatch(0, _nbBlocks, 1, 1);
        _collisionShader.Dispatch(0, _nbBlocks, 1, 1);
    }

    public void SetWinds(Vector3[] p_winds)
    {
        if (p_winds != null)
        {
            _windBuffer.SetData(p_winds);
            _updateShader.SetBuffer(0, "Winds", _windBuffer);
        }
    }

    public void SetDeltaTime(float p_deltaTime)
    {
        _deltaTime = p_deltaTime;
        _updateShader.SetFloat("_DeltaTime", p_deltaTime * Time.deltaTime);
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
    }
}