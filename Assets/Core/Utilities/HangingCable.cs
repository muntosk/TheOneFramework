using UnityEngine;

namespace TheOneFramework.Utilities
{
    // A cable that hangs between this object and `end`, generated as a tube mesh. Move either end
    // (or change sag/thickness) in the editor and it rebuilds live. The mesh is rebuilt on load
    // instead of saved into the scene, so long cables don't bloat the scene file.
    [ExecuteAlways]
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class HangingCable : MonoBehaviour
    {
        [Tooltip("Where the cable ends. Make it a child or any other object in the scene.")]
        [SerializeField] private Transform end;

        [Tooltip("How far (metres) the middle of the cable droops below the straight line between the ends.")]
        [SerializeField, Min(0.0f)] private float sag = 0.5f;

        [SerializeField, Min(0.001f)] private float thickness = 0.03f;

        [SerializeField, Range(4, 128)] private int lengthSegments = 24;

        [SerializeField, Range(3, 16)] private int radialSegments = 6;

        private Mesh mesh;
        private Vector3 lastStart;
        private Vector3 lastEnd;
        private Quaternion lastRotation;
        private bool dirty;

        private void OnEnable()
        {
            Rebuild();
        }

        private void OnDisable()
        {
            if (mesh != null)
            {
                DestroyImmediate(mesh);
                mesh = null;
            }
        }

        private void OnValidate()
        {
            // Can't touch the mesh from OnValidate, so rebuild on the next Update instead.
            dirty = true;
        }

        private void Update()
        {
            if (end == null)
            {
                return;
            }

            // Only rebuild when something actually moved, so static cables cost nothing at runtime.
            if (dirty || transform.position != lastStart || end.position != lastEnd || transform.rotation != lastRotation)
            {
                Rebuild();
            }
        }

        private void Rebuild()
        {
            if (end == null)
            {
                return;
            }

            lastStart = transform.position;
            lastEnd = end.position;
            lastRotation = transform.rotation;
            dirty = false;

            if (mesh == null)
            {
                mesh = new Mesh { name = "HangingCable", hideFlags = HideFlags.DontSave };
                GetComponent<MeshFilter>().sharedMesh = mesh;
            }

            int rings = lengthSegments + 1;
            int ringVerts = radialSegments + 1; // duplicate seam vertex for clean UVs
            var vertices = new Vector3[rings * ringVerts];
            var normals = new Vector3[vertices.Length];
            var uvs = new Vector2[vertices.Length];
            var triangles = new int[lengthSegments * radialSegments * 6];

            // Work in local space so the mesh follows this object.
            Vector3 a = Vector3.zero;
            Vector3 b = transform.InverseTransformPoint(end.position);
            Vector3 down = transform.InverseTransformDirection(Vector3.down);

            float length = 0.0f;
            Vector3 previous = a;
            Vector3 frameUp = Vector3.Cross(b - a, down).sqrMagnitude > 1e-6f ? down : Vector3.forward;

            for (int i = 0; i < rings; i++)
            {
                float t = i / (float)lengthSegments;
                Vector3 point = PointAt(a, b, down, t);
                Vector3 tangent = (PointAt(a, b, down, Mathf.Min(1.0f, t + 0.01f)) - PointAt(a, b, down, Mathf.Max(0.0f, t - 0.01f))).normalized;

                // Parallel-transport the frame so the tube doesn't twist.
                Vector3 side = Vector3.Cross(tangent, frameUp).normalized;
                frameUp = Vector3.Cross(side, tangent).normalized;

                length += Vector3.Distance(previous, point);
                previous = point;

                for (int r = 0; r < ringVerts; r++)
                {
                    float angle = r / (float)radialSegments * Mathf.PI * 2.0f;
                    Vector3 normal = side * Mathf.Cos(angle) + frameUp * Mathf.Sin(angle);
                    int v = i * ringVerts + r;
                    vertices[v] = point + normal * (thickness * 0.5f);
                    normals[v] = normal;
                    uvs[v] = new Vector2(r / (float)radialSegments, length / Mathf.Max(thickness, 0.001f) * 0.25f);
                }
            }

            int tri = 0;
            for (int i = 0; i < lengthSegments; i++)
            {
                for (int r = 0; r < radialSegments; r++)
                {
                    int v0 = i * ringVerts + r;
                    int v1 = v0 + ringVerts;
                    triangles[tri++] = v0;
                    triangles[tri++] = v1;
                    triangles[tri++] = v0 + 1;
                    triangles[tri++] = v0 + 1;
                    triangles[tri++] = v1;
                    triangles[tri++] = v1 + 1;
                }
            }

            mesh.Clear();
            mesh.vertices = vertices;
            mesh.normals = normals;
            mesh.uv = uvs;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();
        }

        // Parabola through both ends with its lowest offset (sag) in the middle - close enough to a
        // real catenary for cables, and much cheaper.
        private Vector3 PointAt(Vector3 a, Vector3 b, Vector3 down, float t)
        {
            return Vector3.Lerp(a, b, t) + down * (sag * 4.0f * t * (1.0f - t));
        }

        private void OnDrawGizmosSelected()
        {
            if (end != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(transform.position, thickness);
                Gizmos.DrawWireSphere(end.position, thickness);
            }
        }
    }
}
