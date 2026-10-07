using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using Random = UnityEngine.Random;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace TheOneFramework.Utilities
{
    // Edit-time set dressing: scatters a weighted mix of prefabs (debris, decals, ...) over whatever
    // surface lies below this object. Rays are cast along this object's -up, so for a wall just rotate
    // it so its green arrow points away from the wall. Hit Scatter in the Inspector to (re)roll; the
    // results are ordinary child objects, so you can still move or delete single pieces afterwards.
    public class DressingScatter : MonoBehaviour
    {
        [Serializable]
        public class Entry
        {
            public GameObject prefab;
            [Min(0.0f)] public float weight = 1.0f;
        }

        [SerializeField] private Entry[] entries = Array.Empty<Entry>();

        [SerializeField, Min(0)] private int count = 12;

        [Tooltip("Radius of the disc (on this object's local XZ plane) the rays start from.")]
        [SerializeField, Min(0.0f)] private float radius = 2.0f;

        [Tooltip("How far each ray looks for a surface.")]
        [SerializeField, Min(0.0f)] private float maxDistance = 3.0f;

        [SerializeField] private LayerMask surfaceMask = ~0;

        [SerializeField] private Vector2 scaleRange = new Vector2(0.8f, 1.2f);

        [Tooltip("Extra random tilt for non-decal pieces, so debris doesn't all lie perfectly flat.")]
        [SerializeField, Range(0.0f, 90.0f)] private float maxTilt = 0.0f;

        [Tooltip("Same seed = same layout. Untick 'New Seed Each Time' to keep a layout you like reproducible.")]
        [SerializeField] private int seed;

        [SerializeField] private bool newSeedEachTime = true;

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1.0f, 0.6f, 0.1f, 0.8f);
            Gizmos.matrix = transform.localToWorldMatrix;
            const int segments = 32;
            for (int i = 0; i < segments; i++)
            {
                float a0 = i / (float)segments * Mathf.PI * 2.0f;
                float a1 = (i + 1) / (float)segments * Mathf.PI * 2.0f;
                Gizmos.DrawLine(new Vector3(Mathf.Cos(a0), 0, Mathf.Sin(a0)) * radius,
                                new Vector3(Mathf.Cos(a1), 0, Mathf.Sin(a1)) * radius);
            }
            Gizmos.DrawLine(Vector3.zero, Vector3.down * maxDistance);
        }

#if UNITY_EDITOR
        public void Scatter()
        {
            Clear();

            float totalWeight = 0.0f;
            foreach (Entry e in entries)
            {
                if (e?.prefab != null) totalWeight += e.weight;
            }
            if (totalWeight <= 0.0f)
            {
                Debug.LogWarning($"[DressingScatter] {name} has no prefabs to scatter.");
                return;
            }

            if (newSeedEachTime)
            {
                seed = Random.Range(int.MinValue, int.MaxValue);
                EditorUtility.SetDirty(this);
            }
            Random.State oldState = Random.state;
            Random.InitState(seed);

            try
            {
                for (int i = 0; i < count; i++)
                {
                    Vector2 disc = Random.insideUnitCircle * radius;
                    Vector3 origin = transform.TransformPoint(new Vector3(disc.x, 0.0f, disc.y));
                    if (!TryHit(origin, -transform.up, out RaycastHit hit))
                    {
                        continue;
                    }

                    Place(PickPrefab(totalWeight), hit);
                }
            }
            finally
            {
                Random.state = oldState;
            }
        }

        public void Clear()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Undo.DestroyObjectImmediate(transform.GetChild(i).gameObject);
            }
        }

        // Nearest hit that isn't one of our own scattered pieces.
        private bool TryHit(Vector3 origin, Vector3 dir, out RaycastHit best)
        {
            best = default;
            float bestDistance = float.MaxValue;
            foreach (RaycastHit h in Physics.RaycastAll(origin, dir, maxDistance, surfaceMask, QueryTriggerInteraction.Ignore))
            {
                if (h.distance < bestDistance && !h.transform.IsChildOf(transform))
                {
                    best = h;
                    bestDistance = h.distance;
                }
            }
            return bestDistance < float.MaxValue;
        }

        private GameObject PickPrefab(float totalWeight)
        {
            float pick = Random.value * totalWeight;
            foreach (Entry e in entries)
            {
                if (e?.prefab == null) continue;
                pick -= e.weight;
                if (pick <= 0.0f) return e.prefab;
            }
            return entries[entries.Length - 1].prefab;
        }

        private void Place(GameObject prefab, RaycastHit hit)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, transform);
            Undo.RegisterCreatedObjectUndo(go, "Scatter dressing");

            float spin = Random.Range(0.0f, 360.0f);
            go.transform.position = hit.point;

            if (go.GetComponent<DecalProjector>() != null)
            {
                // Projector looks along its forward, so point that into the surface.
                go.transform.rotation = Quaternion.AngleAxis(spin, hit.normal) * Quaternion.LookRotation(-hit.normal, AnyPerpendicular(hit.normal));
            }
            else
            {
                Quaternion tilt = Quaternion.Euler(Random.Range(-maxTilt, maxTilt), 0.0f, Random.Range(-maxTilt, maxTilt));
                go.transform.rotation = Quaternion.FromToRotation(Vector3.up, hit.normal)
                                        * Quaternion.AngleAxis(spin, Vector3.up)
                                        * tilt
                                        * prefab.transform.rotation;
            }

            go.transform.localScale = prefab.transform.localScale * Random.Range(scaleRange.x, scaleRange.y);
        }

        private static Vector3 AnyPerpendicular(Vector3 normal)
        {
            Vector3 reference = Mathf.Abs(Vector3.Dot(normal, Vector3.up)) > 0.9f ? Vector3.forward : Vector3.up;
            return Vector3.ProjectOnPlane(reference, normal).normalized;
        }
#endif
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(DressingScatter))]
    public class DressingScatterEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            EditorGUILayout.Space();
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Scatter"))
                {
                    foreach (UnityEngine.Object t in targets) ((DressingScatter)t).Scatter();
                }
                if (GUILayout.Button("Clear"))
                {
                    foreach (UnityEngine.Object t in targets) ((DressingScatter)t).Clear();
                }
            }
        }
    }
#endif
}
