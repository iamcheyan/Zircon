using System;
using BCnEncoder.Decoder;
using BCnEncoder.Shared;
using Godot;

namespace ZirconClient.Formats;

// DXT1/5/BC7 纹理解码，用 BCnEncoder.NET
public static class BcnDecoder
{
    public static byte[] Decode(byte[] buffer, ZlImageCodec codec, int width, int height)
    {
        if (buffer == null || buffer.Length == 0 || width <= 0 || height <= 0)
            return Array.Empty<byte>();

        CompressionFormat format = codec switch
        {
            ZlImageCodec.Dxt1 => CompressionFormat.Bc1WithAlpha,
            ZlImageCodec.Dxt5 => CompressionFormat.Bc3,
            ZlImageCodec.Bc7 => CompressionFormat.Bc7,
            _ => CompressionFormat.Bc7,
        };

        var decoder = new BcDecoder();
        ColorRgba32[] pixels = decoder.DecodeRaw(buffer, width, height, format);

        // 转 BGRA32 (B,G,R,A 顺序)
        byte[] result = new byte[width * height * 4];
        for (int i = 0; i < pixels.Length && i * 4 + 3 < result.Length; i++)
        {
            int o = i * 4;
            result[o] = pixels[i].b;
            result[o + 1] = pixels[i].g;
            result[o + 2] = pixels[i].r;
            result[o + 3] = pixels[i].a;
        }
        return result;
    }
}
