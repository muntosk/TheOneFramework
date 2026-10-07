using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>
/// Converts a folder of .vmt/.vtf files into regular, editable Unity assets:
/// .png textures (normal maps already converted) and URP .mat materials, mirrored
/// into a "Baked" folder next to the "materials" folder. Re-baking updates the
/// existing assets in place, so references in scenes keep working.
/// </summary>
public static class VmtBaker
{
    const string DefaultSourceFolder = "Assets/ThirdParty/Portal2HarunExport/Export";

    class BakedTexture
    {
        public string PngPath;
        public bool IsNormal;
        public bool HasAlpha;
        public bool ClampU, ClampV, PointSample;
    }

    [MenuItem("Tools/Portal 2/Bake VMTs To Editable Materials")]
    [MenuItem("Assets/Portal 2/Bake VMTs To Editable Materials")]
    static void BakeFromMenu() => Bake(GetSourceFolder());

    /// <summary>Uses the selected Project folder if it contains .vmt files, else the default folder.</summary>
    static string GetSourceFolder()
    {
        foreach (var guid in Selection.assetGUIDs)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (AssetDatabase.IsValidFolder(path) &&
                Directory.GetFiles(path, "*.vmt", SearchOption.AllDirectories).Length > 0)
                return path;
        }
        return DefaultSourceFolder;
    }

    public static void Bake(string sourceFolder)
    {
        if (!AssetDatabase.IsValidFolder(sourceFolder))
        {
            Debug.LogError($"[VmtBaker] Folder '{sourceFolder}' does not exist.");
            return;
        }

        string materialsRoot = VmtMaterialBuilder.FindMaterialsRoot(sourceFolder);
        string outputRoot = Path.GetDirectoryName(materialsRoot)?.Replace('\\', '/') + "/Baked";

        var vmtPaths = Directory.GetFiles(sourceFolder, "*.vmt", SearchOption.AllDirectories);
        var vmts = new VmtData[vmtPaths.Length];
        for (int i = 0; i < vmtPaths.Length; i++)
        {
            vmtPaths[i] = vmtPaths[i].Replace('\\', '/');
            vmts[i] = VmtData.Parse(File.ReadAllText(vmtPaths[i]));
        }

        try
        {
            // Pass 1: find out which .vtf files are needed, and whether as colour, normal map or SSBump.
            var textureKinds = new Dictionary<string, VtfTextureKind>();
            for (int i = 0; i < vmtPaths.Length; i++)
            {
                string vmtPath = vmtPaths[i];
                var probe = VmtMaterialBuilder.Build("probe", vmts[i], (texRef, kind) =>
                {
                    string vtfPath = VmtMaterialBuilder.ResolveTexturePath(vmtPath, texRef);
                    if (vtfPath == null)
                        Debug.LogWarning($"[VmtBaker] Could not find texture '{texRef}' referenced by '{vmtPath}'.");
                    else if (!textureKinds.ContainsKey(vtfPath))
                        textureKinds[vtfPath] = kind;
                    return null;
                });
                if (probe == null)
                {
                    Debug.LogError("[VmtBaker] Could not find the 'Universal Render Pipeline/Lit' shader.");
                    return;
                }
                Object.DestroyImmediate(probe);
                Directory.CreateDirectory(Path.GetDirectoryName(OutputPath(materialsRoot, outputRoot, vmtPath, ".mat")));
            }

            // Pass 2: write the textures as .png.
            var baked = new Dictionary<string, BakedTexture>();
            int done = 0;
            foreach (var job in textureKinds)
            {
                EditorUtility.DisplayProgressBar("Baking VTF textures", job.Key, done++ / (float)textureKinds.Count);
                var result = WritePng(job.Key, job.Value, OutputPath(materialsRoot, outputRoot, job.Key, ".png"));
                if (result != null) baked[job.Key] = result;
            }

            AssetDatabase.Refresh();
            AssetDatabase.StartAssetEditing();
            try
            {
                foreach (var tex in baked.Values)
                    ConfigureTextureImporter(tex);
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }

            // Pass 3: build the materials against the baked textures.
            for (int i = 0; i < vmtPaths.Length; i++)
            {
                string vmtPath = vmtPaths[i];
                EditorUtility.DisplayProgressBar("Baking VMT materials", vmtPath, i / (float)vmtPaths.Length);

                string matPath = OutputPath(materialsRoot, outputRoot, vmtPath, ".mat");
                string name = Path.GetFileNameWithoutExtension(matPath);
                var mat = VmtMaterialBuilder.Build(name, vmts[i], (texRef, kind) =>
                {
                    string vtfPath = VmtMaterialBuilder.ResolveTexturePath(vmtPath, texRef);
                    return vtfPath != null && baked.TryGetValue(vtfPath, out var tex)
                        ? AssetDatabase.LoadAssetAtPath<Texture2D>(tex.PngPath)
                        : null;
                });

                var existing = AssetDatabase.LoadAssetAtPath<Material>(matPath);
                if (existing != null)
                {
                    EditorUtility.CopySerialized(mat, existing);
                    existing.name = name;
                    EditorUtility.SetDirty(existing);
                    Object.DestroyImmediate(mat);
                }
                else
                {
                    AssetDatabase.CreateAsset(mat, matPath);
                }
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[VmtBaker] Baked {vmtPaths.Length} materials and {baked.Count} textures into '{outputRoot}'.");
            EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<Object>(outputRoot));
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
    }

    /// <summary>Mirrors a path under the materials root into the output root, with a new extension.</summary>
    static string OutputPath(string materialsRoot, string outputRoot, string sourcePath, string extension)
    {
        string rel = sourcePath.StartsWith(materialsRoot + "/", StringComparison.OrdinalIgnoreCase)
            ? sourcePath.Substring(materialsRoot.Length + 1)
            : Path.GetFileName(sourcePath);
        return outputRoot + "/" + Path.ChangeExtension(rel, extension);
    }

    static BakedTexture WritePng(string vtfPath, VtfTextureKind kind, string pngPath)
    {
        VtfImage img;
        try
        {
            img = VtfDecoder.Decode(File.ReadAllBytes(vtfPath));
        }
        catch (Exception e)
        {
            Debug.LogError($"[VmtBaker] Could not bake '{vtfPath}': {e.Message}");
            return null;
        }

        // A manual override on the .vtf's own importer wins over what the .vmt says.
        bool flipGreen = true;
        if (AssetImporter.GetAtPath(vtfPath) is VtfImporter importer)
        {
            flipGreen = importer.flipGreen;
            if (importer.kind != VtfTextureKind.Auto) kind = importer.kind;
        }

        var pixels = img.Mips[0];
        VtfDecoder.ConvertForUnity(pixels, kind, flipGreen);

        var tex = new Texture2D(img.Width, img.Height, TextureFormat.RGBA32, false, kind != VtfTextureKind.Color);
        tex.SetPixels32(pixels);
        Directory.CreateDirectory(Path.GetDirectoryName(pngPath));
        File.WriteAllBytes(pngPath, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);

        return new BakedTexture
        {
            PngPath = pngPath,
            IsNormal = kind != VtfTextureKind.Color,
            HasAlpha = kind == VtfTextureKind.Color && VtfDecoder.HasAlpha(pixels),
            ClampU = img.ClampU,
            ClampV = img.ClampV,
            PointSample = img.PointSample,
        };
    }

    static void ConfigureTextureImporter(BakedTexture tex)
    {
        if (!(AssetImporter.GetAtPath(tex.PngPath) is TextureImporter ti)) return;

        ti.textureType = tex.IsNormal ? TextureImporterType.NormalMap : TextureImporterType.Default;
        ti.sRGBTexture = !tex.IsNormal;
        ti.mipmapEnabled = true;
        ti.alphaSource = tex.HasAlpha ? TextureImporterAlphaSource.FromInput : TextureImporterAlphaSource.None;
        ti.alphaIsTransparency = tex.HasAlpha;
        ti.wrapModeU = tex.ClampU ? TextureWrapMode.Clamp : TextureWrapMode.Repeat;
        ti.wrapModeV = tex.ClampV ? TextureWrapMode.Clamp : TextureWrapMode.Repeat;
        ti.filterMode = tex.PointSample ? FilterMode.Point : FilterMode.Trilinear;
        ti.anisoLevel = 4;
        ti.SaveAndReimport();
    }
}
