using UnityEngine;

namespace TheOneFramework.Utilities
{
    // Level-building block: a box mesh whose UVs follow its real size, so you can resize it with the
    // normal Scale tool and the texture keeps its tile size instead of stretching. With worldAligned on
    // (the default) UVs come from world position, so tiles line up across neighbouring panels the way
    // Hammer's world-aligned textures do. Pair it with a BoxCollider of size 1 (it scales along).
    // The mesh is rebuilt on load rather than saved into the scene.
    [ExecuteAlways]
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class TiledBox : MonoBehaviour
    {
        [Tooltip("How many metres one repeat of the texture covers.")]
        [SerializeField, Min(0.01f)] private float textureWorldSize = 2.4384f;

        [Tooltip("Tile from world position so neighbouring panels line up. Untick to tile from this box's own corner.")]
        [SerializeField] private bool worldAligned = true;

        // Per face: outward normal, then the u and v axes (v points up on side faces).
        // Chosen so cross(v, u) == normal, which gives Unity's clockwise front-face winding.
        private static readonly Vector3[,] Faces =
        {
            { Vector3.right,   Vector3.forward, Vector3.up },
            { Vector3.left,    Vector3.back,    Vector3.up },
            { Vector3.forward, Vector3.left,    Vector3.up },
            { Vector3.back,    Vector3.right,   Vector3.up },
            { Vector3.up,      Vector3.right,   Vector3.forward },
            { Vector3.down,    Vector3.left,    Vector3.forward },
        };

        private Mesh mesh;
        private Vector3 lastScale;
        private Vector3 lastPosition;
        private Quaternion lastRotation;
        private bool dirty = true;

        public float TextureWorldSize
        {
            get => textureWorldSize;
            set { textureWorldSize = Mathf.Max(0.01f, value); dirty = true; }
        }

        private void OnEnable()
        {
            dirty = true;
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
            bool moved = worldAligned && (transform.position != lastPosition || transform.rotation != lastRotation);
            if (dirty || moved || transform.lossyScale != lastScale)
            {
                Rebuild();
            }
        }

        private void Rebuild()
        {
            lastScale = transform.lossyScale;
            lastPosition = transform.position;
            lastRotation = transform.rotation;
            dirty = false;

            if (mesh == null)
            {
                mesh = new Mesh { name = "TiledBox", hideFlags = HideFlags.DontSave };
                GetComponent<MeshFilter>().sharedMesh = mesh;
            }

            var vertices = new Vector3[24];
            var normals = new Vector3[24];
            var uvs = new Vector2[24];
            var triangles = new int[36];

            for (int f = 0; f < 6; f++)
            {
                Vector3 n = Faces[f, 0];
                Vector3 u = Faces[f, 1];
                Vector3 v = Faces[f, 2];
                Vector3 centre = n * 0.5f;

                Vector3 worldU = transform.TransformDirection(u).normalized;
                Vector3 worldV = transform.TransformDirection(v).normalized;

                for (int c = 0; c < 4; c++)
                {
                    // Corner order: (-u,-v), (-u,+v), (+u,+v), (+u,-v).
                    float su = c < 2 ? -0.5f : 0.5f;
                    float sv = c == 0 || c == 3 ? -0.5f : 0.5f;
                    Vector3 local = centre + u * su + v * sv;

                    Vector3 p = worldAligned
                        ? transform.TransformPoint(local)
                        : Vector3.Scale(local + Vector3.one * 0.5f, lastScale);
                    Vector3 axisU = worldAligned ? worldU : Vector3.Scale(u, Sign(lastScale));
                    Vector3 axisV = worldAligned ? worldV : Vector3.Scale(v, Sign(lastScale));

                    int i = f * 4 + c;
                    vertices[i] = local;
                    normals[i] = n;
                    uvs[i] = new Vector2(Vector3.Dot(p, axisU), Vector3.Dot(p, axisV)) / textureWorldSize;
                }

                int b = f * 4;
                int t = f * 6;
                triangles[t] = b;
                triangles[t + 1] = b + 1;
                triangles[t + 2] = b + 2;
                triangles[t + 3] = b;
                triangles[t + 4] = b + 2;
                triangles[t + 5] = b + 3;
            }

            mesh.Clear();
            mesh.vertices = vertices;
            mesh.normals = normals;
            mesh.uv = uvs;
            mesh.triangles = triangles;
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
        }

        private static Vector3 Sign(Vector3 v)
        {
            return new Vector3(Mathf.Sign(v.x), Mathf.Sign(v.y), Mathf.Sign(v.z));
        }
    }
}
