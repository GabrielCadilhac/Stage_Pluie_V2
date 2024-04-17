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

    private GameObject[] _hodoPoints;
    
    private RectTransform _rectTransform;

    private Vector3 _right;
    private float _radius;

    public void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        _hodoPoints = new GameObject[transform.childCount];
        _bounds = _boxCollider.bounds;
        
        // Rayon est égal à la taille la plus petite / 2
        _radius = Mathf.Min(_bounds.size.x, _bounds.size.z) / 2f;
        _right = new Vector3(1f, 0f, 0f);

        for (int i = 0; i < transform.childCount; i++)
        {
            _hodoPoints[i] = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            _hodoPoints[i].name = "Sphere" + i;
            _hodoPoints[i].transform.localScale *= 0.5f;
            _hodoPoints[i].transform.parent = _parent.transform;

            ComputeChildPos(i);
        }
    }

    private void ComputeChildPos(int p_index)
    {
        Transform child = transform.GetChild(p_index);

        Vector3 dir = child.position - transform.position;
        float ySign = Mathf.Sign(dir.y);
        float distance = 2f * dir.magnitude / _rectTransform.sizeDelta.x;

        float cosTheta = Vector3.Dot(_right, dir.normalized);
        float sinTheta = Mathf.Sqrt(1f - cosTheta * cosTheta) * ySign;

        _hodoPoints[p_index].transform.localPosition = new Vector3(distance * _radius * cosTheta, _bounds.center.y, distance * _radius * sinTheta);
    }

    public void Update()
    {
        //for (int i = 0; i < transform.childCount; i++)
        //    computeChildPos(i);

        //Vector3[] curvePoints = new Vector3[_hodoPoints.Length];
        //for (int i = 0; i < _hodoPoints.Length; i++)
        //    curvePoints[i] = _hodoPoints[i].transform.localPosition;

        //_bezierCurve.SetPoint(curvePoints);
    }

    private void UpdateCurve(Vector3[] p_curvePoints)
    {
        Vector3 min = _bezierCurve.transform.InverseTransformPoint(_bounds.min);
        Vector3 max = _bezierCurve.transform.InverseTransformPoint(_bounds.max);

        float t = 0.25f;
        Vector3 c1 = (1f - t) * min + t * max + p_curvePoints[1];
        Vector3 c2 = t * min + (1f - t) * max + p_curvePoints[2];

        _bezierCurve.SetPoint(0, min);
        _bezierCurve.SetPoint(1, c1);
        _bezierCurve.SetPoint(2, c2);
        _bezierCurve.SetPoint(3, max);
    }

    private void OnDrawGizmos()
    {
        if (_rectTransform == null) return;

        for (int i = 0; i < transform.childCount; i++)
            ComputeChildPos(i);

        Vector3[] curvePoints = new Vector3[_hodoPoints.Length];
        for (int i = 0; i < _hodoPoints.Length; i++)
            curvePoints[i] = _hodoPoints[i].transform.localPosition;

        UpdateCurve(curvePoints);
    }

    public Vector3[] GetPoints()
    {
        Vector3[] curvePoints = new Vector3[_hodoPoints.Length];
        for (int i = 0; i < _hodoPoints.Length; i++)
            curvePoints[i] = _hodoPoints[i].transform.localPosition;

        return curvePoints;
    }
}
