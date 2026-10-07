using System;
using UnityEngine;

public enum VtfTextureKind
{
    Auto,
    Color,
    NormalMap,
    SSBump,
}

/// <summary>
/// A decoded VTF: the first frame/face/slice of every mip, largest mip first,
/// rows bottom-to-top like Unity expects.
/// </summary>
public class VtfImage
{
    public int Width;
    public int Height;
    public uint Flags;
    public int Format;
    public Color32[][] Mips;

    public bool ClampU => (Flags & VtfDecoder.FlagClampS) != 0;
    public bool ClampV => (Flags & VtfDecoder.FlagClampT) != 0;
    public bool PointSample => (Flags & VtfDecoder.FlagPointSample) != 0;
}

/// <summary>
/// Reads Valve Texture Format files (v7.0 - v7.5) into plain RGBA32 pixels.
/// Shared by VtfImporter and VmtBaker.
/// </summary>
public static class VtfDecoder
{
    public const uint FlagPointSample = 0x1;
    public const uint FlagClampS = 0x4;
    public const uint FlagClampT = 0x8;
    public const uint FlagNormal = 0x80;
    public const uint FlagEnvMap = 0x4000;
    public const uint FlagSSBump = 0x1000000;

    const int RGBA8888 = 0, ABGR8888 = 1, RGB888 = 2, BGR888 = 3, RGB565 = 4, I8 = 5, IA88 = 6,
        A8 = 8, BGRA8888 = 12, DXT1 = 13, DXT3 = 14, DXT5 = 15, BGRX8888 = 16, BGR565 = 17,
        DXT1_ONEBITALPHA = 20, UV88 = 22;

    // Source's self-shadowing bump basis (tangent space).
    static readonly Vector3 SSBumpBasis0 = new Vector3(0.81649661f, 0f, 0.57735026f);
    static readonly Vector3 SSBumpBasis1 = new Vector3(-0.40824834f, 0.70710677f, 0.57735026f);
    static readonly Vector3 SSBumpBasis2 = new Vector3(-0.40824822f, -0.70710683f, 0.57735026f);

    public static VtfImage Decode(byte[] b)
    {
        if (b.Length < 64 || b[0] != 'V' || b[1] != 'T' || b[2] != 'F' || b[3] != 0)
            throw new Exception("not a VTF file (bad signature)");

        uint minor = U32(b, 8);
        uint headerSize = U32(b, 12);
        int width = U16(b, 16);
        int height = U16(b, 18);
        uint flags = U32(b, 20);
        int frames = Math.Max(1, U16(b, 24));
        int firstFrame = U16(b, 26);
        int format = I32(b, 52);
        int mipCount = Math.Max(1, (int)b[56]);
        int lowResFormat = I32(b, 57);
        int lowResWidth = b[61];
        int lowResHeight = b[62];
        int depth = minor >= 2 ? Math.Max(1, U16(b, 63)) : 1;

        if (width <= 0 || height <= 0)
            throw new Exception($"invalid dimensions ({width}x{height})");

        int faces = 1;
        if ((flags & FlagEnvMap) != 0)
            faces = minor < 5 && firstFrame != 0xFFFF ? 7 : 6;

        long offset = -1;
        if (minor >= 3)
        {
            uint resourceCount = U32(b, 68);
            for (int i = 0; i < resourceCount; i++)
            {
                int entry = 80 + i * 8;
                if (entry + 8 > b.Length) break;
                if (b[entry] == 0x30 && b[entry + 1] == 0 && b[entry + 2] == 0)
                {
                    offset = U32(b, entry + 4);
                    break;
                }
            }
            if (offset < 0) throw new Exception("no high-res image resource");
        }
        else
        {
            offset = headerSize;
            if (lowResFormat >= 0 && lowResWidth > 0 && lowResHeight > 0)
                offset += Math.Max(0, ImageSize(lowResFormat, lowResWidth, lowResHeight));
        }

        // Mips are stored smallest first; each mip holds frames * faces * slices images.
        var mips = new Color32[mipCount][];
        long pos = offset;
        for (int m = mipCount - 1; m >= 0; m--)
        {
            int w = Math.Max(1, width >> m);
            int h = Math.Max(1, height >> m);
            int d = Math.Max(1, depth >> m);
            int size = ImageSize(format, w, h);
            if (size < 0 || !CanDecode(format))
                throw new Exception($"unsupported VTF image format {format}");
            if (pos + size > b.Length)
                throw new Exception("file is truncated");

            mips[m] = DecodeImage(b, (int)pos, format, w, h);
            pos += (long)size * frames * faces * d;
        }

        return new VtfImage { Width = width, Height = height, Flags = flags, Format = format, Mips = mips };
    }

