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

        // The point that has to cross the portal plane before the traveller warps - its middle,
        // like Source does. Using the pivot made the player (pivot at the feet) warp the moment
        // their toes touched a floor portal, coming out the other side with no speed at all.
        Vector3 WarpCheckPoint { get; }
        void SetIsInPortal(Portal inPortal, Portal outPortal, Collider wallCollider);
        void ExitPortal(Collider wallCollider);
        void Warp();
    }
}
