using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// A parsed .vmt: the shader name plus the effective $parameters. Root-level keys
/// are used, overridden by DX9 / HDR_DX9 fallback blocks (what Portal 2 actually renders);
/// DX8 and "&lt;dx90" fallbacks are ignored.
/// </summary>
public class VmtData
{
    public string Shader = "";
    public readonly Dictionary<string, string> Params = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    public bool Has(string key) => Params.ContainsKey(key);
    public string Get(string key) => Params.TryGetValue(key, out var v) ? v : null;

    public bool IsTrue(string key)
    {
        var v = Get(key);
        return v != null && v != "0" && !v.Equals("false", StringComparison.OrdinalIgnoreCase);
    }

    public float Float(string key, float fallback)
    {
        var v = Get(key);
        return v != null && float.TryParse(v.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var f) ? f : fallback;
    }

    /// <summary>Parses "[r g b]" (0-1), "{r g b}" (0-255) or a single number.</summary>
    public Color Color(string key, Color fallback)
    {
        var v = Get(key)?.Trim();
        if (string.IsNullOrEmpty(v)) return fallback;

        float scale = v.StartsWith("{") ? 1f / 255f : 1f;
        var parts = v.Trim('[', ']', '{', '}').Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
        var nums = new List<float>();
        foreach (var p in parts)
            if (float.TryParse(p, NumberStyles.Float, CultureInfo.InvariantCulture, out var f))
                nums.Add(f * scale);

        if (nums.Count >= 3) return new Color(nums[0], nums[1], nums[2], 1f);
        if (nums.Count == 1) return new Color(nums[0], nums[0], nums[0], 1f);
        return fallback;
    }

    class Block
    {
        public readonly List<KeyValuePair<string, string>> Values = new List<KeyValuePair<string, string>>();
        public readonly List<KeyValuePair<string, Block>> Children = new List<KeyValuePair<string, Block>>();
    }

    public static VmtData Parse(string text)
    {
        var data = new VmtData();
        var tokens = Tokenize(text);
        if (tokens.Count < 2) return data;

        data.Shader = tokens[0].text;
        int i = 1;
        if (tokens[i].text != "{" || tokens[i].quoted) return data;
        i++;
        var root = ParseBlock(tokens, ref i);

        data.Apply(root);
        foreach (var child in root.Children)
        {
            string n = child.Key.ToLowerInvariant();
            if ((n.EndsWith("_dx9") && !n.EndsWith("_hdr_dx9")) || n == ">=dx90" || n == ">=dx90_20b")
                data.Apply(child.Value);
        }
        foreach (var child in root.Children)
            if (child.Key.EndsWith("_hdr_dx9", StringComparison.OrdinalIgnoreCase))
                data.Apply(child.Value);

        return data;
    }

    void Apply(Block block)
    {
        foreach (var kv in block.Values)
        {
            string key = kv.Key;
            int q = key.LastIndexOf('?');
            if (q >= 0) key = key.Substring(q + 1);
            if (key.StartsWith("$")) Params[key] = kv.Value;
        }
    }

    static Block ParseBlock(List<(string text, bool quoted)> tokens, ref int i)
    {
        var block = new Block();
        while (i < tokens.Count)
        {
            var t = tokens[i++];
            if (!t.quoted && t.text == "}") break;
            if (i >= tokens.Count) break;

            var next = tokens[i];
            if (!next.quoted && next.text == "{")
            {
                i++;
                block.Children.Add(new KeyValuePair<string, Block>(t.text, ParseBlock(tokens, ref i)));
            }
            else if (!next.quoted && next.text == "}")
            {
                // Key without a value; let the loop close the block.
            }
            else
            {
                i++;
                block.Values.Add(new KeyValuePair<string, string>(t.text, next.text));
            }
        }
        return block;
    }

