using System.Collections;
using UnityEngine;

namespace TheOneFramework.Audio
{
    // Background music for a level, on the mixer's Music channel. Starts its clip on load (faded in)
    // and can cross-fade to another track, e.g. from a trigger's UnityEvent: CrossFadeTo(clip) when
    // the plot twist hits, Stop() to fade out.
    public class LevelMusic : MonoBehaviour
    {
        [SerializeField] private AudioClip startClip;
        [SerializeField, Range(0.0f, 1.0f)] private float volume = 0.6f;
        [SerializeField, Min(0.0f)] private float fadeTime = 2.0f;

        private AudioSource current;
        private AudioSource next;

        private void Awake()
        {
            current = CreateSource();
            next = CreateSource();
        }

        private void Start()
        {
            if (startClip != null)
            {
                CrossFadeTo(startClip);
            }
        }

        public void CrossFadeTo(AudioClip clip)
        {
            if (clip == null || (current.isPlaying && current.clip == clip))
            {
                return;
            }

            (current, next) = (next, current);
            current.clip = clip;
            current.volume = 0.0f;
            current.Play();
            StopAllCoroutines();
            StartCoroutine(Fade(current, next));
        }

        public void Stop()
        {
            StopAllCoroutines();
            StartCoroutine(Fade(null, current));
        }

        private IEnumerator Fade(AudioSource fadeIn, AudioSource fadeOut)
        {
            float outStart = fadeOut != null ? fadeOut.volume : 0.0f;
            for (float t = 0.0f; t < fadeTime; t += Time.unscaledDeltaTime)
            {
                float k = t / fadeTime;
                if (fadeIn != null) fadeIn.volume = volume * k;
                if (fadeOut != null) fadeOut.volume = outStart * (1.0f - k);
                yield return null;
            }

            if (fadeIn != null) fadeIn.volume = volume;
            if (fadeOut != null)
            {
                fadeOut.volume = 0.0f;
                fadeOut.Stop();
            }
        }

        private AudioSource CreateSource()
        {
            var source = gameObject.AddComponent<AudioSource>();
            source.loop = true;
            source.playOnAwake = false;
            source.spatialBlend = 0.0f;
            GameAudio.Route(source, AudioChannel.Music);
            return source;
        }
    }
}
