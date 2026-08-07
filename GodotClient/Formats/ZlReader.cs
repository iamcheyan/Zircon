using System;
using System.Collections.Generic;
using System.IO;
using Godot;
using System.Drawing;
using Rectangle = System.Drawing.Rectangle;

namespace ZirconClient.Formats;

// .Zl 图库读取器（移植自 RenderingCore/Library/MirLibrary.cs + ZlImageMetadata.cs）
// 支持旧格式（version 0/1）与 ZL2 压缩容器（version 2, Deflate 压缩, 按 entry 索引）
public sealed class ZlLibrary : IDisposable
{
    public int Version;
    public ZlImage[] Images;
    private readonly FileStream _stream;
    private readonly BinaryReader _reader;
    public string FileName { get; }

    private readonly Dictionary<int, ImageTexture> _texCache = new();

    private readonly Dictionary<int, Zl2Entry> _zl2Entries = new();
    private bool _isZl2;

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
        if (TryReadCompressedContainer())
            return;

        _reader.BaseStream.Seek(0, SeekOrigin.Begin);

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

    // ZL2 压缩容器: 头部 → index 块(entry: offset/大小/压缩/编解码) → metadata 块(帧定义)
    // 移植自 RenderingCore/Library/MirLibrary.cs TryReadCompressedContainer
    private bool TryReadCompressedContainer()
    {
        if (_reader.BaseStream.Length < 43) return false;
        long pos = _reader.BaseStream.Position;
        byte[] sig = _reader.ReadBytes(3);
        if (sig.Length != 3 || sig[0] != 'Z' || sig[1] != 'L' || sig[2] != '2')
        {
            _reader.BaseStream.Seek(pos, SeekOrigin.Begin);
            return false;
        }

        _reader.ReadInt32(); // Version
        int imageCount = _reader.ReadInt32();
        int atlasCount = _reader.ReadInt32();
        _reader.ReadByte();  // 默认压缩
        int flags = _reader.ReadByte();
        _reader.ReadInt16(); // 保留
        long metadataOffset = _reader.ReadInt64();
        int metadataSize = _reader.ReadInt32();
        long indexOffset = _reader.ReadInt64();
        int indexSize = _reader.ReadInt32();

        _zl2Entries.Clear();
        _reader.BaseStream.Seek(indexOffset, SeekOrigin.Begin);
        using (var indexStream = new MemoryStream(_reader.ReadBytes(indexSize)))
        using (var indexReader = new BinaryReader(indexStream))
        {
            int entryCount = indexReader.ReadInt32();
            for (int i = 0; i < entryCount; i++)
            {
                Zl2Entry entry = Zl2Entry.Read(indexReader);
                _zl2Entries[entry.Id] = entry;
            }
        }

        _reader.BaseStream.Seek(metadataOffset, SeekOrigin.Begin);
        using (var metadataStream = new MemoryStream(_reader.ReadBytes(metadataSize)))
        using (var reader = new BinaryReader(metadataStream))
        {
            Version = reader.ReadInt32();
            int count = reader.ReadInt32();
            reader.ReadInt32(); // AtlasGroupImageCount
            reader.ReadInt32(); // AtlasPageSize
            Images = new ZlImage[count];

            for (int i = 0; i < Images.Length; i++)
            {
                if (!reader.ReadBoolean()) continue;
                Images[i] = ZlImage.Read(reader, Version);
            }
            // atlas 页与 layer mappings 本端用不到(GetUseZlAtlasPages=false), 块内自包含, 无需继续读
            _ = flags; _ = atlasCount; _ = imageCount;
        }

        _isZl2 = true;
        return true;
    }

    // 读取第 index 帧的像素数据，返回 BGRA32 byte[]
    public byte[] GetImageData(int index)
    {
        if (index < 0 || index >= Images.Length) return null;
        var img = Images[index];
        if (img == null || img.Width <= 0 || img.Height <= 0) return null;

        // ZL2: Position 即 entry Id (0 也是合法 id)
        if (_isZl2) return GetZl2ImageData(img);
        if (img.Position == 0) return null;

        int dataSize = img.GetDataSize();
        byte[] buffer;
        lock (_reader)
        {
            _reader.BaseStream.Seek(img.Position, SeekOrigin.Begin);
            buffer = _reader.ReadBytes(dataSize);
        }

        return ZlImageCodecUtil.DecodeToBgra(buffer, img.ImageCodec, img.Width, img.Height);
    }

