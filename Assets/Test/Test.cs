using UnityEngine;
public class Test : MonoBehaviour
{
    [SerializeField] private Transform _particle;
    [SerializeField] private Vector3 _rotation;

    private OBB _bounds;

    private Vector3 _min, _max;

    private Matrix4x4 _rotMatrix;

    float _pointSize = 0.5f;
    float _boxSize = 9f;


    private struct OBB
    {
        public Vector3 center;
        public Vector3 size;
        public Vector3 rotation;
    }

    private void Start()
    {
        _bounds          = new OBB();
        _bounds.center   = new Vector3(0f, 0f , 0f);
        _bounds.size     = new Vector3(9f, 9f, 9f);
        _bounds.rotation = new Vector3(0f, 0f, 0f);
    }

    private void Update()
    {
        UpdateRotation();
        transform.localScale = Vector3.one * _boxSize * 2f;
        Quaternion quat = new Quaternion();
        quat.eulerAngles = new Vector3(-_rotation.x, _rotation.y, -_rotation.z) * (180f / Mathf.PI);
        transform.rotation = rightCoordToUnityCord(quat);

        if (TestCollision(_min, _max, _rotMatrix * _particle.position))
        {
            Debug.Log("Collision !");
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            Debug.Log($"min {_min}");
            Debug.Log($"max {_max}");
            Debug.Log($"particle {_rotMatrix * _particle.position}");
        }
    }

    private Quaternion rightCoordToUnityCord(Quaternion q)
    {
        return new Quaternion(q.x, q.y, -q.z, -q.w);
    }

    private bool TestCollision(Vector3 p_min, Vector3 p_max, Vector3 p_pos)
    {
        return p_min.x < p_pos.x && p_pos.x < p_max.x &&
               p_min.y < p_pos.y && p_pos.y < p_max.y &&
               p_min.z < p_pos.z && p_pos.z < p_max.z;
    }

    private void UpdateRotation()
    {
        Vector3 c = new Vector3(Mathf.Cos(_rotation.x), Mathf.Cos(_rotation.y), Mathf.Cos(_rotation.z));
        Vector3 s = new Vector3(Mathf.Sin(_rotation.x), Mathf.Sin(_rotation.y), Mathf.Sin(_rotation.z));

        Matrix4x4 Rx = new Matrix4x4(new Vector4(1f, 0f, 0f, 0f),
                                     new Vector4(0f, c.x, s.x, 0f),
                                     new Vector4(0f, -s.x, c.x, 0f),
                                     new Vector4(0f, 0f, 0f, 1f));

        Matrix4x4 Ry = new Matrix4x4(new Vector4(c.y, 0f, s.y, 0f),
                                     new Vector4(0f, 1f, 0f, 0f),
                                     new Vector4(-s.y, 0f, c.y, 0f),
                                     new Vector4(0f, 0f, 0f, 1f));

        Matrix4x4 Rz = new Matrix4x4(new Vector4(c.z, -s.z, 0f, 0f),
                                     new Vector4(s.z, c.z, 0f, 0f),
                                     new Vector4(0f, 0f, 1f, 0f),
                                     new Vector4(0f, 0f, 0f, 1f));

        _rotMatrix = Rx * Ry * Rz;
    }

    private void OnDrawGizmos()
    {
        // Cube points
        Vector3 p0 = _rotMatrix * new Vector3(-1f, -1f, -1f);
        Vector3 p1 = _rotMatrix * new Vector3(-1f,  1f, -1f);
        Vector3 p2 = _rotMatrix * new Vector3( 1f, -1f, -1f);
        Vector3 p3 = _rotMatrix * new Vector3( 1f,  1f, -1f);
        Vector3 p5 = _rotMatrix * new Vector3(-1f, -1f,  1f);
        Vector3 p6 = _rotMatrix * new Vector3(-1f,  1f,  1f);
        Vector3 p7 = _rotMatrix * new Vector3( 1f, -1f,  1f);
        Vector3 p8 = _rotMatrix * new Vector3( 1f,  1f,  1f);

        // Min
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(p0 * _boxSize, _pointSize);
        _min = new Vector3(-1f, -1f, -1f) * _boxSize;
        Gizmos.color = Color.white;
        
        Gizmos.DrawSphere(p1 * _boxSize, _pointSize);
        Gizmos.DrawSphere(p2 * _boxSize, _pointSize);
        Gizmos.DrawSphere(p3 * _boxSize, _pointSize);
        Gizmos.DrawSphere(p5 * _boxSize, _pointSize);
        Gizmos.DrawSphere(p6 * _boxSize, _pointSize);
        Gizmos.DrawSphere(p7 * _boxSize, _pointSize);

        // Max
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(p8 * _boxSize, _pointSize);
        _max = new Vector3(1f, 1f, 1f) * _boxSize;
        Gizmos.color = Color.white;
    }
}