    public static VtfTextureKind ResolveKind(VtfTextureKind requested, VtfImage img, string fileName)
    {
        if (requested != VtfTextureKind.Auto) return requested;
        if ((img.Flags & FlagSSBump) != 0 || fileName.ToLowerInvariant().Contains("ssbump")) return VtfTextureKind.SSBump;
        if ((img.Flags & FlagNormal) != 0) return VtfTextureKind.NormalMap;
        return VtfTextureKind.Color;
    }

    /// <summary>
    /// Converts pixels to what Unity expects, in place. Source normal maps are
    /// DirectX-style (green points down); SSBumps become regular tangent-space normals.
    /// </summary>
    public static void ConvertForUnity(Color32[] px, VtfTextureKind kind, bool flipGreen)
    {
        if (kind == VtfTextureKind.NormalMap)
        {
            for (int i = 0; i < px.Length; i++)
            {
                var c = px[i];
                px[i] = new Color32(c.r, flipGreen ? (byte)(255 - c.g) : c.g, c.b, 255);
            }
        }
        else if (kind == VtfTextureKind.SSBump)
        {
            for (int i = 0; i < px.Length; i++)
            {
                var c = px[i];
                Vector3 n = SSBumpBasis0 * (c.r / 255f) + SSBumpBasis1 * (c.g / 255f) + SSBumpBasis2 * (c.b / 255f);
                n = n.sqrMagnitude < 1e-8f ? Vector3.forward : n.normalized;
                if (flipGreen) n.y = -n.y;
                px[i] = new Color32(ToByte(n.x * 0.5f + 0.5f), ToByte(n.y * 0.5f + 0.5f), ToByte(n.z * 0.5f + 0.5f), 255);
            }
        }
    }

    public static bool HasAlpha(Color32[] px)
    {
        foreach (var c in px)
            if (c.a < 255) return true;
        return false;
    }

    static bool CanDecode(int format)
    {
        switch (format)
        {
            case RGBA8888: case ABGR8888: case RGB888: case BGR888: case RGB565: case I8: case IA88:
            case A8: case BGRA8888: case DXT1: case DXT3: case DXT5: case BGRX8888: case BGR565:
            case DXT1_ONEBITALPHA: case UV88:
                return true;
            default:
                return false;
        }
    }

    static int ImageSize(int format, int w, int h)
    {
        int blocks = Math.Max(1, (w + 3) / 4) * Math.Max(1, (h + 3) / 4);
        switch (format)
        {
            case DXT1: case DXT1_ONEBITALPHA: return blocks * 8;
            case DXT3: case DXT5: return blocks * 16;
        }
        int bpp = BytesPerPixel(format);
        return bpp < 0 ? -1 : w * h * bpp;
    }

    static int BytesPerPixel(int format)
    {
        switch (format)
        {
            case 0: case 1: case 11: case 12: case 16: case 23: case 26: return 4;
            case 2: case 3: case 9: case 10: return 3;
            case 4: case 6: case 17: case 18: case 19: case 21: case 22: return 2;
            case 5: case 7: case 8: return 1;
            case 24: case 25: return 8;
            default: return -1;
        }
    }

    static Color32[] DecodeImage(byte[] b, int o, int format, int w, int h)
    {
        var px = new Color32[w * h];
        switch (format)
        {
            case DXT1:
            case DXT1_ONEBITALPHA:
                DecodeBlocks(b, o, w, h, px, 1);
                return px;
            case DXT3:
                DecodeBlocks(b, o, w, h, px, 3);
                return px;
            case DXT5:
                DecodeBlocks(b, o, w, h, px, 5);
                return px;
        }

        int bpp = BytesPerPixel(format);
        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
            px[(h - 1 - y) * w + x] = ReadPixel(b, o + (y * w + x) * bpp, format);
        return px;
    }

