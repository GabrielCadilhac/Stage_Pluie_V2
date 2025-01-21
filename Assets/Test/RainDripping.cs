using System;
using UnityEngine;

public class RainDripping
{
    private const int NB_DROPS = 200;
    private Vector3 GRAVITY = new Vector3(0f, -9.81f, 0f);

    private float _dripFallSpeed = 2f;

    private GraphicsBuffer _dripsPosBuffer, _drawTestBuffer;
    private Bounds _bounds;
    private Material _material;

    private Vector3[] _dripPos;
    private int[] _existDrops;

    private ComputeBuffer _dripVelBuffer;
    private ComputeShader _dripsShader;

    private int _nbBlocks;

    public RainDripping(Transform p_transform, Material p_material, ComputeShader p_dripComputeShader)
    {
        _dripsShader = p_dripComputeShader;

        // Create buffers
        _bounds         = new Bounds(p_transform.position, new Vector3(10f, 10f, 10f));
        _dripsPosBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Raw, NB_DROPS, 3 * sizeof(float));
        _drawTestBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Raw, NB_DROPS, sizeof(int));
        _dripVelBuffer  = new ComputeBuffer(NB_DROPS, 3 * sizeof(float));

        // Init buffers
        _dripPos = new Vector3[NB_DROPS];
        _existDrops = new int[NB_DROPS];
        Vector3[] dropsVel = new Vector3[NB_DROPS];
        for (int i = 0; i < NB_DROPS; i++)
        {
            _existDrops[i] = 0;
            dropsVel[i] = Vector3.zero;
        }
        _drawTestBuffer.SetData(_existDrops);
        _dripVelBuffer.SetData(dropsVel);

        // Init drip material
        _material = p_material;
        _material.SetBuffer("Positions", _dripsPosBuffer);
        _material.SetBuffer("CanBeDrawn", _drawTestBuffer);

        _nbBlocks = Mathf.Clamp(Mathf.FloorToInt((float)NB_DROPS / (float)Constants.BLOCK_SIZE + 0.5f), 1, Constants.MAX_BLOCKS_NUMBER);

        // Init drip shader
        _dripsShader.SetBuffer(0, "Positions", _dripsPosBuffer);
        _dripsShader.SetBuffer(0, "Velocities", _dripVelBuffer);
        _dripsShader.SetBuffer(0, "CanBeDrawn", _drawTestBuffer);

        _dripsShader.SetInt("_NumParticles", NB_DROPS);
        _dripsShader.SetInt("_Resolution", _nbBlocks);
        _dripsShader.SetFloat("_DeltaTime", Time.deltaTime * _dripFallSpeed);
        _dripsShader.SetVector("_Min", _bounds.min);
        _dripsShader.SetVector("_Max", _bounds.max);
        _dripsShader.SetVector("_Gravity", GRAVITY);
    }

    public void Draw(Transform p_transform)
    {
        _bounds.center = p_transform.position;

        // Update drip position
        _dripsShader.Dispatch(0, _nbBlocks, 1, 1);

        // Render drip material
        RenderParams rp = new RenderParams(_material);
        rp.worldBounds = new Bounds(p_transform.position, _bounds.size);
        rp.matProps = new MaterialPropertyBlock();
        Graphics.RenderPrimitives(rp, MeshTopology.Quads, 4, NB_DROPS);
    }

    // Add drips to the system
    public void GenerateDripping(Transform p_transform, Vector3 p_pos, float p_size)
    {
        // Compute the drip initial position
        Vector3 origin = p_transform.position - (p_transform.localScale.x / 2f) * (p_transform.up + p_transform.right);

        float cellSize = p_transform.localScale.x / p_size;
        Vector3 halfSize = (p_transform.up + p_transform.right) * cellSize / 2f;

        Vector3 offset = p_pos.x * cellSize * p_transform.right + p_pos.y * cellSize * p_transform.up;

        // Add a new drips by searching free space in _existDrops
        int i = 0;
        while (i < NB_DROPS && _existDrops[i] == 1)
            i++;

        if (i < NB_DROPS)
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
