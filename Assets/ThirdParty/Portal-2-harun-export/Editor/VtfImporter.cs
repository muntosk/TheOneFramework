using System;
using System.IO;
using UnityEditor.AssetImporters;
using UnityEngine;

/// <summary>
/// Imports Valve Texture Format (.vtf) files directly into Unity Texture2D assets.
/// Only reads the final (full-resolution) mip of the main frame/face, which sits
/// at the end of the file for the common case (no cubemaps, single frame).
/// Supports the formats actually found in the Portal 2 export: DXT1, DXT5, RGB888.
/// </summary>
[ScriptedImporter(1, "vtf")]
public class VtfImporter : ScriptedImporter
{
    enum VtfImageFormat
    {
        RGBA8888 = 0,
        ABGR8888 = 1,
        RGB888 = 2,
        BGR888 = 3,
        RGB565 = 4,
        I8 = 5,
        IA88 = 6,
        P8 = 7,
        A8 = 8,
        DXT1 = 13,
        DXT3 = 14,
        DXT5 = 15,
        BGRA8888 = 12,
        DXT1_ONEBITALPHA = 20,
    }

    public override void OnImportAsset(AssetImportContext ctx)
    {
        byte[] bytes = File.ReadAllBytes(ctx.assetPath);
        string fileName = Path.GetFileNameWithoutExtension(ctx.assetPath);

        if (bytes.Length < 64 || bytes[0] != (byte)'V' || bytes[1] != (byte)'T' || bytes[2] != (byte)'F')
        {
            ctx.LogImportError($"'{ctx.assetPath}' is not a valid VTF file (bad signature).");
            return;
        }

        int width = BitConverter.ToUInt16(bytes, 16);
        int height = BitConverter.ToUInt16(bytes, 18);
        int highResFormat = BitConverter.ToInt32(bytes, 52);

        if (width <= 0 || height <= 0)
        {
            ctx.LogImportError($"'{ctx.assetPath}' has invalid dimensions ({width}x{height}).");
            return;
        }

        TextureFormat texFormat;
        int dataSize;

        switch ((VtfImageFormat)highResFormat)
        {
            case VtfImageFormat.DXT1:
            case VtfImageFormat.DXT1_ONEBITALPHA:
                texFormat = TextureFormat.DXT1;
                dataSize = Mathf.Max(1, (width + 3) / 4) * Mathf.Max(1, (height + 3) / 4) * 8;
                break;
            case VtfImageFormat.DXT5:
                texFormat = TextureFormat.DXT5;
                dataSize = Mathf.Max(1, (width + 3) / 4) * Mathf.Max(1, (height + 3) / 4) * 16;
                break;
            case VtfImageFormat.RGB888:
                texFormat = TextureFormat.RGB24;
                dataSize = width * height * 3;
                break;
            case VtfImageFormat.RGBA8888:
                texFormat = TextureFormat.RGBA32;
                dataSize = width * height * 4;
                break;
            case VtfImageFormat.BGRA8888:
                texFormat = TextureFormat.BGRA32;
                dataSize = width * height * 4;
                break;
            default:
                ctx.LogImportError($"'{ctx.assetPath}' uses unsupported VTF image format {highResFormat}. " +
                    "Only DXT1/DXT5/RGB888/RGBA8888/BGRA8888 are handled.");
                return;
        }

        if (dataSize <= 0 || dataSize > bytes.Length)
        {
            ctx.LogImportError($"'{ctx.assetPath}': computed image size ({dataSize}) exceeds file size ({bytes.Length}). " +
                "This VTF may use cubemaps/volume textures, which aren't supported.");
            return;
        }

        // VTF stores mips smallest-to-largest, and (for the common non-cubemap,
        // single-frame case) the full-resolution image is always the last thing in the file.
        byte[] pixelData = new byte[dataSize];
        Buffer.BlockCopy(bytes, bytes.Length - dataSize, pixelData, 0, dataSize);

        var tex = new Texture2D(width, height, texFormat, false, false)
        {
            name = fileName,
            wrapMode = TextureWrapMode.Repeat,
            filterMode = FilterMode.Bilinear
        };
        tex.LoadRawTextureData(pixelData);
        tex.Apply(false, false);

        ctx.AddObjectToAsset("texture", tex);
        ctx.SetMainObject(tex);
    }
}