    static Color32 ReadPixel(byte[] b, int i, int format)
    {
        switch (format)
        {
            case RGBA8888: return new Color32(b[i], b[i + 1], b[i + 2], b[i + 3]);
            case ABGR8888: return new Color32(b[i + 3], b[i + 2], b[i + 1], b[i]);
            case RGB888: return new Color32(b[i], b[i + 1], b[i + 2], 255);
            case BGR888: return new Color32(b[i + 2], b[i + 1], b[i], 255);
            case BGRA8888: return new Color32(b[i + 2], b[i + 1], b[i], b[i + 3]);
            case BGRX8888: return new Color32(b[i + 2], b[i + 1], b[i], 255);
            case I8: return new Color32(b[i], b[i], b[i], 255);
            case IA88: return new Color32(b[i], b[i], b[i], b[i + 1]);
            case A8: return new Color32(255, 255, 255, b[i]);
            case UV88: return new Color32(b[i], b[i + 1], 0, 255);
            case RGB565:
            {
                int v = U16(b, i);
                return new Color32(Expand5(v & 31), Expand6((v >> 5) & 63), Expand5((v >> 11) & 31), 255);
            }
            case BGR565:
                return Expand565(U16(b, i));
            default:
                return new Color32(255, 0, 255, 255);
        }
    }

    /// <param name="kind">1 = DXT1, 3 = DXT3, 5 = DXT5</param>
    static void DecodeBlocks(byte[] b, int o, int w, int h, Color32[] px, int kind)
    {
        int blocksW = Math.Max(1, (w + 3) / 4);
        int blocksH = Math.Max(1, (h + 3) / 4);
        int blockSize = kind == 1 ? 8 : 16;
        var colors = new Color32[4];
        var alphas = new byte[16];
        var palette = new byte[8];

        for (int by = 0; by < blocksH; by++)
        for (int bx = 0; bx < blocksW; bx++)
        {
            int p = o + (by * blocksW + bx) * blockSize;
            int colorBlock = p;

            if (kind == 3)
            {
                for (int i = 0; i < 16; i++)
                    alphas[i] = (byte)(((b[p + i / 2] >> ((i & 1) * 4)) & 0xF) * 17);
                colorBlock = p + 8;
            }
            else if (kind == 5)
            {
                byte a0 = b[p], a1 = b[p + 1];
                palette[0] = a0;
                palette[1] = a1;
                if (a0 > a1)
                {
                    for (int i = 1; i <= 6; i++) palette[i + 1] = (byte)(((7 - i) * a0 + i * a1) / 7);
                }
                else
                {
                    for (int i = 1; i <= 4; i++) palette[i + 1] = (byte)(((5 - i) * a0 + i * a1) / 5);
                    palette[6] = 0;
                    palette[7] = 255;
                }

                ulong bits = 0;
                for (int k = 0; k < 6; k++) bits |= (ulong)b[p + 2 + k] << (8 * k);
                for (int i = 0; i < 16; i++) alphas[i] = palette[(bits >> (3 * i)) & 7];
                colorBlock = p + 8;
            }

            int c0 = U16(b, colorBlock), c1 = U16(b, colorBlock + 2);
            colors[0] = Expand565(c0);
            colors[1] = Expand565(c1);
            if (c0 > c1 || kind != 1)
            {
                colors[2] = Mix(colors[0], colors[1], 2, 1, 3);
                colors[3] = Mix(colors[0], colors[1], 1, 2, 3);
            }
            else
            {
                colors[2] = Mix(colors[0], colors[1], 1, 1, 2);
                colors[3] = new Color32(0, 0, 0, 0);
            }

            uint indices = U32(b, colorBlock + 4);
            for (int i = 0; i < 16; i++)
            {
                int x = bx * 4 + (i & 3), y = by * 4 + (i >> 2);
                if (x >= w || y >= h) continue;
                var c = colors[(indices >> (2 * i)) & 3];
                if (kind != 1) c.a = alphas[i];
                px[(h - 1 - y) * w + x] = c;
            }
        }
    }

    static Color32 Mix(Color32 a, Color32 b, int wa, int wb, int div) =>
        new Color32((byte)((a.r * wa + b.r * wb) / div), (byte)((a.g * wa + b.g * wb) / div),
            (byte)((a.b * wa + b.b * wb) / div), 255);

    static Color32 Expand565(int v) =>
        new Color32(Expand5((v >> 11) & 31), Expand6((v >> 5) & 63), Expand5(v & 31), 255);

    static byte Expand5(int v) => (byte)((v << 3) | (v >> 2));
    static byte Expand6(int v) => (byte)((v << 2) | (v >> 4));
    static byte ToByte(float v) => (byte)Mathf.Clamp(Mathf.RoundToInt(v * 255f), 0, 255);

    static int U16(byte[] b, int i) => b[i] | (b[i + 1] << 8);
    static uint U32(byte[] b, int i) => (uint)(b[i] | (b[i + 1] << 8) | (b[i + 2] << 16) | (b[i + 3] << 24));
    static int I32(byte[] b, int i) => (int)U32(b, i);
}
