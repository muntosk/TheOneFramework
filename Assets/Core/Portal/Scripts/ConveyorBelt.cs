using System.Collections.Generic;
using UnityEngine;
using TheOneFramework.Audio;

namespace TheOneFramework.Portals
{
    // Carries whatever stands on it along this object's +Z: the player (CharacterController) and
    // physics objects like cubes. Needs a trigger collider covering a thin slab just above the belt
    // surface - the belt itself is a normal solid collider. Like the Lift, the player is moved with
    // CharacterController.Move, since a CharacterController doesn't follow moving ground on its own.
    [RequireComponent(typeof(Collider))]
    public class ConveyorBelt : MonoBehaviour
    {
        [Tooltip("Metres per second along this object's forward (+Z).")]
        [SerializeField] private float speed = 1.2f;

        [Tooltip("Optional hum/rattle that loops on the belt.")]
        [SerializeField] private AudioClip loopClip;
        [SerializeField, Range(0.0f, 1.0f)] private float volume = 0.6f;

        private readonly HashSet<CharacterController> riders = new HashSet<CharacterController>();
        private readonly HashSet<Rigidbody> bodies = new HashSet<Rigidbody>();

        public float Speed
        {
            get => speed;
            set => speed = value;
        }

        private Collider zone;

        private void Awake()
        {
            zone = GetComponent<Collider>();
            zone.isTrigger = true;

            if (loopClip != null)
            {
                var source = gameObject.AddComponent<AudioSource>();
                source.clip = loopClip;
                source.loop = true;
                source.spatialBlend = 1.0f;
                source.volume = volume;
                GameAudio.Route(source, AudioChannel.Ambience);
                source.Play();
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other is CharacterController cc)
            {
                riders.Add(cc);
            }
            else if (other.attachedRigidbody != null && !other.attachedRigidbody.isKinematic)
            {
                bodies.Add(other.attachedRigidbody);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other is CharacterController cc)
            {
                riders.Remove(cc);
            }
            else if (other.attachedRigidbody != null)
            {
                bodies.Remove(other.attachedRigidbody);
            }
        }

        // Something teleported away (through a portal, a respawn) never gets OnTriggerExit, so check
        // every frame that it's really still above the belt.
        private bool StillOnBelt(Component c) => c != null && (zone.ClosestPoint(c.transform.position) - c.transform.position).sqrMagnitude < 0.01f;

        private void Update()
        {
            Vector3 step = transform.forward * (speed * Time.deltaTime);
            riders.RemoveWhere(cc => !StillOnBelt(cc));
            foreach (CharacterController cc in riders)
            {
                // Disabled while teleported or grabbed - leave it alone then.
                if (cc.enabled) cc.Move(step);
            }
        }

        private void FixedUpdate()
        {
            bodies.RemoveWhere(rb => rb == null || rb.isKinematic || !StillOnBelt(rb));
            foreach (Rigidbody rb in bodies)
            {
                // Keep the fall/jump part, replace the sideways part with the belt's motion.
                Vector3 v = rb.linearVelocity;
                Vector3 along = transform.forward * speed;
                rb.linearVelocity = new Vector3(along.x, v.y, along.z);
            }
        }
    }
}
