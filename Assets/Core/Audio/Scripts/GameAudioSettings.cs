using UnityEngine;
using UnityEngine.Audio;

namespace TheOneFramework.Audio
{
    // Which mixer group each kind of sound plays through. GameAudio loads the one named
    // "GameAudio" from a Resources folder, so there's nothing to wire up per scene. Without it (or
    // with empty groups) sounds still play, just straight to the master output.
    [CreateAssetMenu(fileName = "GameAudio", menuName = "TheOneFramework/Game Audio Settings")]
    public class GameAudioSettings : ScriptableObject
    {
        [Tooltip("The mixer whose exposed volume parameters the VolumeSliders control: " +
                 "MasterVolume, MusicVolume, SfxVolume, VoiceVolume, AmbienceVolume.")]
        public AudioMixer mixer;

        public AudioMixerGroup sfx;
        public AudioMixerGroup music;
        public AudioMixerGroup voice;
        public AudioMixerGroup ambience;

        [Header("Anti-cacophony")]
        [Tooltip("Most one-shot sounds that can play at the same time; the oldest gets cut off beyond this.")]
        [Min(1)] public int maxVoices = 32;

        [Tooltip("The same clip won't play more often than this at once (e.g. a pile of cubes all landing).")]
        [Min(1)] public int maxSameClip = 3;

        [Tooltip("The same clip won't restart within this many seconds of itself.")]
        [Min(0.0f)] public float sameClipCooldown = 0.05f;
    }
}
