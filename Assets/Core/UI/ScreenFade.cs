using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace TheOneFramework.UI
{
    // Full-screen black fade that survives scene loads, so one scene can fade out and the next one
    // fade back in. Made on first use - nothing to put in a scene:
    //   yield return ScreenFade.Out(0.5f);  ...  yield return ScreenFade.In(0.5f);
    public class ScreenFade : MonoBehaviour
    {
        private static ScreenFade instance;

        private CanvasGroup group;
        private Coroutine running;

        public static bool IsBlack => instance != null && instance.group.alpha >= 0.999f;

        // True while a fade (in or out) is running.
        public static bool IsFading => instance != null && instance.running != null;

        // Domain reload is off in this project, so statics survive between Play sessions - reset them.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            instance = null;
        }

        public static Coroutine Out(float duration) => Instance.FadeTo(1.0f, duration);

        public static Coroutine In(float duration) => Instance.FadeTo(0.0f, duration);

        // Instantly black, e.g. at the start of a scene that was loaded behind a fade.
        public static void SetBlack()
        {
            Instance.Stop();
            instance.group.alpha = 1.0f;
            instance.group.blocksRaycasts = true;
        }

        private static ScreenFade Instance
        {
            get
            {
                if (instance == null) Create();
                return instance;
            }
        }

        private static void Create()
        {
            var go = new GameObject("ScreenFade");
            DontDestroyOnLoad(go);

            Canvas canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = short.MaxValue;

            var image = new GameObject("Black", typeof(RectTransform)).AddComponent<Image>();
            image.transform.SetParent(go.transform, false);
            image.color = Color.black;
            var rt = image.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            instance = go.AddComponent<ScreenFade>();
            instance.group = go.AddComponent<CanvasGroup>();
            instance.group.alpha = 0.0f;
            instance.group.blocksRaycasts = false;
            instance.group.interactable = false;
        }

        private Coroutine FadeTo(float target, float duration)
        {
            Stop();
            running = StartCoroutine(Fade(target, duration));
            return running;
        }

        private void Stop()
        {
            if (running != null) StopCoroutine(running);
            running = null;
        }

        private IEnumerator Fade(float target, float duration)
        {
            float start = group.alpha;
            group.blocksRaycasts = true;
            for (float t = 0.0f; t < duration; t += Time.unscaledDeltaTime)
            {
                group.alpha = Mathf.SmoothStep(start, target, t / duration);
                yield return null;
            }
            group.alpha = target;
            group.blocksRaycasts = target > 0.0f;
            running = null;
        }
    }
}
