using System;
using System.Collections;
using UnityEngine;

public class Hodograph : MonoBehaviour
{
    [SerializeField] private BoxCollider _boxCollider;
    [SerializeField] private GameObject _parent;
    [SerializeField] private BezierCurve _bezierCurve;
    
    private Bounds _bounds;

    private GameObject[] _hodoPoints;
    
    private RectTransform _rectTransform;

    private Vector3 _right;
    private float _radius;


    public void Start()
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

            computeChildPos(i);
        }
    }

    private void computeChildPos(int p_index)
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
        for (int i = 0; i < transform.childCount; i++)
            computeChildPos(i);

        Vector3[] curvePoints = new Vector3[_hodoPoints.Length];
        for (int i = 0; i < _hodoPoints.Length; i++)
            curvePoints[i] = _hodoPoints[i].transform.localPosition;

        _bezierCurve.SetPoint(curvePoints);
    }
}