    // ZL2: entry → 压缩段 → Deflate 解压 → primary 段(offset 0, StoredImageDataSize) 按元数据 codec 解码;
    // primary 失败时回退 Bc7 段
    private byte[] GetZl2ImageData(ZlImage img)
    {
        if (!_zl2Entries.TryGetValue(img.Position, out Zl2Entry entry)) return null;

        byte[] payload;
        lock (_reader)
        {
            _reader.BaseStream.Seek(entry.Offset, SeekOrigin.Begin);
            payload = _reader.ReadBytes(entry.CompressedSize);
        }
        byte[] raw = entry.Compression == ZlContainerCompression.None
            ? payload
            : DecompressDeflate(payload, entry.UncompressedSize);
        if (raw == null || raw.Length == 0) return null;

        // primary 段
        int primarySize = img.GetDataSize();
        if (primarySize <= 0 || primarySize > raw.Length) primarySize = raw.Length;
        byte[] segment = primarySize == raw.Length ? raw : raw[..primarySize];

        byte[] bgra = ZlImageCodecUtil.DecodeToBgra(segment, img.ImageCodec, img.Width, img.Height);
        int expected = img.Width * img.Height * 4;
        if (bgra.Length == expected) return bgra;

        // 回退: Bc7 段 (primary 段之后, Bc7DataSize 字节)
        if (img.Bc7DataSize > 0 && primarySize + img.Bc7DataSize <= raw.Length)
        {
            byte[] bc7Seg = raw.AsSpan(primarySize, img.Bc7DataSize).ToArray();
            bgra = ZlImageCodecUtil.DecodeToBgra(bc7Seg, ZlImageCodec.Bc7, img.Width, img.Height);
            if (bgra.Length == expected) return bgra;
        }

        GD.PrintErr($"[ZlReader] ZL2 帧 {img.Position} 解码尺寸不符: w={img.Width} h={img.Height} codec={img.ImageCodec} primary={primarySize}");
        return bgra;
    }

    private static byte[] DecompressDeflate(byte[] payload, int uncompressedSize)
    {
        try
        {
            using var input = new MemoryStream(payload);
            using var deflate = new System.IO.Compression.DeflateStream(input, System.IO.Compression.CompressionMode.Decompress);
            using var output = new MemoryStream(uncompressedSize);
            deflate.CopyTo(output);
            return output.ToArray();
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[ZlReader] Deflate 解压失败: {ex.Message}");
            return null;
        }
    }

    // 读取第 index 帧，返回 Godot ImageTexture (带缓存, 避免重复解码)
    public ImageTexture GetImageTexture(int index)
    {
        return GetPartTexture(index, ZlImagePart.Image);
    }

    public ImageTexture GetShadowTexture(int index)
    {
        return GetPartTexture(index, ZlImagePart.Shadow);
    }

    public ImageTexture GetOverlayTexture(int index)
    {
        return GetPartTexture(index, ZlImagePart.Overlay);
    }

    private ImageTexture GetPartTexture(int index, ZlImagePart part)
    {
        if (index < 0 || index >= Images.Length) return null;
        if (Images[index] == null) return null;

        if (part == ZlImagePart.Image && _texCache.TryGetValue(index, out var cached)) return cached;

        ZlImage img = Images[index];
        int width = part == ZlImagePart.Shadow ? img.ShadowWidth : part == ZlImagePart.Overlay ? img.OverlayWidth : img.Width;
        int height = part == ZlImagePart.Shadow ? img.ShadowHeight : part == ZlImagePart.Overlay ? img.OverlayHeight : img.Height;
        if (width <= 0 || height <= 0) return null;
        byte[] bgra = GetPartData(index, part);
        if (bgra == null) return null;

        int expected = width * height * 4;
        if (bgra.Length < expected) return null;
        // BGRA → RGBA (Godot 用 RGBA8)
        byte[] rgba = new byte[bgra.Length];
        for (int i = 0; i < bgra.Length; i += 4)
        {
            rgba[i] = bgra[i + 2];     // R <- B
            rgba[i + 1] = bgra[i + 1]; // G
            rgba[i + 2] = bgra[i];     // B <- R
            rgba[i + 3] = bgra[i + 3]; // A
        }

        var godotImage = Image.CreateFromData(width, height, false, Image.Format.Rgba8, rgba);
        var texture = ImageTexture.CreateFromImage(godotImage);
        if (part == ZlImagePart.Image) _texCache[index] = texture;
        return texture;
    }

    private byte[] GetPartData(int index, ZlImagePart part)
    {
        var img = Images[index];
        if (part == ZlImagePart.Image) return GetImageData(index);

        byte[] raw;
        if (_isZl2)
        {
            if (!_zl2Entries.TryGetValue(img.Position, out var entry)) return null;
            lock (_reader)
            {
                _reader.BaseStream.Seek(entry.Offset, SeekOrigin.Begin);
                raw = _reader.ReadBytes(entry.CompressedSize);
            }
            raw = entry.Compression == ZlContainerCompression.None ? raw : DecompressDeflate(raw, entry.UncompressedSize);
        }
        else
        {
            if (img.Position == 0) return null;
            int primary = img.GetImagePayloadSize();
            int shadow = img.GetShadowPayloadSize();
            int overlay = img.GetOverlayPayloadSize();
            int total = primary + shadow + overlay;
            lock (_reader)
            {
                _reader.BaseStream.Seek(img.Position, SeekOrigin.Begin);
                raw = _reader.ReadBytes(total);
            }
        }
        if (raw == null || raw.Length == 0) return null;

        int primarySize = img.GetImagePayloadSize();
        int shadowSize = img.GetShadowPayloadSize();
        int offset = part == ZlImagePart.Shadow ? primarySize : primarySize + shadowSize;
        int size = part == ZlImagePart.Shadow ? img.GetShadowDataSize() : img.GetOverlayDataSize();
        if (size <= 0 || offset < 0 || offset + size > raw.Length) return null;
        byte[] segment = raw.AsSpan(offset, size).ToArray();
        var codec = part == ZlImagePart.Shadow ? img.ShadowCodec : img.OverlayCodec;
        int width = part == ZlImagePart.Shadow ? img.ShadowWidth : img.OverlayWidth;
        int height = part == ZlImagePart.Shadow ? img.ShadowHeight : img.OverlayHeight;
        byte[] decoded = ZlImageCodecUtil.DecodeToBgra(segment, codec, (short)width, (short)height);
        if (decoded.Length == width * height * 4) return decoded;

        // ZL2 的 payload 后面还可能有 BC7 fallback 段。
        int bc7Size = part == ZlImagePart.Shadow ? img.ShadowBc7DataSize : img.OverlayBc7DataSize;
        if (bc7Size > 0 && offset + size + bc7Size <= raw.Length)
        {
            byte[] bc7 = raw.AsSpan(offset + size, bc7Size).ToArray();
            return ZlImageCodecUtil.DecodeToBgra(bc7, ZlImageCodec.Bc7, (short)width, (short)height);
        }
        return decoded;
    }

