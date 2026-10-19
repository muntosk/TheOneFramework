using UnityEngine;

namespace TheOneFramework.Portals
{
    // Fills a rectangular wall opening around the round Portal door: a wall-thick plate with a circular
    // hole, so the corners between the door's ring and the square hole in the wall are closed. ChamberShell
    // adds one to every door opening. UVs are world-aligned like TiledBox, so the tiles run on from the
    // wall around it. Pivot = bottom middle of the opening, the plate spans z -depth/2 .. +depth/2.
    // The mesh is rebuilt on load rather than saved into the scene.
    [ExecuteAlways]
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class DoorSurround : MonoBehaviour
    {
        public float width = 2.4384f;
        public float height = 2.2f;
        public float depth = 0.25f;
        [Tooltip("Middle of the door's ring, measured from the floor of the opening.")]
        public float holeCentreY = 0.83f;
        [Tooltip("Slightly smaller than the ring's outside, so the edge stays hidden behind it.")]
        public float holeRadius = 1.17f;
        [SerializeField, Min(0.01f)] private float textureWorldSize = 2.4384f;

        private const int Segments = 64;
        private Mesh mesh;
        private Vector3 lastPosition;
        private Quaternion lastRotation;

        private void OnEnable() => Rebuild();

        private void OnDisable()
        {
            if (mesh != null) DestroyImmediate(mesh);
            mesh = null;
        }

        private void OnValidate() => lastPosition = new Vector3(float.NaN, 0, 0);   // rebuild on the next Update

        private void Update()
        {
            if (transform.position != lastPosition || transform.rotation != lastRotation) Rebuild();
        }

        // Each face is a ring of quads between the circle and the rectangle, along rays from the hole's
        // centre. Where the circle pokes out of the rectangle (below the floor) the quad collapses to nothing.
        public void Rebuild()
        {
            lastPosition = transform.position;
            lastRotation = transform.rotation;
            if (mesh == null)
            {
                mesh = new Mesh { name = "DoorSurround", hideFlags = HideFlags.DontSave };
                GetComponent<MeshFilter>().sharedMesh = mesh;
            }

            // Evenly spread rays, plus one straight at each corner of the rectangle - otherwise the two
            // rays either side of a corner both hit the edges next to it and leave a sliver of the corner open.
            var angles = new System.Collections.Generic.List<float>();
            for (int s = 0; s < Segments; s++) angles.Add(s * Mathf.PI * 2.0f / Segments);
            foreach (var corner in new[] { new Vector2(width / 2, height), new Vector2(-width / 2, height), new Vector2(-width / 2, 0), new Vector2(width / 2, 0) })
            {
                float a = Mathf.Atan2(corner.y - holeCentreY, corner.x);
                angles.Add(a < 0 ? a + Mathf.PI * 2.0f : a);
            }
            angles.Sort();
            int rays = angles.Count;

            var vertices = new Vector3[2 * rays * 2];
            var normals = new Vector3[vertices.Length];
            var uvs = new Vector2[vertices.Length];
            var triangles = new int[2 * rays * 6];
            Vector3 worldU = transform.right, worldV = transform.up;

            for (int face = 0; face < 2; face++)
            {
                float z = face == 0 ? -depth / 2 : depth / 2;
                Vector3 normal = face == 0 ? Vector3.back : Vector3.forward;
                int start = face * rays * 2;
                for (int s = 0; s < rays; s++)
                {
                    float angle = angles[s];
                    var direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                    float toEdge = DistanceToRectangle(direction);
                    Vector2 inner = new Vector2(0, holeCentreY) + direction * Mathf.Min(holeRadius, toEdge);
                    Vector2 outer = new Vector2(0, holeCentreY) + direction * toEdge;

                    int i = start + s * 2;
                    vertices[i] = new Vector3(inner.x, inner.y, z);
                    vertices[i + 1] = new Vector3(outer.x, outer.y, z);
                    for (int k = i; k <= i + 1; k++)
                    {
                        normals[k] = normal;
                        Vector3 p = transform.TransformPoint(vertices[k]);
                        uvs[k] = new Vector2(Vector3.Dot(p, face == 0 ? worldU : -worldU), Vector3.Dot(p, worldV)) / textureWorldSize;
                    }

                    int next = start + (s + 1) % rays * 2;
                    int t = (face * rays + s) * 6;
                    // Angles run counter-clockwise seen from -Z; Unity draws clockwise faces, so the front (-Z)
                    // face takes the reverse order and the back (+Z) face, seen mirrored, the plain one.
                    if (face == 1)
                    {
                        triangles[t] = i; triangles[t + 1] = i + 1; triangles[t + 2] = next + 1;
                        triangles[t + 3] = i; triangles[t + 4] = next + 1; triangles[t + 5] = next;
                    }
                    else
                    {
                        triangles[t] = i; triangles[t + 1] = next + 1; triangles[t + 2] = i + 1;
                        triangles[t + 3] = i; triangles[t + 4] = next; triangles[t + 5] = next + 1;
                    }
                }
            }

            mesh.Clear();
            mesh.vertices = vertices;
            mesh.normals = normals;
            mesh.uv = uvs;
            mesh.triangles = triangles;
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
        }

        // How far a ray from the hole's centre runs before it leaves the opening's rectangle.
        private float DistanceToRectangle(Vector2 direction)
        {
            float best = float.MaxValue;
            if (direction.x > 1e-5f) best = Mathf.Min(best, (width / 2) / direction.x);
            if (direction.x < -1e-5f) best = Mathf.Min(best, (-width / 2) / direction.x);
            if (direction.y > 1e-5f) best = Mathf.Min(best, (height - holeCentreY) / direction.y);
            if (direction.y < -1e-5f) best = Mathf.Min(best, (-holeCentreY) / direction.y);
            return best;
        }
    }
}
