using UnityEngine;
using UnityEngine.Events;

namespace TheOneFramework.Portals
{
    // Combines several separate triggers (PressurePlates, PoleButtons, ...) into a single AND
    // condition without any of them - or the Door they feed - needing to know about each other.
    // Wire each source's "pressed" event to Activate() and, if it has one, its "released" event to
    // Deactivate(); this then fires onAllActive/onNotAllActive once every required source is (or
    // stops being) active, which you wire onward to a Door's Open()/Close().
    public class RequireAllTriggers : MonoBehaviour
    {
        [Tooltip("How many Activate() calls need to be currently active before onAllActive fires.")]
        [SerializeField]
        private int requiredCount = 2;

        public UnityEvent onAllActive;
        public UnityEvent onNotAllActive;

        private int activeCount = 0;

        public bool AllActive => activeCount >= requiredCount;

        public void Activate()
        {
            bool wasAllActive = AllActive;
            activeCount++;
            Debug.Log($"[RequireAllTriggers] {name} activeCount={activeCount}/{requiredCount}");

            if (!wasAllActive && AllActive)
            {
                Debug.Log($"[RequireAllTriggers] {name} ALL ACTIVE");
                onAllActive.Invoke();
            }
        }

        public void Deactivate()
        {
            bool wasAllActive = AllActive;
            activeCount = Mathf.Max(0, activeCount - 1);
            Debug.Log($"[RequireAllTriggers] {name} activeCount={activeCount}/{requiredCount}");

            if (wasAllActive && !AllActive)
            {
                Debug.Log($"[RequireAllTriggers] {name} NOT ALL ACTIVE");
                onNotAllActive.Invoke();
            }
        }
    }
}
