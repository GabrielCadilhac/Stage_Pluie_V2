using System.Collections;
using UnityEngine;
public abstract class BasePrimitive
{
    protected Vector3 _position;
    protected float _speed, _size, _param, _strength;

    public BasePrimitive(Vector3 position, float p_param, float p_speed, float p_size, float p_strength)
    {
        _position = position;
        _speed = p_speed;
        _size = p_size;
        _param = p_param;
        _strength = p_strength;
    }

    public void SetPosition(Vector3 p_position)
    {
        _position = p_position;
    }

    public void SetSpeed(float p_newSpeed)
    {
        _speed = p_newSpeed;
    }

    public float GetParam()
    {
        return _param;
    }

    public abstract Vector3 GetValue(float p_j, float p_i, float p_k);
    public abstract uint GetPrimType();
}
