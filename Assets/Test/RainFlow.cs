using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class RainFlow : MonoBehaviour
{
    struct Drop
    {
        public Vector3 pos;
        public Vector3 vel;
        public float mass;
    }

    struct Mesh
    {
        public float[] axis; //xmin, xmax, ymin, ymax;
    }

    struct Collision
    {
        public float t;
        public int axis;
    }

    private const int SIZE = 16;
    private const float ALPHA1 = 0.001f;
    private const float ALPHA2 = 0.0001f;

    private Texture2D _texture;

    private float[,] _flowMap;

    private float[,] _affinityCoeff;
    private int[,] _obstacles;

    private List<Drop> _drops;
    
    private Vector3[] _neighbors = new Vector3[8] {
                                            new Vector3(-1f, -1f, 0f),
                                            new Vector3(-1f,  0f, 0f),
                                            new Vector3(-1f,  1f, 0f),
                                            new Vector3( 0f,  1f, 0f),
                                            new Vector3( 1f,  1f, 0f),
                                            new Vector3( 1f,  0f, 0f),
                                            new Vector3( 1f, -1f, 0f),
                                            new Vector3( 0f, -1f, 0f),
                                        };

    private float _cosThetaC = 85f * math.PI / 180f;

    private Mesh[,] _mesh;

    // Start is called before the first frame update
    void Start()
    {
        GetComponent<Renderer>().material.mainTexture = _texture;
        _texture = new Texture2D(SIZE, SIZE);
        _texture.filterMode = FilterMode.Point;

        _flowMap = new float[SIZE, SIZE];
        _affinityCoeff = new float[SIZE, SIZE];
        _obstacles = new int[SIZE, SIZE];

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

        _drops = new List<Drop>();

        GenerateMesh();
        DrawFlowMap();
    }

    void AddDrop(int p_i, int p_j)
    {
        Drop drop = new Drop();
        drop.pos = new Vector3(p_i, p_j, 0f); // initial position
        drop.vel = new Vector3(1f, -2f, 0f); // initial velocity
        drop.mass = 1f; // initial mass

        _flowMap[(int)drop.pos.x, (int)drop.pos.y] = drop.mass;

        _drops.Add(drop);
    }

    void GenerateMesh()
    {
        float cellSize = transform.localScale.x / SIZE;
        _mesh = new Mesh[SIZE, SIZE];
        for (int i = 0; i < SIZE; i++)
        {
            for (int j = 0; j < SIZE; j++)
            {
                _mesh[i, j].axis = new float[4];
                _mesh[i, j].axis[0] = i * cellSize;
                _mesh[i, j].axis[1] = (i + 1) * cellSize;
                _mesh[i, j].axis[2] = j * cellSize;
                _mesh[i, j].axis[3] = (j + 1) * cellSize;
            }
        }
    }

    void DrawFlowMap()
    {
        for (int i = 0; i < SIZE; i++)
        {
            for (int j = 0; j < SIZE; j++)
            {
                float r = _flowMap[i, j];
                _texture.SetPixel(i, j, new Color(r, r, r));
            }
        }
        _texture.Apply();
    }

    // Compute the probability based on the Newton's Law
    float[] NewtonsLaw(Vector3 p_vp, int i, int j)
    {
        // Compute Dsum
        float dl1 = 0f;
        float dl2 = 0f;
        Vector3 d1 = Vector3.zero;
        Vector3 d2 = Vector3.zero;

        float[] probas = new float[8];

        for (int k = 0; k < 8; k++)
        {
            Vector3 d = _neighbors[k];
            float newDl = Mathf.Max(Vector3.Dot(d.normalized, p_vp.normalized), 0f);
            if (newDl > dl1)
            {
                dl2 = dl1;
                d2 = d1;
                dl1 = newDl;
                d1 = d;
            }
            else if (newDl > dl2)
            {
                dl2 = newDl;
                d2 = d;
            }
        }

        float Dsum = 1f / (Vector3.Cross(d1, p_vp).magnitude) + 1f / (Vector3.Cross(d2, p_vp).magnitude);

        for (int k = 0; k < 8; k++)
        {
            Vector3 d = _neighbors[k];
            float newDl = Mathf.Max(Vector3.Dot(d.normalized, p_vp.normalized), 0f);
            if (newDl == dl1 || newDl == dl2)
                probas[k] = 1f / (Dsum * Vector3.Cross(d, p_vp).magnitude);
            else
                probas[k] = 0f;
        }
        return probas;
    }

    float UStepFunction(Vector3 p_dk, Vector3 p_vp)
    {
        return math.step(0f, _cosThetaC - Mathf.Acos(Vector3.Dot(p_dk.normalized, p_vp.normalized)));
    }

    // Compute the probability based on the surface affinity
    float[] Affinity(Vector3 p_vp, int p_i, int p_j)
    {
        float Asum = 0f;
        float[] probas = new float[8] { 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f };

        // Compute Asum
        for (int k = 0; k < 8; k++)
        {
            Vector3 dl = _neighbors[k] + new Vector3(p_i, p_j, 0f);
            if (dl.x >= 0 && dl.x < SIZE && dl.y >= 0 && dl.y < SIZE)
                Asum += _affinityCoeff[(int) dl.x, (int) dl.y] * UStepFunction(_neighbors[k], p_vp);
        }

        // Compute the probability Ak
        for (int k = 0; k < 8; k++)
        {
            Vector3 dl = _neighbors[k] + new Vector3(p_i, p_j, 0f);
            if (dl.x >= 0 && dl.x < SIZE && dl.y >= 0 && dl.y < SIZE)
                probas[k] = (_affinityCoeff[(int) dl.x, (int) dl.y] / Asum) * UStepFunction(_neighbors[k], p_vp);
        }
        return probas;
    }

    // Compute the probability based on the wet/dry conditions
    float[] WetDryConditions(Vector3 p_vp, int p_i, int p_j)
    {
        // Compute Wsum
        float Wsum = 0f;
        float[] probas = new float[8] {0f,0f,0f,0f,0f,0f,0f,0f};

        for (int k = 0; k < 8; k++)
        {
            Vector3 dl = _neighbors[k] + new Vector3(p_i, p_j, 0f);
            float gdk = 0f;
            if (dl.x >= 0 && dl.x < SIZE && dl.y >= 0 && dl.y < SIZE)
                gdk = _flowMap[(int)dl.x, (int)dl.y] > 0f ? 1f : 0f;
            Wsum += gdk * UStepFunction(dl, p_vp);
        }

        if (Wsum == 0f)
            return probas;

        // Compute the probability Wk
        for (int k = 0; k < 8; k++)
        {
            Vector3 dl = _neighbors[k] + new Vector3(p_i, p_j, 0f);
            if (dl.x >= 0 && dl.x < SIZE && dl.y >= 0 && dl.y < SIZE)
                probas[k] = (_flowMap[(int)(_neighbors[k].x + p_i), (int)(_neighbors[k].y + p_j)] / Wsum) * UStepFunction(_neighbors[k], p_vp);
            else
                probas[k] = 0f;
        }
        return probas;
    }

    // Compute the probability based on the obstacles existance
    float[] ObstaclesExistance(int p_i, int p_j)
    {
        float[] probas = new float[8];
        // Compute Ek
        for (int k = 0; k < 8; k++)
        {
            Vector3 dl = _neighbors[k] + new Vector3(p_i, p_j, 0f);
            if (dl.x >= 0 && dl.x < SIZE && dl.y >= 0 && dl.y < SIZE)
                probas[k] = _obstacles[(int)dl.x, (int)dl.y] > 0 ? 0f : 1f;
            else
                probas[k] = 1f;
        }
        return probas;
    }

    // Compute the roulette direction and return the index of the direction
    int RouletteDirection(Vector3 p_vp, float[] p_newtonProbas, float[] p_affinityProbas, float[] p_wetDryProbas, float[] p_obstaclesProbas)
    {
        float Rsum = 0f;
        for (int k = 0; k < 8; k++)
        {
            float D = p_newtonProbas[k];
            float A = p_affinityProbas[k];
            float W = p_wetDryProbas[k];
            float E = p_obstaclesProbas[k];

            // Compute Rsum
            Rsum += E * (ALPHA1 * p_vp.magnitude * D + ALPHA2 * A + W);
        }

        float[] Rk = new float[8];
        for (int k = 0; k < 8; k++)
        {
            float D = p_newtonProbas[k];
            float A = p_affinityProbas[k];
            float W = p_wetDryProbas[k];
            float E = p_obstaclesProbas[k];

            Rk[k] = E * (ALPHA1 * p_vp.magnitude * D + ALPHA2 * A + W) / Rsum;
        }

        float r = UnityEngine.Random.Range(0f, 1f);
        int i = 0;
        float rk = Rk[i];
        while (rk < r)
        {
            i++;
            rk += Rk[i];
        }

        return i;
    }

    float h(float p_x)
    {
        return p_x >= 0.4f ? 0.1f : math.log(math.sqrt(p_x) + 1f) / 2.17f;
    }

    // Compute the collision with a Mesh
    Collision ComputeMinT(Drop p_drop, Mesh p_mesh, Vector3 extForce)
    {
        Collision c = new Collision();
        c.t = Mathf.Infinity;
        c.axis = 0;
        //Loop through the mesh
        for (int i = 0; i < 4; i++)
        {
            int currentAxis = i < 2 ? 0 : 1; // if i equals 0 or 1, the axis is x, otherwise y
            float massForce = extForce[currentAxis] / p_drop.mass;
            float t1 = (-p_drop.vel.x + math.sqrt(p_drop.vel.x * p_drop.vel.x - 4f * massForce * (p_drop.pos.x - p_mesh.axis[i]))) / (2f * massForce);
            float t2 = (-p_drop.vel.x - math.sqrt(p_drop.vel.x * p_drop.vel.x - 4f * massForce * (p_drop.pos.x - p_mesh.axis[i]))) / (2f * massForce);
            if (t1 > 0f && t1 < t2 && c.t > t1)
            {
                c.axis = currentAxis;
                c.t = t1;
            }
            else if (t2 > 0f && c.t > t2)
            {
                c.axis = currentAxis;
                c.t = t2;
            }
        }
        return c;
    }

    void UpdateParticle(List<Drop> p_drops, int p_idDrop)
    {
        Drop drop = p_drops[p_idDrop];
        int i = (int)drop.pos.x;
        int j = (int)drop.pos.y;

        Vector3 extForce = new Vector3(0f, -9.81f, 0f);
        Mesh currentMesh = _mesh[i, j];
        Collision col    = ComputeMinT(drop, currentMesh, extForce);

        Vector3 Vp = (extForce / drop.mass) * col.t + drop.vel;

        float[] newtonProbas    = NewtonsLaw(Vp, i, j);
        float[] affinityProbas  = Affinity(Vp, i, j);
        float[] wetDryProbas    = WetDryConditions(Vp, i, j);
        float[] obstaclesProbas = ObstaclesExistance(i, j);

        int p = RouletteDirection(Vp, newtonProbas, affinityProbas, wetDryProbas, obstaclesProbas);

        Vector3 dp = new Vector3(i,j,0f) + _neighbors[p];

        // if dp is outside the mesh, remove the drop from the list
        if (dp.x < 0 || dp.x >= SIZE || dp.y < 0 || dp.y >= SIZE)
        {
            _drops.Remove(drop);
            return;
        }

        float remainingMass = h(_affinityCoeff[i, j]) * _flowMap[i, j];

        drop.mass -= remainingMass;
        _flowMap[(int)dp.x, (int)dp.y] += drop.mass;
        _flowMap[i, j] -= _flowMap[i, j] - remainingMass;

        // Update the drop position and velocity
        drop.pos = dp;
        drop.vel = Vector3.Dot(drop.vel, _neighbors[p]) * _neighbors[p].normalized;

        p_drops[p_idDrop] = drop;
    }

    void Update()
    {
        // Init _drops[0] with null
        if (Input.GetKeyDown(KeyCode.E))
        {
            // Loop through the drops and call UpdateParticle for each
            for (int i = 0; i < _drops.Count; i++)
                UpdateParticle(_drops, i);
            DrawFlowMap();
        }

        // Raycasst mouse to get the uv of the plane
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit))
            {
                Vector2 uv = hit.textureCoord;
                int i = (int)(uv.x * SIZE);
                int j = (int)(uv.y * SIZE);
                
                AddDrop(i, j);
                DrawFlowMap();
            }
        }
    }

    private void OnDrawGizmos()
    {
        if (_mesh == null) return;

        // Loop through the _mesh and draw segment for each axis
        for (int i = 0; i < SIZE; i++)
        {
            for (int j = 0; j < SIZE; j++)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(new Vector3(_mesh[i, j].axis[0], _mesh[i, j].axis[2], 0f), new Vector3(_mesh[i, j].axis[1], _mesh[i, j].axis[2], 0f));
                Gizmos.DrawLine(new Vector3(_mesh[i, j].axis[1], _mesh[i, j].axis[2], 0f), new Vector3(_mesh[i, j].axis[1], _mesh[i, j].axis[3], 0f));
                Gizmos.DrawLine(new Vector3(_mesh[i, j].axis[1], _mesh[i, j].axis[3], 0f), new Vector3(_mesh[i, j].axis[0], _mesh[i, j].axis[3], 0f));
                Gizmos.DrawLine(new Vector3(_mesh[i, j].axis[0], _mesh[i, j].axis[3], 0f), new Vector3(_mesh[i, j].axis[0], _mesh[i, j].axis[2], 0f));
            }
        }
    }
}