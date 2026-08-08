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
    private readonly CanvasItemMaterial _blendMaterial = new()
    {
        BlendMode = CanvasItemMaterial.BlendModeEnum.Mix
    };
    private Node2D _source;
    private Node2D _target;
    private ZlLibrary _library;
    private int _startIndex;
    private float _imageScale;
    private double _expireAt;
    private Vector2[] _points = System.Array.Empty<Vector2>();
    private Vector2[] _velocities = System.Array.Empty<Vector2>();
    private bool _initialized;

    public void Setup(Node2D source, Node2D target, LibraryFile file, int startIndex, float imageScale, double lifetimeMs, bool blend = false)
    {
        _source = source;
        _target = target;
        _library = LibraryCache.Get(file);
        _startIndex = startIndex;
        _imageScale = Mathf.Max(0.01f, imageScale);
        _expireAt = Godot.Time.GetTicksMsec() + lifetimeMs;
        ZIndex = 10000;
        Material = blend ? _blendMaterial : null;
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
        if (_points.Length != count)
        {
            _points = new Vector2[count];
            _velocities = new Vector2[count];
            _initialized = false;
        }
        if (!_initialized)
        {
            for (int i = 0; i < count; i++) _points[i] = a.Lerp(b, i / (float)(count - 1));
            System.Array.Clear(_velocities, 0, _velocities.Length);
            _initialized = true;
        }

        // 旧端 MirLineEffect 的中间链节：Gravity=.05、SpringStrength=.15、Damping=.9。
        _points[0] = a;
        _points[^1] = b;
        for (int i = 1; i < count - 1; i++)
        {
            _velocities[i] += new Vector2(0, .05f);
            Vector2 midpoint = (_points[i - 1] + _points[i + 1]) * .5f;
            _velocities[i] += (midpoint - _points[i]) * .15f;
            _points[i] += _velocities[i];
            _velocities[i] *= .9f;
        }
        QueueRedraw();
    }

    public override void _Draw()
    {
        if (_library == null || _points.Length < 2 || _startIndex < 0 || _startIndex >= _library.Images.Length) return;
        var image = _library.Images[_startIndex];
        // 旧版 MirLineEffect 使用 ImageType.Image，即使 Blend=true 也不能
        // 走技能特效的黑色颜色键缓存；普通图像中的黑色可能是有效链条像素。
        var texture = _library.GetImageTexture(_startIndex);
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
