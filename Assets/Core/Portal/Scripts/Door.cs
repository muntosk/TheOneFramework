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
        }

        [SerializeField]
        private Panel leftPanel = new Panel { openLocalPosOffset = new Vector3(-1.0f, 0.0f, 0.0f) };

        [SerializeField]
        private Panel rightPanel = new Panel { openLocalPosOffset = new Vector3(1.0f, 0.0f, 0.0f) };

        [SerializeField]
        private float moveSpeed = 2.0f;

        [Tooltip("Degrees per second for panels that rotate open.")]
        [SerializeField]
        private float rotateSpeed = 90.0f;

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
        }

        private void Update()
        {
            float maxDelta = moveSpeed * Time.deltaTime;
            float maxDegrees = rotateSpeed * Time.deltaTime;
            MovePanel(leftPanel, maxDelta, maxDegrees);
            MovePanel(rightPanel, maxDelta, maxDegrees);
        }

        private static void MovePanel(Panel panel, float maxDelta, float maxDegrees)
        {
            if (panel?.panelTransform == null)
            {
                return;
            }

            panel.panelTransform.localPosition = Vector3.MoveTowards(
                panel.panelTransform.localPosition, panel.targetLocalPos, maxDelta);
            panel.panelTransform.localRotation = Quaternion.RotateTowards(
                panel.panelTransform.localRotation, panel.targetLocalRot, maxDegrees);
        }

        // ContextMenu: also callable from the component's ⋮ menu in the Inspector, for testing in Play mode.
        [ContextMenu("Open")]
        public void Open()
        {
            Debug.Log($"[Door] {name} OPEN");
            SetTarget(leftPanel, true);
            SetTarget(rightPanel, true);
        }

        [ContextMenu("Close")]
        public void Close()
        {
            Debug.Log($"[Door] {name} CLOSE");
            SetTarget(leftPanel, false);
            SetTarget(rightPanel, false);
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
    }
}
