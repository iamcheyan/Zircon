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
        // Zl 的 DXT 数据按 floor(w/4)×floor(h/4) 块存储, BCnEncoder 按 ceil 解。
        // 数据不足时尾部补零, 边缘块(非 4 倍数像素)填充为垃圾但可解码。
        int bytesPerBlock = format == CompressionFormat.Bc1WithAlpha ? 8 : 16;
        int ceilBytes = ((width + 3) / 4) * ((height + 3) / 4) * bytesPerBlock;
        if (buffer.Length < ceilBytes)
        {
            var padded = new byte[ceilBytes];
            Array.Copy(buffer, padded, buffer.Length);
            buffer = padded;
        }

        ColorRgba32[] pixels;
        try
        {
            pixels = decoder.DecodeRaw(buffer, width, height, format);
        }
        catch (Exception ex)
        {
            int blocks = (width + 3) / 4 * ((height + 3) / 4);
            GD.PrintErr($"[BcnDecoder] 解码失败: codec={codec} w={width} h={height} format={format} len={buffer.Length} expectBlocks={blocks} expectBytes={ceilBytes} err={ex.GetType().Name}: {ex.Message}");
            throw;
        }

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
