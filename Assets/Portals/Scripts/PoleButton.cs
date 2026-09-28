using UnityEngine;
using UnityEngine.Events;

namespace TheOneFramework.Portals
{
    // Wall/pole-mounted push button. Works with either of the project's two existing interaction
    // paths: set shouldActivateOnCollision so walking into its trigger collider presses it, and/or
    // add a ProximityTrigger child so it gets the "hold E" world-space prompt instead. Defaults to
    // a permanent one-shot press (canRepeat = false) like Portal's big red buttons - tick canRepeat
    // on if you want it re-pressable.
    public class PoleButton : PlayerActivatable
    {
        [Tooltip("Optional - the part of the button that visually sinks in when pressed.")]
        [SerializeField]
        private Transform buttonCap;

        [SerializeField]
        private float pressedDepth = 0.02f;

        [SerializeField]
        private float moveSpeed = 6.0f;

        public UnityEvent onPressed;

        private Vector3 raisedLocalPos;
        private Vector3 pressedLocalPos;
        private Vector3 targetLocalPos;

        private void Reset()
        {
            canRepeat = false;
        }

        private void Awake()
        {
            if (buttonCap != null)
            {
                raisedLocalPos = buttonCap.localPosition;
                pressedLocalPos = raisedLocalPos + Vector3.down * pressedDepth;
                targetLocalPos = raisedLocalPos;
            }
        }

        private void Update()
        {
            if (buttonCap == null)
            {
                return;
            }

            buttonCap.localPosition = Vector3.MoveTowards(buttonCap.localPosition, targetLocalPos, moveSpeed * Time.deltaTime);
        }

        protected override void OnActivate()
        {
            Debug.Log($"[PoleButton] {name} PRESSED");
            targetLocalPos = pressedLocalPos;
            onPressed.Invoke();
        }
    }
}
