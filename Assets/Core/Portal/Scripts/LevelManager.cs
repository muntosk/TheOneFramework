using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Core.Portal.Scripts
{

 
    public class LevelManager : MonoBehaviour
    {
        private static LevelManager _instance;

        // Creates itself on first use if no scene has one, so pressing Play in any level works
        // without having to place a LevelManager there. Note: an auto-created one uses the
        // default values of the fields below, not whatever you set on a placed one.
        public static LevelManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    // One might be in the scene but not have run Awake yet.
                    _instance = FindFirstObjectByType<LevelManager>();
                }

                if (_instance == null)
                {
                    // AddComponent runs Awake immediately, which registers it as _instance.
                    new GameObject(nameof(LevelManager)).AddComponent<LevelManager>();
                }

                return _instance;
            }
        }

        [Header("Level Settings")]
        [Tooltip("The prefix of your scene names (e.g., 'Level_' for 'Level_1')")]
        [SerializeField] private string levelPrefix = "Level_";

        [SerializeField] private int startLevelIndex = 1;

        private int currentLevelNumber;

        // Where the player stood inside the exit lift, relative to that lift, so the next level's
        // arrival lift can put them back in the same spot - wherever that lift is placed.
        public struct LiftPose
        {
            public Vector3 localPosition;
            public float localYaw;       // body facing, relative to the lift
            public float localLookYaw;   // camera facing, relative to the lift
            public float lookPitch;
        }

        private LiftPose? _liftPose;

        public void SaveLiftPose(LiftPose pose) => _liftPose = pose;

        // Hands the saved pose over once, then forgets it. False when there is none, e.g. when
        // pressing Play directly in a level instead of arriving by lift.
        public bool TryTakeLiftPose(out LiftPose pose)
        {
            pose = _liftPose.GetValueOrDefault();
            bool has = _liftPose.HasValue;
            _liftPose = null;
            return has;
        }

        private void Awake()
        {
            // Singleton pattern: ensures only one manager exists across all scenes
            // _instance can already be this when the Instance getter found us before our Awake ran.
            if (_instance == null || _instance == this)
            {
                _instance = this;
                // DontDestroyOnLoad silently does nothing for a child object (it would die with the
                // old scene, taking the saved lift pose with it), so make sure we're a root first.
                transform.SetParent(null);
                DontDestroyOnLoad(gameObject);
                currentLevelNumber = startLevelIndex;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        // Call this function when the player enters the elevator
        public void LoadNextLevel()
        {
            int nextLevelNumber = currentLevelNumber + 1;
            string nextSceneName = levelPrefix + nextLevelNumber;

            // Check if the next scene actually exists in the Build Settings
            if (Application.CanStreamedLevelBeLoaded(nextSceneName))
            {
                currentLevelNumber = nextLevelNumber;
                StartCoroutine(ElevatorLoadRoutine(nextSceneName));
            }
            else
            {
                Debug.LogWarning($"LevelManager: '{nextSceneName}' could not be found! The player might have finished the game, or the scene is missing from Build Settings.");
                // Optional: Load a Main Menu or Credits scene here
            }
        }

        private IEnumerator ElevatorLoadRoutine(string sceneName)
        {
            // 1. Play your elevator doors closing animation here
            // 2. Wait for the animation to finish (e.g., 1.5 seconds)
            yield return new WaitForSeconds(1.5f);

            // 3. Load the scene asynchronously in the background
            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);

            // Wait until the new level is fully loaded
            while (!asyncLoad.isDone)
            {
                yield return null;
            }

            // 4. The new scene is open! You can trigger the doors opening animation here
        }
    }
}

