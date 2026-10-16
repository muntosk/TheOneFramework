using UnityEngine;
using TheOneFramework.Audio;

// The fire at the end of the disposal belt. The particle systems are placed in the editor but stay
// off until the player gets close; walking (or respawning) away puts them out again.
public class Incinerator : MonoBehaviour
{
    [SerializeField] private ParticleSystem[] fire;
    [Tooltip("Optional glow that comes on with the fire.")]
    [SerializeField] private Light glow;
    [SerializeField] private float glowIntensity = 12.0f;

    [Tooltip("Distance to the player at which the fire starts.")]
    [SerializeField] private float igniteDistance = 14.0f;

    [Header("Audio")]
    [SerializeField] private AudioClip igniteClip;
    [SerializeField] private AudioClip fireLoopClip;
    [SerializeField, Range(0.0f, 1.0f)] private float volume = 0.9f;

    private Transform player;
    private AudioSource loop;
    private bool lit;

    private void Start()
    {
        var cc = FindAnyObjectByType<CharacterController>();
        if (cc != null) player = cc.transform;

        if (fireLoopClip != null)
        {
            loop = gameObject.AddComponent<AudioSource>();
            loop.clip = fireLoopClip;
            loop.loop = true;
            loop.spatialBlend = 1.0f;
            loop.maxDistance = 40.0f;
            loop.volume = volume;
            GameAudio.Route(loop, AudioChannel.Sfx);
        }

        SetLit(false);
    }

    private void Update()
    {
        if (player == null) return;

        bool near = Vector3.Distance(player.position, transform.position) < igniteDistance;
        if (near != lit) SetLit(near);
    }

    public void SetLit(bool on)
    {
        if (on && !lit && igniteClip != null)
        {
            GameAudio.PlayAt(igniteClip, transform.position, volume);
        }
        lit = on;

        foreach (ParticleSystem ps in fire)
        {
            if (ps == null) continue;
            if (on) ps.Play();
            else ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }

        if (glow != null) glow.intensity = on ? glowIntensity : 0.0f;

        if (loop != null)
        {
            if (on && !loop.isPlaying) loop.Play();
            else if (!on) loop.Stop();
        }
    }
}
