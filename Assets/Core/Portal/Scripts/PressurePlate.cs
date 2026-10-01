using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace TheOneFramework.Portals
{
    // Floor button that stays pressed for as long as something heavy enough (the player, a
    // Carryable cube, or any other physics prop) rests on it, and releases again once the last
    // such thing leaves. Unlike PlayerActivatable's one-shot Activate(), a Portal floor button
    // needs a continuously tracked pressed/released state, so this doesn't reuse that base class.
    [RequireComponent(typeof(Collider))]
    public class PressurePlate : MonoBehaviour
    {
        [Tooltip("Optional - the part of the button that visually sinks down while pressed (e.g. a " +
            "child cylinder sitting on top of the base). Leave empty for a plate with no moving part.")]
        [SerializeField]
        private Transform plateTop;

        [SerializeField]
        private float pressedDepth = 0.05f;

        [SerializeField]
        private float moveSpeed = 6.0f;

        public UnityEvent onPressed;
        public UnityEvent onReleased;

        private readonly HashSet<Collider> weightsOnPlate = new HashSet<Collider>();

        private Vector3 raisedLocalPos;
        private Vector3 pressedLocalPos;
        private Vector3 targetLocalPos;

        public bool IsPressed => weightsOnPlate.Count > 0;

        private void Awake()
        {
            GetComponent<Collider>().isTrigger = true;

            if (plateTop != null)
            {
                raisedLocalPos = plateTop.localPosition;
                pressedLocalPos = raisedLocalPos + Vector3.down * pressedDepth;
                targetLocalPos = raisedLocalPos;
            }
        }

        private void Update()
        {
            PruneStaleWeights();

            if (plateTop == null)
            {
                return;
            }

            targetLocalPos = IsPressed ? pressedLocalPos : raisedLocalPos;
            plateTop.localPosition = Vector3.MoveTowards(plateTop.localPosition, targetLocalPos, moveSpeed * Time.deltaTime);
        }

        // Unity never calls OnTriggerExit for a collider that gets disabled or destroyed while
        // still overlapping the trigger (e.g. PlayerCarry disables a Carryable's Collider the
        // moment it's picked up off a plate), so weightsOnPlate can otherwise hold a stale entry
        // forever and leave the plate permanently pressed. Sweep those out every frame instead.
        private void PruneStaleWeights()
        {
            if (weightsOnPlate.Count == 0)
            {
                return;
            }

            bool wasPressed = IsPressed;
            weightsOnPlate.RemoveWhere(other => other == null || !other.enabled);

            if (wasPressed && !IsPressed)
            {
                Debug.Log($"[PressurePlate] {name} RELEASED");
                onReleased.Invoke();
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!IsWeight(other))
            {
                return;
            }

            bool wasPressed = IsPressed;
            weightsOnPlate.Add(other);
            Debug.Log($"[PressurePlate] {name} <- {other.name} entered (weightsOnPlate={weightsOnPlate.Count})");

            if (!wasPressed && IsPressed)
            {
                Debug.Log($"[PressurePlate] {name} PRESSED");
                onPressed.Invoke();
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (!weightsOnPlate.Remove(other))
            {
                return;
            }

            Debug.Log($"[PressurePlate] {name} <- {other.name} left (weightsOnPlate={weightsOnPlate.Count})");

            if (!IsPressed)
            {
                Debug.Log($"[PressurePlate] {name} RELEASED");
                onReleased.Invoke();
            }
        }

        // Weight-bearing = the player's CharacterController or any physics prop's Rigidbody
        // (Carryable cubes always have one, see Carryable.cs). Checked via GetComponentInParent so
        // this keeps working regardless of which child collider actually touches the trigger.
        private static bool IsWeight(Collider other)
        {
            return other.GetComponentInParent<CharacterController>() != null
                || other.GetComponentInParent<Rigidbody>() != null;
        }
    }
}
