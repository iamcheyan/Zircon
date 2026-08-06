using System;
using System.IO;
using Godot;
using System.Drawing;
using Rectangle = System.Drawing.Rectangle;

namespace ZirconClient.Formats;

// .Zl 图库读取器（移植自 RenderingCore/Library/MirLibrary.cs + ZlImageMetadata.cs）
// 支持旧格式（version 0/1），不支持 ZL2 压缩容器（仅 7 个文件用，后续补）
public sealed class ZlLibrary : IDisposable
{
    public int Version;
    public ZlImage[] Images;
    private readonly FileStream _stream;
    private readonly BinaryReader _reader;
    public string FileName { get; }

    public ZlLibrary(string fileName)
    {
        FileName = fileName;
        _stream = File.OpenRead(fileName);
        _reader = new BinaryReader(_stream);
        ReadLibrary();
    }

    private void ReadLibrary()
    {
        _reader.BaseStream.Seek(0, SeekOrigin.Begin);

        // 检查 ZL2 签名
        if (_reader.BaseStream.Length >= 3)
        {
            long pos = _reader.BaseStream.Position;
            byte[] sig = _reader.ReadBytes(3);
            if (sig[0] == 'Z' && sig[1] == 'L' && sig[2] == '2')
            {
                GD.PrintErr($"[ZlReader] ZL2 压缩容器格式暂不支持: {Path.GetFileName(FileName)}");
                Images = Array.Empty<ZlImage>();
                return;
            }
            _reader.BaseStream.Seek(pos, SeekOrigin.Begin);
        }

        // 旧格式: 先读 Int32(元数据块大小) → 读该块到内存 → 在内存里解析
        int metaSize = _reader.ReadInt32();
        byte[] metaBlock = _reader.ReadBytes(metaSize);
        using (var mstream = new MemoryStream(metaBlock))
        using (var metaReader = new BinaryReader(mstream))
        {
            int value = metaReader.ReadInt32();
            int count = value & 0x1FFFFFF;
            Version = (value >> 25) & 0x7F;
            if (Version == 0) count = value;

            Images = new ZlImage[count];
            for (int i = 0; i < Images.Length; i++)
            {
                if (!metaReader.ReadBoolean()) continue;
                Images[i] = ZlImage.Read(metaReader, Version);
            }
        }
    }

    // 读取第 index 帧的像素数据，返回 BGRA32 byte[]
    public byte[] GetImageData(int index)
    {
        if (index < 0 || index >= Images.Length) return null;
        var img = Images[index];
        if (img == null || img.Position == 0 || img.Width <= 0 || img.Height <= 0) return null;

        int dataSize = img.GetDataSize();
        byte[] buffer;
        lock (_reader)
        {
            _reader.BaseStream.Seek(img.Position, SeekOrigin.Begin);
            buffer = _reader.ReadBytes(dataSize);
        }

        return ZlImageCodecUtil.DecodeToBgra(buffer, img.ImageCodec, img.Width, img.Height);
    }

    // 读取第 index 帧，返回 Godot ImageTexture
    public ImageTexture GetImageTexture(int index)
    {
        byte[] bgra = GetImageData(index);
        if (bgra == null) return null;

        var img = Images[index];
        // BGRA → RGBA (Godot 用 RGBA8)
        byte[] rgba = new byte[bgra.Length];
        for (int i = 0; i < bgra.Length; i += 4)
        {
            rgba[i] = bgra[i + 2];     // R <- B
            rgba[i + 1] = bgra[i + 1]; // G
            rgba[i + 2] = bgra[i];     // B <- R
            rgba[i + 3] = bgra[i + 3]; // A
        }

        var godotImage = Image.CreateFromData(img.Width, img.Height, false, Image.Format.Rgba8, rgba);
        return ImageTexture.CreateFromImage(godotImage);
    }

    public void Dispose()
    {
        _reader?.Dispose();
        _stream?.Dispose();
    }
}

// 单帧元数据
public sealed class ZlImage
{
    public int Version;
    public int Position;
    public short Width;
    public short Height;
    public short OffSetX;
    public short OffSetY;
    public byte ShadowType;
    public short ShadowWidth, ShadowHeight;
    public short ShadowOffSetX, ShadowOffSetY;
    public short OverlayWidth, OverlayHeight;
    public ZlImageCodec ImageCodec;
    public int StoredImageDataSize;

