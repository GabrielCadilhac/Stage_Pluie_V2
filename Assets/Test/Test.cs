using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Test : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        Random.InitState(0);
        float[] test = new float[4];
        float total = 0f;
        for (int i = 0; i < test.Length; i++)
        {
            test[i] = Random.Range(0.0f, 1.0f);
            total += test[i];
        }

        float testTotal = 0f;
        for (int i = 0; i < test.Length; i++)
        {
            testTotal += test[i] / total;
        }

        Debug.Log(testTotal);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
