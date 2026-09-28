using UnityEngine;

namespace TheOneFramework.Portals
{
    // Implemented by anything that should be able to travel through a Portal.
    // PortalableObject implements this for Rigidbody-driven props; PlayerPortalTraveller
    // implements it for the CharacterController-driven player, since the two need very
    // different logic to redirect position, rotation and velocity through a portal.
    public interface IPortalTraveller
    {
        Transform Transform { get; }
        void SetIsInPortal(Portal inPortal, Portal outPortal, Collider wallCollider);
        void ExitPortal(Collider wallCollider);
        void Warp();
    }
}
