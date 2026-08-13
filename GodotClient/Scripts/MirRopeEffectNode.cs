using Godot;
using Library;
using ZirconClient.Formats;

namespace ZirconClient.Scripts;

/// <summary>旧端 MirRopeEffect：先从施法者飞向目标，落地后保持绳索的下垂物理。</summary>
public partial class MirRopeEffectNode : Node2D
{
    private readonly CanvasItemMaterial _blendMaterial = new()
    {
        BlendMode = CanvasItemMaterial.BlendModeEnum.Mix
    };
    private Node2D _source;
    private Node2D _target;
    private ZlLibrary _library;
    private readonly System.Collections.Generic.List<Vector2> _points = new();
    private readonly System.Collections.Generic.List<Vector2> _velocities = new();
    private long _launchStart;
    private bool _landed;
    private const float LinkLength = 30f;
    private const float LaunchDuration = 600f;
    private const float ThrowArcHeight = 120f;
    private const float OvershootFactor = 1.15f;

    public void Setup(Node2D source, Node2D target)
    {
        _source = source;
        _target = target;
        _library = LibraryCache.Get(LibraryFile.MagicEx7);
        _launchStart = (long)Godot.Time.GetTicksMsec();
        ZIndex = 10000;
        // MirRopeEffect 的旧版基类 Blend 默认是 false；保留普通 Alpha
        // 绘制，只有明确传入 Blend 的链条才使用 Add。
        Material = null;
        Rebuild(2, AnchorSource(_source));
    }

    public override void _Process(double delta)
    {
        if (!IsInstanceValid(_source) || !IsInstanceValid(_target)) { QueueFree(); return; }
        Vector2 start = AnchorSource(_source), end = AnchorTarget(_target);
        if (!_landed)
        {
            float t = Mathf.Clamp((float)((long)Godot.Time.GetTicksMsec() - _launchStart) / LaunchDuration, 0f, 1.2f);
            Vector2 tip = ThrownTarget(start, end, t);
            int count = Mathf.Max(2, Mathf.CeilToInt(start.DistanceTo(tip) / (LinkLength * .5f)) + 1);
            Rebuild(count, start);
            for (int i = 0; i < _points.Count; i++) _points[i] = start.Lerp(tip, i / (float)(_points.Count - 1));
            if (t >= 1f) _landed = true;
        }
        else
        {
            int count = Mathf.Max(2, Mathf.CeilToInt(start.DistanceTo(end) / (LinkLength * .5f)) + 1);
            if (_points.Count != count) Rebuild(count, start);
            _points[0] = start; _points[^1] = end;
            for (int i = 1; i < _points.Count - 1; i++)
            {
                _velocities[i] += new Vector2(0, .05f);
                _velocities[i] += ((_points[i - 1] + _points[i + 1]) * .5f - _points[i]) * .15f;
                _points[i] += _velocities[i]; _velocities[i] *= .9f;
            }
        }
        QueueRedraw();
    }

    public override void _Draw()
    {
        // 设置开关门控：关闭"显示特效/粒子"时不绘制（客户端特效唯一总闸）
        if (!ClientSettings.DrawEffects && !ClientSettings.DrawParticles) return;
        if (_library == null || _points.Count < 2 || _library.Images.Length <= 81) return;
        var image = _library.Images[81];
        // MirRopeEffect 继承 MirLineEffect，旧版源图类型是 Image；不要
        // 对绳索素材执行技能特效的黑色透明键清理。
        var texture = _library.GetImageTexture(81);
        if (image == null || texture == null) return;
        for (int i = 0; i < _points.Count - 1; i++)
        {
            Vector2 a = _points[i], b = _points[i + 1], d = b - a;
            DrawSetTransform((a + b) * .5f, Mathf.Atan2(d.Y, d.X) + Mathf.Pi / 2f,
                new Vector2(.5f, d.Length() / LinkLength));
            DrawTextureRect(texture, new Rect2(-image.Width / 2f, -image.Height / 2f, image.Width, image.Height), false,
                // MirRopeEffect inherits MirLineEffect with Blend=false and
                // Opacity=1; the old client does not apply an extra .9 alpha.
                Colors.White);
        }
        DrawSetTransform(Vector2.Zero, 0, Vector2.One);
    }

    // 原版 MirRopeEffect.ToWorld 使用 DrawY（格子原点），而 Godot 节点
    // Position 是 objectBaseline，因此先减 32；再叠加原版的
    // SourceOffset/TargetOffset 与 AnchorOffsetY=50。
    private static Vector2 AnchorSource(Node2D node)
    {
        Vector2 delta = DirectionOffset(DirectionOf(node), true);
        return node.Position + new Vector2(-10f + delta.X, -62f + delta.Y);
    }

    private static Vector2 AnchorTarget(Node2D node)
    {
        Vector2 delta = DirectionOffset(DirectionOf(node), false);
        return node.Position + new Vector2(8f + delta.X, -72f + delta.Y);
    }

    private static MirDirection DirectionOf(Node2D node) => node switch
    {
        PlayerRenderer player => player.Direction,
        MapObjectNode mapObject => mapObject.Direction,
        _ => MirDirection.Down,
    };

    private static Vector2 DirectionOffset(MirDirection direction, bool source)
    {
        if (source)
            return direction switch
            {
                MirDirection.Up => new Vector2(0, -50),
                MirDirection.UpRight => new Vector2(40, -35),
                MirDirection.Right => new Vector2(35, -15),
                MirDirection.DownRight => new Vector2(27, -7),
                MirDirection.DownLeft => new Vector2(-17, -10),
                MirDirection.Left => new Vector2(-25, -20),
                MirDirection.UpLeft => new Vector2(-15, -40),
                _ => Vector2.Zero,
            };

        return direction switch
        {
            MirDirection.Up => new Vector2(0, -50),
            MirDirection.UpRight => new Vector2(25, -45),
            MirDirection.UpLeft => new Vector2(-25, -45),
            MirDirection.Right => new Vector2(40, -30),
            MirDirection.Left => new Vector2(-40, -30),
            MirDirection.DownRight => new Vector2(25, -10),
            MirDirection.DownLeft => new Vector2(-25, -10),
            _ => Vector2.Zero,
        };
    }
    private void Rebuild(int count, Vector2 at)
    {
        _points.Clear(); _velocities.Clear();
        for (int i = 0; i < count; i++) { _points.Add(at); _velocities.Add(Vector2.Zero); }
    }
    private static Vector2 ThrownTarget(Vector2 start, Vector2 end, float t)
    {
        float c = Mathf.Clamp(t, 0f, 1f);
        float tx = 1f - Mathf.Pow(1f - c, 3f), ty = 1f - Mathf.Pow(1f - c, 2f);
        Vector2 result = new(start.Lerp(end, tx).X, start.Lerp(end, ty).Y);
        result.Y -= Mathf.Sin(c * Mathf.Pi) * ThrowArcHeight;
        if (t > 1f) result += (end - start) * ((t - 1f) * OvershootFactor * .5f);
        return result;
    }
}
