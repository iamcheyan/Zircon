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

        // The original client decodes DXT1 itself.  In particular, c0 <= c1
        // selects the fourth transparent colour, while an opaque pure-black
        // colour is nudged to (1,1,1) so it cannot be mistaken for a keyed
        // transparent pixel by the old sprite pipeline.
        if (codec == ZlImageCodec.Dxt1)
            return DecodeDxt1(buffer, width, height);

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

    private static byte[] DecodeDxt1(byte[] buffer, int width, int height)
    {
        int blocksWide = (width + 3) / 4;
        int blocksHigh = (height + 3) / 4;
        byte[] result = new byte[width * height * 4];

        for (int by = 0; by < blocksHigh; by++)
        for (int bx = 0; bx < blocksWide; bx++)
        {
            int blockOffset = (by * blocksWide + bx) * 8;
            if (blockOffset + 8 > buffer.Length) continue;

            ushort c0 = (ushort)(buffer[blockOffset] | (buffer[blockOffset + 1] << 8));
            ushort c1 = (ushort)(buffer[blockOffset + 2] | (buffer[blockOffset + 3] << 8));
            var colours = new byte[16]; // four RGBA colours
            Decode565(c0, colours, 0);
            Decode565(c1, colours, 4);

            if (c0 > c1)
            {
                Interpolate(colours, 0, 4, colours, 8, 2, 1, 3);
                Interpolate(colours, 0, 4, colours, 12, 1, 2, 3);
                colours[15] = 255;
                colours[11] = 255;
            }
            else
            {
                Interpolate(colours, 0, 4, colours, 8, 1, 1, 2);
                colours[12] = colours[13] = colours[14] = 0;
                colours[15] = 0;
            }

            uint indices = BitConverter.ToUInt32(buffer, blockOffset + 4);
            for (int py = 0; py < 4; py++)
            for (int px = 0; px < 4; px++)
            {
                int x = bx * 4 + px;
                int y = by * 4 + py;
                if (x >= width || y >= height) continue;

                int paletteIndex = (int)((indices >> (2 * (py * 4 + px))) & 3);
                int src = paletteIndex * 4;
                int dst = (y * width + x) * 4;
                byte r = colours[src];
                byte g = colours[src + 1];
                byte b = colours[src + 2];
                byte a = colours[src + 3];
                if (a == 255 && r == 0 && g == 0 && b == 0)
                    r = g = b = 1;
                result[dst] = b;
                result[dst + 1] = g;
                result[dst + 2] = r;
                result[dst + 3] = a;
            }
        }
        return result;
    }

    private static void Decode565(ushort value, byte[] output, int offset)
    {
        byte r5 = (byte)((value >> 11) & 0x1F);
        byte g6 = (byte)((value >> 5) & 0x3F);
        byte b5 = (byte)(value & 0x1F);
        output[offset] = (byte)((r5 << 3) | (r5 >> 2));
        output[offset + 1] = (byte)((g6 << 2) | (g6 >> 4));
        output[offset + 2] = (byte)((b5 << 3) | (b5 >> 2));
        output[offset + 3] = 255;
    }

    private static void Interpolate(byte[] colours, int a, int b, byte[] output, int dst,
        int aWeight, int bWeight, int divisor)
    {
        output[dst] = (byte)((colours[a] * aWeight + colours[b] * bWeight) / divisor);
        output[dst + 1] = (byte)((colours[a + 1] * aWeight + colours[b + 1] * bWeight) / divisor);
        output[dst + 2] = (byte)((colours[a + 2] * aWeight + colours[b + 2] * bWeight) / divisor);
        output[dst + 3] = 255;
    }
}
