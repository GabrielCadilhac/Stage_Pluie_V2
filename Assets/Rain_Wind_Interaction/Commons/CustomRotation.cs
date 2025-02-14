using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[ExecuteInEditMode]
public class CustomRotation : MonoBehaviour
{
    [FormerlySerializedAs("_rotation")] [SerializeField] private Vector3 rotation;

    private Matrix4x4 _rotMat;

    private void Update()
    {
        Vector3 angles = rotation * Mathf.Deg2Rad; // In radians
        Vector3 c = new Vector3(Mathf.Cos(angles.x), Mathf.Cos(angles.y), Mathf.Cos(angles.z));
        Vector3 s = new Vector3(Mathf.Sin(angles.x), Mathf.Sin(angles.y), Mathf.Sin(angles.z));

        Matrix4x4 rx = new Matrix4x4(new Vector4(1f, 0f, 0f, 0f),
                                     new Vector4(0f, c.x, s.x, 0f),
                                     new Vector4(0f, -s.x, c.x, 0f),
                                     new Vector4(0f, 0f, 0f, 1f));

        Matrix4x4 ry = new Matrix4x4(new Vector4(c.y, 0f, s.y, 0f),
                                     new Vector4(0f, 1f, 0f, 0f),
                                     new Vector4(-s.y, 0f, c.y, 0f),
                                     new Vector4(0f, 0f, 0f, 1f));

        Matrix4x4 rz = new Matrix4x4(new Vector4(c.z, -s.z, 0f, 0f),
                                     new Vector4(s.z, c.z, 0f, 0f),
                                     new Vector4(0f, 0f, 1f, 0f),
                                     new Vector4(0f, 0f, 0f, 1f));
        _rotMat = rx * ry * rz;

        transform.rotation = _rotMat.rotation;
    }

    public Matrix4x4 GetRotation()
    {
        return _rotMat;
    }
}
