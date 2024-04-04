using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Hodograph : MonoBehaviour
{
    [SerializeField] GameObject _canvas;
    LineRenderer _lineRenderer;

    public void Start()
    {
        GameObject line = new GameObject();
        line.name = "Line";
        line.transform.parent = _canvas.transform;
        _lineRenderer = line.AddComponent<LineRenderer>();
        _lineRenderer.positionCount = _canvas.transform.childCount;
        //_lineRenderer.useWorldSpace = false;
    }

    public void Update()
    {

        for (int i = 0; i < _canvas.transform.childCount; i++)
        {
            Vector3 pos = _canvas.transform.GetChild(i).transform.position;
            _lineRenderer.SetPosition(i, pos);
        }
    }
}
