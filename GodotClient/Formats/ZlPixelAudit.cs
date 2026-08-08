using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ZirconClient.Formats;

/// <summary>
/// 独立的原版 payload 读取器。
///
/// 这里刻意不调用 ZlLibrary.GetImageData，而是按
/// RenderingCore/Library/MirLibrary.cs 的原版顺序重新读取：
/// metadata -> image payload -> BC7 fallback -> codec 解码。
/// 这样可以把 Godot 当前路径与原版 payload 选择路径做逐像素比较，
/// 不会因为两边共用同一个 GetImageData 而产生“自测通过”。
/// </summary>
public sealed class ZlPixelReference : IDisposable
{
    private sealed class Entry
    {
        public int Id;
        public int UncompressedSize;
        public int CompressedSize;
        public long Offset;
        public ZlContainerCompression Compression;
    }

    private readonly FileStream _stream;
    private readonly BinaryReader _reader;
    private readonly Dictionary<int, Entry> _entries = new();
    private readonly bool _zl2;

    public ZlPixelReference(string fileName)
    {
        _stream = File.OpenRead(fileName);
        _reader = new BinaryReader(_stream);
        _zl2 = ReadZl2Index();
    }

    public byte[] DecodeImage(ZlLibrary library, int index)
        => DecodePart(library, index, false, false);

    public byte[] DecodeShadow(ZlLibrary library, int index)
        => DecodePart(library, index, true, false);

    public byte[] DecodeOverlay(ZlLibrary library, int index)
        => DecodePart(library, index, false, true);

    private byte[] DecodePart(ZlLibrary library, int index, bool shadow, bool overlay)
    {
        if (library?.Images == null || index < 0 || index >= library.Images.Length)
            return null;
        ZlImage image = library.Images[index];
        int width = shadow ? image?.ShadowWidth ?? 0 : overlay ? image?.OverlayWidth ?? 0 : image?.Width ?? 0;
        int height = shadow ? image?.ShadowHeight ?? 0 : overlay ? image?.OverlayHeight ?? 0 : image?.Height ?? 0;
        if (image == null || width <= 0 || height <= 0)
            return null;

        byte[] raw;
        if (_zl2)
        {
            if (!_entries.TryGetValue(image.Position, out Entry entry)) return null;
            _stream.Seek(entry.Offset, SeekOrigin.Begin);
            byte[] compressed = _reader.ReadBytes(entry.CompressedSize);
            raw = entry.Compression == ZlContainerCompression.None
                ? compressed
                : Decompress(compressed, entry.UncompressedSize);
        }
        else
        {
            if (image.Position == 0) return null;
            _stream.Seek(image.Position, SeekOrigin.Begin);
            raw = _reader.ReadBytes(image.GetImagePayloadSize()
                + image.GetShadowPayloadSize() + image.GetOverlayPayloadSize());
        }

        if (raw == null || raw.Length == 0) return null;

        int imagePayloadSize = image.GetImagePayloadSize();
        int shadowPayloadSize = image.GetShadowPayloadSize();
        int payloadOffset = shadow ? imagePayloadSize : overlay ? imagePayloadSize + shadowPayloadSize : 0;
        int primarySize = shadow ? image.GetShadowDataSize() : overlay ? image.GetOverlayDataSize() : image.GetDataSize();
        int bc7Size = shadow ? image.ShadowBc7DataSize : overlay ? image.OverlayBc7DataSize : image.Bc7DataSize;
        var codec = shadow ? image.ShadowCodec : overlay ? image.OverlayCodec : image.ImageCodec;
        if (primarySize <= 0 || payloadOffset < 0 || payloadOffset + primarySize > raw.Length) return null;
        byte[] primary = raw.AsSpan(payloadOffset, primarySize).ToArray();
        byte[] decoded = ZlImageCodecUtil.DecodeToBgra(primary, codec, (short)width, (short)height);
        int expected = width * height * 4;
        if (decoded.Length == expected) return decoded;

        // 与原版 MirImage.CreateImage 的格式回退保持一致。
        if (bc7Size > 0 && payloadOffset + primarySize + bc7Size <= raw.Length)
        {
            byte[] bc7 = raw.AsSpan(payloadOffset + primarySize, bc7Size).ToArray();
            return ZlImageCodecUtil.DecodeToBgra(bc7, ZlImageCodec.Bc7, (short)width, (short)height);
        }
        return decoded;
    }

    private bool ReadZl2Index()
    {
        if (_stream.Length < 43) return false;
        long start = _stream.Position;
        byte[] signature = _reader.ReadBytes(3);
        if (signature.Length != 3 || signature[0] != 'Z' || signature[1] != 'L' || signature[2] != '2')
        {
            _stream.Seek(start, SeekOrigin.Begin);
            return false;
        }

        _reader.ReadInt32(); // Version
        _reader.ReadInt32(); // image count
        _reader.ReadInt32(); // atlas count
        _reader.ReadByte();
        _reader.ReadByte();
        _reader.ReadInt16();
        _reader.ReadInt64(); // metadata offset
        _reader.ReadInt32(); // metadata size
        long indexOffset = _reader.ReadInt64();
        int indexSize = _reader.ReadInt32();

        _stream.Seek(indexOffset, SeekOrigin.Begin);
        using var indexStream = new MemoryStream(_reader.ReadBytes(indexSize));
        using var indexReader = new BinaryReader(indexStream);
        int count = indexReader.ReadInt32();
        for (int i = 0; i < count; i++)
        {
            indexReader.ReadByte(); // type
            var entry = new Entry
            {
                Id = indexReader.ReadInt32(),
                UncompressedSize = indexReader.ReadInt32(),
                CompressedSize = indexReader.ReadInt32(),
                Offset = indexReader.ReadInt64(),
                Compression = (ZlContainerCompression)indexReader.ReadByte(),
            };
            indexReader.ReadByte(); // codec
            _entries[entry.Id] = entry;
        }
        return true;
    }

    private static byte[] Decompress(byte[] payload, int expectedSize)
    {
        using var input = new MemoryStream(payload);
        using var deflate = new System.IO.Compression.DeflateStream(
            input, System.IO.Compression.CompressionMode.Decompress);
        using var output = new MemoryStream(Math.Max(0, expectedSize));
        deflate.CopyTo(output);
        return output.ToArray();
    }

    public void Dispose()
    {
        _reader.Dispose();
        _stream.Dispose();
    }
}

public readonly record struct ZlPixelDiff(int DifferentPixels, int DifferentBytes, byte MaxDelta);

public static class ZlPixelDiffHelper
{
    public static ZlPixelDiff Compare(byte[] expected, byte[] actual)
    {
        if (expected == null || actual == null)
            return new ZlPixelDiff(expected == actual ? 0 : 1, expected == actual ? 0 : 1, 255);

        int length = Math.Min(expected.Length, actual.Length);
        int pixels = 0, bytes = Math.Abs(expected.Length - actual.Length);
        byte max = 0;
        for (int i = 0; i < length; i++)
        {
            int delta = Math.Abs(expected[i] - actual[i]);
            if (delta == 0) continue;
            bytes++;
            if (delta > max) max = (byte)delta;
            if ((i & 3) == 0) pixels++;
        }
        return new ZlPixelDiff(pixels, bytes, max);
    }
}
