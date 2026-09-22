using System.Collections.Generic;
using UnityEngine;

namespace TheOneFramework.Portals
{
    [RequireComponent(typeof(BoxCollider))]
    public class Portal : MonoBehaviour
    {
        [field: SerializeField]
        public Portal OtherPortal { get; private set; }

        [SerializeField]
        private Renderer outlineRenderer;

        [field: SerializeField]
        public Color PortalColour { get; private set; }

        [SerializeField]
        private LayerMask placementMask;

        [SerializeField]
        private Transform testTransform;

        private readonly List<IPortalTraveller> portalObjects = new List<IPortalTraveller>();
        // Real-time cooldown per traveller (shared across all portals) after a warp, so landing
        // right at/past the exit plane can't immediately re-trigger another warp back. A spatial
        // dead zone was tried first but depends on how far the traveller's pivot sits from the
        // plane - which varies a lot (e.g. a CharacterController's pivot sitting almost exactly at
        // ground level while walking across a floor portal, vs. well clear of a wall portal's
        // plane) and made floor portals unreliable. A flat time window avoids that entirely.
        private static readonly Dictionary<IPortalTraveller, float> lastWarpTime = new Dictionary<IPortalTraveller, float>();
        private const float warpCooldown = 0.25f;
        public bool IsPlaced { get; private set; } = false;
        private Collider wallCollider;

        // Components.
        public Renderer Renderer { get; private set; }
        private new BoxCollider collider;

        private void Awake()
        {
            collider = GetComponent<BoxCollider>();
            Renderer = GetComponent<Renderer>();

            // This project has "Enter Play Mode Options" set to skip domain reload, so `static`
            // fields survive from one Play session to the next. Without this clear, a stale
            // timestamp from a previous session (e.g. 45s in) would be compared against the new
            // session's Time.time (starting back at 0), making sinceLastWarp permanently negative
            // and blocking every warp until enough real time passes to catch back up.
            lastWarpTime.Clear();
        }

        private void Start()
        {
            outlineRenderer.material.SetColor("_OutlineColour", PortalColour);

            gameObject.SetActive(false);
        }

