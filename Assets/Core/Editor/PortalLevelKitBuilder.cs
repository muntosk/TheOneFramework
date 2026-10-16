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

            foreach (string sub in new[] { "Surfaces", "Glass", "Platforms", "Fizzler", "Hazards", "Chamber" })
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
            BuildToxicWater();
            BuildChamber();

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

            mat.SetTexture("_BaseMap", ripples != null ? GreyFromGreen(ripples, "Assets/Core/Portal/Materials/FizzlerRipples.png") : null);
            mat.SetColor("_BaseColor", new Color(0.25f, 0.6f, 1.0f, 1.0f) * 1.5f);
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

        // The Source ripple texture only has data in its green channel (Portal 2's shader tints it), so a
        // colour tint would always stay green. Copies green into grey so _BaseColor decides the colour.
        private static Texture2D GreyFromGreen(Texture2D source, string path)
        {
            var rt = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            Graphics.Blit(source, rt);
            var previous = RenderTexture.active;
            RenderTexture.active = rt;
            var copy = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false, true);
            copy.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0);
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(rt);

            Color[] pixels = copy.GetPixels();
            for (int i = 0; i < pixels.Length; i++)
            {
                float g = pixels[i].g;
                pixels[i] = new Color(g, g, g, 1.0f);
            }
            copy.SetPixels(pixels);
            File.WriteAllBytes(path, copy.EncodeToPNG());
            Object.DestroyImmediate(copy);

            AssetDatabase.ImportAsset(path);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
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

        private const string WaterFolder = "Assets/ThirdParty/TerrainPainting/Water/Simple/URP Sample Scenes/";
        private const string ToxicWaterPath = KitRoot + "/Hazards/ToxicWater.prefab";
        private const float WaterDepth = 1.0f;

        // One panel of deadly water: the URP water surface (transparent, depth-based, so a flat plane
        // rather than a box) with a 1 m deep KillZone trigger under it. Pivot = middle of the surface:
        // put it at floor height over a pit and scale X/Z to fit. Falling in fades out and respawns you
        // on the last solid ground; cubes go back to where they started.
        private static void BuildToxicWater()
        {
            var root = new GameObject("ToxicWater");
            root.layer = LayerMask.NameToLayer("Ignore Raycast");   // portal shots and pickups go through

            var trigger = root.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.size = new Vector3(Panel, WaterDepth, Panel);
            trigger.center = new Vector3(0.0f, -WaterDepth * 0.5f, 0.0f);

            var killZone = root.AddComponent<KillZone>();
            killZone.letThrough = false;
            killZone.sinkBeforeDeath = 0.3f;

            // Unity's Plane is 10x10 m, like the one the water prefabs use.
            var surface = GameObject.CreatePrimitive(PrimitiveType.Plane);
            surface.name = "Surface";
            surface.layer = root.layer;
            Object.DestroyImmediate(surface.GetComponent<Collider>());
            surface.transform.SetParent(root.transform, false);
            surface.transform.localScale = new Vector3(Panel / 10.0f, 1.0f, Panel / 10.0f);

            var material = AssetDatabase.LoadAssetAtPath<Material>(WaterFolder + "Materials/Water1.mat");
            if (material == null)
            {
                Debug.LogWarning("[LevelKit] Water1.mat not found in TerrainPainting/Water.");
            }
            var renderer = surface.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            Save(root, ToxicWaterPath);
        }

        // Swaps every water plane (Water1/Water2 material) in the open scenes for the kit's ToxicWater,
        // same position, yaw and size, keeping the old material if it was a different one. Undoable.
        [MenuItem("Tools/Portal/Replace Water With ToxicWater (Open Scenes)")]
        private static void ReplaceWater()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ToxicWaterPath);
            if (prefab == null)
            {
                EditorUtility.DisplayDialog("Replace Water", "ToxicWater.prefab doesn't exist yet - run Tools > Portal > Build Level Kit first.", "OK");
                return;
            }

            var waterMaterials = new[]
            {
                AssetDatabase.LoadAssetAtPath<Material>(WaterFolder + "Materials/Water1.mat"),
                AssetDatabase.LoadAssetAtPath<Material>(WaterFolder + "Materials/Water2.mat"),
            };

            var targets = new System.Collections.Generic.List<GameObject>();
            foreach (MeshRenderer r in Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include))
            {
                if (System.Array.IndexOf(waterMaterials, r.sharedMaterial) < 0) continue;

                GameObject nearest = PrefabUtility.GetNearestPrefabInstanceRoot(r.gameObject);
                GameObject outermost = PrefabUtility.GetOutermostPrefabInstanceRoot(r.gameObject);

                // Already a kit water: leave it.
                if (nearest != null && PrefabUtility.GetCorrespondingObjectFromSource(nearest) == prefab) continue;

                GameObject old = r.gameObject;
                if (nearest != null)
                {
                    // Only swap a water prefab placed on its own - never part of a bigger prefab
                    // (deleting that would take the rest with it, and Unity won't allow it anyway).
                    string source = AssetDatabase.GetAssetPath(PrefabUtility.GetCorrespondingObjectFromSource(nearest));
                    if (nearest != outermost || !source.StartsWith(WaterFolder))
                    {
                        Debug.LogWarning($"[LevelKit] Skipped water '{r.name}': it's inside prefab '{outermost.name}'. Replace it in that prefab instead.", r);
                        continue;
                    }
                    old = nearest;
                }
                if (!targets.Contains(old)) targets.Add(old);
            }

            if (targets.Count == 0)
            {
                Debug.Log("[LevelKit] No water planes found in the open scenes.");
                return;
            }

            Undo.SetCurrentGroupName("Replace Water With ToxicWater");
            foreach (GameObject old in targets)
            {
                var renderer = old.GetComponentInChildren<MeshRenderer>(true);
                var filter = renderer.GetComponent<MeshFilter>();
                Bounds local = filter != null && filter.sharedMesh != null ? filter.sharedMesh.bounds : new Bounds(Vector3.zero, Vector3.one * 10.0f);
                Vector3 scale = renderer.transform.lossyScale;
                Vector3 centre = renderer.transform.TransformPoint(local.center);
                float sizeX = Mathf.Abs(local.size.x * scale.x);
                float sizeZ = Mathf.Abs(local.size.z * scale.z);
                float yaw = renderer.transform.eulerAngles.y;

                var water = (GameObject)PrefabUtility.InstantiatePrefab(prefab, old.scene);
                Undo.RegisterCreatedObjectUndo(water, "Replace Water");
                water.transform.SetParent(old.transform.parent, false);
                water.transform.SetSiblingIndex(old.transform.GetSiblingIndex());
                water.transform.SetPositionAndRotation(centre, Quaternion.Euler(0.0f, yaw, 0.0f));
                Vector3 parentScale = water.transform.parent != null ? water.transform.parent.lossyScale : Vector3.one;
                water.transform.localScale = new Vector3(sizeX / Panel / parentScale.x, 1.0f, sizeZ / Panel / parentScale.z);

                Material oldMaterial = renderer.sharedMaterial;
                var surface = water.GetComponentInChildren<MeshRenderer>();
                if (oldMaterial != surface.sharedMaterial)
                {
                    surface.sharedMaterial = oldMaterial;   // e.g. Water2: keep that look as an override
                }

                Debug.Log($"[LevelKit] Replaced {old.name} in {old.scene.name} ({sizeX:F1} x {sizeZ:F1} m).", water);
                Undo.DestroyObjectImmediate(old);
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(water.scene);
            }

            Debug.Log($"[LevelKit] Replaced {targets.Count} water plane(s). Save the scene to keep it (Ctrl+Z undoes).");
        }

        // ---------- Chamber: lift connectors + ChamberShell ----------

        private const string GameplayRoot = "Assets/Core/Portal/Prefabs/Gameplay/";

        // Measured on LiftPlatform_base: its landing floor is 6.30 m above the prefab's pivot, the
        // landing's open front edge is at local z 7.9, the room's side walls are 6.92 m out and 7.6 m tall.
        private const float LiftLandingY = 6.30f;
        private const float LiftFrontZ = 7.9f;
        private const float LiftRoomHalfWidth = 7.4f;
        private const float LiftRoomWallHeight = 7.6f;
        private const float CorridorLength = 2 * Panel;
        private const float CorridorHeight = 2.2f;   // = ChamberShell's opening / the Door prefab

        private static void BuildChamber()
        {
            GameObject arrival = BuildLiftConnector("LiftConnector_Arrival", "LiftPlatform_variant_arrival", false);
            GameObject exit = BuildLiftConnector("LiftConnector_Exit", "LiftPlatform_variant_exit", true);

            var root = new GameObject("Chamber");
            var shell = root.AddComponent<ChamberShell>();
            shell.wallPrefab = Kit("Surfaces/Wall_NonPortalable");
            shell.floorPrefab = Kit("Surfaces/Floor_NonPortalable");
            shell.ceilingPrefab = Kit("Surfaces/Ceiling_NonPortalable");
            shell.arrivalConnector = arrival;
            shell.exitConnector = exit;
            shell.signPrefab = BuildChamberSign();
            shell.lightStripMaterial = LightStripMaterial();
            Save(root, $"{KitRoot}/Chamber/Chamber.prefab");
        }

        // ---------- materials shared by the chamber parts ----------

        private static Material LitMaterial(string name, Color color, Color emission, float smoothness = 0.4f)
        {
            string path = $"Assets/Core/Portal/Materials/{name}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.SetColor("_BaseColor", color);
            mat.SetFloat("_Smoothness", smoothness);
            if (emission.maxColorComponent > 0)
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", emission);
                mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static Material LightStripMaterial() => LitMaterial("LightStrip", Color.white, new Color(0.9f, 0.96f, 1f) * 3.0f, 0.8f);

        // ---------- the Portal-style chamber sign ----------

        // A backlit white sign: big chamber number, "/19", a progress bar. Pivot at floor level; the
        // readable side faces -Z (ChamberShell turns it to face into the room).
        private static GameObject BuildChamberSign()
        {
            var board = LitMaterial("ChamberSign_Board", new Color(0.93f, 0.94f, 0.95f), new Color(0.55f, 0.58f, 0.6f), 0.3f);
            var dark = LitMaterial("ChamberSign_Dark", new Color(0.12f, 0.13f, 0.14f), Color.black, 0.3f);
            var frame = LitMaterial("ChamberSign_Frame", new Color(0.35f, 0.37f, 0.4f), Color.black, 0.6f);
            var font = AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>("Assets/Demo/UI/Fonts/TitilliumWeb/TitilliumWeb-Bold SDF.asset");

            var root = new GameObject("ChamberSign");
            System.Func<string, Transform, Vector3, Vector3, Material, GameObject> cube = (n, parent, pos, size, mat) =>
            {
                var c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                c.name = n;
                Object.DestroyImmediate(c.GetComponent<Collider>());
                c.transform.SetParent(parent, false);
                c.transform.localPosition = pos;
                c.transform.localScale = size;
                c.GetComponent<Renderer>().sharedMaterial = mat;
                return c;
            };
            const float W = 1.1f, H = 2.0f, Y = 0.9f;   // board bottom at Y
            cube("Frame", root.transform, new Vector3(0, Y + H / 2, 0.03f), new Vector3(W + 0.08f, H + 0.08f, 0.06f), frame);
            cube("Board", root.transform, new Vector3(0, Y + H / 2, 0), new Vector3(W, H, 0.04f), board);
            cube("Line", root.transform, new Vector3(0, Y + H * 0.53f, -0.022f), new Vector3(W * 0.84f, 0.02f, 0.005f), dark);

            System.Func<string, string, float, Vector3, Vector2, TMPro.TextAlignmentOptions, TMPro.TextMeshPro> text = (n, s, size, pos, rect, align) =>
            {
                var g = new GameObject(n);
                g.transform.SetParent(root.transform, false);
                g.transform.localPosition = pos;
                var tmp = g.AddComponent<TMPro.TextMeshPro>();
                if (font != null) tmp.font = font;
                tmp.text = s;
                tmp.fontSize = size;
                tmp.color = new Color(0.12f, 0.13f, 0.14f);
                tmp.alignment = align;
                tmp.rectTransform.sizeDelta = rect;
                tmp.textWrappingMode = TMPro.TextWrappingModes.NoWrap;
                return tmp;
            };
            var number = text("Number", "01", 9f, new Vector3(-0.05f, Y + H * 0.78f, -0.025f), new Vector2(W * 0.84f, 0.8f), TMPro.TextAlignmentOptions.Left);
            var total = text("Total", "/19", 3f, new Vector3(0.05f, Y + H * 0.64f, -0.025f), new Vector2(W * 0.84f, 0.3f), TMPro.TextAlignmentOptions.Right);

            // progress bar: dark track, fill scaled from its left edge
            float barY = Y + H * 0.42f, barW = W * 0.84f;
            cube("ProgressTrack", root.transform, new Vector3(0, barY, -0.021f), new Vector3(barW, 0.05f, 0.004f), frame);
            var pivot = new GameObject("ProgressFill").transform;
            pivot.SetParent(root.transform, false);
            pivot.localPosition = new Vector3(-barW / 2, barY, -0.024f);
            cube("Fill", pivot, new Vector3(barW / 2, 0, 0), new Vector3(barW, 0.05f, 0.004f), dark);

            // a row of hazard/feature icons (plain tiles - lit ones are dark)
            for (int i = 0; i < 5; i++)
            {
                cube("Icon_" + i, root.transform, new Vector3(-barW / 2 + 0.09f + i * (barW - 0.18f) / 4, Y + H * 0.2f, -0.022f), new Vector3(0.14f, 0.14f, 0.004f), i % 2 == 0 ? dark : frame);
            }

            var sign = root.AddComponent<ChamberSign>();
            var so = new SerializedObject(sign);
            so.FindProperty("numberText").objectReferenceValue = number;
            so.FindProperty("totalText").objectReferenceValue = total;
            so.FindProperty("progressFill").objectReferenceValue = pivot;
            so.ApplyModifiedPropertiesWithoutUndo();

            string path = $"{KitRoot}/Chamber/ChamberSign.prefab";
            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }

        // ---------- clean, Portal-like dressing for the lift room ----------

        // Places a prop so its renderers' bounds sit with their bottom-centre on `bottomCentre`.
        private static GameObject Prop(string path, Transform parent, string name, Vector3 bottomCentre, float yaw, Vector3 scale)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                Debug.LogWarning($"[LevelKit] Prop {path} not found.");
                return null;
            }
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            go.name = name;
            go.transform.localRotation = Quaternion.Euler(0, yaw, 0);
            go.transform.localScale = Vector3.Scale(go.transform.localScale, scale);
            go.transform.localPosition = Vector3.zero;
            var renderers = go.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return go;
            Bounds b = renderers[0].bounds;
            foreach (var r in renderers) b.Encapsulate(r.bounds);
            Vector3 current = parent.InverseTransformPoint(new Vector3(b.center.x, b.min.y, b.center.z));
            go.transform.localPosition = bottomCentre - current;
            return go;
        }

        // Room (connector space): x +-6.9, floor 0, ceiling 7.6, z from roomBack to roomFront; lift at liftZ.
        private static void DressLiftRoom(Transform root, float roomBack, float roomFront, float liftZ)
        {
            var dress = new GameObject("Dressing_Clean").transform;
            dress.SetParent(root, false);
            const string pipe = "Assets/ThirdParty/Portal2AssetCollection/Prefabs/default_triple_wallpipe_straight_a_model.dmx.019.prefab";
            const string grid = "Assets/Core/Portal/Prefabs/Props/default_squarebeam_off_8x8_model.dmx.002.prefab";
            const float half = 6.9f, ceiling = LiftRoomWallHeight;

            // pipes running along both side walls and the back wall
            for (float z = roomBack + 1.4f; z < roomFront - 1.0f; z += 2.4f)
            {
                Prop(pipe, dress, "Pipe_L", new Vector3(-half + 0.25f, 3.6f, z), 0, Vector3.one);
                Prop(pipe, dress, "Pipe_R", new Vector3(half - 0.25f, 3.6f, z), 180, Vector3.one);
            }
            for (float x = -half + 1.4f; x < half - 1.0f; x += 2.4f)
            {
                Prop(pipe, dress, "Pipe_Back", new Vector3(x, 3.6f, roomBack + 0.25f), 90, Vector3.one);
            }
            // beam grid under the ceiling
            Prop(grid, dress, "CeilingBeams", new Vector3(0, ceiling - 0.6f, (roomBack + roomFront) / 2), 0, new Vector3(1.35f, 1, 1.55f));

            // glowing strips along the walls + spots on the lift
            var strip = LightStripMaterial();
            System.Action<string, Vector3, Vector3> bar = (n, pos, size) =>
            {
                var c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                c.name = n;
                Object.DestroyImmediate(c.GetComponent<Collider>());
                c.transform.SetParent(dress, false);
                c.transform.localPosition = pos;
                c.transform.localScale = size;
                c.GetComponent<Renderer>().sharedMaterial = strip;
                c.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            };
            float len = roomFront - roomBack - 1.0f, mid = (roomBack + roomFront) / 2;
            bar("WallStrip_L", new Vector3(-half + 0.05f, 2.4f, mid), new Vector3(0.06f, 0.08f, len));
            bar("WallStrip_R", new Vector3(half - 0.05f, 2.4f, mid), new Vector3(0.06f, 0.08f, len));
            bar("WallStrip_Back", new Vector3(0, 2.4f, roomBack + 0.05f), new Vector3(half * 2 - 1.0f, 0.08f, 0.06f));
            foreach (var corner in new[] { new Vector3(-5.5f, ceiling - 0.8f, roomBack + 1.5f), new Vector3(5.5f, ceiling - 0.8f, roomFront - 1.5f) })
            {
                var spot = new GameObject("Spot").AddComponent<Light>();
                spot.transform.SetParent(dress, false);
                spot.transform.localPosition = corner;
                spot.transform.LookAt(dress.TransformPoint(new Vector3(0, 1.5f, liftZ)));
                spot.type = LightType.Spot;
                spot.spotAngle = 45;
                spot.range = 16;
                spot.intensity = 12;
                spot.color = new Color(0.86f, 0.93f, 1f);
            }
        }

        // A lift (your LiftPlatform variant) with its open front closed off by a wall, and a short
        // corridor out of it. Pivot = the corridor's mouth at floor level, +Z pointing out of it, so a
        // ChamberShell can put it straight against a wall opening. Both get the round Door in the mouth;
        // the entrance one opens by itself as you walk up to it, the exit one is wired to the puzzle.
        private static GameObject BuildLiftConnector(string name, string liftVariant, bool isExit)
        {
            var root = new GameObject(name);
            float w = ChamberShell.Panel, h = CorridorHeight, l = CorridorLength, t = Thickness;

            var liftPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{GameplayRoot}LiftPlatform_variants/{liftVariant}.prefab");
            if (liftPrefab == null)
            {
                Debug.LogWarning($"[LevelKit] {liftVariant} not found - {name} built without a lift.");
            }
            else
            {
                var lift = (GameObject)PrefabUtility.InstantiatePrefab(liftPrefab, root.transform);
                lift.name = "Lift";
                lift.transform.localPosition = new Vector3(0, -LiftLandingY, -l - t - LiftFrontZ);
                // The ring tube has no floor or walls; the corridor below replaces it.
                Transform tube = lift.transform.Find("elevator_entrance");
                if (tube != null) tube.gameObject.SetActive(false);
                Transform arcs = lift.transform.Find("elevator_frame_section");   // belongs to that tube
                if (arcs != null) arcs.gameObject.SetActive(false);
                DressLiftRoom(root.transform, -l - t - LiftFrontZ - 7.99f, -l - t, -l - t - LiftFrontZ);
            }

            var parts = new GameObject("Corridor").transform;
            parts.SetParent(root.transform, false);
            GameObject wall = Kit("Surfaces/Wall_NonPortalable"), floor = Kit("Surfaces/Floor_NonPortalable"), ceiling = Kit("Surfaces/Ceiling_NonPortalable");
            KitBox(floor, parts, "Floor", new Vector3(-w / 2, -t, -l - t), new Vector3(w / 2, 0, 0));
            KitBox(ceiling, parts, "Ceiling", new Vector3(-w / 2 - t, h, -l - t), new Vector3(w / 2 + t, h + t, 0));
            KitBox(wall, parts, "Wall_L", new Vector3(-w / 2 - t, -t, -l - t), new Vector3(-w / 2, h, 0));
            KitBox(wall, parts, "Wall_R", new Vector3(w / 2, -t, -l - t), new Vector3(w / 2 + t, h, 0));
            // Closes the lift room's open front, leaving the corridor opening.
            float z0 = -l - t, z1 = -l;
            KitBox(wall, parts, "LiftFront_L", new Vector3(-LiftRoomHalfWidth, -t, z0), new Vector3(-w / 2 - t, LiftRoomWallHeight, z1));
            KitBox(wall, parts, "LiftFront_R", new Vector3(w / 2 + t, -t, z0), new Vector3(LiftRoomHalfWidth, LiftRoomWallHeight, z1));
            KitBox(wall, parts, "LiftFront_Above", new Vector3(-w / 2 - t, h, z0), new Vector3(w / 2 + t, LiftRoomWallHeight, z1));

            var stripBar = GameObject.CreatePrimitive(PrimitiveType.Cube);
            stripBar.name = "CorridorLightStrip";
            Object.DestroyImmediate(stripBar.GetComponent<Collider>());
            stripBar.transform.SetParent(parts, false);
            stripBar.transform.localPosition = new Vector3(0, h - 0.03f, -l / 2 - t / 2);
            stripBar.transform.localScale = new Vector3(0.25f, 0.06f, l - 0.4f);
            stripBar.GetComponent<Renderer>().sharedMaterial = LightStripMaterial();
            var lamp = new GameObject("CorridorLight").AddComponent<Light>();
            lamp.transform.SetParent(parts, false);
            lamp.transform.localPosition = new Vector3(0, h - 0.4f, -l / 2);
            lamp.type = LightType.Point;
            lamp.range = 6.0f;
            lamp.intensity = 3.0f;
            lamp.color = new Color(0.86f, 0.93f, 1f);

            var doorPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(GameplayRoot + "Door.prefab");
            if (doorPrefab != null)
            {
                var door = (GameObject)PrefabUtility.InstantiatePrefab(doorPrefab, root.transform);
                door.name = isExit ? "ExitDoor" : "EntranceDoor";
                door.transform.localPosition = new Vector3(0, 0, t / 2);   // in the chamber wall's opening

                if (!isExit)
                {
                    // Walk up the corridor and the entrance door opens (and stays open).
                    var opener = new GameObject("DoorOpener");
                    opener.transform.SetParent(root.transform, false);
                    opener.transform.localPosition = new Vector3(0, h / 2, -l * 0.6f);
                    var trigger = opener.AddComponent<BoxCollider>();
                    trigger.isTrigger = true;
                    trigger.size = new Vector3(w, h, l * 0.8f);
                    var evt = opener.AddComponent<UnityEventTrigger>();
                    evt.shouldActivateOnCollision = true;
                    evt.targetEvent = new UnityEngine.Events.UnityEvent();
                    UnityEditor.Events.UnityEventTools.AddPersistentListener(evt.targetEvent, door.GetComponent<TheOneFramework.Portals.Door>().Open);
                }
            }

            string path = $"{KitRoot}/Chamber/{name}.prefab";
            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }

        private static GameObject Kit(string path) => AssetDatabase.LoadAssetAtPath<GameObject>($"{KitRoot}/{path}.prefab");

        private static void KitBox(GameObject prefab, Transform parent, string name, Vector3 min, Vector3 max)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            go.name = name;
            go.transform.localPosition = (min + max) * 0.5f;
            go.transform.localScale = max - min;
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
