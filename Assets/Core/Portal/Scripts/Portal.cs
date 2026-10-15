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

        [Tooltip("Seconds the oval takes to grow open after being placed.")]
        [SerializeField]
        private float openDuration = 0.2f;

        private static readonly int openID = Shader.PropertyToID("_Open");
        private static readonly int outlineColourID = Shader.PropertyToID("_OutlineColour");
        private float placedTime = float.NegativeInfinity;

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
        // The surface it was shot onto plus anything else solid right behind it (e.g. a black wall a
        // white panel is mounted on) - travellers stop colliding with all of these while inside the
        // portal, otherwise that wall behind the panel would still block them.
        private Collider[] wallColliders = new Collider[0];
        public Collider[] WallColliders => wallColliders;

        // Portals that are currently placed (a portal's GameObject is only active while placed).
        public static readonly List<Portal> ActivePortals = new List<Portal>();

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

        private void OnEnable()
        {
            ActivePortals.Add(this);
        }

        private void OnDisable()
        {
            ActivePortals.Remove(this);
        }

        private void Start()
        {
            outlineRenderer.material.SetColor(outlineColourID, PortalColour);
            Renderer.material.SetColor(outlineColourID, PortalColour);

            gameObject.SetActive(false);
        }

        private void Update()
        {
            Renderer.enabled = OtherPortal.IsPlaced;
            UpdateOpenAnimation();

            for (int i = portalObjects.Count - 1; i >= 0; --i)
            {
                var traveller = portalObjects[i];
                float z = transform.InverseTransformPoint(traveller.WarpCheckPoint).z;
                float sinceLastWarp = Time.time - (lastWarpTime.TryGetValue(traveller, out var t) ? t : -warpCooldown);

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
                    traveller.ExitPortal(wallColliders);
                }
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            Debug.Log($"[Portal] {name} OnTriggerEnter({other.name}) IsPlaced={IsPlaced} walls={wallColliders.Length}");
            var obj = other.GetComponent<IPortalTraveller>();
            if (obj != null)
            {
                portalObjects.Add(obj);
                obj.SetIsInPortal(this, OtherPortal, wallColliders);
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
                obj.ExitPortal(wallColliders);
            }
        }

        // Grows the oval from a point to full size with a slight overshoot, like Portal 2.
        private void UpdateOpenAnimation()
        {
            float t = Mathf.Clamp01((Time.time - placedTime) / Mathf.Max(openDuration, 0.0001f));
            float u = t - 1.0f;
            float open = 1.0f + 2.2f * u * u * u + 1.2f * u * u;

            Renderer.material.SetFloat(openID, open);
            outlineRenderer.material.SetFloat(openID, open);
        }

        // Turns collision between a traveller and every wall collider behind a portal off or on.
        public static void IgnoreWalls(Collider traveller, Collider[] walls, bool ignore)
        {
            if (walls == null) return;
            foreach (Collider wall in walls)
            {
                if (wall != null) Physics.IgnoreCollision(traveller, wall, ignore);
            }
        }

        // The surface hit plus every static solid collider in a slab just behind the portal (its own
        // rectangle, half a metre deep). Travellers move along +forward to go through, so that's "behind".
        private Collider[] CollidersBehind(Collider surface)
        {
            const float depth = 0.5f;
            Vector3 halfExtents = new Vector3(transform.lossyScale.x * 0.5f, transform.lossyScale.y * 0.5f, depth * 0.5f);
            Vector3 centre = transform.position + transform.forward * (depth * 0.5f);

            var walls = new List<Collider> { surface };
            foreach (Collider c in Physics.OverlapBox(centre, halfExtents, transform.rotation, ~0, QueryTriggerInteraction.Ignore))
            {
                if (c != surface && c.attachedRigidbody == null && !(c is CharacterController)) walls.Add(c);
            }
            return walls.ToArray();
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
                transform.position = testTransform.position;
                transform.rotation = testTransform.rotation;
                this.wallColliders = CollidersBehind(wallCollider);

                gameObject.SetActive(true);
                IsPlaced = true;
                placedTime = Time.time;
                UpdateOpenAnimation();
                return true;
            }

            return false;
        }

        // Half-extents derived from the portal's actual size (a unit Quad's local bounds are
        // +-0.5, so lossyScale directly gives world half-size) instead of hardcoded numbers -
        // those used to assume a fixed 2x4 portal regardless of how it was actually scaled,
        // which made placement fail unpredictably for a differently-sized portal.
        // The outline quad is bigger than the portal itself (it's a child scaled 1.2 x 1.1), so
        // size placement off the outline when there is one - otherwise the portal fits but its
        // coloured border hangs past the surface's edge.
        private Vector3 PlacementScale => outlineRenderer != null ? outlineRenderer.transform.lossyScale : transform.lossyScale;
        private float HalfWidth => PlacementScale.x * 0.5f;
        private float HalfHeight => PlacementScale.y * 0.5f;

        // testTransform is a child of this (scaled) portal, so its TransformPoint/TransformVector
        // would stretch every offset by that scale again - doubling all vertical offsets for a
        // 1x2 portal, which used to stop portals from getting within ~1m of a surface's top or
        // bottom edge. HalfWidth/HalfHeight are already in world units, so only rotate them.
        private Vector3 TestPoint(Vector3 localOffset)
        {
            return testTransform.position + testTransform.rotation * localOffset;
        }
        // Smaller = allowed to sit closer to a surface's edge (e.g. flush with a floor seam),
        // at the cost of a bit less protection against the portal overhanging past that edge.
        private const float edgeMargin = 0.02f;

        // True when the surface at this point of the portal (x/y in the portal's plane) accepts portals.
        // Just finding *a* collider there isn't enough: white panels sit on top of black walls, so
        // past a panel's edge there's still a wall - only not one a portal may hang over.
        private bool OnPortalableSurface(Vector2 localPoint)
        {
            Vector3 from = TestPoint(new Vector3(localPoint.x, localPoint.y, -0.1f));
            var hits = Physics.RaycastAll(from, testTransform.forward, 0.2f, placementMask, QueryTriggerInteraction.Ignore);

            // The nearest hit that isn't one of the portals themselves.
            RaycastHit? nearest = null;
            foreach (var hit in hits)
            {
                if (hit.collider.GetComponentInParent<Portal>() != null) continue;
                if (nearest == null || hit.distance < nearest.Value.distance) nearest = hit;
            }
            if (nearest == null) return false;

            var surface = nearest.Value.collider.GetComponentInParent<PortalSurface>();
            return surface == null || surface.AllowPortals;
        }

        // Ensure the portal cannot extend past the edge of a (portalable) surface: while an edge's
        // middle isn't over one, slide the portal inwards a little.
        private void FixOverhangs()
        {
            float halfWidth = HalfWidth + edgeMargin;
            float halfHeight = HalfHeight + edgeMargin;
            var edges = new[]
            {
                new Vector2(-halfWidth, 0.0f), new Vector2(halfWidth, 0.0f),
                new Vector2(0.0f, -halfHeight), new Vector2(0.0f, halfHeight)
            };

            const float step = 0.02f;
            foreach (var edge in edges)
            {
                Vector3 inwards = testTransform.rotation * -(Vector3)edge.normalized;
                float maxShift = edge.magnitude * 2.0f;
                for (float moved = 0.0f; moved < maxShift && !OnPortalableSurface(edge); moved += step)
                {
                    testTransform.position += inwards * step;
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
                Vector3 raycastPos = TestPoint(new Vector3(0.0f, 0.0f, -0.1f));
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

            // Ensure the portal does not intersect walls.
            var intersections = Physics.OverlapBox(TestPoint(new Vector3(0.0f, 0.0f, -0.1f)), checkExtents, testTransform.rotation, placementMask);

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

            // Ensure the portal corners overlap a surface that accepts portals.
            bool isOverlapping = true;

            float cornerX = halfWidth - edgeMargin, cornerY = halfHeight - edgeMargin;
            var corners = new[]
            {
                new Vector2(-cornerX, -cornerY), new Vector2(-cornerX, cornerY),
                new Vector2(cornerX, -cornerY), new Vector2(cornerX, cornerY)
            };
            var cornerNames = new[] { "bottom-left", "top-left", "bottom-right", "top-right" };
            for (int i = 0; i < corners.Length; ++i)
            {
                bool onSurface = OnPortalableSurface(corners[i]);
                if (!onSurface)
                {
                    Debug.Log($"[Portal] {name} CheckOverlap failed: {cornerNames[i]} corner isn't over a portal surface");
                }
                isOverlapping &= onSurface;
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
