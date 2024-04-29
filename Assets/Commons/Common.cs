using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public static class Constants
{
    // GPU
    public const int MAX_BLOCKS_NUMBER = 65536;
    public const int BLOCK_SIZE = 1024;

    // Hodograph
    public const int HODOGRAPH_POINTS = 4;

    // Super Primitive
    public const  float ENERGY_STRENGTH = 3f;      // Rapport entre l'énergie et la force
    public const  float ENERGY_SPEED    = 0.25f;   // Rapport entre l'énergie et la vitesse
    
    public const  float COEFF_DISSIP    = 0.01f;   // Quantité d'énergie dissipé à chq pas de temps
    public const  float COEFF_TRANSFERT = 0.2f;    // Quantité d'énergie transféré à chq pas de temps

    // Energy Cascade
    public const float MEAN_ENERGY_PRIM = 0.30f;   // Energie minimale pour créer une nouvelle primitive
    public const float STD_ENERGY_PRIM  = 0.25f;   // Pourcentage de différence entre les nouvelles énergies

    public const float MIN_SIZE_TALL    = 0.2f;    // Taille minimale d'une grande primitive
    public const float MIN_SIZE_MEDIUM  = 0.1f;    // Taille minimale d'une primitive moyenne
    public const float MIN_SIZE_SMALL   = 0.01f;   // Taille minimale d'une petite primitive (avant destruction)
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

    public static Vector2 Divide(Vector2 a, Vector2 b)
    {
        return new Vector2(a.x / b.x, a.y / b.y);
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
