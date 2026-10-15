using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace TheOneFramework.Portals
{
    // Pick up / carry mechanic for puzzle props (Portal 2 style companion cube). While held, the
    // object's own Collider is disabled so it can never overlap a Portal's trigger on its own -
    // Portal.cs has no way to warp something it never gets an OnTriggerEnter for. Instead the held
    // object is snapped to holdPoint every LateUpdate, which runs after PlayerPortalTraveller's
    // Warp() (called from Portal.Update()), so it automatically rides along to the player's new,
    // already-warped position - no separate "sync the carried object through the portal" logic
    // needed at all.
    [RequireComponent(typeof(CharacterController))]
    public class PlayerCarry : MonoBehaviour
    {
        [SerializeField]
        private Transform holdPoint;

        [SerializeField]
        private Camera aimCamera;

        [SerializeField]
        private LayerMask carryLayerMask = ~0;

        [SerializeField]
        private float pickupRange = 6.0f;

        [SerializeField]
        private float launchForce = 12.0f;

        [Tooltip("Layers treated as solid world geometry the held object shouldn't clip through. " +
            "Must exclude the Player's own layer (and the carried object's), or the clearance check " +
            "below will immediately hit the player's own collider and pin the object at the camera.")]
        [SerializeField]
        private LayerMask worldLayerMask = ~0;

        [SerializeField]
        private float heldClearanceRadius = 0.4f;

        private static readonly Quaternion halfTurn = Quaternion.Euler(0.0f, 180.0f, 0.0f);

        private CharacterController controller;
        private Transform head;

        private Carryable held;
        private Portal heldPortal;   // the portal the held object is currently shown through, if any
        private PortalPair portalPair;
        private Portal[] Portals
        {
            get
            {
                if (portalPair == null) portalPair = FindAnyObjectByType<PortalPair>();
                return portalPair != null && portalPair.Portals != null ? portalPair.Portals : System.Array.Empty<Portal>();
            }
        }
        private Collider heldCollider;
        private Rigidbody heldRigidbody;
        private bool wasHeldKinematic;

        public bool IsCarrying => held != null;

        // Unity doesn't guarantee whether this Update() or PortalGun's runs first, so on the frame
        // LMB launches the held object, PortalGun could already see IsCarrying == false and fire a
        // portal from the very same click. PortalGun checks this too to block that frame.
        private int launchFrame = -1;
        public bool LaunchedThisFrame => launchFrame == Time.frameCount;

        public void DestroyHeld()
        {
            if (IsCarrying)
            {
                Destroy(held.gameObject);
            }
        }

        // Lets go of the held object and puts it back where it started (e.g. when the player dies).
        public void ReturnHeldToOrigin()
        {
            if (IsCarrying)
            {
                Carryable carryable = held;
                Release(Vector3.zero);
                carryable.ReturnToOrigin();
            }
        }

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            var thirdPerson = GetComponent<StarterAssets.ThirdPersonControllerFixed>();
            if (thirdPerson != null && thirdPerson.CinemachineCameraTarget != null) head = thirdPerson.CinemachineCameraTarget.transform;
        }

        private void Update()
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame)
            {
                if (held != null)
                {
                    Drop();
                }
                else
                {
                    TryPickUp();
                }
            }
            else if (held != null && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                Launch();
            }
