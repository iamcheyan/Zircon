using Godot;
using Library;

namespace ZirconClient.Controls;

/// <summary>
/// 贴图控件 (移植自 Client/Controls/DXImageControl.cs)。
/// 用 .Zl 图库的一张图绘制自己; 支持普通/悬停/按下三态与偏移。
/// </summary>
public partial class DXImageControl : DXControl
{
    public LibraryFile LibraryFile = LibraryFile.Interface;

    private int _index = -1;
    public int Index
    {
        get => _index;
        set
        {
            if (_index == value) return;
            _index = value;
            QueueRedraw();
            if (!FixedSize)
                Size = MirSkin.GetSize(LibraryFile, value);
        }
    }

    public int HoverIndex = -1;
    public int PressedIndex = -1;

    /// <summary>true: 尺寸固定为 Size, 不随图自动调整 (贴图偏移仍生效)</summary>
    public bool FixedSize;

    public bool DrawImage = true;

    /// <summary>true: 绘制时加上图的 OffSetX/OffSetY 偏移</summary>
    public bool UseOffSet = true;

    public float ImageOpacity = 1f;
    public bool GrayScale;

    protected override void DrawControl()
    {
        if (!DrawImage) return;

        int idx = GetCurrentIndex();
        if (idx < 0) return;

        var tex = MirSkin.GetTexture(LibraryFile, idx);
        if (tex == null) return;

        Vector2I off = UseOffSet ? MirSkin.GetOffset(LibraryFile, idx) : Vector2I.Zero;
        Rect2 dest = new(off, tex.GetSize());

        if (ImageOpacity < 1f)
        {
            var old = SelfModulate;
            SelfModulate = new Color(1, 1, 1, ImageOpacity);
            DrawTextureRect(tex, dest, false);
            SelfModulate = old;
        }
        else
        {
            DrawTextureRect(tex, dest, false);
        }

        if (!IsEnabled)
        {
            // 禁用: 压一层半透明黑灰
            DrawRect(dest, new Color(0.25f, 0.25f, 0.25f, 0.6f));
        }
        else if (GrayScale)
        {
            DrawRect(dest, new Color(0.5f, 0.5f, 0.5f, 0.4f));
        }
    }

    /// <summary>当前应显示的图索引 (按下 > 悬停 > 普通)</summary>
    public int GetCurrentIndex()
    {
        if (IsPressed && PressedIndex >= 0) return PressedIndex;
        if (IsHovered && HoverIndex >= 0) return HoverIndex;
        return Index;
    }
}
