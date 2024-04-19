using System;
using System.Collections;
using UnityEngine;

public class Hodograph : MonoBehaviour
{
    [SerializeField] private BoxCollider _boxCollider;
    [SerializeField] private BezierCurve _bezierCurve;
    [SerializeField] private GameObject _parent;
    [SerializeField] private Transform _rainTransform;
    
    private Bounds _bounds;

    private Vector3[] _hodoPos;
    
    private RectTransform _rectTransform;

    private Vector3 _right;
    private float _radius;

    public void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        _hodoPos = new Vector3[transform.childCount];
        _bounds = _boxCollider.bounds;
        
        // Rayon est égal à la taille la plus petite / 2
        _radius = Mathf.Min(_bounds.size.x, _bounds.size.z) / 2f;
        _right = new Vector3(1f, 0f, 0f);

        for (int i = 0; i < transform.childCount; i++)
            _hodoPos[i] = ComputeChildPos(i);
    }

    private Vector3 ComputeChildPos(int p_index)
    {
        Transform child = transform.GetChild(p_index);

        Vector3 dir = child.position - transform.position;
        float ySign = Mathf.Sign(dir.y);
        float distance = 2f * dir.magnitude / _rectTransform.sizeDelta.x;

        float cosTheta = Vector3.Dot(_right, dir.normalized);
        float sinTheta = Mathf.Sqrt(1f - cosTheta * cosTheta) * ySign;

        return new Vector3(distance * _radius * cosTheta, _bounds.center.y, distance * _radius * sinTheta);
    }

    public void Update()
    {
        if (_rectTransform == null) return;

        for (int i = 0; i < transform.childCount; i++)
            _hodoPos[i] = ComputeChildPos(i);

        Vector3[] curvePoints = new Vector3[_hodoPos.Length];
        for (int i = 0; i < _hodoPos.Length; i++)
            curvePoints[i] = _hodoPos[i];

        UpdateCurve(curvePoints);
    }

    private void UpdateCurve(Vector3[] p_curvePoints)
    {
        Vector3 min = _bezierCurve.transform.InverseTransformPoint(_bounds.min);
        Vector3 max = _bezierCurve.transform.InverseTransformPoint(_bounds.max);

        float t = 0.25f;
        Vector3 c1 = (1f - t) * min + t * max - p_curvePoints[1];
        Vector3 c2 = t * min + (1f - t) * max - p_curvePoints[2];

        _bezierCurve.SetPoint(0, min);
        _bezierCurve.SetPoint(1, c1);
        _bezierCurve.SetPoint(2, c2);
        _bezierCurve.SetPoint(3, max);
    }

    public Vector3[] GetPoints()
    {
        return _hodoPos;
    }
}
