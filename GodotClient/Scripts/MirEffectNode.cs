using System;
using Godot;
using Library;
using ZirconClient.Formats;

namespace ZirconClient.Scripts;

/// <summary>
/// 通用序列帧特效节点（移植自原版 Client/Models/MirEffect.cs）。
/// 支持: 目标/地图锚定、Delays 帧推进、Loop/Reversed、Blend 混合、
///       方向×Skip、CompleteAction/FrameIndexChanged 回调。
/// 用 Godot 序列帧节点承载原版 EffectNode 的完整播放语义。
/// </summary>
public partial class MirEffectNode : Node2D
{
    protected CanvasItemMaterial _blendMaterial;
    public enum EffectLayer
    {
        Floor,
        Object,
        Final,
    }

    // 锚定: 跟随对象(_target!=null)或固定格子坐标
    protected MapObjectNode _target;
    protected Node2D _targetNode;
    private Func<int> _targetRenderYFn;
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
    public bool UseOffSet = true; // true 使用图库 OffSet；false 从节点左上角绘制
    public int FrameLight;
    public Color FrameLightColour = Colors.White;

    // 生命周期
    protected double _startMs;
    public Action CompleteAction;
    public Action<int> FrameIndexChanged;
    protected int _frameIndex = -1;

    // Z 排序 (对应原版 DrawType: Floor/Object/Final)
    public int ZLayer = 60;
    public EffectLayer DrawType = EffectLayer.Object;

    // 附加偏移
    public int AdditionalOffX, AdditionalOffY;

    /// <summary>对应旧端 MirEffect.StartTime，和每帧 Delay 完全独立。</summary>
    public void SetStartDelay(double delayMs) => _startMs = Godot.Time.GetTicksMsec() + Math.Max(0, delayMs);

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
        UpdateRenderLayer();
    }

    /// <summary>
    /// 旧端 MirEffect.Target 的对象锚定版本。PlayerRenderer 和 MapObjectNode
    /// 都必须跟随实时 Position，不能退化成施法包中的静态格子。
    /// </summary>
    public void SetupTarget(LibraryFile file, int startIndex, int frameCount, double frameDelayMs,
        Node2D target, Func<int> targetRenderYFn)
    {
        _lib = LibraryCache.Get(file);
        StartIndex = startIndex;
        FrameCount = frameCount;
        _target = target as MapObjectNode;
        _targetNode = target;
        _targetRenderYFn = targetRenderYFn;
        Delays = new double[frameCount];
        for (int i = 0; i < frameCount; i++) Delays[i] = frameDelayMs;
        _startMs = Godot.Time.GetTicksMsec();
        UpdateRenderLayer();
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
        if (_targetNode != null)
            // MapObjectNode 使用对象基线，而旧端 MirEffect.Target 使用
            // MapObject.DrawY；当前对象基线多一格，需还原到旧端锚点。
            Position = _targetNode.Position - new Vector2(0f, 32f);
        else if (_cameraFn != null)
            Position = _cameraFn();

        Position += new Vector2(AdditionalOffX, AdditionalOffY);
        UpdateRenderLayer();

        QueueRedraw();
    }

    protected void UpdateRenderLayer()
    {
        ZIndex = DrawType switch
        {
            EffectLayer.Floor => 50,
            EffectLayer.Final => 10000,
            _ => 100 + (_targetRenderYFn?.Invoke() ?? _target?.CellY ?? MapCellY),
        };
    }

    protected int CurrentRenderY => _targetRenderYFn?.Invoke() ?? _target?.CellY ?? MapCellY;

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
        // 原版 MirLibrary.DrawBlend 使用亮化混合，不是普通 Alpha 淡化。
        // 每个特效节点独立设置 Material，避免把世界对象或同节点的其它层
        // 错误地改成 Add 混合。
        Material = Blend ? (_blendMaterial ??= new CanvasItemMaterial
        {
            BlendMode = CanvasItemMaterial.BlendModeEnum.Add
        }) : null;
        if (_lib == null || _frameIndex < 0) return;
        int df = DrawFrame;
        if (df < 0 || df >= _lib.Images.Length) return;

        var img = _lib.Images[df];
        if (img == null || img.Width <= 0 || img.Height <= 0) return;

        // Client/Models/MirEffect.Draw uses ImageType.Image for every effect.
        // The effect black-key path is not equivalent to the legacy renderer
        // and can remove the subject beneath shield/overlay frames.
        var tex = _lib.GetImageTexture(df);
        if (tex == null) return;

        // MirLibrary.Draw uses the supplied position as the top-left when
        // useOffSet=false; centered particles use their own centered path.
        float ox = UseOffSet ? img.OffSetX : 0f;
        float oy = UseOffSet ? img.OffSetY : 0f;

        var destRect = new Rect2(ox, oy, img.Width, img.Height);
        var srcRect = new Rect2(0, 0, img.Width, img.Height);

        if (Blend)
        {
            // Godot 混合: 用 BlendRate 降 alpha (近似原版 DrawBlend)
            Color c = new(FrameLightColour.R, FrameLightColour.G, FrameLightColour.B,
                BlendRate * FrameLightColour.A);
            DrawTextureRectRegion(tex, destRect, srcRect, c);
        }
        else
        {
            Color c = new(FrameLightColour.R, FrameLightColour.G, FrameLightColour.B,
                Opacity * FrameLightColour.A);
            DrawTextureRectRegion(tex, destRect, srcRect, c);
        }
    }
}
