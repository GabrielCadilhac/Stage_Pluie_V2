using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DestroyObject : MonoBehaviour
{
    public void OnEnable()
    {
        Debug.Log("Sphere supprim�e !");
        Destroy(gameObject);
    }
}
