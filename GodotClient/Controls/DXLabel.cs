using System.Collections.Generic;
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

    /// <summary>在文字底部画一条下划线（旧版物品链接 FontStyle.Underline 的移植）。</summary>
    public bool DrawUnderline;

    public HorizontalAlignment Align = HorizontalAlignment.Left;
    public VerticalAlignment VAlign = VerticalAlignment.Top;

    /// <summary>文字绘制内边距（悬浮提示框等带背景的标签用，避免文字贴边）。</summary>
    public Vector2I TextPadding;

    /// <summary>true: 尺寸跟随文字大小 (旧 DXLabel 默认)</summary>
    public bool AutoSize = true;

    protected override void DrawControl()
    {
        if (string.IsNullOrEmpty(Text)) return;
        var font = MirSkin.GetFont();
        if (font == null) return;

        var lines = GetLines();
        Vector2 textSize = MirSkin.MeasureText(lines.Count == 0 ? string.Empty : lines[0], FontSize);
        Vector2 pos = TextPadding;

        if (Align == HorizontalAlignment.Center) pos.X = TextPadding.X + (Size.X - textSize.X - TextPadding.X * 2) / 2f;
        else if (Align == HorizontalAlignment.Right) pos.X = Size.X - textSize.X - TextPadding.X;
        float lineHeight = textSize.Y;
        float blockHeight = lineHeight * lines.Count;
        if (VAlign == VerticalAlignment.Center) pos.Y = TextPadding.Y + (Size.Y - blockHeight - TextPadding.Y * 2) / 2f;
        else if (VAlign == VerticalAlignment.Bottom) pos.Y = Size.Y - blockHeight - TextPadding.Y;

        Color colour = IsEnabled ? TextColour : new Color(TextColour, 0.5f);

        for (int i = 0; i < lines.Count; i++)
        {
            Vector2 linePos = new(pos.X, pos.Y + i * lineHeight);
            int drawSize = MirSkin.ScaledSize(FontSize);
            if (DrawOutline)
                DrawStringOutline(font, linePos, lines[i], HorizontalAlignment.Left, -1, drawSize, 4, OutlineColour);
            else if (DrawShadow)
                DrawStringOutline(font, linePos + new Vector2(1, 1), lines[i], HorizontalAlignment.Left, -1, drawSize, 2, new Color(0, 0, 0, 0.7f));
            DrawString(font, linePos, lines[i], HorizontalAlignment.Left, -1, drawSize, colour);
            if (DrawUnderline)
                DrawLine(new Vector2(linePos.X, linePos.Y + drawSize + 1),
                    new Vector2(linePos.X + textSize.X, linePos.Y + drawSize + 1), colour, 1f);
        }
    }

    private List<string> GetLines()
    {
        var result = new List<string>();
        foreach (var source in (Text ?? string.Empty).Replace("\r", string.Empty).Split('\n'))
        {
            if (AutoSize || Size.X <= 0)
            {
                result.Add(source);
                continue;
            }
            string line = string.Empty;
            foreach (char ch in source)
            {
                string candidate = line + ch;
                if (line.Length > 0 && MirSkin.MeasureText(candidate, FontSize).X > Size.X)
                {
                    result.Add(line);
                    line = ch.ToString();
                }
                else line = candidate;
            }
            result.Add(line);
        }
        return result.Count == 0 ? new List<string> { string.Empty } : result;
    }

    public override void _Draw()
    {
        // 标签通常无背景, 直接画文字; 保留基类背景能力但跳过自身背景绘制顺序
        base._Draw();
    }

    protected override void OnControlAdded(DXControl c) { }
}
