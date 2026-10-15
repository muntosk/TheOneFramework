using UnityEngine;

namespace TheOneFramework.Portals
{
    // Marks a physics prop as something PlayerCarry can pick up. Keep PortalableObject on the
    // same object too if it should also be able to travel through a portal on its own (pushed,
    // thrown, dropped near one) - the two components don't need to know about each other.
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(Collider))]
    public class Carryable : MonoBehaviour
    {
        private Vector3 originPosition;
        private Quaternion originRotation;

        private void Awake()
        {
            originPosition = transform.position;
            originRotation = transform.rotation;
        }

        // Puts it back where it was placed (or dispensed), e.g. after falling into a KillZone.
        public void ReturnToOrigin()
        {
            var body = GetComponent<Rigidbody>();
            if (!body.isKinematic)
            {
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }
            body.position = originPosition;
            body.rotation = originRotation;
            transform.SetPositionAndRotation(originPosition, originRotation);
        }
    }
}
