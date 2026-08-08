using System.Drawing;

namespace Zircon.BotRunner;

/// <summary>
/// Minimal read-only map collision data for local movement decisions.
/// The authoritative movement check remains on the server; this only keeps
/// bots from repeatedly walking into the same static wall.
/// </summary>
public sealed class BotMap
{
    private readonly bool[,] _blocked;

    private BotMap(bool[,] blocked) => _blocked = blocked;

    public int Width => _blocked.GetLength(0);
    public int Height => _blocked.GetLength(1);

    public bool CanWalk(Point point)
        => point.X >= 0 && point.Y >= 0 && point.X < Width && point.Y < Height && !_blocked[point.X, point.Y];

    public static BotMap Load(string path)
    {
        if (!File.Exists(path)) return null;

        try
        {
            using var stream = File.OpenRead(path);
            using var reader = new BinaryReader(stream);
            reader.BaseStream.Seek(22, SeekOrigin.Begin);
            int width = reader.ReadInt16();
            int height = reader.ReadInt16();
            if (width <= 0 || height <= 0 || width > 4096 || height > 4096) return null;

            reader.BaseStream.Seek(28 + (width / 2) * (height / 2) * 3, SeekOrigin.Begin);
            var blocked = new bool[width, height];
            for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
            {
                byte flag = reader.ReadByte();
                blocked[x, y] = (flag & 0x03) != 0x03;
                reader.ReadBytes(13);
            }

            return new BotMap(blocked);
        }
        catch (IOException) { return null; }
        catch (ArgumentException) { return null; }
    }
}
