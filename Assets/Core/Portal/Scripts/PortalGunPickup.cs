using UnityEngine;
using UnityEngine.Events;
using TheOneFramework.Audio;

namespace TheOneFramework.Portals
{
    // The portal gun lying on its pedestal: walk into it (trigger collider) and the player's
    // PortalGun gets equipped. Hides the display model and fires onPickedUp, e.g. to start the
    // announcer's explanation. Removes itself when the player already has the gun.
    [RequireComponent(typeof(Collider))]
    public class PortalGunPickup : PlayerActivatable
    {
        [Tooltip("The gun model on the pedestal, hidden once it's picked up.")]
        [SerializeField]
        private GameObject displayModel;

        [Tooltip("Slowly spins and bobs the display model, so it reads as something to pick up.")]
        [SerializeField]
        private bool animateDisplay = true;

        [SerializeField]
        private AudioClip pickupClip;

        [SerializeField, Range(0.0f, 1.0f)]
        private float volume = 0.8f;

        public UnityEvent onPickedUp;

        private PortalGun gun;
        private Vector3 displayStartPos;

        private void Reset() => shouldActivateOnCollision = true;

        private void Start()
        {
            GetComponent<Collider>().isTrigger = true;
            gun = FindAnyObjectByType<PortalGun>();
            if (displayModel != null) displayStartPos = displayModel.transform.localPosition;
            if (gun != null && gun.IsEquipped) Hide();
        }

        private void Update()
        {
            if (!animateDisplay || displayModel == null || !displayModel.activeSelf) return;
            displayModel.transform.Rotate(0.0f, 30.0f * Time.deltaTime, 0.0f, Space.World);
            displayModel.transform.localPosition = displayStartPos + Vector3.up * (Mathf.Sin(Time.time * 2.0f) * 0.05f);
        }

        protected override void OnActivate()
        {
            if (gun == null) gun = FindAnyObjectByType<PortalGun>();
            if (gun == null || gun.IsEquipped) return;

            gun.Equip();
            if (pickupClip != null) GameAudio.PlayAt(pickupClip, transform.position, volume);
            Hide();
            onPickedUp.Invoke();
        }

        private void Hide()
        {
            if (displayModel != null) displayModel.SetActive(false);
            GetComponent<Collider>().enabled = false;
            canBeActivated = false;
        }
    }
}
