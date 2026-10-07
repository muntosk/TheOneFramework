using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace TheOneFramework.Audio
{
    public enum AudioChannel
    {
        Sfx,
        Music,
        Voice,
        Ambience,
    }

    // Mixer volumes a VolumeSlider can control. Each maps to an exposed mixer parameter named
    // "<value>Volume", e.g. MusicVolume.
    public enum MixerVolume
    {
        Master,
        Music,
        Sfx,
        Voice,
        Ambience,
    }

    // The one place gameplay code plays sounds through, instead of AudioSource.PlayClipAtPoint (which
    // can't be routed to a mixer group). One-shots come from a small pool of AudioSources, routed to
    // the right mixer channel, and are rate-limited per clip so piles of identical sounds don't stack
    // into a wall of noise. Volumes set through here are saved and restored on the next launch.
    public static class GameAudio
    {
        private const string PrefsPrefix = "volume.";

        private static GameAudioSettings settings;
        private static bool settingsLoaded;
        private static Runner runner;

        // Domain reload is off in this project, so statics survive between Play sessions - reset them.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            settings = null;
            settingsLoaded = false;
            runner = null;
        }

        // Make sure saved volumes are applied right at startup, not only once something plays.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            EnsureRunner();
        }

        public static GameAudioSettings Settings
        {
            get
            {
                if (!settingsLoaded)
                {
                    settings = Resources.Load<GameAudioSettings>("GameAudio");
                    settingsLoaded = true;
                }
                return settings;
            }
        }

        public static AudioMixerGroup Group(AudioChannel channel)
        {
            GameAudioSettings s = Settings;
            if (s == null) return null;
            switch (channel)
            {
                case AudioChannel.Music: return s.music;
                case AudioChannel.Voice: return s.voice;
                case AudioChannel.Ambience: return s.ambience;
                default: return s.sfx;
            }
        }

        // For AudioSources a script owns itself (loops etc.): send them through the right channel.
        public static void Route(AudioSource source, AudioChannel channel)
        {
            if (source != null)
            {
                source.outputAudioMixerGroup = Group(channel);
            }
        }

        // 3D one-shot at a world position.
        public static AudioSource PlayAt(AudioClip clip, Vector3 position, float volume = 1.0f, AudioChannel channel = AudioChannel.Sfx)
        {
            return EnsureRunner().Play(clip, position, volume, channel, spatial: true);
        }

        // 2D one-shot (UI, announcer, anything that shouldn't come from a place).
        public static AudioSource Play2D(AudioClip clip, float volume = 1.0f, AudioChannel channel = AudioChannel.Sfx)
        {
            return EnsureRunner().Play(clip, Vector3.zero, volume, channel, spatial: false);
        }

        public static AudioClip Pick(AudioClip[] clips)
        {
            return clips != null && clips.Length > 0 ? clips[Random.Range(0, clips.Length)] : null;
        }

        // Linear 0..1, like a slider.
        public static float GetVolume(MixerVolume volume)
        {
            return PlayerPrefs.GetFloat(PrefsPrefix + volume, 1.0f);
        }

        public static void SetVolume(MixerVolume volume, float linear)
        {
            linear = Mathf.Clamp01(linear);
            PlayerPrefs.SetFloat(PrefsPrefix + volume, linear);
            ApplyVolume(volume, linear);
        }

        private static void ApplyVolume(MixerVolume volume, float linear)
        {
            AudioMixer mixer = Settings != null ? Settings.mixer : null;
            if (mixer == null) return;

            // Mixer volume is in decibels: 1 -> 0 dB, 0.5 -> -6 dB, 0 -> -80 dB (silent).
            float db = linear > 0.0001f ? Mathf.Log10(linear) * 20.0f : -80.0f;
            if (!mixer.SetFloat(volume + "Volume", db))
            {
                Debug.LogWarning($"[GameAudio] Mixer has no exposed parameter '{volume}Volume'.");
            }
        }

        private static Runner EnsureRunner()
        {
            if (runner == null)
            {
                var go = new GameObject("[GameAudio]");
                Object.DontDestroyOnLoad(go);
                runner = go.AddComponent<Runner>();
            }
            return runner;
        }

        // Hidden helper object that owns the AudioSource pool and survives scene loads.
        private class Runner : MonoBehaviour
        {
            private readonly List<AudioSource> pool = new List<AudioSource>();
            private readonly Dictionary<AudioClip, float> lastStart = new Dictionary<AudioClip, float>();

            private void Start()
            {
                // Mixer parameters can't be set before the first frame, so restore saved volumes here.
                foreach (MixerVolume v in System.Enum.GetValues(typeof(MixerVolume)))
                {
                    ApplyVolume(v, GetVolume(v));
                }
            }

            public AudioSource Play(AudioClip clip, Vector3 position, float volume, AudioChannel channel, bool spatial)
            {
                if (clip == null) return null;

                GameAudioSettings s = Settings;
                int maxSame = s != null ? s.maxSameClip : 3;
                float cooldown = s != null ? s.sameClipCooldown : 0.05f;
                int maxVoices = s != null ? s.maxVoices : 32;

                if (lastStart.TryGetValue(clip, out float last) && Time.unscaledTime - last < cooldown)
                {
                    return null;
                }

                int playingSame = 0;
                AudioSource free = null;
                AudioSource oldest = null;
                foreach (AudioSource source in pool)
                {
                    if (!source.isPlaying)
                    {
                        if (free == null) free = source;
                        continue;
                    }
                    if (source.clip == clip) playingSame++;
                    if (oldest == null || source.time > oldest.time) oldest = source;
                }

                if (playingSame >= maxSame)
                {
                    return null;
                }

                AudioSource chosen = free;
                if (chosen == null)
                {
                    chosen = pool.Count < maxVoices ? CreateSource() : oldest;
                }

                chosen.transform.position = position;
                chosen.spatialBlend = spatial ? 1.0f : 0.0f;
                chosen.outputAudioMixerGroup = Group(channel);
                chosen.clip = clip;
                chosen.volume = volume;
                chosen.Play();
                lastStart[clip] = Time.unscaledTime;
                return chosen;
            }

            private AudioSource CreateSource()
            {
                var child = new GameObject("OneShot");
                child.transform.SetParent(transform, false);
                var source = child.AddComponent<AudioSource>();
                source.playOnAwake = false;
                // Doppler sounds awful when you teleport through a portal.
                source.dopplerLevel = 0.0f;
                pool.Add(source);
                return source;
            }
        }
    }
}
