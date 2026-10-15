using TheOneFramework.Audio;
using TheOneFramework.Portals;
using UnityEngine;

// Put on deadly water/goo (or any object): the player who falls in fades out and respawns on the
// last solid ground they stood on (or the spawn point), cubes go back to where they started.
// Uses the object's trigger collider; without one (e.g. a water plane) it makes its own from the
// mesh, `depth` deep, and turns the solid collider off so you sink in instead of walking on it.
public class KillZone : MonoBehaviour
{
    [Tooltip("Respawn on the last solid ground the player stood on. Off: at the spawn point.")]
    public bool respawnAtSafeGround = true;

    [Tooltip("Turn off the object's own solid colliders so the player sinks in instead of standing on it.")]
    public bool letThrough = true;

    [Tooltip("How deep below the surface the deadly volume goes (only when it builds its own trigger).")]
    public float depth = 3.0f;

    [Tooltip("How far below the top of the trigger the player sinks before it counts (so the splash is visible).")]
    public float sinkBeforeDeath = 0.4f;

    [Header("Audio")]
    [Tooltip("Played where the player falls in.")]
    public AudioClip[] playerClips;
    [Tooltip("Played where a cube falls in.")]
    public AudioClip objectClip;
    [Range(0.0f, 1.0f)] public float volume = 0.8f;

    private Collider zone;

    private void Awake()
    {
        if (letThrough)
        {
            foreach (Collider c in GetComponents<Collider>())
            {
                if (!c.isTrigger) c.enabled = false;
            }
        }

        zone = TriggerCollider();
        if (zone != null) return;

        // No trigger yet: build one from the mesh, extending down from the surface.
        var filter = GetComponent<MeshFilter>();
        Bounds bounds = filter != null && filter.sharedMesh != null
            ? filter.sharedMesh.bounds
            : new Bounds(Vector3.zero, Vector3.one);

        float scaleY = Mathf.Max(Mathf.Abs(transform.lossyScale.y), 0.0001f);
        float top = bounds.max.y;
        float bottom = bounds.max.y - depth / scaleY;

        var box = gameObject.AddComponent<BoxCollider>();
        box.isTrigger = true;
        box.center = new Vector3(bounds.center.x, (top + bottom) * 0.5f, bounds.center.z);
        box.size = new Vector3(bounds.size.x, top - bottom, bounds.size.z);
        zone = box;
    }

    private Collider TriggerCollider()
    {
        foreach (Collider c in GetComponents<Collider>())
        {
            if (c.isTrigger && c.enabled) return c;
        }
        return null;
    }

    private float Surface => zone != null ? zone.bounds.max.y : float.MaxValue;

    // Players that came in through the surface. Someone poking into the volume from below (a lift
    // shaft, a room under the water level) doesn't count - only falling in does.
    private readonly System.Collections.Generic.HashSet<RespawnBehaviour> fellIn = new System.Collections.Generic.HashSet<RespawnBehaviour>();

    // The player dies once they've fallen in and sunk sinkBeforeDeath below the surface.
    private void OnTriggerStay(Collider other)
    {
        var player = other.GetComponentInParent<RespawnBehaviour>();
        if (player == null || player.IsDying || player.IsCarried || !fellIn.Contains(player)) return;

        if (player.transform.position.y > Surface - sinkBeforeDeath) return;

        if (playerClips != null && playerClips.Length > 0)
        {
            GameAudio.PlayAt(GameAudio.Pick(playerClips), other.transform.position, volume);
        }
        fellIn.Remove(player);   // the teleport may not send OnTriggerExit
        player.Kill(respawnAtSafeGround);
    }

    private void OnTriggerEnter(Collider other)
    {
        var player = other.GetComponentInParent<RespawnBehaviour>();
        if (player != null)
        {
            // Feet at (or just under, when falling fast) the surface: they're coming in from above.
            if (player.transform.position.y >= Surface - 0.5f) fellIn.Add(player);
            return;
        }

        var carryable = other.GetComponentInParent<Carryable>();
        if (carryable != null && carryable.transform.position.y >= Surface - 0.5f)
        {
            if (objectClip != null)
            {
                GameAudio.PlayAt(objectClip, carryable.transform.position, volume);
            }
            carryable.ReturnToOrigin();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        var player = other.GetComponentInParent<RespawnBehaviour>();
        if (player != null) fellIn.Remove(player);
    }
}
