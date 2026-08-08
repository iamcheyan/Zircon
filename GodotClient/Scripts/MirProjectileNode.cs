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

    private Func<int, int, Vector2> _cameraFnByCell;
    private int _targetCellX;
    private int _targetCellY;

    public void SetupProjectile(LibraryFile file, int startIndex, int frameCount, double frameDelayMs,
        MapObjectNode target, int mapCellX, int mapCellY,
        System.Drawing.Point origin, Func<int, int, Vector2> cameraFnByCell)
    {
        Setup(file, startIndex, frameCount, frameDelayMs, target, mapCellX, mapCellY, null);

        Origin = origin;
        Direction = MirDirection.Up;
        _cameraFnByCell = cameraFnByCell;
        _targetCellX = mapCellX;
        _targetCellY = mapCellY;
    }

    public void SetupProjectileTarget(LibraryFile file, int startIndex, int frameCount, double frameDelayMs,
        Node2D target, Func<int> targetRenderYFn, System.Drawing.Point origin,
        Func<int, int, Vector2> cameraFnByCell)
    {
        SetupTarget(file, startIndex, frameCount, frameDelayMs, target, targetRenderYFn);
        Origin = origin;
        Direction = MirDirection.Up;
        _cameraFnByCell = cameraFnByCell;
        _targetCellX = 0;
        _targetCellY = 0;
    }

    public override void _Process(double delta)
    {
        double now = Godot.Time.GetTicksMsec();

        if (_cameraFnByCell == null)
        {
            CompleteAction?.Invoke();
            QueueFree();
            return;
        }

        // 原版每帧重新取目标位置。这样目标移动、玩家滚屏和玩家自身的
        // MovingOffSet 都会反映到飞行轨迹，而不是沿着旧屏幕坐标漂移。
        Vector2 originScreen = _cameraFnByCell(Origin.X, Origin.Y);
        Vector2 targetScreen = _targetNode != null
            // 与 MirEffectNode 一致：目标特效锚在对象节点（objectBaseline），
            // 不还原到旧端格子原点帧，否则相对身体恒高 32px。
            ? _targetNode.Position
            : _cameraFnByCell(_targetCellX, _targetCellY);
        var origin = ToLegacyProjectilePoint(originScreen);
        var target = ToLegacyProjectilePoint(targetScreen);

        Direction = Functions.DirectionFromPoint(origin, target);
        Direction16 = Functions.Direction16(origin, target);
        long duration = Functions.Distance(origin, target);
        if (Delay > 0) duration *= Delay;
        if (!Has16Directions) Direction16 /= 2;

        if (duration <= 0)
        {
            CompleteAction?.Invoke();
            QueueFree();
            return;
        }

        // MirProjectile 的帧序列与普通 MirEffect 不同：飞行期间循环播放，
        // 到达目标才结束，而不是播放完 frameCount 就提前消失。
        int frame = GetProjectileFrame(now);
        if (Reversed) frame = FrameCount - frame - 1;
        if (frame != _frameIndex)
        {
            _frameIndex = frame;
            FrameIndexChanged?.Invoke(frame);
        }

        double t = Math.Clamp((now - _startMs) / duration, 0, 1);
        Position = originScreen.Lerp(targetScreen, (float)t);

        Position += new Vector2(AdditionalOffX, AdditionalOffY);

        // 投射物在飞行过程中跨越多个地图行，必须和地形/角色共享动态
        // RenderY 排序；只在 Setup 时计算一次会导致火球穿过建筑或角色。
        if (DrawType == EffectLayer.Object)
        {
            int renderY = (int)MathF.Round(Mathf.Lerp(Origin.Y, CurrentRenderY, (float)t));
            ZIndex = 100 + renderY;
        }
        else
        {
            UpdateRenderLayer();
        }

        // 原版 MirProjectile 的结束条件不是“一到 duration 就删除”：
        // 没有对象目标且未设置 Explode 的直线投射物，要继续绘制到完全
        // 离开视口。否则长距离/穿屏技能会在目标格突然消失。
        if (now - _startMs > duration)
        {
            if (_targetNode == null && !Explode && IsProjectileVisible())
            {
                QueueRedraw();
                return;
            }

            CompleteAction?.Invoke();
            QueueFree();
            return;
        }

        QueueRedraw();
    }

    private bool IsProjectileVisible()
    {
        if (_lib == null || _frameIndex < 0 || FrameCount <= 0) return false;
        int frame = _frameIndex + StartIndex + Direction16 * Skip;
        if (frame < 0 || frame >= _lib.Images.Length) return false;
        var image = _lib.Images[frame];
        if (image == null || image.Width <= 0 || image.Height <= 0) return false;

        Rect2 bounds = new(Position.X + (UseOffSet ? image.OffSetX : 0f),
            Position.Y + (UseOffSet ? image.OffSetY : 0f), image.Width, image.Height);
        return bounds.Intersects(new Rect2(Vector2.Zero, GetViewportRect().Size));
    }

    private static System.Drawing.Point ToLegacyProjectilePoint(Vector2 screen)
    {
        // 原客户端把等距屏幕 Y 从 32 高度换算为 48 高度后才计算
        // 方向和距离；这里必须保持该顺序，不能直接用 Godot 的屏幕 Y。
        return new System.Drawing.Point((int)screen.X, (int)(screen.Y / 32f * 48f));
    }

    private int GetProjectileFrame(double now)
    {
        double total = TotalDuration;
        if (total <= 0 || FrameCount <= 0) return 0;
        double elapsed = (now - _startMs) % total;
        for (int i = 0; i < Delays.Length; i++)
        {
            elapsed -= Delays[i];
            if (elapsed < 0) return i;
        }
        return FrameCount - 1;
    }

    public override void _Draw()
    {
        // MirProjectile ultimately calls the same legacy DrawBlend path as
        // MirEffect, whose blend mode is normal alpha compositing.
        Material = null;
        if (_lib == null || _frameIndex < 0) return;
        // 投射物帧: frame + StartIndex + Direction16 * Skip (16 向)
        int df = _frameIndex + StartIndex + Direction16 * Skip;
        if (df < 0 || df >= _lib.Images.Length) return;

        var img = _lib.Images[df];
        if (img == null || img.Width <= 0 || img.Height <= 0) return;
        // Legacy MirEffect/MirProjectile also renders ImageType.Image.
        var tex = _lib.GetImageTexture(df);
        if (tex == null) return;

        // MirProjectile 继承旧版 MirEffect.Draw：useOffSet=false 时，传入
        // 的 DrawX/DrawY 是贴图左上角，不是中心点。只有独立的 centered
        // 粒子绘制路径才会减去半宽/半高。
        float ox = UseOffSet ? img.OffSetX : 0f;
        float oy = UseOffSet ? img.OffSetY : 0f;

        var destRect = new Rect2(ox, oy, img.Width, img.Height);
        var srcRect = new Rect2(0, 0, img.Width, img.Height);

        Color c = Blend
            ? new Color(FrameLightColour.R, FrameLightColour.G, FrameLightColour.B,
                BlendRate * FrameLightColour.A)
            : new Color(FrameLightColour.R, FrameLightColour.G, FrameLightColour.B,
                Opacity * FrameLightColour.A);
        DrawTextureRectRegion(tex, destRect, srcRect, c);
    }
}
