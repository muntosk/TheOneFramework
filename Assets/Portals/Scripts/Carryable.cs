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
    }
}
