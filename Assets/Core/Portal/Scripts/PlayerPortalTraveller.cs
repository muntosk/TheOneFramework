using StarterAssets;
using UnityEngine;

namespace TheOneFramework.Portals
{
    // Warps the CharacterController-driven player through a portal, preserving momentum by
    // reading CharacterController.velocity (its actual last-frame velocity) and handing the
    // redirected result to ThirdPersonControllerFixed.ApplyPortalVelocity, instead of relying on a
    // Rigidbody like PortalableObject does for physics props.
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(ThirdPersonControllerFixed))]
    public class PlayerPortalTraveller : MonoBehaviour, IPortalTraveller
    {
        private static readonly Quaternion halfTurn = Quaternion.Euler(0.0f, 180.0f, 0.0f);

        [Tooltip("How far a wall portal's bottom edge may sit above your feet and still let you walk in without jumping - like stepping over a small lip.")]
        [SerializeField]
        private float walkInHeight = 0.25f;

        [Header("Funneling")]
        [Tooltip("Max sideways speed (m/s) used to steer you toward the middle of a floor portal you're falling onto, like Portal 2 does. 0 turns it off.")]
        [SerializeField]
        private float funnelStrength = 4.0f;

        [Tooltip("How far outside a floor portal's edge your predicted landing spot may be and still get steered in.")]
        [SerializeField]
        private float funnelReach = 1.0f;

        [Tooltip("Only funnel while falling at least this fast, so walking or hopping near a floor portal isn't affected.")]
        [SerializeField]
        private float minFunnelFallSpeed = 2.0f;

        [Tooltip("How quickly the steering kicks in. Higher = snappier.")]
        [SerializeField]
        private float funnelResponsiveness = 8.0f;

        // Surfaces tilted less than ~45 degrees from horizontal count as floor/ceiling (same as PortalGun).
        private const float floorCeilingThreshold = 0.7f;

        private CharacterController controller;
        private ThirdPersonControllerFixed thirdPersonController;

        private Portal inPortal;
        private Portal outPortal;
        private Collider[] wallColliders;
        private Collider[] preIgnoredWalls;
        private Vector3 airVelocity;
        private float lastAirTime = -10.0f;

        public Transform Transform => transform;
        public Vector3 WarpCheckPoint => transform.TransformPoint(controller.center);

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            thirdPersonController = GetComponent<ThirdPersonControllerFixed>();
        }

        public void SetIsInPortal(Portal inPortal, Portal outPortal, Collider[] wallColliders)
        {
            this.inPortal = inPortal;
            this.outPortal = outPortal;
            this.wallColliders = wallColliders;

            Portal.IgnoreWalls(controller, wallColliders, IsOpen(inPortal));
        }

        public void ExitPortal(Collider[] wallColliders)
        {
            // After a warp this is the portal just left; don't turn back on a wall the exit portal
            // shares with it (both portals on one wall), that one is still being passed through.
            foreach (var wall in wallColliders ?? new Collider[0])
            {
                bool stillPassing = !ReferenceEquals(wallColliders, this.wallColliders) &&
                    this.wallColliders != null && System.Array.IndexOf(this.wallColliders, wall) >= 0;
                if (wall != null && !stillPassing) Physics.IgnoreCollision(controller, wall, false);
            }
        }

        // Halfway through a portal: feet past its surface, inside its oval. Dropping into a floor portal
        // at speed takes the body a metre or more below the floor before the warp; hazards under it
        // (KillZone water, RespawnBehaviour's fall threshold) must not count that.
        public bool IsPassingThrough =>
            inPortal != null && inPortal.IsPlaced &&
            IsOpen(inPortal) &&
            inPortal.transform.InverseTransformPoint(transform.position).z > 0.0f &&
            IsWithinPortalRect(inPortal, transform.position + Vector3.up * walkInHeight);

        // A wall is usually one single big collider, so Physics.IgnoreCollision can only turn its
        // collision fully on or off for the whole thing - there's no way to disable just the part
        // behind the portal. So instead of ignoring the wall for as long as you're anywhere inside
        // the (deliberately a bit oversized) portal trigger volume, re-check every frame whether
        // you're actually within the portal's own rectangle and only ignore collision then. Step
        // outside that rectangle (but still within the trigger) and the wall solidifies again.
        private void Update()
        {
            UpdateFunnel();
            UpdateFloorPortalApproach();

            if (inPortal == null)
            {
                return;
            }

            // The portal was shot somewhere else (or removed) since: its old walls are solid again.
            if (!inPortal.IsPlaced || !ReferenceEquals(inPortal.WallColliders, wallColliders))
            {
                Portal.IgnoreWalls(controller, wallColliders, false);
                inPortal = null;
                return;
            }

            // Check a point a little above the feet rather than transform.position itself: the
            // pivot sits exactly at the feet, so for a wall portal whose bottom edge was even
            // slightly above the floor the feet were "below the portal" and the wall stayed solid
            // until you jumped. Checking the capsule's middle instead was far too forgiving.
            Vector3 checkPoint = transform.position + Vector3.up * walkInHeight;
            bool withinPortalRect = IsWithinPortalRect(inPortal, checkPoint);
            Portal.IgnoreWalls(controller, wallColliders, withinPortalRect && IsOpen(inPortal));
        }

        // A portal only leads somewhere once its partner is placed too; until then its wall stays solid.
        private static bool IsOpen(Portal portal) => portal.OtherPortal != null && portal.OtherPortal.IsPlaced;

        private static bool IsWithinPortalRect(Portal portal, Vector3 worldPos)
        {
            Transform t = portal.transform;
            Vector3 offset = worldPos - t.position;

            float horiz = Vector3.Dot(offset, t.right);
            float vert = Vector3.Dot(offset, t.up);

            // A unit Quad's local bounds are +-0.5, so lossyScale directly gives world half-size
            // (same assumption Portal.cs itself uses for HalfWidth/HalfHeight).
            float halfWidth = t.lossyScale.x * 0.5f;
            float halfHeight = t.lossyScale.y * 0.5f;

            // And close to it: without this, standing anywhere in the room in line with the portal
            // counted as "in the portal", turning the wall behind it off from across the room.
            float depth = Vector3.Dot(offset, t.forward);

            return Mathf.Abs(horiz) <= halfWidth && Mathf.Abs(vert) <= halfHeight && Mathf.Abs(depth) <= maxPortalDepth;
        }

        // How far in front of / behind a portal's plane still counts as being in it.
        private const float maxPortalDepth = 1.0f;

        // Falling fast, one frame's move can be a metre or more - enough to land on the floor under a
        // floor portal before its trigger has even noticed you, which kills the fall speed (you'd then
        // sink in slowly and come out the other side with no momentum). So when you're about to reach
        // a floor portal from above, inside its oval, its floor stops being solid a frame early.
        // Also remembers how fast you were going in the air, for Warp.
        private void UpdateFloorPortalApproach()
        {
            if (preIgnoredWalls != null && !(inPortal != null && ReferenceEquals(preIgnoredWalls, wallColliders)))
            {
                Portal.IgnoreWalls(controller, preIgnoredWalls, false);
            }
            preIgnoredWalls = null;

            if (controller.isGrounded) return;
            Vector3 velocity = thirdPersonController.IntendedVelocity;
            airVelocity = velocity;
            lastAirTime = Time.time;
            if (velocity.y > -minFunnelFallSpeed) return;

            Vector3 feet = transform.position;
            float reach = -velocity.y * Time.deltaTime * 3.0f + 0.5f;
            foreach (var portal in Portal.ActivePortals)
            {
                if (portal.OtherPortal == null || !portal.OtherPortal.IsPlaced || !portal.IsPlaced) continue;
                Transform t = portal.transform;
                if (Vector3.Dot(-t.forward, Vector3.up) < floorCeilingThreshold) continue;

                float height = feet.y - t.position.y;
                if (height < -0.5f || height > reach) continue;
                if (!IsWithinPortalRect(portal, new Vector3(feet.x, t.position.y, feet.z))) continue;

                Portal.IgnoreWalls(controller, portal.WallColliders, true);
                preIgnoredWalls = portal.WallColliders;
                return;
            }
        }

        // Portal 2-style funneling: while falling, predict where you'll reach the height of each
        // placed floor portal, and if that spot is near the portal, nudge your sideways speed so
        // you land in its middle. Re-evaluated every frame, so it smoothly corrects instead of
        // snapping, and needs nothing once you're already on course.
        private void UpdateFunnel()
        {
            if (funnelStrength <= 0.0f || controller.isGrounded)
            {
                return;
            }

            Vector3 velocity = controller.velocity;
            float fallSpeed = -velocity.y;
            if (fallSpeed < minFunnelFallSpeed)
            {
                return;
            }

            float gravity = Mathf.Max(0.01f, -thirdPersonController.Gravity);
            Vector3 feet = transform.position;
            Vector3 horizontalVelocity = new Vector3(velocity.x, 0.0f, velocity.z);

            float bestTime = float.MaxValue;
            Vector3 bestCorrection = Vector3.zero;

            foreach (var portal in Portal.ActivePortals)
            {
                if (portal.OtherPortal == null || !portal.OtherPortal.IsPlaced)
                {
                    continue;
                }

                Transform t = portal.transform;
                if (Vector3.Dot(-t.forward, Vector3.up) < floorCeilingThreshold)
                {
                    continue;
                }

                float height = feet.y - t.position.y;
                if (height <= 0.0f)
                {
                    continue;
                }

                // height = fallSpeed * time + 0.5 * gravity * time^2
                float time = (-fallSpeed + Mathf.Sqrt(fallSpeed * fallSpeed + 2.0f * gravity * height)) / gravity;
                Vector3 landing = feet + horizontalVelocity * time;
                Vector3 toCenter = t.position - landing;
                toCenter.y = 0.0f;

                float portalRadius = Mathf.Max(t.lossyScale.x, t.lossyScale.y) * 0.5f;
                if (toCenter.magnitude > portalRadius + funnelReach || time >= bestTime)
                {
                    continue;
                }

                bestTime = time;
                bestCorrection = toCenter / Mathf.Max(time, 0.05f);
            }

            if (bestTime < float.MaxValue)
            {
                Vector3 correction = Vector3.ClampMagnitude(bestCorrection, funnelStrength);
                thirdPersonController.AddExternalVelocity(correction * Mathf.Clamp01(funnelResponsiveness * Time.deltaTime));
            }
        }

        // Coming out of a wall portal, keep the whole upright capsule inside the portal's opening
        // (otherwise e.g. arriving from a floor portal near its edge could put your feet below the
        // exit's bottom edge, inside the floor) and just in front of the wall instead of half in it.
        private Vector3 FitInsideWallPortal(Vector3 center, Transform outTransform)
        {
            Vector3 normal = -outTransform.forward;
            if (Mathf.Abs(normal.y) >= floorCeilingThreshold)
            {
                return center;
            }

            Vector3 scale = transform.lossyScale;
            float radius = controller.radius * Mathf.Max(scale.x, scale.z);
            float halfHeight = Mathf.Max(controller.height * 0.5f, controller.radius) * scale.y;

            Vector3 offset = center - outTransform.position;
            float right = Vector3.Dot(offset, outTransform.right);
            float up = Vector3.Dot(offset, outTransform.up);
            float forward = Vector3.Dot(offset, normal);

            float maxRight = Mathf.Max(0.0f, outTransform.lossyScale.x * 0.5f - radius);
            float maxUp = Mathf.Max(0.0f, outTransform.lossyScale.y * 0.5f - halfHeight);
            right = Mathf.Clamp(right, -maxRight, maxRight);
            up = Mathf.Clamp(up, -maxUp, maxUp);
            forward = Mathf.Max(forward, radius + controller.skinWidth);

            return outTransform.position + outTransform.right * right + outTransform.up * up + normal * forward;
        }

        // The capsule always stays upright, so coming out of a floor portal its middle (which is
        // what crossed the plane) sits right at the exit plane and its lower half would start
        // inside the floor. Push it out along world up/down until it fully clears the plane.
        private Vector3 ClearExitPlane(Vector3 position, Transform outTransform)
        {
            Vector3 normal = -outTransform.forward;
            if (Mathf.Abs(normal.y) < floorCeilingThreshold)
            {
                return position;
            }

            float scale = transform.lossyScale.y;
            float halfHeight = Mathf.Max(controller.height * 0.5f, controller.radius) * scale;
            float centerY = position.y + controller.center.y * scale;
            float clearance = controller.skinWidth + 0.02f;

            // Height of the (possibly slightly tilted) portal plane right at this spot.
            Vector3 p = outTransform.position;
            float planeY = p.y - (normal.x * (position.x - p.x) + normal.z * (position.z - p.z)) / normal.y;

            if (normal.y > 0.0f)
            {
                float bottom = centerY - halfHeight;
                if (bottom < planeY + clearance)
                {
                    position.y += planeY + clearance - bottom;
                }
            }
            else
            {
                float top = centerY + halfHeight;
                if (top > planeY - clearance)
                {
                    position.y -= top - (planeY - clearance);
                }
            }

            return position;
        }

        public void Warp()
        {
            if (inPortal == null || outPortal == null) return;   // its portal was moved meanwhile

            Transform inTransform = inPortal.transform;
            Transform outTransform = outPortal.transform;

            // CharacterController has no persistent velocity vector of its own; .velocity only
            // reflects the displacement from the last Move() call, which is enough to redirect.
            Vector3 currentVelocity = controller.velocity;
            // If that last Move() got cut short (touched down on the portal's floor this frame or
            // just before), use the speed you were still flying at.
            if (Time.time - lastAirTime < 0.25f && airVelocity.sqrMagnitude > currentVelocity.sqrMagnitude)
            {
                currentVelocity = airVelocity;
            }

            // Map the capsule's middle (the point that triggered the warp) rather than the feet,
            // then hang the upright capsule back off it.
            Vector3 relativePos = inTransform.InverseTransformPoint(WarpCheckPoint);
            relativePos = halfTurn * relativePos;
            Vector3 newCenter = outTransform.TransformPoint(relativePos);

            Quaternion relativeRot = Quaternion.Inverse(inTransform.rotation) * transform.rotation;
            relativeRot = halfTurn * relativeRot;
            Quaternion newRotation = outTransform.rotation * relativeRot;

            Vector3 relativeVel = inTransform.InverseTransformDirection(currentVelocity);
            relativeVel = halfTurn * relativeVel;
            Vector3 newVelocity = outTransform.TransformDirection(relativeVel);

            // A CharacterController always assumes world-up, so the player must stay upright even
            // through a floor/ceiling portal - only the horizontal facing (yaw) carries over from
            // the full 3D rotation above, never pitch/roll (which would otherwise tip the capsule
            // onto its side, unlike PortalableObject's Rigidbody props which have no such constraint).
            Vector3 flatForward = newRotation * Vector3.forward;
            flatForward.y = 0.0f;
            if (flatForward.sqrMagnitude < 0.0001f)
            {
                flatForward = transform.forward;
                flatForward.y = 0.0f;
            }
            Quaternion uprightRotation = Quaternion.LookRotation(flatForward.normalized, Vector3.up);
            float yawDelta = Mathf.DeltaAngle(transform.eulerAngles.y, uprightRotation.eulerAngles.y);

            newCenter = FitInsideWallPortal(newCenter, outTransform);
            Vector3 centerOffset = uprightRotation * Vector3.Scale(controller.center, transform.lossyScale);
            Vector3 newPosition = ClearExitPlane(newCenter - centerOffset, outTransform);

            // CharacterController resists a direct transform teleport while enabled.
            controller.enabled = false;
            transform.SetPositionAndRotation(newPosition, uprightRotation);
            controller.enabled = true;

            thirdPersonController.ApplyPortalVelocity(newVelocity, yawDelta);

            Debug.Log($"[Warp] {inPortal.name} -> {outPortal.name} | pos {transform.position:F2} -> {newPosition:F2} | vel {currentVelocity:F2} -> {newVelocity:F2} | rot {transform.eulerAngles:F1} -> {uprightRotation.eulerAngles:F1} (yawDelta {yawDelta:F1})");

            var tmp = inPortal;
            inPortal = outPortal;
            outPortal = tmp;

            // From now on it's the walls behind the portal we came out of that matter - keeping the
            // old ones let you walk through the entry wall later whenever you stood in line with the
            // exit portal.
            wallColliders = inPortal.WallColliders;
            Portal.IgnoreWalls(controller, wallColliders, true);
        }
    }
}
