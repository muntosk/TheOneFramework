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
            bool canFire = carry == null || !carry.IsCarrying;

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

        private void FirePortal(int portalID, Vector3 pos, Vector3 dir, float distance)
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

                if (inPortal == null)
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

                distance -= Vector3.Distance(pos, hit.point);

                FirePortal(portalID, pos, dir, distance);
                return;
            }

            // Orient the portal according to camera look direction and surface direction.
            var cameraRotation = aimCamera.transform.rotation;
            var portalRight = cameraRotation * Vector3.right;

            if (Mathf.Abs(portalRight.x) >= Mathf.Abs(portalRight.z))
            {
                portalRight = (portalRight.x >= 0) ? Vector3.right : -Vector3.right;
            }
            else
            {
                portalRight = (portalRight.z >= 0) ? Vector3.forward : -Vector3.forward;
            }

            var portalForward = -hit.normal;
            var portalUp = -Vector3.Cross(portalRight, portalForward);
            var portalRotation = Quaternion.LookRotation(portalForward, portalUp);

            bool wasPlaced = portals.Portals[portalID].PlacePortal(hit.collider, hit.point, portalRotation);
            Debug.Log($"[PortalGun] PlacePortal({portalID}) -> wasPlaced={wasPlaced}");

            if (wasPlaced && crosshair != null)
            {
                crosshair.SetPortalPlaced(portalID, true);
            }
        }
    }
}
