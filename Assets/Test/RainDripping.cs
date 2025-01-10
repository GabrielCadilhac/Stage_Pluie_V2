using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class RainDripping
{
    private const int NB_DROPS = 100;

    private float _dripFallSpeed = 2f;    

    private GameObject _testCube;
    private Transform _transform;

    private GraphicsBuffer _dripsBuffer;
    private Bounds _bounds;
    private Material _material;

    private List<Vector3> _dripPos;

    // Start is called before the first frame update
    public RainDripping(GameObject p_testCube, Transform p_transform, Material p_material)
    {
        _testCube = p_testCube;
        _transform = p_transform;

        _bounds = new Bounds(new Vector3(0f, 0f, 0f), new Vector3(100f, 100f, 100f));
        _dripsBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Raw, NB_DROPS, 3 * sizeof(float));

        _material = p_material;
        _material.SetBuffer("Positions", _dripsBuffer);
        
        _dripPos = new List<Vector3>();
    }

    public void Draw()
    {
        if (_dripPos.Count == 0)
            return;

        _material.SetInt("_DripsCount", _dripPos.Count);

        RenderParams rp = new RenderParams(_material);
        rp.worldBounds = new Bounds(_transform.position, _bounds.size);
        rp.matProps = new MaterialPropertyBlock();
        Graphics.RenderPrimitives(rp, MeshTopology.Quads, 4, NB_DROPS);

        UpdateDrips();
    }

    public void GenerateDripping(Vector3 p_pos, float p_size)
    {
        Vector3 origin = _transform.position - (_transform.localScale.x / 2f) * (_transform.up + _transform.right);

        float cellSize = _transform.localScale.x / p_size;
        Vector3 halfSize = (_transform.up + _transform.right) * cellSize / 2f;

        Vector3 offset = p_pos.x * cellSize * _transform.right + p_pos.y * cellSize * _transform.up;
        //_testCube.transform.position = origin + halfSize + offset;

        _dripPos.Add(origin + halfSize + offset);
        _dripsBuffer.SetData(_dripPos.ToArray());
    }

    private void UpdateDrips()
    {
        for (int i = 0; i < _dripPos.Count; i++)
        {
            _dripPos[i] += Vector3.down * _dripFallSpeed * Time.deltaTime;
        }
        _dripsBuffer.SetData(_dripPos.ToArray());
    }

    public void OnDisable()
    {
        _dripsBuffer.Dispose();
        _dripsBuffer = null;
    }
}
