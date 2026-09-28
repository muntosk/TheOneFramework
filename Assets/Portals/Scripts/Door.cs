using System;
using UnityEngine;

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

            [Tooltip("Local-space offset (relative to the panel's own closed position) it slides to when open.")]
            public Vector3 openLocalOffset = new Vector3(-1.0f, 0.0f, 0.0f);

            [NonSerialized] public Vector3 closedLocalPos;
            [NonSerialized] public Vector3 openLocalPos;
            [NonSerialized] public Vector3 targetLocalPos;
        }

        [SerializeField]
        private Panel leftPanel = new Panel { openLocalOffset = new Vector3(-1.0f, 0.0f, 0.0f) };

        [SerializeField]
        private Panel rightPanel = new Panel { openLocalOffset = new Vector3(1.0f, 0.0f, 0.0f) };

        [SerializeField]
        private float moveSpeed = 2.0f;

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

            panel.closedLocalPos = panel.panelTransform.localPosition;
            panel.openLocalPos = panel.closedLocalPos + panel.openLocalOffset;
            panel.targetLocalPos = panel.closedLocalPos;
        }

        private void Update()
        {
            float maxDelta = moveSpeed * Time.deltaTime;
            MovePanel(leftPanel, maxDelta);
            MovePanel(rightPanel, maxDelta);
        }

        private static void MovePanel(Panel panel, float maxDelta)
        {
            if (panel?.panelTransform == null)
            {
                return;
            }

            panel.panelTransform.localPosition = Vector3.MoveTowards(
                panel.panelTransform.localPosition, panel.targetLocalPos, maxDelta);
        }

        public void Open()
        {
            Debug.Log($"[Door] {name} OPEN");
            SetTarget(leftPanel, true);
            SetTarget(rightPanel, true);
        }

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
        }
    }
}
