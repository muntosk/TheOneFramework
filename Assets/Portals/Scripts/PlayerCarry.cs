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
        private float pickupRange = 3.0f;

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

        private Carryable held;
        private Collider heldCollider;
        private Rigidbody heldRigidbody;
        private bool wasHeldKinematic;

        public bool IsCarrying => held != null;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
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

            held.transform.SetPositionAndRotation(GetClearedHoldPosition(), holdPoint.rotation);
        }

        // The held object's own Collider stays disabled the whole time it's carried (see class
        // comment above), so nothing here uses real physics collision to stop it clipping through
        // walls. Instead: cast from the camera towards holdPoint and, if something solid is in the
        // way, pull the hold position back to just in front of it - same trick as Half-Life 2's
        // gravity gun. Re-enabling the Collider instead would let the object trigger a portal warp
        // on its own while PlayerCarry is still forcibly repositioning it every frame - two systems
        // fighting over the same Transform.
        private Vector3 GetClearedHoldPosition()
        {
            Vector3 origin = aimCamera.transform.position;
            Vector3 toHoldPoint = holdPoint.position - origin;
            float distance = toHoldPoint.magnitude;

            if (distance <= heldClearanceRadius)
            {
                return holdPoint.position;
            }

            Vector3 direction = toHoldPoint / distance;
            if (Physics.SphereCast(origin, heldClearanceRadius, direction, out RaycastHit hit, distance,
                worldLayerMask, QueryTriggerInteraction.Ignore))
            {
                return origin + direction * Mathf.Max(hit.distance - heldClearanceRadius, 0.0f);
            }

            return holdPoint.position;
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

        // Same recursive through-a-portal raycast trick as PortalGun.FirePortal(): if the ray hits
        // a portal surface, re-fire it from the linked portal's far side instead of stopping there,
        // so you can reach through a portal to pick up something on the other side of it.
        private bool RaycastThroughPortals(Vector3 pos, Vector3 dir, float distance, out RaycastHit hit)
        {
            if (!Physics.Raycast(pos, dir, out hit, distance, carryLayerMask))
            {
                return false;
            }

            if (!hit.collider.CompareTag("Portal"))
            {
                return true;
            }

            var inPortal = hit.collider.GetComponent<Portal>();
            if (inPortal == null)
            {
                return false;
            }

            var outPortal = inPortal.OtherPortal;

            Vector3 relativePos = inPortal.transform.InverseTransformPoint(hit.point + dir);
            relativePos = halfTurn * relativePos;
            Vector3 newPos = outPortal.transform.TransformPoint(relativePos);

            Vector3 relativeDir = inPortal.transform.InverseTransformDirection(dir);
            relativeDir = halfTurn * relativeDir;
            Vector3 newDir = outPortal.transform.TransformDirection(relativeDir);

            float remainingDistance = distance - Vector3.Distance(newPos, hit.point);

            return RaycastThroughPortals(newPos, newDir, remainingDistance, out hit);
        }

        private void Drop()
        {
            // Hand off the player's current momentum so the object keeps moving naturally instead
            // of hanging dead in the air the instant physics takes back over.
            Release(controller.velocity);
        }

        private void Launch()
        {
            Release(controller.velocity + aimCamera.transform.forward * launchForce);
        }

        private void Release(Vector3 velocity)
        {
            heldCollider.enabled = true;
            heldRigidbody.isKinematic = wasHeldKinematic;
            heldRigidbody.linearVelocity = velocity;

            held = null;
            heldCollider = null;
            heldRigidbody = null;
        }
    }
}
