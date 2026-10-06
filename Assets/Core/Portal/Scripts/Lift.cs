using System;
using StarterAssets;
using TheOneFramework.Portals;
using UnityEngine;

namespace Core.Portal.Scripts
{
    public enum LiftMode
    {
        Exit,
        Arrival
    }

    // Portal 2 style lift. Needs a trigger collider on this object covering the inside of the lift.
    // Exit: door starts open; stepping into the trigger closes it, waits a moment, rises, and
    //       loads the next level once at the top. Stepping back out during the wait cancels it.
    //       Right before loading, it saves where the player stands inside it (see LevelManager.LiftPose).
    // Arrival: starts `height` below where it's placed, puts the player in that saved spot, rises up
    //       to its placed position by itself, then opens.
    // Whoever stands inside is carried along, since a CharacterController doesn't follow moving
    // platforms on its own.
    public class Lift : MonoBehaviour
    {
        [SerializeField] private float speed = 2.0f;
        [Tooltip("How far up (local space) the lift travels from its starting position.")]
        [SerializeField] private float height = 3.0f;
        [Tooltip("Seconds between the lift being triggered and it starting to move, so the door can close first.")]
        [SerializeField] private float startDelay = 1.5f;
        [Tooltip("Optional door. Exit: open until the player steps in. Arrival: opens once the lift is up.")]
        [SerializeField] private Door door;
        [Tooltip("Decide if the lift exits the level or enters the level.")]
        [SerializeField] private LiftMode liftMode = LiftMode.Exit;

        private Vector3 _target;
        private bool _triggered;
        private bool _arrived;
        private float _delayTimer;
        private CharacterController _rider;

        private void Awake()
        {
            if (liftMode == LiftMode.Exit)
            {
                _target = transform.localPosition + Vector3.up * height;
            }
            else
            {
                _target = transform.localPosition;                 // placed position = where it ends up
                transform.localPosition -= Vector3.up * height;    // start below
                _triggered = true;                                 // starts by itself
            }
        }

        private void Start()
        {
            if (liftMode == LiftMode.Exit)
            {
                // In Start rather than Awake, so Door has already stored its closed position in its own Awake.
                if (door != null)
                {
                    door.Open();
                }
            }
            else
            {
                PlaceArrivingPlayer();
            }
        }

        private void Update()
        {
            if (!_triggered || _arrived)
            {
                return;
            }

            if (liftMode == LiftMode.Exit)
            {
                // Wait for the door to close before moving.
                if (_delayTimer < startDelay)
                {
                    // Player stepped back out before it left: cancel and let them in again later.
                    if (liftMode == LiftMode.Exit && _rider == null)
                    {
                        Cancel();
                        return;
                    }

                    _delayTimer += Time.deltaTime;
                    return;
                }
            }

            Vector3 before = transform.position;
            transform.localPosition = Vector3.MoveTowards(transform.localPosition, _target, speed * Time.deltaTime);
            if (_rider != null)
            {
                // World-space delta, since CharacterController.Move works in world space.
                _rider.Move(transform.position - before);
            }

            if (transform.localPosition != _target) return;

            _arrived = true;
            if (liftMode == LiftMode.Exit)
            {
                SaveRiderPose();
                LevelManager.Instance.LoadNextLevel();
            }
            else if (door != null)
            {
                door.Open();
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            CharacterController cc = other.GetComponentInParent<CharacterController>();
            if (cc == null) return;   // only the player, not cubes etc.

            _rider = cc;
            Raise();
        }

        private void OnTriggerExit(Collider other)
        {
            if (_rider != null && other.GetComponentInParent<CharacterController>() == _rider)
            {
                _rider = null;
            }
        }

        // Also in the component's ⋮ menu, to test without walking in.
        [ContextMenu("Raise")]
        public void Raise()
        {
            // One-way trip: ignore re-entering once it's going (and Arrival is triggered from Awake).
            if (_triggered)
            {
                return;
            }

            _triggered = true;
            _delayTimer = 0.0f;
            if (door != null)
            {
                door.Close();
            }
        }

        private void Cancel()
        {
            _triggered = false;
            _delayTimer = 0.0f;
            if (door != null)
            {
                door.Open();
            }
        }

        // Everything relative to this lift, so the arrival lift can be anywhere in the next scene.
        private void SaveRiderPose()
        {
            if (_rider == null)
            {
                Debug.LogWarning($"[Lift] {name} reached the top without a rider - nothing saved for the next level.");
                return;
            }

            Debug.Log($"[Lift] {name} saved rider pose ({_rider.name}, local {transform.InverseTransformPoint(_rider.transform.position):F2})");

            Transform player = _rider.transform;
            float liftYaw = transform.eulerAngles.y;
            ThirdPersonControllerFixed look = _rider.GetComponent<ThirdPersonControllerFixed>();

            LevelManager.Instance.SaveLiftPose(new LevelManager.LiftPose
            {
                localPosition = transform.InverseTransformPoint(player.position),
                localYaw = player.eulerAngles.y - liftYaw,
                localLookYaw = look != null ? look.LookYaw - liftYaw : player.eulerAngles.y - liftYaw,
                lookPitch = look != null ? look.LookPitch : 0.0f,
            });
        }

        // Runs while the lift is still down below (Awake already lowered it), so the player starts at the bottom.
        private void PlaceArrivingPlayer()
        {
            if (!LevelManager.Instance.TryTakeLiftPose(out LevelManager.LiftPose pose))
            {
                Debug.Log($"[Lift] {name} arrival: no saved pose, leaving the player at the spawn point.");
                return;   // pressed Play directly in this level: leave the player where they were placed
            }

            CharacterController player = FindFirstObjectByType<CharacterController>();
            if (player == null)
            {
                Debug.LogWarning($"[Lift] {name} arrival: no CharacterController found to place.");
                return;
            }

            float liftYaw = transform.eulerAngles.y;

            // CharacterController resists a direct transform teleport while enabled.
            player.enabled = false;
            player.transform.SetPositionAndRotation(
                transform.TransformPoint(pose.localPosition),
                Quaternion.Euler(0.0f, liftYaw + pose.localYaw, 0.0f));
            player.enabled = true;

            // Otherwise it teleports the player to the level's spawn point in its first LateUpdate.
            RespawnBehaviour respawn = player.GetComponentInParent<RespawnBehaviour>();
            if (respawn != null)
            {
                respawn.KeepCurrentPosition();
            }

            Debug.Log($"[Lift] {name} arrival: placed {player.name} at {player.transform.position:F2} (RespawnBehaviour {(respawn != null ? "told to keep it" : "NOT found")})");

            if (player.TryGetComponent(out ThirdPersonControllerFixed look))
            {
                look.SetLook(liftYaw + pose.localLookYaw, pose.lookPitch);
            }

            // Don't wait for OnTriggerEnter - it may only fire after the lift has already started moving.
            _rider = player;
        }

        private void OnDrawGizmosSelected()
        {
            if (liftMode == LiftMode.Exit)
            {
                
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(transform.position, 0.3f);
                Gizmos.DrawLine(transform.position, transform.position + Vector3.up * height);
                Gizmos.DrawWireSphere(_target, 0.3f);
            }
        }
    }
}
