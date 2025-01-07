using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CustomRotation : MonoBehaviour
{
    [SerializeField] private Vector3 _rotation;

    private void Update()
    {
        transform.rotation = Quaternion.Euler(_rotation);
    }

    public Vector3 GetRotation()
    {
        return _rotation;
    }
}
