using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using TheOneFramework.UI;

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
                currentLevelNumber = LevelNumberOf(SceneManager.GetActiveScene().name, startLevelIndex);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        // Starts loading the next level in the background (e.g. as soon as the lift starts moving)
        // without showing it yet; ActivateNextLevel() switches to it. False if there is no next level.
        public bool PreloadNextLevel()
        {
            if (_preload != null) return true;

            string nextSceneName = levelPrefix + (currentLevelNumber + 1);
            if (!Application.CanStreamedLevelBeLoaded(nextSceneName))
            {
                Debug.LogWarning($"LevelManager: '{nextSceneName}' could not be found! The player might have finished the game, or the scene is missing from Build Settings.");
                return false;
            }

            _preload = SceneManager.LoadSceneAsync(nextSceneName);
            _preload.allowSceneActivation = false;
            _preloadNumber = currentLevelNumber + 1;
            return true;
        }

        // Switches to the preloaded level (preloading it now if that hadn't happened yet).
        public void ActivateNextLevel()
        {
            if (!PreloadNextLevel()) return;

            currentLevelNumber = _preloadNumber;
            _preload.allowSceneActivation = true;
            _preload = null;
            SceneManager.sceneLoaded += FadeInIfNobodyDoes;
        }

        // The exit lift faded to black. An arrival lift in the new level fades back in itself; a level
        // without one would otherwise stay black.
        private void FadeInIfNobodyDoes(Scene scene, LoadSceneMode mode)
        {
            SceneManager.sceneLoaded -= FadeInIfNobodyDoes;
            StartCoroutine(FadeInAfterStart());
        }

        private IEnumerator FadeInAfterStart()
        {
            yield return null;   // let every Start() run first (an arrival lift starts its own fade there)
            yield return null;
            if (ScreenFade.IsBlack && !ScreenFade.IsFading) ScreenFade.In(1.0f);
        }

        // Call this function when the player enters the elevator
        public void LoadNextLevel()
        {
            ActivateNextLevel();
        }

        private AsyncOperation _preload;
        private int _preloadNumber;

        // "Level_3" -> 3, so pressing Play directly in any level still loads the right next one.
        private int LevelNumberOf(string sceneName, int fallback)
        {
            if (sceneName.StartsWith(levelPrefix) && int.TryParse(sceneName.Substring(levelPrefix.Length), out int number))
            {
                return number;
            }
            return fallback;
        }
    }
}
