using UnityEngine;
using System.IO;

public static class Constants
{
    // GPU
    public const int MAX_BLOCKS_NUMBER = 65535;
    public const int BLOCK_SIZE = 1024;

    // Hodograph
    public const int HODOGRAPH_POINTS = 4;

    // Global
    public static float DELTA_TIME = 3f; // Simulation delta time

    // Wind
    public static Vector3 GLOBAL_WIND = new Vector3(4f, 0f, 2f); // Global Wind
    public static float LOCAL_WIND_STRENGTH = 8f;

    // Super Primitive
    public static float ENERGY_STRENGTH = 2.78f;      // Rapport entre l'énergie et la force
    public static float ENERGY_SPEED    = 0.304f;   // Rapport entre l'énergie et la vitesse

    public static float COEFF_DISSIP    = 0.01f;   // Quantité d'énergie dissipé à chq pas de temps
    public static float COEFF_TRANSFERT = 0.3f;    // Quantité d'énergie transféré à chq pas de temps

    public static float UNIFORM_STRENGTH = 2f; // Modifier la force des primitives en fonction de leur types
    public static float VORTEX_STRENGTH  = 2f;
    public static float SOURCE_STRENGTH  = 2f;
    public static float SINK_STRENGTH    = 2f;

    // Energy Cascade
    public static float MEAN_ENERGY_PRIM = 0.30f;   // Energie minimale pour créer une nouvelle primitive
    public static float STD_ENERGY_PRIM  = 0.25f;   //Ecart type entre les nouvelles 

    public static float MIN_SIZE_TALL    = 0.2f;    // Taille minimale d'une grande primitive
    public static float MIN_SIZE_MEDIUM  = 0.1f;    // Taille minimale d'une primitive moyenne
    public static float MIN_SIZE_SMALL   = 0.01f;   // Taille minimale d'une petite primitive (avant destruction)

    // Debug
    public static bool RENDER_SPHERE   = false; // Afficher les sphères de debug pour voir les déplacements des primitives
    public static bool DRAW_DEBUG_GRID = true;  // Show turbulence vector field
    public static float SPHERE_SIZE    = 20f;     // 100 correspond à la vraie taille des primitives

    // Splash
    public static bool RENDER_SPLASH = true; // Draw splash on the rainbox bottom plane

    // Configuration profiler
    public static Config CONFIG;
}

public struct GPUTurbulence
{
    public Vector3 pos;
    public float size;
    public float param;
    public float strength;
    public int type;
};

public struct OBB
{
    public Vector3 center;
    public Vector3 size;
    public Matrix4x4 rotation;
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

    public static Config[] LoadFile(string fileName)
    {
        string jsonString = File.ReadAllText(fileName);
        Config[] configsArray = JsonUtility.FromJson<Configs>(jsonString).configs;
        return configsArray;
    }
}
