using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Imports Valve Material Type (.vmt) files as URP/Lit (or Standard) Materials.
/// Resolves $basetexture / $bumpmap references against sibling .vtf files
/// (which VtfImporter turns into Texture2D assets) by walking up to the
/// "materials" root folder, matching case-insensitively like Source does.
/// </summary>
[ScriptedImporter(1, "vmt", importQueueOffset: 1)]
public class VmtImporter : ScriptedImporter
{
    public override void OnImportAsset(AssetImportContext ctx)
    {
        string text = File.ReadAllText(ctx.assetPath);
        var kv = ParseVmt(text);

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        if (shader == null)
        {
            ctx.LogImportError("Could not find 'Universal Render Pipeline/Lit' or 'Standard' shader.");
            return;
        }

        var mat = new Material(shader) { name = Path.GetFileNameWithoutExtension(ctx.assetPath) };
        string materialsRoot = FindMaterialsRoot(ctx.assetPath);

        if (kv.TryGetValue("$basetexture", out var baseTexRef))
        {
            var tex = ResolveTexture(materialsRoot, baseTexRef, ctx);
            if (tex != null)
            {
                if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex);
                if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", tex);
            }
        }

        if (kv.TryGetValue("$bumpmap", out var bumpRef))
        {
            var tex = ResolveTexture(materialsRoot, bumpRef, ctx);
            if (tex != null && mat.HasProperty("_BumpMap"))
            {
                mat.SetTexture("_BumpMap", tex);
                mat.EnableKeyword("_NORMALMAP");
            }
        }

        if (kv.ContainsKey("$translucent"))
        {
            mat.SetFloat("_Surface", 1f);
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.renderQueue = (int)RenderQueue.Transparent;
        }
        else if (kv.ContainsKey("$alphatest"))
        {
            if (mat.HasProperty("_AlphaClip")) mat.SetFloat("_AlphaClip", 1f);
            mat.EnableKeyword("_ALPHATEST_ON");
            mat.renderQueue = (int)RenderQueue.AlphaTest;
        }

        ctx.AddObjectToAsset("material", mat);
        ctx.SetMainObject(mat);
    }

    static Dictionary<string, string> ParseVmt(string text)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var lines = text.Replace("\r\n", "\n").Split('\n');

        foreach (var rawLine in lines)
        {
            string line = rawLine;
            int commentIdx = line.IndexOf("//", StringComparison.Ordinal);
            if (commentIdx >= 0) line = line.Substring(0, commentIdx);
            line = line.Trim();

            if (line.Length == 0 || line[0] != '$') continue;

            int split = -1;
            for (int i = 0; i < line.Length; i++)
            {
                if (char.IsWhiteSpace(line[i])) { split = i; break; }
            }
            if (split < 0) continue;

            string key = line.Substring(0, split).ToLowerInvariant();
            string value = line.Substring(split + 1).Trim().Trim('"').Trim();
            if (value.Length > 0 && !result.ContainsKey(key))
                result[key] = value;
        }

        return result;
    }

    static string FindMaterialsRoot(string assetPath)
    {
        string dir = Path.GetDirectoryName(assetPath)?.Replace('\\', '/');
        while (!string.IsNullOrEmpty(dir))
        {
            if (string.Equals(Path.GetFileName(dir), "materials", StringComparison.OrdinalIgnoreCase))
                return dir;
            dir = Path.GetDirectoryName(dir)?.Replace('\\', '/');
        }
        return Path.GetDirectoryName(assetPath)?.Replace('\\', '/');
    }

    static Texture2D ResolveTexture(string materialsRoot, string vmtRef, AssetImportContext ctx)
    {
        if (string.IsNullOrEmpty(materialsRoot)) return null;

        string rel = vmtRef.Replace('\\', '/').TrimStart('/');
        if (rel.EndsWith(".vtf", StringComparison.OrdinalIgnoreCase))
            rel = rel.Substring(0, rel.Length - 4);

        string candidate = FindCaseInsensitive(materialsRoot, rel + ".vtf");
        if (candidate == null)
        {
            ctx.LogImportWarning($"Could not find texture '{vmtRef}' referenced by '{ctx.assetPath}'.");
            return null;
        }

        AssetDatabase.ImportAsset(candidate, ImportAssetOptions.ForceSynchronousImport);
        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(candidate);
        if (tex != null)
            ctx.DependsOnArtifact(candidate);
        return tex;
    }

    static string FindCaseInsensitive(string root, string relativePath)
    {
        string direct = Path.Combine(root, relativePath).Replace('\\', '/');
        if (File.Exists(direct)) return direct;

        string[] parts = relativePath.Split('/');
        string current = root;

        for (int i = 0; i < parts.Length; i++)
        {
            if (!Directory.Exists(current)) return null;
            bool isLast = i == parts.Length - 1;
            string match = null;

            if (isLast)
            {
                foreach (var f in Directory.GetFiles(current))
                {
                    if (string.Equals(Path.GetFileName(f), parts[i], StringComparison.OrdinalIgnoreCase))
                    {
                        match = f;
                        break;
                    }
                }
            }
            else
            {
                foreach (var d in Directory.GetDirectories(current))
                {
                    if (string.Equals(Path.GetFileName(d), parts[i], StringComparison.OrdinalIgnoreCase))
                    {
                        match = d;
                        break;
                    }
                }
            }

            if (match == null) return null;
            current = match;
        }

        return current.Replace('\\', '/');
    }
}
