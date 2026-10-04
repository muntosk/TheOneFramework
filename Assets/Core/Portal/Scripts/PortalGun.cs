using StarterAssets;
using UnityEngine;

namespace TheOneFramework.Portals
{
    // Aims from aimCamera (screen-centre raycast) rather than this transform, since in third
    // person the character's own forward direction usually doesn't match where the camera looks.
    public class PortalGun : MonoBehaviour
    {
        [SerializeField]
        private PortalPair portals;

        [SerializeField]
        private LayerMask layerMask;

        [SerializeField]
        private Crosshair crosshair;

        [SerializeField]
        private Camera aimCamera;

        [SerializeField]
        private float maxDistance = 250.0f;

        [Tooltip("When ticked, portals only stick to colliders with a PortalSurface component (on themselves or a parent). When unticked, anything in the layer mask works unless its PortalSurface says otherwise.")]
        [SerializeField]
        private bool requirePortalSurface = false;

        // Surfaces tilted less than ~45 degrees from horizontal count as floor/ceiling.
        private const float floorCeilingThreshold = 0.7f;

        // Caps how many portals one shot may pass through, so two portals facing each other
        // can't bounce the shot back and forth forever.
        private const int maxPortalPassthroughs = 8;

        private StarterAssetsInputs input;
        private PlayerCarry carry;

        private bool firePortal1Held;
        private bool firePortal2Held;

        private void Awake()
        {
            input = GetComponent<StarterAssetsInputs>();
            carry = GetComponent<PlayerCarry>();
        }

        private void Update()
        {
            // Block portal firing while carrying something so LMB is free to launch the carried
            // object instead (see PlayerCarry.Launch()).
            bool canFire = carry == null || (!carry.IsCarrying && !carry.LaunchedThisFrame);

            if (canFire && input.firePortal1 && !firePortal1Held)
            {
                FirePortal(0, aimCamera.transform.position, aimCamera.transform.forward, maxDistance);
            }
            else if (canFire && input.firePortal2 && !firePortal2Held)
            {
                FirePortal(1, aimCamera.transform.position, aimCamera.transform.forward, maxDistance);
            }

            firePortal1Held = input.firePortal1;
            firePortal2Held = input.firePortal2;
        }

        private void FirePortal(int portalID, Vector3 pos, Vector3 dir, float distance, int depth = 0)
        {
            if (!Physics.Raycast(pos, dir, out RaycastHit hit, distance, layerMask))
            {
                Debug.Log($"[PortalGun] FirePortal({portalID}) missed - no raycast hit from {pos} dir {dir}");
                return;
            }

            Debug.Log($"[PortalGun] FirePortal({portalID}) hit {hit.collider.name} (layer {hit.collider.gameObject.layer}, tag {hit.collider.tag}) at {hit.point}");

            // If we shoot an existing portal, recursively fire through it to place on the far side.
            if (hit.collider.CompareTag("Portal"))
            {
                var inPortal = hit.collider.GetComponent<Portal>();

                if (inPortal == null || inPortal.OtherPortal == null || !inPortal.OtherPortal.IsPlaced ||
                    depth >= maxPortalPassthroughs)
                {
                    return;
                }

                var outPortal = inPortal.OtherPortal;

                Vector3 relativePos = inPortal.transform.InverseTransformPoint(hit.point + dir);
                relativePos = Quaternion.Euler(0.0f, 180.0f, 0.0f) * relativePos;
                pos = outPortal.transform.TransformPoint(relativePos);

                Vector3 relativeDir = inPortal.transform.InverseTransformDirection(dir);
                relativeDir = Quaternion.Euler(0.0f, 180.0f, 0.0f) * relativeDir;
                dir = outPortal.transform.TransformDirection(relativeDir);

                distance -= hit.distance;

                FirePortal(portalID, pos, dir, distance, depth + 1);
                return;
            }

            var surface = hit.collider.GetComponentInParent<PortalSurface>();
            if (surface != null ? !surface.AllowPortals : requirePortalSurface)
            {
                Debug.Log($"[PortalGun] FirePortal({portalID}) rejected - {hit.collider.name} is not a portal surface");
                return;
            }

            Vector3 placePoint = (surface != null && surface.SnapToCenter)
                ? surface.GetSnappedPoint(hit.collider, hit.point, hit.normal)
                : hit.point;

            var portalForward = -hit.normal;
            var portalRotation = Quaternion.LookRotation(portalForward, GetPortalUp(hit.normal, dir));

            bool wasPlaced = portals.Portals[portalID].PlacePortal(hit.collider, placePoint, portalRotation);
            Debug.Log($"[PortalGun] PlacePortal({portalID}) -> wasPlaced={wasPlaced}");

            if (wasPlaced && crosshair != null)
            {
                crosshair.SetPortalPlaced(portalID, true);
            }
        }

        // Same rules as Portal 2: wall portals always stand upright, while floor/ceiling portals
        // point their top along the shot direction. This used to snap the camera's right vector
        // to a world axis instead, which tilted portals on angled walls and broke the rotation
        // entirely when that axis ended up parallel to the wall normal (steep shots at X-facing walls).
        private static Vector3 GetPortalUp(Vector3 surfaceNormal, Vector3 shotDir)
        {
            bool isFloorOrCeiling = Mathf.Abs(Vector3.Dot(surfaceNormal, Vector3.up)) > floorCeilingThreshold;
            Vector3 reference = isFloorOrCeiling ? shotDir : Vector3.up;
            Vector3 up = Vector3.ProjectOnPlane(reference, surfaceNormal);

            // Shooting dead straight down/up leaves nothing to project - fall back to any
            // horizontal direction lying in the surface.
            if (up.sqrMagnitude < 0.0001f)
            {
                up = Vector3.ProjectOnPlane(Vector3.forward, surfaceNormal);
            }

            return up.normalized;
        }
    }
}
