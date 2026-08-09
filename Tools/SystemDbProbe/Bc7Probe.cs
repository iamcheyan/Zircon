// Bc7Probe — 用 BCnEncoder (客户端同款) 解码 ZL2 库的指定帧, 输出原始 BGRA。
// 用法: dotnet run --project Tools/SystemDbProbe -- --bc7 <lib> <frame> <out.raw>
// 输出文件头: 4B 宽 + 4B 高 (int32 LE), 之后为 BGRA 像素 (w*h*4)。
using System.IO.Compression;
using BCnEncoder.Decoder;
using BCnEncoder.Shared;

public static class Bc7Probe
{
    public static void Run(string libPath, int frame, string outPath)
    {
        byte[] d = File.ReadAllBytes(libPath);
        if (d.Length < 4 || d[0] != (byte)'Z' || d[1] != (byte)'L' || d[2] != (byte)'2')
            throw new InvalidDataException("不是 ZL2 库: " + libPath);

        // header (19B 起): metadataOffset long(19), metadataSize int(27), indexOffset long(31), indexSize int(39)
        long metaOff = BitConverter.ToInt64(d, 19);
        int metaSize = BitConverter.ToInt32(d, 27);
        long idxOff = BitConverter.ToInt64(d, 31);
        int idxSize = BitConverter.ToInt32(d, 39);

        // index: entryCount + Zl2Entry × n (23B each)
        var entries = new Dictionary<int, (int unc, int comp, long off, byte compr, byte codec)>();
        int p = (int)idxOff;
        int n = BitConverter.ToInt32(d, p); p += 4;
        for (int i = 0; i < n; i++)
        {
            byte type = d[p];
            int id = BitConverter.ToInt32(d, p + 1);
            int unc = BitConverter.ToInt32(d, p + 5);
            int comp = BitConverter.ToInt32(d, p + 9);
            long off = BitConverter.ToInt64(d, p + 13);
            byte c = d[p + 21];
            byte codec = d[p + 22];
            entries[id] = (unc, comp, off, c, codec);
            p += 23;
        }

        // metadata
        int mp = (int)metaOff;
        int mv = BitConverter.ToInt32(d, mp); mp += 4;
        int count = BitConverter.ToInt32(d, mp); mp += 4;
        mp += 8; // atlasGroup, atlasPageSize
        int imgPos = -1, w = 0, h = 0, imgCodec = -1, stored = 0, bc7 = 0, fallback = 0;
        for (int i = 0; i < count; i++)
        {
            bool present = d[mp] != 0; mp += 1;
            if (!present) continue;
            int position = BitConverter.ToInt32(d, mp);
            w = BitConverter.ToInt16(d, mp + 4);
            h = BitConverter.ToInt16(d, mp + 6);
            int stype = d[mp + 12];
            mp += 25;
            mp += 4; // atlasPage
            mp += 16; // source + visible rects
            imgCodec = d[mp]; mp += 3; // codecs
            mp += 3; // prefs
            stored = BitConverter.ToInt32(d, mp);
            bc7 = BitConverter.ToInt32(d, mp + 4);
            fallback = BitConverter.ToInt32(d, mp + 8);
            mp += 36;
            if (i == frame)
            {
                imgPos = position;
                break;
            }
        }
        if (imgPos < 0 || !entries.TryGetValue(imgPos, out var e))
            throw new InvalidDataException($"帧 {frame} 无 payload entry");

        byte[] payload = Decompress(d, e);
        byte[] seg = payload.AsSpan(0, Math.Min(stored, payload.Length)).ToArray();
        int useCodec = seg.Length > 0 ? imgCodec : 3; // 主段失败回退 BC7

        byte[] bgra;
        if (useCodec == 3)
        {
            var dec = new BcDecoder();
            var px = dec.DecodeRaw(seg, w, h, CompressionFormat.Bc7);
            bgra = new byte[w * h * 4];
            for (int i = 0; i < px.Length && i * 4 + 3 < bgra.Length; i++)
            {
                bgra[i * 4] = px[i].b;
                bgra[i * 4 + 1] = px[i].g;
                bgra[i * 4 + 2] = px[i].r;
                bgra[i * 4 + 3] = px[i].a;
            }
        }
        else if (useCodec == 2) // Bgra32
        {
            bgra = seg;
        }
        else
        {
            throw new InvalidDataException($"codec {useCodec} 未支持 (帧 {frame})");
        }

        using var fs = File.Create(outPath);
        fs.Write(BitConverter.GetBytes(w));
        fs.Write(BitConverter.GetBytes(h));
        fs.Write(bgra);
        Console.WriteLine($"BC7 帧 {frame}: {w}x{h} codec={useCodec} stored={stored} -> {outPath}");
    }

    private static byte[] Decompress(byte[] d, (int unc, int comp, long off, byte compr, byte codec) e)
    {
        if (e.compr == 0)
            return d.AsSpan((int)e.off, e.unc).ToArray();
        using var input = new MemoryStream(d, (int)e.off, e.comp);
        using var deflate = new DeflateStream(input, CompressionMode.Decompress);
        using var output = new MemoryStream(e.unc);
        deflate.CopyTo(output);
        return output.ToArray();
    }
}
