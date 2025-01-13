using System.Collections.Generic;
using UnityEngine;

public class RainDripping
{
    private const int NB_DROPS = 100;

    private float _dripFallSpeed = 2f;    

    private Transform p_transform;

    private GraphicsBuffer _dripsBuffer;
    private Bounds _bounds;
    private Material _material;

    private List<Vector3> _dripPos;

    public RainDripping(Transform p_transform, Material p_material)
    {
        this.p_transform = p_transform;

        _bounds      = new Bounds(p_transform.position, new Vector3(10f, 10f, 10f));
        _dripsBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Raw, NB_DROPS, 3 * sizeof(float));

        _material = p_material;
        _material.SetBuffer("Positions", _dripsBuffer);
        _dripPos = new List<Vector3>();
    }

    public void Draw(Transform p_transform)
    {
        if (_dripPos.Count == 0)
            return;

        _bounds.center = p_transform.position;

        _material.SetInt("_DripsCount", _dripPos.Count);

        RenderParams rp = new RenderParams(_material);
        rp.worldBounds = new Bounds(p_transform.position, _bounds.size);
        rp.matProps = new MaterialPropertyBlock();
        Graphics.RenderPrimitives(rp, MeshTopology.Quads, 4, NB_DROPS);

        UpdateDrips();
    }

    public void GenerateDripping(Transform p_transform, Vector3 p_pos, float p_size)
    {
        Vector3 origin = p_transform.position - (p_transform.localScale.x / 2f) * (p_transform.up + p_transform.right);

        float cellSize = p_transform.localScale.x / p_size;
        Vector3 halfSize = (p_transform.up + p_transform.right) * cellSize / 2f;

        Vector3 offset = p_pos.x * cellSize * p_transform.right + p_pos.y * cellSize * p_transform.up;

        _dripPos.Add(origin + halfSize + offset);
        _dripsBuffer.SetData(_dripPos.ToArray());
    }

    private void UpdateDrips()
    {
        List<int> dripToDel = new List<int>();
        for (int i = 0; i < _dripPos.Count; i++)
        {
            _dripPos[i] += Vector3.down * _dripFallSpeed * Time.deltaTime;

            if (_dripPos[i].y < _bounds.min.y)
                dripToDel.Add(i);
        }

        // Compute collision with the ground / bounds
        for (int i = 0; i < dripToDel.Count; i++)
            _dripPos.RemoveAt(dripToDel[i]);

        _dripsBuffer.SetData(_dripPos.ToArray());
    }

    public void OnDisable()
    {
        _dripsBuffer.Dispose();
        _dripsBuffer = null;
    }
}
