using Godot;

namespace ZirconClient.Controls;

/// <summary>
/// 地图标记控件 (移植自 Client/Controls/DXMapInfoControl.cs)。
/// 纯色方块; Hollow=true 时只画边框 (玩家当前位置标记)。
/// 标记层保持原版的实心/空心绘制语义。
/// </summary>
public partial class DXMapInfoControl : DXControl
{
    public bool Hollow;

    protected override void DrawControl()
    {
        if (BackColour.A <= 0) return;

        if (Hollow)
            DrawRect(new Rect2(Vector2.Zero, Size), BackColour, false, 1f);
        else
            DrawRect(new Rect2(Vector2.Zero, Size), BackColour);
    }
}