    static List<(string text, bool quoted)> Tokenize(string s)
    {
        var tokens = new List<(string, bool)>();
        int i = 0;
        while (i < s.Length)
        {
            char c = s[i];
            if (char.IsWhiteSpace(c)) { i++; continue; }
            if (c == '/' && i + 1 < s.Length && s[i + 1] == '/')
            {
                while (i < s.Length && s[i] != '\n') i++;
                continue;
            }
            if (c == '{' || c == '}')
            {
                tokens.Add((c.ToString(), false));
                i++;
                continue;
            }
            if (c == '"')
            {
                int end = s.IndexOf('"', i + 1);
                if (end < 0) end = s.Length;
                tokens.Add((s.Substring(i + 1, end - i - 1), true));
                i = end + 1;
                continue;
            }

            int start = i;
            while (i < s.Length && !char.IsWhiteSpace(s[i]) && s[i] != '"' && s[i] != '{' && s[i] != '}') i++;
            tokens.Add((s.Substring(start, i - start), false));
        }
        return tokens;
    }
}

/// <summary>
/// Turns a VmtData into a URP Lit/Unlit material. Texture loading is left to the
/// caller so the importer can use the .vtf assets and the baker its baked .png files.
/// </summary>
public static class VmtMaterialBuilder
{
    /// <param name="loadTexture">(vmt texture reference, how it is used) → texture or null</param>
    public static Material Build(string name, VmtData vmt, Func<string, VtfTextureKind, Texture2D> loadTexture)
    {
        string shaderName = vmt.Shader.ToLowerInvariant();
        bool unlit = shaderName == "unlitgeneric";
        bool refract = shaderName == "refract";

        var shader = Shader.Find(unlit ? "Universal Render Pipeline/Unlit" : "Universal Render Pipeline/Lit");
        if (shader == null) return null;
        var mat = new Material(shader) { name = name };

        Texture2D baseTex = null;
        if (vmt.Has("$basetexture"))
        {
            baseTex = loadTexture(vmt.Get("$basetexture"), VtfTextureKind.Color);
            if (baseTex != null)
            {
                mat.SetTexture("_BaseMap", baseTex);
                mat.SetTexture("_MainTex", baseTex);
            }
        }

        var tiling = TransformScale(vmt.Get("$basetexturetransform"));
        mat.SetTextureScale("_BaseMap", tiling);
        mat.SetTextureScale("_MainTex", tiling);

        float alpha = vmt.Float("$alpha", 1f);
        Color tint = refract
            ? vmt.Color("$refracttint", UnityEngine.Color.white)
            : vmt.Color("$color", UnityEngine.Color.white) * vmt.Color("$color2", UnityEngine.Color.white);
        tint.a = refract ? 0.35f : alpha;
        mat.SetColor("_BaseColor", tint);
        mat.SetColor("_Color", tint);

        string bumpRef = vmt.Get(refract ? "$normalmap" : "$bumpmap") ?? vmt.Get("$normalmap");
        if (!unlit && bumpRef != null)
        {
            var kind = vmt.IsTrue("$ssbump") ? VtfTextureKind.SSBump : VtfTextureKind.NormalMap;
            var bump = loadTexture(bumpRef, kind);
            if (bump != null)
            {
                mat.SetTexture("_BumpMap", bump);
                mat.SetFloat("_BumpScale", 1f);
                mat.EnableKeyword("_NORMALMAP");
            }
        }

        bool translucent = refract || vmt.IsTrue("$translucent") || alpha < 1f;
        bool alphaTest = !translucent && vmt.IsTrue("$alphatest");
        SetupSurface(mat, translucent, alphaTest, vmt.Float("$alphatestreference", 0.5f));

        if (!unlit)
        {
            float smoothness = 0.2f;
            if (refract)
            {
                smoothness = 0.95f;
            }
            else if (vmt.Has("$envmap"))
            {
                var envTint = vmt.Color("$envmaptint", UnityEngine.Color.white);
                smoothness = Mathf.Lerp(0.45f, 0.85f, Mathf.Clamp01((envTint.r + envTint.g + envTint.b) / 3f));
            }
            mat.SetFloat("_Metallic", 0f);
            mat.SetFloat("_Smoothness", smoothness);

            if (vmt.IsTrue("$basealphaenvmapmask") && !translucent && !alphaTest && baseTex != null)
            {
                mat.SetFloat("_SmoothnessTextureChannel", 1f);
                mat.EnableKeyword("_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A");
            }

            if (vmt.IsTrue("$selfillum") && baseTex != null)
            {
                mat.SetTexture("_EmissionMap", baseTex);
                mat.SetColor("_EmissionColor", UnityEngine.Color.white);
                mat.EnableKeyword("_EMISSION");
                mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive;
            }
        }

        if (vmt.IsTrue("$nocull"))
            mat.SetFloat("_Cull", (float)CullMode.Off);

        return mat;
    }

