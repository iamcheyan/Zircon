using Godot;
using Library;
using ZirconClient.Formats;

namespace ZirconClient.Scripts;

/// <summary>
/// 旧端 MirLineEffect 的 Godot 版本：按实时两端位置分节绘制链条，
/// 而不是把链条压成目标身上的一张静态序列帧。
/// </summary>
public partial class MirLineEffectNode : Node2D
{
    private Node2D _source;
    private Node2D _target;
    private ZlLibrary _library;
    private int _startIndex;
    private float _imageScale;
    private double _expireAt;
    private Vector2[] _points = System.Array.Empty<Vector2>();

    public void Setup(Node2D source, Node2D target, LibraryFile file, int startIndex, float imageScale, double lifetimeMs)
    {
        _source = source;
        _target = target;
        _library = LibraryCache.Get(file);
        _startIndex = startIndex;
        _imageScale = Mathf.Max(0.01f, imageScale);
        _expireAt = Godot.Time.GetTicksMsec() + lifetimeMs;
        ZIndex = 10000;
    }

    public override void _Process(double delta)
    {
        if (!IsInstanceValid(_source) || !IsInstanceValid(_target) || Godot.Time.GetTicksMsec() >= _expireAt)
        {
            QueueFree();
            return;
        }

        Vector2 a = _source.Position - new Vector2(0, 50);
        Vector2 b = _target.Position - new Vector2(0, 50);
        float distance = a.DistanceTo(b);
        int count = Mathf.Max(2, Mathf.CeilToInt(distance / (30f * _imageScale)) + 1);
        if (_points.Length != count) _points = new Vector2[count];

        for (int i = 0; i < count; i++)
        {
            float t = i / (float)(count - 1);
            float sag = Mathf.Sin(t * Mathf.Pi) * Mathf.Min(28f, distance * 0.12f);
            _points[i] = a.Lerp(b, t) + new Vector2(0, sag);
        }
        QueueRedraw();
    }

    public override void _Draw()
    {
        if (_library == null || _points.Length < 2 || _startIndex < 0 || _startIndex >= _library.Images.Length) return;
        var image = _library.Images[_startIndex];
        var texture = _library.GetEffectTexture(_startIndex);
        if (image == null || texture == null || image.Width <= 0 || image.Height <= 0) return;

        for (int i = 0; i < _points.Length - 1; i++)
        {
            Vector2 p1 = _points[i];
            Vector2 p2 = _points[i + 1];
            Vector2 delta = p2 - p1;
            float length = delta.Length();
            Vector2 middle = (p1 + p2) * 0.5f;
            DrawSetTransform(middle, Mathf.Atan2(delta.Y, delta.X) + Mathf.Pi / 2f,
                new Vector2(_imageScale, length / 30f));
            DrawTextureRectRegion(texture,
                new Rect2(-image.Width / 2f, -image.Height / 2f, image.Width, image.Height),
                new Rect2(0, 0, image.Width, image.Height), new Color(1, 1, 1, 0.85f));
        }
        DrawSetTransform(Vector2.Zero, 0, Vector2.One);
    }
}
