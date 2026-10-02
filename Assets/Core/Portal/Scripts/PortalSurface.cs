using UnityEngine;

namespace TheOneFramework.Portals
{
    // Marks a collider (or a parent of colliders) as a surface portals can be shot onto - the
    // equivalent of Portal 2's white "portalable" panels. Whether PortalGun requires this
    // component at all is controlled by PortalGun's requirePortalSurface toggle.
    public class PortalSurface : MonoBehaviour
    {
        [Tooltip("Untick to explicitly block portals here (e.g. dark metal panels), even when the gun doesn't require a PortalSurface.")]
        [SerializeField]
        private bool allowPortals = true;

        [Tooltip("Place portals at the centre of this panel instead of exactly where you hit it - like Portal 2's placement helpers. Best used on panels roughly the size of a portal.")]
        [SerializeField]
        private bool snapToCenter = false;

        public bool AllowPortals => allowPortals;
        public bool SnapToCenter => snapToCenter;

        // Centre of the hit collider's face, kept on the surface plane through the hit point.
        public Vector3 GetSnappedPoint(Collider hitCollider, Vector3 hitPoint, Vector3 hitNormal)
        {
            Vector3 toCenter = hitCollider.bounds.center - hitPoint;
            return hitPoint + Vector3.ProjectOnPlane(toCenter, hitNormal);
        }
    }
}
