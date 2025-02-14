using UnityEngine;
using UnityEngine.Serialization;

namespace Test
{
    public class Test : MonoBehaviour
    {
        [FormerlySerializedAs("_particle")] [SerializeField] private Transform particle;
        [FormerlySerializedAs("_material")] [SerializeField] private Material material;

        [FormerlySerializedAs("_angles")] [SerializeField] private Vector3 angles;

        private Obb _bounds;

        private struct Obb
        {
            public Vector3 Center;
            public Vector3 Size;
            public Matrix4x4 Rotation;
        }

        private void Start()
        {
            _bounds = new Obb();
            UpdateObb();
        }

        private void Update()
        {
            UpdateObb();

            if (TestCollision(particle.position, _bounds))
                material.color = Color.green;
            else
                material.color = Color.red;
        }

        private bool TestCollision(Vector3 pPos, Obb pObb)
        {
            Vector3 d = pPos - pObb.Center;
            Vector3 ux = _bounds.Rotation.GetColumn(0);
            Vector3 uy = _bounds.Rotation.GetColumn(1);
            Vector3 uz = _bounds.Rotation.GetColumn(2);

            // Normalize ux, uy and uz
            ux.Normalize();
            uy.Normalize();
            uz.Normalize();

            // Project the particle on the box axis : ux, uy and uz
            float px = Vector3.Dot(d, ux);
            float py = Vector3.Dot(d, uy);
            float pz = Vector3.Dot(d, uz);

            return Mathf.Abs(px) <= pObb.Size.x && Mathf.Abs(py) <= pObb.Size.y && Mathf.Abs(pz) <= pObb.Size.z;
        }

        private void UpdateObb()
        {
            Vector3 angles = this.angles * Mathf.Deg2Rad; // In radians

            Vector3 c = new Vector3(Mathf.Cos(angles.x), Mathf.Cos(angles.y), Mathf.Cos(angles.z));
            Vector3 s = new Vector3(Mathf.Sin(angles.x), Mathf.Sin(angles.y), Mathf.Sin(angles.z));

            Matrix4x4 rx = new Matrix4x4(new Vector4(1f, 0f, 0f, 0f),
                new Vector4(0f, c.x, s.x, 0f),
                new Vector4(0f, -s.x, c.x, 0f),
                new Vector4(0f, 0f, 0f, 1f));

            Matrix4x4 ry = new Matrix4x4(new Vector4(c.y, 0f, s.y, 0f),
                new Vector4(0f, 1f, 0f, 0f),
                new Vector4(-s.y, 0f, c.y, 0f),
                new Vector4(0f, 0f, 0f, 1f));

            Matrix4x4 rz = new Matrix4x4(new Vector4(c.z, -s.z, 0f, 0f),
                new Vector4(s.z, c.z, 0f, 0f),
                new Vector4(0f, 0f, 1f, 0f),
                new Vector4(0f, 0f, 0f, 1f));
            // Define the OBB
            _bounds.Rotation = rx * ry * rz;
            _bounds.Center = transform.position;
            _bounds.Size = transform.localScale / 2f;

            transform.rotation = _bounds.Rotation.rotation;
        }

        private void OnDrawGizmos()
        {
            Matrix4x4 trans = new Matrix4x4(new Vector4(1f, 0f, 0f, 0f),
                new Vector4(0f, 1f, 0f, 0f),
                new Vector4(0f, 0f, 1f, 0f),
                new Vector4(_bounds.Center.x, _bounds.Center.y, _bounds.Center.z, 1f));

            Gizmos.matrix = trans * _bounds.Rotation * trans.inverse;
            Gizmos.DrawCube(_bounds.Center, transform.localScale);
            Gizmos.matrix = Matrix4x4.identity;
        }
    }
}
