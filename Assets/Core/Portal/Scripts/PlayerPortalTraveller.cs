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

        private CharacterController controller;
        private ThirdPersonControllerFixed thirdPersonController;

        private Portal inPortal;
        private Portal outPortal;
        private Collider wallCollider;

        public Transform Transform => transform;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            thirdPersonController = GetComponent<ThirdPersonControllerFixed>();
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

        // A wall is usually one single big collider, so Physics.IgnoreCollision can only turn its
        // collision fully on or off for the whole thing - there's no way to disable just the part
        // behind the portal. So instead of ignoring the wall for as long as you're anywhere inside
        // the (deliberately a bit oversized) portal trigger volume, re-check every frame whether
        // you're actually within the portal's own rectangle and only ignore collision then. Step
        // outside that rectangle (but still within the trigger) and the wall solidifies again.
        private void Update()
        {
            if (inPortal == null)
            {
                return;
            }

            bool withinPortalRect = IsWithinPortalRect(inPortal, transform.position);
            Physics.IgnoreCollision(controller, wallCollider, withinPortalRect);
        }

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

            return Mathf.Abs(horiz) <= halfWidth && Mathf.Abs(vert) <= halfHeight;
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

            // CharacterController resists a direct transform teleport while enabled.
            controller.enabled = false;
            transform.SetPositionAndRotation(newPosition, uprightRotation);
            controller.enabled = true;

            thirdPersonController.ApplyPortalVelocity(newVelocity, yawDelta);

            Debug.Log($"[Warp] {inPortal.name} -> {outPortal.name} | pos {transform.position:F2} -> {newPosition:F2} | vel {currentVelocity:F2} -> {newVelocity:F2} | rot {transform.eulerAngles:F1} -> {uprightRotation.eulerAngles:F1} (yawDelta {yawDelta:F1})");

            var tmp = inPortal;
            inPortal = outPortal;
            outPortal = tmp;
        }
    }
}
