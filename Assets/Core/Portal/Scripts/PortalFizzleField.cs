using System;
using UnityEngine;
using TheOneFramework.Audio;

namespace TheOneFramework.Portals
{
    public class PortalFizzleField : MonoBehaviour
    {
        [SerializeField] private PortalPair portalPair;

        [Header("Audio")]
        [Tooltip("Hum that loops on the field itself. Leave empty for silence.")]
        [SerializeField] private AudioClip loopClip;
        [Tooltip("Played when the player walks through while portals are open.")]
        [SerializeField] private AudioClip[] portalFizzleClips;
        [Tooltip("Played where a cube gets fizzled.")]
        [SerializeField] private AudioClip objectFizzleClip;
        [SerializeField, Range(0.0f, 1.0f)] private float volume = 0.8f;

        private void Awake()
        {
            if (portalPair == null)
            {
                portalPair = FindAnyObjectByType<PortalPair>();
            }

            if (loopClip != null)
            {
                var loopSource = gameObject.AddComponent<AudioSource>();
                loopSource.clip = loopClip;
                loopSource.loop = true;
                loopSource.spatialBlend = 1.0f;
                loopSource.volume = volume;
                GameAudio.Route(loopSource, AudioChannel.Ambience);
                loopSource.Play();
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            // When the player walks through it
            if (other.CompareTag("Player"))
            {
                bool anyPortalOpen = false;
                foreach (Portal portal in portalPair.Portals)
                {
                    anyPortalOpen |= portal.IsPlaced;
                }
                if (anyPortalOpen && portalFizzleClips != null && portalFizzleClips.Length > 0)
                {
                    GameAudio.PlayAt(portalFizzleClips[UnityEngine.Random.Range(0, portalFizzleClips.Length)], other.transform.position, volume);
                }

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
                if (objectFizzleClip != null)
                {
                    GameAudio.PlayAt(objectFizzleClip, carryable.transform.position, volume);
                }
                Destroy(carryable.gameObject);
            }



        }
    }
}
