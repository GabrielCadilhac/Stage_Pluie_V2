using UnityEngine;
using System.IO;

public static class Constants
{
    // GPU
    public const int MaxBlocksNumber = 65535;
    public const int BlockSize = 1024;

    // Hodograph
    public const int HodographPoints = 4;

    // Global
    public static float DeltaTime = 3f; // Simulation delta time

    // Wind
    public static Vector3 GlobalWind = new Vector3(4f, 0f, 2f); // Global Wind
    public static float LocalWindStrength = 8f;

    // Super Primitive
    public static float EnergyStrength = 2.78f;      // Rapport entre l'�nergie et la force
    public static float EnergySpeed    = 0.304f;   // Rapport entre l'�nergie et la vitesse

    public static float CoeffDissip    = 0.01f;   // Quantit� d'�nergie dissip� � chq pas de temps
    public static float CoeffTransfert = 0.3f;    // Quantit� d'�nergie transf�r� � chq pas de temps

    public static float UniformStrength = 2f; // Modifier la force des primitives en fonction de leur types
    public static float VortexStrength  = 2f;
    public static float SourceStrength  = 2f;
    public static float SinkStrength    = 2f;

    // Energy Cascade
    public static float MeanEnergyPrim = 0.30f;   // Energie minimale pour cr�er une nouvelle primitive
    public static float StdEnergyPrim  = 0.25f;   //Ecart type entre les nouvelles 

    public static float MinSizeTall    = 0.2f;    // Taille minimale d'une grande primitive
    public static float MinSizeMedium  = 0.1f;    // Taille minimale d'une primitive moyenne
    public static float MinSizeSmall   = 0.01f;   // Taille minimale d'une petite primitive (avant destruction)

    // Debug
    public static bool RenderSphere   = false; // Afficher les sph�res de debug pour voir les d�placements des primitives
    public static bool DrawDebugGrid = true;  // Show turbulence vector field
    public static float SphereSize    = 20f;     // 100 correspond � la vraie taille des primitives

    // Splash
    public static bool RenderSplash = true; // Draw splash on the rainbox bottom plane

    // Configuration profiler
    public static Config Config;
}

public struct GPUTurbulence
{
    public Vector3 Pos;
    public float Size;
    public float Param;
    public float Strength;
    public int Type;
};

public struct Obb
{
    public Vector3 Center;
    public Vector3 Size;
    public Matrix4x4 Rotation;
}

public static class Common
{
    public static Vector3Int NbCells = new Vector3Int(9, 9, 9);

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

    public static Vector3 Cart2Cyl(Vector3 pCoord)
    {
        float r = Mathf.Sqrt(pCoord.x * pCoord.x + pCoord.y * pCoord.y);
        float theta = 0f;
        if (pCoord.x != 0f)
            theta = Mathf.Atan(pCoord.y / pCoord.x);
        else if (pCoord.y > 0f)
            theta = Mathf.PI / 2f;
        else if (pCoord.y < 0f)
            theta = -Mathf.PI / 2f;

        if (pCoord.x < 0f)
            theta += Mathf.PI;

        return new(r, theta, pCoord.z);
    }

    // Convert cylindrical coords (r,theta) to cartesian
    public static Vector2 Cyl2Cart(Vector2 pCoord)
    {
        return new Vector2(pCoord.x * Mathf.Cos(pCoord.y), pCoord.x * Mathf.Sin(pCoord.y));
    }

    public static Config[] LoadFile(string fileName)
    {
        string jsonString = File.ReadAllText(fileName);
        Config[] configsArray = JsonUtility.FromJson<Configs>(jsonString).configs;
        return configsArray;
    }
}
