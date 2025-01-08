using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CustomRotation : MonoBehaviour
{
    [SerializeField] private Vector3 _rotation;

    private Matrix4x4 _rotMat;

    private void Update()
    {
        Vector3 angles = _rotation * Mathf.Deg2Rad; // In radians
        Vector3 c = new Vector3(Mathf.Cos(angles.x), Mathf.Cos(angles.y), Mathf.Cos(angles.z));
        Vector3 s = new Vector3(Mathf.Sin(angles.x), Mathf.Sin(angles.y), Mathf.Sin(angles.z));

        Matrix4x4 Rx = new Matrix4x4(new Vector4(1f, 0f, 0f, 0f),
                                     new Vector4(0f, c.x, s.x, 0f),
                                     new Vector4(0f, -s.x, c.x, 0f),
                                     new Vector4(0f, 0f, 0f, 1f));

        Matrix4x4 Ry = new Matrix4x4(new Vector4(c.y, 0f, s.y, 0f),
                                     new Vector4(0f, 1f, 0f, 0f),
                                     new Vector4(-s.y, 0f, c.y, 0f),
                                     new Vector4(0f, 0f, 0f, 1f));

        Matrix4x4 Rz = new Matrix4x4(new Vector4(c.z, -s.z, 0f, 0f),
                                     new Vector4(s.z, c.z, 0f, 0f),
                                     new Vector4(0f, 0f, 1f, 0f),
                                     new Vector4(0f, 0f, 0f, 1f));
        _rotMat = Rx * Ry * Rz;

        transform.rotation = _rotMat.rotation;
    }

    public Matrix4x4 GetRotation()
    {
        return _rotMat;
    }
}