    static void SetupSurface(Material mat, bool transparent, bool alphaClip, float cutoff)
    {
        mat.SetFloat("_Surface", transparent ? 1f : 0f);
        mat.SetFloat("_Blend", 0f);
        mat.SetFloat("_AlphaClip", alphaClip ? 1f : 0f);
        mat.SetFloat("_Cutoff", cutoff);

        if (transparent)
        {
            mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            mat.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            mat.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
            mat.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
            mat.SetFloat("_ZWrite", 0f);
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.SetShaderPassEnabled("DepthOnly", false);
            mat.renderQueue = (int)RenderQueue.Transparent;
        }
        else
        {
            mat.SetFloat("_SrcBlend", (float)BlendMode.One);
            mat.SetFloat("_DstBlend", (float)BlendMode.Zero);
            mat.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
            mat.SetFloat("_DstBlendAlpha", (float)BlendMode.Zero);
            mat.SetFloat("_ZWrite", 1f);
            mat.SetOverrideTag("RenderType", alphaClip ? "TransparentCutout" : "Opaque");
            if (alphaClip) mat.EnableKeyword("_ALPHATEST_ON");
            mat.renderQueue = (int)(alphaClip ? RenderQueue.AlphaTest : RenderQueue.Geometry);
        }
    }

    /// <summary>Reads the "scale x y" part of a $basetexturetransform.</summary>
    static Vector2 TransformScale(string transform)
    {
        if (string.IsNullOrEmpty(transform)) return Vector2.one;
        var parts = transform.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < parts.Length - 2; i++)
        {
            if (!parts[i].Equals("scale", StringComparison.OrdinalIgnoreCase)) continue;
            if (float.TryParse(parts[i + 1], NumberStyles.Float, CultureInfo.InvariantCulture, out var x) &&
                float.TryParse(parts[i + 2], NumberStyles.Float, CultureInfo.InvariantCulture, out var y))
                return new Vector2(x, y);
        }
        return Vector2.one;
    }

    /// <summary>
    /// Resolves a VMT texture reference (e.g. "Concrete/cinderblock01") to a .vtf asset path,
    /// relative to the "materials" folder above the .vmt and case-insensitive like Source.
    /// </summary>
    public static string ResolveTexturePath(string vmtAssetPath, string texRef)
    {
        string root = FindMaterialsRoot(Path.GetDirectoryName(vmtAssetPath));
        if (string.IsNullOrEmpty(root) || string.IsNullOrEmpty(texRef)) return null;

        string rel = texRef.Replace('\\', '/').TrimStart('/');
        if (rel.EndsWith(".vtf", StringComparison.OrdinalIgnoreCase))
            rel = rel.Substring(0, rel.Length - 4);
        return FindCaseInsensitive(root, rel + ".vtf");
    }

    /// <summary>Walks up from a folder to the nearest "materials" folder (or returns the folder itself).</summary>
    public static string FindMaterialsRoot(string folder)
    {
        folder = folder?.Replace('\\', '/');
        string dir = folder;
        while (!string.IsNullOrEmpty(dir))
        {
            if (string.Equals(Path.GetFileName(dir), "materials", StringComparison.OrdinalIgnoreCase))
                return dir;
            dir = Path.GetDirectoryName(dir)?.Replace('\\', '/');
        }
        return folder;
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

            foreach (var entry in isLast ? Directory.GetFiles(current) : Directory.GetDirectories(current))
            {
                if (string.Equals(Path.GetFileName(entry), parts[i], StringComparison.OrdinalIgnoreCase))
                {
                    match = entry;
                    break;
                }
            }

            if (match == null) return null;
            current = match;
        }

        return current.Replace('\\', '/');
    }
}