    private enum ZlImagePart { Image, Shadow, Overlay }

    public void Dispose()
    {
        foreach (var tex in _texCache.Values)
            tex?.Dispose();
        _texCache.Clear();
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
    public ZlImageCodec ShadowCodec;
    public ZlImageCodec OverlayCodec;
    public int StoredImageDataSize;
    public int Bc7DataSize;
    public int FallbackDataSize;
    public int StoredShadowDataSize, ShadowBc7DataSize, ShadowFallbackDataSize;
    public int StoredOverlayDataSize, OverlayBc7DataSize, OverlayFallbackDataSize;

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
            img.ShadowCodec = (ZlImageCodec)reader.ReadByte();
            img.OverlayCodec = (ZlImageCodec)reader.ReadByte();
            reader.ReadByte(); reader.ReadByte(); reader.ReadByte(); // RuntimePreferences
            img.StoredImageDataSize = reader.ReadInt32();
            img.Bc7DataSize = reader.ReadInt32();
            img.FallbackDataSize = reader.ReadInt32();
            img.StoredShadowDataSize = reader.ReadInt32();
            img.ShadowBc7DataSize = reader.ReadInt32();
            img.ShadowFallbackDataSize = reader.ReadInt32();
            img.StoredOverlayDataSize = reader.ReadInt32();
            img.OverlayBc7DataSize = reader.ReadInt32();
            img.OverlayFallbackDataSize = reader.ReadInt32();
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

    public int GetShadowDataSize() => Version >= 2 && StoredShadowDataSize > 0
        ? StoredShadowDataSize : GetDataSize(ShadowWidth, ShadowHeight, ShadowCodec);

    public int GetOverlayDataSize() => Version >= 2 && StoredOverlayDataSize > 0
        ? StoredOverlayDataSize : GetDataSize(OverlayWidth, OverlayHeight, OverlayCodec);

    public int GetImagePayloadSize() => GetDataSize() + Bc7DataSize + FallbackDataSize;
    public int GetShadowPayloadSize() => GetShadowDataSize() + ShadowBc7DataSize + ShadowFallbackDataSize;
    public int GetOverlayPayloadSize() => GetOverlayDataSize() + OverlayBc7DataSize + OverlayFallbackDataSize;

    private static int GetDataSize(short width, short height, ZlImageCodec codec)
    {
        if (width <= 0 || height <= 0) return 0;
        return codec switch
        {
            ZlImageCodec.Bgra32 => width * height * 4,
            ZlImageCodec.Dxt1 => Math.Max(1, width / 4) * Math.Max(1, height / 4) * 8,
            ZlImageCodec.Dxt5 => Math.Max(1, width / 4) * Math.Max(1, height / 4) * 16,
            ZlImageCodec.Bc7 => Math.Max(1, width / 4) * Math.Max(1, height / 4) * 16,
            _ => 0,
        };
    }
}

public enum ZlImageCodec : byte
{
    Dxt1, Dxt5, Bgra32, Bc7, Png,
}

// ZL2 容器条目 (移植自 RenderingCore/LibraryFormat/ZlFormat.cs)
public enum ZlContainerCompression : byte
{
    None,
    DeflateFast,
    DeflateBest,
}

public sealed class Zl2Entry
{
    public byte Type;
    public int Id;
    public int UncompressedSize;
    public int CompressedSize;
    public long Offset;
    public ZlContainerCompression Compression;
    public ZlImageCodec Codec;

    public static Zl2Entry Read(BinaryReader reader)
    {
        return new Zl2Entry
        {
            Type = reader.ReadByte(),
            Id = reader.ReadInt32(),
            UncompressedSize = reader.ReadInt32(),
            CompressedSize = reader.ReadInt32(),
            Offset = reader.ReadInt64(),
            Compression = (ZlContainerCompression)reader.ReadByte(),
            Codec = (ZlImageCodec)reader.ReadByte(),
        };
    }
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
