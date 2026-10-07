using System.IO;
using TheOneFramework.Portals;
using TheOneFramework.Utilities;
using UnityEditor;
using UnityEngine;

namespace TheOneFramework.EditorTools
{
    // Builds the level-building kit in Core/Portal/Prefabs/LevelKit from the baked Portal 2 materials:
    // portalable / non-portalable wall, floor and ceiling panels, glass, a fizzler and fold-out /
    // rising platforms. Every panel is a TiledBox, so resize it with the Scale tool and the tiles keep
    // their size. Re-running rebuilds the prefabs in place (same GUIDs), so placed copies update too.
    public static class PortalLevelKitBuilder
    {
        private const string Baked = "Assets/ThirdParty/Portal2HarunExport/Baked/";
        private const string KitRoot = "Assets/Core/Portal/Prefabs/LevelKit";
        private const string PortalSurfaceLayer = "PortalSurface";

        // Source maps use 0.25 units per texel, 1 unit = 0.01905 m.
        private const float MetresPerTexel = 0.25f * 0.01905f;

        // One Portal 2 panel is 128 units.
        private const float Panel = 128 * 0.01905f;
        private const float Thickness = 0.25f;

        [MenuItem("Tools/Portal/Build Level Kit")]
        private static void Build()
        {
            int surfaceLayer = LayerMask.NameToLayer(PortalSurfaceLayer);
            if (surfaceLayer < 0)
            {
                Debug.LogError($"[LevelKit] Layer '{PortalSurfaceLayer}' does not exist.");
                return;
            }

            foreach (string sub in new[] { "Surfaces", "Glass", "Platforms", "Fizzler" })
            {
                EnsureFolder($"{KitRoot}/{sub}");
            }

            // Surfaces. In Portal white = portals allowed, black metal = not.
            var wallSize = new Vector3(Panel, Panel, Thickness);
            var floorSize = new Vector3(Panel, Thickness, Panel);
            Surface("Wall_Portalable", "tile/white_wall_tile003a", true, wallSize, surfaceLayer);
            Surface("Wall_NonPortalable", "metal/black_wall_metal_002a", false, wallSize, surfaceLayer);
            Surface("Floor_Portalable", "tile/white_floor_tile002a", true, floorSize, surfaceLayer);
            Surface("Floor_NonPortalable", "metal/black_floor_metal_001c", false, floorSize, surfaceLayer);
            Surface("Ceiling_Portalable", "tile/white_ceiling_tile002a", true, floorSize, surfaceLayer);
            Surface("Ceiling_NonPortalable", "metal/black_ceiling_metal_001a", false, floorSize, surfaceLayer);

            // Glass: blocks portal shots and the player, but you can see through.
            Surface("Glass_Clear", "glass/glasswindow007a", false, new Vector3(Panel, Panel, 0.05f), surfaceLayer, "Glass");
            Surface("Glass_Frosted", "glass/glasswindow_frosted", false, new Vector3(Panel, Panel, 0.05f), surfaceLayer, "Glass");

            BuildFizzler();
            BuildWallPlatform(surfaceLayer);
            BuildFloorPlatform(surfaceLayer);

            AssetDatabase.SaveAssets();
            Debug.Log($"[LevelKit] Done - see {KitRoot}. Run Tools > Portal > Assign Portal 2 Sounds to give the new platforms/fizzler their sounds.");
        }

        private static void Surface(string name, string bakedMaterial, bool portalable, Vector3 size, int layer, string folder = "Surfaces")
        {
            var go = new GameObject(name);
            AddPanel(go, bakedMaterial, portalable, size, layer, worldAligned: true);
            Save(go, $"{KitRoot}/{folder}/{name}.prefab");
        }

        // Turns go into a TiledBox panel with collider + PortalSurface (portalable or blocking).
        private static void AddPanel(GameObject go, string bakedMaterial, bool portalable, Vector3 size, int layer, bool worldAligned)
        {
            go.layer = layer;
            go.transform.localScale = size;

            var material = AssetDatabase.LoadAssetAtPath<Material>(Baked + bakedMaterial + ".mat");
            if (material == null)
            {
                Debug.LogWarning($"[LevelKit] Missing baked material {bakedMaterial} - bake Export first?");
            }

            go.AddComponent<MeshRenderer>().sharedMaterial = material;
            var tiled = go.AddComponent<TiledBox>();
            tiled.TextureWorldSize = TextureSize(material);
            var so = new SerializedObject(tiled);
            so.FindProperty("worldAligned").boolValue = worldAligned;
            so.ApplyModifiedPropertiesWithoutUndo();

            go.AddComponent<BoxCollider>();

            var surface = go.AddComponent<PortalSurface>();
            var surfaceSo = new SerializedObject(surface);
            surfaceSo.FindProperty("allowPortals").boolValue = portalable;
            surfaceSo.ApplyModifiedPropertiesWithoutUndo();
        }

        private static float TextureSize(Material material)
        {
            Texture tex = material != null && material.HasProperty("_BaseMap") ? material.GetTexture("_BaseMap") : null;
            return tex != null ? tex.width * MetresPerTexel : Panel;
        }

