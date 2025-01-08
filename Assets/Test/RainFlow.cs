using Unity.Mathematics;
using UnityEngine;

public class RainFlow : MonoBehaviour
{
    private const int SIZE = 256;

    private Vector2Int _textureSize;
    private Texture2D _texture;

    private float[,] _flowMap = new float[SIZE, SIZE];

    private float[,,] _newtonsProbas   = new float[SIZE, SIZE, 8];
    private float[,,] _affinityProbas  = new float[SIZE, SIZE, 8];
    private float[,,] _wetDryProbas    = new float[SIZE, SIZE, 8];
    private float[,,] _obstaclesProbas = new float[SIZE, SIZE, 8];

    private float[,] _affinityCoeff = new float[SIZE, SIZE];
    private int[,]   _obstacles = new int[SIZE, SIZE];

    private Vector3[] _neighbors = new Vector3[8] {
                                            new Vector3(-1f, -1f, 0f),
                                            new Vector3(-1f, 0f, 0f),
                                            new Vector3(-1f, 1f, 0f),
                                            new Vector3(0f, 1f, 0f),
                                            new Vector3(1f, 1f, 0f),
                                            new Vector3(1f, 0f, 0f),
                                            new Vector3(1f, -1f, 0f),
                                            new Vector3(0f, -1f, 0f),
                                        };

    private float _cosThetaC = math.cos(85f * math.PI / 180f);
    // Start is called before the first frame update
    void Start()
    {
        _textureSize = new Vector2Int(256, 256);
        _texture = new Texture2D(_textureSize.x, _textureSize.y);

        // Initialize the _affinityCoeff to 0.5f
        for (int i = 0; i < SIZE; i++)
        {
            for (int j = 0; j < SIZE; j++)
            {
                _flowMap[i, j] = 0f;
                _obstacles[i, j] = 0;
                _affinityCoeff[i, j] = 0.5f;
            }
        }

        _texture.Apply();
        GetComponent<Renderer>().material.mainTexture = _texture;
    }

    void DrawFlowMap()
    {
        for (int i = 0; i < SIZE; i++)
        {
            for (int j = 0; j < SIZE; j++)
                _texture.SetPixel(i, j, new Color(_flowMap[i,j], 0f, 0f));
        }
    }

    // Compute the probability based on the Newton's Law
    void NewtonsLaw(Vector2 p_vp, int i, int j)
    {
        // Compute Dsum
        float dl1 = Mathf.Infinity;
        float dl2 = Mathf.Infinity;
        Vector3 d1 = Vector3.zero;
        Vector3 d2 = Vector3.zero;

        for (int k = 0; k < 8; k++)
        {
            Vector2 d = _neighbors[k];
            float newDl = Vector2.Dot(d, p_vp);
            if (newDl < dl1)
            {
                dl2 = dl1;
                d2 = d1;
                dl1 = newDl;
                d1 = d;
            }
            else if (newDl < dl2)
            {
                dl2 = newDl;
                d2 = d;
            }
        }

        float Dsum = 1f / (Vector3.Cross(d1, p_vp).magnitude) + 1f / (Vector3.Cross(d2, p_vp).magnitude);
        for (int k = 0; k < 8; j++)
        {
            Vector3 n = _neighbors[k];
            if (n.Equals(d1))
                _newtonsProbas[i, j, k] = 1 / (Dsum * Vector3.Cross(n, d1).magnitude);
            else if (n.Equals(d2))
                _newtonsProbas[i, j, k] = 1 / (Dsum * Vector3.Cross(n, d2).magnitude);
            else
                _newtonsProbas[i, j, k] = 0f;
        }
    }

    // Compute the probability based on the surface affinity
    void Affinity(Vector3 p_vp, int p_i, int p_j)
    {
        float Asum = 0f;
        // Compute Asum
        for (int k = 0; k < 8; k++)
        {
            Vector3 n = _neighbors[k];
            Asum += _affinityCoeff[p_i, p_j] * UStepFunction(n, p_vp);
        }

        // Compute the probability Ak
        for (int k = 0; k < 8; k++)
            _affinityProbas[p_i, p_j, k] = (_affinityCoeff[p_i, p_j]/Asum) * math.step(0f, _cosThetaC - Vector3.Dot(_neighbors[k].normalized, p_vp));
    }

    // Compute the probability based on the wet/dry conditions
    void WetDryConditions(Vector3 p_vp, int p_i, int p_j)
    {
        // Compute Wsum
        float Wsum = 0f;
        for (int k = 0; k < 8; k++)
        {
            Vector3 dl = _neighbors[k] + new Vector3(p_i, p_j, 0f);
            float gdk = _flowMap[(int) dl.x, (int) dl.y] > 0f ? 1f : 0f;
            Wsum += gdk * UStepFunction(dl, p_vp);
        }

        // Compute the probability Wk
        for (int k = 0; k < 8; k++)
            _wetDryProbas[p_i, p_j, k] = (_flowMap[(int)(_neighbors[k].x + p_i), (int)(_neighbors[k].y + p_j)] / Wsum) * UStepFunction(_neighbors[k], p_vp);
    }

    // Compute the probability based on the obstacles existance
    void ObstaclesExistance(int p_i, int p_j)
    {
        // Compute Ek
        for (int k = 0; k < 8; k++)
            _obstaclesProbas[p_i, p_j, k] = _obstacles[p_i, p_j] > 0 ? 0f : 1f;
    }

    // Compute the roulette direction and return the index of the direction
    int RouletteDirection(Vector3 p_vp, int p_i, int p_j)
    {
        float alpha1 = 0.001f;
        float alpha2 = 0.0001f;
        
        float Rsum = 0f;
        for (int k = 0; k < 8; k++)
        {
            float D = _newtonsProbas[p_i, p_j, k];
            float A = _affinityProbas[p_i, p_j, k];
            float W = _wetDryProbas[p_i, p_j, k];
            float E = _obstaclesProbas[p_i, p_j, k];

            // Compute Rsum
            Rsum += E * (alpha1 * p_vp.magnitude * D + alpha2 * A + W);
        }

        float[] Rk = new float[8];
        for (int k = 0; k < 8; k++)
        {
            float D = _newtonsProbas[p_i, p_j, k];
            float A = _affinityProbas[p_i, p_j, k];
            float W = _wetDryProbas[p_i, p_j, k];
            float E = _obstaclesProbas[p_i, p_j, k];

            Rk[k] = E * (alpha1 * p_vp.magnitude * D + alpha2 * A + W) / Rsum;
        }

        float r = UnityEngine.Random.Range(0f, 1f);
        float rk = 0f;
        int i = 0;
        while (rk < r)
        {
            rk += Rk[i];
            i++;
        }

        return i;
    }

    float UStepFunction(Vector3 p_dk, Vector3 p_vp)
    {
        return math.step(0f, _cosThetaC - Vector3.Dot(p_dk.normalized, p_vp.normalized));
    }

    // Update is called once per frame
    void Update()
    {
        Vector2 Vp = new Vector2(0f, 0f);

        for (int i = 0; i < SIZE; i++)
        {
            for (int j = 0; j < SIZE; j++)
            {
                NewtonsLaw(Vp, i, j);
                Affinity(i, j);
                WetDryConditions(i, j);
                ObstaclesExistance(i, j);
                RouletteDirection(Vp, i, j);
            }
        }
    }
}
