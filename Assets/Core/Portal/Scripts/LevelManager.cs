using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Core.Portal.Scripts
{
    public class LevelManager : MonoBehaviour
    {
        public static LevelManager Instance;

        [Header("Level Settings")]
        [Tooltip("The prefix of your scene names (e.g., 'Level_' for 'Level_1')")]
        [SerializeField] private string levelPrefix = "Level_"; 
        [SerializeField] private int startLevelIndex = 1;

        private int currentLevelNumber;

        private void Awake()
        {
            // Singleton pattern: ensures only one manager exists across all scenes
            if (Instance == null)
            {
                Instance = this;
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