        private static void BuildFizzler()
        {
            Material field = FizzlerMaterial();

            var root = new GameObject("Fizzler");
            var trigger = root.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.size = new Vector3(Panel, Panel, 0.3f);
            trigger.center = new Vector3(0.0f, Panel * 0.5f, 0.0f);
            root.AddComponent<PortalFizzleField>();

            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "Field";
            Object.DestroyImmediate(quad.GetComponent<Collider>());
            quad.transform.SetParent(root.transform, false);
            quad.transform.localPosition = new Vector3(0.0f, Panel * 0.5f, 0.0f);
            quad.transform.localScale = new Vector3(Panel, Panel, 1.0f);
            quad.GetComponent<MeshRenderer>().sharedMaterial = field;
            quad.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            quad.AddComponent<TextureScroller>();

            var emitterModel = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Core/Portal/Models/fizzler_emitter.fbx");
            if (emitterModel != null)
            {
                for (int side = -1; side <= 1; side += 2)
                {
                    var emitter = (GameObject)PrefabUtility.InstantiatePrefab(emitterModel, root.transform);
                    emitter.name = side < 0 ? "EmitterLeft" : "EmitterRight";
                    emitter.transform.localPosition = new Vector3(side * Panel * 0.5f, 0.0f, 0.0f);
                    emitter.transform.localRotation = Quaternion.Euler(0.0f, side < 0 ? 0.0f : 180.0f, 0.0f);
                }
            }
            else
            {
                Debug.LogWarning("[LevelKit] fizzler_emitter.fbx not found - fizzler built without emitter models.");
            }

            Save(root, $"{KitRoot}/Fizzler/Fizzler.prefab");
        }

        // Additive, double-sided, unlit blue ripples - an approximation of Portal 2's SolidEnergy shader.
        private static Material FizzlerMaterial()
        {
            string path = "Assets/Core/Portal/Materials/Fizzler.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                AssetDatabase.CreateAsset(mat, path);
            }

            var ripples = AssetDatabase.LoadAssetAtPath<Texture2D>(Baked + "effects/fizzler_ripples_dim.png");
            if (ripples == null) Debug.LogWarning("[LevelKit] effects/fizzler_ripples_dim.png not baked yet.");

            mat.SetTexture("_BaseMap", ripples);
            mat.SetColor("_BaseColor", new Color(0.35f, 0.75f, 1.0f, 1.0f) * 1.5f);
            mat.SetFloat("_Surface", 1.0f); // transparent
            mat.SetFloat("_Blend", 2.0f);   // additive
            mat.SetFloat("_Cull", 0.0f);    // both sides
            mat.SetFloat("_ZWrite", 0.0f);
            mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.One);
            mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            EditorUtility.SetDirty(mat);
            return mat;
        }

        // Root sits on the wall surface, facing out (+Z). Retracted the panel is flush in the wall;
        // extended it has folded 90 degrees around its bottom edge into a walkable ledge.
        private static void BuildWallPlatform(int layer)
        {
            var root = new GameObject("Platform_FromWall");
            var hinge = new GameObject("Hinge");
            hinge.transform.SetParent(root.transform, false);

            var panel = new GameObject("Panel");
            panel.transform.SetParent(hinge.transform, false);
            AddPanel(panel, "tile/white_wall_tile003a", true, new Vector3(Panel, Panel, Thickness), layer, worldAligned: false);
            panel.transform.localPosition = new Vector3(0.0f, Panel * 0.5f, -Thickness * 0.5f);

            var platform = root.AddComponent<ExtendingPlatform>();
            var so = new SerializedObject(platform);
            so.FindProperty("movingPart").objectReferenceValue = hinge.transform;
            so.FindProperty("extendedPosOffset").vector3Value = Vector3.zero;
            so.FindProperty("extendedRotOffset").vector3Value = new Vector3(90.0f, 0.0f, 0.0f);
            so.FindProperty("duration").floatValue = 0.8f;
            so.ApplyModifiedPropertiesWithoutUndo();

            Save(root, $"{KitRoot}/Platforms/Platform_FromWall.prefab");
        }

        // Root sits at floor level. Retracted the top is flush with the floor; extended a piston
        // pushes it up by 2 m.
        private static void BuildFloorPlatform(int layer)
        {
            var root = new GameObject("Platform_FromFloor");
            var piston = new GameObject("Piston");
            piston.transform.SetParent(root.transform, false);

            var top = new GameObject("Top");
            top.transform.SetParent(piston.transform, false);
            AddPanel(top, "tile/white_floor_tile002a", true, new Vector3(Panel, Thickness, Panel), layer, worldAligned: false);
            top.transform.localPosition = new Vector3(0.0f, -Thickness * 0.5f, 0.0f);

            var column = new GameObject("Column");
            column.transform.SetParent(piston.transform, false);
            AddPanel(column, "metal/black_wall_metal_002a", false, new Vector3(0.6f, 3.0f, 0.6f), layer, worldAligned: false);
            column.transform.localPosition = new Vector3(0.0f, -Thickness - 1.5f, 0.0f);

            var platform = root.AddComponent<ExtendingPlatform>();
            var so = new SerializedObject(platform);
            so.FindProperty("movingPart").objectReferenceValue = piston.transform;
            so.FindProperty("extendedPosOffset").vector3Value = new Vector3(0.0f, 2.0f, 0.0f);
            so.FindProperty("extendedRotOffset").vector3Value = Vector3.zero;
            so.FindProperty("duration").floatValue = 1.2f;
            so.ApplyModifiedPropertiesWithoutUndo();

            Save(root, $"{KitRoot}/Platforms/Platform_FromFloor.prefab");
        }

        private static void Save(GameObject go, string path)
        {
            PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}
