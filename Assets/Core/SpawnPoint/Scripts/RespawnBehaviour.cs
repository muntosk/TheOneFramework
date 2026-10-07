using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Cinemachine;
using Yarn.Unity;

public class RespawnBehaviour : MonoBehaviour
{
    public GameObject spawnPoint = null;
    private Transform respawnPoint;

    private Transform teleportationTarget = null;

    public CinemachineCamera cinemachineCamera;

    public float fallThreshold = -10f;

    private bool skipInitialRespawn = false;

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
        if (transform.position.y < fallThreshold)
        {
            Respawn();
        }
    }

    void LateUpdate() 
    {
        if (teleportationTarget != null) {
            TeleportImmediatelyTo(teleportationTarget);
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

    private void TeleportImmediatelyTo(Transform targetTransform)
    {
        var characterController = GetComponent<CharacterController>();
        if (characterController != null) {
            var currentCameraPosition = Vector3.zero;
            if (cinemachineCamera != null) {
                currentCameraPosition = cinemachineCamera.Follow.transform.position;
            }

            characterController.enabled = false;
            transform.position = targetTransform.position;
            transform.rotation = targetTransform.rotation;
            characterController.enabled = true;

            if (cinemachineCamera != null) {
                cinemachineCamera.OnTargetObjectWarped(cinemachineCamera.Follow, cinemachineCamera.Follow.transform.position - currentCameraPosition);
            }
        }
    }

    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (hit.collider.CompareTag("SpawnPoint")) {
            SetRespawnPoint(hit.gameObject);
        }
        if (hit.collider.CompareTag("RespawnPlayers")) {
            Respawn();
        }
    }
}
