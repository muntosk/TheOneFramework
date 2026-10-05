using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace TheOneFramework.Portals
{
    // Parent controller for a Portal-style sliding door made of two panels. Sits on the door's
    // root object with the panels as children (Door1/Door2, ...) - wire a single Open()/Close()
    // call here from a PressurePlate or PoleButton instead of to each panel separately, so the
    // whole root+panels hierarchy can be dragged into Assets as one reusable, self-contained
    // prefab. Each panel keeps its own slide offset, so one can go left and the other right.
    public class Door : MonoBehaviour
    {
        [Serializable]
        public class Panel
        {
            public Transform panelTransform;

            [FormerlySerializedAs("openLocalOffset")] [Tooltip("Local-space offset (relative to the panel's own closed position) it slides to when open.")]
            public Vector3 openLocalPosOffset = new Vector3(-1.0f, 0.0f, 0.0f);
            [Tooltip("Local rotation in degrees (relative to the panel's own closed rotation) it turns to when open. Rotates around the panel's pivot, so put hinged panels under an empty parent at the hinge.")]
            public Vector3 openLocalRotOffset = Vector3.zero;

            [NonSerialized] public Quaternion closedLocalRot;
            [NonSerialized] public Quaternion openLocalRot;
            [NonSerialized] public Quaternion targetLocalRot;

            [NonSerialized] public Vector3 closedLocalPos;
            [NonSerialized] public Vector3 openLocalPos;
            [NonSerialized] public Vector3 targetLocalPos;

            [NonSerialized] public Collider[] colliders;
        }

        [SerializeField]
        private Panel leftPanel = new Panel { openLocalPosOffset = new Vector3(-1.0f, 0.0f, 0.0f) };

        [SerializeField]
        private Panel rightPanel = new Panel { openLocalPosOffset = new Vector3(1.0f, 0.0f, 0.0f) };

        [FormerlySerializedAs("DisableColliderOnOpen")]
        [Tooltip("Turn off the panels' colliders once the door is fully open (back on as soon as it starts closing).")]
        [SerializeField] private bool disableCollidersOnOpen = false;

        [SerializeField]
        private float moveSpeed = 2.0f;

        [Tooltip("Degrees per second for panels that rotate open.")]
        [SerializeField]
        private float rotateSpeed = 90.0f;

        private bool _isOpen;
        // True while panels are still travelling - lets Update skip all work once the door has settled.
        private bool _isMoving;

        private void Awake()
        {
            InitPanel(leftPanel);
            InitPanel(rightPanel);
        }

        private static void InitPanel(Panel panel)
        {
            if (panel?.panelTransform == null)
            {
                return;
            }

            panel.closedLocalRot = panel.panelTransform.localRotation;
            panel.openLocalRot = panel.closedLocalRot * Quaternion.Euler(panel.openLocalRotOffset);
            panel.targetLocalRot = panel.closedLocalRot;

            panel.closedLocalPos = panel.panelTransform.localPosition;
            panel.openLocalPos = panel.closedLocalPos + panel.openLocalPosOffset;
            panel.targetLocalPos = panel.closedLocalPos;

            panel.colliders = panel.panelTransform.GetComponentsInChildren<Collider>();
        }

        private void Update()
        {
            if (!_isMoving)
            {
                return;
            }

            float maxDelta = moveSpeed * Time.deltaTime;
            float maxDegrees = rotateSpeed * Time.deltaTime;
            bool leftArrived = MovePanel(leftPanel, maxDelta, maxDegrees);
            bool rightArrived = MovePanel(rightPanel, maxDelta, maxDegrees);

            if (leftArrived && rightArrived)
            {
                _isMoving = false;
                if (_isOpen && disableCollidersOnOpen)
                {
                    SetCollidersEnabled(false);
                }
            }
        }

        // Steps the panel towards its target; returns true once it's there (or has no transform).
        private static bool MovePanel(Panel panel, float maxDelta, float maxDegrees)
        {
            if (panel?.panelTransform == null)
            {
                return true;
            }

            Transform t = panel.panelTransform;
            t.localPosition = Vector3.MoveTowards(t.localPosition, panel.targetLocalPos, maxDelta);
            t.localRotation = Quaternion.RotateTowards(t.localRotation, panel.targetLocalRot, maxDegrees);

            // MoveTowards/RotateTowards snap exactly onto the target on the last step, so this becomes true.
            return t.localPosition == panel.targetLocalPos && t.localRotation == panel.targetLocalRot;
        }

        // ContextMenu: also callable from the component's ⋮ menu in the Inspector, for testing in Play mode.
        [ContextMenu("Open")]
        public void Open() => SetOpen(true);

        [ContextMenu("Close")]
        public void Close() => SetOpen(false);

        private void SetOpen(bool open)
        {
            Debug.Log($"[Door] {name} {(open ? "OPEN" : "CLOSE")}");
            _isOpen = open;
            _isMoving = true;
            SetTarget(leftPanel, open);
            SetTarget(rightPanel, open);

            // Solid again right away when closing, so nothing slips through the closing gap.
            if (!open)
            {
                SetCollidersEnabled(true);
            }
        }

        private static void SetTarget(Panel panel, bool open)
        {
            if (panel?.panelTransform == null)
            {
                return;
            }

            panel.targetLocalPos = open ? panel.openLocalPos : panel.closedLocalPos;
            panel.targetLocalRot = open ? panel.openLocalRot : panel.closedLocalRot;
        }

        private void SetCollidersEnabled(bool enabled)
        {
            SetCollidersEnabled(leftPanel, enabled);
            SetCollidersEnabled(rightPanel, enabled);
        }

        private static void SetCollidersEnabled(Panel panel, bool enabled)
        {
            if (panel?.colliders == null)
            {
                return;
            }

            foreach (Collider c in panel.colliders)
            {
                c.enabled = enabled;
            }
        }
    }
}
