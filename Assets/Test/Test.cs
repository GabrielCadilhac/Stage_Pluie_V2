using UnityEngine;
public class Test : MonoBehaviour
{
    [SerializeField] private Transform _particle;
    [SerializeField] private Vector3 _rotation;
    [SerializeField] private Material _material;

    private OBB _bounds;

    float _boxSize = 9f;

    private struct OBB
    {
        public Vector3 center;
        public Vector3 size;
        public Matrix4x4 rotation;
    }

    private void Start()
    {
        _bounds          = new OBB();
        _bounds.center   = new Vector3(0f, 0f , 0f);
        _bounds.size     = new Vector3(9f, 9f, 9f);
        _bounds.rotation = Matrix4x4.identity;
    }

    private void Update()
    {
        UpdateRotation();
        transform.localScale = Vector3.one * _boxSize * 2f;
        transform.rotation   = _bounds.rotation.rotation;

        if (TestCollision(_particle.position, _bounds))
            _material.color = Color.green;
        else
            _material.color = Color.red;
    }

    private bool TestCollision(Vector3 p_pos, OBB p_obb)
    {
        Vector3 D = p_pos - p_obb.center;
        Vector3 ux = _bounds.rotation.GetColumn(0);
        Vector3 uy = _bounds.rotation.GetColumn(1);
        Vector3 uz = _bounds.rotation.GetColumn(2);

        // Normalize ux, uy and uz
        ux.Normalize();
        uy.Normalize();
        uz.Normalize();

        // Project the particle on the box axis : ux, uy and uz
        float px = Vector3.Dot(D, ux);
        float py = Vector3.Dot(D, uy);
        float pz = Vector3.Dot(D, uz);

        return Mathf.Abs(px) <= p_obb.size.x && Mathf.Abs(py) <= p_obb.size.y && Mathf.Abs(pz) <= p_obb.size.z;
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

        _bounds.rotation = Rx * Ry * Rz;
    }

    private void OnDrawGizmos()
    {
        Gizmos.matrix = _bounds.rotation;
        Gizmos.DrawCube(Vector3.zero, transform.localScale);
        Gizmos.matrix = Matrix4x4.identity;
    }
}
