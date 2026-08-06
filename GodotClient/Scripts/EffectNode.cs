using System;
using Godot;
using Library;
using ZirconClient.Formats;

namespace ZirconClient.Scripts;

// 临时魔法/打击特效: 在指定格子画一帧 Magic.Zl 图片, 短暂显示后自删
public partial class EffectNode : Node2D
{
    private ZlLibrary _lib;
    private int _frame;
    private double _bornMs;
    private Func<Vector2> _posFn;
    private const double LifeMs = 500;

    public void Setup(int cellX, int cellY, Func<Vector2> posFn)
    {
        _lib = LibraryCache.Get(LibraryFile.Magic);
        _frame = 0;
        _posFn = posFn;
        _bornMs = Godot.Time.GetTicksMsec();
        ZIndex = 60;
    }

    public override void _Process(double delta)
    {
        if (_posFn != null) Position = _posFn();
        if (Godot.Time.GetTicksMsec() - _bornMs > LifeMs)
        {
            QueueFree();
            return;
        }
        QueueRedraw();
    }

    public override void _Draw()
    {
        if (_lib == null || _frame < 0 || _frame >= _lib.Images.Length) return;
        var img = _lib.Images[_frame];
        if (img == null || img.Width <= 0 || img.Height <= 0) return;
        var tex = _lib.GetImageTexture(_frame);
        if (tex == null) return;
        DrawTextureRectRegion(tex,
            new Rect2(-img.Width / 2f, -img.Height / 2f, img.Width, img.Height),
            new Rect2(0, 0, img.Width, img.Height));
    }
}
