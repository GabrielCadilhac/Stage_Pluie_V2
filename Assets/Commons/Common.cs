using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public static class Constants
{
    public const int MAX_BLOCKS_NUMBER = 65536;
    public const int BLOCK_SIZE = 1024;
}

public static class Common
{
    public static Vector3Int NB_CELLS = new Vector3Int(9, 9, 9);

    // Draw an arrow
    public static void DrawArrow(Vector3 pos, Vector3 direction, Color color, float arrowHeadLength = 0.25f, float arrowHeadAngle = 20.0f)
    {
        Gizmos.color = color;
        if (direction == Vector3.zero)
            return;

        Gizmos.DrawRay(pos, direction);

        Vector3 right = Quaternion.LookRotation(direction) * Quaternion.Euler(0, 180 + arrowHeadAngle, 0) * new Vector3(0, 0, 1);
        Vector3 left = Quaternion.LookRotation(direction) * Quaternion.Euler(0, 180 - arrowHeadAngle, 0) * new Vector3(0, 0, 1);
        Gizmos.DrawRay(pos + direction, right * arrowHeadLength);
        Gizmos.DrawRay(pos + direction, left * arrowHeadLength);
    }

    public static Vector3 Divide(Vector3 a, Vector3 b)
    {
        return new Vector3(a.x / b.x, a.y / b.y, a.z / b.z);
    }

    public static Vector3 Multiply(Vector3 a, Vector3 b)
    {
        return new Vector3(a.x * b.x, a.y * b.y, a.z * b.z);
    }

    public static Vector3 Abs(Vector3 a)
    {
        return new Vector3(Mathf.Abs(a.x), Mathf.Abs(a.y), Mathf.Abs(a.z));
    }

    public static Vector3 Cart2Cyl(Vector3 p_coord)
    {
        float r = Mathf.Sqrt(p_coord.x * p_coord.x + p_coord.y * p_coord.y);
        float theta = 0f;
        if (p_coord.x != 0f)
            theta = Mathf.Atan(p_coord.y / p_coord.x);
        else if (p_coord.y > 0f)
            theta = Mathf.PI / 2f;
        else if (p_coord.y < 0f)
            theta = -Mathf.PI / 2f;

        if (p_coord.x < 0f)
            theta += Mathf.PI;

        return new(r, theta, p_coord.z);
    }

    // Convert cylindrical coords (r,theta) to cartesian
    public static Vector2 Cyl2Cart(Vector2 p_coord)
    {
        return new Vector2(p_coord.x * Mathf.Cos(p_coord.y), p_coord.x * Mathf.Sin(p_coord.y));
    }
}
