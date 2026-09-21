using StarterAssets;
using UnityEngine;

namespace TheOneFramework.Portals
{
    // Warps the CharacterController-driven player through a portal, preserving momentum by
    // reading CharacterController.velocity (its actual last-frame velocity) and handing the
    // redirected result to ThirdPersonController.ApplyPortalVelocity, instead of relying on a
    // Rigidbody like PortalableObject does for physics props.
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(ThirdPersonController))]
    public class PlayerPortalTraveller : MonoBehaviour, IPortalTraveller
    {
        private static readonly Quaternion halfTurn = Quaternion.Euler(0.0f, 180.0f, 0.0f);

        private CharacterController controller;
        private ThirdPersonController thirdPersonController;

        private Portal inPortal;
        private Portal outPortal;
        private Collider wallCollider;

        public Transform Transform => transform;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            thirdPersonController = GetComponent<ThirdPersonController>();
        }

        public void SetIsInPortal(Portal inPortal, Portal outPortal, Collider wallCollider)
        {
            this.inPortal = inPortal;
            this.outPortal = outPortal;
            this.wallCollider = wallCollider;

            Physics.IgnoreCollision(controller, wallCollider);
        }

        public void ExitPortal(Collider wallCollider)
        {
            Physics.IgnoreCollision(controller, wallCollider, false);
        }

        public void Warp()
        {
            Transform inTransform = inPortal.transform;
            Transform outTransform = outPortal.transform;

            // CharacterController has no persistent velocity vector of its own; .velocity only
            // reflects the displacement from the last Move() call, which is enough to redirect.
            Vector3 currentVelocity = controller.velocity;

            Vector3 relativePos = inTransform.InverseTransformPoint(transform.position);
            relativePos = halfTurn * relativePos;
            Vector3 newPosition = outTransform.TransformPoint(relativePos);

            Quaternion relativeRot = Quaternion.Inverse(inTransform.rotation) * transform.rotation;
            relativeRot = halfTurn * relativeRot;
            Quaternion newRotation = outTransform.rotation * relativeRot;

            Vector3 relativeVel = inTransform.InverseTransformDirection(currentVelocity);
            relativeVel = halfTurn * relativeVel;
            Vector3 newVelocity = outTransform.TransformDirection(relativeVel);

            float yawDelta = Mathf.DeltaAngle(transform.eulerAngles.y, newRotation.eulerAngles.y);

            // CharacterController resists a direct transform teleport while enabled.
            controller.enabled = false;
            transform.SetPositionAndRotation(newPosition, newRotation);
            controller.enabled = true;

            thirdPersonController.ApplyPortalVelocity(newVelocity, yawDelta);

            var tmp = inPortal;
            inPortal = outPortal;
            outPortal = tmp;
        }
    }
}
