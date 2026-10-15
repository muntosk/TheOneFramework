using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace TheOneFramework.Portals
{
    // A test chamber's box: floor, ceiling and four walls out of level-kit panels, with an opening in
    // the front wall for the arrival lift and one in the back wall for the exit lift (both
    // LiftConnector prefabs, placed and kept in line by this). Sizes are in panels (2.44 m). Change a
    // number in the Inspector and the shell rebuilds itself; everything else in the room (pits,
    // ledges, white portal panels, buttons) you place yourself as children of the chamber.
    // Pivot = middle of the front wall at floor level; the room runs along +Z. Keep its scale at 1.
    // White panels can simply go on top of a black wall: portals ignore the wall behind them too.
    [ExecuteAlways]
    public class ChamberShell : MonoBehaviour
    {
        public const float Panel = 128 * 0.01905f;
        private const float Thickness = 0.25f;
        private const float OpeningWidth = Panel;
        private const float OpeningHeight = 2.2f;   // the Door prefab's height

        [Header("Size (panels)")]
        [Min(1)] public int width = 4;
        [Min(1)] public int length = 6;
        [Min(1)] public int height = 4;

        [Tooltip("Build a floor at height 0. Turn off to build your own (pits, ledges, ...).")]
        public bool buildFloor = true;
        [Tooltip("How far (m) the walls go down below floor height, e.g. for a pit.")]
        [Min(0.0f)] public float wallsBelowFloor = 0.0f;

        [Header("Openings (panels from the middle / from the floor)")]
        public float entranceX = 0.0f;
        public float exitX = 0.0f;
        [Tooltip("Height of the exit floor above the chamber floor, e.g. 2 for an exit on a ledge.")]
        public float exitHeight = 0.0f;

        [Header("Dressing")]
        [Tooltip("Glowing light strips in the ceiling, each with a lamp - the room lights itself.")]
        public bool ceilingLights = true;
        [Tooltip("The Portal-style chamber sign next to the entrance. 0 = no sign.")]
        [Min(0)] public int chamberNumber = 1;
        [Min(1)] public int chamberTotal = 19;
        [Tooltip("The lift rooms' own decoration (vines, debris, grime, extra pipes) on top of the clean " +
            "Portal dressing. Untick for a spotless facility.")]
        public bool liftDecoration = true;

        [Header("Parts (filled in by Tools > Portal > Build Level Kit)")]
        public GameObject wallPrefab;
        public GameObject floorPrefab;
        public GameObject ceilingPrefab;
        public GameObject arrivalConnector;
        public GameObject exitConnector;
        public GameObject signPrefab;
        public Material lightStripMaterial;

#if UNITY_EDITOR
        private bool rebuildQueued;

        private void OnEnable() => QueueRebuild();
        private void OnValidate() => QueueRebuild();

        private void QueueRebuild()
        {
            if (Application.isPlaying || rebuildQueued) return;
            rebuildQueued = true;
            EditorApplication.delayCall += () =>
            {
                rebuildQueued = false;
                if (this != null) Rebuild();
            };
        }

        [ContextMenu("Rebuild")]
        public void Rebuild()
        {
            // Only scene objects get geometry - not the prefab asset itself.
            if (EditorUtility.IsPersistent(this) || wallPrefab == null) return;

            Transform shell = transform.Find("Shell");
            if (shell != null) DestroyImmediate(shell.gameObject);
            shell = new GameObject("Shell").transform;
            shell.SetParent(transform, false);

            float w = width * Panel, l = length * Panel, h = height * Panel, t = Thickness;
            float bottom = -wallsBelowFloor;
            float ex = entranceX * Panel, xx = exitX * Panel, xy = exitHeight * Panel;

            if (buildFloor) Box(floorPrefab, shell, "Floor", new Vector3(-w / 2, -t, 0), new Vector3(w / 2, 0, l));
            Box(ceilingPrefab, shell, "Ceiling", new Vector3(-w / 2 - t, h, -t), new Vector3(w / 2 + t, h + t, l + t));
            Box(wallPrefab, shell, "Wall_Left", new Vector3(-w / 2 - t, bottom - t, -t), new Vector3(-w / 2, h + t, l + t));
            Box(wallPrefab, shell, "Wall_Right", new Vector3(w / 2, bottom - t, -t), new Vector3(w / 2 + t, h + t, l + t));
            WallWithOpening(shell, "Wall_Front", -w / 2, w / 2, bottom - t, h, -t, 0, ex, 0);
            WallWithOpening(shell, "Wall_Back", -w / 2, w / 2, bottom - t, h, l, l + t, xx, xy);

            // Connectors sit with their pivot on the outside of the wall opening, facing in.
            PlaceConnector(arrivalConnector, "Arrival", new Vector3(ex, 0, -t), Quaternion.identity);
            PlaceConnector(exitConnector, "Exit", new Vector3(xx, xy, l + t), Quaternion.Euler(0, 180, 0));

            if (ceilingLights) BuildCeilingLights(shell, w, l, h);

            // Sign on the front wall beside the entrance, facing into the room, on whichever side has room.
            if (signPrefab != null && chamberNumber > 0)
            {
                float side = ex <= 0 ? 1.0f : -1.0f;
                var sign = (GameObject)PrefabUtility.InstantiatePrefab(signPrefab, shell);
                sign.name = "ChamberSign";
                sign.transform.localPosition = new Vector3(ex + side * (OpeningWidth / 2 + 1.0f), 0, 0.02f);
                sign.transform.localRotation = Quaternion.Euler(0, 180, 0);
                var chamberSign = sign.GetComponent<ChamberSign>();
                if (chamberSign != null) chamberSign.Set(chamberNumber, chamberTotal);
            }
        }

        // A strip per 2x2 panels, running along the room, with a lamp under each.
        private void BuildCeilingLights(Transform parent, float w, float l, float h)
        {
            var lights = new GameObject("CeilingLights").transform;
            lights.SetParent(parent, false);
            int columns = Mathf.Max(1, width / 2), rows = Mathf.Max(1, length / 2);
            for (int c = 0; c < columns; c++)
            {
                float x = -w / 2 + (c + 0.5f) * (w / columns);
                for (int r = 0; r < rows; r++)
                {
                    float z = (r + 0.5f) * (l / rows);
                    var strip = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    strip.name = "LightStrip";
                    DestroyImmediate(strip.GetComponent<Collider>());
                    strip.transform.SetParent(lights, false);
                    strip.transform.localPosition = new Vector3(x, h - 0.03f, z);
                    strip.transform.localScale = new Vector3(0.3f, 0.06f, Mathf.Min(Panel * 1.5f, l / rows - 0.5f));
                    if (lightStripMaterial != null) strip.GetComponent<Renderer>().sharedMaterial = lightStripMaterial;
                    strip.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

                    var lamp = new GameObject("Lamp").AddComponent<Light>();
                    lamp.transform.SetParent(strip.transform.parent, false);
                    lamp.transform.localPosition = new Vector3(x, h - 0.7f, z);
                    lamp.type = LightType.Point;
                    lamp.range = Mathf.Max(h * 1.6f, Panel * 3);
                    lamp.intensity = 5.0f;
                    lamp.color = new Color(0.88f, 0.94f, 1f);
                    lamp.shadows = c == 0 && r == 0 ? LightShadows.Soft : LightShadows.None;
                }
            }
        }

        private void WallWithOpening(Transform parent, string name, float x0, float x1, float y0, float y1, float z0, float z1, float holeX, float holeY)
        {
            float hx0 = holeX - OpeningWidth / 2, hx1 = holeX + OpeningWidth / 2;
            float hy0 = holeY, hy1 = holeY + OpeningHeight;
            Box(wallPrefab, parent, name + "_L", new Vector3(x0, y0, z0), new Vector3(hx0, y1, z1));
            Box(wallPrefab, parent, name + "_R", new Vector3(hx1, y0, z0), new Vector3(x1, y1, z1));
            if (hy0 > y0) Box(wallPrefab, parent, name + "_Below", new Vector3(hx0, y0, z0), new Vector3(hx1, hy0, z1));
            if (hy1 < y1) Box(wallPrefab, parent, name + "_Above", new Vector3(hx0, hy1, z0), new Vector3(hx1, y1, z1));
        }

        private static void Box(GameObject prefab, Transform parent, string name, Vector3 min, Vector3 max)
        {
            if (prefab == null || max.x - min.x < 0.01f || max.y - min.y < 0.01f || max.z - min.z < 0.01f) return;
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            go.name = name;
            go.transform.localPosition = (min + max) * 0.5f;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = max - min;
        }

        private void PlaceConnector(GameObject prefab, string name, Vector3 position, Quaternion rotation)
        {
            if (prefab == null) return;
            Transform connector = transform.Find(name);
            if (connector == null)
            {
                var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, transform);
                go.name = name;
                connector = go.transform;
            }
            connector.localPosition = position;
            connector.localRotation = rotation;

            Transform decoration = connector.Find("Lift/Decoration");
            if (decoration != null) decoration.gameObject.SetActive(liftDecoration);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = new Color(0.3f, 0.7f, 1f, 0.6f);
            float w = width * Panel, l = length * Panel, h = height * Panel;
            Gizmos.DrawWireCube(new Vector3(0, h / 2, l / 2), new Vector3(w, h, l));
        }
#endif
    }
}
