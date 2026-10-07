using System.Collections.Generic;
using UnityEngine;
using TheOneFramework.Audio;

namespace TheOneFramework.Portals
{
    // Portal-style panel that folds out of a wall or rises out of the floor. Sits on the platform's
    // root; movingPart is the child that animates between its placed (retracted) pose and that pose
    // plus the extended offsets. Wire Extend()/Retract()/Toggle() from a PressurePlate or PoleButton,
    // like Door. Rotation happens around movingPart's own pivot, so for a fold-out panel put the
    // panel under an empty at the hinge. Whoever stands on it is carried along while it moves, and a
    // kinematic Rigidbody on movingPart (added automatically) lets it push cubes properly.
    public class ExtendingPlatform : MonoBehaviour
    {
        [SerializeField]
        private Transform movingPart;

        [Tooltip("Local position offset (relative to movingPart's placed pose) when fully extended.")]
        [SerializeField]
        private Vector3 extendedPosOffset = new Vector3(0.0f, 2.0f, 0.0f);

        [Tooltip("Local rotation in degrees (relative to the placed pose) when fully extended.")]
        [SerializeField]
        private Vector3 extendedRotOffset = Vector3.zero;

        [Tooltip("Seconds for a full extend or retract.")]
        [SerializeField, Min(0.01f)]
        private float duration = 0.8f;

        [SerializeField]
        private bool startExtended = false;

        [Header("Audio")]
        [Tooltip("Played at the platform when it starts extending. Leave empty for silence.")]
        [SerializeField]
        private AudioClip extendClip;

        [SerializeField]
        private AudioClip retractClip;

        [SerializeField, Range(0.0f, 1.0f)]
        private float volume = 0.8f;

        private Vector3 retractedLocalPos;
        private Quaternion retractedLocalRot;
        private Vector3 extendedLocalPos;
        private Quaternion extendedLocalRot;

        private Rigidbody body;
        private Collider[] colliders;
        private float progress;
        private float target;

        private readonly HashSet<CharacterController> riders = new HashSet<CharacterController>();
        private readonly Collider[] overlapBuffer = new Collider[16];

        public bool IsExtended => target > 0.5f;

        private void Awake()
        {
            if (movingPart == null)
            {
                Debug.LogWarning($"[ExtendingPlatform] {name} has no movingPart assigned.");
                enabled = false;
                return;
            }

            retractedLocalPos = movingPart.localPosition;
            retractedLocalRot = movingPart.localRotation;
            extendedLocalPos = retractedLocalPos + extendedPosOffset;
            extendedLocalRot = retractedLocalRot * Quaternion.Euler(extendedRotOffset);

            body = movingPart.GetComponent<Rigidbody>();
            if (body == null)
            {
                body = movingPart.gameObject.AddComponent<Rigidbody>();
            }
            body.isKinematic = true;
            body.interpolation = RigidbodyInterpolation.Interpolate;

            colliders = movingPart.GetComponentsInChildren<Collider>();

            progress = target = startExtended ? 1.0f : 0.0f;
            ApplyPose(progress, teleport: true);
        }

        [ContextMenu("Extend")]
        public void Extend() => SetExtended(true);

        [ContextMenu("Retract")]
        public void Retract() => SetExtended(false);

        public void Toggle() => SetExtended(!IsExtended);

        public void SetExtended(bool extended)
        {
            float newTarget = extended ? 1.0f : 0.0f;
            if (Mathf.Approximately(newTarget, target))
            {
                return;
            }

            target = newTarget;
            AudioClip clip = extended ? extendClip : retractClip;
            if (clip != null)
            {
                GameAudio.PlayAt(clip, movingPart.position, volume);
            }
        }

        private void FixedUpdate()
        {
            if (Mathf.Approximately(progress, target))
            {
                return;
            }

            FindRiders();
            Matrix4x4 before = PoseMatrix(progress);

            progress = Mathf.MoveTowards(progress, target, Time.fixedDeltaTime / duration);
            ApplyPose(progress, teleport: false);

            // Carry riders by how the point they stand on moved (works for rotation as well).
            Matrix4x4 after = PoseMatrix(progress);
            foreach (CharacterController rider in riders)
            {
                Vector3 feet = rider.transform.position;
                Vector3 moved = after.MultiplyPoint3x4(before.inverse.MultiplyPoint3x4(feet));
                rider.Move(moved - feet);
            }
        }

        private void ApplyPose(float t, bool teleport)
        {
            float eased = Mathf.SmoothStep(0.0f, 1.0f, t);
            Vector3 localPos = Vector3.Lerp(retractedLocalPos, extendedLocalPos, eased);
            Quaternion localRot = Quaternion.Slerp(retractedLocalRot, extendedLocalRot, eased);

            Transform parent = movingPart.parent;
            Vector3 worldPos = parent != null ? parent.TransformPoint(localPos) : localPos;
            Quaternion worldRot = parent != null ? parent.rotation * localRot : localRot;

            if (teleport)
            {
                movingPart.SetLocalPositionAndRotation(localPos, localRot);
            }
            else
            {
                body.MovePosition(worldPos);
                body.MoveRotation(worldRot);
            }
        }

        // World matrix movingPart will have at progress t (MovePosition only lands next physics step).
        private Matrix4x4 PoseMatrix(float t)
        {
            float eased = Mathf.SmoothStep(0.0f, 1.0f, t);
            Vector3 localPos = Vector3.Lerp(retractedLocalPos, extendedLocalPos, eased);
            Quaternion localRot = Quaternion.Slerp(retractedLocalRot, extendedLocalRot, eased);
            Matrix4x4 local = Matrix4x4.TRS(localPos, localRot, movingPart.localScale);
            return movingPart.parent != null ? movingPart.parent.localToWorldMatrix * local : local;
        }

        // Anything with a CharacterController standing in a thin slab just above our colliders.
        private void FindRiders()
        {
            riders.Clear();
            foreach (Collider c in colliders)
            {
                if (c == null || !c.enabled || c.isTrigger)
                {
                    continue;
                }

                Bounds b = c.bounds;
                Vector3 centre = new Vector3(b.center.x, b.max.y + 0.15f, b.center.z);
                Vector3 halfExtents = new Vector3(b.extents.x, 0.2f, b.extents.z);
                int count = Physics.OverlapBoxNonAlloc(centre, halfExtents, overlapBuffer, Quaternion.identity, ~0, QueryTriggerInteraction.Ignore);
                for (int i = 0; i < count; i++)
                {
                    CharacterController cc = overlapBuffer[i].GetComponentInParent<CharacterController>();
                    if (cc != null)
                    {
                        riders.Add(cc);
                    }
                }
            }
        }
    }
}
