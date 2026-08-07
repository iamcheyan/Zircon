using Godot;

namespace ZirconClient.Controls;

/// <summary>
/// 地图标记控件 (移植自 Client/Controls/DXMapInfoControl.cs, 简化版)。
/// 纯色方块; Hollow=true 时只画边框 (玩家当前位置标记)。
/// 原版的 BorderAnimation/Overlay 动画发光系统 M12 不移植。
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
