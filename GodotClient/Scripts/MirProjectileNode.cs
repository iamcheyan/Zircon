using System;
using Godot;
using Library;
using ZirconClient.Formats;

namespace ZirconClient.Scripts;

/// <summary>
/// 飞行物特效节点（移植自原版 Client/Models/MirProjectile.cs）。
/// 从 Origin 格子飞向 Target/MapCell，按时间线性插值位置，
/// 到达后触发 CompleteAction 并自删。支持 16 方向帧、Explode、Delay 倍率。
/// </summary>
public partial class MirProjectileNode : MirEffectNode
{
    public System.Drawing.Point Origin;
    public int Speed = 50;
    public bool Explode;
    public int Delay;            // 越大越慢(duration *= Delay)
    public int Direction16;
    public bool Has16Directions = true;

    // 缓存: 起点与终点的屏幕坐标(Setup 时算一次)
    private Vector2 _originScreen;
    private Vector2 _targetScreen;
    private double _durationMs;  // 飞行总时长
    private bool _initialized;

    public void SetupProjectile(LibraryFile file, int startIndex, int frameCount, double frameDelayMs,
        MapObjectNode target, int mapCellX, int mapCellY,
        System.Drawing.Point origin, Func<int, int, Vector2> cameraFnByCell)
    {
        Setup(file, startIndex, frameCount, frameDelayMs, target, mapCellX, mapCellY, null);

        Origin = origin;
        Direction = MirDirection.Up; // 占位, 16 向单独用 Direction16

        _originScreen = cameraFnByCell(origin.X, origin.Y);
        _targetScreen = cameraFnByCell(mapCellX, mapCellY);

        // 16 方向
        Direction16 = Functions.Direction16(
            new System.Drawing.Point((int)_originScreen.X, (int)(_originScreen.Y / 32 * 48)),
            new System.Drawing.Point((int)_targetScreen.X, (int)(_targetScreen.Y / 32 * 48)));

        long dist = Functions.Distance(
            new System.Drawing.Point((int)_originScreen.X, (int)(_originScreen.Y / 32 * 48)),
            new System.Drawing.Point((int)_targetScreen.X, (int)(_targetScreen.Y / 32 * 48)));

        _durationMs = dist; // 1 tick = 1ms 近似(原版用 TimeSpan.TicksPerMillisecond)
        if (Delay > 0) _durationMs *= Delay;
        if (!Has16Directions) Direction16 /= 2;

        _initialized = true;
    }

    public override void _Process(double delta)
    {
        double now = Godot.Time.GetTicksMsec();

        if (_initialized && _durationMs > 0 && now - _startMs > _durationMs)
        {
            // 到达目标
            CompleteAction?.Invoke();
            QueueFree();
            return;
        }

        // 帧推进(用基类逻辑但跳过它的位置跟随)
        int frame = GetFrame(now);
        if (Reversed) frame = FrameCount - frame - 1;
        if (frame >= FrameCount)
        {
            CompleteAction?.Invoke();
            QueueFree();
            return;
        }
        if (frame != _frameIndex)
        {
            _frameIndex = frame;
            FrameIndexChanged?.Invoke(frame);
        }

        // 位置: Origin→Target 线性插值
        if (_initialized && _durationMs > 0)
        {
            double t = Math.Clamp((now - _startMs) / _durationMs, 0, 1);
            Position = _originScreen.Lerp(_targetScreen, (float)t);
        }
        else if (_cameraFn != null)
        {
            Position = _cameraFn();
        }

        Position += new Vector2(AdditionalOffX, AdditionalOffY);

        QueueRedraw();
    }

    public override void _Draw()
    {
        if (_lib == null || _frameIndex < 0) return;
        // 投射物帧: frame + StartIndex + Direction16 * Skip (16 向)
        int df = _frameIndex + StartIndex + Direction16 * Skip;
        if (df < 0 || df >= _lib.Images.Length) return;

        var img = _lib.Images[df];
        if (img == null || img.Width <= 0 || img.Height <= 0) return;
        var tex = _lib.GetImageTexture(df);
        if (tex == null) return;

        float ox = UseOffSet ? img.OffSetX : -img.Width / 2f;
        float oy = UseOffSet ? img.OffSetY : -img.Height / 2f;

        var destRect = new Rect2(ox, oy, img.Width, img.Height);
        var srcRect = new Rect2(0, 0, img.Width, img.Height);

        Color c = Blend ? new Color(1, 1, 1, BlendRate) : new Color(1, 1, 1, Opacity);
        DrawTextureRectRegion(tex, destRect, srcRect, c);
    }
}