using System.Collections;
using TheOneFramework.Audio;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TheOneFramework.UI
{
    // The start screen: Start loads the first level (with a fade to black), Settings shows the volume
    // sliders, Quit closes the game. The background slowly zooms in so the screen doesn't feel frozen.
    // Built by Tools > UI > Build Main Menu Scene.
    public class MainMenu : MonoBehaviour
    {
#if UNITY_EDITOR
        [Tooltip("Scene loaded by the Start button. Drag a scene in here; it's added to the build scene list automatically.")]
        [SerializeField] private UnityEditor.SceneAsset firstLevelScene;
#endif

        // Path of firstLevelScene, filled in by OnValidate (SceneAssets don't exist in a build).
        [HideInInspector] public string firstLevel;

        [Header("Panels")]
        public GameObject mainPanel;
        public GameObject settingsPanel;

        [Header("Fade")]
        [Tooltip("Black overlay that fades in before the level loads.")]
        public CanvasGroup fade;
        [Min(0.0f)] public float fadeDuration = 0.8f;

        [Header("Background")]
        public RectTransform background;
        [Tooltip("How far the background zooms in over one cycle (1.08 = 8% bigger).")]
        public float zoom = 1.08f;
        [Min(1.0f)] public float zoomCycle = 30.0f;

        [Header("Music")]
        public AudioSource music;

        private bool loading;

#if UNITY_EDITOR
        public void SetFirstLevel(UnityEditor.SceneAsset scene)
        {
            firstLevelScene = scene;
            OnValidate();
        }

        private void OnValidate()
        {
            firstLevel = firstLevelScene != null ? UnityEditor.AssetDatabase.GetAssetPath(firstLevelScene) : "";
            if (string.IsNullOrEmpty(firstLevel)) return;

            var scenes = new System.Collections.Generic.List<UnityEditor.EditorBuildSettingsScene>(UnityEditor.EditorBuildSettings.scenes);
            int index = scenes.FindIndex(s => s.path == firstLevel);
            if (index >= 0 && scenes[index].enabled) return;

            if (index >= 0) scenes[index].enabled = true;
            else scenes.Add(new UnityEditor.EditorBuildSettingsScene(firstLevel, true));
            UnityEditor.EditorBuildSettings.scenes = scenes.ToArray();
            Debug.Log($"[MainMenu] Added {firstLevel} to the build scene list.");
        }
#endif

        private void Start()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            Time.timeScale = 1.0f;

            ShowMain();

            if (fade != null)
            {
                fade.alpha = 1.0f;
                fade.blocksRaycasts = false;
                StartCoroutine(Fade(1.0f, 0.0f));
            }

            if (music != null)
            {
                GameAudio.Route(music, AudioChannel.Music);
                if (!music.isPlaying) music.Play();
            }
        }

        private void Update()
        {
            if (background != null)
            {
                // Ease in and out between 1 and zoom.
                float t = 0.5f - 0.5f * Mathf.Cos(Time.unscaledTime / zoomCycle * Mathf.PI * 2.0f);
                background.localScale = Vector3.one * Mathf.Lerp(1.0f, zoom, t);
            }
        }

        public void StartGame()
        {
            if (loading) return;
            loading = true;
            // A new game: the portal gun has to be picked up again.
            TheOneFramework.Portals.PortalGun.ResetPickup();
            StartCoroutine(LoadLevel());
        }

        public void ShowSettings()
        {
            mainPanel.SetActive(false);
            settingsPanel.SetActive(true);
        }

        public void ShowMain()
        {
            if (settingsPanel != null) settingsPanel.SetActive(false);
            if (mainPanel != null) mainPanel.SetActive(true);
        }

        public void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private IEnumerator LoadLevel()
        {
            AsyncOperation load = string.IsNullOrEmpty(firstLevel) ? null : SceneManager.LoadSceneAsync(firstLevel);
            if (load == null)
            {
                Debug.LogError($"[MainMenu] No First Level set, or '{firstLevel}' is not in the build scene list.");
                loading = false;
                yield break;
            }
            load.allowSceneActivation = false;

            if (fade != null)
            {
                fade.blocksRaycasts = true;
                float startVolume = music != null ? music.volume : 0.0f;
                yield return Fade(0.0f, 1.0f, v =>
                {
                    if (music != null) music.volume = startVolume * (1.0f - v);
                });
            }

            load.allowSceneActivation = true;
        }

        private IEnumerator Fade(float from, float to, System.Action<float> onStep = null)
        {
            for (float t = 0.0f; t < fadeDuration; t += Time.unscaledDeltaTime)
            {
                float v = Mathf.SmoothStep(from, to, t / fadeDuration);
                fade.alpha = v;
                onStep?.Invoke(Mathf.InverseLerp(from, to, v));
                yield return null;
            }
            fade.alpha = to;
            onStep?.Invoke(1.0f);
        }
    }
}
