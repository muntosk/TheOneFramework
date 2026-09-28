using System;
using UnityEngine;

namespace TheOneFramework.Portals
{
    public class PortalFizzleField : MonoBehaviour
    {
        [SerializeField] private PortalPair portalPair;

        private void Awake()
        {
            if (portalPair == null)
            {
                portalPair = FindAnyObjectByType<PortalPair>();            
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                foreach (Portal portal in portalPair.Portals)
                {
                    portal.RemovePortal();
                }

                var playerCarriedObject = other.GetComponent<PlayerCarry>();
                playerCarriedObject.DestroyHeld();
            }

            var carryable = other.GetComponent<Carryable>();
            if (carryable != null)
            {
                Destroy(carryable.gameObject);
            }
            

            
        }
    }
}