        private void Update()
        {
            Renderer.enabled = OtherPortal.IsPlaced;

            for (int i = portalObjects.Count - 1; i >= 0; --i)
            {
                var traveller = portalObjects[i];
                float z = transform.InverseTransformPoint(traveller.Transform.position).z;
                float sinceLastWarp = Time.time - (lastWarpTime.TryGetValue(traveller, out var t) ? t : -warpCooldown);

                Debug.Log($"[Portal] {name} tracking {traveller.Transform.name} z={z:F3}");

                if (z > 0.0f && sinceLastWarp > warpCooldown)
                {
                    lastWarpTime[traveller] = Time.time;
                    traveller.Warp();

                    // Don't rely on OnTriggerExit to clean this up: disabling/re-enabling the
                    // CharacterController as part of the teleport (in Warp()) can make Unity's
                    // physics miss the exit event for the portal being left, leaving the traveller
                    // stuck in this list forever and re-checking (and re-warping) from its new,
                    // unrelated position on every subsequent frame.
                    portalObjects.RemoveAt(i);
                    traveller.ExitPortal(wallCollider);
                }
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            Debug.Log($"[Portal] {name} OnTriggerEnter({other.name}) IsPlaced={IsPlaced} wallCollider={wallCollider}");
            var obj = other.GetComponent<IPortalTraveller>();
            if (obj != null)
            {
                portalObjects.Add(obj);
                obj.SetIsInPortal(this, OtherPortal, wallCollider);
                Debug.Log($"[Portal] {name} tracking traveller, count={portalObjects.Count}");
            }
        }

        private void OnTriggerExit(Collider other)
        {
            Debug.Log($"[Portal] {name} OnTriggerExit({other.name})");
            var obj = other.GetComponent<IPortalTraveller>();

            if (obj != null && portalObjects.Contains(obj))
            {
                portalObjects.Remove(obj);
                obj.ExitPortal(wallCollider);
            }
        }

        public bool PlacePortal(Collider wallCollider, Vector3 pos, Quaternion rot)
        {
            testTransform.position = pos;
            testTransform.rotation = rot;
            testTransform.position -= testTransform.forward * 0.001f;

            FixOverhangs();
            FixIntersects();

            if (CheckOverlap())
            {
                this.wallCollider = wallCollider;
                transform.position = testTransform.position;
                transform.rotation = testTransform.rotation;

                gameObject.SetActive(true);
                IsPlaced = true;
                return true;
            }

            return false;
        }

        // Half-extents derived from the portal's actual size (a unit Quad's local bounds are
        // +-0.5, so lossyScale directly gives world half-size) instead of hardcoded numbers -
        // those used to assume a fixed 2x4 portal regardless of how it was actually scaled,
        // which made placement fail unpredictably for a differently-sized portal.
        private float HalfWidth => transform.lossyScale.x * 0.5f;
        private float HalfHeight => transform.lossyScale.y * 0.5f;
        // Smaller = allowed to sit closer to a surface's edge (e.g. flush with a floor seam),
        // at the cost of a bit less protection against the portal overhanging past that edge.
        private const float edgeMargin = 0.02f;

        // Ensure the portal cannot extend past the edge of a surface.
        private void FixOverhangs()
        {
            float halfWidth = HalfWidth;
            float halfHeight = HalfHeight;

            var testPoints = new List<Vector3>
            {
                new Vector3(-(halfWidth + edgeMargin),  0.0f, 0.1f),
                new Vector3(  halfWidth + edgeMargin,   0.0f, 0.1f),
                new Vector3( 0.0f, -(halfHeight + edgeMargin), 0.1f),
                new Vector3( 0.0f,   halfHeight + edgeMargin,  0.1f)
            };

            var testDirs = new List<Vector3>
            {
                 Vector3.right,
                -Vector3.right,
                 Vector3.up,
                -Vector3.up
            };

            var testDists = new List<float>
            {
                halfWidth + edgeMargin, halfWidth + edgeMargin,
                halfHeight + edgeMargin, halfHeight + edgeMargin
            };

            for (int i = 0; i < 4; ++i)
            {
                RaycastHit hit;
                Vector3 raycastPos = testTransform.TransformPoint(testPoints[i]);
                Vector3 raycastDir = testTransform.TransformDirection(testDirs[i]);

                if (Physics.CheckSphere(raycastPos, 0.05f, placementMask))
                {
                    break;
                }
                else if (Physics.Raycast(raycastPos, raycastDir, out hit, testDists[i], placementMask))
                {
                    var offset = hit.point - raycastPos;
                    testTransform.Translate(offset, Space.World);
                }
            }
        }

        // Ensure the portal cannot intersect a section of wall.
        private void FixIntersects()
        {
            var testDirs = new List<Vector3>
            {
                 Vector3.right,
                -Vector3.right,
                 Vector3.up,
                -Vector3.up
            };

            float halfWidth = HalfWidth + edgeMargin;
            float halfHeight = HalfHeight + edgeMargin;
            var testDists = new List<float> { halfWidth, halfWidth, halfHeight, halfHeight };

            for (int i = 0; i < 4; ++i)
            {
                RaycastHit hit;
                Vector3 raycastPos = testTransform.TransformPoint(0.0f, 0.0f, -0.1f);
                Vector3 raycastDir = testTransform.TransformDirection(testDirs[i]);

                if (Physics.Raycast(raycastPos, raycastDir, out hit, testDists[i], placementMask))
                {
                    var offset = (hit.point - raycastPos);
                    var newOffset = -raycastDir * (testDists[i] - offset.magnitude);
                    testTransform.Translate(newOffset, Space.World);
                }
            }
        }

        // Once positioning has taken place, ensure the portal isn't intersecting anything.
        private bool CheckOverlap()
        {
            float halfWidth = HalfWidth;
            float halfHeight = HalfHeight;
            var checkExtents = new Vector3(halfWidth - edgeMargin, halfHeight - edgeMargin, 0.05f);

            var checkPositions = new Vector3[]
            {
                testTransform.position + testTransform.TransformVector(new Vector3( 0.0f,  0.0f, -0.1f)),

                testTransform.position + testTransform.TransformVector(new Vector3(-halfWidth, -halfHeight, -0.1f)),
                testTransform.position + testTransform.TransformVector(new Vector3(-halfWidth,  halfHeight, -0.1f)),
                testTransform.position + testTransform.TransformVector(new Vector3( halfWidth, -halfHeight, -0.1f)),
                testTransform.position + testTransform.TransformVector(new Vector3( halfWidth,  halfHeight, -0.1f)),

                testTransform.TransformVector(new Vector3(0.0f, 0.0f, 0.2f))
            };

            // Ensure the portal does not intersect walls.
            var intersections = Physics.OverlapBox(checkPositions[0], checkExtents, testTransform.rotation, placementMask);

            if (intersections.Length > 1)
            {
                Debug.Log($"[Portal] {name} CheckOverlap failed: {intersections.Length} overlapping colliders ({string.Join(", ", System.Array.ConvertAll(intersections, c => c.name))})");
                return false;
            }
            else if (intersections.Length == 1)
            {
                // We are allowed to intersect the old portal position.
                if (intersections[0] != collider)
                {
                    Debug.Log($"[Portal] {name} CheckOverlap failed: overlapping {intersections[0].name}, not our own collider");
                    return false;
                }
            }

            // Ensure the portal corners overlap a surface.
            bool isOverlapping = true;

            var cornerNames = new[] { "bottom-left", "top-left", "bottom-right", "top-right" };
            for (int i = 1; i < checkPositions.Length - 1; ++i)
            {
                bool hit = Physics.Linecast(checkPositions[i],
                    checkPositions[i] + checkPositions[checkPositions.Length - 1], placementMask);

                if (!hit)
                {
                    Debug.Log($"[Portal] {name} CheckOverlap failed: {cornerNames[i - 1]} corner has no surface underneath it");
                }

                isOverlapping &= hit;
            }

            return isOverlapping;
        }

        public void RemovePortal()
        {
            gameObject.SetActive(false);
            IsPlaced = false;
        }
    }
}
