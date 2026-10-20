using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using TheOneFramework.Audio;

namespace TheOneFramework.Portals
{
    // Wall/pole-mounted push button. Works with either of the project's two existing interaction
    // paths: set shouldActivateOnCollision so walking into its trigger collider presses it, and/or
    // add a ProximityTrigger child so it gets the "hold E" world-space prompt instead. Defaults to
    // a permanent one-shot press (canRepeat = false) like Portal's big red buttons - tick canRepeat
    // on if you want it re-pressable. Give it a resetAfter time for a Portal-style timer button: it
    // ticks while counting down, then pops back up and fires onReleased (wire Close()/Retract() there).
    public class PoleButton : PlayerActivatable
    {
        [Tooltip("Optional - the part of the button that visually sinks in when pressed.")]
        [SerializeField]
        private Transform buttonCap;

        [SerializeField]
        private float pressedDepth = 0.02f;

        [SerializeField]
        private float moveSpeed = 6.0f;

        [Tooltip("Played at the button when pressed. Leave empty for silence.")]
        [SerializeField]
        private AudioClip pressClip;

        [SerializeField, Range(0.0f, 1.0f)]
        private float volume = 0.8f;

        [Header("Timer")]
        [Tooltip("Seconds until the button pops back up and fires onReleased. 0 = it stays pressed.")]
        [SerializeField, Min(0.0f)]
        private float resetAfter = 0.0f;

        [Tooltip("Played once per second while the timer runs. Leave empty for silence.")]
        [SerializeField]
        private AudioClip tickClip;

        [Tooltip("Played when the timer runs out.")]
        [SerializeField]
        private AudioClip releaseClip;

        public UnityEvent onPressed;
        public UnityEvent onReleased;

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
            if (pressClip != null)
            {
                GameAudio.PlayAt(pressClip, transform.position, volume);
            }
            onPressed.Invoke();

            if (resetAfter > 0.0f)
            {
                StartCoroutine(CountDown());
            }
        }

        private IEnumerator CountDown()
        {
            // Can't be pressed again (or shown as pressable) while it's counting down.
            canBeActivated = false;
            float end = Time.time + resetAfter;
            while (Time.time < end)
            {
                if (tickClip != null)
                {
                    GameAudio.PlayAt(tickClip, transform.position, volume);
                }
                yield return new WaitForSeconds(Mathf.Min(1.0f, end - Time.time));
            }

            Debug.Log($"[PoleButton] {name} RELEASED");
            targetLocalPos = raisedLocalPos;
            if (releaseClip != null)
            {
                GameAudio.PlayAt(releaseClip, transform.position, volume);
            }
            onReleased.Invoke();
            canBeActivated = true;
        }
    }
}
