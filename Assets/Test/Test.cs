using UnityEngine;

namespace Test
{
    public class Test : MonoBehaviour
    {
        [SerializeField] private Transform _particle;
        [SerializeField] private Material _material;

        [SerializeField] private Vector3 _angles;

        private OBB _bounds;

        private struct OBB
        {
            public Vector3 center;
            public Vector3 size;
            public Matrix4x4 rotation;
        }

        private void Start()
        {
            _bounds = new OBB();
            UpdateOBB();
        }

        private void Update()
        {
            UpdateOBB();

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

        private void UpdateOBB()
        {
            Vector3 angles = _angles * Mathf.Deg2Rad; // In radians

            Vector3 c = new Vector3(Mathf.Cos(angles.x), Mathf.Cos(angles.y), Mathf.Cos(angles.z));
            Vector3 s = new Vector3(Mathf.Sin(angles.x), Mathf.Sin(angles.y), Mathf.Sin(angles.z));

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
            // Define the OBB
            _bounds.rotation = Rx * Ry * Rz;
            _bounds.center = transform.position;
            _bounds.size = transform.localScale / 2f;

            transform.rotation = _bounds.rotation.rotation;
        }

        private void OnDrawGizmos()
        {
            Matrix4x4 trans = new Matrix4x4(new Vector4(1f, 0f, 0f, 0f),
                new Vector4(0f, 1f, 0f, 0f),
                new Vector4(0f, 0f, 1f, 0f),
                new Vector4(_bounds.center.x, _bounds.center.y, _bounds.center.z, 1f));

            Gizmos.matrix = trans * _bounds.rotation * trans.inverse;
            Gizmos.DrawCube(_bounds.center, transform.localScale);
            Gizmos.matrix = Matrix4x4.identity;
        }
    }
}
