using System.Collections;
using UnityEngine;
public abstract class BasePrimitive
{
    protected Vector3 _position;
    protected float _speed, _size, _currentLerp;

    private BezierCurve _bezierCurve;

    public BasePrimitive(BezierCurve bezierCurve, Vector3 position, float p_speed, float size)
    {
        _bezierCurve = bezierCurve;
        _position = position;

        _speed = p_speed;
        _size = size;

        _currentLerp = 0.5f;
    }

    public void Update(float p_deltaTime, Vector3 p_min, Vector3 p_nDivSize)
    {
        Vector3 point = _bezierCurve.GetPoint(_currentLerp, true);

        float j = (point.x - p_min.x) * p_nDivSize.x;
        float i = (point.y - p_min.y) * p_nDivSize.y;
        float k = (point.z - p_min.z) * p_nDivSize.z;

        _position = new Vector3(j, i, k);

        _currentLerp += _speed * p_deltaTime * Time.deltaTime;
    }

    public void CheckCollision()
    {
        _currentLerp = _currentLerp > 1f ? 0f : _currentLerp;
    }

    public void SetSpeed(float p_newSpeed)
    {
        _speed = p_newSpeed;
    }

    public abstract Vector3 GetValue(float p_j, float p_i, float p_k);
}
