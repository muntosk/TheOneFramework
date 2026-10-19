using System.Collections.Generic;
using UnityEngine;

// The ending's robot copies: they come out of the facility in a column and walk down the path
// (the children of `route`, in order) towards the player, all in step. When the front row gets within
// `stopDistance` of the player the whole column halts, switches to idle and turns to face them, so a
// long conversation never lets them walk into (or on the spot in front of) the player. If the player
// walks away they set off again. Starts marching when this object is enabled (<<set_enabled_in_unity>>).
// With `startInFull` the whole column is already on the path at the start (for a route that begins out in
// the open); without it, the rows come out one by one from the route's first point (e.g. a doorway).
public class DeploymentMarch : MonoBehaviour
{
    [SerializeField] private Transform route;
    [SerializeField] private Transform[] units;

    [Header("Formation")]
    [SerializeField] private int columns = 2;
    [SerializeField] private float columnSpacing = 1.3f;
    [SerializeField] private float rowSpacing = 1.8f;
    [SerializeField] private bool startInFull = true;

    [Header("Walking")]
    [Tooltip("Matches the walk clip, so the feet don't slide.")]
    [SerializeField] private float walkSpeed = 1.56f;
    [SerializeField] private float stopDistance = 7.0f;
    [SerializeField] private float resumeDistance = 10.0f;
    [SerializeField] private float turnSpeed = 180.0f;

    [Header("Body animation")]
    [SerializeField] private string walkState = "Walk";
    [SerializeField] private string idleState = "Idle";
    [SerializeField] private float crossFade = 0.3f;

    private readonly List<Vector3> points = new List<Vector3>();
    private readonly List<float> distances = new List<float>();
    private Transform player;
    private Animator[] bodies;
    private Renderer[][] renderers;
    private float head;
    private bool stopped;

    private void Awake()
    {
        BuildPath();

        bodies = new Animator[units.Length];
        renderers = new Renderer[units.Length][];
        for (int i = 0; i < units.Length; i++)
        {
            bodies[i] = units[i].GetComponentInChildren<Animator>();
            renderers[i] = units[i].GetComponentsInChildren<Renderer>(true);
        }
    }

    private void OnEnable()
    {
        var controller = FindAnyObjectByType<StarterAssets.ThirdPersonControllerFixed>();
        player = controller != null ? controller.transform : null;
        int rows = Mathf.CeilToInt(units.Length / (float)Mathf.Max(columns, 1));
        head = startInFull ? (rows - 1) * rowSpacing : 0.0f;
        stopped = false;
        Place(true);
    }

    private void Update()
    {
        if (points.Count < 2) return;

        float length = distances[distances.Count - 1];
        float toPlayer = player != null ? FlatDistance(PointAt(head), player.position) : float.MaxValue;

        if (!stopped && (toPlayer < stopDistance || head >= length))
            SetStopped(true);
        else if (stopped && toPlayer > resumeDistance && head < length)
            SetStopped(false);

        if (!stopped) head = Mathf.Min(head + walkSpeed * Time.deltaTime, length);
        Place(false);
    }

    private void SetStopped(bool value)
    {
        stopped = value;
        foreach (Animator body in bodies)
            if (body != null) body.CrossFadeInFixedTime(value ? idleState : walkState, crossFade);
    }

    // Puts every unit on the path, `row * rowSpacing` behind the head of the column. Units whose spot
    // is still inside the facility stay hidden until they come out.
    private void Place(bool snap)
    {
        for (int i = 0; i < units.Length; i++)
        {
            int row = i / columns, column = i % columns;
            float along = head - row * rowSpacing;
            float side = (column - (columns - 1) * 0.5f) * columnSpacing;

            bool visible = along >= 0.0f;
            foreach (Renderer r in renderers[i]) r.enabled = visible;
            along = Mathf.Max(along, 0.0f);

            Vector3 forward = TangentAt(along);
            Vector3 position = PointAt(along) + Vector3.Cross(Vector3.up, forward) * side;
            position.y = GroundHeight(position);
            units[i].position = position;

            Vector3 facing = forward;
            if (stopped && player != null)
            {
                facing = player.position - position;
                facing.y = 0.0f;
            }
            if (facing.sqrMagnitude < 0.0001f) continue;

            Quaternion goal = Quaternion.LookRotation(facing);
            units[i].rotation = snap ? goal : Quaternion.RotateTowards(units[i].rotation, goal, turnSpeed * Time.deltaTime);
        }
    }

    // Samples a smooth (Catmull-Rom) curve through the route points, flattened to XZ.
    private void BuildPath()
    {
        points.Clear();
        distances.Clear();
        if (route == null || route.childCount < 2) return;

        var corners = new List<Vector3>();
        foreach (Transform child in route)
            corners.Add(new Vector3(child.position.x, 0.0f, child.position.z));

        const int stepsPerSegment = 16;
        for (int c = 0; c < corners.Count - 1; c++)
        {
            Vector3 p0 = corners[Mathf.Max(c - 1, 0)], p1 = corners[c];
            Vector3 p2 = corners[c + 1], p3 = corners[Mathf.Min(c + 2, corners.Count - 1)];
            for (int s = 0; s < stepsPerSegment; s++)
                AddPoint(CatmullRom(p0, p1, p2, p3, s / (float)stepsPerSegment));
        }
        AddPoint(corners[corners.Count - 1]);
    }

    private void AddPoint(Vector3 p)
    {
        distances.Add(points.Count == 0 ? 0.0f : distances[distances.Count - 1] + Vector3.Distance(points[points.Count - 1], p));
        points.Add(p);
    }

    private static Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        return 0.5f * (2.0f * p1 + (p2 - p0) * t + (2.0f * p0 - 5.0f * p1 + 4.0f * p2 - p3) * t * t
                       + (3.0f * p1 - p0 - 3.0f * p2 + p3) * t * t * t);
    }

    private int SegmentAt(float d)
    {
        int i = 1;
        while (i < distances.Count - 1 && distances[i] < d) i++;
        return i;
    }

    private Vector3 PointAt(float d)
    {
        int i = SegmentAt(d);
        float span = distances[i] - distances[i - 1];
        float t = span > 0.0f ? Mathf.Clamp01((d - distances[i - 1]) / span) : 0.0f;
        return Vector3.Lerp(points[i - 1], points[i], t);
    }

    private Vector3 TangentAt(float d)
    {
        int i = SegmentAt(d);
        return (points[i] - points[i - 1]).normalized;
    }

    private static float GroundHeight(Vector3 p)
    {
        Terrain terrain = Terrain.activeTerrain;
        return terrain != null ? terrain.SampleHeight(p) + terrain.transform.position.y : p.y;
    }

    private static float FlatDistance(Vector3 a, Vector3 b)
    {
        a.y = b.y = 0.0f;
        return Vector3.Distance(a, b);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (route == null) return;
        Gizmos.color = Color.cyan;
        Transform previous = null;
        foreach (Transform child in route)
        {
            Gizmos.DrawSphere(child.position, 0.3f);
            if (previous != null) Gizmos.DrawLine(previous.position, child.position);
            previous = child;
        }
    }
#endif
}