    public static ZlImage Read(BinaryReader reader, int version)
    {
        var img = new ZlImage
        {
            Version = version,
            Position = reader.ReadInt32(),
            Width = reader.ReadInt16(),
            Height = reader.ReadInt16(),
            OffSetX = reader.ReadInt16(),
            OffSetY = reader.ReadInt16(),
            ShadowType = reader.ReadByte(),
            ShadowWidth = reader.ReadInt16(),
            ShadowHeight = reader.ReadInt16(),
            ShadowOffSetX = reader.ReadInt16(),
            ShadowOffSetY = reader.ReadInt16(),
            OverlayWidth = reader.ReadInt16(),
            OverlayHeight = reader.ReadInt16(),
        };

        img.ImageCodec = version == 0 ? ZlImageCodec.Dxt1 : ZlImageCodec.Dxt5;

        if (version >= 2)
        {
            reader.ReadInt32(); // AtlasPage
            reader.ReadInt16(); reader.ReadInt16(); reader.ReadInt16(); reader.ReadInt16(); // SourceRectangle
            reader.ReadInt16(); reader.ReadInt16(); reader.ReadInt16(); reader.ReadInt16(); // VisibleBounds
            img.ImageCodec = (ZlImageCodec)reader.ReadByte();
            reader.ReadByte(); // ShadowCodec
            reader.ReadByte(); // OverlayCodec
            reader.ReadByte(); reader.ReadByte(); reader.ReadByte(); // RuntimePreferences
            img.StoredImageDataSize = reader.ReadInt32();
            reader.ReadInt32(); reader.ReadInt32(); // ImageBc7DataSize, ImageFallbackDataSize
            reader.ReadInt32(); reader.ReadInt32(); reader.ReadInt32(); // Shadow sizes
            reader.ReadInt32(); reader.ReadInt32(); reader.ReadInt32(); // Overlay sizes
        }
        return img;
    }

    public int GetDataSize()
    {
        if (Version >= 2 && StoredImageDataSize > 0) return StoredImageDataSize;
        return ImageCodec switch
        {
            ZlImageCodec.Bgra32 => Width * Height * 4,
            ZlImageCodec.Png => 0, // PNG 大小不固定，需读到文件末尾或用 StoredImageDataSize
            ZlImageCodec.Dxt1 => Math.Max(1, Width / 4) * Math.Max(1, Height / 4) * 8,
            ZlImageCodec.Dxt5 => Math.Max(1, Width / 4) * Math.Max(1, Height / 4) * 16,
            ZlImageCodec.Bc7 => Math.Max(1, Width / 4) * Math.Max(1, Height / 4) * 16,
            _ => Width * Height * 4,
        };
    }
}

public enum ZlImageCodec : byte
{
    Dxt1, Dxt5, Bgra32, Bc7, Png,
}

// 编解码工具
public static class ZlImageCodecUtil
{
    public static byte[] DecodeToBgra(byte[] buffer, ZlImageCodec codec, short width, short height)
    {
        if (buffer == null || buffer.Length == 0) return Array.Empty<byte>();

        switch (codec)
        {
            case ZlImageCodec.Bgra32:
                return buffer; // 已经是 BGRA32

            case ZlImageCodec.Png:
                // 用 Godot Image 加载 PNG
                var img = Image.CreateFromData(width, height, false, Image.Format.Rgba8, new byte[width * height * 4]);
                // PNG: 用 Godot 的 Image.Load
                using (var pngStream = new MemoryStream(buffer))
                {
                    var godotImg = new Image();
                    godotImg.LoadPngFromBuffer(buffer);
                    var pngData = godotImg.GetData();
                    // PNG 加载后是 RGBA，转 BGRA
                    // 简单起见直接转 RGBA 给 GodotImage（调用方处理）
                    return ConvertRgbaToBgra(godotImg, width, height);
                }

            case ZlImageCodec.Dxt1:
            case ZlImageCodec.Dxt5:
            case ZlImageCodec.Bc7:
                return DecodeBCn(buffer, codec, width, height);

            default:
                return buffer;
        }
    }

    private static byte[] ConvertRgbaToBgra(Image godotImg, int width, int height)
    {
        // Godot Image 数据是 RGBA8，转成 BGRA
        byte[] rgba = godotImg.GetData();
        // 注意: GetData() 返回的是压缩后的格式，可能需要 GetImage()
        // 这里简化处理：直接用像素数据
        byte[] bgra = new byte[width * height * 4];
        // 如果数据是 RGBA
        for (int i = 0; i < bgra.Length && i + 3 < rgba.Length; i += 4)
        {
            bgra[i] = rgba[i + 2];     // B <- R (如果是RGBA)
            bgra[i + 1] = rgba[i + 1]; // G
            bgra[i + 2] = rgba[i];     // R <- B
            bgra[i + 3] = rgba[i + 3]; // A
        }
        return bgra;
    }

    private static byte[] DecodeBCn(byte[] buffer, ZlImageCodec codec, short width, short height)
    {
        // 用 BCnEncoder.NET 解码 DXT1/5/BC7
        // 暂时返回空，等加 NuGet 包
        // TODO: 加 BCnEncoder.NET 包
        return BcnDecoder.Decode(buffer, codec, width, height);
        return Array.Empty<byte>();
    }
}
