using UnityEngine;

namespace TheOneFramework.Portals
{
    // Simple companion-cube dispenser: drops a fresh cube at spawnPoint whenever SpawnCube() runs.
    // Wire it to a PoleButton's onPressed, call it from Start(), or hook it up to anything else
    // that raises a UnityEvent. Destroys the previously dispensed cube first so only one ever
    // exists at a time, matching how Portal's droppers behave.
    public class CubeDispenser : MonoBehaviour
    {
        [SerializeField]
        private GameObject cubePrefab;

        [Tooltip("Where the cube appears. Defaults to this object's own position if left empty.")]
        [SerializeField]
        private Transform spawnPoint;

        [SerializeField]
        private bool spawnOnStart = true;

        private GameObject currentCube;

        private void Start()
        {
            if (spawnOnStart)
            {
                SpawnCube();
            }
        }

        public void SpawnCube()
        {
            if (cubePrefab == null)
            {
                Debug.LogWarning($"[CubeDispenser] {name} has no cubePrefab assigned.");
                return;
            }

            if (currentCube != null)
            {
                Debug.Log($"[CubeDispenser] {name} destroying previous cube {currentCube.name}");
                Destroy(currentCube);
            }

            Transform origin = spawnPoint != null ? spawnPoint : transform;
            currentCube = Instantiate(cubePrefab, origin.position, origin.rotation);
            Debug.Log($"[CubeDispenser] {name} spawned {currentCube.name} at {origin.position}");
        }
    }
}
