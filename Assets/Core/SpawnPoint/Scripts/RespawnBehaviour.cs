using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Cinemachine;
using Yarn.Unity;
using StarterAssets;
using TheOneFramework.Portals;
using TheOneFramework.UI;

public class RespawnBehaviour : MonoBehaviour
{
    public GameObject spawnPoint = null;
    private Transform respawnPoint;

    private Transform teleportationTarget = null;

    public CinemachineCamera cinemachineCamera;

    public float fallThreshold = -10f;

    [Header("Dying (KillZone / falling)")]
    [Tooltip("Seconds the screen takes to fade to black, and again to fade back in.")]
    public float deathFade = 0.5f;
    [Tooltip("Seconds of full black between fading out and back in.")]
    public float deathBlackTime = 0.3f;
    [Tooltip("How long you have to stand on still, solid ground before it counts as a safe place to respawn.")]
    public float safeGroundTime = 0.5f;

    private bool skipInitialRespawn = false;

    // Last spot the player stood on steadily (not a moving or physics object) - where a KillZone
    // puts you back, so you don't have to redo the whole level.
    private bool hasSafeGround;
    private Vector3 safePosition;
    private Quaternion safeRotation;
    private Collider groundCollider;
    private Vector3 groundColliderPosition;
    private float groundedTime;

    private bool dying;
    public bool IsDying => dying;

    // Set every frame something carries the player (a moving lift); KillZones leave them alone then.
    private float carriedUntil;
    public bool IsCarried => Time.time < carriedUntil;
    public void MarkCarried() { carriedUntil = Time.time + 0.25f; }

    void Start()
    {
        if (spawnPoint != null) {
            SetRespawnPoint(spawnPoint);
        } else {
            var spawns = GameObject.FindGameObjectsWithTag("SpawnPoint");
            if (spawns.Length > 0)
            {
                var id = Random.Range(0, spawns.Length);
                SetRespawnPoint(spawns[id]);
            }
        }

        // Still remember the spawn point for falling off the map, just don't teleport there now.
        if (respawnPoint != null && !skipInitialRespawn) {
            Debug.Log($"[Respawn] {name} initial teleport to spawn point {respawnPoint.name}");
            Respawn();
        }
    }

    // Called by something that already placed the player at scene start (e.g. an arrival Lift), so
    // the initial teleport to the spawn point doesn't undo it. Works whether our Start ran before
    // or after the caller's: the flag covers "not yet", clearing the pending target covers "already".
    public void KeepCurrentPosition()
    {
        skipInitialRespawn = true;
        teleportationTarget = null;
    }

    void Update()
    {
        if (transform.position.y < fallThreshold && !dying && !IsCarried)
        {
            Kill(true);
        }

        TrackSafeGround();
    }

    void LateUpdate()
    {
        if (teleportationTarget != null) {
            TeleportImmediatelyTo(teleportationTarget.position, teleportationTarget.rotation);
            teleportationTarget = null;
        }
    }

    public void SetRespawnPoint(GameObject spawnPoint)
    {
        respawnPoint = spawnPoint.transform;
    }

    public void Respawn()
    {
        TeleportTo(respawnPoint);
    }

    public void Teleport(GameObject target) {
        TeleportTo(target.transform);
    }

    public void TeleportTo(Transform targetTransform)
    {
        teleportationTarget = targetTransform;
    }

    // Fade to black, put the player back and fade in again. atSafeGround: back to the last solid
    // spot they stood on; otherwise (or if there is none yet) to the spawn point.
    public void Kill(bool atSafeGround)
    {
        if (!dying)
        {
            StartCoroutine(Die(atSafeGround));
        }
    }

    private IEnumerator Die(bool atSafeGround)
    {
        dying = true;
        var controller = GetComponent<ThirdPersonControllerFixed>();
        if (controller != null) controller.enabled = false;

        yield return ScreenFade.Out(deathFade);

        var carry = GetComponent<PlayerCarry>();
        if (carry != null) carry.ReturnHeldToOrigin();

        if (atSafeGround && hasSafeGround) {
            TeleportImmediatelyTo(safePosition, safeRotation);
        } else if (respawnPoint != null) {
            TeleportImmediatelyTo(respawnPoint.position, respawnPoint.rotation);
        } else if (hasSafeGround) {
            TeleportImmediatelyTo(safePosition, safeRotation);
        } else {
            Debug.LogWarning($"[Respawn] {name} died but has no spawn point or safe ground to go back to.");
        }
        groundedTime = 0.0f;

        if (controller != null) controller.enabled = true;
        yield return new WaitForSecondsRealtime(deathBlackTime);
        yield return ScreenFade.In(deathFade);
        dying = false;
    }

    private void TrackSafeGround()
    {
        var characterController = GetComponent<CharacterController>();
        bool steady = !dying
            && characterController != null && characterController.isGrounded
            && groundCollider != null
            && groundCollider.attachedRigidbody == null
            && groundCollider.GetComponentInParent<KillZone>() == null
            && groundCollider.transform.position == groundColliderPosition;

        groundColliderPosition = groundCollider != null ? groundCollider.transform.position : Vector3.zero;
        groundedTime = steady ? groundedTime + Time.deltaTime : 0.0f;

        if (groundedTime >= safeGroundTime) {
            hasSafeGround = true;
            safePosition = transform.position;
            safeRotation = Quaternion.Euler(0.0f, transform.eulerAngles.y, 0.0f);
        }
    }

    private void TeleportImmediatelyTo(Vector3 position, Quaternion rotation)
    {
        var characterController = GetComponent<CharacterController>();
        if (characterController != null) {
            var currentCameraPosition = Vector3.zero;
            if (cinemachineCamera != null) {
                currentCameraPosition = cinemachineCamera.Follow.transform.position;
            }

            characterController.enabled = false;
            transform.position = position;
            transform.rotation = rotation;
            characterController.enabled = true;

            if (cinemachineCamera != null) {
                cinemachineCamera.OnTargetObjectWarped(cinemachineCamera.Follow, cinemachineCamera.Follow.transform.position - currentCameraPosition);
            }
        }
    }

    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        // Something we're standing on (not a wall we bump into).
        if (hit.normal.y > 0.7f) {
            if (hit.collider != groundCollider) {
                groundCollider = hit.collider;
                groundColliderPosition = hit.collider.transform.position;
                groundedTime = 0.0f;
            }
        }

        if (hit.collider.CompareTag("SpawnPoint")) {
            SetRespawnPoint(hit.gameObject);
        }
        if (hit.collider.CompareTag("RespawnPlayers")) {
            Respawn();
        }
    }
}
