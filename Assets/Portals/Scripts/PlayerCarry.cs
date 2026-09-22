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

        private CharacterController controller;

        private Carryable held;
        private Collider heldCollider;
        private Rigidbody heldRigidbody;
        private bool wasHeldKinematic;

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
#endif
        }

        private void LateUpdate()
        {
            if (held == null)
            {
                return;
            }

            held.transform.SetPositionAndRotation(holdPoint.position, holdPoint.rotation);
        }

        private void TryPickUp()
        {
            if (!Physics.Raycast(aimCamera.transform.position, aimCamera.transform.forward,
                out RaycastHit hit, pickupRange, carryLayerMask))
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

        private void Drop()
        {
            heldCollider.enabled = true;
            heldRigidbody.isKinematic = wasHeldKinematic;

            // Hand off the player's current momentum so the object keeps moving naturally instead
            // of hanging dead in the air the instant physics takes back over.
            heldRigidbody.linearVelocity = controller.velocity;

            held = null;
            heldCollider = null;
            heldRigidbody = null;
        }
    }
}
