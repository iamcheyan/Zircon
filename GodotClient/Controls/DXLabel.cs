using Godot;

namespace ZirconClient.Controls;

/// <summary>
/// 文本标签 (移植自 Client/Controls/DXLabel.cs)。
/// 用 MirSkin 的中文字体绘制; 支持对齐/描边/阴影/自动尺寸。
/// </summary>
public partial class DXLabel : DXControl
{
    public int FontSize = 12;

    private Color _textColour = Colors.White;
    public Color TextColour
    {
        get => _textColour;
        set { _textColour = value; QueueRedraw(); }
    }

    public bool DrawOutline;
    public Color OutlineColour = Colors.Black;
    public bool DrawShadow;

    public HorizontalAlignment Align = HorizontalAlignment.Left;
    public VerticalAlignment VAlign = VerticalAlignment.Top;

    /// <summary>true: 尺寸跟随文字大小 (旧 DXLabel 默认)</summary>
    public bool AutoSize = true;

    protected override void DrawControl()
    {
        if (string.IsNullOrEmpty(Text)) return;
        var font = MirSkin.GetFont();
        if (font == null) return;

        Vector2 textSize = MirSkin.MeasureText(Text, FontSize);
        Vector2 pos = Vector2.Zero;

        if (Align == HorizontalAlignment.Center) pos.X = (Size.X - textSize.X) / 2f;
        else if (Align == HorizontalAlignment.Right) pos.X = Size.X - textSize.X;
        if (VAlign == VerticalAlignment.Center) pos.Y = (Size.Y - textSize.Y) / 2f;
        else if (VAlign == VerticalAlignment.Bottom) pos.Y = Size.Y - textSize.Y;

        Color colour = IsEnabled ? TextColour : new Color(TextColour, 0.5f);

        if (DrawOutline)
            DrawStringOutline(font, pos, Text, HorizontalAlignment.Left, -1, FontSize, 4, OutlineColour);
        else if (DrawShadow)
            DrawStringOutline(font, pos + new Vector2(1, 1), Text, HorizontalAlignment.Left, -1, FontSize, 2, new Color(0, 0, 0, 0.7f));

        DrawString(font, pos, Text, HorizontalAlignment.Left, -1, FontSize, colour);
    }

    public override void _Draw()
    {
        // 标签通常无背景, 直接画文字; 保留基类背景能力但跳过自身背景绘制顺序
        base._Draw();
    }

    protected override void OnControlAdded(DXControl c) { }
}
