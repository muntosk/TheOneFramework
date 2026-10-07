using UnityEngine;
using TheOneFramework.Audio;

public class SimpleAudioTrigger : PlayerActivatable 
{
    public AudioClip audioClip;

    [Tooltip("Which mixer channel this plays through (e.g. Voice for announcer lines).")]
    public AudioChannel channel = AudioChannel.Sfx;

    [Range(0.0f, 1.0f)]
    public float volume = 1.0f;

    override protected void OnActivate()
    {        
        if (audioClip != null)
        {
            GameAudio.Play2D(audioClip, volume, channel);
        }
    }
}
