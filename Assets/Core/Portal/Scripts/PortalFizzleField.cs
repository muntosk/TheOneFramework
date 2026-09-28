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
            // When the player walks through it
            if (other.CompareTag("Player"))
            {
                // Just remove all portals
                foreach (Portal portal in portalPair.Portals)
                {
                    portal.RemovePortal();
                }
                var playerCarriedObject = other.GetComponent<PlayerCarry>();
                playerCarriedObject.DestroyHeld();
                
                // Just remove all Carriables
                GameObject[] targets = GameObject.FindGameObjectsWithTag("Fizzler");
        
                foreach (GameObject target in targets)
                {
                    Destroy(target);
                }
            }

            // When a cube flies through the fizzler (rizzler)
            var carryable = other.GetComponent<Carryable>();
            if (carryable != null)
            {
                Destroy(carryable.gameObject);
            }
            

            
        }
    }
}