#endif
        }

        private void LateUpdate()
        {
            if (held == null)
            {
                return;
            }

            PlaceHeld();
        }

        // The held object's own Collider stays disabled the whole time it's carried (see class
        // comment above), so nothing here uses real physics collision to stop it clipping through
        // walls. Instead: cast from the player's head towards holdPoint and, if something solid is in
        // the way, pull the hold position back to just in front of it - same trick as Half-Life 2's
        // gravity gun. Re-enabling the Collider instead would let the object trigger a portal warp
        // on its own while PlayerCarry is still forcibly repositioning it every frame - two systems
        // fighting over the same Transform.
        // If that line passes through a portal first, the object goes on the far side of it, like in
        // Portal: walk up to a portal and the cube you carry already sticks out of the other one.
        // The head (not the camera) is the origin: it warps together with the player, whereas a
        // third-person camera can still be on the old side for a frame right after a warp.
        private void PlaceHeld()
        {
            Vector3 origin = head != null ? head.position : aimCamera.transform.position;
            Vector3 target = holdPoint.position;
            Quaternion rotation = holdPoint.rotation;
            heldPortal = null;

            Vector3 toTarget = target - origin;
            float distance = toTarget.magnitude;
            if (distance > 0.001f && FirstPortalOnLine(origin, toTarget / distance, distance, out Portal portal, out float portalDistance))
            {
                heldPortal = portal;
                origin = ThroughPortal(portal, origin + toTarget / distance * portalDistance);
                target = ThroughPortal(portal, target);
                rotation = ThroughPortal(portal, rotation);
            }

            held.transform.SetPositionAndRotation(Cleared(origin, target), rotation);
        }

        // The nearest placed portal (with a placed partner) whose plane the line crosses within the
        // portal's rectangle, unless a wall comes first. Worked out from the planes rather than with a
        // raycast against the portals' triggers: the head is often already inside that trigger box
        // when walking up to a portal, and a raycast never hits a collider it starts inside of.
        private bool FirstPortalOnLine(Vector3 origin, Vector3 direction, float distance, out Portal portal, out float portalDistance)
        {
            NearestPortalCrossing(origin, direction, distance, out portal, out portalDistance);

            // A wall between the head and that portal (the wall the portal itself is on sits behind it).
            if (portal != null && Physics.Raycast(origin, direction, portalDistance - 0.01f, worldLayerMask, QueryTriggerInteraction.Ignore))
            {
                portal = null;
            }
            return portal != null;
        }

        private void NearestPortalCrossing(Vector3 origin, Vector3 direction, float distance, out Portal portal, out float portalDistance)
        {
            portal = null;
            portalDistance = float.MaxValue;

            foreach (var candidate in Portals)
            {
                if (candidate == null || !candidate.IsPlaced || !candidate.OtherPortal.IsPlaced) continue;

                Transform t = candidate.transform;
                float along = Vector3.Dot(direction, t.forward);
                if (along <= 0.0f) continue;   // moving away from / along the portal, not into it
                float crossing = Vector3.Dot(t.position - origin, t.forward) / along;
                if (crossing < 0.0f || crossing > distance || crossing >= portalDistance) continue;

                Vector3 local = t.InverseTransformPoint(origin + direction * crossing);
                if (Mathf.Abs(local.x) > 0.5f || Mathf.Abs(local.y) > 0.5f) continue;

                portal = candidate;
                portalDistance = crossing;
            }
        }

        private static Vector3 ThroughPortal(Portal portal, Vector3 point)
        {
            Vector3 local = halfTurn * portal.transform.InverseTransformPoint(point);
            return portal.OtherPortal.transform.TransformPoint(local);
        }

        private static Quaternion ThroughPortal(Portal portal, Quaternion rotation)
        {
            Quaternion local = halfTurn * (Quaternion.Inverse(portal.transform.rotation) * rotation);
            return portal.OtherPortal.transform.rotation * local;
        }

        private static Vector3 ThroughPortalDirection(Portal portal, Vector3 direction)
        {
            Vector3 local = halfTurn * portal.transform.InverseTransformDirection(direction);
            return portal.OtherPortal.transform.TransformDirection(local);
        }

        private Vector3 Cleared(Vector3 origin, Vector3 target)
        {
            Vector3 toTarget = target - origin;
            float distance = toTarget.magnitude;

            if (distance <= heldClearanceRadius)
            {
                return target;
            }

            Vector3 direction = toTarget / distance;
            if (Physics.SphereCast(origin, heldClearanceRadius, direction, out RaycastHit hit, distance,
                worldLayerMask, QueryTriggerInteraction.Ignore) && !InPortalOpening(hit.point))
            {
                return origin + direction * Mathf.Max(hit.distance - heldClearanceRadius, 0.0f);
            }

            return target;
        }

        // A wall hit right where a portal is open isn't in the way - the object is about to go through.
        private bool InPortalOpening(Vector3 point)
        {
            foreach (var portal in Portals)
            {
                if (portal == null || !portal.IsPlaced || !portal.OtherPortal.IsPlaced) continue;
                Vector3 local = portal.transform.InverseTransformPoint(point);
                if (Mathf.Abs(local.x) <= 0.5f && Mathf.Abs(local.y) <= 0.5f && Mathf.Abs(local.z) < 0.3f) return true;
            }
            return false;
        }

        private void TryPickUp()
        {
            if (!RaycastThroughPortals(aimCamera.transform.position, aimCamera.transform.forward,
                pickupRange, out RaycastHit hit))
            {
                return;
            }

            var carryable = hit.collider.GetComponentInParent<Carryable>();
            if (carryable == null)
            {
                return;
            }

            held = carryable;
            heldCollider = carryable.GetComponent<Collider>();
            heldRigidbody = carryable.GetComponent<Rigidbody>();

            heldCollider.enabled = false;
            wasHeldKinematic = heldRigidbody.isKinematic;
            heldRigidbody.isKinematic = true;
        }

        // If the ray crosses a portal before it hits anything solid, carry on from the linked portal's
        // far side, so you can reach through a portal to pick up something on the other side of it.
        // The crossing comes from the portal planes, not from hitting the portal's trigger (which a
        // ray misses when it starts inside it, e.g. standing right in front of the portal).
        private bool RaycastThroughPortals(Vector3 pos, Vector3 dir, float distance, out RaycastHit hit, int depth = 0)
        {
            bool solid = Physics.Raycast(pos, dir, out hit, distance, carryLayerMask, QueryTriggerInteraction.Ignore);

            NearestPortalCrossing(pos, dir, solid ? hit.distance + 0.01f : distance, out Portal portal, out float crossing);
            if (portal == null || depth >= 4)
            {
                return solid;
            }

            Vector3 newDir = ThroughPortalDirection(portal, dir);
            Vector3 newPos = ThroughPortal(portal, pos + dir * crossing) + newDir * 0.01f;
            return RaycastThroughPortals(newPos, newDir, distance - crossing, out hit, depth + 1);
        }

        private void Drop()
        {
            // Hand off the player's current momentum so the object keeps moving naturally instead
            // of hanging dead in the air the instant physics takes back over.
            Release(controller.velocity);
        }

        private void Launch()
        {
            launchFrame = Time.frameCount;
            Release(controller.velocity + aimCamera.transform.forward * launchForce);
        }

        private void Release(Vector3 velocity)
        {
            // Held out of the far side of a portal: the throw/momentum comes out of that portal too.
            if (heldPortal != null) velocity = ThroughPortalDirection(heldPortal, velocity);
            heldPortal = null;

            heldCollider.enabled = true;
            heldRigidbody.isKinematic = wasHeldKinematic;
            heldRigidbody.linearVelocity = velocity;

            held = null;
            heldCollider = null;
            heldRigidbody = null;
        }
    }
}
