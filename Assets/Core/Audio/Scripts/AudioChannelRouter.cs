using UnityEngine;

namespace TheOneFramework.Audio
{
    // For AudioSources you place in a scene yourself (music, ambience loops, a humming machine):
    // sends them through the right mixer channel from GameAudioSettings, so you don't have to drag
    // the mixer group onto every single one.
    [RequireComponent(typeof(AudioSource))]
    public class AudioChannelRouter : MonoBehaviour
    {
        [SerializeField] private AudioChannel channel = AudioChannel.Ambience;

        private void Awake()
        {
            GameAudio.Route(GetComponent<AudioSource>(), channel);
        }
    }
}
