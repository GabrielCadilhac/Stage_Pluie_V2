using System;
using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Serialization;

public class Hodograph : MonoBehaviour
{
    [FormerlySerializedAs("_boxCollider")] [SerializeField] private BoxCollider boxCollider;
    [FormerlySerializedAs("_bezierCurve")] [SerializeField] private BezierCurve bezierCurve;
    [FormerlySerializedAs("_parent")] [SerializeField] private GameObject parent;
    [FormerlySerializedAs("_rainTransform")] [SerializeField] private Transform rainTransform;
    
    private Bounds _bounds;

    private Vector3[] _hodoPos;
    
    private RectTransform _rectTransform;

    private Vector3 _right;
    private float _radius;

    public void Start()
    {
        _rectTransform = GetComponent<RectTransform>();
        _hodoPos = new Vector3[transform.childCount];
        _bounds = boxCollider.bounds;
        
        // Rayon est �gal � la taille la plus petite / 2
        _radius = Mathf.Min(_bounds.size.x, _bounds.size.z) / 2f;
        _right = new Vector3(1f, 0f, 0f);

        for (int i = 0; i < transform.childCount; i++)
            _hodoPos[i] = ComputeChildPos(i);

        //UpdateCurve(_hodoPos);
    }

    private Vector3 ComputeChildPos(int pIndex)
    {
        Transform child = transform.GetChild(pIndex);

        Vector3 dir = child.localPosition - transform.position;
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

        //UpdateCurve(_hodoPos);
    }

    private void UpdateCurve(Vector3[] pCurvePoints)
    {
        float spacing = 1f / (Constants.HodographPoints - 1f);

        Vector3 rainPos = rainTransform.position;
        Vector3 start = new Vector3(pCurvePoints[0].x, rainPos.y, pCurvePoints[0].z  + rainPos.z);
        Vector3 end   = new Vector3(pCurvePoints[3].x, rainPos.y + boxCollider.size.y, pCurvePoints[3].z + rainPos.z);

        bezierCurve.SetPoint(0, start);
        bezierCurve.SetPoint(3, end);

        Vector3 c1 = Vector3.Lerp(start, end, spacing);
        Vector3 c2 = Vector3.Lerp(start, end, 2f * spacing);

        bezierCurve.SetPoint(1, c1);
        bezierCurve.SetPoint(2, c2);
    }

    public Vector3[] GetPoints()
    {
        return _hodoPos;
    }
}
