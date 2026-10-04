using System;
using System.IO;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;

/// <summary>
/// Imports Valve Texture Format (.vtf) files directly into Unity Texture2D assets,
/// keeping the VTF's own mip chain. Normal maps are converted to Unity's convention
/// and SSBumps (Source's self-shadowing bump maps) are converted to normal maps.
/// </summary>
[ScriptedImporter(2, "vtf")]
public class VtfImporter : ScriptedImporter
{
    [Tooltip("Auto: SSBump if the file name contains 'ssbump', normal map if the VTF has the normal flag, colour otherwise.")]
    public VtfTextureKind kind = VtfTextureKind.Auto;

    [Tooltip("Source normal maps are DirectX-style (green points down); Unity wants green up.")]
    public bool flipGreen = true;

    [Tooltip("Compress to DXT1/DXT5 (colour) or BC5 (normal maps).")]
    public bool compress = true;

    [Range(0, 16)]
    public int anisoLevel = 4;

    public override void OnImportAsset(AssetImportContext ctx)
    {
        VtfImage img;
        try
        {
            img = VtfDecoder.Decode(File.ReadAllBytes(ctx.assetPath));
        }
        catch (Exception e)
        {
            ctx.LogImportError($"'{ctx.assetPath}': {e.Message}");
            return;
        }

        string fileName = Path.GetFileNameWithoutExtension(ctx.assetPath);
        var resolvedKind = VtfDecoder.ResolveKind(kind, img, fileName);
        bool isNormal = resolvedKind != VtfTextureKind.Color;

        int fullChain = 1 + (int)Math.Floor(Math.Log(Math.Max(img.Width, img.Height), 2));
        bool hasOwnMips = img.Mips.Length >= fullChain;
        int mipsToUse = hasOwnMips ? fullChain : 1;
        for (int m = 0; m < mipsToUse; m++)
            VtfDecoder.ConvertForUnity(img.Mips[m], resolvedKind, flipGreen);

        var tex = new Texture2D(img.Width, img.Height, TextureFormat.RGBA32, fullChain, isNormal)
        {
            name = fileName,
            wrapModeU = img.ClampU ? TextureWrapMode.Clamp : TextureWrapMode.Repeat,
            wrapModeV = img.ClampV ? TextureWrapMode.Clamp : TextureWrapMode.Repeat,
            filterMode = img.PointSample ? FilterMode.Point : FilterMode.Trilinear,
            anisoLevel = anisoLevel,
        };
        for (int m = 0; m < mipsToUse; m++)
            tex.SetPixels32(img.Mips[m], m);
        tex.Apply(!hasOwnMips, false);

        if (compress && img.Width % 4 == 0 && img.Height % 4 == 0)
        {
            var format = isNormal ? TextureFormat.BC5
                : VtfDecoder.HasAlpha(img.Mips[0]) ? TextureFormat.DXT5 : TextureFormat.DXT1;
            EditorUtility.CompressTexture(tex, format, TextureCompressionQuality.Normal);
        }

        ctx.AddObjectToAsset("texture", tex);
        ctx.SetMainObject(tex);
    }
}
