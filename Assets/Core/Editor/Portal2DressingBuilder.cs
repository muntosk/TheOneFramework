using System.Collections.Generic;
using System.IO;
using System.Linq;
using TheOneFramework.Utilities;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace TheOneFramework.EditorTools
{
    // Turns the baked Portal 2 stain/debris/moss/graffiti textures into URP decal prefabs, plus
    // ready-made DressingScatter prefabs (grime, debris, overgrowth) that use them. Run after
    // "Bake VMTs To Editable Materials". Re-running updates everything in place (same GUIDs), so
    // decals already placed in scenes keep working.
    public static class Portal2DressingBuilder
    {
        private const string BakedRoot = "Assets/ThirdParty/Portal2HarunExport/Baked/";
        private const string SourceRoot = "Assets/ThirdParty/Portal2HarunExport/Export/";
        private const string OutputRoot = "Assets/Demo/Dressing";
        private const string DecalFolder = OutputRoot + "/Prefabs/Decals";
        private const string ScatterFolder = OutputRoot + "/Prefabs/Scatter";
        private const string DecalMaterialFolder = OutputRoot + "/Materials/Decals";
        private const string DecalTextureFolder = OutputRoot + "/Textures/Decals";
        // Folders of .fbx models converted from Portal 2 .mdl (Blender + SourceIO).
        private static readonly string[] ModelFolders = { OutputRoot + "/Models/Vines", OutputRoot + "/Models/LiftShaft", OutputRoot + "/Models/Electrical", OutputRoot + "/Models/Ending", OutputRoot + "/Models/Facility", "Assets/Core/Portal/Models" };
        private const string BakedModelMaterials = BakedRoot + "models";

        // Source engine units -> metres (same factor the imported .smd models use).
        private const float UnitsToMetres = 0.01905f;
        private const float DefaultDecalScale = 0.25f;
        private const float ProjectionDepth = 0.5f;
        // Overlays have no real size in Source (the mapper stretches them), so cap the longest side.
        private const float MaxDecalSize = 3.0f;

        private static readonly string[] Grime =
        {
            "decals/decalstain", "decals/prodpipestain01", "overlays/stain_", "overlays/blacktile_stain01",
            "overlays/tile_crack_stain001a", "overlays/dirtartifacts01a", "overlays/office_stain_", "overlays/tideline01b",
        };
        private static readonly string[] DebrisDecals =
        {
            "decals/debris_concrete001a", "decals/debris_tile001a", "decals/plaster", "overlays/debris_overlay",
        };
        private static readonly string[] Overgrowth =
        {
            "decals/ivy02", "decals/ivy03", "decals/moss_wall_decal", "overlays/overlay_moss01",
        };
        private static readonly string[] RatMan = { "overlays/ratman_" };

        private static readonly (string path, float weight)[] DebrisModels =
        {
            ("Assets/Core/Portal/Prefabs/Props/Body_smd_debris_blacktile_cluster01.smd.prefab", 1.0f),
            ("Assets/Core/Portal/Prefabs/Props/Body_smd_debris_dest_phys_02.smd.prefab", 1.0f),
            ("Assets/Core/Portal/Prefabs/Props/Body_smd_tile_broken_32_32.smd.008.prefab", 1.5f),
            ("Assets/Core/Portal/Prefabs/Props/Body_smd_rubble_ceiling_256_256.smd.prefab", 0.3f),
        };

        [MenuItem("Tools/Portal 2/Build Set Dressing (Decals + Scatter Prefabs)")]
        private static void Build()
        {
            Shader decalShader = Shader.Find("Shader Graphs/Decal");
            if (decalShader == null)
            {
                Debug.LogError("[Portal2Dressing] Could not find the URP 'Shader Graphs/Decal' shader.");
                return;
            }

            EnsureFolder(DecalMaterialFolder);
            EnsureFolder(DecalTextureFolder);

            try
            {
                List<GameObject> grime = BuildDecals(decalShader, "Grime", Grime);
                List<GameObject> debrisDecals = BuildDecals(decalShader, "Debris", DebrisDecals);
                List<GameObject> overgrowth = BuildDecals(decalShader, "Overgrowth", Overgrowth);
                BuildDecals(decalShader, "RatMan", RatMan);

                var debrisEntries = new List<(GameObject, float)>();
                foreach (var (path, weight) in DebrisModels)
                {
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    if (prefab != null) debrisEntries.Add((prefab, weight));
                    else Debug.LogWarning($"[Portal2Dressing] Missing debris prefab {path}");
                }
                debrisEntries.AddRange(debrisDecals.Select(d => (d, 0.5f)));
                debrisEntries.AddRange(grime.Select(d => (d, 0.15f)));

                BuildScatter("GrimeScatter", grime.Select(d => (d, 1.0f)), count: 10, radius: 3.0f, maxTilt: 0.0f);
                BuildScatter("DebrisScatter", debrisEntries, count: 18, radius: 2.0f, maxTilt: 25.0f);
                BuildScatter("OvergrowthScatter", overgrowth.Select(d => (d, 1.0f)), count: 5, radius: 1.5f, maxTilt: 0.0f);

                RemapModelMaterials();
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[Portal2Dressing] Done - see {OutputRoot}.");
        }

        // The converted .fbx material slots carry the original .vmt file names, so point each slot at
        // the baked material of that name (searched across all baked model materials).
        private static void RemapModelMaterials()
        {
            var baked = new Dictionary<string, Material>(System.StringComparer.OrdinalIgnoreCase);
            foreach (string guid in AssetDatabase.FindAssets("t:Material", new[] { BakedModelMaterials }))
            {
                var mat = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
                baked[mat.name] = mat;
            }

            foreach (string guid in AssetDatabase.FindAssets("t:Model", ModelFolders))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var importer = (ModelImporter)AssetImporter.GetAtPath(path);
                importer.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;

                foreach (var renderer in AssetDatabase.LoadAllAssetsAtPath(path).OfType<Renderer>())
                {
                    foreach (Material slot in renderer.sharedMaterials)
                    {
                        if (slot == null) continue;
                        if (baked.TryGetValue(slot.name, out Material target))
                            importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), slot.name), target);
                        else
                            Debug.LogWarning($"[Portal2Dressing] No baked material '{slot.name}' for {path} - bake Export first?");
                    }
                }
                importer.SaveAndReimport();
            }
        }

        private static List<GameObject> BuildDecals(Shader shader, string category, string[] prefixes)
        {
            var result = new List<GameObject>();
            string prefabFolder = $"{DecalFolder}/{category}";
            EnsureFolder(prefabFolder);

            foreach (string bakedMatPath in FindBakedMaterials(prefixes))
            {
                string name = Path.GetFileNameWithoutExtension(bakedMatPath);
                EditorUtility.DisplayProgressBar("Building decals", name, 0.5f);

                var baked = AssetDatabase.LoadAssetAtPath<Material>(bakedMatPath);
                var texture = baked != null && baked.HasProperty("_BaseMap") ? baked.GetTexture("_BaseMap") as Texture2D : null;
                if (texture == null)
                {
                    Debug.LogWarning($"[Portal2Dressing] {bakedMatPath} has no texture - bake first?");
                    continue;
                }

                string vmt = ReadSourceVmt(bakedMatPath);
                if (vmt != null && vmt.TrimStart('\uFEFF', ' ', '\t', '\r', '\n', '"').StartsWith("DecalModulate", System.StringComparison.OrdinalIgnoreCase))
                {
                    texture = ConvertModulate(texture, name);
                }

                Material mat = CreateOrUpdate($"{DecalMaterialFolder}/{name}.mat", () => new Material(shader), m =>
                {
                    m.shader = shader;
                    if (!m.HasProperty("Base_Map")) Debug.LogWarning("[Portal2Dressing] Decal shader has no Base_Map property.");
                    m.SetTexture("Base_Map", texture);
                    m.enableInstancing = true;
                });

                float scale = ParseDecalScale(vmt);
                float width = texture.width * scale * UnitsToMetres;
                float height = texture.height * scale * UnitsToMetres;
                float shrink = Mathf.Min(1.0f, MaxDecalSize / Mathf.Max(width, height));
                width *= shrink;
                height *= shrink;

                var go = new GameObject(name);
                go.transform.rotation = Quaternion.Euler(90.0f, 0.0f, 0.0f); // projects downwards when dropped on a floor
                var projector = go.AddComponent<DecalProjector>();
                projector.material = mat;
                projector.size = new Vector3(width, height, ProjectionDepth);
                projector.pivot = Vector3.zero;

                string prefabPath = $"{prefabFolder}/{name}.prefab";
                result.Add(PrefabUtility.SaveAsPrefabAsset(go, prefabPath));
                Object.DestroyImmediate(go);
            }

            Debug.Log($"[Portal2Dressing] {category}: {result.Count} decal(s)");
            return result;
        }

        private static IEnumerable<string> FindBakedMaterials(string[] prefixes)
        {
            var found = new SortedSet<string>();
            foreach (string prefix in prefixes)
            {
                string folder = BakedRoot + Path.GetDirectoryName(prefix);
                string filePrefix = Path.GetFileName(prefix);
                if (!AssetDatabase.IsValidFolder(folder)) continue;

                foreach (string guid in AssetDatabase.FindAssets("t:Material", new[] { folder }))
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    string file = Path.GetFileNameWithoutExtension(path);
                    if (Path.GetDirectoryName(path)?.Replace('\\', '/') == folder &&
                        file.StartsWith(filePrefix, System.StringComparison.OrdinalIgnoreCase) &&
                        !file.Contains("_subrect") && !file.EndsWith("model"))
                    {
                        found.Add(path);
                    }
                }
            }
            return found;
        }

        private static string ReadSourceVmt(string bakedMatPath)
        {
            string rel = bakedMatPath.Substring(BakedRoot.Length);
            string vmtPath = SourceRoot + Path.ChangeExtension(rel, ".vmt");
            return File.Exists(vmtPath) ? File.ReadAllText(vmtPath) : null;
        }

        private static float ParseDecalScale(string vmt)
        {
            if (vmt == null) return DefaultDecalScale;
            var match = System.Text.RegularExpressions.Regex.Match(vmt, @"\$decalscale""?\s+""?([0-9.]+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            return match.Success && float.TryParse(match.Groups[1].Value, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out float s) && s > 0.0f ? s : DefaultDecalScale;
        }

        // Source's DecalModulate darkens below mid-grey and brightens above it, with no alpha. URP decals
        // alpha-blend instead, so turn "how much darker than grey" into a black colour with that much alpha.
        private static Texture2D ConvertModulate(Texture2D source, string name)
        {
            string sourcePath = AssetDatabase.GetAssetPath(source);
            var readable = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            readable.LoadImage(File.ReadAllBytes(sourcePath));

            Color32[] pixels = readable.GetPixels32();
            for (int i = 0; i < pixels.Length; i++)
            {
                float lum = (pixels[i].r * 0.299f + pixels[i].g * 0.587f + pixels[i].b * 0.114f) / 255.0f;
                byte alpha = (byte)(Mathf.Clamp01((0.5f - lum) * 2.0f) * 255.0f);
                pixels[i] = new Color32(0, 0, 0, alpha);
            }
            readable.SetPixels32(pixels);

            string outPath = $"{DecalTextureFolder}/{name}.png";
            File.WriteAllBytes(outPath, readable.EncodeToPNG());
            Object.DestroyImmediate(readable);

            AssetDatabase.ImportAsset(outPath);
            var importer = (TextureImporter)AssetImporter.GetAtPath(outPath);
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(outPath);
        }

        private static void BuildScatter(string name, IEnumerable<(GameObject prefab, float weight)> entries,
            int count, float radius, float maxTilt)
        {
            var list = entries.Where(e => e.prefab != null).ToList();
            var go = new GameObject(name);
            var scatter = go.AddComponent<DressingScatter>();

            var so = new SerializedObject(scatter);
            SerializedProperty array = so.FindProperty("entries");
            array.arraySize = list.Count;
            for (int i = 0; i < list.Count; i++)
            {
                SerializedProperty element = array.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("prefab").objectReferenceValue = list[i].prefab;
                element.FindPropertyRelative("weight").floatValue = list[i].weight;
            }
            so.FindProperty("count").intValue = count;
            so.FindProperty("radius").floatValue = radius;
            so.FindProperty("maxTilt").floatValue = maxTilt;
            so.ApplyModifiedPropertiesWithoutUndo();

            EnsureFolder(ScatterFolder);
            PrefabUtility.SaveAsPrefabAsset(go, $"{ScatterFolder}/{name}.prefab");
            Object.DestroyImmediate(go);
        }

        // Updates an existing asset in place (keeps its GUID) or creates it.
        private static T CreateOrUpdate<T>(string path, System.Func<T> create, System.Action<T> setup) where T : Object
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null)
            {
                setup(existing);
                EditorUtility.SetDirty(existing);
                return existing;
            }

            T asset = create();
            setup(asset);
            AssetDatabase.CreateAsset(asset, path);
            return asset;
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
