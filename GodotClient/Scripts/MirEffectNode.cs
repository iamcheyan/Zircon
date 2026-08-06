using System;
using Godot;
using Library;
using ZirconClient.Formats;

namespace ZirconClient.Scripts;

/// <summary>
/// 通用序列帧特效节点（移植自原版 Client/Models/MirEffect.cs）。
/// 支持: 目标/地图锚定、Delays 帧推进、Loop/Reversed、Blend 混合、
///       方向×Skip、CompleteAction/FrameIndexChanged 回调。
/// 替代 M5 的单帧 EffectNode 占位。
/// </summary>
public partial class MirEffectNode : Node2D
{
    // 锚定: 跟随对象(_target!=null)或固定格子坐标
    protected MapObjectNode _target;
    public int MapCellX, MapCellY;
    protected Func<Vector2> _cameraFn; // GameScene.ComputeObjectScreenPos 委托

    // 图库
    protected ZlLibrary _lib;

    // 帧序列
    public int StartIndex;
    public int FrameCount;
    public double[] Delays;       // 每帧延迟(ms)
    public bool Loop;
    public bool Reversed;
    public int Skip = 10;         // 方向间隔(原版默认10)
    public MirDirection Direction;

    // 绘制
    public bool Blend;            // 混合绘制
    public float BlendRate = 0.7f;
    public float Opacity = 1f;
    public bool UseOffSet = true; // 用图库 OffSet 居中

    // 生命周期
    protected double _startMs;
    public Action CompleteAction;
    public Action<int> FrameIndexChanged;
    protected int _frameIndex = -1;

    // Z 排序 (对应原版 DrawType: Floor/Object/Final)
    public int ZLayer = 60;

    // 附加偏移
    public int AdditionalOffX, AdditionalOffY;

    /// <summary>
    /// 初始化特效。
    /// file: 图库; startIndex/frameCount: 起始帧与帧数; frameDelayMs: 每帧毫秒;
    /// target: 跟随对象(null=用格子); mapCellX/Y: 地图格子; cameraFn: 格子→屏幕坐标换算。
    /// </summary>
    public void Setup(LibraryFile file, int startIndex, int frameCount, double frameDelayMs,
        MapObjectNode target, int mapCellX, int mapCellY, Func<Vector2> cameraFn)
    {
        _lib = LibraryCache.Get(file);
        StartIndex = startIndex;
        FrameCount = frameCount;
        _target = target;
        MapCellX = mapCellX;
        MapCellY = mapCellY;
        _cameraFn = cameraFn;

        Delays = new double[frameCount];
        for (int i = 0; i < frameCount; i++)
            Delays[i] = frameDelayMs;

        _startMs = Godot.Time.GetTicksMsec();
        ZIndex = ZLayer;
    }

    public void SetDelay(int frame, double ms)
    {
        if (frame >= 0 && frame < Delays.Length) Delays[frame] = ms;
    }

    public override void _Process(double delta)
    {
        double now = Godot.Time.GetTicksMsec();
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

        // 位置跟随目标或固定格子
        if (_target != null)
            Position = _target.Position;
        else if (_cameraFn != null)
            Position = _cameraFn();

        Position += new Vector2(AdditionalOffX, AdditionalOffY);

        QueueRedraw();
    }

    protected int GetFrame(double now)
    {
        double elapsed = now - _startMs;

        if (Loop)
            elapsed = elapsed % TotalDuration;
        else if (elapsed >= TotalDuration)
            return FrameCount;

        if (Reversed)
        {
            for (int i = 0; i < Delays.Length; i++)
            {
                elapsed -= Delays[Delays.Length - 1 - i];
                if (elapsed >= 0) continue;
                return i;
            }
        }
        else
        {
            for (int i = 0; i < Delays.Length; i++)
            {
                elapsed -= Delays[i];
                if (elapsed >= 0) continue;
                return i;
            }
        }
        return FrameCount;
    }

    public double TotalDuration
    {
        get
        {
            double t = 0;
            foreach (var d in Delays) t += d;
            return t;
        }
    }

    public int DrawFrame => _frameIndex + StartIndex + (int)Direction * Skip;

    public override void _Draw()
    {
        if (_lib == null || _frameIndex < 0) return;
        int df = DrawFrame;
        if (df < 0 || df >= _lib.Images.Length) return;

        var img = _lib.Images[df];
        if (img == null || img.Width <= 0 || img.Height <= 0) return;

        var tex = _lib.GetImageTexture(df);
        if (tex == null) return;

        // UseOffSet: 用图库 OffSet 居中(原版 Draw 用 OffSetX/OffSetY)
        float ox = UseOffSet ? img.OffSetX : -img.Width / 2f;
        float oy = UseOffSet ? img.OffSetY : -img.Height / 2f;

        var destRect = new Rect2(ox, oy, img.Width, img.Height);
        var srcRect = new Rect2(0, 0, img.Width, img.Height);

        if (Blend)
        {
            // Godot 混合: 用 BlendRate 降 alpha (近似原版 DrawBlend)
            Color c = new(1, 1, 1, BlendRate);
            DrawTextureRectRegion(tex, destRect, srcRect, c);
        }
        else
        {
            Color c = new(1, 1, 1, Opacity);
            DrawTextureRectRegion(tex, destRect, srcRect, c);
        }
    }
}