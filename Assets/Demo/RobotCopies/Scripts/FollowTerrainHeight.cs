using UnityEngine;

// Keeps a walking copy's feet on the terrain while a keyframed route moves it across uneven ground
// (the route clip itself only moves it horizontally).
public class FollowTerrainHeight : MonoBehaviour
{
    [SerializeField] private float offset = 0.0f;

    private void LateUpdate()
    {
        Terrain terrain = Terrain.activeTerrain;
        if (terrain == null) return;

        Vector3 p = transform.position;
        p.y = terrain.SampleHeight(p) + terrain.transform.position.y + offset;
        transform.position = p;
    }
}
