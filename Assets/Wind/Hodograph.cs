using System;
using System.Collections;
using Unity.VisualScripting;
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

        UpdateCurve(_hodoPos);
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

        UpdateCurve(_hodoPos);
    }

    private void UpdateCurve(Vector3[] p_curvePoints)
    {
        float spacing = 1f / (Constants.HODOGRAPH_POINTS - 1f);

        Vector3 rainPos = _rainTransform.position;
        Vector3 start = new Vector3(p_curvePoints[0].x, rainPos.y, p_curvePoints[0].z  + rainPos.z);
        Vector3 end   = new Vector3(p_curvePoints[3].x, rainPos.y + _boxCollider.size.y, p_curvePoints[3].z + rainPos.z);

        _bezierCurve.SetPoint(0, start);
        _bezierCurve.SetPoint(3, end);

        Vector3 c1 = Vector3.Lerp(start, end, spacing);
        Vector3 c2 = Vector3.Lerp(start, end, 2f * spacing);

        _bezierCurve.SetPoint(1, c1);
        _bezierCurve.SetPoint(2, c2);
    }

    public Vector3[] GetPoints()
    {
        return _hodoPos;
    }
}
